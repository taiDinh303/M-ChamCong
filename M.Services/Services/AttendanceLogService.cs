using M.Contract.Repositories.Entities;
using M.Contract.Repositories.IUOW;
using M.Contract.Serivces.Interface;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelViews.AttendanceLogModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Service
{
    public class AttendanceLogService : IAttendanceLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AttendanceLogService(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<BasePaginatedList<AttendanceLogResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            IGenericRepository<AttendanceLog> repo =
                _unitOfWork.GetRepository<AttendanceLog>();

            IQueryable<AttendanceLog> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .Include(x => x.Attendance)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<AttendanceLog> attendanceLogs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<AttendanceLogResponseModelView> result = attendanceLogs
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<AttendanceLogResponseModelView>(
                result.AsReadOnly(),
                totalItems,
                pageNumber,
                pageSize);
        }

        public async Task<AttendanceLogResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<AttendanceLog> repo =
                _unitOfWork.GetRepository<AttendanceLog>();

            AttendanceLog attendanceLog = await repo.Entities
                .Where(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                .Include(x => x.Attendance)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance log not found");

            return attendanceLog.ToViewModel();
        }

        public async Task CreateAsync(CreateAttendanceLogModelView model)
        {
            IGenericRepository<AttendanceLog> repo =
                _unitOfWork.GetRepository<AttendanceLog>();

            // Kiểm tra Attendance
            IGenericRepository<Attendance> attendanceRepo =
                _unitOfWork.GetRepository<Attendance>();

            bool attendanceExists = await attendanceRepo.Entities
                .AnyAsync(x =>
                    x.Id == model.AttendanceId &&
                    !x.DeletedTime.HasValue);

            if (!attendanceExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance not found");
            }

            // Kiểm tra Type
            if (!Enum.IsDefined(typeof(AttendanceLogType), model.Type))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Invalid attendance log type");
            }

            // Kiểm tra Method
            if (!Enum.IsDefined(typeof(AttendanceMethod), model.Method))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Invalid attendance method");
            }

            string currentUser =
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";

            AttendanceLog attendanceLog = model.ToEntity();

            attendanceLog.CreatedBy = currentUser;
            attendanceLog.CreatedTime = CoreHelper.SystemTimeNow;

            await repo.InsertAsync(attendanceLog);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateAttendanceLogModelView model)
        {
            IGenericRepository<AttendanceLog> repo =
                _unitOfWork.GetRepository<AttendanceLog>();

            AttendanceLog attendanceLog = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance log not found");

            // Kiểm tra Attendance
            IGenericRepository<Attendance> attendanceRepo =
                _unitOfWork.GetRepository<Attendance>();

            bool attendanceExists = await attendanceRepo.Entities
                .AnyAsync(x =>
                    x.Id == model.AttendanceId &&
                    !x.DeletedTime.HasValue);

            if (!attendanceExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance not found");
            }

            // Kiểm tra Type
            if (!Enum.IsDefined(typeof(AttendanceLogType), model.Type))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Invalid attendance log type");
            }

            // Kiểm tra Method
            if (!Enum.IsDefined(typeof(AttendanceMethod), model.Method))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Invalid attendance method");
            }

            string currentUser =
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";

            model.ToEntity(attendanceLog);

            attendanceLog.LastUpdatedBy = currentUser;
            attendanceLog.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await repo.UpdateAsync(attendanceLog);
            await _unitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<AttendanceLog> repo =
                _unitOfWork.GetRepository<AttendanceLog>();

            AttendanceLog attendanceLog = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance log not found");

            string currentUser =
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";

            attendanceLog.DeletedBy = currentUser;
            attendanceLog.DeletedTime = CoreHelper.SystemTimeNow;

            await repo.UpdateAsync(attendanceLog);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<AttendanceLog> repo =
                _unitOfWork.GetRepository<AttendanceLog>();

            AttendanceLog attendanceLog = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance log not found");

            await repo.DeleteAsync(attendanceLog);
            await _unitOfWork.SaveAsync();
        }
    }
}
