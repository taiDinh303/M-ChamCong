using M.Contract.Repositories.Entity;
using M.Repositories.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace M.API.Controllers
{
    /// <summary>
    /// Endpoint chẩn đoán tạm thời khi deploy Somee (branch web_publish).
    /// GET /api/diagnostics -> tra: environment, CSDL, thư mục uploads, key JWT.
    /// Xóa file này sau khi web chạy ổn.
    /// </summary>
    [ApiController]
    [Route("api/diagnostics")]
    [AllowAnonymous]
    public class DiagnosticsController : ControllerBase
    {
        private readonly DatabaseContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public DiagnosticsController(DatabaseContext db, IWebHostEnvironment env, IConfiguration config)
        {
            _db = db;
            _env = env;
            _config = config;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var result = new Dictionary<string, object>
            {
                ["environment"] = _env.EnvironmentName,
                ["contentRoot"] = _env.ContentRootPath,
                ["webRoot"] = _env.WebRootPath,
                ["MyCnn_Connection"] = _db.Database.GetConnectionString() ?? "(null)",
                ["jwtKeyPresent"] = !string.IsNullOrEmpty(_config["JwtSettings:Key"]),
                ["uploads"] = Diagnostics.UploadsWritable(_env)
            };

            // CSDL: thử 1 query nhẹ
            try
            {
                int users = _db.ApplicationUser.Count();
                result["db"] = $"OK, AspNetUsers={users}";
            }
            catch (Exception ex)
            {
                result["db"] = "FAIL: " + ex.Message;
            }
            return Ok(result);
        }
    }

    public static class Diagnostics
    {
        public static string UploadsWritable(IWebHostEnvironment env)
        {
            try
            {
                var root = M.API.PhotoStore.GetRoot(env);
                var probe = System.IO.Path.Combine(root, ".write-probe.txt");
                System.IO.File.WriteAllText(probe, "probe");
                System.IO.File.Delete(probe);
                return $"OK, root={root}";
            }
            catch (Exception ex)
            {
                return "FAIL: " + ex.Message;
            }
        }
    }
}
