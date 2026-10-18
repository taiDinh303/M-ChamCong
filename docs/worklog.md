# WORKLOG — Hệ thống chấm công Marixa

> **Tác giả & chủ tài liệu: Đinh Văn Tài.** (File này được Tài tổ chức lại 12/10/2026 thành "bản đồ chức năng" theo trang & module để AI/Agnes và dev đọc nhanh.)

Điểm vào tài liệu là [00-README.md](00-README.md). File này quản lý ma trận chức năng, bằng chứng thực hiện và lịch sử. Cập nhật hiện trạng: **18/10/2026** theo source, kiểm thử cô lập, nâng schema và đọc các trang cá nhân/HR live; production chưa nghiệm thu toàn bộ.

**Quy tắc cập nhật:** theo [00-README.md §4](00-README.md#4-quy-tắc-cập-nhật-cho-codex-agnes-và-developer). Quyền chuẩn hóa tài liệu đã được chủ dự án cấp ngày 16/10/2026. Quy tắc “chỉ sửa worklog / đặc tả đông băng” trong lịch sử không còn là hướng dẫn hiện hành. **EN:** Code presence is not runtime proof; record evidence separately and preserve approved requirements.

## 1. Trạng thái hiện tại (cập nhật 18/10/2026, múi giờ Asia/Ho_Chi_Minh)

- **Stack trong mã nguồn (chưa xác minh live):** backend `marixa_aspnet/M.API` (ASP.NET Core 8, EF Core, JWT/Identity, Swagger) · frontend `marixa_react` (React 19 + Vite SPA, React Router 7) · DB linh hoạt **SQL Server (dev) / PostgreSQL Supabase (prod)** · ảnh **Supabase Storage bucket public `marixa-photos`** (dev fallback `wwwroot/uploads`) · deploy **Render (API, Docker) + Vercel (web) + Supabase**.
- **Role:** 4 core đã map quyền `Admin`, `Manager`, `HR`, `Employee` (frontend `marixa_react/src/constants/modules.js`) + 3 role nghiệp vụ **đã seed** `Accountant`, `Sale`, `Marketing` (`M.API/Seed/RoleSeeder.cs`, **chưa** có quyền module chuyên biệt; các chức năng access user vẫn theo logic hiện tại).
- **Next.js `marixa_aspnet/web`: ĐÃ XÓA** (12/10/2026, không liên quan — không còn bị tham chiếu bởi Dockerfile/solution).
- **Repository:** `origin = github.com/taiDinh303/M-ChamCong`.
- **Hosting snapshot (chủ dự án cung cấp 12/10, chưa xác minh live 16/10):** bảng đầy đủ ở [00-README.md §5](00-README.md#5-thông-tin-nhanh-và-hosting). Blueprint hiện khai báo plan standard, khác snapshot Free.
- Chấm công, nghỉ phép, tăng ca, sửa công, hồ sơ nhân sự, bảng lương, hợp đồng, BHXH, công việc, quản trị tài khoản/role **đều đã có code** (backend + frontend). Các mục **chưa có** tập trung ở: offline queue, cộng phép tự động/retention, audit đầy đủ và nghiệm thu live (xem ma trận §2).

## 2. Ma trận chức năng theo module & trang

> Trạng thái: ✅ có code · 🟡 có một phần/có sai khác · ❌ chưa có. **Không trạng thái nào thay bằng chứng chạy thực tế.** Đọc bằng chứng ngày 17/10 ở §4 cho backend/frontend/browser cô lập; không suy nghiệm thu production. Tên file không ghi extension là từ khóa tìm kiếm trong src, không phải đường dẫn đầy đủ.

### A. Module Đăng nhập / Kích hoạt (trùng trạng thái đăng nhập)

| Trang / chức năng | Trạng thái | Code chính | Ghi chú |
| --- | --- | --- | --- |
| `/login` đăng nhập (username/email/sđt + mật khẩu, remember me) | ✅ | `modules/login` (LoginPage, LoginForm), `POST /api/Auth/login` | Login linh hoạt nhiều định danh. |
| `/kich-hoat` kích hoạt bằng mã + đặt mật khẩu | ✅ | `modules/activate`, `POST /api/Auth/activate`, `ActivationCodeController` | Quy trình bàn giao NV. |
| `/unauthorized` thiếu quyền | ✅ | `modules/unauthorized` | |
| Đổi mật khẩu | ✅ | AuthService/SessionTests | Auth/owner/password cũ, stamp thu hồi phiên; live chưa nghiệm thu. |
| Link Google (connect/disconnect) | 🟡 | `UserLoginController` | Có API; UI gắn vào chưa rõ. |

### B. Module Nhân viên (`/attendance/*`, module `me`)

| Trang / chức năng | Trạng thái | Code chính | Ghi chú |
| --- | --- | --- | --- |
| `/attendance` chấm vào/ra + camera | ✅ | AttendanceService/ClockInPage | UI/API theo rule; chấm không ảnh/GPS khi tùy chọn có browser smoke, thiết bị thật chưa nghiệm thu. |
| Ảnh chấm công (upload) | ✅ | `POST /api/Upload/photo` + `SupabaseStorageService` | Storage khi đọc được cấu hình hợp lệ; thiếu cấu hình dùng đĩa /uploads. Env đúng SUPABASE__SERVICEKEY; xem08. Không có fallback đĩa khi Storage đã config nhưng upload lỗi. |
| GPS chấm công | ✅ | AttendanceCard/AttendanceService | UI gửi lat/lon khi bật; API bounds/rule. Chưa geofence và chưa thử quyền GPS trên thiết bị thật. |
| `/attendance/attendance-history` lịch sử + sửa công/OT | ✅ | HistoryPage, CorrectionPanel, OvertimePanel | Có tab gửi/theo dõi, staff có tab duyệt đúng quyền; HR/Admin tự duyệt theo quyết định. |
| `/attendance/statistics` thống kê công | ✅ | `modules/me/pages/StatisticsPage`, StatKpis/StatWarnings | |
| `/attendance/leave` đơn nghỉ | ✅ | LeavePage, LeaveLedgerDialog, LeaveRequestService | Mine đúng quyền; PDF, hủy Pending, sổ phép. Staff hủy Approved khác chủ và cấp/điều chỉnh phép trong trang hiện có. |
| `/attendance/salary` bảng lương (của tôi) | ✅ | `modules/me/pages/SalaryPage`, `EmployeeSalaryController`, `PayrollController` | |
| `/attendance/contracts` hợp đồng | ✅ | `modules/me/pages/ContractPage`, `EmployeeContractController` | |
| `/attendance/insurance` BHXH & thuế | ✅ | `modules/me/pages/InsurancePage`, `EmployeeInsuranceController` | |
| `/attendance/bank-accounts` tài khoản ngân hàng | ✅ | `modules/me/pages/BankPage`, `EmployeeBankAccountController`, `BankController` | |
| `/attendance/attendance-rules` quy định (ca làm) | ✅ | `modules/me/pages/RulesPage`, `AttendanceRuleController/PublicController` | |
| `/attendance/reports` báo cáo của tôi | ✅ | `modules/me/pages/ReportsPage`, `EmployeeReportController` | |
| `/attendance/promotions` đề xuất thăng chức | ✅ | `modules/me/pages/PromotionsPage`, `EmployeePromotionController` | |
| `/attendance/handover` bàn giao nghỉ việc | ✅ | `modules/me/pages/HandoverPage`, `EmployeeHandoverController` | Có upload file (pdf/docx/xlsx…). |

### C. Module Công việc & Hồ sơ

| Trang / chức năng | Trạng thái | Code chính | Ghi chú |
| --- | --- | --- | --- |
| `/work` công việc (dự án/task/hiệu suất) | ✅ | `modules/work/WorkPage`, `WorkProject/WorkTask/WorkPerformance Controller` | |
| `/profile` hồ sơ cá nhân | ✅ | `modules/profile/ProfilePage` | |

### D. Module HR / Manager / Admin (`/employees/*`)

| Trang / chức năng | Trạng thái | Code chính | Ghi chú |
| --- | --- | --- | --- |
| `/employees` danh sách nhân sự (CRUD, import/export) | ✅ | `modules/hr/pages/HrPage.jsx`, `EmployeeController`, `DepartmentController`, `PositionController` | |
| `/employees/attendance-history` đối soát & duyệt công | ✅ | `EmployeeAttendanceHistoryPage` (hrMode), `AttendanceController.approve` | |
| `/employees/work-schedule` lịch làm/ca | 🟡 | `EmployeeWorkSchedulePage`, `ShiftController`, `EmployeeShiftController` | Gán ca theo NV/ngày. |
| `/employees/statistics` thống kê (HR) | ✅ | `EmployeeStatisticsPage` (hrMode) | |
| `/employees/contracts` hợp đồng | ✅ | `EmployeeContractPage`, `AdminContractPage` | |
| `/employees/payroll` | 🟡 | `App.jsx` | Redirect về /employees; không phải màn hình bảng lương riêng. |
| `/employees/leaves` duyệt nghỉ phép | ✅ | `EmployeeLeavePage` (hrMode), `LeaveAdminPage` | |
| `/employees/reports` báo cáo | ✅ | `ReportsPage`, `EmployeeReportController` | |
| `/employees/accounts` cấp tài khoản | ✅ | `AccountIssuancePage`, `UserController`, `ActivationCodeController` | |
| `/employees/promotions` thăng chức | ✅ | `PromotionsPage`, `EmployeePromotionController` | |
| `/employees/handover` duyệt bàn giao | ✅ | `MeHandoverPage` (reviewMode/admin) | |

### E. Module Admin (`/admin/*`)

| Trang / chức năng | Trạng thái | Code chính | Ghi chú |
| --- | --- | --- | --- |
| `/admin` dashboard | ✅ | `modules/admin/pages/AdminHomepage` | |
| `/admin/roles` quản lý role | ✅ | `AdminRolesPage`, `RoleController`, `UserRoleController` | |
| `/admin/accounts` cấp tài khoản | ✅ | `AdminAccountIssuancePage` | |
| `/admin/block-accounts` khóa tài khoản | ✅ | `AdminBlockAccountPage`, `UserController` | Chặn khóa admin cuối cần kiểm tra. |
| `/admin/work` | ✅ | `AdminWorkPage` | |
| `/admin/contracts` | ✅ | `AdminContractPage` | |
| `/admin/leaves` | ✅ | `LeaveAdminPage` | |
| `/admin/payroll` | ✅ | `PayrollAdminPage` | |
| `/admin/resigned` nghỉ việc | ✅ | `AdminResignedPage` (PayrollResignedReport) | |
| `/admin/reports` | ✅ | `ReportsPage` (admin) | |

## 3. Chức năng "nền" (backend/cross-cutting) — đã có vs chưa có

| Chức năng | Trạng thái | Ghi chú / nơi làm |
  | --- | --- | --- |
| Auth JWT + Identity, quyền core | 🟡 | Server guards/JWT revoke/lockout có test; frontend có session/prefix guard; Manager scope rộng và live còn mở. |
| DB linh hoạt SQL Server/PostgreSQL | 🟡 | Model/scripts/SQL Server fixture và PostgreSQL CI; live chưa nghiệm thu. |
| Soft delete + audit (trên bảng công) | 🟡 | `AuditLog` entity + `AuditLogController` có; kiểm tra mọi module có ghi audit đều hay chưa. |
| Chấm công online + ảnh + tính công/OT | 🟡 | UI/API có owner/ngày VN/break/phút/approved OT; thiết bị thật/live còn mở. |
| **Offline / đồng bộ trễ (IndexedDB queue)** | ❌ | Chưa có. Event đến sau khi khóa kỳ cần "điều chỉnh kỳ mở sau". Xem `01-plan.md`. |
| **Khóa/mở kỳ công + snapshot** | 🟡 | Backend period/guard/version/hash và UI Thống kê đã có, browser cô lập có lock/reopen/relock/export; live chưa nghiệm thu. |
| **XLSX snapshot & PDF nghỉ** | 🟡 | Backend có export theo quyền, Unicode/multipage; UI tải XLSX version/PDF đã kiểm cô lập, live chưa nghiệm thu. |
| **Cộng phép năm tự động hằng tháng (idempotent)** | ❌ | Chưa có; cần job/cron + giao dịch ledger. |
| **Role nghiệp vụ (Kế toán, Sale, Marketing)** | 🟡 | Đã **seed** trong `RoleSeeder` (`Accountant`, `Sale`, `Marketing` — "Accountant" khớp `SampleDataSeeder`). **Chưa gán quyền module** (chưa map `MODULES`/endpoint). Khi cần cấp quyền thật → thêm vào `[Authorize(Roles=...)]`. (12/10/2026, code trước theo yêu cầu.) |
| **Ca làm chung** | ✅ | Một active, không version ca; break cấu hình, seeder giữ rule, DB unique. |
| **Giới hạn dung lượng / dọn ảnh (retention, cron)** | ❌ | Chưa có job dọn ảnh. |
| **Backup/restore chạy thật** | 🟡 | 18/10 đã restore và đối chiếu bản sao public schema trên PostgreSQL cô lập; chưa thử phục hồi toàn hạ tầng, Storage/file và bản sao ngoài máy. |
| **Integration/E 2 E test** | 🟡 | Unit/HTTP/SQL Server và Node tests có suite; browser smoke cô lập thủ công, chưa tự động E2E/thiết bị thật/production. |
| Supabase Auth / Data API và RLS | 🟡 | Ứng dụng dùng .NET Identity/JWT. Đã kiểm metadata RLS/grants và advisor trong phạm vi public schema; còn hai WARN cũ liên quan quyền EXECUTE hàm event trigger, chưa audit toàn bộ hạ tầng. |

## 4. Log sự kiện (mới trên cùng)

### 18/10/2026 — Codex: giản lược điều hướng Home

- Chủ dự án phản ánh Header Home chật, hỏi vị trí/ẩn/footer cho Vận hành/Thông tin… và yêu cầu bỏ mục lục. Chọn bỏ thanh phân mục và nút mở mục lục trên Home vì các thẻ module đã là điều hướng chính; footer giữ thông tin thương hiệu. Giữ lọc quyền của thẻ, các nút thông báo/ngôn ngữ/tài khoản/về phân khu và sidebar của module.
- Xóa NAV_ITEMS placeholder, prop/render/state/ref/listener riêng cho menu Home và CSS không còn dùng; giữ Escape/click-outside của menu tài khoản. Chỉnh padding Header Home mobile, có mức gọn cho <=380px. REQ-UI-06/TC-UI-10, quyết định hiện hành ở 01/09.
- Kiểm local: 21 frontend tests PASS, lint/build PASS, còn cảnh báo chunk lớn cũ. Browser desktop 1280 và điện thoại 375/320: Header một hàng, không nav/nút mục lục hoặc tràn ngang; các thẻ vẫn hiện, chọn Nhân sự vào đúng route, sidebar module mở/Escape được; menu tài khoản mở/Escape được. Rà soát React: không giữ state/ref/import chết, effect cleanup còn đủ, giữ Link/role guards hiện có. Không đổi backend/schema và không chạy lại backend tests local. CI/rollout chờ tại thời điểm commit.
- Chốt lượt trước: PR #5 đã merge `3dfe507`, CI PR/main success, Vercel production đã kiểm cờ 40×40/tâm lệch 0px. Render redeploy lại mất PDF DEMO, đã phục hồi qua API với fingerprint predeploy/owner/trạng thái và audit; 7 tệp tải 200, snapshot/XLSX/readiness 200, khác owner 403/anonymous 401. Rủi ro private App_Data vẫn mở, không coi phục hồi DEMO là sửa storage.

### 18/10/2026 — Codex: căn giữa cờ ngôn ngữ trên Header

- Theo ảnh chủ dự án gửi, cờ lệch do chế độ flagOnly vẫn giữ caret/gap/padding mặc định; mobile còn padding wrapper làm vùng nút chật. Thêm modifier flag-only để ẩn caret đúng biến thể, bỏ padding/gap của button Header và đồng bộ wrapper 42×42 desktop, 40×40 mobile. Không đổi luồng chọn ngôn ngữ hoặc picker có chữ ở login/kích hoạt.
- Kiểm chứng local: 21 frontend tests PASS, lint/build PASS (cảnh báo chunk lớn cũ); browser desktop và điện thoại xác nhận tâm cờ trùng tâm khung nút (sai lệch x/y = 0px), chọn vi/en/zh và đóng dropdown đúng. Không thêm test mô phỏng CSS; không đổi backend/schema hoặc chạy lại backend tests local cho thay đổi này. Chờ CI/rollout tại thời điểm commit.
- Chốt rollout trước: PR #4 đã merge `ee949b0`, CI PR/main đều success; Vercel và menu live đã xác minh, Render snapshot/XLSX tháng 9 đều 200, nút XLSX trên web tải thành công; 4 PDF chính + 3 phụ lục DEMO tải 200, khác owner 403/anonymous 401. Sau rollout đã phục hồi đúng fixture bằng API có audit và fingerprint predeploy, không sửa snapshot bất biến; rủi ro App_Data vẫn mở.

### 18/10/2026 — Codex: menu điện thoại toàn màn hình và kiểm chứng sau PR #3

- **Quyền triển khai:** chủ dự án xác nhận “Đồng ý merge và triển khai sau khi CI đạt”. PR #3 đã merge main `1dc83a8`; CI PR/main xanh, Vercel production READY đúng SHA, Render readiness 200 và nộp báo cáo DEMO mới 200/mã 30 ký tự xác nhận bản sửa đã hoạt động.
- **Yêu cầu tiếp:** menu điện thoại toàn màn hình hợp lý với danh sách nhiều mục. Panel dùng toàn viewport/dynamic height, nút đóng sticky, safe area, cuộn trong menu; giữ native dialog/Escape/focus/khóa cuộn nền và tự đóng khi chọn trang. Desktop giữ hành vi cũ. REQ-UI-06/TC-UI-09.
- **Snapshot:** smoke live phát hiện DEMO tháng 9 dùng digest lowercase, API so sánh Ordinal với uppercase gây 409 snapshot/XLSX. Sửa so sánh OrdinalIgnoreCase (vẫn phải khớp đủ digest); seed mới uppercase. Không sửa payload/hash lịch sử hoặc bỏ guard bất biến. PeriodTests kiểm hai kiểu chữ, tampering/invalid hex, quyền owner và export; DemoScenarioTests kiểm seed→snapshot→XLSX.
- **Kiểm chứng bản tiếp theo:** Release warnings-as-errors 0 warning/0 error; backend **101 PASS / 0 FAIL / 0 SKIP**, SQLite/LocalDB/PostgreSQL loopback; frontend **21 PASS**, lint/build PASS, còn cảnh báo chunk lớn cũ. Browser local 375×667 xác nhận menu phủ đủ 375×667, header đóng sticky, chọn Thăng chức tự đóng, Escape trả focus/nền cuộn lại. Chưa dùng thiết bị thật.
- **Storage FAIL live:** sau redeploy, ba report DEMO tải PDF 404; phục hồi có guard đúng ID/marker/trạng thái fixture, ghi event và nộp lại qua API owner, ba PDF trở lại 200. Tạo thêm một report DEMO smoke xác minh lỗi mã report đã sửa. App_Data chưa bền; không khẳng định sẵn sàng lưu hồ sơ thật. Bản menu/hash tiếp theo đang chờ CI/rollout và kiểm lại snapshot/XLSX/private download.

### 18/10/2026 — Codex: dữ liệu DEMO live và header/menu responsive

- **Yêu cầu:** chủ dự án xác nhận thêm dữ liệu giả thực tế vào Supabase hiện tại để test web, giữ dữ liệu cũ. Đồng thời bỏ ngày Home, đưa tiêu đề/mô tả lên header desktop/dưới hàng logo mobile, thay sidebar kéo ngang bằng nút cạnh logo. REQ-UI-06/REQ-DEMO-01; hướng dẫn bộ test tại [Marixa.Demo](../marixa_aspnet/tools/Marixa.Demo/README.md).
- **Backup trước ghi:** pg_dump PostgreSQL 17.11 lấy public schema/dữ liệu nhất quán, mã hóa OpenPGP AES-256 ngoài Git tại `C:\Users\Admin\Documents\Marixa Backups\20261018T030606Z-demo-seed`. Ciphertext 163644 byte; SHA-256 nội dung dump `59c349dddf88d5da2d36251b374e993944e99f54536201134e13010d944d3ffb`. Key/passphrase bảo vệ bằng Windows DPAPI/ACL riêng. Đây là backup public, không bao gồm binary Storage/App_Data hay full DR; không lưu bản dump giải mã trên đĩa.
- **CURRENT live:** thêm 6 nhân viên/tài khoản DEMO chỉ Employee, 3 phòng ban, 4 chức vụ, các hồ sơ hợp đồng/lương/bảo hiểm/ngân hàng/người phụ thuộc/ca/lịch làm/payroll; 73 công (72 tháng 10 + 1 tháng 9), 143 log, 12 OT, 4 đơn nghỉ, 10 giao dịch sổ phép, 1 yêu cầu sửa công, 3 promotion, 3 handover, dự án/task/bình luận/đánh giá. Tháng 9 trước đó không có công/kỳ, dùng snapshot/hash/event mẫu; tháng 10 giữ mở. Report: 3 hồ sơ, 3 version + 3 attachment (6 PDF thật), 6 event sau nộp qua API. Mã/tên có DEMO, email example.invalid, không CCCD/điện thoại thật.
- **Bảo toàn và guard:** EF SaveChanges + Serializable, không raw SQL/bulk để vượt guard, không chạy seed ở startup. Hash của **54 dòng cũ trong 46 bảng** giữ nguyên sau bước seed. Sau thêm, 42/46 bảng có dữ liệu; chỉ AspNetRoleClaims/AspNetUserClaims/AspNetUserLogins/AspNetUserTokens còn trống đúng chức năng, không bịa dữ liệu bảo mật. Rule active, role/tài khoản cũ, lịch sử migration và dữ liệu cũ được giữ. Credential DEMO và hướng dẫn clipboard nằm riêng trong thư mục backup, không vào chat/Git.
- **Source UI:** [AuthenticatedShell](../marixa_react/src/components/layout/AuthenticatedShell.jsx) dùng chung cá nhân/HR/Admin/Work, heading portal từ HrHero, route fallback khi tải; giữ KPI/nội dung trang. Mobile native dialog từ trái, chọn link/Escape/ngoài đóng, khóa cuộn nền, trả focus; bỏ usePillNav/thanh ngang. Menu phân mục Home mobile thành dropdown. Không thay token/theme hoặc bố cục bảng/form chính.
- **Lỗi phát hiện và sửa:** tạo report mới dùng mã dài 40 ký tự trong cột tối đa 30 gây 500 trên SQL Server; đã rút mã <=30, thêm regression. Lỗi này còn trong bản API đang deploy cho đến rollout source; hồ sơ DEMO có mã hợp lệ và nộp lại qua endpoint chính thức nên tải được file thật ngay, không tạo đường dẫn giả. Lượt seed live đầu dừng trước transaction vì bootstrap Admin thiếu EmployeeId; đã chọn reviewer Admin có hồ sơ, thêm regression, không sửa liên kết tài khoản cũ.
- **Kiểm chứng:** Release solution (gồm tool) **0 warning / 0 error**; backend **97 PASS / 0 FAIL / 0 SKIP**, hai provider local; frontend **21 PASS**, lint sạch, production build PASS. Vite còn cảnh báo chunk lớn có từ trước (~1.23 MB main, ~940 kB ExcelJS trước gzip). Browser local API/LocalDB: desktop tiêu đề trên header, Home không ngày; khung 375/390px tiêu đề dưới hàng logo, không sidebar ngang, menu mở/chọn link/Escape và focus đúng. Không coi iframe responsive là thiết bị thật. Live: Bàn giao 3 và Báo cáo 3 hiển thị đúng; demo.an/binh/chi tải PDF và đọc công/phép đều **200**; console lỗi live 0 ở lượt cuối. Không duyệt hồ sơ thật và không kiểm khu Admin theo yêu cầu.
- **NOT VERIFIED / phát hành:** giao diện mới và sửa report code chưa merge/deploy; cần quyền rollout riêng theo AGENTS/18. File report private đang ở App_Data Render, volume/redeploy durability chưa xác minh; bộ DEMO không giải quyết rủi ro này. Chưa thử mọi thao tác ghi/thiết bị thật hoặc tất cả role. Không tự xóa log/ledger/snapshot để reset DEMO.

### 18/10/2026 — Codex: sao lưu, nâng schema Supabase và kiểm tra live

- **Quyền và phạm vi:** chủ dự án xác nhận “Đồng ý, bạn sao lưu và cập nhật”, thay lựa chọn tự chạy SQL trước đó. Chỉ nâng schema cần thiết và kiểm tra; giữ dữ liệu nghiệp vụ, Identity/JWT, quyền truy cập và Storage. Không kiểm khu Admin theo yêu cầu mới nhất; dùng phiên Admin để đọc khu HR.
- **Backup trước nâng:** snapshot nhất quán 43 bảng public / 688 cột / 54 bản ghi lúc 2026-10-17T17:06:53.102979Z, mã hóa OpenPGP AES-256, khóa riêng được bảo vệ bằng passphrase/Windows DPAPI, ngoài Git tại `C:\Users\Admin\Documents\Marixa Backups\20261017T165802Z-schema-upgrade`. Restore trên PostgreSQL 17.11 loopback: 43/43 bảng khớp dữ liệu, 54 FK và một hàm được phục hồi. SHA-256 nội dung đã giải mã `8859bcf2a1a02c9b92c867f216453367b380948f520b5eec044b1178233db20f`; manifest/proof và công cụ khôi phục được giữ cùng backup. Chưa bao gồm file Storage, schema nội bộ Supabase hoặc cấu hình hosting; xem [22](22-backup-recovery.md).
- **Migration live thành công:** lần lượt attendance-periods, workforce-policies và [attendance-integrity](../marixa_aspnet/sql/postgresql/20261017171012_attendance_integrity.sql), lịch sử Supabase `20261017171439`, `20261017171444`, `20261017171450` UTC. Ba bảng, năm cột, ActualHours numeric(10,2), ba unique index đã đủ; public schema đạt **46 bảng / 730 cột** khớp model kiểm tra. Đối chiếu số dòng và hash của 43 bảng cũ trước/sau nâng xác nhận giữ dữ liệu; các cột mới và biểu diễn số thập phân được chuẩn hóa khi đối chiếu. Không sửa nội dung hai SQL cũ hoặc reset/seed live.
- **Kiểm thử cô lập:** áp cả ba SQL hai lần trên bản phục hồi, kiểm đọc EF và bảo toàn dữ liệu. Test mới chứng minh duplicate preflight rollback không sửa dòng, filter soft-delete và hai unique index chặn ghi trùng. Release build warnings-as-errors **0 warning / 0 error**; suite backend **95 PASS / 0 FAIL / 0 SKIP**, gồm SQL Server LocalDB và PostgreSQL loopback. Lần thử trước có hai lỗi kết nối do PostgreSQL local dừng; đã chạy lại toàn bộ sau khi khởi động đúng và đạt số cuối trên. Không chạy fixture trên live.
- **Runtime live:** `/health/ready` từ 503 sang **200 READY**, `/health/live` 200. Phiên Employee đọc lịch sử công (giờ lẻ 7.02), OT, sửa công, thống kê, nghỉ phép, bàn giao, quy định thành công; khu HR đúng 403 với phiên Employee. Sau khi chủ dự án đổi sang Admin: HR Duyệt công đọc **6 bản ghi**, tab OT/sửa công tải được; Thống kê đọc kỳ **10/2026 đang mở**, 9 nhân viên/6 bản ghi; Nghỉ phép và Bàn giao tải trạng thái rỗng đúng. Console error **0** trong lượt kiểm tra cuối; các trang đã báo 500 không tái hiện lỗi. Không dùng số 0 trên màn hình đang tải làm bằng chứng.
- **Quyền và vệ sinh bản sao:** ba bảng kỳ công mới đều bật RLS, không có policy và không cấp SELECT cho anon/authenticated. PostgreSQL local đã dừng; auto-review chặn xóa thư mục `restore-cluster` chứa bản phục hồi giải mã, nên thư mục vẫn nằm dưới ACL riêng ngoài Git, cần chủ máy dọn thủ công. Giữ backup mã hóa/manifest/proof/công cụ. Validator tài liệu: **45 file / 351 link / 9 anchor / 0 lỗi**; diff whitespace sạch.
- **Giới hạn còn lại:** chưa tạo/duyệt/hủy đơn, cấp phép, sửa công, khóa/mở kỳ hoặc chạy job có tác động dữ liệu live; chưa nghiệm thu Manager scope/thiết bị camera-GPS/file/full DR. Advisor còn hai WARN cũ về EXECUTE `rls_auto_enable()` (event trigger), không nới RLS/grants để sửa lỗi 500. Mô tả rule cũ còn câu tự tính OT sau 17h: giữ nguyên dữ liệu, cần người quản trị chỉnh diễn đạt theo chính sách OT đã duyệt; công thức server hiện hành không lấy mô tả này làm quy tắc. Project thực tế ở ap-northeast-2, khác snapshot Singapore do chủ dự án cung cấp 12/10; không suy plan hiện tại từ snapshot cũ.

### 17/10/2026 — Codex: kiểm tra Supabase chỉ đọc sau merge frontend

- **CURRENT:** PR #2 đã merge vào main `bf11d5c`. Kết nối Supabase hiện có truy cập được đúng project marixa001; không cần chủ dự án gửi secret. Kiểm tra lúc khoảng 16:51 UTC chỉ đọc metadata, thống kê dữ liệu và log; chưa chạy migration hoặc ghi dữ liệu live.
- **Đối chiếu model:** dựng model Npgsql trên máy, không mở kết nối DB: **46 bảng / 730 cột**; public schema live có **43 bảng / 688 cột**. Thiếu AttendancePeriods, AttendancePeriodSnapshots, AttendancePeriodEvents (37 cột), cùng 5 cột AttendanceRules.BreakStartTime, EmployeeHandovers.AppliedAt, LeaveRequests.CancelledBy/CancelledAt/CancellationReason. Attendances.ActualHours còn integer, model yêu cầu numeric(10,2). Không thấy sai khác kiểu/nullability/độ dài/precision khác trong các cột đã có theo phép so sánh này.
- **Ràng buộc:** thiếu UX_AttendanceRules_Active, UX_Attendances_Employee_Date và UX_LeaveLedger_Request_Year_Type. Script workforce-policies đã có index ledger; hai index còn lại cần bổ sung đúng filter trong DatabaseContext, sau preflight. Ba bảng period và index/FK tương ứng có trong script attendance-periods; không sửa migration lịch sử.
- **Dữ liệu, phạm vi hẹp:** kiểm **54 quan hệ FK theo model**, tổng bản ghi mồ côi 0. Không thấy trùng ngày công chưa xóa hoặc trùng Deduct/Refund; đúng 1 rule active. Không thấy giờ công ngoài 0–24, cặp checkout <= checkin, OT Approved thiếu/sai cửa sổ, promotion chưa áp đã đến ngày hoặc bàn giao Approved quá ngày làm cuối. Các phép kiểm này không xác nhận độ đúng nghiệp vụ của từng hồ sơ. Không lấy tên/mật khẩu hash/file nhân viên vào báo cáo.
- **Quyền DB:** 43 bảng hiện có đều bật RLS, không có policy public; quyền cấp cho anon/authenticated không tự vượt RLS. Owner bảng là postgres; ứng dụng dùng Identity/JWT và kết nối backend, không thay bằng Supabase Auth hoặc mở Data API trong đợt sửa schema.
- **Runtime live:** GET /health/ready trả **503 NOT_READY**. Log PostgreSQL trong cửa sổ 24 giờ có **163 lỗi 42703** (thiếu cột), **6 lỗi 42P01** (thiếu bảng); không coi đây là số request frontend duy nhất. Chưa smoke các endpoint cần tài khoản thật trong lượt này. Chưa chứng nhận production ổn định.
- **TARGET, chờ chuyển quyền thao tác live:** chủ dự án trước đó chọn tự chạy SQL; đã gửi câu hỏi xác nhận có giao Codex sao lưu và áp nâng schema trực tiếp hay không. Kế hoạch: backup kiểm chứng được → hai SQL theo thứ tự ở [23](23-data-migration.md) → bổ sung hai unique index còn thiếu → so schema/index/row counts → readiness và smoke API được phép. Phải giữ dữ liệu, log/snapshot/history; không dùng init/seed/reset để sửa thiếu schema.

### 17/10/2026 — Codex: hoàn thiện React theo API và xác nhận nguyên nhân 500

- **Phạm vi được chủ dự án đồng ý:** giữ layout; kỳ công/XLSX trong Thống kê, tab OT và sửa công trong Lịch sử, Admin chỉnh rule bằng hộp thoại, HR/Admin cấp/điều chỉnh phép thủ công có sổ lịch sử. Chưa tự cấp phép hằng tháng. REQ-UI-01…05 / TC-UI-01…08 tại [06](06-testing.md), thao tác tại [30](30-user-guide.md).
- **Nguyên nhân live:** log PostgreSQL 42703 thiếu EmployeeHandovers.AppliedAt; truy vấn chỉ đọc metadata xác nhận thiếu ba bảng period, AppliedAt bàn giao, metadata hủy nghỉ, BreakStartTime; ActualHours còn integer. Preflight chỉ đọc không thấy nhóm trùng công-ngày/rule active/ledger Deduct-Refund. Chủ dự án chọn **tự chạy SQL**, agent không chạy migration hoặc ghi DB/bucket live. Thứ tự sau backup: [kỳ công](../marixa_aspnet/sql/postgresql/20261017-attendance-periods.sql) → [chính sách](../marixa_aspnet/sql/postgresql/20261017-workforce-policies.sql). Root API 404 là fallback bình thường; log mới đã qua startup JWT.
- **Các sửa chính:** nghỉ cá nhân/notification dùng /LeaveRequest/mine thay endpoint staff; Admin duyệt bằng approve/reject riêng; phân trang tải đủ khi server giới hạn 200; không nuốt lỗi thành số 0. RememberMe chọn sessionStorage/localStorage, expiry và 401 đúng phiên; RequireSession và quyền subroute Admin. Công thường/OT/số lẻ/break/ngày VN lấy API; bỏ công thức frontend tròn giờ/cố định 12–13, sửa rollover giờ tay cuối tháng/năm. File multipart/blob errors, CSV formula escaping, trạng thái Approved chờ AppliedAt và xóa dữ liệu bàn giao mẫu tự điền. Làm mới công sau duyệt OT/sửa công, làm mới kỳ sau sửa nguồn; chặn phản hồi cũ của bộ lọc/phiên tải đè dữ liệu mới. Dialog mới có busy/error/Escape/trả focus; CSS dùng token hiện có.
- **Sửa backend cần thiết cho UI:** DTO LeaveLedger cho Adjust âm [-999,999]; service vẫn chặn Grant âm/zero và Adjust zero. Có HTTP regression. Mô tả rule seed mới chỉ nói OT đã duyệt; không ghi đè rule đã có. Giữ thêm thay đổi JWT/config/tests của sự cố trước; không bỏ validation. Không sửa migration lịch sử.
- **Tự động trên máy này:** Release backend warnings-as-errors **0 warning / 0 error**; M.Tests **93 PASS, 1 SKIP, 0 FAIL / tổng 94** (SQLite/HTTP + SQL Server LocalDB; PostgreSQL loopback chưa có nên skip). React **21 PASS / 0 FAIL**, ESLint **0 error / 0 warning**, production build PASS. Vite còn cảnh báo chunk lớn: main khoảng 1.23 MB, ExcelJS tải động khoảng 940 kB trước gzip; không tăng ngưỡng để che cảnh báo. Thêm workflow frontend npm ci/test/lint/build/Markdown; remote CI chưa được chứng nhận bởi lần chạy local. Validator gồm Markdown React: 45 file, 345 link, 9 anchor, 0 lỗi.
- **Đối chiếu API:** AST của client và Swagger local: **190 khai báo method/route, 0 lệch**; không đồng nghĩa đã test mọi payload. HTTP read smoke với JWT thật trên LocalDB giả: **40 request Admin + 16 request Employee = 56/56 HTTP 200**; không gọi cloud trong smoke.
- **Browser cô lập đã quan sát:** TC-UI-01 chấm vào/ra không ảnh/GPS khi tùy chọn; TC-UI-02 gửi/duyệt OT và sửa công; TC-UI-03 duyệt công → review → lock v1 → XLSX → reopen → đổi rule → review/lock v2 → xuất lại v1; XML worksheet/shared strings của v1 giữ nguyên trước/sau; TC-UI-04 sửa nghỉ 60→90 phút, reload hiện 12:00–13:30; TC-UI-05 Grant 12 + Adjust -0.5 = 11.5; TC-UI-06 Employee xin nghỉ 0.5, Admin khác chủ duyệt/PDF/hủy, ledger đúng Deduct/Refund và số dư trở về 11.5; TC-UI-07 Employee /admin/roles→unauthorized, guest vào thống kê→login. Hai phiên riêng cùng tồn tại, Employee không có nút cấp phép.
- **UI phạm vi hẹp:** desktop mặc định 1265px cho ledger/period, viewport 375px cho Quy định và dialog (client width 360 do scrollbar; content width 360, dialog 328); Escape đóng và trả focus về nút mở. Chưa đủ nghiệm thu mọi màn hình/375–1440px/thiết bị thật. Tất cả seed/file tải là dữ liệu giả; DB Marixa_Verify_Frontend_GUID đã được dọn sau kiểm tra; không xóa DB người dùng.
- **Giới hạn phát hành:** schema live vẫn chưa nâng ở lần đọc metadata cuối; 500 production chưa thể coi đã đóng. Chưa deploy/merge main; kiểm [18](18-release-checklist.md), chủ dự án hoàn tất SQL và xác minh readiness trước rollout. Review frontend có rubric tại [FRONTEND_REVIEW](../marixa_react/FRONTEND_REVIEW.md).

### 17/10/2026 — Codex: chẩn đoán Render thoát 134 vì JWT key

- Log chủ dự án cung cấp cho commit `2555a25` lúc 10:11–10:12 UTC (17:11–17:12 giờ VN): Docker build/export image thành công, tiến trình .NET thoát ở `OptionsValidationException` do signing key không hợp lệ. Chưa đọc được env live; trình duyệt được cấp truy cập đang ở trang đăng nhập Render. Không kết luận đã sửa deployment.
- Ảnh Environment chủ dự án bổ sung xác nhận JWT key vẫn là placeholder có `CHANGE_ME`, dù dài hơn 32 ký tự; cần thay giá trị bằng secret ngẫu nhiên riêng. Không sao chép credential DB/Storage từ ảnh vào repo/log. Chưa có bằng chứng env mới đã được áp dụng hoặc deployment phục hồi.
- REQ-OPS-01 / TC-OPS-JWT: giữ nguyên validation, thêm tên `JWTSETTINGS__KEY` và yêu cầu key vào lỗi khởi động; không ghi giá trị key. Blueprint tạo key bằng `generateValue` khi chưa tồn tại, giữ key hiện hữu. Service tạo thủ công cần sửa env trên dashboard; không tạo/sync service mới hoặc đổi plan để xử lý sự cố.
- Thêm 7 ca kiểm thử host Production cô lập: từ chối thiếu/trống/ngắn/key mẫu, lỗi không lộ key và đọc env `__` vào key dùng xác minh Bearer. Không mở DB hoặc kết nối cloud trong các ca này. Hướng dẫn tạo key/Save and deploy/readiness tại [08 §2.1](08-deploy-detail.md#21-render-build-xong-nhưng-ứng-dụng-thoát-134-vì-jwt-key), registry tại 17 và runbook 21.
- Kiểm chứng local: `dotnet build marixa_aspnet/M.BE.sln -c Release -warnaserror` PASS, 0 warning / 0 error; `dotnet test marixa_aspnet/M.BE.sln -c Release --no-build` PASS: 92 PASS / 0 FAIL / 1 SKIP (PostgreSQL không có local), tổng 93. TRX: TEMP/marixa-render-jwt-tests/render-jwt.trx. Markdown: 43 file / 320 link / 9 anchor / 1 JSON, không lỗi. Docker local không có, chưa rebuild container tại máy; chưa đặt secret/redeploy/chạy migration DB live.

### 17/10/2026 — Codex: bộ tài liệu chọn lọc và hoàn thiện backend

- Đối chiếu docs_mau, thêm tài liệu phù hợp 14–25/27/30/31/34 và meta guides; N/A 26/28/29/32/33 có lý do trong intake. Sửa backend README/ARCHITECTURE mô tả nhầm Next.js, bổ sung score/risk/API inventory.
- Phỏng vấn chủ dự án: OT giao actual×Approved, công thường trong ca trừ nghỉ mặc định 12–13 cấu hình được, phút→2 số; HR/Admin self-review công/OT/sửa, leave cần người khác. Chủ dự án giao Codex chọn ngày hiệu lực và phần phép: worker đúng ngày/catch-up, hủy Approved có reason/Refund một lần, chưa auto accrual.
- Khôi phục/hoàn thiện HR service/private storage, account/JWT/lockout/guards; thêm period/snapshot/version/XLSX/PDF, raw log/ledger retention, trace/rate/readiness, migration/scripts và provider tests.
- Kiểm chứng local sau bản vá Pkcs 8.0.2: restore -warnaserror PASS; Release build: 0 warning / 0 error; M.Tests: 85 PASS / 0 FAIL / 1 SKIP (PostgreSQL local không có),86 ca tổng, TRX verified-local.trx trong TEMP/marixa-v2-artifacts/test-results. SQL Server LocalDB chạy migration từ schema trước release, kiểm default nghỉ 12:00, period transaction/version/constraint trên DB giả đã xóa sau test.
+- EF has-pending-model-changes: không drift; đã sinh/đọc idempotent SQL cho 2 migration mới, không DROP TABLE/COLUMN trong Up. Publish Release PASS, có font/license/SQL scripts. NuGet audit 8 project gồm transitive: 0 package có advisory sau ghim Pkcs 8.0.2; trước vá có 2 advisory qua PDFsharp.
+- Export QA độc lập: pypdf đọc đúng tên/loại nghỉ Unicode, 35 đoạn lý do / 200 ký tự token dài, font embedded; Poppler render và xem đủ 2 trang. openpyxl mở XLSX 2 sheet, giờ numeric/0.00, ô '=CMD()' là text, style/filter/freeze hợp lệ. Markdown: 43 file / 317 link / 7 anchor / 1 JSON: 0 lỗi; PowerShell code blocks parse 0 lỗi. Frontend npm run build PASS, có cảnh báo bundle lớn; không browser E2E.
+- CI remote [run 39001331032](https://github.com/taiDinh303/M-ChamCong/actions/runs/39001331032), source commit f3a344b: SUCCESS toàn bộ restore/build/test/Markdown/publish/Docker. TRX: 85 PASS / 0 FAIL / 1 SKIP SQL Server (Linux); PostgreSQL schema/additive upgrade lặp/period transaction/constraints PASS. Kết hợp fixture Windows đã kiểm cả hai provider. PR nháp [#1](https://github.com/taiDinh303/M-ChamCong/pull/1); nhánh codex/docs-backend-completion đã push. Không apply/deploy DB live trong nhiệm vụ này.
- Điểm theo rubric backend:5.3→8.4/10, chỉ chốt cùng kiểm chứng cuối; [BACKEND_REVIEW](../marixa_aspnet/BACKEND_REVIEW.md). Nhánh công việc codex/docs-backend-completion tránh autoDeploy khi schema live chưa nâng.


> Lịch sử trước lần chuẩn hóa 16/10 được giữ để truy vết. Số file, trạng thái, quyết định đã rút hoặc quyền sửa tài liệu trong các entry cũ không thay hướng dẫn hiện hành ở 00/01.

### 16/10/2026 (4) — Codex: chuẩn hóa tài liệu theo quyền của chủ dự án

- **Phạm vi được giao:** Đinh Văn Tài cho phép sửa bộ 00–13 để Codex/Agnes dùng trong các công việc sau. Đã cập nhật đủ 14 file đánh số, worklog và 4 Markdown phụ trợ trong figma để tránh tham chiếu mâu thuẫn. Giữ các thay đổi đang có và lịch sử sự kiện.
- **Quy tắc mới:** [00-README.md](00-README.md) là điểm vào duy nhất; tài liệu được bảo trì trong phạm vi nhiệm vụ được giao, có bằng chứng. Phân biệt CURRENT/TARGET/PENDING/NOT VERIFIED; code mô tả hiện trạng, quyết định nghiệp vụ mô tả kết quả cần đạt. Quy tắc đông băng cũ được thay thế; không tự mở rộng quyền sang code/deploy/cloud.
- **Nội dung:** sửa ký tự NUL/index/số file/role; đồng bộ quyết định một project, Identity/JWT và Next.js đã xóa; bổ sung route/API/quyền, entity/enum/ràng buộc, transition/test matrix, setup PowerShell/proxy/env, startup/schema/Storage, glossary và Figma coverage. Backlog cập nhật tại [01-plan.md](01-plan.md); OT/tự duyệt/hủy Approved cần quyết định rõ trước khi thay nghiệp vụ.
- **Phát hiện bổ sung:** tên env Storage đúng là SUPABASE__SERVICEKEY; URI normalizer có giới hạn SSL/query options; seeder chạy cả Production và có thể reset rule; route registry/quyền sở hữu còn khoảng trống; hai service tổng phép chưa thống nhất. Plugin có 60 variable definitions và UI Kit placeholder, không phải bằng chứng Figma đã hoàn thành. Các vấn đề ứng dụng vẫn mở.
- **Bằng chứng kiểm tra tài liệu:** đối chiếu source; kiểm tra UTF-8/ký tự điều khiển, file/link/anchor, H1, bảng, code fence và JSON mẫu; parse cú pháp PowerShell bằng parser, không thực thi. Inventory thiết kế khớp 42 route hiển thị từ App.jsx; CSS có 55 token, plugin có 50 COLOR + 9 FLOAT + 1 STRING definitions. Lượt tích hợp PASS: 19 Markdown, 285 link nội bộ, 11 anchor, 2 JSON mẫu và cú pháp 9 khối PowerShell; không còn lỗi ký tự điều khiển/link/fence/bảng, git diff --check đạt. Đây không phải runtime test.
- **Giới hạn:** không sửa mã ứng dụng, plugin, token hoặc cấu hình triển khai; không chạy API/migration/build/test suite, không truy cập DB/Dashboard/Figma live. Chỉ dẫn nhà cung cấp được đối chiếu nguồn chính thức trong 08/11; bảng hosting vẫn là snapshot chủ dự án cung cấp ngày 12/10.

### 16/10/2026 (3) — Đinh Văn Tài: chốt Q4=C (Dev Mode copy) + Q6=B (UI Kit first) + tạo Figma setup plugin
- **Q4=C đã chốt:** workflow chính Figma→code = **Dev Mode copy token/CSS** (không codegen plugin). `docs/13-figma-sync.md` §1 + §4.1 + §4.5 cập nhật: §4.5 codegen đánh dấu "future option only". Codegen giữ làm upgrade path khi scale >5 designer.
- **Q6=B đã chốt:** UI Kit (10 reusable components: Button×4, Input, Card, Badge×2, Dialog, Toast) tạo **trước** các route pages.
- **Tạo Figma setup plugin (1 lần chạy):** `figma/plugins/setup-design-tokens.ts` (442 dòng TypeScript) + manifest JSON — paste + Run trong Figma → tự tạo: (a) 56 Figma Variables (46 COLOR + 9 FLOAT + 1 STRING, mode Light, khớp key `figma/tokens/tokens.json`) · (b) Page `UI Kit` với 10 component placeholders · (c) 11 route pages × 2 frames (mobile 375×812 + desktop 1440×900, nền `th-bg` #F5F6F8). Plugin idempotent (chạy lại skip cái đã có).
- **Shadow values (4 biến `--th-*-shadow`)**: KHÔNG tạo thành Figma Variable (Figma dùng Effects→Drop Shadow, không phải variable) → ghi chú trong plugin comment + `files-map.md §3`.
- **Cập nhật `figma/README.md`:** thêm dòng `plugins/` vào bảng folder · cập nhật §3 workflow (Q4=C Dev Mode = chính, codegen = future option).
- **Figma MCP extension (Dev Mode read-only):** có khả năng dùng sau này để AI đọc design context trực tiếp từ Figma file; không dùng để TẠO variables (phải chạy plugin trong Figma browser).
- Không sửa `theme.css` / `tokens.json`; chỉ tạo plugin + cập nhật docs (13-figma-sync, figma/README).

### 16/10/2026 (2) — Đinh Văn Tài: xác nhận Figma file thật + quy ước 2-frame (mobile 375 / desktop 1440)
- **Figma file thật (cung cấp bởi chủ dự án):** tên `Marixa001` · key `2TYFTZweE1j2enGLUNXaq3` · URL `https://www.figma.com/design/2TYFTZweE1j2enGLUNXaq3/Marixa001` · team/project `Marixa_001` (Free · owner `taiDz`). File đã có, Pages = "Page 1" (chưa tạo page per-route).
- **Variables + UI Kit chưa tạo** (chủ dự án trả Q2 = A) → hướng dẫn tạo 1 lần trong Figma đã ghi vào `figma/files-map.md §3`: (a) tạo Figma Variables khớp key trong `figma/tokens/tokens.json` (mode Light, đặt tên `color/th-*`, `typo/th-font`…) · (b) tạo UI Kit components (Button / Input / Card / Badge / Table row / Dialog / Toast).
- **Quy ước Q5 = B (đã chốt):** mỗi route = **1 Figma Page** chứa **2 frame** (`-mobile` 375px + `-desktop` 1440px) → đã ghi vào `figma/files-map.md §0` + ghi chú trước §2.
- **Q4 (loại workflow sync) & Q6 (UI Kit) — chưa chốt** (chủ dự án chưa rõ) → **chưa sửa `docs/13-figma-sync.md §4`**; chỉ ghi nhận trong chat, chờ chủ dự án trả lời Q4/Q6 rồi cập nhật spec.
- **Đổi meta trong `figma/`:** điền `figma/files-map.md §0` (URL + key + team + quy ước 2-frame + hướng dẫn §3) + 1 changelog entry `fix` trong `figma/tokens/changelog.md` (xác nhận Figma file thật). **Không** đổi `theme.css` / `tokens.json`.

### 16/10/2026 — Đinh Văn Tài: scaffold figma/ + docs/13-figma-sync.md
- **Tạo `figma/` (root repo):** `README.md` (entry point + AI read order) · `files-map.md` (page↔route table, DRAFT — chờ Figma file thật) · `tokens/tokens.json` (mirror 50+ biến `--th-*` từ `marixa_react/src/theme.css` + UI metrics từ `docs/09-ui-design.md §2`) · `tokens/changelog.md` (scaffold + baseline entry `code→Figma`) · `generated/` (placeholder draft zone).
- **Tạo `docs/13-figma-sync.md` (spec đông băng):** SSoT matrix (theme.css = code SSoT, Figma = design intent, tokens.json = mirror) + 5 workflow (token change · layout change · new-page · deprecate · codegen) + hard rules (không binary `.fig`, không secret/token, `theme.css` thắng khi lệch, changelog mới-trên-cùng, codegen gated qua review, files-map 1:1 `modules.js`).
- **Đăng ký trong `docs/00-README.md`:** thêm dòng task-table `| Figma sync (design↔code) | 13-figma-sync.md |` + mở rộng phạm vi "spec frozen" từ `02–12` → `02–13` (6 vị trí).
- **Quyết định (chuyên gia):** `figma/` đặt ở **root repo** (không trong `marixa_react/`) — design-as-data, cùng cấp với `docs/`, Figma phục vụ cả .NET + React, git reviewer thấy design + code trong 1 diff, sống qua thay đổi stack frontend.
- **Chiến lược token:** mirror full 50+ biến `theme.css` vào `tokens.json` (không phải Figma-export minimal) để làm reference hoàn chỉnh; `_meta.note` ghi rõ `theme.css` là code SSoT, khi lệch `theme.css` thắng.
- Không sửa code; chỉ tạo Markdown trong `figma/` + `docs/`.

### 14/10/2026 (2) — Đinh Văn Tài: đồng bộ tên file docs (đánh số 00–12)

- **Đổi tên 2 file cho đồng bộ đánh số:** `SETUP_CLOUD.md` → `11-setup-cloud.md`; `plan.md` → `01-plan.md`. Giờ toàn bộ `docs/` mang tên `00`–`12` + `worklog.md`.
- **Cập nhật mọi tham chiếu/link** tới 2 file đổi tên trong `00`, `01`, `04`, `06`, `12`, `worklog`.
- **Thêm `10-setup-local.md` và `12-glossary.md`** vào bảng "file nào cho việc nào" của `00-README`; mở rộng "spec" từ `01`–`10` thành `01`–`10`.
- Không sửa code; chỉ chỉnh Markdown trong `docs/`.

### 14/10/2026 — Đinh Văn Tài: tổ chức lại tài liệu (gộp trùng lặp, thêm 09/10, gắn P0/P1/P2, sửa mojibake)
- **Gộp nội dung trùng về `00-README.md` (nguồn duy nhất):** bảng hosting (trước lặp 5 file: 00/06/08/11-setup-cloud/worklog) giờ chỉ ở `00-README` §Quick facts, các file khác link về. Quy tắc đọc/ghi (trước lặp 4 file) chuẩn hoá ở `00-README` (mục mới "Quy tắc đọc / ghi tài liệu (NGUỒN DUY NHẤT)"). `plan §2` là nguồn "việc tiếp theo", `worklog §6` chỉ link.
- **Thêm `10-setup-local.md`:** hướng dẫn setup dev local end-to-end (.NET 8 SDK, SQL Server, Node, build, seed, migration, test).
- **Thêm `12-glossary.md`:** bảng thuật ngữ dùng chung (ca làm, kỳ công, snapshot, idempotent, session pooler, DB linh hoạt, soft delete, bucket public, module, role, offline queue, export, blueprint, SPA fallback).
- **Gộp `06` vào `08`:** nội dung `06` (biến môi trường + bước triển khai + troubleshooting) nhúng vào `08`. `06` giờ chỉ là overview + link.
- **Gắn P0/P1/P2 vào `01-plan.md §2`:** P0 = khóa kỳ/snapshot/export + test + deploy thật; P1 = offline + sổ phép + backup; P2 = map quyền role mới + bảo mật ảnh.
- **Sửa mojibake:** `worklog.md` bị lỗi ký tự (backticks + hyphen trong route bị mất, diacritics lỗi) — đã reconstruct toàn bộ file bằng nội dung chuẩn, kiểm tra lại bằng cách đọc raw bytes.

### 14/10/2026 (2) — Đinh Văn Tài: đồng bộ tên file docs (đánh số 00–12) + sửa backticks

- **Đổi tên 2 file cho đồng bộ đánh số:** `SETUP_CLOUD.md` → `11-setup-cloud.md`; `plan.md` → `01-plan.md`. Giờ `docs/` gồm `00`–`12` + `worklog.md`.
- **Cập nhật mọi tham chiếu/link** tới 2 file đổi tên trong `00`, `01`, `04`, `06`, `12`, `worklog`.
- **Thêm `10-setup-local.md` + `12-glossary.md`** vào bảng "file nào cho việc nào" trong `00-README`; mở rộng "spec" `01`–`10` → `01`–`10`.
- **Sửa lỗi backticks** trong `00-README` (một số dòng `09`/`12-plan` mất backtick do tool encode) — reconstruct toàn bộ `00-README.md` từ bản sạch, verify bằng byte inspection (0 dòng hỏng, 4 dòng có backtick đúng).
- Không sửa code; chỉ chỉnh Markdown trong `docs/`.

### 14/10/2026 (4) — Đinh Văn Tài: sắp xếp lại file theo thứ tự đọc (logic)
- **Đổi tên file theo thứ tự đọc quan trọng → phụ:**
  | Số mới | File | Nội dung |
  | --- | --- | --- |
  | 01 | `01-plan.md` | Kế hoạch, việc tiếp theo, quyết định (từ `12-plan.md`) |
  | 02 | `02-business-analysis.md` | Nghiệp vụ, role, quy tắc (từ `01`) |
  | 03 | `03-architecture.md` | Kiến trúc (từ `04`) |
  | 04 | `04-database-design.md` | DB / entity (từ `02`) |
  | 05 | `05-workflow.md` | Luồng nghiệp vụ (từ `03`) |
  | 06 | `06-testing.md` | Kiểm thử (từ `05`) |
  | 07 | `07-deploy-overview.md` | Deploy overview (từ `06`) |
  | 08 | `08-deploy-detail.md` | Deploy chi tiết + troubleshooting (từ `08`) |
  | 09 | `09-ui-design.md` | UI / design (từ `07`) |
  | 10 | `10-setup-local.md` | Setup dev local (từ `09`) |
  | 11 | `11-setup-cloud.md` | Setup cloud dashboard (giữ) |
  | 12 | `12-glossary.md` | Thuật ngữ (từ `10`) |

- **Cập nhật link/tham chiếu** trong toàn bộ file (heading, cross-link, nội dung).
- **Viết lại `00-README`** với bảng "file nào cho việc nào" mới theo logic đọc.
- Không sửa code; chỉ chỉnh Markdown trong `docs/`.
### 12/10/2026 — Đinh Văn Tài: thực thi yêu cầu chủ dự án (delete Next.js, thêm role, rút "ngày hiệu lực")
- **Xoá `marixa_aspnet/web`** (bản Next.js): đã kiểm tra không bị tham chiếu bởi Dockerfile/solution/scripts → `git rm -r` + `Remove-Item` xóa sạch khỏi đĩa. Không còn tồn tại.
- **Thêm 3 role** `Accountant`, `Sale`, `Marketing` vào `M.API/Seed/RoleSeeder.cs` ("code trước" theo yêu cầu; "Accountant" khớp `SampleDataSeeder` sẵn có). Chưa gán vào `MODULES`/endpoint; comment ghi rõ.
- **Rút lại** đề xuất "nâng `AttendanceRule` lên version + ngày hiệu lực" (chủ dự án xác nhận **không** cần). Giữ ca làm đơn giản: 1 rule active.
- Cập nhật `worklog.md` §1 (role + web deleted), ma trận §2–3 (ca làm ✅ đơn giản, role 🟡 seed chưa map), `00-README.md` quick facts.
- Chạy build .NET (`dotnet build`) để xác nhận RoleSeeder không lỗi compile (thực hiện khi chủ dự án cho phép / trước khi push).

### 12/10/2026 — Đinh Văn Tài: phỏng vấn chủ dự án, chốt quyết định
Câu trả lời của chủ dự án → cập nhật tài liệu (chỉ sửa `.md`):
1. **Ảnh chấm công** = giữ **public** (Q1.A) → `marixa-photos` public, không tự chuyển private.
2. **Phép năm** = giữ quy tắc hiện tại (Q2.A).
3. **Ca làm** = nâng lên **version + ngày hiệu lực** (Q3.B) → mục "chưa có" trong worklog.
4. **Offline/IndexedDB** = chưa làm, ưu tiên sau (Q4.A).
5. **Khóa kỳ + snapshot + Excel/PDF** = chưa có (Q5.A) → mục "chưa có".
6. **`marixa_aspnet/web` (Next.js)** = **đình chỉ (decommission)** (Q6.A) → không thuộc đường chạy; ghi "chờ xoá khi chủ dự án xác nhận". Lệnh xoá thư mục thật sẽ chờ xác nhận (có trong Git).
7. **GPS chấm công** = chưa wire UI (Q7.A) → 🟡.
8. **Role** = hiện 4 role, **sẽ thêm Kế toán / Sale / Marketing...** (Q8.B) → ghi "chờ thiết kế role mới", không tự thêm.
9. **Render** = `https://marixa001-asp.onrender.com` (swagger `/swagger`), plan **Free**.
10. **Vercel** = `https://marixa001.vercel.app`, plan **Hobby**.
11. **Supabase** = `zphqfcwrwitrqdeueonu`, **Singapore, Free**; **bỏ hẳn ref test/prod cũ**.
12. Ghi 2 URL public (web + API) vào tài liệu cho AI/dev tiếp cận nhanh.
13. Tài liệu: **song ngữ** (tiêu đề Việt, nội dung có phần Anh cho AI).
14. `worklog.md` giữ làm index duy nhất (kèm `docs/00-README.md` nếu cần).

> Lưu ý: chưa sửa code/render.yaml/.env (ngoài phạm vi `.md` của lượt này). Việc xoá `marixa_aspnet/web` + thêm role + nâng ca làm có ngày hiệu lực = **chờ chủ dự án ra lệnh**.

### 12/10/2026 — Đinh Văn Tài: bổ sung instance hosting đang chạy
- Chủ dự án cung cấp các instance đang hosting (không chứa secret): Supabase ref `zphqfcwrwitrqdeueonu`, Render web service `srv-db4hmnks728c73aq6h90`, Vercel team/project `taiteam/marixa_001`.
- Đã ghi vào `docs/06` (mục 0a), `docs/08` (Hosting đang chạy), `docs/11-setup-cloud.md` (mục 1) và trạng thái ở đây.
- Không sửa code; chỉ bổ sung tài liệu.

### 12/10/2026 — Đinh Văn Tài: tổ chức lại tài liệu
- Viết lại toàn bộ `docs/01`–`08`, `01-plan.md`, `11-setup-cloud.md`, `worklog.md` cho **phù hợp bộ stack đang chạy** (ASP.NET 8 `M.API` + React 19 `marixa_react`), không còn bám vào kiến trúc Next.js + Supabase Auth cũ (chưa triển khai).
- Thay thế tên/liên kết đồng đội cũ → **Đinh Văn Tài**; bỏ tham chiếu repo cá nhân cũ, cập nhật về `taiDinh303/M-ChamCong`.
- Bỏ tên đồng đội cũ (nơi còn sót) và các link tài khoản/repo cá nhân cũ ra khỏi mọi file .md; gộp về **Đinh Văn Tài**.
- Xây dựng **ma trận chức năng theo module & trang** (phần 2–3) làm nguồn duy nhất để biết "đã có / chưa có" → dễ cho AI và dev.
- Không sửa code, không chạy migration; chỉ chỉnh Markdown trong `docs/`.

### 09/10/2026 (ghi nhận cũ, giữ nguyên nội dung sự kiện)
- Backend .NET + 12 migration SQL Supabase; chấm công + idempotency; seed dữ liệu mẫu; xác nhận DB/Auth.
- Quyết định ảnh/GPS **tùy chọn**, ngoài văn phòng **vẫn tính công**, offline không giới hạn ngày đồng bộ, tăng ca ngày nghỉ **tự tính** (không cần đơn), tính **theo phút**, ca làm chung do admin quản lý.
- Lưu ý: các quyết định "tăng ca ngày nghỉ tự động / ca có ngày hiệu lực / điều chỉnh kỳ sau" là **phải làm tiếp** (đối chiếu mục 3 "chưa có").

## 5. Vấn đề / rủi ro đang mở

**Hiện hành 17/10:** quyết định nghiệp vụ ở 01, danh mục rủi ro ở 34. Ownership/JWT/bootstrap/timezone/storage/seed đã được sửa phía server; không giữ lỗi đã sửa như backlog mở. Còn cần schema/grants/volume/restore và smoke live, Manager scope, thiết bị/camera/GPS thật, dữ liệu cũ, offline/accrual/retention và giảm bundle. Chỉ gọi production đạt khi có bằng chứng riêng.

### Rà soát ban đầu 16/10/2026 — bằng chứng trước chuẩn hóa

> Bản ghi lịch sử: đề xuất sửa docs trong bảng dưới đã được xử lý ở sự kiện chuẩn hóa mới nhất §4. Các sai khác code/chức năng thiếu vẫn mở; chi tiết mới nhất ở 00–13 và mục hiện hành §5 phía trên. Phạm vi “không thay đổi đặc tả” bên dưới chỉ áp dụng lượt rà soát ban đầu.

**Kết luận tại thời điểm rà soát ban đầu:** 15 file Markdown (`00`–`13` + `worklog`) bao phủ đủ nhóm chủ đề chính, nhưng **chưa đủ chính xác và nhất quán để coi là baseline ổn định**. Stack tổng quan ASP.NET Core 8 + React 19/Vite + Identity/JWT khớp code. Những phát biểu về hành vi đã triển khai cần sửa hoặc gắn nhãn “yêu cầu / chưa triển khai”. Đây là kết quả đọc mã nguồn, không phải chứng nhận runtime/production.

**Phạm vi:** đã đọc toàn bộ `docs/*.md`; đối chiếu controllers/services, EF context, startup/seeders, route/module registry, frontend chấm công/export, cấu hình local và dữ liệu Figma trong repo. Kiểm tra đường dẫn đích của Markdown links tương đối: không thấy file đích bị thiếu (chưa kiểm tra anchors/URL ngoài). Không chạy API, migration, build, test hoặc truy cập dashboard/Figma live. Không thay đổi đặc tả/code; bổ sung kết quả tại §5 theo quy tắc `00-README.md`.

| Mức | Vấn đề xác nhận | Bằng chứng mã nguồn / tài liệu | Đề xuất |
| --- | --- | --- | --- |
| P1 | Ảnh được mô tả tùy chọn nhưng UI bắt buộc có ảnh mới và upload thành công trước khi chấm. | `01` §1, `05` §1/§7, `06`, `02` §6; `marixa_react/src/modules/me/components/AttendanceCard.jsx` hàm `check`; seeder đặt `PhotoRequired = true`. | Phân biệt yêu cầu nghiệp vụ với UI/API thực tế; quyết định thống nhất chính sách trước khi đổi code. |
| P1 | GPS có UI đọc vị trí, nhưng chưa gửi/lưu tọa độ và chưa có kiểm tra bán kính trong luồng check-in hiện tại. | `AttendanceCard.jsx` có `useGeolocation` nhưng gọi `relatedApi.checkin(employeeId, type, photoUrl)`; `modules/me/api/relatedApi.js`; `AttendanceService.CheckInAsync` không xử lý GPS. `05`/`06` nói gắn cờ trong/ngoài văn phòng; worklog nói chưa có UI GPS cũng chưa chính xác. | Ghi “đã hiển thị GPS, chưa nối vào API/lưu trữ/geofence”; không coi OfficeLocation entity hoặc SQL RPC cũ là bằng chứng luồng .NET đã hoàn tất. |
| P1 | Ngày công không được xác định theo ngày Việt Nam như `04` §1 mô tả. | `AttendanceService.CheckInAsync`: `now = DateTime.UtcNow`, tìm/tạo `AttendanceDate = now.Date`. Trong 00:00–06:59 UTC+7, ngày UTC là ngày hôm trước. | Ghi rõ sai khác và mở việc sửa cách xác định ngày; test biên 00:00/07:00 VN. |
| P1 | Quy tắc OT mâu thuẫn: `02` §4, `05` §5, `06` dòng OT theo đơn, `12` nói phải duyệt và lấy giao giờ; code tự tính phần giờ ra vượt ca chuẩn. | `AttendanceTimeCalculator.Compute` tính `otMin` từ `outVn - stdEnd`; không nhận đơn OT đã duyệt. `02` §3 và `06` dòng ra 18:00 lại khớp công thức hiện tại. | Chốt một quy tắc; mô tả hiện trạng riêng với thiết kế OT theo đơn. |
| P1 | Luồng nghỉ phép mô tả hủy đơn đã duyệt sinh hoàn phép nhưng API service chặn hủy đơn Approved. | `05` §4, `06` ma trận nghỉ; `LeaveRequestService.CancelAsync` chỉ nhận Pending, thông báo Approved/Rejected immutable. Có helper refund không đồng nghĩa luồng hủy Approved hoạt động. | Sửa mô tả luồng hiện tại; nếu cần hủy Approved thì ghi yêu cầu bổ sung. |
| P1 | Quy tắc “không tự duyệt” không đồng nhất giữa các loại đơn. | `02`/`05` nói cấm tự duyệt; `LeaveRequestService.DecideAsync` chặn owner, nhưng `OvertimeRequestService.UpdateAsync` cho self-approval khi `IsElevated()` (Admin/HR). | Lập ma trận quyền theo từng thao tác/loại đơn, ghi rõ ngoại lệ hiện tại và yêu cầu cần thay đổi. |
| P1 | Khóa kỳ/snapshot/export theo phiên bản chưa có nhưng nhiều luồng được giới thiệu là khớp code đang chạy. | `02` §5, `05` §2/§6/§7, `09` trạng thái kỳ khóa, `07`/`11` smoke test; trái với `01` và worklog §3 ghi chưa có. | Gắn nhãn “thiết kế mục tiêu/chưa triển khai”, tách tiêu chí tương lai khỏi smoke test có thể chạy ngay. |
| P1 | Setup cloud tự mâu thuẫn về số project và còn chỉ dẫn Supabase Auth ngoài đường chạy hiện tại. | `11` §1 xác nhận 1 project, §2 lại yêu cầu tạo 2 project; `08` Step 1 yêu cầu bật Auth trong khi ứng dụng dùng .NET Identity/JWT. `04` §5 và `11` §1 vẫn nói Next.js chờ xóa, trái với `01` và checkout không còn `marixa_aspnet/web`. | Đồng bộ theo quyết định hiện hành; phân biệt tài liệu kiến trúc cũ và cấu hình ứng dụng đang dùng. |
| P1 | Cam kết ca cấu hình ổn định chưa phản ánh hành vi seeder lúc boot. | `02` §3 nói seed chỉ là mặc định chỉnh được; `AttendanceRuleSeeder.SeedAsync` khi DEFAULT-CA đã tồn tại vẫn ghi lại 08:00/17:00, StandardHours=8, BreakMinutes=60, IsActive=true. | Ghi rõ nguy cơ reset cấu hình khi restart; xác minh ràng buộc chỉ 1 rule active, không suy ra từ quyết định nghiệp vụ. |
| P2 | Index có ký tự điều khiển thật và nhiều tham chiếu đánh số cũ. | `00-README.md` có 37 ký tự U+0000. Heading đầu `02`–`06`, `09` vẫn mang số cũ; `02` §0 nói quyết định ở plan §2 thay vì §1; `07` nói gộp `06` dù `06` hiện là testing; worklog có entry 14/10 bị dính heading/trùng. | Loại NUL, chuẩn hóa H1/số file, sửa tham chiếu theo tên đầy đủ; giữ lịch sử nhưng ghi chú mapping cũ. |
| P2 | Vai trò kế toán bị ghi sai tên; mô tả 4 role chưa phân biệt với 7 role seed. | `01` dùng Accounting; `RoleSeeder.cs` thực tế là Accountant, cùng Admin/Employee/Manager/HR/Sale/Marketing. `04` bảng role chỉ nêu 4. | Dùng chính xác Accountant; tách “7 role đã seed” và “4 role core có quyền riêng”. |
| P2 | Setup local thiếu thao tác env/proxy và phần test dễ tạo cảm giác đã có suite. | `10` chưa hướng dẫn copy `.env.example` → `.env.local`, chưa giải thích `VITE_API_PROXY_TARGET`; `vite.config.js` proxy mặc định http://localhost:57432, khớp launchSettings. `package.json` không có script test; không thấy test project trong solution. Lệnh frontend `cd marixa_react` sau `cd marixa_aspnet` trong §5 sai nếu chạy nối tiếp. | Ghi rõ 2 terminal/cwd, env proxy và cổng HTTP/HTTPS; ghi test suite chưa triển khai, không coi dotnet test exit 0 là có test chạy. |
| P2 | Mô tả export thiếu chức năng đã có và chưa tách định dạng. | Worklog/`12` nói chỉ upload báo cáo; HR có Excel import/export hồ sơ (`modules/hr/pages/HrPage.jsx`, `hrUtils.js`), lịch sử công có `exportMonth` CSV, admin có CSV. Chưa thấy export XLSX bảng công snapshot/PDF đơn nghỉ. | Liệt kê riêng Excel hồ sơ, CSV công hiện thời, XLSX bảng công khóa và PDF đơn nghỉ. |
| P2 | Data dictionary chưa đầy đủ và ràng buộc đang là yêu cầu, chưa có bằng chứng DB enforce. | `04` thiếu WorkTaskComment và EmployeeReportVersion/Attachment/Event có trong `DatabaseContext`; liên kết Employee.UserId được `AdminSeeder` dùng trực tiếp. Không thấy unique index EmployeeId+AttendanceDate trong EF context; check-then-insert trong service không bảo đảm chống race. | Bổ sung PK/FK/nullability/enums/index và phân loại “đang enforce”/“cần enforce”; không chứng nhận DB production khi chưa kiểm tra. |
| P2 | Hướng dẫn deploy thiếu config Storage ở bảng env tổng hợp và mô tả smoke test ảnh theo fallback local. | `08` §6 thiếu SUPABASE__URL/SUPABASE__SERVICE_KEY; §4 smoke test nói ảnh từ Render /uploads, trong khi UploadController có config sẽ trả URL Supabase public. Troubleshooting “chạy migration Supabase” chưa phân biệt SQL cũ và EF schema hiện tại. | Đồng bộ bảng env và 2 nhánh lưu ảnh; ghi rõ migration nào thuộc kiến trúc nào. Giá/plan/UI nhà cung cấp cần kiểm chứng riêng bằng nguồn chính thức trước lần deploy. |
| P2 | Figma scaffold chưa đáp ứng quy tắc mapping 1:1 và tên token còn mâu thuẫn. | `13` nói 11 route/1 page mỗi route; `modules.js`/`App.jsx` có nhiều route hơn, files-map gom /employees/* và /admin/*, thiếu /home và nhiều route con; /finance là redirect. Ví dụ `color/brand/th-primary` trong `13` khác `color/th-primary` trong files-map. Không có bằng chứng setup plugin đã chạy thành công trong Figma. | Xác định mapping theo màn hình hay nhóm module, ghi coverage rõ ràng; thống nhất tên variable với plugin; giữ trạng thái scaffold/chưa verify live. |
| P2 | Độ chính xác phép tính và khả năng schema sync chưa được mô tả đủ. | Calculator làm tròn ActualHours thành int, OT 1 chữ số thập phân, cửa sổ nghỉ bắt đầu cố định 12:00. `PostgresSchemaSync` chỉ ADD cột thiếu, không đổi kiểu/xóa cột hiện có. | Bổ sung công thức, rounding, ca qua đêm, giới hạn schema sync và quy trình migration/backfill/rollback; không hứa tự đồng bộ mọi thay đổi entity. |

**Nội dung còn thiếu để ổn định:** ma trận endpoint + role + quyền sở hữu; hợp đồng request/response/error kèm ví dụ; trạng thái enum và transition thực tế; data dictionary đầy đủ; hướng dẫn bootstrap/migration/rollback/backup có bằng chứng; tiêu chí done gắn test cụ thể. Các phần này có thể bổ sung vào file hiện hữu, không cần tạo thêm file đánh số.

**Ưu tiên đề xuất:** xử lý P1 trước; tiếp theo làm sạch index/link/thuật ngữ, setup local và ma trận trạng thái. Giữ các nội dung nghiệp vụ mong muốn nhưng ghi rõ “target / pending”, tránh đổi yêu cầu chỉ để khớp bug của code. Không đánh dấu tính năng hoặc nghiệm thu production là hoàn thành từ lượt rà soát tĩnh này.

## 6. Việc tiếp theo

> **Nguồn duy nhất cho danh sách "việc tiếp theo theo module" là [`01-plan.md`](01-plan.md) §2 (checkbox).** `worklog.md` không liệt kê lại — chỉ link về.

- Khi hoàn thành 1 mục trong `01-plan.md` §2: thêm entry vào §4 (log sự kiện) của file này, ghi ngày + bằng chứng.
- Khi phát sinh việc mới: cập nhật backlog trong phạm vi đã được giao; đề xuất cần quyết định mới ghi §5 trước, theo [00-README.md](00-README.md).

## 7. Quy tắc cập nhật worklog

Theo [00-README.md §4](00-README.md#4-quy-tắc-cập-nhật-cho-codex-agnes-và-developer). Mỗi entry ghi ngày, thay đổi thật, phạm vi kiểm tra và bằng chứng. Sự kiện cũ giữ để truy vết; trạng thái hiện hành phải có ngày và căn cứ riêng. Đóng lỗi tài liệu không đồng nghĩa đóng lỗi ứng dụng hoặc nghiệm thu cloud.
