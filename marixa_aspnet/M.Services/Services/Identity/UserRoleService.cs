using M.Core.Security;
using M.Contract.Services.Security;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Services.Interface;
using M.Core.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Identity
{
    public class UserRoleService : IUserRoleService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAdminAccountGuard _adminGuard;
        private readonly ICurrentUser _actor;

        public UserRoleService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IAdminAccountGuard adminGuard, ICurrentUser actor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _adminGuard = adminGuard;
            _actor = actor;
        }

        public async Task AddRoleToUserAsync(Guid userId, Guid roleId)
        {
            if (!_actor.IsInRole(RoleNames.Admin))
                throw new ErrorException(403, "FORBIDDEN", "Administrator access required.");
            // find user
            ApplicationUser user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound,
                                            ResponseCodeConstants.NOT_FOUND,
                                            "User not found");

            // find role
            ApplicationRole role = await _roleManager.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId && !r.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound,
                                            ResponseCodeConstants.NOT_FOUND,
                                            "Role not found");

            // check duplicate
            bool alreadyInRole = await _userManager.IsInRoleAsync(user, role.Name!);
            if (alreadyInRole)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest,
                                         ResponseCodeConstants.DUPLICATE,
                                         "User already has this role");
            }

            // add
            IdentityResult result = await _userManager.AddToRoleAsync(user, role.Name!);
            if (!result!.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ErrorException(StatusCodes.Status400BadRequest,
                                         ResponseCodeConstants.FAILED,
                                         $"Failed to assign role: {errors}");
            }
        }


        public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId)
        {
            if (!_actor.IsInRole(RoleNames.Admin))
                throw new ErrorException(403, "FORBIDDEN", "Administrator access required.");
            ApplicationUser user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound,
                                            ResponseCodeConstants.NOT_FOUND,
                                            "User not found");

            ApplicationRole role = await _roleManager.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId && !r.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound,
                                            ResponseCodeConstants.NOT_FOUND,
                                            "Role not found");

            bool inRole = await _userManager.IsInRoleAsync(user, role.Name!);
            if (!inRole)
            {
                throw new ErrorException(StatusCodes.Status404NotFound,
                                         ResponseCodeConstants.NOT_FOUND,
                                         "User does not have this role");
            }

            IdentityResult? result = null;
            async Task Remove() => result = await _userManager.RemoveFromRoleAsync(user, role.Name!);
            if (role.Name == "Admin") await _adminGuard.ProtectDeactivationAsync(userId, Remove);
            else await Remove();
            if (!result!.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ErrorException(StatusCodes.Status400BadRequest,
                                         ResponseCodeConstants.FAILED,
                                         $"Failed to remove role: {errors}");
            }
        }

        public async Task<IList<string>> GetRolesByUserAsync(Guid userId)
        {
            if (!_actor.IsAuthenticated || (_actor.UserId != userId && !_actor.IsInRole(RoleNames.Admin)))
                throw new ErrorException(403, "FORBIDDEN", "User roles are outside your access scope.");
            ApplicationUser user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound,
                                            ResponseCodeConstants.NOT_FOUND,
                                            "User not found");

            IList<string> roles = await _userManager.GetRolesAsync(user);
            return roles;
        }
    }
}
