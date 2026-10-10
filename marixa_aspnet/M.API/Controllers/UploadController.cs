using M.Core.Base;
using M.Core.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;

namespace M.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly SupabaseStorageService _supabase;
        private readonly ILogger<UploadController> _logger;

        private static readonly string[] AllowedExtensions =
            [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxBytes = 5 * 1024 * 1024; // 5MB

        public UploadController(
            IWebHostEnvironment env,
            SupabaseStorageService supabase,
            ILogger<UploadController> logger)
        {
            _env = env;
            _supabase = supabase;
            _logger = logger;
        }

        /// <summary>
        /// Upload ảnh chấm công (vào ca / ra ca).
        /// type: "checkin" (vào ca) hoặc "checkout" (ra ca).
        ///
        /// Nếu đã cấu hình SUPABASE__URL + SUPABASE__SERVICE_KEY:
        ///   Upload lên Supabase Storage → trả URL absolute public
        ///   https://<ref>.supabase.co/storage/v1/object/public/marixa-photos/...
        ///   Ảnh bền, không mất khi redeploy Render.
        ///
        /// Nếu chưa cấu hình (dev local):
        ///   Fallback lưu wwwroot/uploads (PhotoStore) → trả path tương đối /uploads/...
        /// </summary>
        [HttpPost("photo")]
        public async Task<IActionResult> UploadPhoto(
            IFormFile file,
            string type = "checkin")
        {
            if (file == null || file.Length == 0)
            {
                return Ok(new BaseResponse<string>(
                    StatusCodeHelper.BadRequest,
                    ResponseCodeConstants.BAD_REQUEST,
                    "Vui lòng chọn file ảnh."));
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension) ||
                file.Length > MaxBytes)
            {
                return Ok(new BaseResponse<string>(
                    StatusCodeHelper.BadRequest,
                    ResponseCodeConstants.BAD_REQUEST,
                    "Chỉ chấp nhận jpg/png/webp, tối đa 5MB."));
            }

            string kind   = type == "checkout" ? "checkout" : "checkin";
            string folder = DateTime.Now.ToString("yyyyMM");

            try
            {
                if (_supabase.IsConfigured)
                {
                    // PROD (Render + Supabase): ảnh lưu bền
                    string publicUrl = await _supabase.UploadPhotoAsync(file, kind, folder);
                    return Ok(BaseResponse<string>.OkResponse(publicUrl, null));
                }

                // DEV local: fallback đĩa
                string fileName = $"{Guid.NewGuid()}{extension}";
                string absoluteDir = Path.Combine(
                    PhotoStore.GetAttendanceFolder(_env, kind), folder);
                Directory.CreateDirectory(absoluteDir);
                string absolutePath = Path.Combine(absoluteDir, fileName);
                await using (var stream = new FileStream(absolutePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                string url = $"/uploads/attendance/{kind}/{folder}/{fileName}";
                return Ok(BaseResponse<string>.OkResponse(url, null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload photo failed");
                return Ok(new BaseResponse<string>(
                    StatusCodeHelper.ServerError,
                    "UPLOAD_FAILED",
                    "Không thể lưu ảnh: " + ex.Message));
            }
        }
    }
}
