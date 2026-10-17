# 23 — Nâng schema và dữ liệu

> Cập nhật 2026-10-17. CURRENT: EF SQL Server + additive PostgreSQL đã có kiểm thử cô lập theo worklog. Live chưa áp.

## 1. Chọn đúng lịch sử

SQL Server dùng [Migrations](../marixa_aspnet/M.Repositories/Migrations), giữ nguyên migration lịch sử; hai migration mới cho kỳ công và chính sách phỏng vấn. PostgreSQL dùng [kỳ công](../marixa_aspnet/sql/postgresql/20261017-attendance-periods.sql) rồi [chính sách](../marixa_aspnet/sql/postgresql/20261017-workforce-policies.sql). Bộ [supabase legacy](../marixa_aspnet/supabase/README.md) không thuộc EF/Identity hiện tại; apply-migrations.ps 1 mặc định từ chối chạy nó.

Production init/seed mặc định tắt. Explicit initializer chỉ hỗ trợ tạo bảng/nullable column/index; cột bắt buộc thiếu cần migration/backfill rõ. Sync không rename/đổi kiểu/drop/reconcile dữ liệu; không là migration engine cho mọi thay đổi. SQL mới có transaction/advisory lock ở PG và unique preflight ledger; chạy lặp không tạo thêm version công.

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
