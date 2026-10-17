using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.OvertimeRequestModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Leave
{
    public class OvertimeRequestService : BaseService, IOvertimeRequestService
    {

        public OvertimeRequestService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<OvertimeRequestResponseModelView>> GetAllAsync(int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);
            RequireStaff();

            IGenericRepository<OvertimeRequest> repo = UnitOfWork.GetRepository<OvertimeRequest>();

            IQueryable<OvertimeRequest> query = repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.Approver)
                .Where(x => !x.DeletedTime.HasValue)
                .OrderByDescending(x => x.WorkDate);

            int totalItems = await query.CountAsync();
            List<OvertimeRequest> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<OvertimeRequestResponseModelView>(
                items.Select(x => x.ToViewModel()).ToList().AsReadOnly(),
                totalItems, pageNumber, pageSize);
        }

        public async Task<OvertimeRequestResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<OvertimeRequest> repo = UnitOfWork.GetRepository<OvertimeRequest>();
            OvertimeRequest entity = await repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.Approver)
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Overtime request not found");
            RequireEmployeeAccess(entity.EmployeeId);

            return entity.ToViewModel();
        }

        public async Task<List<OvertimeRequestResponseModelView>> ByEmployeeIdAsync(Guid employeeId)
        {
            RequireEmployeeAccess(employeeId);

            IGenericRepository<OvertimeRequest> repo = UnitOfWork.GetRepository<OvertimeRequest>();
            List<OvertimeRequest> entities = await repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.Approver)
                .Where(x => x.EmployeeId == employeeId && !x.DeletedTime.HasValue)
                .OrderByDescending(x => x.WorkDate)
                .ToListAsync();

            return entities.Select(x => x.ToViewModel()).ToList();
        }

        public async Task CreateAsync(CreateOvertimeRequestModelView model)
        {
            RequireEmployeeAccess(model.EmployeeId);
            ValidateWindow(model.WorkDate, model.StartAt, model.EndAt);
            if (model.AttendanceId is Guid attendanceId && !await Repository<Attendance>().Entities
                .AnyAsync(a => a.Id == attendanceId && a.EmployeeId == model.EmployeeId
                    && a.AttendanceDate.Date == model.WorkDate.Date && !a.DeletedTime.HasValue))
                throw new ErrorException(400, "INVALID_ATTENDANCE_REFERENCE", "Attendance must belong to the same employee and business day.");


            IGenericRepository<OvertimeRequest> repo = UnitOfWork.GetRepository<OvertimeRequest>();

            // 1) Nhân viên tồn tại
            IGenericRepository<Employee> employeeRepo = UnitOfWork.GetRepository<Employee>();
            bool employeeExists = await employeeRepo.Entities
                .AnyAsync(x => x.Id == model.EmployeeId && !x.DeletedTime.HasValue);
            if (!employeeExists)
                throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Employee not found");

            // 2) Bản ghi chấm công (nếu có) phải hợp lệ
            if (model.AttendanceId is Guid attId && attId != Guid.Empty)
            {
                IGenericRepository<Attendance> attRepo = UnitOfWork.GetRepository<Attendance>();
                bool attExists = await attRepo.Entities
                    .AnyAsync(x => x.Id == attId && !x.DeletedTime.HasValue);
                if (!attExists)
                    throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_INPUT",
                        "Attendance record not found");
            }

            // 3) Ngăn trùng: chỉ cho một yêu cầu Tăng ca đang Chờ duyệt trên cùng ngày + nhân viên
            bool duplicate = await repo.Entities.AnyAsync(x =>
                x.EmployeeId == model.EmployeeId &&
                x.WorkDate.Date == model.WorkDate.Date &&
                x.Status == OvertimeStatus.Pending &&
                !x.DeletedTime.HasValue);
            if (duplicate)
                throw new ErrorException(StatusCodes.Status400BadRequest, "DUPLICATE",
                    "Employee already has a pending overtime request for this date");

            string currentUser = CurrentUserName;

            OvertimeRequest entity = model.ToEntity();
            entity.WorkDate = DateTime.SpecifyKind(model.WorkDate.Date, DateTimeKind.Utc);
            entity.OvertimeHours = CalcHoursFromRange(entity.StartAt, entity.EndAt);
            entity.CreatedBy = currentUser;
            entity.LastUpdatedBy = currentUser;
            entity.CreatedTime = UtcNow;
            entity.LastUpdatedTime = UtcNow;

            await repo.InsertAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateOvertimeRequestModelView model)
        {
            ValidateWindow(model.WorkDate, model.StartAt, model.EndAt);
            IGenericRepository<OvertimeRequest> repo = UnitOfWork.GetRepository<OvertimeRequest>();

            OvertimeRequest entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Overtime request not found");
            RequireEmployeeAccess(entity.EmployeeId);

            if (model.EmployeeId != entity.EmployeeId)
                throw new ErrorException(400, "INVALID_EMPLOYEE", "An overtime request cannot change its owner.");
            if (model.AttendanceId.HasValue && !await Repository<Attendance>().Entities.AnyAsync(a => a.Id == model.AttendanceId
                    && a.EmployeeId == entity.EmployeeId && a.AttendanceDate.Date == model.WorkDate.Date && !a.DeletedTime.HasValue))
                throw new ErrorException(400, "INVALID_ATTENDANCE_REFERENCE", "Attendance must belong to the same employee and day.");

            if (entity.Status != OvertimeStatus.Pending)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_STATE",
                    "Only pending overtime requests can be updated");

            string currentUser = CurrentUserName;

            model.ToEntity(entity);
            entity.WorkDate = DateTime.SpecifyKind(model.WorkDate.Date, DateTimeKind.Utc);
            entity.OvertimeHours = CalcHoursFromRange(entity.StartAt, entity.EndAt);
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        /// <summary>
        /// Duyệt / từ chối. Tuân thủ "không ai tự duyệt yêu cầu của chính mình"
        /// (worklog V1): nếu reviewer không phải Admin/HR, phải là HR; và
        /// không được duyệt đơn của chính reviewer.
        /// </summary>
        public async Task ReviewAsync(ReviewOvertimeRequestModelView model)
        {
            if (model.Status is not (OvertimeStatus.Approved or OvertimeStatus.Rejected))
                throw new ErrorException(400, "INVALID_STATE", "Review must approve or reject the request.");
            RequireStaff();
            model.ApprovedBy = RequireCurrentEmployeeId();

            IGenericRepository<OvertimeRequest> repo = UnitOfWork.GetRepository<OvertimeRequest>();

            OvertimeRequest entity = await repo.Entities
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Overtime request not found");

            if (entity.Status != OvertimeStatus.Pending)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_STATE",
                    "Overtime request is already processed");
            if (model.Status == OvertimeStatus.Approved) ValidateWindow(entity.WorkDate, entity.StartAt, entity.EndAt);

            // Người duyệt phải tồn tại (Employee)
            IGenericRepository<Employee> employeeRepo = UnitOfWork.GetRepository<Employee>();
            bool reviewerExists = await employeeRepo.Entities
                .AnyAsync(x => x.Id == model.ApprovedBy && !x.DeletedTime.HasValue);
            if (!reviewerExists)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_INPUT",
                    "Reviewer (Employee) not found");

            // Quy tắc: không tự duyệt yêu cầu của chính mình, trừ Admin/HR toàn quyền.
            if (entity.EmployeeId == model.ApprovedBy && !IsElevated())
                throw new ErrorException(StatusCodes.Status403Forbidden, "FORBIDDEN",
                    "You cannot approve your own overtime request");

            string currentUser = CurrentUserName;

            entity.Status = model.Status;
            entity.ApprovedBy = model.ApprovedBy;
            entity.ApprovedAt = UtcNow.UtcDateTime;
            entity.Note = model.Note;
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            RequireStaff();
            IGenericRepository<OvertimeRequest> repo = UnitOfWork.GetRepository<OvertimeRequest>();
            OvertimeRequest entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Overtime request not found");

            string currentUser = CurrentUserName;
            if (entity.Status != OvertimeStatus.Pending)
                throw new ErrorException(409, "CLOSED_REQUEST", "Decided overtime requests must be retained for audit.");
            entity.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        // ===== Helpers =====
        private static void ValidateWindow(DateTime workDate, DateTimeOffset? start, DateTimeOffset? end)
        {
            if (!start.HasValue || !end.HasValue || end <= start || end - start > TimeSpan.FromDays(1)
                || start.Value.ToOffset(TimeSpan.FromHours(7)).Date != workDate.Date)
                throw new ErrorException(400, "INVALID_OVERTIME_WINDOW", "Overtime needs a valid start/end window within 24 hours, starting on the selected Vietnam business day.");
        }

        private static decimal? CalcHoursFromRange(DateTimeOffset? start, DateTimeOffset? end)
        {
            if (start is null || end is null) return null;
            if (end < start) return null;
            return Math.Round((decimal)(end.Value - start.Value).TotalHours, 2);
        }

        // Admin/HR có quyền duyệt cả yêu cầu cá nhân của chính mình (HR/admin).
        private bool IsElevated()
        {
            return CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR");
        }
    }
}
