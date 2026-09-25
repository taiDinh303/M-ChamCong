

## Phụ lục — Bảng bằng chứng (evidence ledger)

| # | Nhận định | Nguồn | Bằng chứng (trích dẫn) |
|---|---|---|---|
| E1 | Chấm công phải có ảnh bắt buộc, 2 thao tác vào/ra ca, quy quyền xem, danh sách người duyệt | PDF tr.7 (Checklist C) | "Chụp ảnh: Bắt buộc mỗi lần chấm. Vào ca · Ra ca: Hai nút riêng. Ai duyệt: Nhân sự, Kế toán trưởng, Giám đốc, CEO." |
| E2 | Yêu cầu ca/lịch ca (sheet 05), mốc giờ chung, chưa tự tính trễ/về sớm, GPS chưa dùng | PDF tr.7 | "kết thúc ca 16:30, 8 giờ/ngày (CẦN XÁC NHẬN). Nghỉ giữa ca: Chưa tự trừ giờ nghỉ. Định vị GPS: Chưa dùng." |
| E3 | Login name = mã nhân viên; ca/lịch ca chưa có; địa điểm chấm công cần đối chiếu; người duyệt công = sheet 02 | PDF tr.5, tr.8 | "Mã nhân viên — cũng là tên đăng nhập. Ca · lịch làm việc: sheet 05 (CHƯA CÓ · ĐỐI CHIẾU)." |
| E4 | Mô hình chỉ ghi nhận sự kiện thực tế, không có ảnh/ca/duyệt | code: Attendance.cs, AttendanceLog.cs | "AttendanceLog: LogTime, Type(CheckIn/CheckOut), Method, Latitude/Longitude, DeviceId, Note." |
| E5 | Không tồn tại entity Shift/HolidayCalendar/WorkLocation/AttendanceRule | code: Entities/ inventory | 15 entity: Attendance, AttendanceLog, Bank, Department, Employee, EmployeeBankAccount, EmployeeContract, EmployeeDependent, EmployeeInsurance, EmployeeSalary, LeaveRequest, LeaveType, Payroll, Position, SalaryGroup |
| E6 | Position ≠ ApplicationRole (hai tầng: HR vs Identity) | code: Position.cs, ApplicationRole.cs | "ApplicationRole : IdentityRole<Guid> (Name, Description). Position: Code, Name, Description, IsActive, Employees." |
| E7 | Employee đủ thông tin nhân sự theo checklist PDF | code: Employee.cs | "EmployeeCode, CitizenId, PhoneNumber, ManagerId, StartDate, ProbationEndDate, LaborType, Status, UsePhoneAttendance." |
| E8 | Duyệt chỉ có trên LeaveRequest | code: LeaveRequest.cs | "ApprovedBy (Employee), Approver, ApprovedAt." |
| E9 | API attendance hiện chỉ CRUD theo ngày | code: AttendanceService.cs | "CreateAttendanceModelView: EmployeeId, AttendanceDate, Status, Note." |
| E10 | Phân quyền theo vai trò khớp IdentityRole/ApplicationUserRole | PDF tr.13 | "Người lao động chỉ xem công của mình. Người duyệt công xem dữ liệu chấm công để duyệt." |
