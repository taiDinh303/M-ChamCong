# Đánh giá frontend Marixa

> 2026-10-17. Phạm vi checkout codex/frontend-api-alignment, API local + LocalDB giả. Không phải chứng nhận production hoặc audit bảo mật toàn sản phẩm.

## Điểm nghiêm khắc: 81/100 (8,1/10)

| Nhóm | Điểm | Bằng chứng và phần bị trừ |
| --- | --- | --- |
| Hợp đồng API / luồng nghiệp vụ | 27/30 | 190 khai báo method/route khớp Swagger, 56 GET thật đạt; browser OT/sửa công/period/export/leave. Chưa chạy mọi payload của mọi module/role. |
| Đúng số liệu / quyền / trạng thái | 22/25 | Công thường/OT/ledger theo API, phút/decimal/VN time, self-review, mine, session/prefix guard, lỗi không giả số 0. Manager scope còn rộng ở backend; live schema chưa nâng. |
| Cấu trúc và bảo trì | 16/20 | API/contract/time/download/modal helper, CSS token, callback làm mới dữ liệu, docs/CI rõ. Một số component HR lớn, CSS global và bundle main/ExcelJS còn lớn; chưa tách toàn bộ theo route. |
| Kiểm chứng và sẵn sàng phát hành | 16/25 | 21 Node tests, lint/build đạt; 93 backend PASS/1 PostgreSQL SKIP; browser hai vai trò, dialog mobile/Escape và snapshot cũ bất biến. Chưa browser suite tự động, cloud/mobile thật/camera/GPS/restore/đủ breakpoint hoặc remote CI. |

Số ca và kịch bản chi tiết ở [worklog](../docs/worklog.md), [06-testing](../docs/06-testing.md). Điểm cao hơn cần bằng chứng bổ sung, không chỉ thêm test mirror hoặc thay đổi nhãn CURRENT.

## Việc còn lại theo phạm vi

P0 phát hành: chủ dự án backup/preflight và tự chạy hai SQL theo [23](../docs/23-data-migration.md), xác minh schema/readiness và smoke trên môi trường được phép. Không sửa UI để che lỗi 500 thiếu cột. File nhân sự/private volume và backup-restore live vẫn theo [18](../docs/18-release-checklist.md).

P1 kiểm chứng: đủ HR/Manager và module CRUD trên dữ liệu giả, camera/GPS thật, keyboard/thiết bị/breakpoint, lỗi mạng và luồng cạnh tranh. Tạo browser E2E lặp lại với DB/bucket cô lập. Dữ liệu ngày/OT range cũ cần đối soát, không suy tự sửa lịch sử.

P2 cấu trúc: chia component lớn và bundle có kiểm soát thứ tự CSS/layout; giữ giao diện hiện có. ExcelJS đang tải động, không làm tăng ngưỡng cảnh báo để coi đã tối ưu. Offline queue, accrual và role nghiệp vụ mới chưa nằm trong chức năng đã triển khai.
