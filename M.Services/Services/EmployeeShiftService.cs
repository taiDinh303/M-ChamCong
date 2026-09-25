using M.Contract.Repositories.Entities;
using M.Contract.Repositories.IUOW;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelViews.EmployeeShiftModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Service
{
    public class EmployeeShiftService : IEmployeeShiftService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmployeeShiftService(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<BasePaginatedList<EmployeeShiftResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            IGenericRepository<EmployeeShift> repo = _unitOfWork.GetRepository<EmployeeShift>();

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
            IGenericRepository<EmployeeShift> repo = _unitOfWork.GetRepository<EmployeeShift>();

            EmployeeShift item = await repo.Entities
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .Include(x => x.Shift)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee shift assignment not found");

            return item.ToViewModel();
        }

        public async Task CreateAsync(CreateEmployeeShiftModelView model)
        {
            IGenericRepository<EmployeeShift> repo = _unitOfWork.GetRepository<EmployeeShift>();

            IGenericRepository<Employee> employeeRepo = _unitOfWork.GetRepository<Employee>();
            bool employeeExists = await employeeRepo.Entities
                .AnyAsync(x => x.Id == model.EmployeeId && !x.DeletedTime.HasValue);

            if (!employeeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee not found");
            }

            IGenericRepository<Shift> shiftRepo = _unitOfWork.GetRepository<Shift>();
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
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";

            EmployeeShift entity = model.ToEntity();
            entity.CreatedBy = currentUser;
            entity.CreatedTime = CoreHelper.SystemTimeNow;

            await repo.InsertAsync(entity);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateEmployeeShiftModelView model)
        {
            IGenericRepository<EmployeeShift> repo = _unitOfWork.GetRepository<EmployeeShift>();

            EmployeeShift entity = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee shift assignment not found");

            string currentUser =
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";

            model.ToEntity(entity);
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await repo.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<EmployeeShift> repo = _unitOfWork.GetRepository<EmployeeShift>();

            EmployeeShift entity = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee shift assignment not found");

            string currentUser =
                _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? "System";

            entity.DeletedBy = currentUser;
            entity.DeletedTime = CoreHelper.SystemTimeNow;

            await repo.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<EmployeeShift> repo = _unitOfWork.GetRepository<EmployeeShift>();

            EmployeeShift entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee shift assignment not found");

            await repo.DeleteAsync(entity);
            await _unitOfWork.SaveAsync();
        }
    }
}
