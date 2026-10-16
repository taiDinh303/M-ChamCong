using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using M.Services.Mappings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.AuditLogModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Audit
{
    /// <summary>
    /// Dịch vụ audit trail (append-only). Ghi dòng log khi có thay đổi nghiệp vụ;
    /// đọc để đối soát. Không có UPDATE/DELETE nghiệp vụ (log chỉ INSERT).
    /// </summary>
    public class AuditLogService : BaseService, IAuditLogService
    {

        public AuditLogService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        public async Task<BasePaginatedList<AuditLogResponseModelView>> GetAllAsync(int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);

            IGenericRepository<AuditLog> repo = UnitOfWork.GetRepository<AuditLog>();

            IQueryable<AuditLog> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue)
                .OrderByDescending(x => x.CreatedTime);

            int totalItems = await query.CountAsync();
            List<AuditLog> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<AuditLogResponseModelView>(
                items.Select(x => x.ToViewModel()).ToList().AsReadOnly(),
                totalItems, pageNumber, pageSize);
        }

        public async Task<BasePaginatedList<AuditLogResponseModelView>> QueryAsync(
            string? entityType,
            Guid? entityId,
            string? actorUserName,
            int pageNumber,
            int pageSize)
        {
            IGenericRepository<AuditLog> repo = UnitOfWork.GetRepository<AuditLog>();

            IQueryable<AuditLog> query = repo.Entities
                .Where(x => !x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(x => x.EntityType == entityType);
            if (entityId.HasValue && entityId.Value != Guid.Empty)
                query = query.Where(x => x.EntityId == entityId.Value);
            if (!string.IsNullOrWhiteSpace(actorUserName))
                query = query.Where(x => x.ActorUserName == actorUserName);

            query = query.OrderByDescending(x => x.CreatedTime);

            int totalItems = await query.CountAsync();
            List<AuditLog> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<AuditLogResponseModelView>(
                items.Select(x => x.ToViewModel()).ToList().AsReadOnly(),
                totalItems, pageNumber, pageSize);
        }

        public async Task<AuditLogResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<AuditLog> repo = UnitOfWork.GetRepository<AuditLog>();

            AuditLog entity = await repo.Entities
                .Include(x => x.ActorEmployee)
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Audit log not found");

            return entity.ToViewModel();
        }

        public async Task CreateAsync(CreateAuditLogModelView model)
        {
            IGenericRepository<AuditLog> repo = UnitOfWork.GetRepository<AuditLog>();

            string currentUser =
                CurrentUserName;

            // Ưu tiên actor từ context (nhân đang đăng nhập) nếu chưa truyền ActorUserName.
            if (string.IsNullOrWhiteSpace(model.ActorUserName))
                model.ActorUserName = currentUser;

            AuditLog entity = model.ToEntity();
            entity.CreatedBy = currentUser;
            entity.LastUpdatedBy = currentUser;
            entity.CreatedTime = UtcNow;
            entity.LastUpdatedTime = UtcNow;

            await repo.InsertAsync(entity);
            await UnitOfWork.SaveAsync();
        }
    }
}
