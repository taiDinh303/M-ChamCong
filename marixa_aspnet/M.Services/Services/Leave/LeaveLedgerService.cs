using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.LeaveLedgerModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Leave
{
    public class LeaveLedgerService : BaseService, ILeaveLedgerService
    {

        public LeaveLedgerService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<LeaveLedgerResponseModelView>> GetAllAsync(int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);
            RequireStaff();

            IGenericRepository<LeaveLedger> repo = UnitOfWork.GetRepository<LeaveLedger>();

            IQueryable<LeaveLedger> query = repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Include(x => x.LeaveRequest)
                .Where(x => !x.DeletedTime.HasValue)
                .OrderByDescending(x => x.CreatedTime);

            int totalItems = await query.CountAsync();
            List<LeaveLedger> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<LeaveLedgerResponseModelView>(
                items.Select(x => x.ToViewModel()).ToList().AsReadOnly(),
                totalItems, pageNumber, pageSize);
        }

        public async Task<LeaveLedgerResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<LeaveLedger> repo = UnitOfWork.GetRepository<LeaveLedger>();
            LeaveLedger entity = await repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Include(x => x.LeaveRequest)
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Leave ledger entry not found");
            RequireEmployeeAccess(entity.EmployeeId);

            return entity.ToViewModel();
        }

        public async Task<List<LeaveLedgerResponseModelView>> ByEmployeeYearAsync(Guid employeeId, int year)
        {
            RequireEmployeeAccess(employeeId);
            IGenericRepository<LeaveLedger> repo = UnitOfWork.GetRepository<LeaveLedger>();
            List<LeaveLedger> entities = await repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Include(x => x.LeaveRequest)
                .Where(x => x.EmployeeId == employeeId && x.Year == year && !x.DeletedTime.HasValue)
                .OrderByDescending(x => x.CreatedTime)
                .ToListAsync();

            return entities.Select(x => x.ToViewModel()).ToList();
        }

        /// <summary>
        /// Số dư phép = (Grant + Adjust dương) - (Deduct) + (Refund) trong năm.
        /// </summary>
        public async Task<LeaveLedgerSummaryModelView> GetSummaryAsync(Guid employeeId, int year)
        {
            RequireEmployeeAccess(employeeId);
            IGenericRepository<LeaveLedger> repo = UnitOfWork.GetRepository<LeaveLedger>();

            List<LeaveLedger> entries = await repo.Entities
                .Where(x => x.EmployeeId == employeeId && x.Year == year && !x.DeletedTime.HasValue)
                .Select(x => new LeaveLedger { EntryType = x.EntryType, Days = x.Days })
                .ToListAsync();

            var balance = LeaveBalanceCalculator.Calculate(entries);

            return new LeaveLedgerSummaryModelView
            {
                EmployeeId = employeeId,
                Year = year,
                TotalGranted = balance.TotalGranted,
                TotalUsed = balance.TotalUsed,
                Remaining = balance.Remaining
            };
        }

        /// <summary>
        /// Ghi 1 giao dịch sổ phép. Nếu kèm LeaveRequestId thì idempotent:
        /// không tạo 2 giao dịch cùng loại + cùng đơn (chống trừ 2 lần).
        /// </summary>
        public async Task CreateEntryAsync(CreateLeaveLedgerEntryModelView model)
        {
            RequireStaff();
            IGenericRepository<LeaveLedger> repo = UnitOfWork.GetRepository<LeaveLedger>();

            // Nhân viên tồn tại
            IGenericRepository<Employee> employeeRepo = UnitOfWork.GetRepository<Employee>();
            bool employeeExists = await employeeRepo.Entities
                .AnyAsync(x => x.Id == model.EmployeeId && !x.DeletedTime.HasValue);
            if (!employeeExists)
                throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Employee not found");

            // Type (nếu có) tồn tại
            if (model.LeaveTypeId is Guid typeId && typeId != Guid.Empty)
            {
                IGenericRepository<LeaveType> typeRepo = UnitOfWork.GetRepository<LeaveType>();
                bool typeExists = await typeRepo.Entities
                    .AnyAsync(x => x.Id == typeId && !x.DeletedTime.HasValue);
                if (!typeExists)
                    throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Leave type not found");
            }

            // Trừ/hoàn (Deduct/Refund) liên quan đơn nghỉ phải là đơn Đã duyệt
            if (model.EntryType is LeaveLedgerEntryType.Deduct or LeaveLedgerEntryType.Refund)
            {
                if (model.LeaveRequestId is Guid reqId && reqId != Guid.Empty)
                {
                    // Idempotent: đã có giao dịch cùng loại + cùng đơn thì bỏ qua
                    bool already = await repo.Entities.AnyAsync(x =>
                        x.EntryType == model.EntryType &&
                        x.LeaveRequestId == reqId &&
                        !x.DeletedTime.HasValue);
                    if (already) return;

                    IGenericRepository<LeaveRequest> reqRepo = UnitOfWork.GetRepository<LeaveRequest>();
                    LeaveRequest? request = await reqRepo.Entities
                        .FirstOrDefaultAsync(x => x.Id == reqId && !x.DeletedTime.HasValue);
                    bool valid = request is not null &&
                        (model.EntryType == LeaveLedgerEntryType.Deduct
                            ? request.Status == LeaveRequestStatus.Approved
                            : request.Status is LeaveRequestStatus.Cancelled or LeaveRequestStatus.Rejected);
                    if (!valid)
                        throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_STATE",
                            model.EntryType == LeaveLedgerEntryType.Deduct
                                ? "Deducting leave requires an approved leave request"
                                : "Refunding leave requires a cancelled/rejected request");
                }
            }

            string currentUser = CurrentUserName;

            LeaveLedger entity = model.ToEntity();
            entity.CreatedBy = currentUser;
            entity.LastUpdatedBy = currentUser;
            entity.CreatedTime = UtcNow;
            entity.LastUpdatedTime = UtcNow;

            await repo.InsertAsync(entity);
            await UnitOfWork.SaveAsync();
        }
    }
}
