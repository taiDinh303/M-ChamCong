using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.BankModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Compensation
{
    public class BankService : BaseService, IBankService
    {

        public BankService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<BankResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);

            IGenericRepository<Bank> repo =
                UnitOfWork.GetRepository<Bank>();

            IQueryable<Bank> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<Bank> banks = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<BankResponseModelView> result = banks
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<BankResponseModelView>(
                result.AsReadOnly(),
                totalItems,
                pageNumber,
                pageSize);
        }

        public async Task<BankResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<Bank> repo =
                UnitOfWork.GetRepository<Bank>();

            Bank bank = await repo.Entities
                .Where(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Bank not found");

            return bank.ToViewModel();
        }

        public async Task CreateAsync(CreateBankModelView model)
        {
            IGenericRepository<Bank> repo =
                UnitOfWork.GetRepository<Bank>();

            // Kiểm tra Code
            bool codeExists = await repo.Entities
                .AnyAsync(x =>
                    x.Code == model.Code &&
                    !x.DeletedTime.HasValue);

            if (codeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Bank code already exists");
            }

            string currentUser =
                CurrentUserName;

            Bank bank = model.ToEntity();

            bank.Code = model.Code.Trim();
            bank.Name = model.Name.Trim();

            if (!string.IsNullOrWhiteSpace(model.ShortName))
            {
                bank.ShortName = model.ShortName.Trim();
            }

            bank.CreatedBy = currentUser;
            bank.CreatedTime = UtcNow;

            await repo.InsertAsync(bank);
            await UnitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateBankModelView model)
        {
            IGenericRepository<Bank> repo =
                UnitOfWork.GetRepository<Bank>();

            Bank bank = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Bank not found");

            // Kiểm tra Code trùng
            bool codeExists = await repo.Entities
                .AnyAsync(x =>
                    x.Id != model.Id &&
                    x.Code == model.Code &&
                    !x.DeletedTime.HasValue);

            if (codeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Bank code already exists");
            }

            string currentUser =
                CurrentUserName;

            model.ToEntity(bank);

            bank.Code = model.Code.Trim();
            bank.Name = model.Name.Trim();

            if (!string.IsNullOrWhiteSpace(model.ShortName))
            {
                bank.ShortName = model.ShortName.Trim();
            }
            else
            {
                bank.ShortName = null;
            }

            bank.LastUpdatedBy = currentUser;
            bank.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(bank);
            await UnitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<Bank> repo =
                UnitOfWork.GetRepository<Bank>();

            Bank bank = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Bank not found");

            string currentUser =
                CurrentUserName;

            bank.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(bank);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<Bank> repo =
                UnitOfWork.GetRepository<Bank>();

            Bank bank = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Bank not found");

            await repo.DeleteAsync(bank);
            await UnitOfWork.SaveAsync();
        }
    }
}
