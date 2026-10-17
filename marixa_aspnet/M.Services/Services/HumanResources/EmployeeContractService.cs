using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.EmployeeContractModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.HumanResources
{
    public class EmployeeContractService : BaseService, IEmployeeContractService
    {

        public EmployeeContractService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<EmployeeContractResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);
            RequireStaff();

            IGenericRepository<EmployeeContract> repo =
                UnitOfWork.GetRepository<EmployeeContract>();

            IQueryable<EmployeeContract> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<EmployeeContract> contracts = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<EmployeeContractResponseModelView> result = contracts
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<EmployeeContractResponseModelView>(
                result.AsReadOnly(),
                totalItems,
                pageNumber,
                pageSize);
        }

        public async Task<EmployeeContractResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<EmployeeContract> repo =
                UnitOfWork.GetRepository<EmployeeContract>();

            EmployeeContract contract = await repo.Entities
                .Where(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee contract not found");
            RequireEmployeeAccess(contract.EmployeeId);

            return contract.ToViewModel();
        }


        public async Task<List<EmployeeContractResponseModelView>> ByEmployeeIdAsync(
            Guid employeeId)
        {
            RequireEmployeeAccess(employeeId);

            IGenericRepository<EmployeeContract> repo =
                UnitOfWork.GetRepository<EmployeeContract>();

            List<EmployeeContract> contracts = await repo.Entities
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();

            return contracts.Select(x => x.ToViewModel()).ToList();
        }
        public async Task CreateAsync(
            CreateEmployeeContractModelView model)
        {
            RequireEmployeeAccess(model.EmployeeId);

            IGenericRepository<EmployeeContract> repo =
                UnitOfWork.GetRepository<EmployeeContract>();

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

            // Kiểm tra ContractNumber
            // Kiểm tra ContractNumber trùng
            bool contractNumberExists = await repo.Entities
                .AnyAsync(x =>
                    x.ContractNumber == model.ContractNumber &&
                    !x.DeletedTime.HasValue);

            if (contractNumberExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Contract number already exists");
            }

            // Kiểm tra ContractType
            if (!Enum.IsDefined(
                    typeof(ContractType),
                    model.ContractType))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Invalid contract type");
            }

            // Kiểm tra ngày hợp đồng
            // Kiểm tra ngày
            if (model.EndDate.HasValue &&
                model.EndDate.Value < model.StartDate)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "End date cannot be earlier than start date");
            }

            string currentUser =
                CurrentUserName;

            EmployeeContract contract = model.ToEntity();

            contract.ContractNumber = model.ContractNumber.Trim();

            contract.CreatedBy = currentUser;
            contract.CreatedTime = UtcNow;

            await repo.InsertAsync(contract);
            await UnitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(
            UpdateEmployeeContractModelView model)
        {
            IGenericRepository<EmployeeContract> repo =
                UnitOfWork.GetRepository<EmployeeContract>();

            EmployeeContract contract = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee contract not found");
            RequireEmployeeAccess(contract.EmployeeId);

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

            // Kiểm tra ContractNumber trùng
            bool contractNumberExists = await repo.Entities
                .AnyAsync(x =>
                    x.Id != model.Id &&
                    x.ContractNumber == model.ContractNumber &&
                    !x.DeletedTime.HasValue);

            if (contractNumberExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Contract number already exists");
            }

            // Kiểm tra ContractType
            if (!Enum.IsDefined(
                    typeof(ContractType),
                    model.ContractType))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "Invalid contract type");
            }

            // Kiểm tra ngày hợp đồng
            // Kiểm tra ngày
            if (model.EndDate.HasValue &&
                model.EndDate.Value < model.StartDate)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "End date cannot be earlier than start date");
            }

            string currentUser =
                CurrentUserName;

            model.ToEntity(contract);

            contract.ContractNumber = model.ContractNumber.Trim();

            contract.LastUpdatedBy = currentUser;
            contract.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(contract);
            await UnitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<EmployeeContract> repo =
                UnitOfWork.GetRepository<EmployeeContract>();

            EmployeeContract contract = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee contract not found");

            string currentUser =
                CurrentUserName;

            contract.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(contract);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<EmployeeContract> repo =
                UnitOfWork.GetRepository<EmployeeContract>();

            EmployeeContract contract = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee contract not found");

            await repo.DeleteAsync(contract);
            await UnitOfWork.SaveAsync();
        }
    }
}
