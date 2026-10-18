# 23 — Nâng schema và dữ liệu

> Cập nhật 2026-10-18. CURRENT: đã nâng schema PostgreSQL live theo quyền chủ dự án; backup, restore thử và đối chiếu dữ liệu tại worklog. Nghiệm thu nghiệp vụ live ghi riêng.

## 1. Chọn đúng lịch sử

SQL Server dùng [Migrations](../marixa_aspnet/M.Repositories/Migrations), giữ nguyên migration lịch sử; hai migration mới cho kỳ công và chính sách phỏng vấn. PostgreSQL dùng [kỳ công](../marixa_aspnet/sql/postgresql/20261017-attendance-periods.sql) → [chính sách](../marixa_aspnet/sql/postgresql/20261017-workforce-policies.sql) → [ràng buộc công/ca](../marixa_aspnet/sql/postgresql/20261017171012_attendance_integrity.sql). Bộ [supabase legacy](../marixa_aspnet/supabase/README.md) không thuộc EF/Identity hiện tại; apply-migrations.ps 1 mặc định từ chối chạy nó.

Production init/seed mặc định tắt. Explicit initializer chỉ hỗ trợ tạo bảng/nullable column/index; cột bắt buộc thiếu cần migration/backfill rõ. Sync không rename/đổi kiểu/drop/reconcile dữ liệu; không là migration engine cho mọi thay đổi. SQL mới có transaction/advisory lock ở PG và unique preflight ledger; chạy lặp không tạo thêm version công.

Script integrity bổ sung hai unique index cho một rule active và một ngày công chưa xóa mỗi nhân viên; database cũ có thể thiếu dù model đã khai báo. Script khóa hai bảng trong transaction, dừng nếu có dữ liệu trùng, đặt lock timeout 5 giây và statement timeout 30 giây. Không tự hợp nhất/xóa bản ghi để vượt preflight. Phải kiểm tra migration history trước khi áp; tên/version do MCP ghi trên live nằm ở worklog, không sửa lịch sử để ép khớp ngày trong tên SQL.

## 2. Preflight bắt buộc trên bản sao dữ liệu

- So table/column/provider/EF migration history với phiên bản xuất phát; không áp T-SQL vào PostgreSQL.
- Tìm trùng Attendance(EmployeeId,AttendanceDate) chưa xóa; active rule>1; Deduct/Refund trùng (LeaveRequestId,Year,EntryType), kể cả bản xóa mềm. Đối soát bằng chứng, không tự gộp/xóa người dùng hoặc công.
- OT Approved thiếu/sai StartAt/EndAt phải sửa có audit; không được suy giờ OT từ số nhập tay. Review kỳ sẽ trả 409 nếu dữ liệu này chưa hợp lệ.
- Kiểm ngày UTC cũ so ngày VN, ca qua đêm, leave qua năm và thăng chức/nghỉ việc tương lai đã bị áp sớm trong bản cũ. Không tự đảo dữ liệu lịch sử hay dịch mọi nhãn ngày.
- Backup DB+files, thử script và smoke trên bản sao, kiểm số liệu trước/sau và rollback. Quyền owner RLS của kết nối .NET cần xác minh.

## 3. Thay đổi lần này

Ba bảng period/state/snapshot/event; unique năm-tháng và period-version. ActualHours int→decimal(10,2) giữ số cũ, live read/snapshot mới dùng công thức mới; không rewrite snapshot cũ. Rule có BreakStartTime mặc định 12:00; handover có AppliedAt; leave có metadata hủy. Ledger có unique deduction/refund theo request/year/type. Cần phối hợp UI để nhận số lẻ, giờ nghỉ và trạng thái duyệt-chờ-hiệu-lực.

Sau apply kiểm readiness, column types, index/filter/FK, RLS/grants 3 bảng mới, công thức mẫu 08–18 nghỉ 1 giờ/OT 17–19=8 giờ thường+1 giờOT, đóng/mở/relock và refund lặp. Job tự catch-up approved-due sau startup; kiểm hồ sơ và admin cuối trước bật app.

Rollback ưu tiên quay app compatible với schema đã mở rộng. EF Down có thể xóa snapshot/history; ép decimal về int mất phần lẻ. Không chạy rollback phá dữ liệu để giải quyết lỗi app; restore đã kiểm thử hoặc forward-fix sau bảo toàn dữ liệu, xem [22](22-backup-recovery.md).

## 4. Bằng chứng lần nâng live 18/10/2026

Chủ dự án giao Codex sao lưu và áp trực tiếp trên marixa001 sau lượt chẩn đoán chỉ đọc. Đã khôi phục snapshot public vào PostgreSQL 17.11 loopback, so đầy đủ 43 bảng/54 bản ghi và chạy cả ba script hai lần trên bản sao trước khi áp live. Live sau nâng có 46 bảng/730 cột, không thiếu cột/index theo model; ActualHours là numeric(10,2). Hash và số hàng của toàn bộ 43 bảng cũ giữ nguyên khi loại riêng cột mới và chuẩn hóa biểu diễn số; readiness trả 200. Chi tiết phạm vi backup và việc chưa nghiệm thu ở [22](22-backup-recovery.md), worklog và [18](18-release-checklist.md).
