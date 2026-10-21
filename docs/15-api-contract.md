# 15 — Hợp đồng API Marixa

> Bổ sung 21/10: quyền đọc Payroll, tạo/ngừng ca và payload skipSunday của EmployeeShift/create-bulk theo [36](36-workflow-improvements.md). Các route cũ được giữ; inventory runtime được sinh lại từ test.

> Cập nhật 2026-10-17. CURRENT theo controllers, service guards và TestServer. API production: NOT VERIFIED.

Nguồn route đầy đủ: [api-inventory](api-inventory.md), sinh từ EndpointDataSource trong ContractInventoryTests. Swagger của đúng phiên bản bổ sung DTO; danh mục role attributes phải đọc cùng [ma trận dữ liệu](16-security-privacy.md). Base của nghiệp vụ là /api; GUID trong URL/body không cấp quyền truy cập.

## 1. Quy ước chung

Đăng nhập trả JWT, gửi Authorization Bearer ở request sau. Tên JSON giữ camelCase, enum số theo DTO hiện hành. Ngày nghiệp vụ yyyy-MM-dd; thời điểm dùng ISO 8601 có offset. Pagination pageNumber bắt đầu 1, pageSize tối đa 200. Dữ liệu thường có statusCode/code/message/data; BasePaginatedList thêm metadata. Download trả stream với Content-Type/Content-Disposition; không bọc JSON. Một số route cũ có envelope khác, đọc contract thực tế trước khi đổi client.

| HTTP | Ý nghĩa / thao tác client |
| --- | --- |
| 400 | Input/enum/ngày/cửa sổ OT không hợp lệ; hiện lỗi để người dùng sửa. |
| 401 / 403 | Hết phiên hoặc thiếu quyền/phạm vi; đăng nhập lại hoặc dừng thao tác. |
| 404 | Route/record/version/file không tồn tại. |
| 409 | Kỳ khóa, chưa review, xung đột/constraint hoặc lịch sử bất biến; đọc lại trạng thái, không tự retry vô hạn. |
| 429 | Auth rate limit; đợi Retry-After. |
| 502 / 503 | Storage lỗi hoặc readiness DB/schema chưa đạt. |

API trả Cache-Control no-store và X-Correlation-ID. Exception/rate-limit có traceId; các envelope cũ không bảo đảm đều có trường này. CORS expose correlation, Retry-After và tên file. Không ghi token/request body vào log.

## 2. API mới và thay đổi tương thích

| ID | Method/route | Quyền / tiền điều kiện | Kết quả |
| --- | --- | --- | --- |
| API-PER-01 | GET /api/AttendancePeriod/{year}/{month} và /history | Admin/HR/Manager | State Open 0/Reviewed 1/Locked 2 và audit. |
| API-PER-02 | POST cùng prefix /review | HR/Admin; reason 5–1000; có công hợp lệ, không đơn Pending | Reviewed; chưa tạo snapshot. |
| API-PER-03 | POST /lock, /reopen | Admin; reason 5–1000; lock cần Reviewed | Lock tạo version mới; reopen giữ các version cũ. |
| API-EXP-01 | GET /snapshot hoặc /export.xlsx?version=1 | Employee chỉ hàng của mình; staff toàn kỳ | Dữ liệu/hash theo phạm vi, hoặc XLSX từ snapshot. Bỏ version lấy bản chốt mới nhất, kể cả kỳ đã mở lại. |
| API-EXP-02 | GET /api/LeaveRequest/{id}/pdf | Chủ đơn hoặc HR/Admin | PDF trạng thái dữ liệu hiện tại, thời điểm xuất; không phải chữ ký số. |
| API-UI-01 | GET /api/LeaveRequest/mine | User có hồ sơ nhân viên | Đơn của chính mình; /by-employee và /get-all cần staff. |
| API-LEDGER-01 | GET /api/LeaveLedger/summary/{employeeId}/year/{year} và /by-employee/{employeeId}/year/{year} | Owner/staff theo guard | Số dư và lịch sử, không dùng MaxDays trừ đơn để đoán. |
| API-LEDGER-02 | POST /api/LeaveLedger/create-entry | HR/Admin; employeeId/year/entryType/days/reason, leaveTypeId tùy chọn | Grant=1 dương, Adjust=2 số có dấu khác 0, [-999,999]; reason 5–500. Deduct/Refund do workflow. |
| API-LEAVE-01 | POST /api/LeaveRequest/cancel/{id} | Pending: chủ/Admin. Approved: HR/Admin khác chủ, reason 5–500 | Giữ người duyệt cũ, ghi người/lúc/lý do hủy; hoàn đúng Deduct; gọi lại không hoàn thêm. |

ActualHours giữ tên JSON nhưng nay là decimal công thường; OvertimeHours là OT được công nhận, tách riêng. BreakStartTime bổ sung vào rule, mặc định 12:00; BreakMinutes mặc định 60. OT phải có StartAt/EndAt; OvertimeHours nhập tay không là nguồn công nhận. Frontend đọc trực tiếp số lẻ từ API, form HR không tự tính/làm tròn công hoặc gửi OT suy đoán; Admin có form cấu hình giờ nghỉ. Kiểm browser/local xem 06, live chưa nghiệm thu.

Không tự retry checkin như có idempotency key: cửa sổ chống lặp 2 phút chỉ là cơ chế hiện hành. Thao tác period state sai trả 409; cancellation lặp hợp lệ không tạo Refund thứ hai. Đồng bộ offline và versioned /api/v1 không có trong stack hiện hành.

Liên quan: [05](05-workflow.md), [06](06-testing.md), [23](23-data-migration.md).
