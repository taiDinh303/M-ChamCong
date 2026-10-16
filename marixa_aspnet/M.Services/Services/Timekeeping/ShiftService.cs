using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.ShiftModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Timekeeping
{
    public class ShiftService : BaseService, IShiftService
    {

        public ShiftService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<ShiftResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);

            IGenericRepository<Shift> repo = UnitOfWork.GetRepository<Shift>();

            IQueryable<Shift> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<Shift> shifts = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<ShiftResponseModelView> result = shifts
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<ShiftResponseModelView>(
                result.AsReadOnly(), totalItems, pageNumber, pageSize);
        }

        public async Task<ShiftResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<Shift> repo = UnitOfWork.GetRepository<Shift>();

            Shift shift = await repo.Entities
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Shift not found");

            return shift.ToViewModel();
        }

        public async Task CreateAsync(CreateShiftModelView model)
        {
            IGenericRepository<Shift> repo = UnitOfWork.GetRepository<Shift>();

            bool codeExists = await repo.Entities
                .AnyAsync(x =>
                    x.Code == model.Code &&
                    !x.DeletedTime.HasValue);

            if (codeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Shift code already exists");
            }

            string currentUser =
                CurrentUserName;

            Shift shift = model.ToEntity();
            shift.Code = model.Code.Trim();
            shift.Name = model.Name.Trim();
            shift.CreatedBy = currentUser;
            shift.CreatedTime = UtcNow;

            await repo.InsertAsync(shift);
            await UnitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateShiftModelView model)
        {
            IGenericRepository<Shift> repo = UnitOfWork.GetRepository<Shift>();

            Shift shift = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Shift not found");

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
                    "Shift code already exists");
            }

            string currentUser =
                CurrentUserName;

            model.ToEntity(shift);
            shift.LastUpdatedBy = currentUser;
            shift.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(shift);
            await UnitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<Shift> repo = UnitOfWork.GetRepository<Shift>();

            Shift shift = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Shift not found");

            string currentUser =
                CurrentUserName;

            shift.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(shift);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<Shift> repo = UnitOfWork.GetRepository<Shift>();

            Shift shift = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Shift not found");

            await repo.DeleteAsync(shift);
            await UnitOfWork.SaveAsync();
        }
    }
}
