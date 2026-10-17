using M.Contract.Repositories.Entities;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Contract.Services.Security;
using M.Contract.Services.Storage;
using M.Core.Base;
using M.Core.Security;
using M.ModelViews.Files;
using M.ModelViews.HumanResources;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static M.Core.Base.BaseException;

namespace M.Services.Services.HumanResources;

public sealed class EmployeeHandoverService(IPrivateDocumentStorage storage, IEmploymentChangeProcessor changes, IAdminAccountGuard adminGuard, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock) : BaseService(unitOfWork, currentUser, clock), IEmployeeHandoverService
{
    private const long MaxAttachmentSize = 10 * 1024 * 1024;
    private const long MaxRequestSize = 20 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip" };

    // Frontend gửi AssetsJson/ProjectsJson ở camelCase (assetType, projectName...)
    // còn model C# dùng PascalCase. Dùng chung 1 options để bind đọc/ghi nhất quán
    // (camelCase + không phân biệt hoa thường) -> hết lỗi 400 "Vui lòng nhập dự án".
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<object> Mine()
    {
        var employeeId = CurrentEmployeeId();
        if (employeeId == Guid.Empty) throw new ErrorException(400, "VALIDATION", "Tài khoản chưa liên kết hồ sơ nhân viên.");
        return await Rows().Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.CreatedTime).ToListAsync();
    }


    public async Task<object> GetAll()
    {
        RequireStaff();
        var query = Rows();
        if (!CurrentUser.IsInRole("Admin") && !CurrentUser.IsInRole("HR"))
        {
            var managerId = CurrentEmployeeId();
            query = query.Where(x => x.ManagerId == managerId);
        }
        return await query.OrderByDescending(x => x.CreatedTime).ToListAsync();
    }


    public async Task<object> SubmitRequest(HandoverSubmission form)
    {
        using var writes = new PrivateDocumentWriteBatch(storage);
        var result = await InTransactionAsync(() => SubmitRequestCore(form, writes));
        writes.Complete();
        return result;
    }

    private async Task<object> SubmitRequestCore(HandoverSubmission form, PrivateDocumentWriteBatch writes)
    {
        var employeeId = CurrentEmployeeId();
        if (employeeId == Guid.Empty) throw new ErrorException(400, "VALIDATION", "Tài khoản chưa liên kết hồ sơ nhân viên.");
        if (DateOnly.FromDateTime(form.LastWorkingDate) < M.Core.Time.BusinessTime.DateAt(UtcNow)) throw new ErrorException(400, "VALIDATION", "Ngày làm việc cuối cùng không được ở quá khứ.");
        if (string.IsNullOrWhiteSpace(form.Reason)) throw new ErrorException(400, "VALIDATION", "Vui lòng nhập lý do nghỉ việc.");
        List<HandoverAssetRow> assets;
        List<HandoverProjectRow> projects;
        List<HandoverAttachmentInput> attachmentInputs;
        try
        {
            assets = JsonSerializer.Deserialize<List<HandoverAssetRow>>(form.AssetsJson, Json) ?? [];
            projects = JsonSerializer.Deserialize<List<HandoverProjectRow>>(form.ProjectsJson, Json) ?? [];
            attachmentInputs = JsonSerializer.Deserialize<List<HandoverAttachmentInput>>(form.AttachmentMetadataJson, Json) ?? [];
        }
        catch (JsonException) { throw new ErrorException(400, "VALIDATION", "Thông tin tài sản hoặc dự án không hợp lệ."); }
        if (projects.Count == 0 || projects.Any(x => string.IsNullOrWhiteSpace(x.ProjectName) || x.Progress is < 0 or > 100))
            throw new ErrorException(400, "VALIDATION", "Vui lòng nhập dự án bàn giao và tiến độ từ 0 đến 100%.");
        // Frontend gửi ProjectsJson với documents/handoverFiles rỗng; file thật nằm trong
        // multipart "Attachments" và chỉ gắn vào project sau khi lưu. Vì vậy kiểm tra bắt buộc
        // phải dựa trên metadata đính kèm (mỗi dự án cần >=1 tài liệu và >=1 file bàn giao).
        for (var i = 0; i < projects.Count; i++)
        {
            var hasDoc = attachmentInputs.Any(x => x.ProjectIndex == i && x.Kind == "documents");
            var hasFile = attachmentInputs.Any(x => x.ProjectIndex == i && x.Kind == "handoverFiles");
            if (!hasDoc) throw new ErrorException(400, "VALIDATION", $"Dự án #{i + 1} phải có ít nhất một tài liệu dự án.");
            if (!hasFile) throw new ErrorException(400, "VALIDATION", $"Dự án #{i + 1} phải có ít nhất một file bàn giao.");
        }
        foreach (var project in projects)
        {
            project.Documents = [];
            project.HandoverFiles = [];
        }
        var files = form.Attachments ?? [];
        if (files.Count != attachmentInputs.Count) throw new ErrorException(400, "VALIDATION", "Danh sách tệp đính kèm không khớp.");
        if (files.Any(f => f.Length == 0 || f.Length > MaxAttachmentSize || !AllowedExtensions.Contains(Path.GetExtension(f.FileName))) || files.Sum(f => f.Length) > MaxRequestSize)
            throw new ErrorException(400, "VALIDATION", "Tệp phải là PDF, Office hoặc ZIP; tối đa 10 MB mỗi tệp và 20 MB tổng cộng.");
        if (attachmentInputs.Any(x => x.ProjectIndex < 0 || x.ProjectIndex >= projects.Count || (x.Kind != "documents" && x.Kind != "handoverFiles")))
            throw new ErrorException(400, "VALIDATION", "Vị trí tệp đính kèm không hợp lệ.");

        var employee = await Repository<Employee>().Entities.Include(x => x.Department).FirstOrDefaultAsync(x => x.Id == employeeId);
        if (employee == null) throw new ErrorException(404, "NOT_FOUND", "Không tìm thấy hồ sơ nhân viên.");
        if (employee.Status is EmployeeStatus.Resigned or EmployeeStatus.Terminated) throw new ErrorException(409, "CONFLICT", "Nhân viên đã nghỉ việc hoặc chấm dứt hợp đồng.");
        var managerId = employee.ManagerId ?? employee.Department?.ManagerId;
        if (!managerId.HasValue || managerId == employeeId) throw new ErrorException(400, "VALIDATION", "Hồ sơ chưa có quản lý trực tiếp để duyệt bàn giao.");
        if (await Repository<EmployeeHandover>().Entities.AnyAsync(x => x.EmployeeId == employeeId && (x.Status == EmployeeHandoverStatus.Pending || (x.Status == EmployeeHandoverStatus.Approved && x.AppliedAt == null))))
            throw new ErrorException(409, "CONFLICT", "Bạn đang có yêu cầu bàn giao chờ duyệt.");

        var handover = new EmployeeHandover
        {
            EmployeeId = employeeId,
            ManagerId = managerId.Value,
            LastWorkingDate = form.LastWorkingDate.Date,
            Reason = form.Reason.Trim(),
            CompanyAssets = string.Join("\n", assets.Select(x => $"{x.AssetCode} · {x.AssetType} · {x.Condition}")),
            WorkProgress = string.Join("\n", projects.Select(x => $"{x.ProjectCode} · {x.ProjectName} · {x.Progress}%")),
            AssetsJson = JsonSerializer.Serialize(assets, Json),
            ProjectsJson = JsonSerializer.Serialize(projects, Json),
            AccountIssued = employee.UserId.HasValue,
            Status = EmployeeHandoverStatus.Pending
        };
        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var metadata = attachmentInputs[i];
            var storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
            await writes.SaveAsync(DocumentArea.EmployeeHandovers, handover.Id, storedName, file);
            var info = new HandoverFileInfo { FileName = Path.GetFileName(file.FileName), StoredName = storedName, FileSize = file.Length };
            if (metadata.Kind == "documents") projects[metadata.ProjectIndex].Documents.Add(info);
            else projects[metadata.ProjectIndex].HandoverFiles.Add(info);
        }
        handover.ProjectsJson = JsonSerializer.Serialize(projects, Json);
        Repository<EmployeeHandover>().Insert(handover);
        await UnitOfWork.SaveAsync();
        return new { message = "Đã gửi yêu cầu bàn giao cho cấp trên." };
    }

    public async Task<DocumentDownload> DownloadFile(Guid id, string storedName)
    {
        var handover = await Repository<EmployeeHandover>().Entities.FirstOrDefaultAsync(x => x.Id == id);
        if (handover == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (!await CanAccess(handover)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        var projects = JsonSerializer.Deserialize<List<HandoverProjectRow>>(handover.ProjectsJson, Json) ?? [];
        var metadata = projects.SelectMany(x => x.Documents.Concat(x.HandoverFiles)).FirstOrDefault(x => x.StoredName == storedName);
        if (metadata == null || Path.GetFileName(storedName) != storedName) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        return storage.Open(DocumentArea.EmployeeHandovers, id, storedName, metadata.FileName);
    }


    public Task<object> Review(Guid id, HandoverReview form) { RequireStaff(); return InTransactionAsync(() => ReviewCore(id, form)); }

    private async Task<object> ReviewCore(Guid id, HandoverReview form)
    {
        var handover = await Repository<EmployeeHandover>().Entities.FirstOrDefaultAsync(x => x.Id == id);
        if (handover == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (handover.Status != EmployeeHandoverStatus.Pending) throw new ErrorException(409, "CONFLICT", "Yêu cầu bàn giao này đã được xử lý.");
        var elevated = CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR");
        var reviewerId = CurrentEmployeeId();
        if (!elevated && (!CurrentUser.IsInRole("Manager") || handover.ManagerId != reviewerId)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (!form.Approve && string.IsNullOrWhiteSpace(form.Note)) throw new ErrorException(400, "VALIDATION", "Vui lòng nhập lý do từ chối.");
        var employee = await Repository<Employee>().Entities.FirstOrDefaultAsync(x => x.Id == handover.EmployeeId);
        if (employee == null) throw new ErrorException(404, "NOT_FOUND", "Không tìm thấy hồ sơ nhân viên.");
        handover.Status = form.Approve ? EmployeeHandoverStatus.Approved : EmployeeHandoverStatus.Rejected;
        handover.ReviewNote = form.Note?.Trim();
        handover.ReviewedByEmployeeId = reviewerId == Guid.Empty ? null : reviewerId;
        handover.ReviewedByName = reviewerId == Guid.Empty
            ? CurrentUserName ?? "Quản trị viên"
            : await Repository<Employee>().Entities.Where(x => x.Id == reviewerId).Select(x => x.GivenName + " " + x.FamilyName).FirstOrDefaultAsync() ?? CurrentUserName;
        handover.ReviewedAt = UtcNow.UtcDateTime;
        if (form.Approve)
        {
            if (employee.UserId.HasValue) await adminGuard.EnsureCanManageAsync(employee.UserId.Value);
            await changes.ApplyHandoverIfDueAsync(handover);
        }
        await UnitOfWork.SaveAsync();
        return new { message = form.Approve ? "Đã duyệt bàn giao; hồ sơ nghỉ việc và tài khoản được áp dụng sau ngày làm việc cuối cùng." : "Đã từ chối yêu cầu bàn giao." };
    }

    private IQueryable<HandoverRow> Rows() =>
        from h in Repository<EmployeeHandover>().Entities
        join e in Repository<Employee>().Entities on h.EmployeeId equals e.Id
        join manager in Repository<Employee>().Entities on h.ManagerId equals manager.Id
        join department in Repository<Department>().Entities on e.DepartmentId equals department.Id into departments
        from department in departments.DefaultIfEmpty()
        select new HandoverRow
        {
            Id = h.Id, EmployeeId = e.Id, EmployeeCode = e.EmployeeCode,
            EmployeeName = e.GivenName + " " + e.FamilyName,
            DepartmentName = department == null ? "" : department.Name,
            ManagerId = h.ManagerId, ManagerName = manager.GivenName + " " + manager.FamilyName,
            LastWorkingDate = h.LastWorkingDate, Reason = h.Reason, CompanyAssets = h.CompanyAssets,
            WorkProgress = h.WorkProgress, AccountIssued = h.AccountIssued, Status = h.Status,
            AssetsJson = h.AssetsJson, ProjectsJson = h.ProjectsJson,
            ReviewNote = h.ReviewNote, ReviewedByName = h.ReviewedByName,
            ReviewedAt = h.ReviewedAt, AppliedAt = h.AppliedAt, CreatedTime = h.CreatedTime
        };

    private Guid CurrentEmployeeId() => CurrentUser.EmployeeId ?? Guid.Empty;

    private Task<bool> CanAccess(EmployeeHandover handover) => Task.FromResult(
        CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR") ||
        handover.EmployeeId == CurrentEmployeeId() ||
        (CurrentUser.IsInRole("Manager") && handover.ManagerId == CurrentEmployeeId()));
}
