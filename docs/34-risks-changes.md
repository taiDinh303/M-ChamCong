# 34 — Rủi ro và quản lý thay đổi

> Cập nhật 2026-10-18. Owner sản phẩm: Đinh Văn Tài. Rủi ro còn lại không được ẩn sau điểm đánh giá backend.

| ID / mức | Rủi ro hiện hành | Cách xử lý / tiêu chí đóng |
| --- | --- | --- |
| R-01 P0 | Cần backup ngoài máy, restore binary và disaster recovery đầy đủ; quyền function hệ thống còn cảnh báo advisory | Schema live và restore public đã kiểm 18/10 theo 22/23; 46 bảng có RLS. Advisor còn hai WARN về quyền gọi function event-trigger rls_auto_enable có sẵn; rà soát hardening riêng, không mở quyền Data API để chữa lỗi 500 |
| R-02 P0 | App_Data và photo fallback chưa volume bền trong blueprint | Chọn persistent volume/private object adapter phù hợp, backup và thử redeploy/restore |
| R-03 P1 | UI mới có kiểm cô lập, chưa nghiệm thu hosting/camera/GPS/đủ role/thiết bị thật; bundle lớn | Smoke sau nâng schema; kiểm mobile/keyboard, phân quyền từng luồng; tách bundle khi có kiểm soát CSS/layout |
| R-04 P1 | Manager scope rộng ở nhiều CRUD, role nghiệp vụ chưa có ma trận riêng | Chủ dự án chốt phạm vi, test từng module; không coi role attribute là đủ |
| R-05 P1 | Dữ liệu ngày cũ, OT thiếu range, leave qua năm, ca qua đêm và thay rule trước khóa | Đối soát bản sao; chốt chính sách biên, không tự backfill/gộp dữ liệu |
| R-06 P1 | Worker phụ thuộc uptime, chưa có cảnh báo/durable scheduler ngoài app | Kiểm pending-due/AppliedAt, baseline và alert; catch-up có test |
| R-07 P1 | Chưa job cấp phép/retention/offline queue, RPO/RTO/on-call chưa chốt | Phỏng vấn bổ sung trước triển khai; theo 22/27/31 |
| R-08 P2 | Service cũ lớn, mutable entities/IQueryable/DTO object, cancellation chưa đồng nhất | Refactor theo nghiệp vụ có regression, không thêm lớp rỗng |
| R-09 P1 | Rate limit IP sau proxy có thể chia sẻ; chưa có malware scanning/file reconciliation job | Xác minh ingress, theo dõi 429; quy trình kiểm file/khôi phục và quét khi cần |

## Quyết định thay đổi 17/10

Chủ dự án xác nhận OT theo giao actual×đơn Approved, công thường trong khung ca trừ nghỉ, tách OT, phút→2 sốthập phân; nghỉ mặc định 12–13 cấu hình theo ca. HR/Admin tự duyệt công/OT/sửa công; nghỉ cần người khác. Chủ dự án giao Codex chọn ngày hiệu lực và hủy/cấp phép: chọn tự áp promotion đúng ngày/nghỉ sau ngày cuối; HR/Admin khác chủ hủy Approved có lý do/hoàn đúng một lần, chưa bật cấp phép tự động. Nguồn phỏng vấn trực tiếp trong nhiệm vụ, không suy từ SQL legacy.

Thay đổi sau này ghi lý do, nguồn quyết định, REQ/API/TC, ảnh hưởng schema/quyền/UI và bằng chứng; cập nhật 01+ADR+worklog. Không sửa sự kiện lịch sử thành hiện trạng mới. Giá/plan/hosting chỉ giữ snapshot tại 00 và kiểm lại trước thao tác cloud.
