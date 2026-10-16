using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.AttendanceCorrectionModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Timekeeping
{
    public class AttendanceCorrectionService : BaseService, IAttendanceCorrectionService
    {

        public AttendanceCorrectionService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<AttendanceCorrectionResponseModelView>> GetAllAsync(int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);
            RequireStaff();

            IGenericRepository<AttendanceCorrection> repo = UnitOfWork.GetRepository<AttendanceCorrection>();

            IQueryable<AttendanceCorrection> query = repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.Reviewer)
                .Where(x => !x.DeletedTime.HasValue)
                .OrderByDescending(x => x.WorkDate);

            int totalItems = await query.CountAsync();
            List<AttendanceCorrection> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<AttendanceCorrectionResponseModelView>(
                items.Select(x => x.ToViewModel()).ToList().AsReadOnly(),
                totalItems, pageNumber, pageSize);
        }

        public async Task<AttendanceCorrectionResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<AttendanceCorrection> repo = UnitOfWork.GetRepository<AttendanceCorrection>();
            AttendanceCorrection entity = await repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.Reviewer)
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Attendance correction not found");
            RequireEmployeeAccess(entity.EmployeeId);

            return entity.ToViewModel();
        }

        public async Task<List<AttendanceCorrectionResponseModelView>> ByEmployeeIdAsync(Guid employeeId)
        {
            RequireEmployeeAccess(employeeId);

            IGenericRepository<AttendanceCorrection> repo = UnitOfWork.GetRepository<AttendanceCorrection>();
            List<AttendanceCorrection> entities = await repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.Reviewer)
                .Where(x => x.EmployeeId == employeeId && !x.DeletedTime.HasValue)
                .OrderByDescending(x => x.WorkDate)
                .ToListAsync();

            return entities.Select(x => x.ToViewModel()).ToList();
        }

        public async Task CreateAsync(CreateAttendanceCorrectionModelView model)
        {
            RequireEmployeeAccess(model.EmployeeId);
            if (model.AttendanceId is Guid attendanceId && !await Repository<Attendance>().Entities
                .AnyAsync(a => a.Id == attendanceId && a.EmployeeId == model.EmployeeId
                    && a.AttendanceDate.Date == model.WorkDate.Date && !a.DeletedTime.HasValue))
                throw new ErrorException(400, "INVALID_ATTENDANCE_REFERENCE", "Attendance must belong to the same employee and business day.");


            IGenericRepository<AttendanceCorrection> repo = UnitOfWork.GetRepository<AttendanceCorrection>();

            // Nhân viên tồn tại
            IGenericRepository<Employee> employeeRepo = UnitOfWork.GetRepository<Employee>();
            bool employeeExists = await employeeRepo.Entities
                .AnyAsync(x => x.Id == model.EmployeeId && !x.DeletedTime.HasValue);
            if (!employeeExists)
                throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Employee not found");

            // Có ít nhất một mốc đề nghị
            bool hasProposal = model.ProposedCheckIn is not null ||
                               model.ProposedCheckOut is not null ||
                               model.ProposedStatus is not null;
            if (!hasProposal)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_INPUT",
                    "A correction must propose at least one value (check-in, check-out, or status)");

            // Nếu ra ca đề nghị mà không có vào ca, phải lớn hơn vào ca (đủ hợp lệ theo kiểu có)
            if (model.ProposedCheckIn is DateTimeOffset ci &&
                model.ProposedCheckOut is DateTimeOffset co && co <= ci)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_INPUT",
                    "Proposed check-out must be after proposed check-in");

            string currentUser = CurrentUserName;

            AttendanceCorrection entity = model.ToEntity();

            // Trước khi tạo: ghi snapshot hiện trạng (attendance gốc nếu có)
            if (model.AttendanceId is Guid attId && attId != Guid.Empty)
            {
                IGenericRepository<Attendance> attRepo = UnitOfWork.GetRepository<Attendance>();
                Attendance? att = await attRepo.Entities
                    .FirstOrDefaultAsync(x => x.Id == attId && !x.DeletedTime.HasValue);
                if (att is not null)
                {
                    if (att.EmployeeId != entity.EmployeeId || att.AttendanceDate.Date != entity.WorkDate.Date)
                        throw new ErrorException(400, "INVALID_ATTENDANCE_REFERENCE", "Attendance owner or date does not match the request.");

                    entity.BeforeJson = SerializeAttendance(att);
                }
            }

            entity.CreatedBy = currentUser;
            entity.LastUpdatedBy = currentUser;
            entity.CreatedTime = UtcNow;
            entity.LastUpdatedTime = UtcNow;

            await repo.InsertAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        /// <summary>
        /// Duyệt / từ chối. Khi DUYỆT: apply giá trị đề nghị lên bản ghi Attendance
        /// (nếu có) và ghi snapshot AfterJson. Tuân thủ "không tự duyệt chính mình".
        /// </summary>
        public async Task ReviewAsync(ReviewAttendanceCorrectionModelView model)
        {
            RequireStaff();
            model.ReviewedBy = RequireCurrentEmployeeId();

            IGenericRepository<AttendanceCorrection> repo = UnitOfWork.GetRepository<AttendanceCorrection>();

            AttendanceCorrection entity = await repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.Attendance)
                .FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND",
                    "Attendance correction not found");

            if (entity.Status != AttendanceCorrectionStatus.Pending)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_STATE",
                    "Correction is already processed");

            IGenericRepository<Employee> employeeRepo = UnitOfWork.GetRepository<Employee>();
            bool reviewerExists = await employeeRepo.Entities
                .AnyAsync(x => x.Id == model.ReviewedBy && !x.DeletedTime.HasValue);
            if (!reviewerExists)
                throw new ErrorException(StatusCodes.Status400BadRequest, "INVALID_INPUT",
                    "Reviewer (Employee) not found");

            // Không tự duyệt (trừ Admin/HR)
            if (entity.EmployeeId == model.ReviewedBy && !IsElevated())
                throw new ErrorException(StatusCodes.Status403Forbidden, "FORBIDDEN",
                    "You cannot approve your own attendance correction");

            string currentUser = CurrentUserName;

            if (model.Status == AttendanceCorrectionStatus.Approved && entity.Attendance is Attendance target)
            {
                if (target.EmployeeId != entity.EmployeeId || target.AttendanceDate.Date != entity.WorkDate.Date)
                    throw new ErrorException(400, "INVALID_ATTENDANCE_REFERENCE", "Attendance owner or date does not match the request.");
                if (target.ApprovalStatus == AttendanceApprovalStatus.Approved)
                    throw new ErrorException(409, "ATTENDANCE_APPROVED", "Reopen approved attendance before applying a correction.");
            }
            entity.Status = model.Status;
            entity.ReviewedBy = model.ReviewedBy;
            entity.ReviewedAt = UtcNow.UtcDateTime;
            entity.ReviewNote = model.ReviewNote;

            if (model.Status == AttendanceCorrectionStatus.Approved)
            {
                // Apply vào bản ghi chấm công (nếu có)
                if (entity.Attendance is Attendance att)
                {
                    if (entity.ProposedCheckIn is not null) att.CheckInTime = entity.ProposedCheckIn;
                    if (entity.ProposedCheckOut is not null) att.CheckOutTime = entity.ProposedCheckOut;
                    if (entity.ProposedStatus is not null) att.Status = entity.ProposedStatus;

                    att.ChangeSummary = $"Đã chỉnh công theo yêu cầu sửa {entity.Id:N} ({entity.Reason})";
                    att.LastUpdatedBy = currentUser;
                    att.LastUpdatedTime = UtcNow;

                    IGenericRepository<Attendance> attRepo = UnitOfWork.GetRepository<Attendance>();
                    await attRepo.UpdateAsync(att);

                    entity.AfterJson = SerializeAttendance(att);
                }
            }

            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        // ===== Helpers =====
        private static string SerializeAttendance(Attendance att)
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                checkInTime = att.CheckInTime, checkOutTime = att.CheckOutTime,
                status = att.Status?.ToString(), plannedShiftId = att.PlannedShiftId, note = att.Note
            });
        }

        private bool IsElevated()
        {
            return CurrentUser.IsInRole("Admin") || CurrentUser.IsInRole("HR");
        }
    }
}
