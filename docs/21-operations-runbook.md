# 21 — Runbook vận hành

> Cập nhật 2026-10-17. Quy trình đề xuất để người vận hành thực hiện; không ghi nhận đã deploy hoặc khôi phục thật.

## 1. Chẩn đoán ban đầu

Ghi thời điểm UTC/VN, phiên bản/commit triển khai, correlation ID, endpoint và ảnh hưởng người dùng; không thu token/mật khẩu. Kiểm /health/live rồi /health/ready, log gần deployment và trạng thái DB/storage/volume. Người có quyền thay đổi production: chủ dự án hoặc người được chỉ định; liên hệ/on-call CHƯA XÁC ĐỊNH.

| Triệu chứng | Kiểm tra | Khắc phục có kiểm soát |
| --- | --- | --- |
| Ready 503 sau release | Column/table/index mới, migration đúng provider, kết nối/TLS | Nâng schema đã review hoặc quay app tương thích; không bật seed mẫu/xóa DB. |
| Login 401 hàng loạt | JWT issuer/key/clock/stamp/role, lockout và liên kết NV | Chỉnh cấu hình đúng hoặc đăng nhập lại sau thay đổi hợp lệ; không cấp token bỏ kiểm. |
| Auth 429 | PeerIP/proxy, số retry, Retry-After | Đợi window; xác minh proxy trước cấu hình IP thật, không trust mọi forwarded header. |
| Upload 502 | Key/header/bucket/network/size | Sửa adapter config; không lặng lẽ chuyển dữ liệu xuống đĩa ephemeral. |
| Download HR 404 | DB metadata, App_Data/legacy file và volume | Restore file theo 22, giữ quyền endpoint; không đưa trở lại đường static public. |
| Mutation 409 kỳ khóa | State/version/history | Admin mở lại có lý do, sửa nguồn, HR review, khóa version mới; bản cũ giữ nguyên. |
| Job due không áp | Approved/AppliedAt/ngày VN, account/position/last admin, log | Sửa nguyên nhân; worker retry mỗi phút/catch-up sau restart. Không set AppliedAt để che lỗi. |

## 2. Tính liên tục và đóng sự cố

Thay đổi cloud/schema phải có backup, kế hoạch quay lại và dữ liệu thử được cho phép. Khi xác nhận lộ credential, thu hồi/rotate tại nguồn, đổi key/stamp cần thiết, kiểm phạm vi log/file/Git; không dán secret vào ticket. Khi mất files, tránh ghi đè backup; quy trình [22](22-backup-recovery.md).

Sau khắc phục: smoke đúng luồng ảnh hưởng và quyền owner/outsider, hash snapshot, số Deduct/Refund, pending-due. Ghi nguyên nhân, hành động, commit, khoảng thời gian và bằng chứng vào worklog; mở việc phòng ngừa có owner. Không đánh dấu incident closed từ một lần Swagger trả 200.
