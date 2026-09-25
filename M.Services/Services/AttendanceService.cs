using M.Contract.Repositories.Entities;
using M.Contract.Repositories.IUOW;
using M.Contract.Serivces.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelViews.AttendanceModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Service
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AttendanceService(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<BasePaginatedList<AttendanceResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            IGenericRepository<Attendance> repo =
                _unitOfWork.GetRepository<Attendance>();

            IQueryable<Attendance> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .Include(x => x.PlannedShift)
                .Include(x => x.Approver)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<Attendance> attendances = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<AttendanceResponseModelView> result = attendances
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<AttendanceResponseModelView>(
                result.AsReadOnly(),
                totalItems,
                pageNumber,
                pageSize);
        }

        public async Task<AttendanceResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<Attendance> repo =
                _unitOfWork.GetRepository<Attendance>();

            Attendance attendance = await repo.Entities
                .Where(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .Include(x => x.PlannedShift)
                .Include(x => x.Approver)
                .Include(x => x.AttendanceLogs)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance not found");

            return attendance.ToViewModel();
        }

        public async Task CreateAsync(CreateAttendanceModelView model)
        {
            IGenericRepository<Attendance> repo =
                _unitOfWork.GetRepository<Attendance>();

            // Kiểm tra Employee
            IGenericRepository<Employee> employeeRepo =
                _unitOfWork.GetRepository<Employee>();

            bool employeeExists = await employeeRepo.Entities
                .AnyAsync(x =>
                    x.Id == model.EmployeeId &&
                    !x.DeletedTime.HasValue);

            if (!employeeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee not found");
            }

            // Kiểm tra Attendance đã tồn tại trong ngày
            bool attendanceExists = await repo.Entities
                .AnyAsync(x =>
                    x.EmployeeId == model.EmployeeId &&
                    x.AttendanceDate.Date == model.AttendanceDate.Date &&
                    !x.DeletedTime.HasValue);

            if (attendanceExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Attendance already exists for this employee on this date");
            }

            // Kiểm tra PlannedShift (nếu có)
            if (model.PlannedShiftId.HasValue)
            {
                IGenericRepository<Shift> shiftRepo =
                    _unitOfWork.GetRepository<Shift>();

                bool shiftExists = await shiftRepo.Entities
                    .AnyAsync(x =>
                        x.Id == model.PlannedShiftId.Value &&
                        !x.DeletedTime.HasValue);

                if (!shiftExists)
                {
                    throw new ErrorException(
                        StatusCodes.Status404NotFound,
                        "NOT_FOUND",
                        "Planned shift not found");
                }
            }

            // Kiểm tra Status
            if (model.Status.HasValue &&
                !Enum.IsDefined(typeof(AttendanceStatus), model.Status.Value))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Invalid attendance status");
            }

            string currentUser =
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";

            Attendance attendance = model.ToEntity();

            attendance.CreatedBy = currentUser;
            attendance.CreatedTime = CoreHelper.SystemTimeNow;

            await repo.InsertAsync(attendance);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateAttendanceModelView model)
        {
            IGenericRepository<Attendance> repo =
                _unitOfWork.GetRepository<Attendance>();

            Attendance attendance = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance not found");

            // Kiểm tra Employee
            IGenericRepository<Employee> employeeRepo =
                _unitOfWork.GetRepository<Employee>();

            bool employeeExists = await employeeRepo.Entities
                .AnyAsync(x =>
                    x.Id == model.EmployeeId &&
                    !x.DeletedTime.HasValue);

            if (!employeeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee not found");
            }

            // Kiểm tra Attendance trùng
            bool attendanceExists = await repo.Entities
                .AnyAsync(x =>
                    x.Id != model.Id &&
                    x.EmployeeId == model.EmployeeId &&
                    x.AttendanceDate.Date == model.AttendanceDate.Date &&
                    !x.DeletedTime.HasValue);

            if (attendanceExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Attendance already exists for this employee on this date");
            }

            // Kiểm tra Status
            if (model.Status.HasValue &&
                !Enum.IsDefined(typeof(AttendanceStatus), model.Status.Value))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Invalid attendance status");
            }

            string currentUser =
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";

            model.ToEntity(attendance);

            attendance.LastUpdatedBy = currentUser;
            attendance.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await repo.UpdateAsync(attendance);
            await _unitOfWork.SaveAsync();
        }

        public async Task ApproveAsync(ApproveAttendanceModelView model)
        {
            IGenericRepository<Attendance> repo =
                _unitOfWork.GetRepository<Attendance>();

            Attendance attendance = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance not found");

            // Kiểm tra người phê duyệt tồn tại
            IGenericRepository<Employee> employeeRepo =
                _unitOfWork.GetRepository<Employee>();

            bool approverExists = await employeeRepo.Entities
                .AnyAsync(x =>
                    x.Id == model.ApprovedBy &&
                    !x.DeletedTime.HasValue);

            if (!approverExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Approver not found");
            }

            attendance.ApprovalStatus = model.ApprovalStatus;
            attendance.ApprovedBy = model.ApprovedBy;
            attendance.ApprovedAt =
                model.ApprovalStatus == AttendanceApprovalStatus.Pending
                    ? null
                    : DateTime.Now;

            if (model.Note != null)
            {
                attendance.Note = model.Note;
            }

            string currentUser =
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";

            attendance.LastUpdatedBy = currentUser;
            attendance.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await repo.UpdateAsync(attendance);
            await _unitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<Attendance> repo =
                _unitOfWork.GetRepository<Attendance>();

            Attendance attendance = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance not found");

            string currentUser =
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";

            attendance.DeletedBy = currentUser;
            attendance.DeletedTime = CoreHelper.SystemTimeNow;

            await repo.UpdateAsync(attendance);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<Attendance> repo =
                _unitOfWork.GetRepository<Attendance>();

            Attendance attendance = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance not found");

            await repo.DeleteAsync(attendance);
            await _unitOfWork.SaveAsync();
        }
    }
}
