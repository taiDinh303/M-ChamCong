using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.EmployeeDependentModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.HumanResources
{
    public class EmployeeDependentService : BaseService, IEmployeeDependentService
    {

        public EmployeeDependentService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<EmployeeDependentResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);
            RequireStaff();

            IGenericRepository<EmployeeDependent> repo =
                UnitOfWork.GetRepository<EmployeeDependent>();

            IQueryable<EmployeeDependent> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<EmployeeDependent> dependents = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<EmployeeDependentResponseModelView> result = dependents
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<EmployeeDependentResponseModelView>(
                result.AsReadOnly(),
                totalItems,
                pageNumber,
                pageSize);
        }

        public async Task<EmployeeDependentResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<EmployeeDependent> repo =
                UnitOfWork.GetRepository<EmployeeDependent>();

            EmployeeDependent dependent = await repo.Entities
                .Where(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee dependent not found");
            RequireEmployeeAccess(dependent.EmployeeId);

            return dependent.ToViewModel();
        }

        public async Task CreateAsync(
            CreateEmployeeDependentModelView model)
        {
            RequireEmployeeAccess(model.EmployeeId);

            IGenericRepository<EmployeeDependent> repo =
                UnitOfWork.GetRepository<EmployeeDependent>();

            // Kiểm tra Employee
            IGenericRepository<Employee> employeeRepo =
                UnitOfWork.GetRepository<Employee>();

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

            // Kiểm tra ngày
            if (model.EffectiveFrom.HasValue &&
                model.EffectiveTo.HasValue &&
                model.EffectiveTo.Value < model.EffectiveFrom.Value)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Effective to cannot be earlier than effective from");
            }

            // Kiểm tra ngày sinh không vượt quá ngày hiện tại
            if (model.BirthDate.HasValue &&
                model.BirthDate.Value.Date > UtcNow.Date)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Birth date cannot be in the future");
            }

            string currentUser =
                CurrentUserName;

            EmployeeDependent dependent = model.ToEntity();

            dependent.GivenName = model.GivenName.Trim();
            dependent.FamilyName = model.FamilyName.Trim();

            if (!string.IsNullOrWhiteSpace(model.CitizenId))
                dependent.CitizenId = model.CitizenId.Trim();

            if (!string.IsNullOrWhiteSpace(model.TaxIdentificationNumber))
                dependent.TaxIdentificationNumber =
                    model.TaxIdentificationNumber.Trim();

            dependent.CreatedBy = currentUser;
            dependent.CreatedTime = UtcNow;

            await repo.InsertAsync(dependent);
            await UnitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(
            UpdateEmployeeDependentModelView model)
        {
            IGenericRepository<EmployeeDependent> repo =
                UnitOfWork.GetRepository<EmployeeDependent>();

            EmployeeDependent dependent = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee dependent not found");
            RequireEmployeeAccess(dependent.EmployeeId);

            // Kiểm tra Employee
            IGenericRepository<Employee> employeeRepo =
                UnitOfWork.GetRepository<Employee>();

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

            // Kiểm tra ngày
            if (model.EffectiveFrom.HasValue &&
                model.EffectiveTo.HasValue &&
                model.EffectiveTo.Value < model.EffectiveFrom.Value)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Effective to cannot be earlier than effective from");
            }

            // Kiểm tra ngày sinh
            if (model.BirthDate.HasValue &&
                model.BirthDate.Value.Date > UtcNow.Date)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Birth date cannot be in the future");
            }

            string currentUser =
                CurrentUserName;

            model.ToEntity(dependent);

            dependent.GivenName = model.GivenName.Trim();
            dependent.FamilyName = model.FamilyName.Trim();

            if (!string.IsNullOrWhiteSpace(model.CitizenId))
                dependent.CitizenId = model.CitizenId.Trim();
            else
                dependent.CitizenId = null;

            if (!string.IsNullOrWhiteSpace(model.TaxIdentificationNumber))
                dependent.TaxIdentificationNumber =
                    model.TaxIdentificationNumber.Trim();
            else
                dependent.TaxIdentificationNumber = null;

            dependent.LastUpdatedBy = currentUser;
            dependent.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(dependent);
            await UnitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<EmployeeDependent> repo =
                UnitOfWork.GetRepository<EmployeeDependent>();

            EmployeeDependent dependent = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee dependent not found");

            string currentUser =
                CurrentUserName;

            dependent.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(dependent);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<EmployeeDependent> repo =
                UnitOfWork.GetRepository<EmployeeDependent>();

            EmployeeDependent dependent = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee dependent not found");

            await repo.DeleteAsync(dependent);
            await UnitOfWork.SaveAsync();
        }
    }
}