using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.ActivationCodeModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Identity
{
    public class ActivationCodeService : BaseService, IActivationCodeService
    {

        public ActivationCodeService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<ActivationCodeResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);

            IGenericRepository<ActivationCode> repo = UnitOfWork.GetRepository<ActivationCode>();

            IQueryable<ActivationCode> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .OrderByDescending(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<ActivationCode> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<ActivationCodeResponseModelView> result = items
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<ActivationCodeResponseModelView>(
                result.AsReadOnly(), totalItems, pageNumber, pageSize);
        }

        public async Task<ActivationCodeResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<ActivationCode> repo = UnitOfWork.GetRepository<ActivationCode>();

            ActivationCode item = await repo.Entities
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .Include(x => x.Employee)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Activation code not found");

            return item.ToViewModel();
        }

        public async Task CreateAsync(CreateActivationCodeModelView model)
        {
            IGenericRepository<ActivationCode> repo = UnitOfWork.GetRepository<ActivationCode>();

            IGenericRepository<Employee> employeeRepo =
                UnitOfWork.GetRepository<Employee>();

            bool employeeExists = await employeeRepo.Entities
                .AnyAsync(x => x.Id == model.EmployeeId && !x.DeletedTime.HasValue);

            if (!employeeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee not found");
            }

            bool codeExists = await repo.Entities
                .AnyAsync(x =>
                    x.Code == model.Code &&
                    !x.DeletedTime.HasValue);

            if (codeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Activation code already exists");
            }

            string currentUser =
                CurrentUserName;

            ActivationCode entity = model.ToEntity();
            entity.Code = model.Code.Trim();
            entity.CreatedBy = currentUser;
            entity.CreatedTime = UtcNow;

            await repo.InsertAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateActivationCodeModelView model)
        {
            IGenericRepository<ActivationCode> repo = UnitOfWork.GetRepository<ActivationCode>();

            ActivationCode entity = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Activation code not found");

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
            IGenericRepository<ActivationCode> repo = UnitOfWork.GetRepository<ActivationCode>();

            ActivationCode entity = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Activation code not found");

            string currentUser =
                CurrentUserName;

            entity.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<ActivationCode> repo = UnitOfWork.GetRepository<ActivationCode>();

            ActivationCode entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Activation code not found");

            await repo.DeleteAsync(entity);
            await UnitOfWork.SaveAsync();
        }
    }
}
