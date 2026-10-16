# 05 — Luồng nghiệp vụ và hợp đồng API Marixa

> Chủ tài liệu: **Đinh Văn Tài**. Cập nhật: **16/10/2026**.
> **CURRENT**: đối chiếu controller, DTO, service và frontend trong repo. Chưa chạy các luồng runtime ở lượt chuẩn hóa tài liệu.

## 1. Cách đọc và quy tắc chung

Quyền cập nhật, thứ tự nguồn và trạng thái theo [00-README.md](00-README.md). Quy tắc cần đạt nằm ở [02-business-analysis.md](02-business-analysis.md); model/enum ở [04-database-design.md](04-database-design.md). Luồng dưới đây không mặc định đã đạt nghiệm thu chỉ vì có source.

**EN — AI contract:** Follow the current route/DTO/service, then compare it with the recorded business target. Enforce identity and ownership on the server. Do not invent draft, cancellation, period locking, GPS persistence or approved-OT integration where source does not implement them. Preserve raw attendance events and record correction provenance.

- Frontend gọi qua [axiosClient.js](../marixa_react/src/services/api/axiosClient.js); các route bên dưới tính từ `/api`. Gửi Bearer token cho endpoint được bảo vệ.
- Phản hồi thường là `BaseResponse<T>` với `data`, `message`, `statusCode`, `code`; đọc [BaseResponse.cs](../marixa_aspnet/M.Core/Base/BaseResponse.cs). Dữ liệu phân trang dùng `BasePaginatedList<T>`. Lỗi validation tự động của ASP.NET có thể khác envelope; không giả định mọi lỗi cùng định dạng.
- Kiểm tra **HTTP status và kết quả nghiệp vụ**. Riêng upload hiện dùng `Ok(...)` cả khi thất bại, nên HTTP 200 không đủ chứng minh ảnh đã lưu.
- GUID người thao tác có trong body hiện tại không được coi là đáng tin chỉ vì tồn tại trong DB. Với endpoint chưa đối chiếu claim/người thao tác, ghi nhận là khoảng trống cần sửa; chưa chứng nhận an toàn quyền sở hữu.

## 2. Chấm vào/ra và ảnh

Nguồn: [AttendanceCard.jsx](../marixa_react/src/modules/me/components/AttendanceCard.jsx), [relatedApi.js](../marixa_react/src/modules/me/api/relatedApi.js), [AttendanceController.cs](../marixa_aspnet/M.API/Controllers/Timekeeping/AttendanceController.cs), [AttendanceService.cs](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceService.cs).

### 2.1. Trình tự CURRENT

1. UI đọc employee từ phiên đăng nhập và tải `GET /Attendance/by-employee/{employeeId}`. Trang chọn record “hôm nay” theo ngày của trình duyệt.
2. UI cho chụp/preview ảnh và hiển thị tọa độ GPS nếu trình duyệt cấp quyền. Nút vào/ra hiện bị khóa khi chưa có ảnh mới; đây là sai khác với quyết định bằng chứng tùy chọn.
3. Upload trước bằng `POST /Upload/photo?type=checkin` hoặc `checkout`, body multipart có trường `file`. Không tự đặt `Content-Type` thiếu boundary.
4. Có URL ảnh thì gọi `POST /Attendance/checkin`. Cùng endpoint cho cả vào và ra; `type=1` là vào, `type=2` là ra.
5. Service kiểm tra Employee tồn tại/chưa xóa; lấy giờ máy chủ UTC, tìm/tạo Attendance bằng **ngày UTC**. Chưa đối chiếu `employeeId` body với claim trong đường này.
6. Nếu đã có log cùng loại trong 2 phút gần nhất: trả `alreadyRecorded=true`, không thêm log. Ngoài cửa sổ đó service có thể thêm log tiếp; không coi mỗi ngày bị DB giới hạn đúng hai log.
7. Ghi log, đồng bộ URL ảnh vào ngày công. Giờ vào là log CheckIn sớm nhất, giờ ra là CheckOut muộn nhất; override nếu có được ưu tiên khi enrich. Tính giờ thực/OT, lưu và trả record mới nhất.
8. UI xóa ảnh chờ, tải lại lịch sử. Upload và ghi công là hai request riêng; upload thành công nhưng checkin thất bại có thể để lại ảnh chưa gắn công. Chưa có transaction/cleanup chung giữa hai bước.

### 2.2. Request/response

Ví dụ body theo DTO hiện tại; thay GUID bằng hồ sơ test được phép thao tác:

```json
{
  "employeeId": "11111111-1111-1111-1111-111111111111",
  "type": 1,
  "method": 4,
  "photoUrl": null,
  "note": "Ví dụ hợp đồng API"
}
```

`method=4` là Phone; `photoUrl` và note nullable. Backend hiện nhận không ảnh, dù UI chặn. **DTO checkin không có latitude/longitude/deviceId**; tọa độ hiển thị trên UI chưa được ghi vào `AttendanceLog` qua luồng này. Chưa có bằng chứng geofence/gắn cờ ngoài văn phòng đã chạy.

`data` trả về gồm `alreadyRecorded`, `message`, `attendance`; đây là cấu trúc kết quả, không phải bản response live đã thu thập. Record Attendance có trạng thái, giờ vào/ra, ảnh, actualHours và overtimeHours; trường OT là giá trị tính lại, không lưu cột DB.

### 2.3. Công thức CURRENT và giới hạn

Trong [AttendanceTimeCalculator.cs](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceTimeCalculator.cs): đổi mốc sang UTC+7 → lấy giờ/phút → tính khoảng làm → trừ phần giao với cửa sổ nghỉ bắt đầu cố định **12:00**, dài `BreakMinutes` của rule → `ActualHours = Math.Round(netMinutes / 60.0)` thành số nguyên. `OvertimeHours = Math.Round(max(0, outMinute - standardEndMinute) / 60, 1)`.

Với rule ra 17:00, nghỉ 60 phút: 08:00–17:00 trả 8 giờ, OT 0; 08:00–18:00 trả 9 giờ, OT 1. Đây là cách calculator hiện tính, **chưa phải OT đã được duyệt**. Hàm trả null khi thiếu mốc hoặc ra trước vào; có xử lý chênh giờ qua nửa đêm nhưng bỏ phần ngày nên ca dài/qua nhiều ngày và nghỉ/OT qua đêm cần kiểm thử riêng. Quy tắc làm tròn cần được thống nhất trước khi đổi.

Status lúc chấm vào dùng ca kế hoạch nếu có, nếu không dùng mốc cố định 08:00 của [AttendanceStatusEvaluator.cs](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceStatusEvaluator.cs); chỉ gán khi Status còn null. Không khẳng định `AttendanceRule.CheckInTime`, grace hoặc ngưỡng về sớm đã được áp dụng đầy đủ. Mặc định evaluator/entity ra 16:30 còn seeder/calculator dùng 17:00.

**TARGET/PENDING:** ngày Việt Nam nhất quán giữa server/UI; bằng chứng tùy chọn đúng quyết định; dữ liệu GPS xuyên suốt nếu thu thập; một rule chung được tôn trọng sau restart; quyền sở hữu, retry và đồng thời được bảo vệ ở backend/DB.

## 3. Đối soát và duyệt ngày công

| Endpoint CURRENT | Quyền khai báo | Hành vi chính |
| --- | --- | --- |
| `GET /Attendance/get-all` | Admin/Manager/HR | Danh sách phân trang. |
| `GET /Attendance/get-by-id/{id}` | Đăng nhập | Truy vấn ID; chưa thấy kiểm tra chủ sở hữu. |
| `GET /Attendance/by-employee/{employeeId}` | Đăng nhập | Truy vấn theo nhân viên; chưa thấy kiểm tra chủ sở hữu. |
| `POST /Attendance/create`, `PUT /Attendance/update` | Admin/Manager/HR | Tạo/sửa ngày công; update từ chối record đang Approved. |
| `POST /Attendance/approve` | Admin/Manager/HR | Body gồm id, approvalStatus, approvedBy, status, note. Yêu cầu Status hợp lệ và approver tồn tại. |
| `DELETE /Attendance/soft-delete/{id}`, `/Attendance/delete/{id}` | Admin/Manager/HR | Xóa mềm/xóa cứng; không coi dữ liệu ngày/log bất biến toàn hệ thống. |

`ApproveAsync` hiện chưa chặn tự duyệt hoặc ràng buộc `ApprovedBy` với claim; có thể đổi trạng thái duyệt theo body. Trạng thái Approved của ngày công **khác khóa kỳ**. Chặn update trực tiếp không chứng minh mọi đường checkin/correction/delete cũng đã chặn thay đổi record Approved.

**TARGET/PENDING:** HR đối soát thiếu mốc, thời gian, ảnh và ngoại lệ có lý do; người xử lý phải đúng quyền và được truy vết. Danh sách “đã kiểm tra”, đối soát thiết bị/máy chủ, cờ ngoài văn phòng và khóa kỳ chưa được coi là chức năng hoàn chỉnh.

## 4. Yêu cầu sửa công

Nguồn: [AttendanceCorrectionController.cs](../marixa_aspnet/M.API/Controllers/Timekeeping/AttendanceCorrectionController.cs), [AttendanceCorrectionService.cs](../marixa_aspnet/M.Services/Services/Timekeeping/AttendanceCorrectionService.cs).

| Bước CURRENT | API và dữ liệu | Kết quả/giới hạn |
| --- | --- | --- |
| Tạo | `POST /AttendanceCorrection/create`: employeeId, attendanceId?, workDate, proposedCheckIn?, proposedCheckOut?, proposedStatus?, reason?. | Tạo Pending trực tiếp; không có Draft lưu DB. Phải có ít nhất một giá trị đề nghị; hai mốc cùng có thì ra phải sau vào. |
| Snapshot đầu | Nếu có attendanceId tồn tại, lưu `BeforeJson`. | Snapshot tại lúc tạo yêu cầu, có thể khác trạng thái ngay trước lúc duyệt nếu công đã đổi sau đó. |
| Xem | `get-by-id/{id}`, `by-employee/{employeeId}` cho đăng nhập; `get-all` cho Admin/Manager/HR. | Quyền sở hữu cần rà lại ở backend. |
| Duyệt/từ chối | `POST /AttendanceCorrection/review` cho Admin/Manager/HR: id, status, reviewedBy, reviewNote?. | Chỉ xử lý khi còn Pending; kiểm tra reviewer tồn tại. HR/Admin có ngoại lệ tự duyệt trong code. |
| Áp dụng khi Approved | Ghi proposedCheckIn/Out/Status vào override của Attendance liên quan; lưu ChangeSummary và `AfterJson`. | Giữ log gốc; không tạo Attendance mới khi không có bản liên quan; chưa chặn Attendance Approved trong đường này. |

Enum có Cancelled nhưng controller hiện không có endpoint hủy riêng. Không suy ra mọi giá trị enum là transition được nghiệm thu. Snapshot của correction không thay thế audit toàn hệ thống hoặc snapshot kỳ công.

**TARGET/PENDING:** bảo vệ employee/attendance cùng chủ và ngày, chống người duyệt giả mạo, xử lý thay đổi công giữa lúc gửi và duyệt, bảo toàn log và audit đầy đủ. Tài liệu cũ yêu cầu chống tự duyệt; ngoại lệ HR/Admin hiện có cần chủ dự án xác nhận theo loại đơn trước khi thay đổi, không tự coi code hoặc mô tả cũ là quyết định cuối cùng.

## 5. Đơn nghỉ và sổ phép

Nguồn: [LeaveRequestController.cs](../marixa_aspnet/M.API/Controllers/Leave/LeaveRequestController.cs), [LeaveRequestService.cs](../marixa_aspnet/M.Services/Services/Leave/LeaveRequestService.cs), [LeaveLedgerService.cs](../marixa_aspnet/M.Services/Services/Leave/LeaveLedgerService.cs).

### 5.1. Trạng thái CURRENT

| Từ | Thao tác | Đến | Điều kiện/kết quả |
| --- | --- | --- | --- |
| Chưa có | `POST /LeaveRequest/create` | Pending | Người có hồ sơ; nhân viên Probation/Working/OnLeave; loại active; ngày hợp lệ; không giao đơn Pending/Approved. Chủ đơn hoặc role được service cho phép tạo hộ. |
| Pending | `PUT /LeaveRequest/update` | Pending | Chủ đơn hoặc Admin/HR/Manager; kiểm tra ngày/loại/khoảng và hạn mức; xóa thông tin duyệt cũ. |
| Pending | `POST /LeaveRequest/approve/{id}` | Approved | Admin/HR/Manager đúng điều kiện service; không tự duyệt; loại có MaxDays thì kiểm tra hạn mức và tạo Deduct nếu chưa có. |
| Pending | `POST /LeaveRequest/reject/{id}` | Rejected | Cùng điều kiện quyền duyệt; không tạo Deduct. |
| Pending | `POST /LeaveRequest/cancel/{id}` | Cancelled | Chủ đơn hoặc Admin; nếu tồn dư Deduct thì có nhánh hoàn. |
| Approved/Rejected/Cancelled | Sửa/hủy qua luồng thường | Bị từ chối | `INVALID_STATUS`; **chưa có luồng hủy Approved và hoàn phép thông thường**. |

Create dùng employeeId, leaveTypeId, fromDate, toDate, totalDays?, reason?. Nếu thiếu totalDays, service lấy số ngày lịch gồm hai đầu; chưa tự trừ Chủ nhật/ngày lễ. Hạn mức dựa trên `LeaveType.MaxDays` và Deduct/Refund; giữ chính sách hiện tại, không tự thêm lịch cấp phép.

Đọc cá nhân bằng `GET /LeaveRequest/mine`; `GET /LeaveRequest/{id}` cho chủ đơn hoặc Admin/HR. `by-employee/{employeeId}` cho Admin/Manager/HR. `get-all` hiện chỉ yêu cầu đăng nhập, service trả mọi đơn chưa xóa; đây là khoảng trống quyền cần sửa. Đường ledger-summary của controller cho Admin/HR.

Service chặn tự duyệt mọi role; HR bị chặn khi chủ đơn có HR hoặc Manager; Manager bị chặn khi chủ đơn có HR hoặc Admin. Code xét từng cờ role nên người nhiều role cần kiểm thử, không suy ra Admin luôn vượt mọi điều kiện trong tổ hợp role. ApprovedBy trong luồng Leave lấy từ claim employeeId, khác OT/correction nhận GUID qua body.

### 5.2. Ledger và phần chưa làm

Deduct hiện do service kiểm tra tồn tại trước khi tạo, không có bằng chứng unique constraint chống hai lần duyệt đồng thời. Cách tổng hợp số dư giữa hai service còn khác nhau về Adjust âm/xóa mềm, nêu tại [04-database-design.md](04-database-design.md). Cần kiểm thử retry/concurrency và đối soát số dư, không đánh dấu “idempotent tuyệt đối” từ truy vấn kiểm tra trước.

**TARGET/PENDING:** luồng hủy sau duyệt và hoàn phép cần đặc tả/triển khai riêng; PDF đơn nghỉ chưa có bằng chứng triển khai. Không nhầm tệp đính kèm báo cáo nhân viên với PDF đơn nghỉ được hệ thống tạo.

## 6. Yêu cầu tăng ca

Nguồn: [OvertimeRequestController.cs](../marixa_aspnet/M.API/Controllers/Leave/OvertimeRequestController.cs), [OvertimeRequestService.cs](../marixa_aspnet/M.Services/Services/Leave/OvertimeRequestService.cs).

1. `POST /OvertimeRequest/create` nhận employeeId, attendanceId?, workDate, startAt?, endAt?, overtimeHours?, reason?. Tạo Pending; chặn một Pending cùng nhân viên/ngày bằng truy vấn. Nếu không nhập overtimeHours, service tính khoảng giờ và làm tròn 2 chữ số khi có cặp hợp lệ.
2. `PUT /OvertimeRequest/update` chỉ nhận record Pending. API create/update yêu cầu đăng nhập; source chưa kiểm tra quyền sở hữu đầy đủ.
3. `POST /OvertimeRequest/review` cho Admin/Manager/HR, nhận id, status, approvedBy, note?. Chỉ xử lý Pending; reviewer phải tồn tại; HR/Admin có ngoại lệ tự duyệt hiện hành trong code. Chưa ràng buộc approvedBy body với claim.
4. Review chỉ cập nhật đơn; **không cập nhật công thức OT của Attendance**. Công thức ngày công vẫn tính giờ ra sau mốc chuẩn kể cả chưa có đơn Approved.

**TARGET/PENDING — cần quyết định:** tài liệu cũ đề xuất chỉ ghi nhận OT sau duyệt và lấy phần giao với thời gian làm thực tế; source/lịch sử cũng có OT tự động. Chủ dự án cần chốt OT tự động, có duyệt hay kết hợp theo loại ngày trước khi sửa code. Chưa gọi hai đại lượng hiện tại là đồng nhất. Cần kiểm thử khoảng giờ thiếu/âm/qua ngày, số giờ nhập tay và liên kết Attendance sai chủ. Enum Cancelled không chứng minh có endpoint hủy riêng.

## 7. Chốt kỳ, offline và báo cáo

| Chủ đề | Trạng thái |
| --- | --- |
| Excel hồ sơ HR; CSV công/admin | **CURRENT:** có code import/export hoặc xuất dữ liệu hiện thời. Kiểm tra chính xác màn hình/định dạng trước khi dùng. |
| Báo cáo nhân viên | **CURRENT:** có tệp, version và event riêng; không là snapshot bảng công. |
| HR kiểm tra → Admin khóa → snapshot kỳ → mở lại có lý do | **TARGET/PENDING:** chưa có đường chạy hoàn chỉnh trong ASP.NET/React. |
| XLSX bảng công theo snapshot/PDF đơn nghỉ | **TARGET/PENDING:** không coi CSV hoặc tệp báo cáo upload là chức năng này. |
| Offline/IndexedDB queue, đồng bộ lại bằng event ID | **TARGET/PENDING:** chưa làm. Khi lỗi mạng hiện cần báo lỗi và tải lại dữ liệu, không hứa đã xếp hàng offline. |

## 8. Lỗi và nghiệm thu

| Tình huống | CURRENT hoặc kỳ vọng cần kiểm tra |
| --- | --- |
| Employee/record không tồn tại | Service thường dùng 404 `NOT_FOUND`. |
| Trùng ngày công/OT Pending | 400 `DUPLICATE`; cần kiểm thử race, không coi DB đã khóa trùng. |
| Log cùng loại trong 2 phút | Success với `alreadyRecorded=true`; không phải HTTP error. |
| Leave chồng khoảng | 400 `OVERLAP`. |
| Leave thiếu hạn mức | Create/update dùng 400, approve dùng 409 `INSUFFICIENT_BALANCE`. |
| Leave tự duyệt | 403 `SELF_APPROVAL_NOT_ALLOWED`. |
| Sửa/hủy đơn đã xử lý | Leave dùng `INVALID_STATUS`; OT/correction dùng `INVALID_STATE`. |
| Ảnh không hợp lệ hoặc upload thất bại | Upload có thể HTTP 200 với `statusCode/code/message` báo lỗi; cần kiểm tra client không dùng chuỗi lỗi làm URL. |
| Token thiếu/hết hạn, dữ liệu của người khác | Cần test HTTP status và không có thay đổi dữ liệu trái phép; chưa chứng nhận mọi API đã chặn. |

Kịch bản, dữ liệu test và tiêu chí đạt nằm tại [06-testing.md](06-testing.md). Khi sửa luồng, cập nhật hợp đồng này và ghi bằng chứng trong [worklog.md](worklog.md); trạng thái việc tiếp theo giữ tại [01-plan.md](01-plan.md).
