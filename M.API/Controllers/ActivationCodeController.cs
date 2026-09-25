using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Store;
using Microsoft.AspNetCore.Mvc;
using ModelViews.ActivationCodeModelView;

namespace M.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivationCodeController : ControllerBase
    {
        private readonly IActivationCodeService _activationCodeService;

        public ActivationCodeController(
            IActivationCodeService activationCodeService)
        {
            _activationCodeService = activationCodeService;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            BasePaginatedList<ActivationCodeResponseModelView> result =
                await _activationCodeService.GetAllAsync(pageNumber, pageSize);

            return Ok(new BaseResponse<BasePaginatedList<ActivationCodeResponseModelView>>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result));
        }

        [HttpGet("get-by-id/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            ActivationCodeResponseModelView result =
                await _activationCodeService.GetByIdAsync(id);

            return Ok(new BaseResponse<ActivationCodeResponseModelView>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result));
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(
            [FromBody] CreateActivationCodeModelView model)
        {
            await _activationCodeService.CreateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Activation code created successfully!"));
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update(
            [FromBody] UpdateActivationCodeModelView model)
        {
            await _activationCodeService.UpdateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Activation code updated successfully!"));
        }

        [HttpDelete("soft-delete/{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            await _activationCodeService.SoftDeleteAsync(id);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Activation code deleted successfully!"));
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _activationCodeService.DeleteAsync(id);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Activation code permanently deleted successfully!"));
        }
    }
}
