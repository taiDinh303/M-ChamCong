# 02 — Phân tích nghiệp vụ Marixa

> Chủ tài liệu và quyết định nghiệp vụ: **Đinh Văn Tài**.
> Cập nhật: **16/10/2026**. Phạm vi xác minh: mã nguồn trong repo, chưa nghiệm thu ứng dụng hoặc hạ tầng live.
> Đối tượng: chủ dự án, developer, Codex và Agnes.

## 1. Phạm vi và cách sử dụng

Marixa phục vụ quản lý nhân sự và chấm công cho khối văn phòng quy mô nhỏ, dự kiến dưới 15 người. Giao diện và thuật ngữ nghiệp vụ dùng tiếng Việt; múi giờ nghiệp vụ là `Asia/Ho_Chi_Minh` (UTC+7). Đường chạy hiện tại là ASP.NET Core 8 + ASP.NET Identity/JWT + React 19/Vite.

Quy ước trạng thái và quyền cập nhật tài liệu theo [00-README.md](00-README.md). **CURRENT** mô tả điều đã đối chiếu trong source; **TARGET/PENDING** mô tả yêu cầu/đề xuất chưa hoàn tất; **NOT VERIFIED** cần kiểm tra runtime hoặc xác nhận thêm. CURRENT không có nghĩa là hành vi đó đã đúng nghiệp vụ.

**EN — AI contract:** Read [00-README.md](00-README.md) and [01-plan.md](01-plan.md) first. Preserve recorded owner decisions. Keep observed implementation separate from required behavior; do not redefine business rules to legitimize a bug. Source inspection does not prove runtime or production readiness.

## 2. Quyết định nghiệp vụ đang giữ

Các quyết định đã ghi trong [01-plan.md](01-plan.md) và lịch sử [worklog.md](worklog.md) được giữ khi chuẩn hóa tài liệu này:

| Chủ đề | Yêu cầu áp dụng | Tình trạng triển khai |
| --- | --- | --- |
| Ảnh và GPS chấm công | Bằng chứng **tùy chọn**; thiếu quyền camera/GPS cần được thể hiện rõ, không tự đặt thêm điều kiện bắt buộc. | **TARGET/PENDING:** UI hiện yêu cầu có ảnh; GPS chưa đi hết luồng lưu dữ liệu. |
| Ảnh lưu trữ | Giữ bucket public `marixa-photos`. | **CURRENT:** backend trả URL public khi cấu hình Storage. Cấu hình bucket live **NOT VERIFIED**. |
| Ca làm | Một quy tắc chung active cho công ty; giữ đơn giản, không bổ sung version hoặc ngày hiệu lực cho `AttendanceRule`. | **CURRENT:** có CRUD rule và chọn rule active mới nhất; chưa đảm bảo duy nhất một rule active. |
| Phép năm | Giữ chính sách hiện tại; không tự đổi mức cấp, cách cộng dồn hoặc điều kiện hưởng. | Có loại nghỉ, hạn mức và ledger. Chưa chứng minh tự động cấp phép định kỳ trong đường chạy hiện tại. |
| Ngoài phạm vi đã làm | Offline/IndexedDB, khóa kỳ, snapshot kỳ và xuất XLSX/PDF theo kỳ. | **TARGET/PENDING:** chưa có luồng hoàn chỉnh trong ứng dụng ASP.NET/React hiện tại. |

Yêu cầu nền tảng vẫn gồm: ngày công theo Việt Nam, giữ log chấm gốc và truy vết các điều chỉnh. **Chính sách OT còn cần chủ dự án xác nhận:** tài liệu cũ mô tả “duyệt rồi lấy phần giao với giờ thực tế”, trong khi source và một số ghi nhận lịch sử có OT tự động. Không tự chọn một cách tính làm yêu cầu cuối cùng. Nguyên tắc chống tự duyệt trong tài liệu cũ cũng cần đối chiếu các ngoại lệ HR/Admin hiện có trước khi thống nhất ma trận áp dụng. Các sai khác với code được ghi tại §5 để xử lý có chủ đích.

## 3. Phạm vi module và vai trò

### 3.1. Phạm vi có source

| Nhóm | CURRENT | Giới hạn |
| --- | --- | --- |
| Nhân sự | Hồ sơ, phòng ban, vị trí, hợp đồng, lương, bảo hiểm, người phụ thuộc, ngân hàng. | Có màn hình/API không đồng nghĩa đã nghiệm thu toàn bộ quy tắc dữ liệu. |
| Chấm công | Vào/ra, ảnh, lịch sử, thống kê, trạng thái duyệt ngày công, rule, lịch/ca và yêu cầu sửa công. | GPS và ngày công còn sai khác; chưa khóa kỳ. |
| Nghỉ và tăng ca | Đơn nghỉ, loại nghỉ, ledger; đơn tăng ca và phê duyệt. | Chưa nối giờ OT được duyệt vào phép tính OT của ngày công. |
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

Sidebar/route guard chỉ quyết định UI. `[Authorize]` chỉ chứng minh yêu cầu đăng nhập ở endpoint đó; quyền xem/sửa **đúng nhân viên** phải được server kiểm tra riêng. Hiện còn khoảng trống đã thấy trong source: `Attendance` đọc theo ID/employee và checkin chưa đối chiếu quyền sở hữu; `LeaveRequest/get-all` chưa giới hạn role hoặc lọc theo chủ đơn. Xem ma trận quyền tại [03-architecture.md](03-architecture.md) và kịch bản tại [06-testing.md](06-testing.md).

## 4. Quy tắc nghiệp vụ cần đạt

| Chủ đề | TARGET/PENDING hoặc yêu cầu cần giữ |
| --- | --- |
| Ngày và thời điểm | Lưu thời điểm có offset/UTC; xác định ngày công theo Việt Nam. Ngày nghiệp vụ không được suy ra bằng ngày UTC. |
| Bằng chứng | Cho phép ảnh/GPS tùy chọn đúng quyết định; hiển thị rõ chưa có bằng chứng/quyền bị chặn; nếu thu thập thì truyền và lưu đúng. |
| Ca và giờ công | Áp dụng một rule chung nhất quán cho giờ chuẩn, nghỉ ca và trạng thái. Không âm thầm đổi lịch sử công khi chỉnh rule. Chưa có snapshot; cách xử lý lịch sử phải được làm rõ trước khi triển khai. |
| Sửa công | Tạo yêu cầu riêng, lưu mốc đề nghị và trước/sau; bảo toàn log gốc. Kết quả duyệt có thể ghi override ở ngày công, phải truy vết được người và lý do. |
| Nghỉ phép | Chỉ thay đổi theo trạng thái được phép; số liệu ledger phải nhất quán, chống trừ/hoàn lặp. Quy tắc hủy đơn đã duyệt chưa được triển khai, không hứa đã có. |
| Tăng ca | **Cần quyết định:** OT tự động, OT có duyệt hay kết hợp theo loại ngày. Mô tả cũ “lấy phần giao đơn Approved với giờ thực tế” là phương án chờ xác nhận, chưa là quy tắc chung đã chốt. Luôn phân biệt giờ vượt mốc với giờ đã được duyệt. |
| Duyệt | Xác thực người duyệt từ phiên đăng nhập và bảo vệ quyền. Tài liệu cũ nêu chống tự duyệt; ngoại lệ HR/Admin hiện có cần chủ dự án xác nhận theo từng loại đơn trước khi sửa chính sách. |
| Chốt kỳ và xuất báo cáo | Nếu triển khai: quy trình HR kiểm tra → Admin khóa → snapshot; mở lại có lý do; báo cáo thể hiện kỳ/trạng thái. Đây là phần chưa làm. |

## 5. Sai khác đã xác minh trong source

| Chủ đề | CURRENT | Hướng xử lý trong [01-plan.md](01-plan.md) |
| --- | --- | --- |
| Ảnh | `AttendanceCard` khóa nút khi chưa có ảnh; DTO/API chấm công cho phép `PhotoUrl` null; rule seed đặt `PhotoRequired=true`. | Đồng bộ với quyết định bằng chứng tùy chọn. |
| GPS | UI chỉ hiển thị tọa độ. DTO checkin không có latitude/longitude; service không gán tọa độ vào log. | Hoàn tất hợp đồng và lưu dữ liệu trước khi mô tả GPS/geofence đã chạy. |
| Ngày công | Checkin tìm/tạo ngày bằng `DateTime.UtcNow.Date`; tính trạng thái/giờ có chuyển UTC+7. | Sửa ranh giới ngày theo Việt Nam, kiểm tra dữ liệu cũ. |
| Ca | Seeder ghi lại `DEFAULT-CA` thành 08:00–17:00, nghỉ 60 phút, active mỗi lần boot; `AttendanceRuleService` không chặn nhiều active. Status checkin dùng ca kế hoạch hoặc mốc cố định 08:00, không đọc `AttendanceRule.CheckInTime`. | Giữ ca chung đơn giản nhưng nhất quán và không mất cấu hình sau restart. |
| Giờ công | Calculator làm tròn giờ thực thành `int`, OT một chữ số thập phân; nghỉ bắt đầu cố định 12:00. `AttendanceRule`/status evaluator có mặc định 16:30, seeder/calculator dùng 17:00. | Thống nhất công thức và độ chính xác; không tự chọn lại chính sách làm tròn. |
| OT | Ngày công tự tính phần ra sau giờ chuẩn; không đọc `OvertimeRequest.Approved`. | Ghi nhận sai khác với phương án có duyệt trong tài liệu cũ; chốt chính sách trước khi đổi phép tính, luôn phân biệt giờ vượt mốc và OT được duyệt. |
| Tự duyệt | Leave chặn mọi người tự duyệt. OT và sửa công có ngoại lệ HR/Admin; duyệt ngày công chưa có kiểm tra tự duyệt trong service. | Rà quyền, người duyệt và ngoại lệ; ghi quyết định trước khi đổi chính sách. |
| Hủy nghỉ | Chỉ `Pending` được hủy. `Approved` trả `INVALID_STATUS`; nhánh refund chỉ xử lý Deduct tồn dư ở Pending. | Giữ mô tả hiện tại; luồng hủy nghỉ đã duyệt/hoàn phép là phần chưa làm cần đặc tả. |
| Audit và bất biến | Có `AuditLog` và snapshot riêng của yêu cầu sửa; có CRUD log/ngày công và xóa cứng được expose cho role quản lý ở một số controller. | Không chứng nhận log/audit bất biến toàn hệ thống; cần kiểm tra quyền và đường sửa/xóa. |

Nguồn chi tiết: [AttendanceService.cs](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceService.cs), [AttendanceTimeCalculator.cs](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceTimeCalculator.cs), [AttendanceRuleSeeder.cs](../marixa_aspnet/M.API/Seed/AttendanceRuleSeeder.cs), [LeaveRequestService.cs](../marixa_aspnet/M.Services/Services/Leave/LeaveRequestService.cs), [OvertimeRequestService.cs](../marixa_aspnet/M.Services/Services/Leave/OvertimeRequestService.cs), [AttendanceCorrectionService.cs](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceCorrectionService.cs), [AttendanceCard.jsx](../marixa_react/src/modules/me/components/AttendanceCard.jsx).

## 6. Báo cáo và điều kiện nghiệm thu

**CURRENT:** có Excel import/export hồ sơ HR, CSV lịch sử công/thống kê ở frontend và tệp báo cáo nhân viên. Các xuất dữ liệu này dùng dữ liệu hiện thời, không chứng minh đã có bảng công XLSX/PDF theo kỳ khóa.

**TARGET/PENDING:** offline, khóa/mở kỳ, snapshot, XLSX bảng công khóa và PDF đơn nghỉ chưa được nghiệm thu hoặc triển khai đầy đủ. SQL lịch sử dưới `marixa_aspnet/supabase` không là bằng chứng các luồng ấy đang chạy trong ASP.NET/React.

Nghiệm thu phải có bằng chứng kiểm thử quyền dữ liệu, ranh giới ngày Việt Nam, tính công/OT/phép, retry/concurrency và việc bảo toàn log/điều chỉnh. Build thành công không thay thế kiểm thử nghiệp vụ. Ma trận và cách ghi kết quả nằm ở [06-testing.md](06-testing.md); trạng thái công việc ở [01-plan.md](01-plan.md).

Xem tiếp [04-database-design.md](04-database-design.md) và [05-workflow.md](05-workflow.md).
