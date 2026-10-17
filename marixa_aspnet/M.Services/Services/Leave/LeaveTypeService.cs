using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.LeaveTypeModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Leave
{
    public class LeaveTypeService : BaseService, ILeaveTypeService
    {

        public LeaveTypeService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<LeaveTypeResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);

            IGenericRepository<LeaveType> repo =
                UnitOfWork.GetRepository<LeaveType>();

            IQueryable<LeaveType> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<LeaveType> leaveTypes = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<LeaveTypeResponseModelView> result = leaveTypes
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<LeaveTypeResponseModelView>(
                result.AsReadOnly(),
                totalItems,
                pageNumber,
                pageSize);
        }

        public async Task<LeaveTypeResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<LeaveType> repo =
                UnitOfWork.GetRepository<LeaveType>();

            LeaveType leaveType = await repo.Entities
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Leave type not found");

            return leaveType.ToViewModel();
        }

        public async Task CreateAsync(CreateLeaveTypeModelView model)
        {
            IGenericRepository<LeaveType> repo =
                UnitOfWork.GetRepository<LeaveType>();

            // Kiểm tra mã loại nghỉ phép (Code) trùng lặp
            bool codeExists = await repo.Entities
                .AnyAsync(x =>
                    x.Code == model.Code &&
                    !x.DeletedTime.HasValue);

            if (codeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Leave type code already exists");
            }

            string currentUser =
                CurrentUserName;

            LeaveType leaveType = model.ToEntity();

            leaveType.Code = model.Code.Trim();
            leaveType.Name = model.Name.Trim();

            leaveType.CreatedBy = currentUser;
            leaveType.CreatedTime = UtcNow;

            await repo.InsertAsync(leaveType);
            await UnitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateLeaveTypeModelView model)
        {
            IGenericRepository<LeaveType> repo =
                UnitOfWork.GetRepository<LeaveType>();

            LeaveType leaveType = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Leave type not found");

            // Kiểm tra trùng mã loại nghỉ phép với bản ghi khác
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
                    "Leave type code already exists");
            }

            // Audit
            string currentUser =
                CurrentUserName;

            model.ToEntity(leaveType);

            leaveType.LastUpdatedBy = currentUser;
            leaveType.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(leaveType);
            await UnitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<LeaveType> repo =
                UnitOfWork.GetRepository<LeaveType>();

            LeaveType leaveType = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Leave type not found");

            string currentUser =
                CurrentUserName;

            leaveType.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(leaveType);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<LeaveType> repo =
                UnitOfWork.GetRepository<LeaveType>();

            LeaveType leaveType = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Leave type not found");

            await repo.DeleteAsync(leaveType);
            await UnitOfWork.SaveAsync();
        }
    }
}
