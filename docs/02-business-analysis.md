# 02 — Phân tích nghiệp vụ Marixa

> Chủ tài liệu và quyết định nghiệp vụ: **Đinh Văn Tài**.
> Cập nhật: **17/10/2026**. Phạm vi xác minh: mã nguồn trong repo, chưa nghiệm thu ứng dụng hoặc hạ tầng live.
> Đối tượng: chủ dự án, developer, Codex và Agnes.

## 1. Phạm vi và cách sử dụng

Marixa phục vụ quản lý nhân sự và chấm công cho khối văn phòng quy mô nhỏ, dự kiến dưới 15 người. Giao diện và thuật ngữ nghiệp vụ dùng tiếng Việt; múi giờ nghiệp vụ là `Asia/Ho_Chi_Minh` (UTC+7). Đường chạy hiện tại là ASP.NET Core 8 + ASP.NET Identity/JWT + React 19/Vite.

Quy ước trạng thái và quyền cập nhật tài liệu theo [00-README.md](00-README.md). **CURRENT** mô tả điều đã đối chiếu trong source; **TARGET/PENDING** mô tả yêu cầu/đề xuất chưa hoàn tất; **NOT VERIFIED** cần kiểm tra runtime hoặc xác nhận thêm. CURRENT không có nghĩa là hành vi đó đã đúng nghiệp vụ.

**EN — AI contract:** Read [00-README.md](00-README.md) and [01-plan.md](01-plan.md) first. Preserve recorded owner decisions. Keep observed implementation separate from required behavior; do not redefine business rules to legitimize a bug. Source inspection does not prove runtime or production readiness.

## 2. Quyết định nghiệp vụ đang giữ

Các quyết định đã ghi trong [01-plan.md](01-plan.md) và lịch sử [worklog.md](worklog.md) được giữ khi chuẩn hóa tài liệu này:

| Chủ đề | Yêu cầu áp dụng | Tình trạng triển khai |
| --- | --- | --- |
| Ảnh và GPS chấm công | Bằng chứng **tùy chọn**; giữ quyết định nghiệp vụ. | API nhận/lưu GPS/device tùy chọn, default PhotoRequired=false. UI đọc rule để yêu cầu ảnh/GPS, gửi tọa độ khi được cung cấp; chưa geofence. |
| Ảnh lưu trữ | Giữ bucket public `marixa-photos`. | **CURRENT:** backend trả URL public khi cấu hình Storage. Cấu hình bucket live **NOT VERIFIED**. |
| Ca làm | Một quy tắc chung active; không version/ngày hiệu lực. | Service thay active trong transaction, seeder không ghi đè; model có unique filtered index. DB live chưa áp/xác minh. |
| Phép năm | Giữ chính sách hiện tại; không tự đổi mức cấp, cách cộng dồn hoặc điều kiện hưởng. | Có loại nghỉ, hạn mức và ledger. Chưa chứng minh tự động cấp phép định kỳ trong đường chạy hiện tại. |
| Ngoài phạm vi đã làm | Offline/IndexedDB, accrual/retention và nghiệm thu live. | Backend period/snapshot/XLSX/PDF đã có; không gọi UI/live hoàn tất từ API. |

Yêu cầu nền tảng vẫn gồm: ngày công theo Việt Nam, giữ log chấm gốc và truy vết các điều chỉnh. **Chính sách OT còn cần chủ dự án xác nhận:** tài liệu cũ mô tả “duyệt rồi lấy phần giao với giờ thực tế”, trong khi source và một số ghi nhận lịch sử có OT tự động. Không tự chọn một cách tính làm yêu cầu cuối cùng. Nguyên tắc chống tự duyệt trong tài liệu cũ cũng cần đối chiếu các ngoại lệ HR/Admin hiện có trước khi thống nhất ma trận áp dụng. Các sai khác với code được ghi tại §5 để xử lý có chủ đích.

## 3. Phạm vi module và vai trò

### 3.1. Phạm vi có source

| Nhóm | CURRENT | Giới hạn |
| --- | --- | --- |
| Nhân sự | Hồ sơ, phòng ban, vị trí, hợp đồng, lương, bảo hiểm, người phụ thuộc, ngân hàng. | Có màn hình/API không đồng nghĩa đã nghiệm thu toàn bộ quy tắc dữ liệu. |
| Chấm công | Vào/ra, ảnh/GPS/device theo rule, lịch sử/duyệt/sửa công/period snapshot. | UI đã nối ảnh/GPS theo rule, tab OT/sửa công và kỳ/XLSX trong trang cũ; chưa geofence, live chưa nghiệm thu. |
| Nghỉ và tăng ca | Đơn/ledger, Approved cancellation/refund; OT chỉ giao actual×đơn duyệt, tách công thường. | Chính sách accrual/qua năm cần chốt thêm. |
| Nhân sự mở rộng | Thăng chức, bàn giao, báo cáo nhân viên và các phiên bản/tệp/sự kiện của báo cáo. | Không nhầm phiên bản báo cáo với snapshot bảng công. |
| Công việc | Dự án, thành viên, task, bình luận và đánh giá hiệu suất. | Quyền từng thao tác cần kiểm tra controller/service tương ứng. |
| Quản trị | Tài khoản, kích hoạt, khóa tài khoản, role, dashboard và các màn hình danh mục. | Khóa tài khoản khác khóa kỳ công. |

Ngoài phạm vi V1: kho, xuất nhập khẩu, CRM, khai thuế/BHXH tự động và mô hình chấm đội công trường. Source có `Shift`/`EmployeeShift`; sự tồn tại các bảng này không thay đổi quyết định dùng một ca chung trong V1.

### 3.2. Vai trò

| Role chính xác trong code | Phạm vi có khai báo quyền |
| --- | --- |
| `Employee` | Các màn hình cá nhân: hồ sơ, chấm công, công/đơn và thông tin nhân sự của mình. Đây cũng là phạm vi dữ liệu cần được backend bảo vệ. |
| `HR` | Nhân sự, đối soát và một số thao tác duyệt; quyền không đồng nhất với Manager ở mọi luồng. |
| `Manager` | Các module được khai báo cho quản lý; không mặc định bằng HR hoặc Admin. |
| `Admin` | Quản trị tài khoản/role, danh mục và các thao tác quản trị được controller cho phép. |
| `Accountant`, `Sale`, `Marketing` | Đã seed tên role; chưa có ma trận quyền module riêng hoàn chỉnh. Không suy ra quyền nghiệp vụ từ tên role. |

Nguồn: [RoleSeeder.cs](../marixa_aspnet/M.API/Seed/RoleSeeder.cs), [modules.js](../marixa_react/src/constants/modules.js), [permission.js](../marixa_react/src/services/auth/permission.js). Tài khoản có thể mang nhiều role; phải kiểm tra tổ hợp role khi nghiệm thu.

Sidebar/route guard chỉ quyết định UI. `[Authorize]` chỉ chứng minh yêu cầu đăng nhập ở endpoint đó; quyền xem/sửa **đúng nhân viên** phải được server kiểm tra riêng. Backend đã bổ sung ownership cho Attendance/các dữ liệu cá nhân và giới hạn LeaveRequest/get-all cho staff. Test hồi quy không thay audit toàn bộ endpoint hoặc phạm vi quản lý từng module. Xem ma trận quyền tại [03-architecture.md](03-architecture.md) và kịch bản tại [06-testing.md](06-testing.md).

## 4. Quy tắc nghiệp vụ cần đạt

| Chủ đề | TARGET/PENDING hoặc yêu cầu cần giữ |
| --- | --- |
| Ngày và thời điểm | Lưu thời điểm có offset/UTC; xác định ngày công theo Việt Nam. Ngày nghiệp vụ không được suy ra bằng ngày UTC. |
| Bằng chứng | Cho phép ảnh/GPS tùy chọn đúng quyết định; hiển thị rõ chưa có bằng chứng/quyền bị chặn; nếu thu thập thì truyền và lưu đúng. |
| Ca và giờ công | Một rule active, BreakStartTime/Minutes, công thường trong ca trừ nghỉ; phút→decimal 2 số. Snapshot giữ rule tại khóa. |
| Sửa công | Tạo yêu cầu riêng, lưu mốc đề nghị và trước/sau; bảo toàn log gốc. Kết quả duyệt có thể ghi override ở ngày công, phải truy vết được người và lý do. |
| Nghỉ phép | Chỉ thay đổi theo trạng thái được phép; số liệu ledger phải nhất quán, chống trừ/hoàn lặp. Quy tắc hủy đơn đã duyệt chưa được triển khai, không hứa đã có. |
| Tăng ca | Quyết định 17/10: chỉ giao actual×đơn Approved, union khoảng chồng, không cộng tự động toàn giờ ra muộn. |
| Duyệt | Xác thực người duyệt từ phiên đăng nhập và bảo vệ quyền. Tài liệu cũ nêu chống tự duyệt; ngoại lệ HR/Admin hiện có cần chủ dự án xác nhận theo từng loại đơn trước khi sửa chính sách. |
| Chốt kỳ và xuất báo cáo | Backend HR review→Admin lock/reopen có lý do; snapshot/version/hash, XLSX scope và PDF đơn nghỉ. UI đã có trong trang Thống kê/Nghỉ phép, kiểm cô lập ghi ở worklog. |

## 5. Sai khác đã xác minh trong source

| Chủ đề | CURRENT | Hướng xử lý trong [01-plan.md](01-plan.md) |
| --- | --- | --- |
| Ảnh | UI/API theo PhotoRequired, default false; camera chỉ mở khi chọn thêm ảnh. | Kiểm camera/quyền/thiết bị thật trước nghiệm thu. |
| GPS | API nhận/lưu latitude/longitude/deviceId; UI gửi lat/lon đã lấy theo thao tác người dùng. | Geofence chưa triển khai; cần kiểm thiết bị/quyền thật. |
| Ngày công | Service dùng BusinessTime ngày VN, timestamp UTC và clock inject; có test ranh giới ngày. | Đối soát ngày cũ/UI và runtime hai provider. |
| Ca | Seeder giữ rule tùy chỉnh; replacement active atomic và index một active; checkin đọc giờ vào rule khi không có ca kế hoạch. | Giữ ca chung, nghiệm thu DB thật và hành vi công lịch sử. |
| Giờ công | ActualHours decimal 2 số trong khung ca trừ break; OT riêng. Nghỉ mặc định 12:00–13:00, cấu hình theo rule. | Biên/ca qua đêm và dữ liệu cũ cần đối soát. |
| OT | Calculator/live read/snapshot dùng đơn Approved có StartAt/EndAt; không dùng số giờ nhập tay để công nhận. | Nghiệm thu UI và dữ liệu Approved cũ thiếu range. |
| Tự duyệt | HR/Admin được tự duyệt công/OT/sửa công; leave luôn cần người khác. | Đã xác nhận 17/10, có regression. |
| Hủy nghỉ | Pending: chủ/Admin. Approved: HR/Admin khác chủ có lý do; ghi hủy riêng, Refund đúng Deduct một lần, giữ approval cũ. | Quyết định 17/10 trong quyền giao Codex, có regression. |
| Audit và bất biến | SaveChanges chặn sửa/xóa raw logs, snapshot/events và Deduct/Refund; có audit period/correction/cancellation. | Không chống DBA/raw SQL, chưa audit đồng nhất mọi module. |

Nguồn chi tiết: [AttendanceService.cs](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceService.cs), [AttendanceTimeCalculator.cs](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceTimeCalculator.cs), [AttendanceRuleSeeder.cs](../marixa_aspnet/M.API/Seed/AttendanceRuleSeeder.cs), [LeaveRequestService.cs](../marixa_aspnet/M.Services/Services/Leave/LeaveRequestService.cs), [OvertimeRequestService.cs](../marixa_aspnet/M.Services/Services/Leave/OvertimeRequestService.cs), [AttendanceCorrectionService.cs](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceCorrectionService.cs), [AttendanceCard.jsx](../marixa_react/src/modules/me/components/AttendanceCard.jsx).

## 6. Báo cáo và điều kiện nghiệm thu

**CURRENT:** có Excel import/export hồ sơ HR, CSV lịch sử công/thống kê ở frontend và tệp báo cáo nhân viên. Các xuất dữ liệu này dùng dữ liệu hiện thời, là định dạng khác với XLSX snapshot/PDF backend mới; xem 15.

**CURRENT backend:** khóa/mở kỳ/snapshot/XLSX/PDF có code và test; UI mới/live chưa nghiệm thu. **PENDING:** offline queue. SQL lịch sử dưới `marixa_aspnet/supabase` không là bằng chứng các luồng ấy đang chạy trong ASP.NET/React.

Nghiệm thu phải có bằng chứng kiểm thử quyền dữ liệu, ranh giới ngày Việt Nam, tính công/OT/phép, retry/concurrency và việc bảo toàn log/điều chỉnh. Build thành công không thay thế kiểm thử nghiệp vụ. Ma trận và cách ghi kết quả nằm ở [06-testing.md](06-testing.md); trạng thái công việc ở [01-plan.md](01-plan.md).

Xem tiếp [04-database-design.md](04-database-design.md) và [05-workflow.md](05-workflow.md).
