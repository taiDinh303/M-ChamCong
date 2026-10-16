using M.Core.Security;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Identity
{
    public class UserLoginService : BaseService, IUserLoginService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserLoginService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
            _userManager = userManager;
        }

        // Check whether the user is connected to a login provider (e.g., Google).
        public async Task<bool> IsConnectedAsync(Guid userId, string provider)
        {
            IGenericRepository<ApplicationUserLogin> loginRepo = UnitOfWork.GetRepository<ApplicationUserLogin>();

            return await loginRepo.Entities
                .AnyAsync(l => l.UserId == userId && l.LoginProvider == provider);
        }

        // Retrieve all login providers linked to the user.
        public async Task<IReadOnlyList<ApplicationUserLogin>> GetAllByUserAsync(Guid userId)
        {
            IGenericRepository<ApplicationUserLogin> loginRepo = UnitOfWork.GetRepository<ApplicationUserLogin>();

            var logins = await loginRepo.Entities
                .Where(l => l.UserId == userId)
                .ToListAsync();

            return logins.AsReadOnly();
        }

        // Link a Google account (or another login provider).
        public async Task ConnectAsync(Guid userId, string provider, string providerKey, string? displayName = null)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    "User not found"
                );

            bool alreadyLinked = await IsConnectedAsync(userId, provider);
            if (alreadyLinked)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ResponseCodeConstants.DUPLICATE,
                    $"{provider} is already connected."
                );

            IGenericRepository<ApplicationUserLogin> loginRepo = UnitOfWork.GetRepository<ApplicationUserLogin>();

            ApplicationUserLogin newLogin = new ApplicationUserLogin
            {
                LoginProvider = provider,
                ProviderKey = providerKey,
                ProviderDisplayName = displayName ?? provider,
                UserId = userId,
            };

            await loginRepo.InsertAsync(newLogin);
            await UnitOfWork.SaveAsync();
        }


        // Need to update*****
        // Unlink a connected account (e.g., Google).
        public async Task DisconnectAsync(Guid userId, string provider)
        {
            IGenericRepository<ApplicationUserLogin> loginRepo = UnitOfWork.GetRepository<ApplicationUserLogin>();

            ApplicationUserLogin login = await loginRepo.Entities
                .FirstOrDefaultAsync(l => l.UserId == userId && l.LoginProvider == provider)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    $"{provider} connection was not found."
                );

            await loginRepo.DeleteAsync(login);
            await UnitOfWork.SaveAsync();
        }

    }
}
