# 17 — Danh mục cấu hình và môi trường

> Cập nhật 2026-10-18. CURRENT từ appsettings/options/bootstrap; profile Supabase User Secrets local đã kiểm read-only theo worklog, không suy cấu hình hosting từ kết quả local.

## 1. Nguồn và độ ưu tiên

ASP.NET nạp appsettings, file theo environment, user-secrets trong Development, env rồi CLI theo host mặc định. __ ánh xạ dấu hai chấm; backend không tự nạp .env. Vite đọc .env lúc dev/build; VITE_* xuất được vào trình duyệt nên không chứa secret. [08](08-deploy-detail.md) quản lý thao tác kết nối/troubleshooting, file này là registry tên biến.

Development Supabase dùng cùng quy ước NTC: User Secrets + keyword/value + Session pooler 5432 + VerifyFull/CA/pool 10; thao tác và giới hạn ở [10 §3.1](10-setup-local.md#31-dùng-supabase-cùng-cách-với-ntc). Marixa local trỏ cloud tắt DatabaseInitialization/Seed bằng User Secrets, giữ nguyên secrets khác. Npgsql vẫn hỗ trợ URI cũ; không cần đổi normalizer để dùng mẫu chung.

| Biến chuẩn | Mặc định / kiểm tra | Phạm vi |
| --- | --- | --- |
| ASPNETCORE_ENVIRONMENT | Development/Production; test host dùng Testing | API |
| DATABASE__PROVIDER | SqlServer; chỉ nhận SqlServer/PostgreSQL | API |
| CONNECTIONSTRINGS__MYCNN | LocalDB trong appsettings; cloud phải override đúng DB/TLS | API secret |
| JWTSETTINGS__KEY | Production bắt buộc secret ngẫu nhiên riêng >=32 byte UTF-8, không nhận key mẫu; Blueprint sync:false yêu cầu giữ secret hợp lệ hiện hữu | API secret |
| JWTSETTINGS__ISSUER / AUDIENCE | API / APIUser; không trống | API |
| JWTSETTINGS__EXPIRATIONMINUTES | 60;1–10080 | API |
| JWTSETTINGS__REMEMBERMEEXPIRATIONMINUTES | 10080; >=thời hạn thường, <=10080 | API |
| CORS__ALLOWEDORIGINS__0, __1 | appsettings localhost:3000; thêm origin frontend chính xác | API |
| RATELIMITING__AUTHREQUESTSPERMINUTE | 20;5–1000, fixed window 1 phút, không queue | API |
| DATABASEINITIALIZATION__ENABLED | Development=true; Production/Testing=false khi không đặt | API; explicit DDL |
| SEED__ENABLED | Development=true; Production=false khi không đặt | API; role/rule |
| SEED__ADMIN__ENABLED / PASSWORD / USERNAME / EMAIL | Admin mặc định không tạo; cần password riêng | API secret |
| SEED__SAMPLE__ENABLED / PASSWORD | Mặc định không tạo; sample chỉ Development | API secret |
| SUPABASE__URL / SUPABASE__SERVICEKEY | Production Storage:Provider=Supabase bắt buộc URL/key; Development thiếu cấu hình dùng local adapter; upload lỗi trả 502 | API; key secret |
| SUPABASE__SERVICE_KEY | Tên Render thống nhất mới; ServiceKey vẫn tương thích trong code | API secret |
| ASPNETCORE_URLS / PORT | launch profile local; Docker nghe PORT, fallback 10000 | API |
| VITE_API_BASE_URL | URL API có /api; ghi lúc build | Web public |
| API_PROXY_TARGET | Dev proxy (VITE_API_PROXY_TARGET alias cũ) mặc định http://localhost:57432 | Web dev |
| MARIXA_TEST_POSTGRES | Chỉ loopback, test tự tạo/drop DB Marixa_Verify_GUID | Test secret tạm |
| MARIXA_ARTIFACT_DIR | Không đặt thì test không ghi sample/inventory | Test |

## 2. Môi trường và xác minh

Local: SQL Server LocalDB hoặc PostgreSQL riêng, không lấy credential production. Test: SQLite per-host và provider fixture với dữ liệu giả. CI: PostgreSQL container riêng; Linux skip LocalDB. Production: init/seed tắt, storage bền, schema được nâng trước app. Hosting snapshot chỉ ở [00](00-README.md); staging riêng CHƯA XÁC ĐỊNH.

Startup validate key/issuer/audience/expiry/rate option và allowlist provider. URI Postgres giữ SSL/query options hỗ trợ, lỗi option lạ fail rõ; không tắt certificate validation để chữa kết nối. Connection string không được in ra log. Chưa có secret rotation scheduler hoặc cấu hình reload nghiệp vụ trực tiếp; restart/redeploy có kiểm soát khi đổi cấu hình, không ghi mật khẩu vào lệnh được lưu lịch sử.

Frontend kiểm thử cô lập phải override cả VITE_API_BASE_URL=/api và API_PROXY_TARGET trỏ API local có DB giả. Không chạy giao diện thử với biến môi trường cũ đang trỏ Render/live. Ghi nhớ đăng nhập chỉ đổi nơi lưu phiên và thời hạn login, không cần biến env mới.

## 3. Chuẩn hóa cấu hình cùng NTC — 2026-10-18

Theo [CONFIGURATION-STANDARD](CONFIGURATION-STANDARD.md): production JSON sạch chỉ giữ defaults, publish/Docker không mang base/Development/local appsettings. Render sáu biến nền, job=true và hai Storage keys; manifest deploy/project.json lưu target public, không chứa secret. --check-config và scripts/Test-Configuration.ps1 kiểm tĩnh rồi thoát trước initializer/worker. Error code CONFIGURATION_INVALID chỉ ra key thiếu/sai; Data Protection thiếu nơi lưu bền có cảnh báo riêng.

BackgroundJobs:Enabled=true trong Production giữ job ngày hiệu lực hiện hữu; Testing không chạy worker. Swagger Production mặc định false; chỉ bật khi có chủ đích. DatabaseInitialization/Seed Production phải false. JWT API/APIUser/60/remember10080 giữ nguyên, key cloud hợp lệ không bị xoay. Không dùng env Testing để chạy thật.

Storage:Provider=Supabase trong Production kiểm URL/key để tránh âm thầm lưu ảnh lên đĩa container khi cấu hình thiếu. SUPABASE__SERVICE_KEY cũ vẫn hoạt động kể cả canonical ServiceKey trong JSON rỗng. Nếu có chủ đích chỉ dùng storage local bền thì override Storage:Provider=Local và cấu hình volume riêng; không suy container có lưu bền sẵn. Tài liệu HR private App_Data chưa được chuyển sang Supabase bởi đợt cấu hình này.

## Quyết định nguồn cấu hình bổ sung — 2026-10-18

Issuer/audience/TTL và Swagger/seed chỉ khai báo trong Production JSON theo chuẩn bảo trì; Render không giữ bản lặp (chỉ bỏ JWT env sau khi image mới có JSON đã rollout). BackgroundJobs:Enabled=true trong JSON và BACKGROUNDJOBS__ENABLED=true trong mẫu env cho cả hai. Storage dùng tên Render SUPABASE__URL/SUPABASE__SERVICE_KEY, giữ tương thích ServiceKey nội bộ; NTC chuẩn bị credential cho ảnh note tương lai, chưa bật endpoint/adapter upload. Xem [chuẩn chung](CONFIGURATION-STANDARD.md) để phân biệt cấu hình dự phòng với tính năng đang có. Không tạo env rỗng.

## Dashboard Marixa đã đối chiếu — 2026-10-18

Render đã lưu chín keys, job=true và MyCnn keyword VerifyFull/CA Linux đúng project bằng Save only, giữ password cloud (khác profile local), JWT/Storage key. Vercel API URL Config/Production giữ đúng /api, framework đổi Vite và bỏ dashboard Build/Output overrides; root marixa_react/Node24.x đã đúng. Source mới có CA/Production JSON chưa deploy; không redeploy image cũ thiếu CA. Chi tiết/bằng chứng ở [worklog](worklog.md); việc lưu env không xác minh credential/DB/schema runtime.
