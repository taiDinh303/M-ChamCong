using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.SalaryGroupModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Compensation
{
    public class SalaryGroupService : BaseService, ISalaryGroupService
    {

        public SalaryGroupService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<SalaryGroupResponseModelView>> GetAllAsync(int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);

            IGenericRepository<SalaryGroup> repo = UnitOfWork.GetRepository<SalaryGroup>();

            IQueryable<SalaryGroup> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<SalaryGroup> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<SalaryGroupResponseModelView> result = items
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<SalaryGroupResponseModelView>(
                result.AsReadOnly(),
                totalItems,
                pageNumber,
                pageSize);
        }

        public async Task<SalaryGroupResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<SalaryGroup> repo = UnitOfWork.GetRepository<SalaryGroup>();

            SalaryGroup entity = await repo.Entities
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Salary group not found");

            return entity.ToViewModel();
        }

        public async Task CreateAsync(CreateSalaryGroupModelView model)
        {
            IGenericRepository<SalaryGroup> repo = UnitOfWork.GetRepository<SalaryGroup>();

            bool codeExists = await repo.Entities
                .AnyAsync(x => x.Code == model.Code.Trim() && !x.DeletedTime.HasValue);

            if (codeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Salary group code already exists");
            }

            string currentUser = CurrentUserName;

            SalaryGroup entity = model.ToEntity();
            entity.Code = model.Code.Trim();
            entity.Name = model.Name.Trim();
            entity.CreatedBy = currentUser;
            entity.CreatedTime = UtcNow;
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = UtcNow;

            await repo.InsertAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateSalaryGroupModelView model)
        {
            IGenericRepository<SalaryGroup> repo = UnitOfWork.GetRepository<SalaryGroup>();

            SalaryGroup entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Salary group not found");

            bool codeExists = await repo.Entities
                .AnyAsync(x => x.Id != model.Id && x.Code == model.Code.Trim() && !x.DeletedTime.HasValue);

            if (codeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Salary group code already exists");
            }

            string currentUser = CurrentUserName;

            model.ToEntity(entity);

            entity.Code = model.Code.Trim();
            entity.Name = model.Name.Trim();
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<SalaryGroup> repo = UnitOfWork.GetRepository<SalaryGroup>();

            SalaryGroup entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Salary group not found");

            string currentUser = CurrentUserName;

            entity.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<SalaryGroup> repo = UnitOfWork.GetRepository<SalaryGroup>();

            SalaryGroup entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Salary group not found");

            await repo.DeleteAsync(entity);
            await UnitOfWork.SaveAsync();
        }
    }
}
