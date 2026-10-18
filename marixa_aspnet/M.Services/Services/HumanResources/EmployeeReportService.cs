using M.Contract.Repositories.Entities;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
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

public sealed class EmployeeReportService(IPrivateDocumentStorage storage, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock) : BaseService(unitOfWork, currentUser, clock), IEmployeeReportService
{
    private const long MaxFileSize = 20 * 1024 * 1024;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".xls", ".xlsx", ".doc", ".docx" };


    public async Task<object> GetAll(string? search, string? period,
        Guid? departmentId, Guid? employeeId, int? status,
        string? reportType, bool? overdue)
    {
        RequireStaff();
        var query = Repository<EmployeeReport>().Entities.Include(r => r.Employee).ThenInclude(e => e!.Department)
            .Include(r => r.Versions).ThenInclude(v => v.Attachments).Include(r => r.Events).AsQueryable();
        if (CurrentUser.IsInRole("Manager") && !CurrentUser.IsInRole("Admin") && !CurrentUser.IsInRole("HR"))
        {
            var managerId = CurrentEmployeeId();
            query = query.Where(r => r.Employee!.ManagerId == managerId || r.Employee.Department!.ManagerId == managerId);
        }
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.ReportCode.Contains(search) || r.Title.Contains(search) || r.Employee!.GivenName.Contains(search) || r.Employee.FamilyName.Contains(search) || r.Versions.Any(v => v.FileName.Contains(search) || v.Attachments.Any(a => a.FileName.Contains(search))));
        if (!string.IsNullOrWhiteSpace(period) && DateTime.TryParse($"{period}-01", out var month))
            query = query.Where(r => r.Period.Year == month.Year && r.Period.Month == month.Month);
        if (departmentId.HasValue) query = query.Where(r => r.Employee!.DepartmentId == departmentId);
        if (employeeId.HasValue) query = query.Where(r => r.EmployeeId == employeeId);
        if (status.HasValue) query = query.Where(r => r.Status == status);
        if (!string.IsNullOrWhiteSpace(reportType)) query = query.Where(r => r.ReportType.Contains(reportType));
        if (overdue == true) query = query.Where(r => r.Status != EmployeeReportStatus.Rejected && r.Status != EmployeeReportStatus.UpperApproved && r.Status != EmployeeReportStatus.Completed && r.Deadline.HasValue && r.Deadline < UtcNow.UtcDateTime);
        if (overdue == false) query = query.Where(r => !r.Deadline.HasValue || r.Deadline >= UtcNow.UtcDateTime);
        return await query.OrderByDescending(r => r.SubmittedAt).Select(ToResult()).ToListAsync();
    }

    public async Task<object> Mine() => await ListForEmployee(CurrentEmployeeId());

    public async Task<object> ByEmployee(Guid employeeId)
    {
        if (employeeId != CurrentEmployeeId() && !await CanManageEmployee(employeeId)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        return await ListForEmployee(employeeId);
    }


    public async Task<object> Submit(ReportSubmission form)
    {

        using var writes = new PrivateDocumentWriteBatch(storage);
        var result = await InTransactionAsync(() => SubmitCore(form, writes));
        writes.Complete();
        return result;
    }

    private async Task<object> SubmitCore(ReportSubmission form, PrivateDocumentWriteBatch writes)
    {
        var employeeId = CurrentEmployeeId();
        if (!await Repository<Employee>().Entities.AnyAsync(e => e.Id == employeeId)) throw new ErrorException(400, "VALIDATION", "Tài khoản chưa liên kết nhân viên.");
        var files = new[] { form.File }.Where(f => f != null).Cast<UploadedDocument>().Concat(form.Files ?? []).ToList();
        if (files.Count == 0) throw new ErrorException(400, "VALIDATION", "Vui lòng chọn file báo cáo.");
        var validation = ValidateFiles(files);
        if (validation != null) throw new ErrorException(400, "VALIDATION", validation);
        if (string.IsNullOrWhiteSpace(form.Title) || string.IsNullOrWhiteSpace(form.ReportType) || !DateTime.TryParse($"{form.Period}-01", out var period))
            throw new ErrorException(400, "VALIDATION", "Vui lòng nhập tên, loại và kỳ báo cáo hợp lệ.");

        var report = form.ReportId.HasValue
            ? await Repository<EmployeeReport>().Entities.Include(r => r.Versions).ThenInclude(v => v.Attachments).FirstOrDefaultAsync(r => r.Id == form.ReportId && r.EmployeeId == employeeId)
            : null;
        if (report != null && report.Status is not (EmployeeReportStatus.Rejected or EmployeeReportStatus.ChangesRequested or EmployeeReportStatus.UpperChangesRequested)) throw new ErrorException(400, "VALIDATION", "Chỉ có thể nộp lại báo cáo bị từ chối hoặc yêu cầu chỉnh sửa.");
        if (form.ReportId.HasValue && report == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        var isResubmission = report != null;
        var oldStoredNames = new List<string>();
        var now = UtcNow.UtcDateTime;
        if (report == null)
        {
            report = new EmployeeReport { ReportCode = $"BC-{period:yyyyMM}-{Guid.NewGuid():N}"[..30], EmployeeId = employeeId };
            Repository<EmployeeReport>().Insert(report);
        }
        else
        {
            var oldVersions = report.Versions.ToList();
            oldStoredNames = oldVersions.SelectMany(version => new[] { version.StoredName }.Concat(version.Attachments.Select(attachment => attachment.StoredName))).Distinct().ToList();
            foreach (var oldVersion in oldVersions) Repository<EmployeeReportVersion>().Delete(oldVersion);
            report.Versions.Clear();
            report.Status = EmployeeReportStatus.PendingManager;
            report.ManagerComment = null;
            report.UpperRequest = null;
            report.UpperRequestDeadline = null;
            report.SentToUpper = false;
            report.SentToUpperAt = null;
            report.UpperRecipientEmployeeId = null;
        }
        report.Title = form.Title.Trim();
        report.ReportType = form.ReportType.Trim();
        report.Period = period;
        report.Deadline = DateTime.TryParse(form.Deadline, out var deadline) ? deadline : null;
        report.SubmittedAt = now;
        report.ReviewedAt = null;
        report.ReviewedBy = null;
        report.Overview = form.Overview;
        report.Results = form.Results;
        report.Issues = form.Issues;
        report.Recommendations = form.Recommendations;
        var file = files[0];
        var version = new EmployeeReportVersion { Report = report, Version = report.Versions.Count + 1, FileName = Path.GetFileName(file.FileName), StoredName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName), FileSize = file.Length, UploadedBy = employeeId, UploadedAt = now };
        await writes.SaveAsync(DocumentArea.EmployeeReports, null, version.StoredName, file);
        foreach (var attachment in files.Skip(1))
        {
            var storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(attachment.FileName);
            await writes.SaveAsync(DocumentArea.EmployeeReports, null, storedName, attachment);
            version.Attachments.Add(new EmployeeReportAttachment { FileName = Path.GetFileName(attachment.FileName), StoredName = storedName, FileSize = attachment.Length });
        }
        Repository<EmployeeReportVersion>().Insert(version);
        AddEvent(report, isResubmission ? "Nhân viên nộp lại báo cáo" : "Nhân viên gửi báo cáo", null);
        if (!isResubmission) AddEvent(report, "Hệ thống chuyển báo cáo đến quản lý", null);
        await UnitOfWork.SaveAsync();
        writes.DeleteAfterCommit(DocumentArea.EmployeeReports, null, oldStoredNames);
        return new { report.Id, report.ReportCode };
    }


    public Task<object> Review(Guid id, ReviewForm form) { RequireStaff(); return InTransactionAsync(() => ReviewCore(id, form)); }

    private async Task<object> ReviewCore(Guid id, ReviewForm form)
    {
        var report = await Repository<EmployeeReport>().GetByIdAsync(id);
        if (report == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (!await CanManageReport(id)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (report.Status is not (EmployeeReportStatus.PendingManager or EmployeeReportStatus.ChangesRequested)) throw new ErrorException(400, "VALIDATION", "Báo cáo này đã được xử lý.");
        if (form.Status is not (EmployeeReportStatus.ManagerApproved or EmployeeReportStatus.Rejected or EmployeeReportStatus.ChangesRequested)) throw new ErrorException(400, "VALIDATION", "Trạng thái duyệt không hợp lệ.");
        if ((form.Status is EmployeeReportStatus.Rejected or EmployeeReportStatus.ChangesRequested) && string.IsNullOrWhiteSpace(form.Comment)) throw new ErrorException(400, "VALIDATION", "Vui lòng nhập lý do hoặc yêu cầu chỉnh sửa.");
        report.Status = form.Status;
        report.ManagerComment = form.Comment?.Trim();
        report.ReviewedBy = CurrentEmployeeIdOrNull();
        report.ReviewedAt = UtcNow.UtcDateTime;
        AddEvent(report, form.Status switch { EmployeeReportStatus.ManagerApproved => "Quản lý duyệt", EmployeeReportStatus.ChangesRequested => "Quản lý yêu cầu sửa", _ => "Quản lý từ chối" }, form.Comment);
        await UnitOfWork.SaveAsync();
        return "Đã cập nhật trạng thái báo cáo.";
    }


    public Task<object> Forward(Guid id, ForwardForm form) { RequireStaff(); return InTransactionAsync(() => ForwardCore(id, form)); }

    private async Task<object> ForwardCore(Guid id, ForwardForm form)
    {
        var report = await Repository<EmployeeReport>().GetByIdAsync(id);
        if (report == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (!await CanManageReport(id)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (report.Status is not (EmployeeReportStatus.PendingManager or EmployeeReportStatus.ChangesRequested or EmployeeReportStatus.ManagerApproved)) throw new ErrorException(400, "VALIDATION", "Báo cáo này đã được xử lý.");
        if (string.IsNullOrWhiteSpace(form.Recipient)) throw new ErrorException(400, "VALIDATION", "Chọn người nhận tiếp theo.");
        if (!form.RecipientEmployeeId.HasValue || form.RecipientEmployeeId == report.EmployeeId || !await Repository<Employee>().Entities.AnyAsync(e => e.Id == form.RecipientEmployeeId)) throw new ErrorException(400, "VALIDATION", "Người nhận cấp trên không hợp lệ.");
        if (form.Recipient.Trim().Length > 200) throw new ErrorException(400, "VALIDATION", "Thông tin người nhận vượt quá giới hạn cho phép.");
        if (form.Note?.Length > 2000) throw new ErrorException(400, "VALIDATION", "Ghi chú gửi cấp trên tối đa 2000 ký tự.");
        if (report.Status != EmployeeReportStatus.ManagerApproved)
        {
            report.ReviewedBy = CurrentEmployeeIdOrNull();
            report.ReviewedAt = UtcNow.UtcDateTime;
            AddEvent(report, "Quản lý duyệt", null);
        }
        report.Status = EmployeeReportStatus.SentToUpper;
        report.UpperRecipient = form.Recipient.Trim();
        report.UpperRecipientEmployeeId = form.RecipientEmployeeId;
        report.UpperNote = form.Note?.Trim();
        report.SentToUpper = true;
        report.SentToUpperAt = UtcNow.UtcDateTime;
        report.SentToUpperBy = CurrentEmployeeIdOrNull();
        AddEvent(report, "Quản lý gửi cấp trên", form.Note, form.Recipient);
        await UnitOfWork.SaveAsync();
        return "Đã gửi báo cáo lên cấp trên.";
    }


    public Task<object> UpperRequest(Guid id, UpperRequestForm form) { RequireStaff(); return InTransactionAsync(() => UpperRequestCore(id, form)); }

    private async Task<object> UpperRequestCore(Guid id, UpperRequestForm form)
    {
        var report = await Repository<EmployeeReport>().GetByIdAsync(id);
        if (report == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (!await CanManageReport(id)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (report.Status is not (EmployeeReportStatus.SentToUpper or EmployeeReportStatus.UpperApproved)) throw new ErrorException(400, "VALIDATION", "Báo cáo chưa được gửi cấp trên.");
        if (report.UpperRecipientEmployeeId != CurrentEmployeeId()) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (string.IsNullOrWhiteSpace(form.Request)) throw new ErrorException(400, "VALIDATION", "Nhập nội dung yêu cầu từ cấp trên.");
        report.Status = EmployeeReportStatus.UpperChangesRequested;
        report.UpperRequest = form.Request.Trim();
        report.UpperRequestDeadline = form.Deadline;
        AddEvent(report, "Cấp trên yêu cầu bổ sung", form.Request, report.UpperRecipient, form.Deadline);
        await UnitOfWork.SaveAsync();
        return "Đã ghi nhận yêu cầu từ cấp trên.";
    }


    public Task<object> UpperDecision(Guid id, UpperRequestForm form) { RequireStaff(); return InTransactionAsync(() => UpperDecisionCore(id, form)); }

    private async Task<object> UpperDecisionCore(Guid id, UpperRequestForm form)
    {
        var report = await Repository<EmployeeReport>().GetByIdAsync(id);
        if (report == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (!await CanManageReport(id)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (report.Status != EmployeeReportStatus.SentToUpper) throw new ErrorException(400, "VALIDATION", "Báo cáo chưa được gửi cấp trên.");
        if (report.UpperRecipientEmployeeId != CurrentEmployeeId()) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (form.Status == EmployeeReportStatus.UpperChangesRequested)
        {
            if (string.IsNullOrWhiteSpace(form.Request)) throw new ErrorException(400, "VALIDATION", "Nhập nội dung yêu cầu từ cấp trên.");
            report.Status = EmployeeReportStatus.UpperChangesRequested;
            report.UpperRequest = form.Request.Trim();
            report.UpperRequestDeadline = form.Deadline;
            AddEvent(report, "Cấp trên yêu cầu bổ sung", form.Request, report.UpperRecipient, form.Deadline);
        }
        else if (form.Status == EmployeeReportStatus.UpperApproved)
        {
            report.Status = EmployeeReportStatus.UpperApproved;
            AddEvent(report, "Cấp trên đã duyệt", form.Request, report.UpperRecipient);
        }
        else throw new ErrorException(400, "VALIDATION", "Trạng thái cấp trên không hợp lệ.");
        await UnitOfWork.SaveAsync();
        return "Đã cập nhật ý kiến cấp trên.";
    }


    public Task<object> Respond(Guid id, RespondForm form) { RequireStaff(); return InTransactionAsync(() => RespondCore(id, form)); }

    private async Task<object> RespondCore(Guid id, RespondForm form)
    {
        var report = await Repository<EmployeeReport>().GetByIdAsync(id);
        if (report == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (!await CanManageReport(id)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (report.Status != EmployeeReportStatus.UpperChangesRequested) throw new ErrorException(400, "VALIDATION", "Báo cáo không có yêu cầu từ cấp trên cần phản hồi.");
        if (string.IsNullOrWhiteSpace(form.Comment)) throw new ErrorException(400, "VALIDATION", "Nhập nội dung phản hồi.");
        AddEvent(report, form.TransferToEmployee ? "Chuyển nhân viên bổ sung" : "Phản hồi cấp trên", form.Comment);
        if (form.TransferToEmployee)
        {
            report.Status = EmployeeReportStatus.ChangesRequested;
            report.ManagerComment = form.Comment.Trim();
        }
        await UnitOfWork.SaveAsync();
        return "Đã lưu phản hồi.";
    }


    public Task<object> Complete(Guid id) { RequireStaff(); return InTransactionAsync(() => CompleteCore(id)); }

    private async Task<object> CompleteCore(Guid id)
    {
        var report = await Repository<EmployeeReport>().GetByIdAsync(id);
        if (report == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (!await CanManageReport(id)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (report.Status is not (EmployeeReportStatus.SentToUpper or EmployeeReportStatus.UpperApproved or EmployeeReportStatus.UpperChangesRequested)) throw new ErrorException(400, "VALIDATION", "Báo cáo chưa sẵn sàng hoàn tất.");
        report.Status = EmployeeReportStatus.Completed;
        AddEvent(report, "Hoàn tất báo cáo", null);
        await UnitOfWork.SaveAsync();
        return "Đã hoàn tất báo cáo.";
    }


    public Task<object> Viewed(Guid id) { RequireStaff(); return InTransactionAsync(() => ViewedCore(id)); }

    private async Task<object> ViewedCore(Guid id)
    {
        if (!await CanManageReport(id)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        var report = await Repository<EmployeeReport>().GetByIdAsync(id);
        if (report == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        AddEvent(report, "Quản lý mở báo cáo", null);
        await UnitOfWork.SaveAsync();
        return new object();
    }



    public async Task<object> UploadForUpper(Guid id, UploadedDocument? file, string? note, string? recipient)
    {
        RequireStaff();
        using var writes = new PrivateDocumentWriteBatch(storage);
        var result = await InTransactionAsync(() => UploadForUpperCore(id, file, note, recipient, writes));
        writes.Complete();
        return result;
    }

    private async Task<object> UploadForUpperCore(Guid id, UploadedDocument? file, string? note, string? recipient, PrivateDocumentWriteBatch writes)
    {
        var report = await Repository<EmployeeReport>().GetByIdAsync(id);
        if (report == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (!await CanManageReport(id)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (report.Status != EmployeeReportStatus.ManagerApproved) throw new ErrorException(400, "VALIDATION", "Chỉ gửi được báo cáo đã duyệt.");
        var error = ValidateFiles(file == null ? [] : [file]);
        if (error != null) throw new ErrorException(400, "VALIDATION", error);
        var upperFile = file!;
        var storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(upperFile.FileName);
        await writes.SaveAsync(DocumentArea.EmployeeReports, null, storedName, upperFile);
        if (report.UpperStoredName != null) writes.DeleteAfterCommit(DocumentArea.EmployeeReports, null, [report.UpperStoredName]);
        report.UpperStoredName = storedName;
        report.UpperFileName = Path.GetFileName(upperFile.FileName);
        report.UpperFileSize = upperFile.Length;
        report.UpperNote = note?.Trim();
        report.UpperRecipient = recipient?.Trim();
        report.SentToUpper = false;
        AddEvent(report, "File cấp trên đã tải lên", note, recipient);
        await UnitOfWork.SaveAsync();
        return "Đã tải file gửi cấp trên.";
    }


    public Task<object> MarkSent(Guid id) { RequireStaff(); return InTransactionAsync(() => MarkSentCore(id)); }

    private async Task<object> MarkSentCore(Guid id)
    {
        var report = await Repository<EmployeeReport>().GetByIdAsync(id);
        if (report == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (!await CanManageReport(id)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (report.Status != EmployeeReportStatus.ManagerApproved || string.IsNullOrEmpty(report.UpperStoredName)) throw new ErrorException(400, "VALIDATION", "Cần tải file gửi cấp trên trước.");
        report.SentToUpper = true;
        report.Status = EmployeeReportStatus.SentToUpper;
        report.SentToUpperAt = UtcNow.UtcDateTime;
        report.SentToUpperBy = CurrentEmployeeIdOrNull();
        AddEvent(report, "Quản lý gửi cấp trên", report.UpperNote, report.UpperRecipient);
        await UnitOfWork.SaveAsync();
        return "Đã đánh dấu gửi cấp trên.";
    }

    public async Task<DocumentDownload> Download(Guid versionId)
    {
        var version = await Repository<EmployeeReportVersion>().Entities.Include(v => v.Report).FirstOrDefaultAsync(v => v.Id == versionId);
        if (version == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (version.Report!.EmployeeId != CurrentEmployeeId() && !await CanManageReport(version.ReportId)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        return storage.Open(DocumentArea.EmployeeReports, null, version.StoredName, version.FileName);
    }

    public async Task<DocumentDownload> DownloadAttachment(Guid attachmentId)
    {
        var attachment = await Repository<EmployeeReportAttachment>().Entities.Include(a => a.Version).ThenInclude(v => v!.Report)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);
        if (attachment == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (attachment.Version!.Report!.EmployeeId != CurrentEmployeeId() && !await CanManageReport(attachment.Version.ReportId)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        return storage.Open(DocumentArea.EmployeeReports, null, attachment.StoredName, attachment.FileName);
    }


    public async Task<DocumentDownload> DownloadUpper(Guid id)
    {
        RequireStaff();
        var report = await Repository<EmployeeReport>().GetByIdAsync(id);
        if (report == null || report.UpperStoredName == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (!await CanManageReport(id)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        return storage.Open(DocumentArea.EmployeeReports, null, report.UpperStoredName, report.UpperFileName ?? "report");
    }

    private async Task<object> ListForEmployee(Guid employeeId)
    {
        var result = await Repository<EmployeeReport>().Entities.Include(r => r.Employee).ThenInclude(e => e!.Department)
            .Include(r => r.Versions).ThenInclude(v => v.Attachments).Include(r => r.Events).Where(r => r.EmployeeId == employeeId).OrderByDescending(r => r.SubmittedAt)
            .Select(ToResult()).ToListAsync();
        return result;
    }

    private static System.Linq.Expressions.Expression<Func<EmployeeReport, object>> ToResult() => r => new
    {
        r.Id,
        r.ReportCode,
        r.Title,
        r.ReportType,
        r.Period,
        r.Deadline,
        r.Status,
        r.SubmittedAt,
        r.ManagerComment,
        r.ReviewedBy,
        r.ReviewedAt,
        r.Overview,
        r.Results,
        r.Issues,
        r.Recommendations,
        r.UpperFileName,
        r.UpperFileSize,
        r.UpperNote,
        r.UpperRecipient,
        r.UpperRecipientEmployeeId,
        r.UpperRequest,
        r.UpperRequestDeadline,
        r.SentToUpper,
        r.SentToUpperAt,
        EmployeeId = r.EmployeeId,
        EmployeeName = r.Employee!.GivenName + " " + r.Employee.FamilyName,
        r.Employee.EmployeeCode,
        DepartmentId = r.Employee.DepartmentId,
        DepartmentName = r.Employee.Department!.Name,
        Versions = r.Versions.OrderBy(v => v.Version).Select(v => new { v.Id, v.Version, v.FileName, v.FileSize, v.UploadedBy, v.UploadedAt, Attachments = v.Attachments.Select(a => new { a.Id, a.FileName, a.FileSize }) }),
        Events = r.Events.OrderBy(e => e.OccurredAt).Select(e => new { e.ActionName, e.Comment, e.Recipient, e.ActorId, e.ActorName, e.OccurredAt, e.Deadline })
    };

    private Guid CurrentEmployeeId() => CurrentUser.EmployeeId ?? Guid.Empty;
    private Guid? CurrentEmployeeIdOrNull() => CurrentUser.EmployeeId;
    private async Task<bool> CanManageReport(Guid reportId)
    {
        if (!IsStaff) return false;
        if (CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR")) return true;
        var managerId = CurrentEmployeeId();
        return await Repository<EmployeeReport>().Entities.Where(r => r.Id == reportId).AnyAsync(r => r.Employee!.ManagerId == managerId || r.Employee.Department!.ManagerId == managerId);
    }
    private async Task<bool> CanManageEmployee(Guid employeeId)
    {
        if (!IsStaff) return false;
        if (CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR")) return true;
        var managerId = CurrentEmployeeId();
        return await Repository<Employee>().Entities.Where(e => e.Id == employeeId).AnyAsync(e => e.ManagerId == managerId || e.Department!.ManagerId == managerId);
    }
    private void AddEvent(EmployeeReport report, string action, string? comment, string? recipient = null, DateTime? deadline = null)
    {
        Repository<EmployeeReportEvent>().Insert(new EmployeeReportEvent { ReportId = report.Id, ActionName = action, Comment = comment, Recipient = recipient, ActorId = CurrentEmployeeIdOrNull(), ActorName = CurrentUserName ?? "Người dùng", OccurredAt = UtcNow.UtcDateTime, Deadline = deadline });
    }
    private static string? ValidateFiles(IEnumerable<UploadedDocument> files)
    {
        var selected = files.ToList();
        return selected.Count == 0 ? "Vui lòng chọn file báo cáo." : selected.Any(f => f.Length == 0) ? "File đính kèm không hợp lệ." : selected.Sum(f => f.Length) > MaxFileSize ? "Tổng dung lượng file tối đa 20 MB." : selected.Any(f => !Extensions.Contains(Path.GetExtension(f.FileName))) ? "Chỉ chấp nhận PDF, Excel hoặc Word." : null;
    }
}
