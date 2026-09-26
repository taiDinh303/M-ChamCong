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

        public async Task<List<AttendanceResponseModelView>> ByEmployeeIdAsync(
            Guid employeeId)
        {
            IGenericRepository<Attendance> repo =
                _unitOfWork.GetRepository<Attendance>();

            List<Attendance> attendances = await repo.Entities
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .Include(x => x.PlannedShift)
                .Include(x => x.Approver)
                .Include(x => x.AttendanceLogs)
                .OrderByDescending(x => x.AttendanceDate)
                .ToListAsync();

            return attendances.Select(x => x.ToViewModel()).ToList();
        }

        public async Task<CheckInAttendanceResponseModelView> CheckInAsync(
            CheckInAttendanceModelView model)
        {
            IGenericRepository<Attendance> repo =
                _unitOfWork.GetRepository<Attendance>();

            IGenericRepository<AttendanceLog> logRepo =
                _unitOfWork.GetRepository<AttendanceLog>();

            IGenericRepository<Employee> employeeRepo =
                _unitOfWork.GetRepository<Employee>();

            // Kiểm tra nhân viên
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

            // Giờ log hiện tại (theo múi giờ máy chủ / VN)
            DateTime now = DateTime.Now;
            DateTimeOffset logTime =
                DateTimeOffset.Now;

            // Tìm bản ghi Attendance của HÔM NAY (tự tạo nếu chưa có)
            Attendance? attendance = await repo.Entities
                .Where(x =>
                    x.EmployeeId == model.EmployeeId &&
                    x.AttendanceDate.Date == now.Date &&
                    !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .Include(x => x.PlannedShift)
                .FirstOrDefaultAsync();

            bool createdToday = false;

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    EmployeeId = model.EmployeeId,
                    AttendanceDate = now.Date,
                    ApprovalStatus =
                        AttendanceApprovalStatus.Pending
                };

                attendance.CreatedBy =
                    _httpContextAccessor.HttpContext?.User?.Identity?.Name
                    ?? "System";
                attendance.CreatedTime = CoreHelper.SystemTimeNow;

                await repo.InsertAsync(attendance);
                createdToday = true;
            }

            // Kiểm tra log trùng (chấm lại cùng loại trong 2 phút -> giữ nguyên)
            bool alreadyRecorded = await logRepo.Entities
                .AnyAsync(x =>
                    x.AttendanceId == attendance.Id &&
                    x.Type == model.Type &&
                    x.LogTime >= logTime.AddMinutes(-2) &&
                    !x.DeletedTime.HasValue);

            if (alreadyRecorded)
            {
                return new CheckInAttendanceResponseModelView
                {
                    AlreadyRecorded = true,
                    Message = "Đã có lần chấm cùng loại gần đây. "
                        + "Không cần bấm lại.",
                    Attendance = attendance.ToViewModel()
                };
            }

            // Thêm log chấm công
            AttendanceLog log = new AttendanceLog
            {
                AttendanceId = attendance.Id,
                LogTime = logTime,
                Type = model.Type,
                Method = model.Method,
                PhotoUrl = model.PhotoUrl,
                Note = model.Note
            };

            log.CreatedBy =
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";
            log.CreatedTime = CoreHelper.SystemTimeNow;

            await logRepo.InsertAsync(log);

            // Đồng bộ ảnh lên Attendance (log vào -> CheckInPhoto, ra -> CheckOutPhoto)
            if (!string.IsNullOrWhiteSpace(model.PhotoUrl))
            {
                if (model.Type == AttendanceLogType.CheckIn)
                    attendance.CheckInPhoto = model.PhotoUrl;
                else
                    attendance.CheckOutPhoto = model.PhotoUrl;
            }

            // Tự đánh trạng thái: có cặp vào/ra -> tính ActualHours;
            // chưa về -> Present
            List<AttendanceLog> logs = await logRepo.Entities
                .Where(x =>
                    x.AttendanceId == attendance.Id &&
                    !x.DeletedTime.HasValue)
                .ToListAsync();

            DateTime? checkIn = logs
                .Where(x => x.Type == AttendanceLogType.CheckIn)
                .Select(x => (DateTime?)x.LogTime.UtcDateTime)
                .Min();

            DateTime? checkOut = logs
                .Where(x => x.Type == AttendanceLogType.CheckOut)
                .Select(x => (DateTime?)x.LogTime.UtcDateTime)
                .Max();

            if (checkIn.HasValue && checkOut.HasValue)
            {
                attendance.ActualHours =
                    (int)Math.Round(
                        (checkOut.Value - checkIn.Value).TotalHours);
            }

            // Tự đánh trạng thái Đúng giờ / Trễ giờ theo giờ VN (UTC+7):
            // so giờ vào ca với giờ bắt đầu ca (ca gán nếu có, nếu không dùng ca hành chính).
            if (checkIn.HasValue)
            {
                DateTime checkInVn = checkIn.Value.AddHours(7);
                TimeOnly checkInVnTime =
                    new TimeOnly(checkInVn.Hour, checkInVn.Minute, checkInVn.Second);

                TimeOnly shiftStart = attendance.PlannedShift != null
                    ? attendance.PlannedShift.StartTime
                    : AttendanceStatusEvaluator.AdminShift().Start;

                if (attendance.Status == null)
                {
                    attendance.Status =
                        checkInVnTime > shiftStart
                            ? AttendanceStatus.Late
                            : AttendanceStatus.Present;
                }
            }

            if (!createdToday)
            {
                attendance.LastUpdatedBy =
                    _httpContextAccessor.HttpContext?.User?.Identity?.Name
                    ?? "System";
                attendance.LastUpdatedTime = CoreHelper.SystemTimeNow;
                await repo.UpdateAsync(attendance);
            }

            await _unitOfWork.SaveAsync();

            // Trả về bản ghi mới nhất (nạp lại các điều hướng)
            Attendance refreshed = await repo.Entities
                .Where(x => x.Id == attendance.Id)
                .Include(x => x.Employee)
                .Include(x => x.PlannedShift)
                .Include(x => x.Approver)
                .Include(x => x.AttendanceLogs)
                .FirstAsync();

            return new CheckInAttendanceResponseModelView
            {
                AlreadyRecorded = false,
                Message = model.Type == AttendanceLogType.CheckIn
                    ? "Đã ghi nhận VÀO CA."
                    : "Đã ghi nhận RA CA.",
                Attendance = refreshed.ToViewModel()
            };
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
