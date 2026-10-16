using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.AttendanceRuleModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Timekeeping
{
    public class AttendanceRuleService : BaseService, IAttendanceRuleService
    {

        public AttendanceRuleService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<List<AttendanceRuleResponseModelView>> GetActiveAsync()
        {
            var rules = await Repository<AttendanceRule>().Entities.AsNoTracking()
                .Where(r => r.IsActive && !r.DeletedTime.HasValue).OrderByDescending(r => r.CreatedTime).ToListAsync();
            return rules.Select(r => r.ToViewModel()).ToList();
        }

        public async Task<BasePaginatedList<AttendanceRuleResponseModelView>> GetAllAsync(
            int pageNumber,
            int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);

            IGenericRepository<AttendanceRule> repo = UnitOfWork.GetRepository<AttendanceRule>();

            IQueryable<AttendanceRule> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<AttendanceRule> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<AttendanceRuleResponseModelView> result = items
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<AttendanceRuleResponseModelView>(
                result.AsReadOnly(), totalItems, pageNumber, pageSize);
        }

        public async Task<AttendanceRuleResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<AttendanceRule> repo = UnitOfWork.GetRepository<AttendanceRule>();

            AttendanceRule item = await repo.Entities
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance rule not found");

            return item.ToViewModel();
        }

        public async Task CreateAsync(CreateAttendanceRuleModelView model)
        {
            RequireAdmin();
            await UnitOfWork.BeginTransactionAsync();
            try
            {
                await CreateAsyncCore(model);
                await UnitOfWork.CommitTransactionAsync();
            }
            catch { await UnitOfWork.RollBackAsync(); throw; }
        }

        private async Task CreateAsyncCore(CreateAttendanceRuleModelView model)
        {
            IGenericRepository<AttendanceRule> repo = UnitOfWork.GetRepository<AttendanceRule>();

            bool codeExists = await repo.Entities
                .AnyAsync(x =>
                    x.Code == model.Code &&
                    !x.DeletedTime.HasValue);

            if (codeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Attendance rule code already exists");
            }

            string currentUser =
                CurrentUserName;

            if (model.IsActive) await DeactivateOtherRulesAsync(null);
            AttendanceRule entity = model.ToEntity();
            entity.Code = model.Code.Trim();
            entity.Name = model.Name.Trim();
            entity.CreatedBy = currentUser;
            entity.CreatedTime = UtcNow;

            await repo.InsertAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateAttendanceRuleModelView model)
        {
            RequireAdmin();
            await UnitOfWork.BeginTransactionAsync();
            try
            {
                await UpdateAsyncCore(model);
                await UnitOfWork.CommitTransactionAsync();
            }
            catch { await UnitOfWork.RollBackAsync(); throw; }
        }

        private async Task UpdateAsyncCore(UpdateAttendanceRuleModelView model)
        {
            IGenericRepository<AttendanceRule> repo = UnitOfWork.GetRepository<AttendanceRule>();

            AttendanceRule entity = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance rule not found");

            string currentUser =
                CurrentUserName;

            if (model.IsActive) await DeactivateOtherRulesAsync(entity.Id);
            else if (entity.IsActive) throw new ErrorException(409, "ACTIVE_RULE", "Activate a replacement before disabling the active rule.");
            model.ToEntity(entity);
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = UtcNow;

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            RequireAdmin();
            IGenericRepository<AttendanceRule> repo = UnitOfWork.GetRepository<AttendanceRule>();

            AttendanceRule entity = await repo.Entities
                .FirstOrDefaultAsync(x =>
                    x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance rule not found");

            if (entity.IsActive) throw new ErrorException(409, "ACTIVE_RULE", "Activate a replacement before deleting the active rule.");
            string currentUser =
                CurrentUserName;

            entity.SoftDelete(currentUser, UtcNow);

            await repo.UpdateAsync(entity);
            await UnitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            RequireAdmin();
            IGenericRepository<AttendanceRule> repo = UnitOfWork.GetRepository<AttendanceRule>();

            AttendanceRule entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Attendance rule not found");

            if (entity.IsActive) throw new ErrorException(409, "ACTIVE_RULE", "Activate a replacement before deleting the active rule.");
            await repo.DeleteAsync(entity);
            await UnitOfWork.SaveAsync();
        }
        private async Task DeactivateOtherRulesAsync(Guid? keepId)
        {
            var active = await Repository<AttendanceRule>().Entities
                .Where(r => r.IsActive && !r.DeletedTime.HasValue && r.Id != keepId).ToListAsync();
            foreach (var rule in active)
            {
                rule.IsActive = false;
                rule.MarkUpdated(CurrentUserName, UtcNow);
            }
            await UnitOfWork.SaveAsync();
        }
    }
}
