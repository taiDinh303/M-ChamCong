using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.EmployeeShiftModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Timekeeping
{
    public class EmployeeShiftService : BaseService, IEmployeeShiftService
    {

        public EmployeeShiftService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<EmployeeShiftResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);
            RequireStaff();

            IGenericRepository<EmployeeShift> repo = UnitOfWork.GetRepository<EmployeeShift>();

            IQueryable<EmployeeShift> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .Include(x => x.Shift)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<EmployeeShift> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<EmployeeShiftResponseModelView> result = items
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<EmployeeShiftResponseModelView>(
                result.AsReadOnly(), totalItems, pageNumber, pageSize);
        }

        public async Task<EmployeeShiftResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<EmployeeShift> repo = UnitOfWork.GetRepository<EmployeeShift>();

            EmployeeShift item = await repo.Entities
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .Include(x => x.Shift)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee shift assignment not found");
            RequireEmployeeAccess(item.EmployeeId);

            return item.ToViewModel();
        }

        public async Task<List<EmployeeShiftResponseModelView>> ByEmployeeIdAsync(
            Guid employeeId)
        {
            RequireEmployeeAccess(employeeId);

            IGenericRepository<EmployeeShift> repo =
                UnitOfWork.GetRepository<EmployeeShift>();

            List<EmployeeShift> items = await repo.Entities
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .Include(x => x.Shift)
                .OrderBy(x => x.EffectiveFrom)
                .ToListAsync();

            return items.Select(x => x.ToViewModel()).ToList();
        }

        public async Task CreateAsync(CreateEmployeeShiftModelView model)
        {
            RequireEmployeeAccess(model.EmployeeId);

            IGenericRepository<EmployeeShift> repo = UnitOfWork.GetRepository<EmployeeShift>();

            IGenericRepository<Employee> employeeRepo = UnitOfWork.GetRepository<Employee>();
            bool employeeExists = await employeeRepo.Entities
                .AnyAsync(x => x.Id == model.EmployeeId && !x.DeletedTime.HasValue);

            if (!employeeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee not found");
            }

            IGenericRepository<Shift> shiftRepo = UnitOfWork.GetRepository<Shift>();
            bool shiftExists = await shiftRepo.Entities
                .AnyAsync(x => x.Id == model.ShiftId && !x.DeletedTime.HasValue);

            if (!shiftExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Shift not found");
            }

            string currentUser =
                CurrentUserName;

            EmployeeShift entity = model.ToEntity();
            entity.CreatedBy = currentUser;
            entity.CreatedTime = UtcNow;

            await repo.InsertAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task<CreateEmployeeShiftBulkResultModelView> CreateBulkAsync(
            CreateEmployeeShiftBulkModelView model,
            string currentUser,
            bool canManageAllEmployees,
            Guid? managerEmployeeId)
        {
            RequireStaff();
            currentUser = CurrentUserName;
            canManageAllEmployees = CurrentUser.IsInRole(RoleNames.Admin) || CurrentUser.IsInRole(RoleNames.HR);
            managerEmployeeId = CurrentUser.EmployeeId;
            if (model.EmployeeIds == null || model.EmployeeIds.Count < 1 ||
                model.EmployeeIds.Any(id => id == Guid.Empty) ||
                model.EmployeeIds.Distinct().Count() != model.EmployeeIds.Count)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_EMPLOYEES",
                    "Chọn ít nhất 1 nhân viên, không chọn trùng.");
            }

            DateTime effectiveFrom = DateTime.SpecifyKind(model.EffectiveFrom.Date, DateTimeKind.Unspecified);
            DateTime effectiveTo = DateTime.SpecifyKind(model.EffectiveTo.Date, DateTimeKind.Unspecified);
            if (effectiveTo < effectiveFrom)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_DATE_RANGE",
                    "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.");
            }
            if ((effectiveTo - effectiveFrom).Days >= 31)
                throw new ErrorException(400, "DATE_RANGE_TOO_LONG", "Mỗi lượt gán tối đa 31 ngày, tính cả ngày đầu và ngày cuối.");

            return await InTransactionAsync(async () =>
            {
                IGenericRepository<Employee> employeeRepo = UnitOfWork.GetRepository<Employee>();
                List<Employee> employees = await employeeRepo.Entities
                    .Where(x => model.EmployeeIds.Contains(x.Id) &&
                        !x.DeletedTime.HasValue &&
                        x.Status != EmployeeStatus.Resigned &&
                        x.Status != EmployeeStatus.Terminated)
                    .Include(x => x.Department)
                    .ToListAsync();
                if (employees.Count != model.EmployeeIds.Count)
                {
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        "INVALID_EMPLOYEES",
                        "Một hoặc nhiều nhân viên không tồn tại hoặc đã được lưu trữ.");
                }

                if (!canManageAllEmployees)
                {
                    if (!managerEmployeeId.HasValue || employees.Any(x =>
                        x.ManagerId != managerEmployeeId &&
                        x.Department?.ManagerId != managerEmployeeId))
                    {
                        throw new ErrorException(
                            StatusCodes.Status403Forbidden,
                            "EMPLOYEE_OUT_OF_MANAGER_SCOPE",
                            "Bạn chỉ được xếp lịch cho nhân viên thuộc phạm vi phụ trách.");
                    }
                }

                IGenericRepository<Shift> shiftRepo = UnitOfWork.GetRepository<Shift>();
                Shift? shift = await shiftRepo.Entities.FirstOrDefaultAsync(x =>
                    x.Id == model.ShiftId && x.IsActive && !x.DeletedTime.HasValue);
                if (shift == null)
                {
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        "INVALID_SHIFT",
                        "Ca làm không tồn tại hoặc đang ngừng hoạt động.");
                }

                List<DateTime> dates = new();
                for (int offset = 0; offset <= (effectiveTo - effectiveFrom).Days; offset++)
                {
                    DateTime date = effectiveFrom.AddDays(offset);
                    int weekdayBit = 1 << (((int)date.DayOfWeek + 6) % 7);
                    if ((shift.WorkDays & weekdayBit) != 0 && (!model.SkipSunday || date.DayOfWeek != DayOfWeek.Sunday))
                        dates.Add(date);
                }
                if (dates.Count == 0)
                    throw new ErrorException(400, "NO_WORKING_DAYS", "Khoảng ngày đã chọn không có ngày làm việc phù hợp với ca và lựa chọn bỏ Chủ nhật.");

                IGenericRepository<EmployeeShift> assignmentRepo = UnitOfWork.GetRepository<EmployeeShift>();
                List<EmployeeShift> overlapping = await assignmentRepo.Entities
                    .Where(x => model.EmployeeIds.Contains(x.EmployeeId) &&
                        x.ShiftId == model.ShiftId &&
                        !x.DeletedTime.HasValue &&
                        x.EffectiveFrom <= effectiveTo &&
                        (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= effectiveFrom))
                    .ToListAsync();

                List<EmployeeShift> assignments = new();
                List<string> alreadyAssignedNames = new();
                foreach (Employee employee in employees)
                {
                    int assignmentsBeforeEmployee = assignments.Count;
                    List<EmployeeShift> employeeAssignments = overlapping
                        .Where(x => x.EmployeeId == employee.Id)
                        .OrderBy(x => x.EffectiveFrom)
                        .ToList();

                    foreach (DateTime date in dates)
                    {
                        if (!employeeAssignments.Any(existing => existing.EffectiveFrom.Date <= date &&
                            (!existing.EffectiveTo.HasValue || existing.EffectiveTo.Value.Date >= date)))
                            assignments.Add(CreateAssignment(employee.Id, model.ShiftId, date,
                                date, model.Note, currentUser));
                    }
                    if (assignments.Count == assignmentsBeforeEmployee && employeeAssignments.Count > 0)
                    {
                        alreadyAssignedNames.Add(employee.FullName);
                    }
                }

                if (assignments.Count > 0)
                {
                    assignmentRepo.InsertRange(assignments);
                    await UnitOfWork.SaveAsync();
                }

                return new CreateEmployeeShiftBulkResultModelView
                {
                    CreatedAssignmentCount = assignments.Count,
                    AssignedEmployeeCount = assignments.Select(x => x.EmployeeId).Distinct().Count(),
                    AlreadyAssignedCount = alreadyAssignedNames.Count,
                    AlreadyAssignedEmployeeNames = alreadyAssignedNames
                };
            });
        }

        private EmployeeShift CreateAssignment(
            Guid employeeId,
            Guid shiftId,
            DateTime effectiveFrom,
            DateTime effectiveTo,
            string? note,
            string currentUser)
        {
            return new EmployeeShift
            {
                EmployeeId = employeeId,
                ShiftId = shiftId,
                EffectiveFrom = DateTime.SpecifyKind(effectiveFrom.Date, DateTimeKind.Unspecified),
                EffectiveTo = DateTime.SpecifyKind(effectiveTo.Date, DateTimeKind.Unspecified),
                Note = note,
                CreatedBy = currentUser,
                CreatedTime = UtcNow
            };
        }

        public async Task UpdateAsync(UpdateEmployeeShiftModelView model)
        {
            IGenericRepository<EmployeeShift> repo = UnitOfWork.GetRepository<EmployeeShift>();

            EmployeeShift entity = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee shift assignment not found");
            RequireEmployeeAccess(entity.EmployeeId);

            string currentUser =
                CurrentUserName;

            model.ToEntity(entity);
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<EmployeeShift> repo = UnitOfWork.GetRepository<EmployeeShift>();

            EmployeeShift entity = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee shift assignment not found");

            string currentUser =
                CurrentUserName;

            entity.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<EmployeeShift> repo = UnitOfWork.GetRepository<EmployeeShift>();

            EmployeeShift entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee shift assignment not found");

            await repo.DeleteAsync(entity);
            await UnitOfWork.SaveAsync();
        }
    }
}
