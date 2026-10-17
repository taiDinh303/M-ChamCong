# 08 — Triển khai và xử lý cấu hình

> Cập nhật 2026-10-17. CURRENT theo source; dashboard/credential/schema live NOT VERIFIED. Registry env tại 17, gate release 18, nâng schema 23.

## 1. Đường chạy

ASP.NET Core 8 M.API trong Docker Render, React/Vite static trên Vercel, PostgreSQL/ảnh public Supabase; Identity JWT do .NET quản lý. [Dockerfile](../marixa_aspnet/Dockerfile) build/publish bằng SDK 8, runtime user app, nghe PORT mặc định 10000. [render.yaml](../render.yaml) health/ready, autoDeploy=true, plan standard; khác snapshot plan Free do chủ dự án cung cấp tại 00, phải đối chiếu dashboard/cost trước áp blueprint.

## 2. Hợp đồng biến môi trường

Nguồn tên/default/secret ở [17-environment-config](17-environment-config.md). Backend không tự nạp .env. Env __ ghi đè appsettings; không đưa server credential vào VITE_*. Production phải override JWT key riêng, DB provider/connection/CORS đúng đích; init/seed mặc định tắt. Admin/sample opt-in password riêng, sample bị cấm ngoài Development.

## 3. Database và startup

DatabaseConfig chỉ nhận SqlServer/PostgreSQL. URI Postgres được normalize, giữ SSL Mode và options hỗ trợ, option lạ fail rõ; keyword/value giữ cấu hình Npgsql. Lấy host/user/port từ đúng project, không đoán theo region. [Supabase connection guide](https://supabase.com/docs/guides/database/connecting-to-postgres) và [Microsoft Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0).

Production mặc định không DDL/seed. Explicit init: SQL Server MigrateAsync; PostgreSQL tạo bảng, áp additive scripts đã review, thêm nullable column thiếu theo type EF, rồi constraints/index. Cột bắt buộc thiếu fail để yêu cầu backfill/migration; không xóa DB để chữa schema. Lỗi quan trọng fail startup; readiness 503 khi query/schema cần thiết thiếu. Swagger/live 200 không chứng minh DB sẵn sàng. Rule seed chỉ tạo khi thiếu, không ghi đè giờ cấu hình.

Hai release migration mới phải preflight dữ liệu/backup/script đúng provider theo 23. T-SQL EF khác SQL Next.js dưới supabase; script legacy mặc định bị chặn. Không dùng init/seed như công cụ rollback hoặc chữa dữ liệu lịch sử.

## 4. File và Storage

SupabasePhotoStorage gửi apikey; legacy JWT thêm Bearer, sb_secret_* không dùng Bearer, theo [API keys](https://supabase.com/docs/guides/getting-started/api-keys). ServiceKey là tên chuẩn, Service_Key là alias cũ. Thiếu cấu hình chọn local photo adapter; đã config nhưng upload lỗi trả 502, không fallback âm thầm. File sai/size/signature trả 400; ảnh tối đa 5 MiB. URL Storage public hoặc /uploads/attendance theo adapter. [Bucket access](https://supabase.com/docs/guides/storage/buckets/fundamentals).

Report/handover dùng private App_Data, có quyền khi tải; legacy handover ngoài đó chỉ đọc qua API, đường public bị chặn. Docker tạo folder và quyền user app nhưng chưa có persistent volume trong blueprint: phải bổ sung storage bền hoặc adapter private phù hợp trước dùng thật. Backup DB không chứa binary ảnh/tài liệu, xem 22.

## 5. Kiểm tra sau rollout và sự cố

Ready 200, login/checkin/duyệt/OT/cancel/period/export/file quyền bằng dữ liệu được phép; kiểm timestamps VN, schema defaults, hash version cũ, pending job AppliedAt và logs/correlation. CORS origin chính xác, Auth 429 có Retry-After; sau proxy cần xác minh ingress trước tin forwarded IP. Token cũ có thể cần đăng nhập lại khi key/stamp/role/link thay đổi.

Thủ tục rollback/readiness/upload/download/job ở [21](21-operations-runbook.md); restore 22. Backend local test/publish không chứng minh cấu hình live, volume hoặc deployment đã thành công.
