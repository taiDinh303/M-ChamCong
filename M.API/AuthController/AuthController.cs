using M.Contract.Repositories.Entity;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Store;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ModelViews.AuthModelView;

namespace M.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // No Authorize
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

        /// <summary>
        /// Login
        /// </summary>
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

    }
}