using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Store;
using Microsoft.AspNetCore.Mvc;
using ModelViews.EmployeeShiftModelView;

namespace M.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeShiftController : ControllerBase
    {
        private readonly IEmployeeShiftService _employeeShiftService;

        public EmployeeShiftController(IEmployeeShiftService employeeShiftService)
        {
            _employeeShiftService = employeeShiftService;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            BasePaginatedList<EmployeeShiftResponseModelView> result =
                await _employeeShiftService.GetAllAsync(pageNumber, pageSize);

            return Ok(new BaseResponse<BasePaginatedList<EmployeeShiftResponseModelView>>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result));
        }

        [HttpGet("get-by-id/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            EmployeeShiftResponseModelView result =
                await _employeeShiftService.GetByIdAsync(id);

            return Ok(new BaseResponse<EmployeeShiftResponseModelView>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result));
        }

        /// <summary>
        /// Retrieves shift assignments for a specific employee
        /// </summary>
        [HttpGet("by-employee/{employeeId}")]
        public async Task<IActionResult> ByEmployee(Guid employeeId)
        {
            List<EmployeeShiftResponseModelView> result =
                await _employeeShiftService.ByEmployeeIdAsync(employeeId);

            return Ok(new BaseResponse<List<EmployeeShiftResponseModelView>>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result
            ));
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(
            [FromBody] CreateEmployeeShiftModelView model)
        {
            await _employeeShiftService.CreateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Employee shift assigned successfully!"));
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update(
            [FromBody] UpdateEmployeeShiftModelView model)
        {
            await _employeeShiftService.UpdateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Employee shift updated successfully!"));
        }

        [HttpDelete("soft-delete/{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            await _employeeShiftService.SoftDeleteAsync(id);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Employee shift deleted successfully!"));
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _employeeShiftService.DeleteAsync(id);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Employee shift permanently deleted successfully!"));
        }
    }
}
