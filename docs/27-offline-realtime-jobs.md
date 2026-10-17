# 27 — Offline và tác vụ tự động

> Cập nhật 2026-10-17. CURRENT: job ngày hiệu lực; offline/realtime/cấp phép/retention chưa triển khai.

## 1. Job ngày hiệu lực

EmploymentChangeWorker chạy khi app hoạt động, sweep ngay startup rồi mỗi 1 phút theo TimeProvider; Testing không đăng ký worker. Processor tìm Approved chưa AppliedAt, promotion đến ngày VN hoặc handover sau LastWorkingDate. Mỗi item transaction Serializable, kiểm lại trước áp; AppliedAt làm dấu hoàn tất, lỗi để retry và log ID/kind. Một item lỗi không chặn các item khác; mỗi tick tối đa 100 mỗi loại.

Promotion cập nhật chức vụ/mã/role rồi stamp; nghỉ việc cập nhật trạng thái và lock account/stamp, giữ hồ sơ. Admin cuối phải có người thay trước áp nghỉ; position/account không còn hợp lệ làm job lỗi có thể xử lý, không ghi AppliedAt giả. Nếu app ngủ/tắt, job catch-up khi chạy lại; không cam kết chính xác 00:00 khi hosting không hoạt động. Chưa có external scheduler hoặc job dashboard.

## 2. Ranh giới còn thiếu

Chưa có IndexedDB queue, idempotency key checkin, realtime push hoặc điều chỉnh late event sang kỳ sau. Cửa sổ chống lặp 2 phút không là queue/idempotency protocol. Mutation tháng Locked trả 409; hiện phải mở lại có lý do rồi review/relock version mới. Không tự sửa snapshot khi đồng bộ muộn.

Cấp phép định kỳ và dọn ảnh chưa bật: cần mức hưởng/đối tượng/ngày cấp/proration/carryover/retention được chốt. Khi thêm job, dùng khóa nghiệp vụ duy nhất, batch/retry giới hạn, audit, dry-run và test chạy lại; không chỉ dùng boolean in-memory. SQL legacy có cron/RPC không nối vào .NET hiện tại.

Liên quan [01](01-plan.md), [15](15-api-contract.md), [20](20-observability.md), [21](21-operations-runbook.md).
