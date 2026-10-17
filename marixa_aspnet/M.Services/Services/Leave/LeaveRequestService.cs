using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.LeaveLedgerModelView;
using M.ModelViews.LeaveRequestModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Leave
{
    /// <summary>
    /// Nghiệp vụ nghỉ phép theo docs Marixa V1:
    ///  - Không ai được duyệt yêu cầu của chính mình.
    ///  - HR duyệt nhân viên + admin có hồ sơ; Admin duyệt HR.
    ///  - Đơn nghỉ phép có hạn mức (LeaveType.MaxDays != null): khi được
    ///    DUYỆT sinh giao dịch Deduct idempotent trong LeaveLedger (theo
    ///    LeaveRequestId + Year). Khi HỦY hợp lệ sinh Refund.
    ///  - Chỉ đơn Pending được sửa/hủy; Approved/Rejected/Canceled đóng băng.
    /// </summary>
    public class LeaveRequestService : BaseService, ILeaveRequestService
    {

        public LeaveRequestService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        // =====================================================================
        // 1. TRUY VẤN
        // =====================================================================

        public async Task<BasePaginatedList<LeaveRequestResponseModelView>> GetAllAsync(int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);
            RequireStaff();

            IGenericRepository<LeaveRequest> repo = UnitOfWork.GetRepository<LeaveRequest>();
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 200);

            IQueryable<LeaveRequest> query = repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Include(x => x.Approver)
                .Where(x => !x.DeletedTime.HasValue)
                .OrderByDescending(x => x.FromDate)
                .ThenByDescending(x => x.CreatedTime);

            int totalItems = await query.CountAsync();
            List<LeaveRequest> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<LeaveRequestResponseModelView> result = items.Select(x => x.ToViewModel()).ToList();
            return new BasePaginatedList<LeaveRequestResponseModelView>(result.AsReadOnly(), totalItems, pageNumber, pageSize);
        }

        public async Task<LeaveRequestResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<LeaveRequest> repo = UnitOfWork.GetRepository<LeaveRequest>();
            LeaveRequest entity = await repo.Entities
                .Include(x => x.Employee).Include(x => x.LeaveType).Include(x => x.Approver)
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Leave request not found");
            RequireEmployeeAccess(entity.EmployeeId);
            return entity.ToViewModel();
        }

        public async Task<List<LeaveRequestResponseModelView>> ByEmployeeIdAsync(Guid employeeId)
        {
            RequireEmployeeAccess(employeeId);
            IGenericRepository<LeaveRequest> repo = UnitOfWork.GetRepository<LeaveRequest>();
            List<LeaveRequest> entities = await repo.Entities
                .Where(x => x.EmployeeId == employeeId && !x.DeletedTime.HasValue)
                .Include(x => x.Employee).Include(x => x.LeaveType).Include(x => x.Approver)
                .OrderByDescending(x => x.FromDate)
                .ToListAsync();
            return entities.Select(x => x.ToViewModel()).ToList();
        }

        /// <summary>
        /// Số dư sổ phép (theo năm) của một nhân viên:
        /// TotalGranted = SUM(Grant) + SUM(Adjust dương)
        /// TotalUsed    = SUM(Deduct dương)
        /// Remaining    = TotalGranted - TotalUsed
        /// </summary>
        public async Task<List<LeaveLedgerSummaryModelView>> LedgerSummaryAsync(Guid employeeId, int year)
        {
            RequireEmployeeAccess(employeeId);
            var entries = await Repository<LeaveLedger>().Entities
                .Where(x => x.EmployeeId == employeeId && x.Year == year && !x.DeletedTime.HasValue)
                .ToListAsync();
            var balance = LeaveBalanceCalculator.Calculate(entries);
            return new List<LeaveLedgerSummaryModelView>
            {
                new() { EmployeeId = employeeId, Year = year, TotalGranted = balance.TotalGranted,
                    TotalUsed = balance.TotalUsed, Remaining = balance.Remaining }
            };
        }

        // =====================================================================
        // 2. TẠO / SỬA
        // =====================================================================

        public Task<Guid> CreateAsync(CreateLeaveRequestModelView model, Guid requesterEmployeeId)
        {
            ArgumentNullException.ThrowIfNull(model);
            RequireEmployeeAccess(model.EmployeeId);
            return InTransactionAsync(() => CreateAsyncCore(model, RequireCurrentEmployeeId()));
        }

        private async Task<Guid> CreateAsyncCore(CreateLeaveRequestModelView model, Guid requesterEmployeeId)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            IGenericRepository<LeaveRequest> repo = UnitOfWork.GetRepository<LeaveRequest>();
            IGenericRepository<Employee> empRepo = UnitOfWork.GetRepository<Employee>();
            IGenericRepository<LeaveType> typeRepo = UnitOfWork.GetRepository<LeaveType>();

            // Validate owner
            Employee owner = await empRepo.Entities
                .FirstOrDefaultAsync(x => x.Id == model.EmployeeId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Employee not found");

            // Only active employee can file leave
            if (owner.Status is not (EmployeeStatus.Probation or EmployeeStatus.Working or EmployeeStatus.OnLeave))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_STATUS",
                    "Employee is not in a valid status to request leave.");
            }

            // Validate leave type (active only)
            LeaveType leaveType = await typeRepo.Entities
                .FirstOrDefaultAsync(x => x.Id == model.LeaveTypeId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Leave type not found");
            if (!leaveType.IsActive)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INACTIVE_LEAVE_TYPE",
                    "Leave type is inactive.");

            // Validate dates
            if (model.ToDate.Date < model.FromDate.Date)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_INPUT",
                    "ToDate must be greater than or equal to FromDate.");

            // Only the owner or an approver with authority may create
            var roleInfo = await ResolveRolesAsync(requesterEmployeeId);
            bool isAdmin = roleInfo.IsAdmin;
            bool isHr = roleInfo.IsHr;
            bool isMgr = roleInfo.IsMgr;
            bool ownerSelf = requesterEmployeeId == model.EmployeeId;
            bool authorisedByRole = isAdmin || isHr || isMgr;
            if (!ownerSelf && !authorisedByRole)
                throw new ErrorException(StatusCodes.Status403Forbidden, "FORBIDDEN",
                    "You can only create leave requests for yourself.");

            // Prevent overlap with other open (Pending/Approved) requests
            bool overlap = await repo.Entities.AnyAsync(x =>
                x.EmployeeId == model.EmployeeId &&
                !x.DeletedTime.HasValue &&
                (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.Approved) &&
                x.FromDate.Date <= model.ToDate.Date &&
                x.ToDate.Date >= model.FromDate.Date);
            if (overlap)
                throw new ErrorException(StatusCodes.Status400BadRequest, "OVERLAP",
                    "Employee already has an open or approved leave request overlapping the specified period.");

            // For limited leave types, ensure projected remaining >= TotalDays
            decimal totalDays = model.TotalDays
                ?? (decimal)(model.ToDate.Date - model.FromDate.Date).TotalDays + 1m;
            ValidateTotalDays(totalDays, model.FromDate, model.ToDate);
            if (leaveType.MaxDays.HasValue)
            {
                int year = model.FromDate.Year;
                decimal alreadyUsedInYear = await UsedInYearAsync(owner.Id, year, leaveType.Id);
                if (alreadyUsedInYear + totalDays > leaveType.MaxDays.Value)
                    throw new ErrorException(StatusCodes.Status400BadRequest, "INSUFFICIENT_BALANCE",
                        $"Not enough {leaveType.Name} balance. Used: {alreadyUsedInYear}, requested: {totalDays}, limit: {leaveType.MaxDays}.");
            }

            string currentUser = CurrentUserName;
            LeaveRequest entity = model.ToEntity();
            entity.TotalDays = totalDays;
            entity.Status = LeaveRequestStatus.Pending;
            entity.CreatedBy = currentUser;
            entity.CreatedTime = UtcNow;
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = UtcNow;

            await repo.InsertAsync(entity);
            await UnitOfWork.SaveAsync();
            return entity.Id;
        }

        public Task UpdateAsync(UpdateLeaveRequestModelView model, Guid requesterEmployeeId)
        {
            ArgumentNullException.ThrowIfNull(model);
            RequireEmployeeAccess(model.EmployeeId);
            return InTransactionAsync(async () => { await UpdateAsyncCore(model, RequireCurrentEmployeeId()); return true; });
        }

        private async Task UpdateAsyncCore(UpdateLeaveRequestModelView model, Guid requesterEmployeeId)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            IGenericRepository<LeaveRequest> repo = UnitOfWork.GetRepository<LeaveRequest>();
            IGenericRepository<Employee> empRepo = UnitOfWork.GetRepository<Employee>();
            IGenericRepository<LeaveType> typeRepo = UnitOfWork.GetRepository<LeaveType>();

            LeaveRequest entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Leave request not found");

            // Only editable while Pending
            if (entity.Status != LeaveRequestStatus.Pending)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_STATUS",
                    "Only pending leave requests can be modified.");

            // Only owner or authorised approver can edit
            bool ownerSelf = entity.EmployeeId == requesterEmployeeId;
            (bool isAdmin, bool isHr, bool isMgr, _) = await ResolveRolesAsync(requesterEmployeeId);
            if (!ownerSelf && !(isAdmin || isHr || isMgr))
                throw new ErrorException(StatusCodes.Status403Forbidden, "FORBIDDEN",
                    "You can only modify your own pending leave requests.");

            // Validate employee & leave type again (defensive)
            bool employeeExists = await empRepo.Entities
                .AnyAsync(x => x.Id == model.EmployeeId && !x.DeletedTime.HasValue);
            if (!employeeExists)
                throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Employee not found");

            LeaveType leaveType = await typeRepo.Entities
                .FirstOrDefaultAsync(x => x.Id == model.LeaveTypeId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Leave type not found");
            if (!leaveType.IsActive)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INACTIVE_LEAVE_TYPE", "Leave type is inactive.");

            if (model.ToDate.Date < model.FromDate.Date)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_INPUT",
                    "ToDate must be greater than or equal to FromDate.");

            // Overlap check (excluding self)
            bool overlap = await repo.Entities.AnyAsync(x =>
                x.Id != model.Id &&
                x.EmployeeId == model.EmployeeId &&
                !x.DeletedTime.HasValue &&
                (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.Approved) &&
                x.FromDate.Date <= model.ToDate.Date &&
                x.ToDate.Date >= model.FromDate.Date);
            if (overlap)
                throw new ErrorException(StatusCodes.Status400BadRequest, "OVERLAP",
                    "Another open/approved leave request overlaps with the specified period.");

            decimal totalDays = model.TotalDays
                ?? (decimal)(model.ToDate.Date - model.FromDate.Date).TotalDays + 1m;
            ValidateTotalDays(totalDays, model.FromDate, model.ToDate);

            // Balance check on limited types (projected remaining after edit)
            if (leaveType.MaxDays.HasValue)
            {
                int year = model.FromDate.Year;
                decimal alreadyUsedInYear = await UsedInYearAsync(model.EmployeeId, year, leaveType.Id);
                // Exclude the entry tied to this request (which should not exist while Pending, but be safe)
                alreadyUsedInYear -= await repo.Entities.AnyAsync(x => x.Id == entity.Id && x.Status == LeaveRequestStatus.Approved)
                    ? totalDays : 0m;
                if (alreadyUsedInYear + totalDays > leaveType.MaxDays.Value)
                    throw new ErrorException(StatusCodes.Status400BadRequest, "INSUFFICIENT_BALANCE",
                        "Not enough balance for the new period.");
            }

            model.ToEntity(entity);
            entity.TotalDays = totalDays;
            entity.Status = LeaveRequestStatus.Pending; // force re-approval after edit
            entity.ApprovedBy = null;
            entity.ApprovedAt = null;

            entity.LastUpdatedBy = CurrentUserName;
            entity.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        // =====================================================================
        // 3. QUYẾT ĐỊNH (APPROVE / REJECT / CANCEL)
        // =====================================================================

        public Task ApproveAsync(Guid id, DecisionLeaveRequestModelView model, Guid approverEmployeeId)
        {
            RequireStaff(); return InTransactionAsync(async () => { await ApproveAsyncCore(id, model, RequireCurrentEmployeeId()); return true; });
        }

        private async Task ApproveAsyncCore(Guid id, DecisionLeaveRequestModelView model, Guid approverEmployeeId)
        {
            await DecideAsync(id, approverEmployeeId, LeaveRequestStatus.Approved,
                model?.Note, ledgerOnPositive: true);
        }

        public Task RejectAsync(Guid id, DecisionLeaveRequestModelView model, Guid approverEmployeeId)
        {
            RequireStaff(); return InTransactionAsync(async () => { await RejectAsyncCore(id, model, RequireCurrentEmployeeId()); return true; });
        }

        private async Task RejectAsyncCore(Guid id, DecisionLeaveRequestModelView model, Guid approverEmployeeId)
        {
            await DecideAsync(id, approverEmployeeId, LeaveRequestStatus.Rejected,
                model?.Note, ledgerOnPositive: false);
        }

        public Task CancelAsync(Guid id, CancelLeaveRequestModelView model, Guid cancellerEmployeeId)
        {
            return InTransactionAsync(async () => { await CancelAsyncCore(id, model, RequireCurrentEmployeeId()); return true; });
        }

        private async Task CancelAsyncCore(Guid id, CancelLeaveRequestModelView model, Guid cancellerEmployeeId)
        {
            IGenericRepository<LeaveRequest> repo = UnitOfWork.GetRepository<LeaveRequest>();
            LeaveRequest entity = await repo.Entities
                .Include(x => x.LeaveType)
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Leave request not found");

            bool isOwner = entity.EmployeeId == cancellerEmployeeId;
            bool isHrAdmin = CurrentUser.IsInRole(RoleNames.Admin) || CurrentUser.IsInRole(RoleNames.HR);
            if (entity.Status == LeaveRequestStatus.Cancelled)
            {
                if (!isOwner && !isHrAdmin) throw new ErrorException(403, "FORBIDDEN", "You cannot cancel this request.");
                return;
            }
            if (entity.Status == LeaveRequestStatus.Approved)
            {
                if (!isHrAdmin || isOwner)
                    throw new ErrorException(403, "FORBIDDEN", "An HR/Admin other than the owner must cancel an approved request.");
                if (string.IsNullOrWhiteSpace(model?.Reason) || model.Reason.Trim().Length < 5)
                    throw new ErrorException(400, "REASON_REQUIRED", "A cancellation reason of 5–500 characters is required.");
            }
            else if (entity.Status != LeaveRequestStatus.Pending)
                throw new ErrorException(409, "INVALID_STATUS", "Only pending or approved leave requests can be cancelled.");
            else if (!isOwner && !CurrentUser.IsInRole(RoleNames.Admin))
                throw new ErrorException(403, "FORBIDDEN", "Only the owner or an administrator can cancel a pending request.");
            if (model?.Reason?.Trim().Length > 500) throw new ErrorException(400, "VALIDATION", "Cancellation reason is too long.");
            entity.Status = LeaveRequestStatus.Cancelled;
            entity.CancelledBy = cancellerEmployeeId;
            entity.CancelledAt = UtcNow.UtcDateTime;
            entity.CancellationReason = model?.Reason?.Trim();
            entity.LastUpdatedBy = CurrentUserName;
            entity.LastUpdatedTime = UtcNow;
            await InsertLedgerRefundAsync(entity, entity.CancellationReason ?? "Hủy đơn chưa duyệt");

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        /// <summary>
        /// Shared decision path for approve/reject.
        /// Enforces:
        ///  - approver != owner (chặn tự duyệt)
        ///  - approver role is one of Admin/Manager/HR
        ///  - HR can't approve another HR's request
        ///  - Admin can approve HR's request
        ///  - Balance check for limited leave types before Deduct
        /// </summary>
        private async Task DecideAsync(
            Guid id,
            Guid approverEmployeeId,
            LeaveRequestStatus newStatus,
            string? note,
            bool ledgerOnPositive)
        {
            IGenericRepository<LeaveRequest> repo = UnitOfWork.GetRepository<LeaveRequest>();
            LeaveRequest entity = await repo.Entities
                .Include(x => x.Employee).Include(x => x.LeaveType)
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Leave request not found");

            if (entity.Status != LeaveRequestStatus.Pending)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_STATUS",
                    "Only pending leave requests can be approved or rejected.");

            // Chặn tự duyệt
            if (entity.EmployeeId == approverEmployeeId)
                throw new ErrorException(StatusCodes.Status403Forbidden, "SELF_APPROVAL_NOT_ALLOWED",
                    "You cannot approve or reject your own leave request.");

            // Role approver
            (bool isAdmin, bool isHr, bool isMgr, _) = await ResolveRolesAsync(approverEmployeeId);
            if (!isAdmin && !isHr && !isMgr)
                throw new ErrorException(StatusCodes.Status403Forbidden, "FORBIDDEN",
                    "Only HR, Manager, or Admin can approve/reject leave requests.");

            // HR không được duyệt HR khác; Manager không duyệt HR/Admin
            (bool ownerIsAdmin, bool ownerIsHr, bool ownerIsMgr, _) =
                await ResolveRolesAsync(entity.EmployeeId);
            if (!isAdmin && isHr && (ownerIsHr || ownerIsMgr))
                throw new ErrorException(StatusCodes.Status403Forbidden, "FORBIDDEN",
                    "HR can only approve employees and admin-with-profile, not other HR/Manager.");
            if (!isAdmin && !isHr && isMgr && (ownerIsHr || ownerIsAdmin))
                throw new ErrorException(StatusCodes.Status403Forbidden, "FORBIDDEN",
                    "Manager can only approve employees.");

            // Nếu là Approve + leave type có MaxDays: kiểm tra số dư trước khi sinh Deduct
            if (ledgerOnPositive && entity.LeaveType?.MaxDays.HasValue == true)
            {
                int year = entity.FromDate.Year;
                decimal alreadyUsed = await UsedInYearAsync(entity.EmployeeId, year, entity.LeaveTypeId);
                decimal projected = alreadyUsed + (entity.TotalDays ?? 0m);
                if (projected > entity.LeaveType.MaxDays.Value)
                    throw new ErrorException(StatusCodes.Status409Conflict, "INSUFFICIENT_BALANCE",
                        $"Insufficient leave balance. Used: {alreadyUsed}, requested: {entity.TotalDays}, limit: {entity.LeaveType.MaxDays}.");
            }

            entity.Status = newStatus;
            entity.ApprovedBy = approverEmployeeId;
            entity.ApprovedAt = UtcNow.UtcDateTime;
            entity.LastUpdatedBy = CurrentUserName;
            entity.LastUpdatedTime = UtcNow;

            // Sinh giao dịch Deduct (idempotent theo LeaveRequestId + Year)
            if (ledgerOnPositive && entity.LeaveType?.MaxDays.HasValue == true)
            {
                await EnsureDeductEntryAsync(entity, note);
            }

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        // ---- Ledger helpers ---------------------------------------------------

        private async Task<decimal> UsedInYearAsync(Guid employeeId, int year, Guid? leaveTypeId)
        {
            IGenericRepository<LeaveLedger> repo = UnitOfWork.GetRepository<LeaveLedger>();
            IQueryable<LeaveLedger> q = repo.Entities
                .Where(x => x.EmployeeId == employeeId && x.Year == year && !x.DeletedTime.HasValue);
            if (leaveTypeId.HasValue)
                q = q.Where(x => x.LeaveTypeId == leaveTypeId);

            var entries = await q.Where(x => x.EntryType == LeaveLedgerEntryType.Deduct || x.EntryType == LeaveLedgerEntryType.Refund)
                .Select(x => new { x.EntryType, x.Days }).ToListAsync();
            decimal deduct = entries.Where(x => x.EntryType == LeaveLedgerEntryType.Deduct).Sum(x => x.Days);
            decimal refund = entries.Where(x => x.EntryType == LeaveLedgerEntryType.Refund).Sum(x => x.Days);
            return deduct - refund;
        }

        /// <summary>
        /// Idempotent: chỉ tạo khi chưa có Deduct cho (requestId, year).
        /// </summary>
        private async Task EnsureDeductEntryAsync(LeaveRequest entity, string? note)
        {
            IGenericRepository<LeaveLedger> repo = UnitOfWork.GetRepository<LeaveLedger>();
            int year = entity.FromDate.Year;
            bool exists = await repo.Entities.AnyAsync(x =>
                x.LeaveRequestId == entity.Id &&
                x.EntryType == LeaveLedgerEntryType.Deduct &&
                x.Year == year);
            if (exists) return;

            LeaveLedger entry = new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = entity.EmployeeId,
                Year = year,
                LeaveTypeId = entity.LeaveTypeId,
                EntryType = LeaveLedgerEntryType.Deduct,
                Days = entity.TotalDays ?? 0m,
                LeaveRequestId = entity.Id,
                Reason = string.IsNullOrEmpty(note)
                    ? $"Duyệt nghỉ phép ({entity.LeaveType?.Name ?? "N/A"})"
                    : $"Duyệt nghỉ phép ({entity.LeaveType?.Name ?? "N/A"}) · {note}",
                CreatedBy = CurrentUserName,
                CreatedTime = UtcNow,
                LastUpdatedBy = CurrentUserName,
                LastUpdatedTime = UtcNow,
            };
            await repo.InsertAsync(entry);
        }

        private async Task InsertLedgerRefundAsync(LeaveRequest entity, string reason)
        {
            var repo = Repository<LeaveLedger>();
            var history = await repo.Entities.Where(e => e.LeaveRequestId == entity.Id && !e.DeletedTime.HasValue).ToListAsync();
            foreach (var group in history.Where(e => e.EntryType == LeaveLedgerEntryType.Deduct).GroupBy(e => e.Year))
            {
                var days = group.Sum(e => e.Days);
                var refunds = history.Where(e => e.Year == group.Key && e.EntryType == LeaveLedgerEntryType.Refund).ToList();
                if (refunds.Count > 0)
                {
                    if (refunds.Sum(e => e.Days) != days) throw new ErrorException(409, "LEAVE_LEDGER_INTEGRITY", "Existing refund does not match its deduction; reconcile it before cancellation.");
                    continue;
                }
                if (days <= 0 || group.Any(e => e.EmployeeId != entity.EmployeeId || e.LeaveTypeId != entity.LeaveTypeId))
                    throw new ErrorException(409, "LEAVE_LEDGER_INTEGRITY", "Leave deduction history is inconsistent.");
                await repo.InsertAsync(new LeaveLedger
                {
                    EmployeeId = entity.EmployeeId, Year = group.Key, LeaveTypeId = entity.LeaveTypeId,
                    EntryType = LeaveLedgerEntryType.Refund, Days = days, LeaveRequestId = entity.Id, Reason = reason,
                    CreatedBy = CurrentUserName, CreatedTime = UtcNow, LastUpdatedBy = CurrentUserName, LastUpdatedTime = UtcNow
                });
            }
        }

        // =====================================================================
        // 4. ADMIN / QUYỀN TRỢ
        // =====================================================================

        public async Task SoftDeleteAsync(Guid id)
        {
            RequireStaff();
            IGenericRepository<LeaveRequest> repo = UnitOfWork.GetRepository<LeaveRequest>();
            LeaveRequest entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Leave request not found");

            if (entity.Status != LeaveRequestStatus.Pending)
                throw new ErrorException(409, "CLOSED_REQUEST", "Decided leave requests must be retained for audit.");
            entity.DeletedBy = CurrentUserName;
            entity.DeletedTime = UtcNow;
            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            RequireStaff();
            IGenericRepository<LeaveRequest> repo = UnitOfWork.GetRepository<LeaveRequest>();
            LeaveRequest entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Leave request not found");

            if (entity.Status != LeaveRequestStatus.Pending)
                throw new ErrorException(409, "CLOSED_REQUEST", "Decided leave requests must be retained for audit.");
            // Never delete leave-balance history through a request endpoint.
            IGenericRepository<LeaveLedger> ledgerRepo = UnitOfWork.GetRepository<LeaveLedger>();
            List<LeaveLedger> ledgers = await ledgerRepo.Entities
                .Where(x => x.LeaveRequestId == entity.Id)
                .ToListAsync();
            if (ledgers.Count != 0)
                throw new ErrorException(409, "LEAVE_LEDGER_EXISTS", "A request with balance history cannot be deleted.");

            await repo.DeleteAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        // =====================================================================
        // 5. HELPERS
        // =====================================================================

        

        /// <summary>
        /// Trả về (isAdmin, isHr, isMgr, userId) của employee này.
        /// Dùng để enforce rule "chặn tự duyệt" + "HR không duyệt HR".
        /// </summary>
        private static void ValidateTotalDays(decimal days, DateTime from, DateTime to)
        {
            if (days <= 0 || days > (decimal)(to.Date - from.Date).TotalDays + 1m)
                throw new ErrorException(400, "INVALID_TOTAL_DAYS", "Leave days must be positive and cannot exceed the selected calendar range.");
        }

        private async Task<(bool IsAdmin, bool IsHr, bool IsMgr, Guid UserId)> ResolveRolesAsync(Guid employeeId)
        {
            if (employeeId == Guid.Empty)
                return (false, false, false, Guid.Empty);

            IGenericRepository<Employee> empRepo = UnitOfWork.GetRepository<Employee>();
            Guid? userId = await empRepo.Entities.AsNoTracking()
                .Where(x => x.Id == employeeId)
                .Select(x => (Guid?)x.UserId)
                .FirstOrDefaultAsync();

            if (userId is null)
                return (false, false, false, Guid.Empty);

            IGenericRepository<ApplicationUserRole> uarRepo = UnitOfWork.GetRepository<ApplicationUserRole>();
            IGenericRepository<ApplicationRole> roleRepo = UnitOfWork.GetRepository<ApplicationRole>();
            List<string> roleNames = await uarRepo.Entities.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Join(roleRepo.Entities.AsNoTracking(),
                      uar => uar.RoleId, role => role.Id,
                      (uar, role) => new { role.Name })
                .Where(x => x.Name != null).Select(x => x.Name!)
                .ToListAsync();

            return (
                roleNames.Contains("Admin"),
                roleNames.Contains("HR"),
                roleNames.Contains("Manager"),
                userId.Value);
        }
    }
}
