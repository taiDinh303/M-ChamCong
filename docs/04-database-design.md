# 04 — Thiết kế dữ liệu Marixa

> Chủ tài liệu: **Đinh Văn Tài**. Cập nhật: **17/10/2026**.
> Trạng thái: **CURRENT** theo EF model và service trong repo; schema/dữ liệu của môi trường live **NOT VERIFIED**.

## 1. Nguồn và quy ước

Nguồn model chính là [DatabaseContext.cs](../marixa_aspnet/M.Repositories/Context/DatabaseContext.cs), [Entities](../marixa_aspnet/M.Contract.Repositories/Entities) và [AuthEntities](../marixa_aspnet/M.Contract.Repositories/AuthEntities). Một file có thể chứa nhiều entity: `EmployeeReport.cs` chứa report/version/attachment/event; `WorkTask.cs` chứa task/comment. Không dùng quy tắc “một file = một bảng”.

Tên dưới đây là **tên entity**, không bảo đảm tên vật lý giống hệt; đối chiếu EF mapping và catalog của đúng môi trường trước khi viết SQL. Quyền cập nhật và nghĩa các trạng thái theo [00-README.md](00-README.md).

**EN — AI contract:** Treat EF entities/context as the current model, not proof of the deployed schema. Check service validation, database constraints and API ownership separately. Preserve the simple shared active rule, public photo bucket and existing leave policy. Schema changes require an explicit migration/backfill/rollback plan; startup column sync is not a complete migration system.

### 1.1. Kiểu dữ liệu và khóa

- Entity nghiệp vụ kế thừa [BaseEntity.cs](../marixa_aspnet/M.Core/Base/BaseEntity.cs): `Id: Guid` là PK; `CreatedTime`/`LastUpdatedTime: DateTimeOffset`; `DeletedTime: DateTimeOffset?`; các trường `CreatedBy`, `LastUpdatedBy`, `DeletedBy` nullable. `Id` khởi tạo bằng `Guid.NewGuid()`.
- Identity dùng các entity và khóa của ASP.NET Identity; không suy ra toàn bộ Identity có cấu trúc `BaseEntity`.
- Dấu `?` trong bảng là nullable theo C# model. EF-generated constraint và constraint thực tế sau schema sync có thể khác nhau; chưa xác minh live.
- `DateTimeOffset` dùng cho mốc chấm và audit. `DateTime` còn dùng cả thời điểm và ngày nghiệp vụ. BusinessTime chuẩn hóa input: date-only midnight giữ nhãn lịch UTC-kind; timestamp không offset hiểu theo VN. Service checkin dùng ngày VN riêng; không dịch lại lịch sử một cách máy móc.
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
| `AttendanceRule` | Code/Name; giờ vào/ra, giờ chuẩn/nghỉ/grace; PhotoRequired/GpsRequired/IsActive. | Unique filtered active, replacement transaction, default ra 17:00; không version/ngày hiệu lực. |
| `Shift` | Code, Name, `StartTime/EndTime: TimeOnly`, `StandardHours/BreakMinutes/WorkDays: int`, `IsNight/IsActive: bool` | Có schema/CRUD; V1 vẫn giữ quyết định một ca chung. |
| `EmployeeShift` | `EmployeeId/ShiftId: Guid`, `EffectiveFrom: DateTime`, `EffectiveTo: DateTime?` | FK Employee và Shift dùng Restrict. |

### 3.2. Công và sự kiện chấm

| Entity | Trường/kiểu trọng yếu | Ràng buộc hoặc lưu ý CURRENT |
| --- | --- | --- |
| `Attendance` | EmployeeId: Guid, AttendanceDate: DateTime, Status?, ApprovalStatus. | Unique EmployeeId+AttendanceDate khi DeletedTime null; ngày VN, cần áp index lên DB thật. |
| `Attendance` | CheckIn/Out DateTimeOffset?, PlannedShiftId Guid?, PlannedHours int?, ActualHours decimal(10,2)? | ActualHours công thường; OT transient từ Approved windows. |
| `Attendance` | `CheckInPhoto/CheckOutPhoto: string(500)?`, `ApprovedBy: Guid?`, `ApprovedAt: DateTime?`, `ChangeSummary: string(500)?`, `Note: string?` | ApprovedBy trỏ Employee. `OvertimeHours: decimal?` là `[NotMapped]`, được tính lại khi đọc. |
| `AttendanceLog` | `AttendanceId: Guid`, `LogTime: DateTimeOffset`, `Type: AttendanceLogType`, `Method: AttendanceMethod` | Quan hệ Attendance 1:n log; cascade khi xóa cứng Attendance. Chặn trùng 2 phút ở service, chưa là unique/idempotency key tại DB. |
| `AttendanceLog` | PhotoUrl?, Latitude/Longitude decimal(10,7)?, DeviceId string(100)?, Note?. | DTO checkin nhận/lưu GPS/device tùy chọn và kiểm bounds. Frontend chưa gửi. |
| `AttendanceLog` | `IsAdjusted: bool`, `AdjustedBy: string?`, `AdjustedAt: DateTimeOffset?`, `AdjustmentNote: string?` | Có model chỉnh log; cần kiểm tra các đường CRUD trước khi khẳng định event bất biến. |
| `AttendanceCorrection` | `EmployeeId: Guid`, `AttendanceId: Guid?`, `WorkDate: DateTime`; `ProposedCheckIn/ProposedCheckOut: DateTimeOffset?`, `ProposedStatus: AttendanceStatus?` | Tạo yêu cầu phải có ít nhất một giá trị đề nghị; hai mốc nếu cùng có thì ra sau vào. |
| `AttendanceCorrection` | `Reason/ReviewNote: string(500)?`, `Status`, `ReviewedBy: Guid?`, `ReviewedAt: DateTime?`, `BeforeJson/AfterJson: text?` | Duyệt áp dụng override vào Attendance liên quan, giữ AttendanceLog; nếu không có Attendance liên quan thì không tự tạo ngày công. |

### 3.3. Đơn nghỉ, ledger và OT

| Entity | Trường/kiểu trọng yếu | Ràng buộc hoặc lưu ý CURRENT |
| --- | --- | --- |
| `LeaveType` | Code, Name bắt buộc; `MaxDays: decimal?`, `IsPaid/IsActive: bool` | `MaxDays` có giá trị là điều kiện service kiểm tra hạn mức và tạo Deduct. Không có cờ riêng “trừ phép năm”. |
| `LeaveRequest` | `EmployeeId/LeaveTypeId: Guid`, `FromDate/ToDate: DateTime`, `TotalDays: decimal(10,2)?`, Reason, Status | Service kiểm tra ngày, loại active, trùng khoảng Pending/Approved; mặc định số ngày là khoảng ngày gồm hai đầu. Không tự suy ra đã trừ cuối tuần/ngày lễ. |
| `LeaveRequest` | ApprovedBy/At giữ quyết định; CancelledBy/At/CancellationReason ghi hủy riêng | Approved hủy có quyền/lý do, Refund một lần; không xóa request đã quyết định. |
| `LeaveLedger` | `EmployeeId: Guid`, `Year: int`, `LeaveTypeId/LeaveRequestId: Guid?`, `EntryType`, `Days: decimal(10,2)`, `Reason: string(500)?` | Deduct/Refund do service tạo dùng lượng ngày dương, ý nghĩa cộng/trừ theo EntryType. Adjust có thể có dấu. Không dựa vào comment “Days âm khi trừ”. |
| `OvertimeRequest` | Employee/Attendance/WorkDate và StartAt/EndAt, requested OvertimeHours decimal | Create/review phải có window hợp lệ≤24 giờ; recognizedOT từ giao actual/window, không số nhập tay. |
| `OvertimeRequest` | Employee/Attendance/WorkDate và StartAt/EndAt, requested OvertimeHours decimal | Create/review phải có window hợp lệ≤24 giờ; recognizedOT từ giao actual/window, không số nhập tay. |

Hai đường summary đã dùng chung LeaveBalanceCalculator: Adjust có dấu, bỏ entry xóa mềm, used tối thiểu 0. Có test hồi quy; không thay chính sách phép. Kiểm tra hạn mức duyệt nghỉ hiện dựa vào `MaxDays` và Deduct/Refund theo năm/loại, không chứng minh đang dùng số dư Grant theo chính sách cộng dồn.

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
| Công theo ngày | Unique filtered UX_Attendances_Employee_Date + transaction checkin. | Dữ liệu trùng/ngày UTC cũ cần preflight; runtime concurrency hai provider chưa thử. |
| Rule | Unique filtered UX_AttendanceRules_Active khi active/chưa xóa; service atomic replacement. | Chưa unique Code; constraint không tự tạo active nếu DB rỗng. |
| Đơn và ledger | Unique UX_LeaveLedger_Request_Year_Type cho Deduct/Refund có request, kể cả xóa mềm; Serializable approval/cancel. | Preflight duplicate + provider test, không sửa/xóa lịch sử deduction/refund. |
| Báo cáo | Unique ReportCode; report → versions/events, version → attachments cascade. | Chưa chứng minh unique `(ReportId, Version)` hoặc kho tệp/DB luôn đồng bộ. |
| Công việc | Index task `(AssigneeId, Status)`, comment TaskId, performance `(EmployeeId, Period)`, member EmployeeId và `(ProjectId, EmployeeId)`. | Các index này không unique. |
| Nhật ký | Index `(EntityType, EntityId)` và CreatedTime. | Append-only là yêu cầu cần giữ; có index không chứng minh chống sửa/xóa audit. |

Đây là ràng buộc **model**, chưa chứng nhận DB live enforce. Quyền quản trị giữ admin cuối, chống tự duyệt và quyền sở hữu là logic ứng dụng; không trình bày như DB constraint.

## 5. Ảnh và tệp

Nguồn: [UploadController.cs](../marixa_aspnet/M.API/Controllers/Files/UploadController.cs), [SupabasePhotoStorage.cs](../marixa_aspnet/M.API/Storage/SupabasePhotoStorage.cs).

- Cloud: bucket public `marixa-photos`, object thực tế là `{checkin|checkout}/{yyyyMM}/{guid}.{ext}` — **không có** tiền tố `attendance/` dù comment cũ ghi như vậy. URL public được lưu vào Attendance/AttendanceLog; DB không lưu blob/base64 ảnh.
- Local: `/uploads/attendance/{checkin|checkout}/{yyyyMM}/{guid}.{ext}` dưới `wwwroot/uploads`. Thư mục tháng lấy theo BusinessTime UTC+7 qua TimeProvider.
- Configuration source đọc `Supabase:Url` và `Supabase:ServiceKey`; biến môi trường tương ứng là `SUPABASE__URL` và **`SUPABASE__SERVICEKEY`**. `SUPABASE__SERVICE_KEY` còn được adapter đọc như alias; cấu hình mới dùng tên chuẩn.
- Upload kiểm extension, 5 MiB và signature JPEG/PNG/WebP; không gọi đó là quét malware hoặc giải mã ảnh đầy đủ. HTTP invalid 400/storage 502, không giả thành công 200.
- Trạng thái bucket/quyền, request Storage thành công, URL render và độ bền sau restart **NOT VERIFIED**; xem [08-deploy-detail.md](08-deploy-detail.md). Quyết định public được giữ, không tự chuyển private khi sửa tài liệu.

Báo cáo và bàn giao dùng IPrivateDocumentStorage, ngoài web root dưới App_Data; API kiểm owner/manager trước mở stream. Đường public legacy /uploads/handovers bị chặn, có fallback đọc có quyền cho file cũ. File và DB không có distributed transaction: write batch dọn file khi rollback; crash giữa hai kho vẫn cần đối soát/backup.

## 5.1. Dữ liệu bổ sung 17/10

AttendancePeriods có YearMonth unique, State/Open 0/Reviewed 1/Locked 2, CurrentVersion, reviewer/reason/time. Snapshots có PeriodId+Version unique, PayloadJson/Sha 256; Events có actor/action/reason/time, FK Restrict. Domain factory/private setters kiểm transition; SaveChanges bảo vệ tháng cũ/mới bị mutation. Snapshot/events không update/delete.

AttendanceRule thêm BreakStartTime mặc định 12:00, BreakMinutes default 60 cho record mới; rule cũ giữ duration đã cấu hình. LeaveRequest thêm CancelledBy/At/CancellationReason, giữ Approver cũ. EmployeeHandover thêm AppliedAt; EmployeePromotion đã có AppliedAt, nay dùng đúng ngày hiệu lực. ActualHours chuyển decimal, không rewrite snapshot cũ.

## 6. Schema, migration và lịch sử kiến trúc

**CURRENT:** DatabaseInitializer opt-in (production mặc định tắt). SQL Server dùng EF migration; PostgreSQL tạo bảng → ADD columns → constraints/index và chỉ bỏ qua lỗi duplicate object đã biết. Lỗi quan trọng fail startup. ADD sync chỉ nullable theo type EF, required thiếu thì fail; không đổi kiểu/rename/backfill/rollback. Migration có snapshot/SQL review và fixture SQL Server; PostgreSQL runtime theo CI/worklog, live chưa áp; preflight theo [08](08-deploy-detail.md).

**TARGET/PENDING khi thay đổi schema:** đánh giá dữ liệu hiện có → migration đúng provider → backup → backfill/constraint → kiểm thử → rollback có kiểm chứng. Đổi kiểu giờ/độ chính xác, unique công/ngày và sửa ngày UTC cần kế hoạch dữ liệu riêng. Không xóa DB để “sửa sync” khi chưa có quyền cụ thể.

`marixa_aspnet/web` hiện không có trong working tree. SQL dưới [supabase/migrations](../marixa_aspnet/supabase/migrations) là tài sản lịch sử của thiết kế Next.js/Supabase Auth/RLS; không coi các bảng/RPC khóa kỳ của thiết kế đó là schema đang dùng bởi EF. Đường chạy hiện tại dùng ASP.NET Identity/JWT và EF; không giả định Supabase Auth hoặc RLS đã bảo vệ API ứng dụng. Cấu hình Data API/grants/RLS thực tế cần kiểm tra riêng theo [08-deploy-detail.md](08-deploy-detail.md).

Xem [02-business-analysis.md](02-business-analysis.md), [05-workflow.md](05-workflow.md) và [06-testing.md](06-testing.md) trước khi thay đổi dữ liệu hoặc trạng thái.
