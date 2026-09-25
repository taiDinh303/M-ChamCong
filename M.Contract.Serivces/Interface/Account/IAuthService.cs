using ModelViews.AuthModelView;

namespace M.Contract.Services.Interface
{
    public interface IAuthService
    {
        Task<AuthResponseModelView> LoginAsync(LoginModelView loginModel);
        Task RegisterAsync(RegisterModelView registerModelView);
    }
}
