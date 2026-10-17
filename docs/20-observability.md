# 20 — Quan sát và phát hiện lỗi

> Cập nhật 2026-10-17. CURRENT: structured logs/health/correlation; chưa có dashboard metrics hoặc SLO đã nghiệm thu.

RequestTelemetryMiddleware nhận X-Correlation-ID tối đa 64 ký tự ASCII an toàn, nếu sai tạo GUID; trả cùng header và log method/endpoint/status/elapsedMs trong scope. Không log query/body/token/file nhân viên. ExceptionMiddleware ghi lỗi server và trả traceId; lỗi validation/auth cũ có thể chỉ có header. Correlation do client gửi chỉ dùng truy tìm, không là bằng chứng danh tính.

| Tín hiệu | Cách đọc / hành động |
| --- | --- |
| /health/live | Process phản hồi; không query DB. |
| /health/ready | Query employee, period và columns mới; lỗi trả 503, không lộ connection string. Không thay full migration verification. |
| 5 xx, latency tăng, 503 | Đối chiếu correlation/time/deployment, DB/schema/pool trước; theo 21. |
| 401/403 tăng | Key/expiry/stamp/role/employee link hoặc quyền; không bỏ auth để chữa lỗi. |
| 429 tăng | Fixed window Auth theo peer IP; sau proxy có thể nhiều người dùng chung bucket. |
| Effective-date job error | ID thay đổi và kind; record còn AppliedAt=null để retry; kiểm last admin/position/account/schema. |
| Private document cleanup warning | DB/file cần đối soát; không xóa hàng loạt theo log. |

Vận hành cần theo dõi CPU/RAM/disk/App_Data/DBconnections từ platform, lỗi upload và số approved-due chưa AppliedAt. Chưa có exporter OpenTelemetry/APM, alert routing hay người trực được xác nhận. Đề xuất alert khi readiness fail liên tục, job due quá 5 phút lúc app đang chạy hoặc 5 xx tăng; đây là PROPOSED, phải đặt ngưỡng từ baseline và gắn người xử lý trước khi gọi là đã giám sát.

Không dùng tên nhân viên/ID employee làm metric label. Log retention và quyền dashboard CHƯA XÁC ĐỊNH; xem [16](16-security-privacy.md), [21](21-operations-runbook.md), [25](25-performance-capacity.md).
