using M.Contract.Serivces.Interface;
using M.Core.Base;
using M.Core.Store;
using Microsoft.AspNetCore.Mvc;
using ModelViews.AttendanceLogModelView;

namespace M.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class AttendanceLogController : ControllerBase
    {
        private readonly IAttendanceLogService _attendanceLogService;

        public AttendanceLogController(
            IAttendanceLogService attendanceLogService)
        {
            _attendanceLogService = attendanceLogService;
        }

        /// <summary>
        /// Retrieves all attendance logs with pagination
        /// </summary>
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(
            int pageNumber = 1,
            int pageSize = 5)
        {
            BasePaginatedList<AttendanceLogResponseModelView> result =
                await _attendanceLogService.GetAllAsync(
                    pageNumber,
                    pageSize);

            return Ok(new BaseResponse<BasePaginatedList<AttendanceLogResponseModelView>>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result
            ));
        }

        /// <summary>
        /// Retrieves attendance log by ID
        /// </summary>
        [HttpGet("get-by-id/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            AttendanceLogResponseModelView result =
                await _attendanceLogService.GetByIdAsync(id);

            return Ok(new BaseResponse<AttendanceLogResponseModelView>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: result
            ));
        }

        /// <summary>
        /// Creates a new attendance log
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create(
            [FromBody] CreateAttendanceLogModelView model)
        {
            await _attendanceLogService.CreateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Attendance log created successfully!"
            ));
        }

        /// <summary>
        /// Updates attendance log information
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> Update(
            [FromBody] UpdateAttendanceLogModelView model)
        {
            await _attendanceLogService.UpdateAsync(model);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Attendance log updated successfully!"
            ));
        }

        /// <summary>
        /// Soft deletes attendance log by ID
        /// </summary>
        [HttpDelete("soft-delete/{id}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            await _attendanceLogService.SoftDeleteAsync(id);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Attendance log deleted successfully!"
            ));
        }

        /// <summary>
        /// Permanently deletes attendance log by ID
        /// </summary>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _attendanceLogService.DeleteAsync(id);

            return Ok(new BaseResponse<string>(
                statusCode: StatusCodeHelper.OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Attendance log permanently deleted successfully!"
            ));
        }
    }
}
