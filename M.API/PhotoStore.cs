using Microsoft.AspNetCore.Hosting;

namespace M.API
{
    /// <summary>
    /// Thư mục lưu ảnh chấm công, DUY NHẤT 1 nơi: &lt;webroot&gt;/uploads/attendance/{checkin|checkout}
    ///
    /// Neo vào WebRootPath (= wwwroot/ nằm NGAY trong thư mục site), không đi tìm .csproj
    /// (thư mục dev). Nguyên nhân: dev-walk-up (.csproj) khi chạy trên IIS/Somee sẽ đi lên
    /// thư mục cha (d:\DZHosts\LocalUser) mà IIS KHÔNG có quyền đọc -> UnauthorizedAccessException
    /// crash boot -> 500.30. WebRootPath luôn ở trong site (IIS có quyền) nên an toàn dev lẫn prod.
    /// </summary>
    public static class PhotoStore
    {
        /// <summary>
        /// Web root của app (= wwwroot/). Dev (dotnet run/VS) và prod (IIS) đều cùng wwwroot
        /// trong thư mục app, nên kết quả tương đương.
        /// </summary>
        public static string GetProjectRoot(IWebHostEnvironment env)
        {
            return string.IsNullOrWhiteSpace(env.WebRootPath)
                ? Path.Combine(env.ContentRootPath, "wwwroot")
                : env.WebRootPath;
        }

        public static string GetRoot(IWebHostEnvironment env)
        {
            string webRoot = GetProjectRoot(env);
            string uploadRoot = Path.Combine(webRoot, "uploads");
            // BOC try/catch: khong duoc giat app khi boot neu chua co quyen ghi.
            // (Chuc nang upload hinh se goi GetAttendanceFolder de tao thu muc khi can.)
            try { Directory.CreateDirectory(uploadRoot); }
            catch { /* IIS se tao thu muc khi upload thuc su; loi quyen chi an huan upload, khong crash boot */ }
            return uploadRoot;
        }

        /// <summary>
        /// Thư mục ảnh chấm công theo loại: vào ca (checkin) / ra ca (checkout).
        /// </summary>
        public static string GetAttendanceFolder(
            IWebHostEnvironment env,
            string type = "checkin")
        {
            string kind = type == "checkout" ? "checkout" : "checkin";
            string dir = Path.Combine(GetRoot(env), "attendance", kind);
            try { Directory.CreateDirectory(dir); }
            catch { /* bi quyen: boi duoc thong bao tai cap upload */ }
            return dir;
        }
    }
}
