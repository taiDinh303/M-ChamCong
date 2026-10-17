using M.Core.Security;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.RoleModelView;
using M.Services.Mappings;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Identity
{
    public class RoleService : BaseService, IRoleService
    {
        // Vai trò hệ thống (đã seed) không cho xóa để tránh mất phân quyền.
        private static readonly HashSet<string> ProtectedRoleNames =
            new(StringComparer.OrdinalIgnoreCase) { "Admin", "Employee", "Manager", "HR" };

        private void GuardSystemRoleDeletion(ApplicationRole role)
        {
            if (role != null && !string.IsNullOrEmpty(role.Name)
                && ProtectedRoleNames.Contains(role.Name))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ResponseCodeConstants.FAILED,
                    $"Vai trò hệ thống '{role.Name}' không thể xóa.");
            }
        }
        private readonly RoleManager<ApplicationRole> _roleManager;

        public RoleService(IUnitOfWork unitOfWork, RoleManager<ApplicationRole> roleManager, ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
            _roleManager = roleManager;
        }

        public async Task<BasePaginatedList<RoleResponseModelView>> GetAllAsync(int pageNumber, int pageSize)
        {
            RequireAdmin();
            (pageNumber, pageSize) = Pagination.Normalize(pageNumber, pageSize);

            IGenericRepository<ApplicationRole> repo = UnitOfWork.GetRepository<ApplicationRole>();
            IQueryable<ApplicationRole> query = repo.Entities
                .Where(r => !r.DeletedTime.HasValue)
                .OrderBy(u => u.CreatedTime);

            int totalItems = await query.CountAsync();

            List<ApplicationRole> roles = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize).ToListAsync();

            List<RoleResponseModelView> result = roles.ToViewModelList();

            return new BasePaginatedList<RoleResponseModelView>(
                result.AsReadOnly(),
                totalItems,
                pageNumber,
                pageSize);
        }

        public async Task<RoleResponseModelView> GetByIdAsync(Guid id)
        {
            RequireAdmin();
            ApplicationRole? role = await UnitOfWork.GetRepository<ApplicationRole>().Entities
                .Where(r => r.Id == id && !r.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    "Role not found"
                );

            return role.ToViewModel();
        }


        public async Task CreateAsync(CreateRoleModelView request)
        {
            RequireAdmin();
            ApplicationRole? existingRole = await _roleManager.FindByNameAsync(request.Name ?? string.Empty);
            if (existingRole != null && !existingRole.DeletedTime.HasValue)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ResponseCodeConstants.EXISTED,
                    "Role already exists"
                );
            }

            ApplicationRole newRole = request.ToEntity();

            IdentityResult result = await _roleManager.CreateAsync(newRole);
            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.FAILED, errors);
            }
        }

        public async Task UpdateAsync(UpdateRoleModelView request)
        {
            RequireAdmin();
            ApplicationRole? role = await _roleManager.FindByIdAsync(request.Id.ToString());
            if (role == null || role.DeletedTime.HasValue)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    "Role not found"
                );
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                if (ProtectedRoleNames.Contains(role.Name ?? string.Empty)
                    && !string.Equals(role.Name, request.Name.Trim(), StringComparison.Ordinal))
                    throw new ErrorException(400, "SYSTEM_ROLE", "System roles cannot be renamed.");
                role.Name = request.Name;
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                role.Description = request.Description;
            }

            //Audit
            string? currentUser = CurrentUserName;
            role.LastUpdatedBy = currentUser ?? "System";
            role.LastUpdatedTime = UtcNow;

            IdentityResult result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.FAILED, errors);
            }
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            RequireAdmin();
            IGenericRepository<ApplicationRole> roleRepo = UnitOfWork.GetRepository<ApplicationRole>();
            ApplicationRole role = await roleRepo.Entities
                .FirstOrDefaultAsync(r => r.Id == id && !r.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Role not found");

            GuardSystemRoleDeletion(role);

            //Audit
            string currentUser = CurrentUserName;
            role.DeletedBy = currentUser;
            role.DeletedTime = UtcNow;

            await roleRepo.UpdateAsync(role);
            await UnitOfWork.SaveAsync();
        }


        public async Task DeleteAsync(Guid id)
        {
            RequireAdmin();
            ApplicationRole role = await _roleManager.FindByIdAsync(id.ToString())
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    "Role not found"
            );

            GuardSystemRoleDeletion(role);

            //delete by roleManager
            IdentityResult result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ResponseCodeConstants.FAILED,
                    errors
                );
            }
        }
    }
}
