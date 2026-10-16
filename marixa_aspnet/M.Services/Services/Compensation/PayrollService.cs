using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.PayrollModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Compensation
{
    public class PayrollService : BaseService, IPayrollService
    {

        public PayrollService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<PayrollResponseModelView>> GetAllAsync(int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);
            RequireStaff();

            IGenericRepository<Payroll> repo = UnitOfWork.GetRepository<Payroll>();

            IQueryable<Payroll> query = repo.Entities
                .Include(x => x.Employee)
                .Where(x => !x.DeletedTime.HasValue)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<Payroll> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<PayrollResponseModelView> result = items
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<PayrollResponseModelView>(
                result.AsReadOnly(),
                totalItems,
                pageNumber,
                pageSize);
        }

        public async Task<PayrollResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<Payroll> repo = UnitOfWork.GetRepository<Payroll>();

            Payroll entity = await repo.Entities
                .Include(x => x.Employee)
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Payroll not found");
            RequireEmployeeAccess(entity.EmployeeId);

            return entity.ToViewModel();
        }

        public async Task CreateAsync(CreatePayrollModelView model)
        {
            RequireEmployeeAccess(model.EmployeeId);

            IGenericRepository<Payroll> repo = UnitOfWork.GetRepository<Payroll>();

            // Kiểm tra Employee tồn tại
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

            // Kiểm tra duplicate payroll cho cùng kỳ (tháng-năm) và cùng nhân viên
            bool payrollExists = await repo.Entities
                .AnyAsync(x =>
                    x.EmployeeId == model.EmployeeId &&
                    x.PayrollMonth.Year == model.PayrollMonth.Year &&
                    x.PayrollMonth.Month == model.PayrollMonth.Month &&
                    !x.DeletedTime.HasValue);

            if (payrollExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Payroll for this employee and month already exists");
            }

            string currentUser = CurrentUserName;

            Payroll entity = model.ToEntity();
            entity.CreatedBy = currentUser;
            entity.CreatedTime = UtcNow;
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = UtcNow;

            await repo.InsertAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdatePayrollModelView model)
        {
            IGenericRepository<Payroll> repo = UnitOfWork.GetRepository<Payroll>();

            Payroll entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Payroll not found");
            RequireEmployeeAccess(entity.EmployeeId);

            // Kiểm tra Employee tồn tại
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

            // Kiểm tra trùng kỳ với record khác
            bool payrollExists = await repo.Entities
                .AnyAsync(x =>
                    x.Id != model.Id &&
                    x.EmployeeId == model.EmployeeId &&
                    x.PayrollMonth.Year == model.PayrollMonth.Year &&
                    x.PayrollMonth.Month == model.PayrollMonth.Month &&
                    !x.DeletedTime.HasValue);

            if (payrollExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Another payroll for this employee and month already exists");
            }

            string currentUser = CurrentUserName;

            model.ToEntity(entity);

            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<Payroll> repo = UnitOfWork.GetRepository<Payroll>();

            Payroll entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Payroll not found");

            string currentUser = CurrentUserName;

            entity.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<Payroll> repo = UnitOfWork.GetRepository<Payroll>();

            Payroll entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Payroll not found");

            await repo.DeleteAsync(entity);
            await UnitOfWork.SaveAsync();
        }
    }
}
