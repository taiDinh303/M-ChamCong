# 08 — Triển khai và xử lý cấu hình

> Cập nhật 2026-10-17. CURRENT theo source; dashboard/credential/schema live NOT VERIFIED. Registry env tại 17, gate release 18, nâng schema 23.

## 1. Đường chạy

ASP.NET Core 8 M.API trong Docker Render, React/Vite static trên Vercel, PostgreSQL/ảnh public Supabase; Identity JWT do .NET quản lý. [Dockerfile](../marixa_aspnet/Dockerfile) build/publish bằng SDK 8, runtime user app, nghe PORT mặc định 10000. [render.yaml](../render.yaml) health/ready, autoDeploy=true, plan standard; khác snapshot plan Free do chủ dự án cung cấp tại 00, phải đối chiếu dashboard/cost trước áp blueprint.

## 2. Hợp đồng biến môi trường

Nguồn tên/default/secret ở [17-environment-config](17-environment-config.md). Backend không tự nạp .env. Env __ ghi đè appsettings; không đưa server credential vào VITE_*. Production phải override JWT key riêng, DB provider/connection/CORS đúng đích; init/seed mặc định tắt. Admin/sample opt-in password riêng, sample bị cấm ngoài Development.

### 2.1 Render build xong nhưng ứng dụng thoát 134 vì JWT key

Nếu log đã có `exporting to image`/`Deploying...`, sau đó `OptionsValidationException` về signing key thì Docker build đã qua; lỗi nằm ở cấu hình khởi động. Log chủ dự án cung cấp ngày 17/10/2026 cho commit `2555a25` thuộc trường hợp này. Cảnh báo DataProtection về đĩa tạm là vấn đề lưu khóa riêng, không phải nguyên nhân exception JWT này.

Ảnh Environment do chủ dự án bổ sung ngày 17/10 xác nhận biến JWT đang giữ key mẫu có `CHANGE_ME`. Đủ 32 ký tự chưa đủ điều kiện: Production còn từ chối marker mẫu/development. Đây là bằng chứng cấu hình tại thời điểm chụp, chưa xác nhận đã sửa hoặc redeploy thành công.

1. Mở đúng service hiện hữu trong Render → **Environment**. Thêm hoặc sửa biến **`JWTSETTINGS__KEY`** (hai dấu gạch dưới). `JWT_SECRET`, `JWT__KEY` và `JwtSettings:Key` không phải tên env được hướng dẫn cho service này. Không dán key vào chat, Git hoặc biến frontend.
2. Dùng secret ngẫu nhiên riêng, tối thiểu 32 byte UTF-8; nên tạo từ 32 byte ngẫu nhiên rồi encode Base64. Không dùng các key mẫu `CHANGE_ME`/`LOCAL-DEVELOPMENT`. Nếu service đã có key hợp lệ dưới tên biến sai, sửa tên và giữ giá trị để tránh xoay key không cần thiết. Nếu thay key, các phiên dùng key cũ cần đăng nhập lại.
3. Chọn **Save and deploy** để dùng image hiện có với env mới; hoặc **Save, rebuild, and deploy** khi cần build lại. Chỉ **Save only** chưa áp biến cho tiến trình đang chạy. Xem [Render environment variables](https://render.com/docs/configure-environment-variables).
4. Xác nhận hết `OptionsValidationException`, ứng dụng bắt đầu nghe cổng, rồi `/health/live` và `/health/ready` đều 200. Nếu readiness 503, kiểm DB/schema riêng theo [23](23-data-migration.md); không bật init/seed để che lỗi hoặc tự áp migration lên DB live.

Có thể tạo key trên máy Windows bằng PowerShell dưới đây; lệnh chỉ đưa giá trị vào clipboard để dán trực tiếp vào ô Value của Render, không in secret ra terminal:

```powershell
$marixaKeyBytes = New-Object byte[] 32
$marixaRng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $marixaRng.GetBytes($marixaKeyBytes)
    [Convert]::ToBase64String($marixaKeyBytes) | Set-Clipboard
} finally { $marixaRng.Dispose() }
```

Blueprint hiện khai báo `generateValue: true` cho key: Render tạo secret nếu biến chưa tồn tại và giữ giá trị hiện hữu theo [Blueprint reference](https://render.com/docs/blueprint-spec#generating-random-secrets). Cấu hình này chỉ có hiệu lực khi service được quản lý/sync bằng Blueprint; push mã không tự thêm env cho service tạo thủ công. Không tạo/sync Blueprint mới chỉ để chữa service hiện tại: tên service/plan trong repo cần được đối chiếu trước.

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
