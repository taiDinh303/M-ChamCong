using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace M.API
{
    /// <summary>
    /// Upload ảnh chấm công lên Supabase Storage (free 1 GB, không phụ thuộc
    /// filesystem ephemeral của Render container).
    ///
    /// Cấu hình qua 2 env var (Render Dashboard):
    ///   SUPABASE__URL         = https://zphqfcwrwitrqdeueonu.supabase.co
    ///   SUPABASE__SERVICE_KEY = (secret, đặt trực tiếp trong Render)
    ///
    /// Nếu không cấu hình (dev local / chưa set) → IsConfigured = false,
    /// UploadController fallback lưu đĩa qua PhotoStore như trước.
    /// </summary>
    public class SupabaseStorageService
    {
        // Bucket public, tạo sẵn trong Supabase Dashboard
        public const string Bucket = "marixa-photos";

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
        private readonly string _baseUrl;
        private readonly string _serviceKey;

        public bool IsConfigured { get; }

        public SupabaseStorageService(IConfiguration configuration)
        {
            _baseUrl    = (configuration["Supabase:Url"] ?? "").TrimEnd('/');
            _serviceKey = configuration["Supabase:ServiceKey"] ?? "";
            IsConfigured = !string.IsNullOrWhiteSpace(_baseUrl)
                       && !string.IsNullOrWhiteSpace(_serviceKey);
        }

        /// <summary>
        /// Upload 1 file ảnh lên Supabase Storage.
        /// Đường dẫn object: attendance/{kind}/{yyyyMM}/{guid}{ext}
        /// Trả về URL public: https://<ref>.supabase.co/storage/v1/object/public/marixa-photos/...
        /// </summary>
        public async Task<string> UploadPhotoAsync(IFormFile file, string kind, string folder)
        {
            if (!IsConfigured)
                throw new InvalidOperationException(
                    "Supabase storage not configured (SUPABASE__URL / SUPABASE__SERVICE_KEY).");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string fileName  = $"{Guid.NewGuid()}{extension}";
            string objectPath = $"{kind}/{folder}/{fileName}";

            // Đọc file vào memory (ảnh ≤ 5 MB, an toàn)
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            byte[] bytes = ms.ToArray();

            string uploadUrl = $"{_baseUrl}/storage/v1/object/{Bucket}/{objectPath}";

            using var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
            {
                Content = content
            };
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _serviceKey);

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Supabase upload failed [{(int)response.StatusCode}]: {body}");
            }

            // URL public (không cần auth khi đọc)
            return $"{_baseUrl}/storage/v1/object/public/{Bucket}/{objectPath}";
        }
    }
}
