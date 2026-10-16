# 04 — Thiết kế dữ liệu Marixa

> Chủ tài liệu: **Đinh Văn Tài**. Cập nhật: **16/10/2026**.
> Trạng thái: **CURRENT** theo EF model và service trong repo; schema/dữ liệu của môi trường live **NOT VERIFIED**.

## 1. Nguồn và quy ước

Nguồn model chính là [DatabaseContext.cs](../marixa_aspnet/M.Repositories/Context/DatabaseContext.cs), [Entities](../marixa_aspnet/M.Contract.Repositories/Entities) và [AuthEntity](../marixa_aspnet/M.Contract.Repositories/AuthEntity). Một file có thể chứa nhiều entity: `EmployeeReport.cs` chứa report/version/attachment/event; `WorkTask.cs` chứa task/comment. Không dùng quy tắc “một file = một bảng”.

Tên dưới đây là **tên entity**, không bảo đảm tên vật lý giống hệt; đối chiếu EF mapping và catalog của đúng môi trường trước khi viết SQL. Quyền cập nhật và nghĩa các trạng thái theo [00-README.md](00-README.md).

**EN — AI contract:** Treat EF entities/context as the current model, not proof of the deployed schema. Check service validation, database constraints and API ownership separately. Preserve the simple shared active rule, public photo bucket and existing leave policy. Schema changes require an explicit migration/backfill/rollback plan; startup column sync is not a complete migration system.

### 1.1. Kiểu dữ liệu và khóa

- Entity nghiệp vụ kế thừa [BaseEntity.cs](../marixa_aspnet/M.Core/Base/BaseEntity.cs): `Id: Guid` là PK; `CreatedTime`/`LastUpdatedTime: DateTimeOffset`; `DeletedTime: DateTimeOffset?`; các trường `CreatedBy`, `LastUpdatedBy`, `DeletedBy` nullable. `Id` khởi tạo bằng `Guid.NewGuid()`.
- Identity dùng các entity và khóa của ASP.NET Identity; không suy ra toàn bộ Identity có cấu trúc `BaseEntity`.
- Dấu `?` trong bảng là nullable theo C# model. EF-generated constraint và constraint thực tế sau schema sync có thể khác nhau; chưa xác minh live.
- `DateTimeOffset` dùng cho mốc chấm và audit. `DateTime` còn dùng cả thời điểm và ngày nghiệp vụ. Bộ lọc input/context chuẩn hóa `DateTime.Kind` sang UTC cho PostgreSQL; việc chuẩn hóa này **không** tự sửa ngày công theo Việt Nam.
- Soft delete được service lọc ở nhiều truy vấn; chưa có bằng chứng global query filter áp dụng đồng nhất. Không suy ra xóa mềm nghĩa là không có API xóa cứng.

## 2. Danh mục entity hiện có

| Nhóm | Entity | Liên kết/nội dung chính |
| --- | --- | --- |
| Tài khoản | `ApplicationUser`, `ApplicationRole`, `ApplicationUserRole`, `ApplicationUserClaim`, `ApplicationRoleClaim`, `ApplicationUserLogin`, `ApplicationUserToken` | ASP.NET Identity. Role seed: Admin, Employee, Manager, HR, Accountant, Sale, Marketing. |
| Kích hoạt | `ActivationCode` | Liên kết Employee bắt buộc, User tùy chọn; phục vụ kích hoạt/cấp tài khoản. |
| Hồ sơ | `Employee` | Mã/họ/tên; `UserId?`, `DepartmentId?`, `PositionId?`, `ManagerId?`; trạng thái nhân viên. |
| Tổ chức | `Department`, `Position` | Danh mục; Department có `ManagerId?` trỏ Employee. |
| Hợp đồng/lương | `EmployeeContract`, `SalaryGroup`, `EmployeeSalary`, `Payroll` | Hợp đồng theo Employee; lịch sử mức lương và khoảng áp dụng; bảng lương tháng. Không đồng nghĩa đã tự tính đủ lương từ bảng công. |
| Bảo hiểm/thân nhân | `EmployeeInsurance`, `EmployeeDependent` | Theo Employee; số bảo hiểm/thuế, người phụ thuộc và thời gian hiệu lực. |
| Ngân hàng | `Bank`, `EmployeeBankAccount` | Tài khoản theo Employee và Bank; cờ primary/trạng thái. |
| Cấu hình công | `AttendanceRule`, `Shift`, `EmployeeShift` | Rule chung; danh mục ca; gán ca có `EffectiveFrom`, `EffectiveTo?`. Các trường hiệu lực của EmployeeShift không là version của AttendanceRule. |
| Lịch/địa điểm | `HolidayCalendar`, `OfficeLocation` | Ngày lễ/lịch; tọa độ, bán kính và cờ primary/active. Có bảng địa điểm không chứng minh luồng geofence đã chạy. |
| Ngày công | `Attendance`, `AttendanceLog`, `AttendanceCorrection` | Tổng hợp ngày, sự kiện chấm, yêu cầu sửa/override. |
| Nghỉ/tăng ca | `LeaveType`, `LeaveRequest`, `LeaveLedger`, `OvertimeRequest` | Loại/hạn mức, đơn nghỉ, giao dịch phép và đơn OT. |
| Nhật ký | `AuditLog` | Actor, action, entity, before/after và reason; có index tra cứu. Chưa chứng minh bất biến toàn hệ thống. |
| Báo cáo | `EmployeeReport`, `EmployeeReportVersion`, `EmployeeReportAttachment`, `EmployeeReportEvent` | Báo cáo theo Employee → version → tệp đính kèm; event theo report. Không phải snapshot kỳ công. |
| Nhân sự mở rộng | `EmployeePromotion`, `EmployeeHandover` | Thăng chức/bàn giao, trạng thái và người/thời điểm duyệt. |
| Công việc | `WorkProject`, `WorkProjectMember`, `WorkTask`, `WorkTaskComment`, `WorkPerformance` | Dự án/thành viên; task/người giao/người nhận; bình luận theo task; đánh giá theo Employee/kỳ. |

## 3. Từ điển dữ liệu trọng yếu

### 3.1. Hồ sơ và ca chung

| Entity | Trường/kiểu trọng yếu | Ràng buộc hoặc lưu ý CURRENT |
| --- | --- | --- |
| `Employee` | `EmployeeCode: string(50)`, `GivenName: string(100)`, `FamilyName: string(100)` bắt buộc; `UserId/DepartmentId/PositionId/ManagerId: Guid?` | User liên kết 1:1 bằng `Employee.UserId`, không chỉ đối chiếu mã hoặc điện thoại. `FullName` là `[NotMapped]`. |
| `AttendanceRule` | `Code: string(50)`, `Name: string(150)` bắt buộc; `CheckInTime/CheckOutTime: TimeOnly`; `StandardHours/BreakMinutes/LateGraceMinutes/EarlyLeaveThresholdMinutes: int`; `PhotoRequired/GpsRequired/IsActive: bool` | Chưa có unique constraint bảo đảm một active. Không có version/ngày hiệu lực; không tự bổ sung. Entity mặc định ra 16:30, seeder ghi 17:00. |
| `Shift` | Code, Name, `StartTime/EndTime: TimeOnly`, `StandardHours/BreakMinutes/WorkDays: int`, `IsNight/IsActive: bool` | Có schema/CRUD; V1 vẫn giữ quyết định một ca chung. |
| `EmployeeShift` | `EmployeeId/ShiftId: Guid`, `EffectiveFrom: DateTime`, `EffectiveTo: DateTime?` | FK Employee và Shift dùng Restrict. |

### 3.2. Công và sự kiện chấm

| Entity | Trường/kiểu trọng yếu | Ràng buộc hoặc lưu ý CURRENT |
| --- | --- | --- |
| `Attendance` | `EmployeeId: Guid`, `AttendanceDate: DateTime`, `Status: AttendanceStatus?`, `ApprovalStatus: AttendanceApprovalStatus` | Service check trùng Employee/ngày; **không thấy unique index cặp này trong context**. Checkin đang dùng ngày UTC. |
| `Attendance` | `CheckInTime/CheckOutTime: DateTimeOffset?`, `PlannedShiftId: Guid?`, `PlannedHours/ActualHours: int?` | Giờ vào/ra là override nullable; nếu null thì derive log sớm nhất/muộn nhất. ActualHours làm tròn số nguyên. |
| `Attendance` | `CheckInPhoto/CheckOutPhoto: string(500)?`, `ApprovedBy: Guid?`, `ApprovedAt: DateTime?`, `ChangeSummary: string(500)?`, `Note: string?` | ApprovedBy trỏ Employee. `OvertimeHours: decimal?` là `[NotMapped]`, được tính lại khi đọc. |
| `AttendanceLog` | `AttendanceId: Guid`, `LogTime: DateTimeOffset`, `Type: AttendanceLogType`, `Method: AttendanceMethod` | Quan hệ Attendance 1:n log; cascade khi xóa cứng Attendance. Chặn trùng 2 phút ở service, chưa là unique/idempotency key tại DB. |
| `AttendanceLog` | `PhotoUrl: string(500)?`, `Latitude/Longitude: decimal(10,7)?`, `DeviceId: string(100)?`, `Note: string?` | Có cột GPS/device; DTO checkin hiện không truyền các trường đó. |
| `AttendanceLog` | `IsAdjusted: bool`, `AdjustedBy: string?`, `AdjustedAt: DateTimeOffset?`, `AdjustmentNote: string?` | Có model chỉnh log; cần kiểm tra các đường CRUD trước khi khẳng định event bất biến. |
| `AttendanceCorrection` | `EmployeeId: Guid`, `AttendanceId: Guid?`, `WorkDate: DateTime`; `ProposedCheckIn/ProposedCheckOut: DateTimeOffset?`, `ProposedStatus: AttendanceStatus?` | Tạo yêu cầu phải có ít nhất một giá trị đề nghị; hai mốc nếu cùng có thì ra sau vào. |
| `AttendanceCorrection` | `Reason/ReviewNote: string(500)?`, `Status`, `ReviewedBy: Guid?`, `ReviewedAt: DateTime?`, `BeforeJson/AfterJson: text?` | Duyệt áp dụng override vào Attendance liên quan, giữ AttendanceLog; nếu không có Attendance liên quan thì không tự tạo ngày công. |

### 3.3. Đơn nghỉ, ledger và OT

| Entity | Trường/kiểu trọng yếu | Ràng buộc hoặc lưu ý CURRENT |
| --- | --- | --- |
| `LeaveType` | Code, Name bắt buộc; `MaxDays: decimal?`, `IsPaid/IsActive: bool` | `MaxDays` có giá trị là điều kiện service kiểm tra hạn mức và tạo Deduct. Không có cờ riêng “trừ phép năm”. |
| `LeaveRequest` | `EmployeeId/LeaveTypeId: Guid`, `FromDate/ToDate: DateTime`, `TotalDays: decimal(10,2)?`, Reason, Status | Service kiểm tra ngày, loại active, trùng khoảng Pending/Approved; mặc định số ngày là khoảng ngày gồm hai đầu. Không tự suy ra đã trừ cuối tuần/ngày lễ. |
| `LeaveRequest` | `ApprovedBy: Guid?`, `ApprovedAt: DateTime?` | ApprovedBy trỏ Employee; khi hủy Pending trường này ghi người hủy. Approved/Rejected không được sửa/hủy qua luồng hiện tại. |
| `LeaveLedger` | `EmployeeId: Guid`, `Year: int`, `LeaveTypeId/LeaveRequestId: Guid?`, `EntryType`, `Days: decimal(10,2)`, `Reason: string(500)?` | Deduct/Refund do service tạo dùng lượng ngày dương, ý nghĩa cộng/trừ theo EntryType. Adjust có thể có dấu. Không dựa vào comment “Days âm khi trừ”. |
| `OvertimeRequest` | `EmployeeId: Guid`, `AttendanceId: Guid?`, `WorkDate: DateTime`, `StartAt/EndAt: DateTimeOffset?`, `OvertimeHours: decimal(10,2)?` | Giờ nhập hoặc tính từ khoảng giờ, làm tròn 2 chữ số; độc lập với OT transient của Attendance. |
| `OvertimeRequest` | Reason/Note tối đa 500, Status, `ApprovedBy: Guid?`, `ApprovedAt: DateTime?` | Service chặn một đơn Pending cùng nhân viên/ngày bằng truy vấn. Không có unique index tương ứng; chưa nối phê duyệt vào tính công. |

Hai đường tổng hợp ledger hiện khác nhau: `LeaveLedgerService.GetSummaryAsync` cộng Adjust cả âm/dương và lọc xóa mềm; `LeaveRequestService.LedgerSummaryAsync` chỉ cộng Adjust dương, giới hạn số dùng tối thiểu 0 và không lọc `DeletedTime` trong truy vấn tổng. Đây là sai khác **cần đối soát**, không phải hai chính sách phép được chấp nhận. Kiểm tra hạn mức duyệt nghỉ hiện dựa vào `MaxDays` và Deduct/Refund theo năm/loại, không chứng minh đang dùng số dư Grant theo chính sách cộng dồn.

### 3.4. Báo cáo và trạng thái

`EmployeeReport` có `ReportCode` unique, Title, ReportType, EmployeeId, Period, nội dung và trạng thái. `EmployeeReportVersion` lưu ReportId, Version, FileName/StoredName, FileSize, UploadedBy/UploadedAt. Attachment liên kết VersionId; Event liên kết ReportId và ghi action, actor, comment, thời điểm. Không coi `UploadedBy`, `ReviewedBy` hoặc mọi GUID có chữ “Id” là FK đã enforce nếu context chưa khai báo.

| Enum/constant | Giá trị CURRENT |
| --- | --- |
| `AttendanceStatus` | Present=1, Late=2, EarlyLeave=3, Absent=4, Leave=5, Holiday=6, Weekend=7; cột có thể null. |
| `AttendanceApprovalStatus` | Pending=0, Approved=1, Rejected=2. |
| `AttendanceLogType` | CheckIn=1, CheckOut=2. |
| `AttendanceMethod` | Manual=1, Fingerprint=2, FaceRecognition=3, Phone=4, GPS=5, Import=6. Tên enum không chứng minh có tích hợp thiết bị/vân tay/khuôn mặt. |
| `LeaveRequestStatus`, `OvertimeStatus`, `AttendanceCorrectionStatus` | Pending=1, Approved=2, Rejected=3, Cancelled=4. Có enum Cancelled không chứng minh có endpoint hủy cho cả ba luồng. |
| `LeaveLedgerEntryType` | Grant=1, Adjust=2, Deduct=3, Refund=4. |
| `EmployeeReportStatus` | PendingManager=0, ManagerApproved=1, Rejected=2, ChangesRequested=3, SentToUpper=4, UpperApproved=5, UpperChangesRequested=6, Completed=7. |

Các enum của lương, hợp đồng, bảo hiểm, nhân sự và công việc tra trực tiếp file entity tương ứng trước khi thay đổi payload; không đổi số enum khi chỉ đổi nhãn tiếng Việt.

## 4. Quan hệ, index và mức bảo đảm

| Ràng buộc | CURRENT trong model/source | Phần cần kiểm chứng/hoàn tất |
| --- | --- | --- |
| Employee ↔ User | One-to-one qua Employee.UserId; xóa User đặt FK null. | Đối soát hồ sơ/tài khoản mồ côi; không nối bằng phone/email như khóa chính. |
| Xóa hồ sơ | Attendance/LeaveRequest/OT/Correction/Ledger trỏ Employee dùng Restrict; nhiều tham chiếu approver dùng SetNull. | Kiểm tra schema thật và tất cả API xóa cứng. |
| Công theo ngày | Check-then-insert ở service. | Unique EmployeeId + ngày nghiệp vụ và xử lý concurrency chưa được bảo đảm tại DB. |
| Rule | Service kiểm tra trùng Code khi tạo, chọn active mới nhất khi tính. | Chưa unique Code/active tại DB, chưa bảo đảm một active khi cập nhật. |
| Đơn và ledger | Index không unique: OT/Correction `(EmployeeId, WorkDate)`, Ledger `(EmployeeId, Year)`. Service kiểm tra Deduct trùng theo đơn. | Chống duyệt đồng thời/trừ lặp cần kiểm thử và ràng buộc/transaction phù hợp. |
| Báo cáo | Unique ReportCode; report → versions/events, version → attachments cascade. | Chưa chứng minh unique `(ReportId, Version)` hoặc kho tệp/DB luôn đồng bộ. |
| Công việc | Index task `(AssigneeId, Status)`, comment TaskId, performance `(EmployeeId, Period)`, member EmployeeId và `(ProjectId, EmployeeId)`. | Các index này không unique. |
| Nhật ký | Index `(EntityType, EntityId)` và CreatedTime. | Append-only là yêu cầu cần giữ; có index không chứng minh chống sửa/xóa audit. |

Đây là ràng buộc **model**, chưa chứng nhận DB live enforce. Quyền quản trị giữ admin cuối, chống tự duyệt và quyền sở hữu là logic ứng dụng; không trình bày như DB constraint.

## 5. Ảnh và tệp

Nguồn: [UploadController.cs](../marixa_aspnet/M.API/Controllers/Files/UploadController.cs), [SupabaseStorageService.cs](../marixa_aspnet/M.API/Storage/SupabasePhotoStorage.cs).

- Cloud: bucket public `marixa-photos`, object thực tế là `{checkin|checkout}/{yyyyMM}/{guid}.{ext}` — **không có** tiền tố `attendance/` dù comment cũ ghi như vậy. URL public được lưu vào Attendance/AttendanceLog; DB không lưu blob/base64 ảnh.
- Local: `/uploads/attendance/{checkin|checkout}/{yyyyMM}/{guid}.{ext}` dưới `wwwroot/uploads`. Thư mục tháng đang lấy `DateTime.Now` của máy chủ, không là ngày công nghiệp vụ.
- Configuration source đọc `Supabase:Url` và `Supabase:ServiceKey`; biến môi trường tương ứng là `SUPABASE__URL` và **`SUPABASE__SERVICEKEY`**. `SUPABASE__SERVICE_KEY` trong comment cũ không ánh xạ đúng tên `ServiceKey`.
- Upload hiện kiểm tra đuôi `.jpg/.jpeg/.png/.webp` và tối đa 5 MiB; chưa chứng minh kiểm tra MIME/nội dung ảnh. Lỗi có thể trả HTTP 200 kèm mã lỗi trong envelope; client cần kiểm tra kết quả nghiệp vụ.
- Trạng thái bucket/quyền, request Storage thành công, URL render và độ bền sau restart **NOT VERIFIED**; xem [08-deploy-detail.md](08-deploy-detail.md). Quyết định public được giữ, không tự chuyển private khi sửa tài liệu.

## 6. Schema, migration và lịch sử kiến trúc

**CURRENT:** [Program.cs](../marixa_aspnet/M.API/Program.cs) có nhánh SQL Server dùng EF migration; PostgreSQL dùng `GenerateCreateScript()` theo từng câu lệnh rồi [PostgresSchemaSync.cs](../marixa_aspnet/M.API/Bootstrap/PostgresSchemaSync.cs). Sync chỉ thêm cột thiếu; không đổi kiểu, xóa/đổi tên cột, backfill hay bảo đảm bổ sung mọi constraint/index. Cột thêm bằng SQL sync cũng không tự kế thừa đầy đủ nullability/default/precision của EF model. Seeder có thể bị catch/log và bỏ qua; boot lên không chứng minh schema/seed hoàn tất.

**TARGET/PENDING khi thay đổi schema:** đánh giá dữ liệu hiện có → migration đúng provider → backup → backfill/constraint → kiểm thử → rollback có kiểm chứng. Đổi kiểu giờ/độ chính xác, unique công/ngày và sửa ngày UTC cần kế hoạch dữ liệu riêng. Không xóa DB để “sửa sync” khi chưa có quyền cụ thể.

`marixa_aspnet/web` hiện không có trong working tree. SQL dưới [supabase/migrations](../marixa_aspnet/supabase/migrations) là tài sản lịch sử của thiết kế Next.js/Supabase Auth/RLS; không coi các bảng/RPC khóa kỳ của thiết kế đó là schema đang dùng bởi EF. Đường chạy hiện tại dùng ASP.NET Identity/JWT và EF; không giả định Supabase Auth hoặc RLS đã bảo vệ API ứng dụng. Cấu hình Data API/grants/RLS thực tế cần kiểm tra riêng theo [08-deploy-detail.md](08-deploy-detail.md).

Xem [02-business-analysis.md](02-business-analysis.md), [05-workflow.md](05-workflow.md) và [06-testing.md](06-testing.md) trước khi thay đổi dữ liệu hoặc trạng thái.
