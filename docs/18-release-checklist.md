# 18 — Điều kiện phát hành

> Cập nhật 2026-10-18. Checklist release; dấu đạt theo bằng chứng ở [worklog](worklog.md), không theo sự tồn tại file.

## 1. Gate trước khi push

- Build Release warnings-as-errors, tests có số pass/fail/skip; provider, export và Markdown theo [19](19-cicd.md). Nếu sửa React: npm test/lint/build và browser smoke trên API/database cô lập; không dùng VITE env trỏ cloud.
- Review diff/secret/license, bảo toàn migration lịch sử; schema mới có script và duplicate preflight theo [23](23-data-migration.md).
- Chính sách đã phỏng vấn ghi trong 01; API mới/decimal giờ công/BreakStartTime/cancellation metadata được mô tả tại 15.
- Không tuyên bố frontend/live/restore đã pass từ kiểm thử backend. Báo điểm và giới hạn tại [BACKEND_REVIEW](../marixa_aspnet/BACKEND_REVIEW.md).

## 2. Gate trước production

| Gate | Bằng chứng bắt buộc | Tình trạng lần này |
| --- | --- | --- |
| Schema upgrade | Backup + preflight + chạy script ở staging tương đương, kiểm index/columns/readiness | Đã restore thử và nâng live 18/10: 46 bảng/730 cột, dữ liệu 43 bảng cũ giữ nguyên, readiness 200; chi tiết worklog |
| Storage bền | Volume App_Data/ảnh local nếu dùng, quyền user app và restore thử | NOT VERIFIED live |
| Config/identity | Secret riêng, CORS origin, init/seed tắt, login + lockout + vai trò | Có test cô lập; live đã dùng phiên Employee/Admin kiểm quyền cá nhân/HR, chưa audit toàn bộ config/lockout |
| Nghiệp vụ | Checkin, duyệt, hủy/hoàn phép, OT riêng, khóa/mở/version/export; smoke có dữ liệu được phép | Backend/Node test và browser cô lập; live đã đọc lịch sử/thống kê/kỳ công/nghỉ phép/bàn giao, chưa nghiệm thu các luồng ghi và thiết bị thật |
| Job ngày hiệu lực | Pending-due count, AppliedAt, retry/last-admin, kiểm khởi động lại | Có test processor; job live chưa test |
| Backup/rollback | Bản DB + files, người vận hành, restore kiểm quyền và hash | Đã thử restore snapshot public trên PostgreSQL 17.11; binary/files, bản ngoài máy và disaster recovery đầy đủ chưa nghiệm thu; xem 22 |

Render blueprint autoDeploy=true. Nhánh thay đổi có migration được push riêng để review; merge vào nhánh deploy chỉ khi các gate production liên quan đã đạt. Quyền push không tự xác nhận dữ liệu/hạ tầng đã sẵn sàng.

Sau rollout: /health/ready 200, smoke một luồng có ghi bằng dữ liệu được phép, kiểm logs/429/5 xx, xác minh private download và file cũ, so hash snapshot cũ. Khi rollback ưu tiên đổi app về bản tương thích schema mở rộng; không chạy Down xóa snapshot hoặc ép decimal về int khi chưa bảo toàn dữ liệu.
