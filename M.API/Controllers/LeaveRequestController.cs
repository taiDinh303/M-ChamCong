using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Store;
using Microsoft.AspNetCore.Mvc;
using ModelViews.LeaveRequestModelView;

namespace M.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveRequestController : ControllerBase
    {
        private readonly ILeaveRequestService _leaveRequestService;

        public LeaveRequestController(ILeaveRequestService leaveRequestService)
        {
            _leaveRequestService = leaveRequestService;
        }

        /// <summary>
        /// Retrieves all leave requests with pagination
        /// </summary>
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            BasePaginatedList<LeaveRequestResponseModelView> result =
                await _leaveRequestService.GetAllAsync(pageNumber, pageSize);

            return Ok(new BaseResponse<BasePaginatedList<LeaveRequestResponseModelView>>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result
            ));
        }

        /// <summary>
        /// Retrieves a leave request by its ID
        /// </summary>
        [HttpGet("get-by-id/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            LeaveRequestResponseModelView result =
                await _leaveRequestService.GetByIdAsync(id);

            return Ok(new BaseResponse<LeaveRequestResponseModelView>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result
            ));
        }

        /// <summary>
        /// Retrieves leave requests for a specific employee
        /// </summary>
        [HttpGet("by-employee/{employeeId}")]
        public async Task<IActionResult> ByEmployee(Guid employeeId)
        {
            List<LeaveRequestResponseModelView> result =
                await _leaveRequestService.ByEmployeeIdAsync(employeeId);

            return Ok(new BaseResponse<List<LeaveRequestResponseModelView>>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result
            ));
        }

        /// <summary>
        /// Creates a new leave request
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateLeaveRequestModelView model)
        {
            await _leaveRequestService.CreateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Leave request created successfully!"
            ));
        }

        /// <summary>
        /// Updates a leave request
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateLeaveRequestModelView model)
        {
            await _leaveRequestService.UpdateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Leave request updated successfully!"
            ));
        }

        /// <summary>
        /// Soft deletes a leave request by ID
        /// </summary>
        [HttpDelete("soft-delete/{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            await _leaveRequestService.SoftDeleteAsync(id);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Leave request deleted successfully!"
            ));
        }

        /// <summary>
        /// Permanently deletes a leave request by ID
        /// </summary>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _leaveRequestService.DeleteAsync(id);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Leave request permanently deleted successfully!"
            ));
        }
    }
}
