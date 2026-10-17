# 24 — Tích hợp ngoài ứng dụng

> Cập nhật 2026-10-17. CURRENT theo adapter/config; tài khoản và kết nối cloud thật: NOT VERIFIED.

| Tích hợp | Mục đích / ranh giới | Lỗi và kiểm chứng |
| --- | --- | --- |
| SQL Server / Npgsql PostgreSQL | EF persistence; URI được normalize, giữ TLS options hỗ trợ | Readiness 503, constraint/deadlock 409; runtime fixture dùng DB giả riêng |
| Supabase Storage HTTP | Chỉ ảnh public marixa-photos; credential server | apikey mọi loại; JWT legacy thêm Bearer, sb_secret_* không dùng Bearer; upload lỗi 502, không fallback âm thầm |
| Render Docker | Chạy API, ingress TLS/PORT | App nghe HTTP nội bộ; volume private chưa xác minh |
| Vercel static | React/Vite, không chạy backend .NET tại đây | API URL/CORS đúng build; frontend deployment chưa thực hiện |
| Figma | Quy trình Dev Mode/UI Kit trong 13 | Scaffold không là bằng chứng đã đồng bộ live |

Không có payment/email/SMS/HRIS/AI vendor/realtime service trong luồng được xác nhận. Code UserLogin không chứng minh Google OAuth production đã được cấu hình. Không thêm webhook hoặc job gọi bên ngoài khi chưa có yêu cầu.

Supabase dùng DB/Storage, Identity vẫn của .NET. Key đặc quyền không đưa VITE_*. Đã đối chiếu [API keys](https://supabase.com/docs/guides/getting-started/api-keys) và [changelog](https://supabase.com/changelog); cần theo dõi thay đổi TLS/CBC theo [thông báo chính thức](https://supabase.com/changelog/cbc-tls-ciphers-deprecated) trước rollout. Không nâng SDK/đổi gói chỉ vì thấy thông báo không liên quan; adapter này dùng HttpClient .NET 8.

Timeout/cancellation truyền xuống photo storage/private stream; nhiều CRUD contract cũ chưa có CancellationToken. Chưa có retry queue/outbox/circuit breaker tổng quát; chỉ retry thao tác idempotent đã xác định, không upload lại hoặc duyệt lại mù. Contract/mock HTTP tests xác minh headers và lỗi, không thay upload thật vào bucket được phép.
