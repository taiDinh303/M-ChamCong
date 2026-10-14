1 — Phân tích nghiệp vụ hệ thống chấm công Marixa

> Tác giả & chủ tài liệu: **Đinh Văn Tài**.
> Đối tượng đọc: AI (Agnes) khi được yêu cầu đọc dự án, và developer.
> Mục đích: mô tả phạm vi, người dùng, quyền và quy tắc nghiệp vụ của hệ thống chấm công Marixa V1, **gắn với mã nguồn đang chạy** trong repo (backend `marixa_aspnet/M.API` .NET 8, frontend `marixa_react` React 19 + Vite).

## 0. Cách đọc file này (dành cho AI)

- File này là "bản đồ nghiệp vụ": đọc trước khi đụng vào code, để biết **ai làm gì, được quyền gì, các quy tắc nào là bắt buộc**.
- Nếu file này khác với `docs/01-plan.md` (mục quyết định đang hiệu lực), **mục 2 của `01-plan.md` thắng** — đó là quy tắc hiện hành.
- Nếu cần xem các module/trang/chức năng đã có hay chưa: đọc `docs/worklog.md` (bảng ma trận theo trang & module).
- Các file đặc tả khác: `02` dữ liệu, `03` luồng, `04` kiến trúc, `05` test, `06`+`08` triển khai, `07` UI.

## 1. Mục tiêu và phạm vi

Marixa cần một hệ thống chấm công cho **khối văn phòng (quy mô nhỏ, ~dưới 15 người)**. Nhân viên chấm vào/ra bằng web (desktop hoặc mobile, có hỗ trợ camera + GPS); HR đối soát; Admin quản trị cấu hình, tài khoản và khóa kỳ.

Giao diện và thuật ngữ dùng **tiếng Việt**, múi giờ nghiệp vụ là `Asia/Ho_Chi_Minh` (UTC+7).

### Có trong V1 (đã code)
- Hồ sơ nhân viên, phòng ban, vị trí, hợp đồng, bảo hiểm, thuế, lương cơ bản, tài khoản ngân hàng.
- Chấm công vào/ra có chụp ảnh (camera) và GPS; ghi log vào/ra; tính giờ thực tế + tăng ca theo quy tắc chấm công.
- Lịch sử chấm công, thống kê công, quy định chấm công (tự động).
- Nghỉ phép (đơn + sổ phép), tăng ca (đơn), sửa công (yêu cầu điều chỉnh).
- Bảng lương (payroll), hợp đồng, thăng chức, bàn giao khi nghỉ việc, báo cáo của nhân viên.
- Công việc (work project / task / performance).
- Quản trị: cấp/khóa tài khoản, quản lý role, dashboard admin.

### Không có trong V1 (scope ngoài)
- Không tính thuế phức tạp / khai báo BHXH tự động, không công trường/công nhân/điểm danh đội.
- Không mô-đun kho, xuất nhập khẩu, CRM.
- Không có hệ đa ca riêng cho từng nhân viên (V1 dùng **một ca làm chung** cho toàn công ty, do admin cấu hình).

## 2. Người dùng và vai trò (role)

Hệ thống hiện dùng **4 role** (xem `M.API/Seed/RoleSeeder.cs` và `marixa_react/src/constants/modules.js`):

| Role | Mô tả | Quyền chính |
| --- | --- | --- |
| `Employee` | Nhân viên | Chấm công, xem hồ sơ/công/đơn của mình, gửi đơn nghỉ/tăng ca/sửa công, xem bảng lương của mình. |
| `HR` | Nhân sự | Tạo/sửa hồ sơ, đối soát & duyệt chấm công, duyệt đơn nghỉ/tăng ca, quản lý hợp đồng/ascendant/bàn giao. |
| `Manager` | Quản lý | Nhóm quyền tương đương HR ở các module nhân sự; tham gia cấp tài khoản, duyệt một số luồng. |
| `Admin` | Quản trị | Toàn quyền: cấp/khóa tài khoản, quản lý role, cấu hình ca làm/lịch/chính sách, khóa/mở kỳ, xem báo cáo & audit toàn hệ thống. |

Quy tắc phân quyền (frontend `permission.js` + backend `[Authorize(Roles=...)]`):
- Mỗi module khai báo quyền truy cập trong `MODULES` (`access: "user" | "admin"`, hoặc `roles: [...]`).
- Sidebar/đường dẫn chỉ là hỗ trợ giao diện; **mọi endpoint bắt buộc có `[Authorize]` phía backend** (trừ login/activate).
- Nguyên tắc "không tự duyệt của chính mình" được áp dụng ở tầng nghiệp vụ khi người phát sinh đơn cũng chính là người có quyền duyệt (ghi rõ trong `05-workflow.md`).

> Lưu ý: V1 thiết kế ban đầu (theo tài liệu cũ của đồng đội) dùng 3 role `employee/hr/admin`; code hiện chạy đã mở rộng lên **4 role** (thêm `Manager`). Việc gộp/mở rộng role thêm nếu cần phải do chủ dự án quyết định, không tự ý đổi.

> **Quyết định xác nhận 12/10/2026 (Đinh Văn Tài):** Ảnh chấm công **giữ public** (chưa chuyển private). Phép năm giữ quy tắc hiện tại. Ca làm chung **giữ đơn giản** (1 rule active; rút đề xuất "version + ngày hiệu lực"). Offline (IndexedDB) và khóa kỳ + snapshot + Excel/PDF **chưa làm**.
> **EN (AI note):** Photos stay **public** (Supabase bucket `marixa-photos`). Leave-accrual rule unchanged. Shared shift stays **simple** (1 active `AttendanceRule`). Offline/IndexedDB queue, period lock + snapshot + Excel/PDF export are all **not built yet** — see `worklog.md`.

> **Role (mở rộng / Roles):** V1 hiện 4 role đã map quyền. Đã **seed** thêm 3 role nghiệp vụ: **Kế toán (Accountant), Sale, Marketing** (trong `RoleSeeder.cs`, **chưa** gán quyền module).
> EN: V1 has 4 mapped roles. 3 business roles (`Accountant`, `Sale`, `Marketing`) are **seeded** in code (no module permissions wired yet).

## 3. Quy tắc chấm công (ca làm chung)

- V1 dùng **một ca làm chung** do admin cấu hình trong bảng `AttendanceRule` (bắt đầu/ kết thúc, số phút nghỉ giữa ca, cờ active). Giá trị khởi tạo (seed) là 08:00–17:00, nghỉ trưa; đây là **mặc định có thể chỉnh**, không phải giờ cố định trong code.
- Khi có đủ cặp vào/ra, hệ thống tính **giờ thực tế (net)** đã trừ nghỉ ca và **giờ tăng ca** (phần vượt mốc ra ca chuẩn) tại thời điểm chấm — không phải chờ HR duyệt (`AttendanceService.CheckInAsync`).
- Vào ca sau mốc bắt đầu → trạng thái `Late`; đúng mốc → `Present`.
- Chấm lại cùng loại trong cửa sổ ngắn (tham số code, ~2 phút) bị chặn trùng, trả về "đã có lần chấm gần đây".
- Ảnh chấm công: chụp từ camera (`CameraCapture.jsx`), upload qua `POST /api/Upload/photo`. Đường lưu ảnh: **Supabase Storage bucket public `marixa-photos`** nếu cấu hình `SUPABASE__URL` + `SUPABASE__SERVICE_KEY`; chưa cấu hình thì fallback lưu đĩa `wwwroot/uploads` (dev local).

## 4. Nghỉ phép, tăng ca, sửa công

- **Đơn nghỉ** (`LeaveRequest`): loại nghỉ do admin định nghĩa (`LeaveType`), khoảng ngày, lý do, trạng thái duyệt. Phê duyệt sinh giao dịch sổ phép (`LeaveLedger`) — không sửa số dư trực tiếp.
- **Tăng ca** (`OvertimeRequest`): ngày, khoảng giờ, lý do; duyệt trước khi tính.
- **Sửa công** (`AttendanceCorrection`): đề xuất mốc đúng + lý do; khi duyệt tạo bản điều chỉnh riêng, giữ nguyên sự kiện gốc.
- Duyệt đi đúng người: HR/Manager/Admín duyệt đơn của Employee; không cho tự duyệt chính mình. Chi tiết luồng ở `05-workflow.md`.

## 5. Bảng công và báo cáo

- Kỳ công tính theo **tháng**, có trạng thái mở → HR kiểm tra → admin khóa.
- Báo cáo (Excel/PDF) theo quyền và phiên bản; Excel kỳ đang mở gắn nhãn "bản tạm", kỳ khóa dùng snapshot.
- Dashboard HR ưu tiên việc cần xử lý, số người đã/chưa chấm, đơn chờ duyệt, cảnh báo. Nhân viên chỉ thấy dữ liệu của mình.

## 6. Điều kiện nghiệm thu nghiệp vụ

1. Nhân viên/HR chấm vào/ra với ảnh (tùy camera) và GPS (tùy quyền); không ghi sự kiện thiếu bằng chứng một cách âm thầm.
2. Đơn nghỉ/tăng ca/sửa công đi đúng người duyệt; không tự duyệt đơn của chính mình.
3. Bảng công áp dụng ca làm chung hiện hành, nghỉ ca và điều chỉnh đã duyệt; thiếu mốc được báo "chưa hoàn chỉnh".
4. Admin khóa/mở kỳ có lý do; Excel/PDF đúng quyền, đúng kỳ và phiên bản.
5. Hệ thống giữ nhật ký (audit) cho các thay đổi quan trọng: công, phép, role, cấu hình, kỳ.

Xem dữ liệu tại [04-database-design.md](04-database-design.md), luồng tại [05-workflow.md](05-workflow.md), UI tại [09-ui-design.md](09-ui-design.md).
