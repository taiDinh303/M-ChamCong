# 05 — Luồng nghiệp vụ hiện hành

> Cập nhật 2026-10-17. CURRENT backend theo source/test; UI mới và production chưa nghiệm thu. Quyết định gốc tại 01, contract tại 15.

## 1. Danh tính và chấm công

Admin/staff được phép cấp tài khoản/kích hoạt theo guard; nhân viên đặt password mạnh rồi login JWT. Login sai 5 lần có lockout; rate limit Auth theo peerIP. Change-password đúng chủ và mật khẩu cũ, stamp/role/link thay đổi làm phiên cũ không còn hợp lệ. Admin thuần không cần hồ sơ NV cho chức năng account, nhưng thao tác nghiệp vụ cần employee claim theo contract.

Checkin lấy timestamp server UTC và ngày VN, kiểm owner/staff/employee/enum/GPS bounds/rule, transaction Serializable tìm/tạo ngày công; log cùng loại trong 2 phút trả alreadyRecorded. Unique công-ngày phải có trên DB. API nhận ảnh/GPS/device tùy chọn theo rule; UI đang còn bắt ảnh/chưa gửiGPS. Raw log không sửa/xóa; correction là đường thay giờ có review/audit. Không có offline queue hoặc idempotency key.

## 2. Tính giờ và duyệt công

ActualHours là công thường trong khung ca, trừ giao giờ nghỉ; OvertimeHours là giao actual×cửa sổ Approved, gom union các khoảng chồng, trừ nghỉ và không đếm đôi với công thường. Giữ phút, round 2 số thập phân AwayFromZero khi ra giờ. Ví dụ 08–18, ca 08–17/nghỉ 12–13, đơn 17–19 Approved→8 thường+1 OT. Chưa đủ vào/ra hoặc ra trước vào thì null, không tự bịa 0 giờ.

Giờ override nếu có, nếu không lấy log vào sớm/ra muộn chưa xóa. Trước khóa, lịch sử đọc theo rule active; snapshot giữ bản rule khi khóa. Bản công Approved không sửa/checkin/xóa trực tiếp; staff mở trạng thái cần thiết rồi áp yêu cầu sửa trong kỳ mở. Người duyệt lấy actor, không tin reviewer GUID từ client. HR/Admin được tự duyệt công/OT/sửa công; Manager không tự duyệt. Không tự suy chính sách lương/ngày lễ từ phép tính.

## 3. Đơn nghỉ và sổ phép

Pending→Approved/Rejected hoặc Cancelled. Chủ/staff tạo/sửa đơn Pending đúng phạm vi, TotalDays>0 không vượt số ngày lịch chọn. Approve/Reject cần người khác và role hợp lệ; logic HR/Manager theo service, Admin ưu tiên khi có nhiều role. Approved loại có MaxDays kiểm hạn mức năm bắt đầu, tạo Deduct trong cùng transaction. Chính sách qua năm chưa mở rộng; phải đối soát trước dùng trường hợp này.

Pending hủy bởi chủ/Admin. Approved hủy bởi HR/Admin khác chủ với lý do 5–500; ghi CancelledBy/At/Reason, giữ ApprovedBy/At cũ; Refund đúng Deduct theo năm/type, retry không hoàn thêm. Kỳ khóa chặn cả hủy nguồn; Admin phải reopen trước. Rejected không đổi, request có quyết định không xóa. Deduct/Refund append-only và unique(request/year/type); manual ledger dùng Grant/Adjust, không giả Deduct/Refund qua CRUD.

## 4. Review, khóa kỳ và xuất

| Bước | Người / điều kiện | Kết quả |
| --- | --- | --- |
| Open | Dữ liệu công/đơn còn được ghi theo quyền | Chưa snapshot |
| Review | HR/Admin; có công, mọi công Approved/status hợp lệ, không Pending leave/OT/correction, đúng 1 rule | Reviewed có lý do/người/lúc |
| Lock | Admin, đang Reviewed và kiểm lại điều kiện | Locked, version tăng, snapshot JSON/rule/hash và event |
| Source đổi khi Reviewed | Guard SaveChanges | Về Open, event ReviewInvalidated |
| Ghi nguồn kỳ Locked | Mọi đường SaveChanges liên quan tháng cũ/mới |409 PERIOD_LOCKED |
| Reopen | Admin có lý do | Open; version cũ còn đọc được |
| Re-review/relock | Cùng gate | Version mới, bản cũ không sửa/xóa |

XLSX đọc snapshot/version, Employee chỉ hàng của mình, staff toàn kỳ; hash theo dữ liệu được trả. PDF đơn nghỉ đọc trạng thái hiện tại, owner hoặc HR/Admin; ghi thời điểm dữ liệu/xuất và metadata hủy khi có. Không là chữ ký điện tử. Không infer ngày vắng chưa có Attendance; cần HR đối soát đầy đủ trước review.

## 5. HR và ngày hiệu lực

Report gửi→manager review/changes→resubmit; file private và write-batch bù khi rollback, giữ route/DTO hiện hành. Handover cần manager, project/assets/files hợp lệ; promotion cần vị trí/role/đề xuất và reviewer đúng scope. Approved không đồng nghĩa đã đổi hồ sơ ngay.

Promotion áp từ EffectiveDate VN; handover áp sau LastWorkingDate. Worker sweep mỗi phút khi app chạy, catch-up sau restart, AppliedAt chống lặp và từng item transaction. Role/code/position đổi kèm stamp; nghỉ việc lock account/stamp nhưng giữ hồ sơ. Last active Admin phải có người thay; lỗi chưa áp để retry, log cần người vận hành xử lý. Không có cron cấp phép/dọn ảnh trong job này.

Nguồn: [AttendanceService](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceService.cs), [calculator](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceTimeCalculator.cs), [period](../marixa_aspnet/M.Services/Services/Timekeeping/AttendancePeriodService.cs), [leave](../marixa_aspnet/M.Services/Services/Leave/LeaveRequestService.cs), [processor](../marixa_aspnet/M.Services/Services/HumanResources/EmploymentChangeProcessor.cs). Kiểm thử [06](06-testing.md), vận hành [21](21-operations-runbook.md).
