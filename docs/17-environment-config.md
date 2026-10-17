# 17 — Danh mục cấu hình và môi trường

> Cập nhật 2026-10-17. CURRENT từ appsettings/options/bootstrap; giá trị secret/live: NOT VERIFIED.

## 1. Nguồn và độ ưu tiên

ASP.NET nạp appsettings, file theo environment, user-secrets trong Development, env rồi CLI theo host mặc định. __ ánh xạ dấu hai chấm; backend không tự nạp .env. Vite đọc .env lúc dev/build; VITE_* xuất được vào trình duyệt nên không chứa secret. [08](08-deploy-detail.md) quản lý thao tác kết nối/troubleshooting, file này là registry tên biến.

| Biến chuẩn | Mặc định / kiểm tra | Phạm vi |
| --- | --- | --- |
| ASPNETCORE_ENVIRONMENT | Development/Production; test host dùng Testing | API |
| DATABASE__PROVIDER | SqlServer; chỉ nhận SqlServer/PostgreSQL | API |
| CONNECTIONSTRINGS__MYCNN | LocalDB trong appsettings; cloud phải override đúng DB/TLS | API secret |
| JWTSETTINGS__KEY | Local-development marker; Production phải thay, tối thiểu 32 byte | API secret |
| JWTSETTINGS__ISSUER / AUDIENCE | API / APIUser; không trống | API |
| JWTSETTINGS__EXPIRATIONMINUTES | 60;1–10080 | API |
| JWTSETTINGS__REMEMBERMEEXPIRATIONMINUTES | 10080; >=thời hạn thường, <=10080 | API |
| CORS__ALLOWEDORIGINS__0, __1 | appsettings localhost:3000; thêm origin frontend chính xác | API |
| RATELIMITING__AUTHREQUESTSPERMINUTE | 20;5–1000, fixed window 1 phút, không queue | API |
| DATABASEINITIALIZATION__ENABLED | Development=true; Production/Testing=false khi không đặt | API; explicit DDL |
| SEED__ENABLED | Development=true; Production=false khi không đặt | API; role/rule |
| SEED__ADMIN__ENABLED / PASSWORD / USERNAME / EMAIL | Admin mặc định không tạo; cần password riêng | API secret |
| SEED__SAMPLE__ENABLED / PASSWORD | Mặc định không tạo; sample chỉ Development | API secret |
| SUPABASE__URL / SUPABASE__SERVICEKEY | Thiếu cấu hình dùng local photo adapter; đã config mà lỗi thì trả 502 | API; key secret |
| SUPABASE__SERVICE_KEY | Alias tương thích cũ; cấu hình mới dùng SERVICEKEY | API secret |
| ASPNETCORE_URLS / PORT | launch profile local; Docker nghe PORT, fallback 10000 | API |
| VITE_API_BASE_URL | URL API có /api; ghi lúc build | Web public |
| VITE_API_PROXY_TARGET | Dev proxy mặc định http://localhost:57432 | Web dev |
| MARIXA_TEST_POSTGRES | Chỉ loopback, test tự tạo/drop DB Marixa_Verify_GUID | Test secret tạm |
| MARIXA_ARTIFACT_DIR | Không đặt thì test không ghi sample/inventory | Test |

## 2. Môi trường và xác minh

Local: SQL Server LocalDB hoặc PostgreSQL riêng, không lấy credential production. Test: SQLite per-host và provider fixture với dữ liệu giả. CI: PostgreSQL container riêng; Linux skip LocalDB. Production: init/seed tắt, storage bền, schema được nâng trước app. Hosting snapshot chỉ ở [00](00-README.md); staging riêng CHƯA XÁC ĐỊNH.

Startup validate key/issuer/audience/expiry/rate option và allowlist provider. URI Postgres giữ SSL/query options hỗ trợ, lỗi option lạ fail rõ; không tắt certificate validation để chữa kết nối. Connection string không được in ra log. Chưa có secret rotation scheduler hoặc cấu hình reload nghiệp vụ trực tiếp; restart/redeploy có kiểm soát khi đổi cấu hình, không ghi mật khẩu vào lệnh được lưu lịch sử.
