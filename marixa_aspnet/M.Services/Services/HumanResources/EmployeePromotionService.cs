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

public sealed class EmployeePromotionService(IEmploymentChangeProcessor changes, IAdminAccountGuard adminGuard, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock) : BaseService(unitOfWork, currentUser, clock), IEmployeePromotionService
{
    private static readonly string[] AssignableRoles = ["Employee", "Manager", "HR"];

    public async Task<object> Mine()
    {
        var employeeId = CurrentEmployeeId();
        if (employeeId == Guid.Empty) throw new ErrorException(400, "VALIDATION", "Tài khoản chưa liên kết hồ sơ nhân viên.");
        return await Query().Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.CreatedTime).ToListAsync();
    }


    public async Task<object> GetAll()
    {
        RequireStaff();
        var query = Query();
        if (!CurrentUser.IsInRole("Admin") && !CurrentUser.IsInRole("HR"))
        {
            var managerId = CurrentEmployeeId();
            query = query.Where(x => x.ManagerId == managerId || x.DepartmentManagerId == managerId || x.ProposedByEmployeeId == managerId);
        }
        return await query.OrderByDescending(x => x.CreatedTime).ToListAsync();
    }

    public async Task<object> Options()
    {
        var canNominate = CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR") || CurrentUser.IsInRole("Manager");
        var employeeQuery = Repository<Employee>().Entities.Where(e => canNominate && e.Status != EmployeeStatus.Resigned && e.Status != EmployeeStatus.Terminated);
        if (canNominate && !CurrentUser.IsInRole("Admin") && !CurrentUser.IsInRole("HR"))
        {
            var managerId = CurrentEmployeeId();
            employeeQuery = employeeQuery.Where(e => e.ManagerId == managerId || e.Department!.ManagerId == managerId);
        }
        var currentEmployeeId = CurrentEmployeeId();
        return new
        {
            Employees = await employeeQuery
            .Select(e => new { e.Id, e.EmployeeCode, Name = e.GivenName + " " + e.FamilyName, e.DepartmentId, Department = e.Department == null ? "" : e.Department.Name, Position = e.Position == null ? "" : e.Position.Name, e.UserId }).ToListAsync(),
            CurrentEmployee = await Repository<Employee>().Entities.Where(e => e.Id == currentEmployeeId)
            .Select(e => new { e.Id, e.EmployeeCode, Name = e.GivenName + " " + e.FamilyName, Department = e.Department == null ? "" : e.Department.Name, Position = e.Position == null ? "" : e.Position.Name }).FirstOrDefaultAsync(),
            Positions = await Repository<Position>().Entities.Where(p => p.IsActive).Select(p => new { p.Id, p.Code, p.Name }).ToListAsync(),
            Roles = canNominate ? AssignableRoles : ["Employee"]
        };
    }

    public Task<object> CreateRequest(PromotionRequest form) { return InTransactionAsync(() => CreateRequestCore(form)); }

    private async Task<object> CreateRequestCore(PromotionRequest form)
    {
        if (string.IsNullOrWhiteSpace(form.Reason) || form.Reason.Trim().Length < 5) throw new ErrorException(400, "VALIDATION", "Vui lòng nhập lý do đề xuất (ít nhất 5 ký tự).");
        if (form.NewPositionId == Guid.Empty) throw new ErrorException(400, "VALIDATION", "Vui lòng chọn chức vụ đề xuất.");
        if (DateOnly.FromDateTime(form.EffectiveDate) < M.Core.Time.BusinessTime.DateAt(UtcNow)) throw new ErrorException(400, "VALIDATION", "Ngày hiệu lực không được nằm trong quá khứ.");
        if (!AssignableRoles.Contains(form.TargetRoleName, StringComparer.OrdinalIgnoreCase)) throw new ErrorException(400, "VALIDATION", "Vai trò được chọn không hợp lệ.");
        var targetRole = AssignableRoles.First(x => x.Equals(form.TargetRoleName, StringComparison.OrdinalIgnoreCase));
        var userId = CurrentUserId();
        var proposerId = CurrentEmployeeId();
        if (userId == Guid.Empty || proposerId == Guid.Empty) throw new ErrorException(400, "VALIDATION", "Tài khoản chưa liên kết hồ sơ nhân viên.");
        var employeeId = form.EmployeeId ?? proposerId;
        var selfRequest = employeeId == proposerId;
        if (!selfRequest && !(CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR") || CurrentUser.IsInRole("Manager"))) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (!selfRequest && !await CanManageEmployee(employeeId)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        var employee = await Repository<Employee>().Entities.Include(e => e.Position).Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == employeeId);
        var position = await Repository<Position>().Entities.FirstOrDefaultAsync(p => p.Id == form.NewPositionId && p.IsActive);
        if (employee == null || position == null) throw new ErrorException(404, "NOT_FOUND", "Không tìm thấy nhân viên hoặc chức vụ đang hoạt động.");
        if (employee.PositionId == position.Id) throw new ErrorException(400, "VALIDATION", "Nhân viên đang giữ chức vụ này.");
        if ((targetRole is "HR" or "Manager") && employee.UserId == null) throw new ErrorException(400, "VALIDATION", "Cần cấp tài khoản cho nhân viên trước khi gán quyền.");
        if (await Repository<EmployeePromotion>().Entities.AnyAsync(x => x.EmployeeId == employeeId && (x.Status == EmployeePromotionStatus.Pending || (x.Status == EmployeePromotionStatus.Approved && x.AppliedAt == null)))) throw new ErrorException(409, "CONFLICT", "Nhân viên đã có đề xuất thăng chức đang chờ xử lý.");
        var proposer = await Repository<Employee>().Entities.FirstOrDefaultAsync(e => e.Id == proposerId);
        Repository<EmployeePromotion>().Insert(new EmployeePromotion
        {
            EmployeeId = employee.Id,
            CurrentEmployeeCode = employee.EmployeeCode,
            CurrentPositionName = employee.Position?.Name,
            NewPositionId = position.Id,
            NewPositionName = position.Name,
            NewPositionCode = position.Code,
            TargetRoleName = targetRole,
            ProposedByUserId = userId,
            ProposedByEmployeeId = proposerId,
            ProposedByName = proposer == null ? CurrentUserName ?? "Người dùng" : $"{proposer.GivenName} {proposer.FamilyName}",
            ProposalType = selfRequest ? EmployeePromotionType.EmployeeRequest : EmployeePromotionType.ManagerNomination,
            Reason = form.Reason.Trim(),
            AdditionalNote = form.AdditionalNote?.Trim(),
            EffectiveDate = form.EffectiveDate.Date,
            Status = EmployeePromotionStatus.Pending
        });
        await UnitOfWork.SaveAsync();
        return new { message = "Đã gửi đề xuất thăng chức." };
    }


    public Task<object> Review(Guid id, PromotionReview form) { RequireStaff(); return InTransactionAsync(() => ReviewCore(id, form)); }

    private async Task<object> ReviewCore(Guid id, PromotionReview form)
    {
        var promotion = await Repository<EmployeePromotion>().Entities.FirstOrDefaultAsync(x => x.Id == id);
        if (promotion == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (promotion.Status != EmployeePromotionStatus.Pending) throw new ErrorException(409, "CONFLICT", "Đề xuất này đã được xử lý.");
        var elevated = CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR");
        var reviewerEmployeeId = CurrentEmployeeId();
        if (promotion.ProposedByUserId == CurrentUserId() && !elevated &&
            (promotion.ProposalType == EmployeePromotionType.EmployeeRequest || promotion.EmployeeId == reviewerEmployeeId)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (!elevated && !await CanManageEmployee(promotion.EmployeeId)) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (form.Approve && !elevated && !CurrentUser.IsInRole("Manager")) throw new ErrorException(403, ResponseCodeConstants.FORBIDDEN, "Access denied.");
        if (!form.Approve && string.IsNullOrWhiteSpace(form.Comment)) throw new ErrorException(400, "VALIDATION", "Vui lòng ghi lý do từ chối.");
        var employee = await Repository<Employee>().Entities.FirstOrDefaultAsync(e => e.Id == promotion.EmployeeId);
        if (employee == null) throw new ErrorException(404, ResponseCodeConstants.NOT_FOUND, "Resource not found.");
        if (form.Approve)
        {
            if (promotion.TargetRoleName == "HR" && !elevated) throw new ErrorException(403, "FORBIDDEN", "Only HR/Admin may approve an HR role.");
            if (employee.UserId.HasValue) await adminGuard.EnsureCanManageAsync(employee.UserId.Value);
            if (!await Repository<Position>().Entities.AnyAsync(p => p.Id == promotion.NewPositionId && p.IsActive && !p.DeletedTime.HasValue))
                throw new ErrorException(409, "POSITION_UNAVAILABLE", "The proposed position is no longer active.");
            promotion.Status = EmployeePromotionStatus.Approved;
            await changes.ApplyPromotionIfDueAsync(promotion);
        }
        else promotion.Status = EmployeePromotionStatus.Rejected;

        var reviewerId = CurrentEmployeeId();
        var reviewer = reviewerId == Guid.Empty ? null : await Repository<Employee>().GetByIdAsync(reviewerId);
        promotion.DecisionNote = form.Comment?.Trim();
        promotion.ReviewedByUserId = CurrentUserId();
        promotion.ReviewedByEmployeeId = reviewerId == Guid.Empty ? null : reviewerId;
        promotion.ReviewedByName = reviewer == null ? CurrentUserName ?? "Quản trị viên" : $"{reviewer.GivenName} {reviewer.FamilyName}";
        promotion.ReviewedAt = UtcNow.UtcDateTime;
        await UnitOfWork.SaveAsync();
        return new { message = form.Approve ? "Đã duyệt; hồ sơ và quyền được áp dụng từ ngày hiệu lực." : "Đã từ chối đề xuất." };
    }

    private IQueryable<PromotionRow> Query()
    {
        var canReviewAll = CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR");
        var canReviewAsManager = CurrentUser.IsInRole("Manager");
        var managerId = CurrentEmployeeId();
        return from x in Repository<EmployeePromotion>().Entities
               join e in Repository<Employee>().Entities on x.EmployeeId equals e.Id
               join department in Repository<Department>().Entities on e.DepartmentId equals department.Id into departments
               from department in departments.DefaultIfEmpty()
               join position in Repository<Position>().Entities on e.PositionId equals position.Id into positions
               from position in positions.DefaultIfEmpty()
               join manager in Repository<Employee>().Entities on e.ManagerId equals manager.Id into managers
               from manager in managers.DefaultIfEmpty()
               select new PromotionRow
               {
                   Id = x.Id,
                   EmployeeId = x.EmployeeId,
                   EmployeeName = e.GivenName + " " + e.FamilyName,
                   DepartmentName = department == null ? "" : department.Name,
                   ManagerId = e.ManagerId,
                   DepartmentManagerId = department == null ? null : department.ManagerId,
                   PhoneNumber = e.PhoneNumber,
                   Email = e.Email,
                   StartDate = e.StartDate,
                   CurrentPosition = position == null ? "" : position.Name,
                   ManagerName = manager == null ? "" : manager.GivenName + " " + manager.FamilyName,
                   CurrentEmployeeCode = x.CurrentEmployeeCode,
                   CurrentPositionName = x.CurrentPositionName,
                   NewEmployeeCode = x.NewEmployeeCode,
                   NewPositionId = x.NewPositionId,
                   NewPositionName = x.NewPositionName,
                   NewPositionCode = x.NewPositionCode,
                   TargetRoleName = x.TargetRoleName,
                   ProposedByName = x.ProposedByName,
                   ProposedByEmployeeId = x.ProposedByEmployeeId,
                   CanReview = canReviewAll || (canReviewAsManager && x.TargetRoleName != "HR" && x.EmployeeId != managerId && (e.ManagerId == managerId || (department != null && department.ManagerId == managerId))),
                   ProposalType = x.ProposalType,
                   Reason = x.Reason,
                   AdditionalNote = x.AdditionalNote,
                   DecisionNote = x.DecisionNote,
                   EffectiveDate = x.EffectiveDate,
                   Status = x.Status,
                   ReviewedByName = x.ReviewedByName,
                   ReviewedAt = x.ReviewedAt,
                   AppliedAt = x.AppliedAt,
                   CreatedTime = x.CreatedTime
               };
    }

    private Guid CurrentEmployeeId() => CurrentUser.EmployeeId ?? Guid.Empty;
    private Guid CurrentUserId() => CurrentUser.UserId ?? Guid.Empty;
    private Task<bool> CanManageEmployee(Guid id)
    {
        if (!IsStaff) return Task.FromResult(false);
        if (CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR")) return Task.FromResult(true);
        var managerId = CurrentEmployeeId();
        return Repository<Employee>().Entities.AnyAsync(e => e.Id == id && (e.ManagerId == managerId || e.Department!.ManagerId == managerId));
    }
}
