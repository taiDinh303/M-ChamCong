# 36 — Bổ sung luồng ca làm và kế toán

> Ngày 21/10/2026. Tham khảo các thao tác kiểm thử MARIXA; tự triển khai trên ASP.NET/React hiện hành, giữ giao diện của project intern.

## 1. Quyết định và phạm vi

- APPROVED — chủ dự án chọn ưu tiên ca làm/chấm công; gán từ 1 nhân viên, tối đa 31 ngày tính cả hai đầu, có tùy chọn bỏ Chủ nhật.
- APPROVED — Admin và HR được tạo/ngừng áp dụng ca. Manager giữ quyền xếp lịch trong phạm vi đang phụ trách.
- APPROVED — đưa bảng lương vào module Kế toán riêng: Admin/Accountant xem toàn công ty, HR tiếp tục nhập lương cơ bản trong hồ sơ, nhân viên chỉ xem thông tin lương cá nhân. Chủ dự án chọn chỉ xem, tìm/lọc kỳ/trạng thái/chi tiết trong lượt này; không thêm công thức tính, duyệt hoặc chi lương. API ghi cũ chỉ dành cho Admin; Accountant không có quyền ghi.
- Giữ một AttendanceRule active để tính công. Shift/EmployeeShift là lịch kế hoạch; không đổi công thức tính công sang ca phân lịch.
- Không nhập mã nguồn nghiệp vụ hoặc giao diện từ dự án tham khảo. Không chuyển sang đa công ty, không thêm MFA hoặc ký số khi chưa có yêu cầu/thiết kế riêng.

## 2. REQ/API/TC

| ID | Hành vi cần đạt | API / màn hình | Kiểm tra |
| --- | --- | --- | --- |
| REQ-SHIFT-01 | Từ chối giờ vào bằng giờ ra, dữ liệu ca không hợp lệ | Shift/create, update; Lịch làm | HTTP tạo/sửa âm; giờ qua đêm vẫn cho phép |
| REQ-SHIFT-02 | Admin/HR tạo và ngừng áp dụng ca; không xóa lịch sử | Shift/create, update; danh mục ca trong Lịch làm | HTTP role, danh mục UI |
| REQ-SHIFT-03 | Gán từ 1 người, tối đa 31 ngày, bỏ Chủ nhật theo lựa chọn; không sinh trùng | EmployeeShift/create-bulk; Lịch làm | Biên 31/32 ngày, Chủ nhật, lặp lại, phạm vi Manager |
| REQ-ACC-01 | Module Kế toán riêng cho bảng lương hiện có, Admin/Accountant chỉ đọc từ giao diện mới | /accounting, /accounting/payroll | HTTP Admin/Accountant 200, HR/Manager/Employee 403; Accountant ghi 403; lọc và chi tiết |

## 3. Hợp đồng và cách dùng

- `POST /api/EmployeeShift/create-bulk`: payload giữ `employeeIds`, `shiftId`, `effectiveFrom`, `effectiveTo`, `note`; thêm `skipSunday` (mặc định false để giữ client cũ). Giao diện mặc định chọn bỏ Chủ nhật. Server kiểm danh sách không rỗng/trùng, nhân viên còn làm, ca còn nhận lịch mới, ngày 1–31 và phạm vi Manager từ actor thật.
- Tạo một bản ghi mỗi ngày-người phù hợp bitmask ngày làm của ca; bỏ Chủ nhật nếu chọn, bỏ qua ngày đã được cùng ca bao phủ. `createdAssignmentCount` đếm bản ghi mới, `assignedEmployeeCount` đếm người có thêm lịch, `alreadyAssignedCount` đếm người không cần thêm lịch. Lượt không có ngày phù hợp trả 400 `NO_WORKING_DAYS`. Kiểm tra/ghi cùng transaction; không cam kết tự retry lỗi serialization.
- Ngừng áp dụng dùng `PUT /api/Shift/update` với `isActive=false`, không xóa. Ca ngừng không nhận gán mới; lịch cũ vẫn đọc được. Quyền sửa giờ ca cũ của Manager giữ nguyên; đổi trạng thái ca chỉ Admin/HR.
- `GET /api/Payroll/get-all`, `get-by-id/{id}`: Admin/Accountant. CRUD ghi cũ: Admin. Không mở Accountant sang API hồ sơ HR, ngân hàng hoặc quyền quản trị.
- Home mở `/accounting`; `/finance` chuyển tới module này và kiểm quyền. `/admin/payroll`, `/hr/payroll` chuyển tới `/accounting/payroll`; HR không có Accountant/Admin sẽ bị chặn. `/me/salary` vẫn là thông tin mức lương của chính nhân viên, không trở thành bảng lương toàn công ty.
- Kế toán mới chỉ hiển thị số đã lưu trong Payroll, không tính lại từ công hoặc cộng lại thành số thực nhận. Các trạng thái hiện có được giữ. Lọc nhân viên/kỳ/trạng thái, phân trang 20 dòng, xem chi tiết và tải lại; lỗi API không hiển thị tổng 0.

## 4. Giới hạn nghiệm thu

CURRENT trong mã nguồn, đã kiểm thử HTTP và trình duyệt với dữ liệu cô lập; bằng chứng và kết quả tại [worklog](worklog.md). NOT VERIFIED trên production và thiết bị thật. Không chạy migration hoặc ghi dữ liệu production. Ca qua đêm và nghỉ áp dụng công thức hiện hành; không tự đặt chính sách OT, thuế hoặc BHXH mới.
