using M.Contract.Repositories.AuthEntities;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using M.ModelViews.ActivationCodeModelView;
using M.ModelViews.AuthModelView;

namespace M.API.Controllers.Identity
{
    [Route("api/[controller]")]
    [ApiController]
    // No Authorize
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

        /// <summary>
        /// Login (linh hoạt: username / email / sđt / mã nhân viên)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModelView model)
        {
            var result = await _authService.LoginAsync(model);

            return Ok(BaseResponse<AuthResponseModelView>.OkResponse(
                result,
                ResponseCodeConstants.SUCCESS
            ));
        }

        /// <summary>
        /// Register
        /// </summary>
        [Authorize(Roles = "Admin,Manager,HR")]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterModelView model)
        {
            await _authService.RegisterAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Register successfully!"
            ));
        }

        /// <summary>
        /// Kích hoạt tài khoản bằng mã kích hoạt (đặt mật khẩu mới).
        /// Quy trình bàn giao: nhân viên nhận mã kích hoạt, đặt mật khẩu, kích hoạt.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("activate")]
        public async Task<IActionResult> Activate([FromBody] ActivateAccountModelView model)
        {
            await _authService.ActivateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Account activated successfully!"
            ));
        }

        /// <summary>
        /// Đổi mật khẩu (đã xác thực qua token).
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModelView model)
        {
            await _authService.ChangePasswordAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Password changed successfully!"
            ));
        }

        /// <summary>
        /// Tạo mã kích hoạt cho một nhân viên (chạy trong quy trình bàn giao).
        /// </summary>
        [Authorize(Roles = "Admin,Manager,HR")]
        [HttpPost("create-activation-code")]
        public async Task<IActionResult> CreateActivationCode(
            [FromBody] CreateActivationCodeModelView model)
        {
            ActivationCodeResponseModelView result =
                await _authService.CreateActivationCodeAsync(model);

            return Ok(new BaseResponse<ActivationCodeResponseModelView>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result
            ));
        }

    }
}
