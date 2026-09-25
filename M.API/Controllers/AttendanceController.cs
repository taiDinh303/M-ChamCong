using M.Contract.Serivces.Interface;
using M.Core.Base;
using M.Core.Store;
using Microsoft.AspNetCore.Mvc;
using ModelViews.AttendanceModelView;

namespace M.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        /// <summary>
        /// Retrieves all attendances with pagination
        /// </summary>
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(
            int pageNumber = 1,
            int pageSize = 5)
        {
            BasePaginatedList<AttendanceResponseModelView> result =
                await _attendanceService.GetAllAsync(pageNumber, pageSize);

            return Ok(new BaseResponse<BasePaginatedList<AttendanceResponseModelView>>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result
            ));
        }

        /// <summary>
        /// Retrieves attendance by ID
        /// </summary>
        [HttpGet("get-by-id/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            AttendanceResponseModelView result =
                await _attendanceService.GetByIdAsync(id);

            return Ok(new BaseResponse<AttendanceResponseModelView>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result
            ));
        }

        /// <summary>
        /// Creates a new attendance
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create(
            [FromBody] CreateAttendanceModelView model)
        {
            await _attendanceService.CreateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Attendance created successfully!"
            ));
        }

        /// <summary>
        /// Updates attendance information
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> Update(
            [FromBody] UpdateAttendanceModelView model)
        {
            await _attendanceService.UpdateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Attendance updated successfully!"
            ));
        }

        /// <summary>
        /// Approves / rejects an attendance record (duyệt ngày công)
        /// </summary>
        [HttpPost("approve")]
        public async Task<IActionResult> Approve(
            [FromBody] ApproveAttendanceModelView model)
        {
            await _attendanceService.ApproveAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Attendance approval status updated successfully!"
            ));
        }

        /// <summary>
        /// Soft deletes attendance by ID
        /// </summary>
        [HttpDelete("soft-delete/{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            await _attendanceService.SoftDeleteAsync(id);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Attendance deleted successfully!"
            ));
        }

        /// <summary>
        /// Permanently deletes attendance by ID
        /// </summary>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _attendanceService.DeleteAsync(id);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Attendance permanently deleted successfully!"
            ));
        }
    }
}
