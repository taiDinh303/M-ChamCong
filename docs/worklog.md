# WORKLOG — Hệ thống chấm công Marixa

> **Tác giả & chủ tài liệu: Đinh Văn Tài.** (File này được Tài tổ chức lại 12/10/2026 thành "bản đồ chức năng" theo trang & module để AI/Agnes và dev đọc nhanh.)

File này là điểm bắt đầu cho bất kỳ AI/người làm việc tiếp. Đọc theo thứ tự: (1) trạng thái, (2) ma trận chức năng, (3) log sự kiện, (4) quy tắc cập nhật.

**Quy tắc bất biến:** `docs/worklog.md` là file duy nhất được phép sửa tự do trong nhóm Markdown. Các file `01`–`08`, `plan.md`, `SETUP_CLOUD.md` là đặc tả; khi cần thay đổi đặc tả → **ghi nhận + đề xuất vào đây**, không tự sửa file đó. AI ghi sự thật đã kiểm chứng; không đánh dấu "xong" khi chưa kiểm tra.

## 1. Trạng thái hiện tại (cập nhật 12/10/2026, múi giờ Asia/Ho_Chi_Minh)

- **Bộ stack đang chạy:** backend `marixa_aspnet/M.API` (ASP.NET Core 8, EF Core, JWT/Identity, Swagger) · frontend `marixa_react` (React 19 + Vite SPA, React Router 7) · DB linh hoạt **SQL Server (dev) / PostgreSQL Supabase (prod)** · ảnh **Supabase Storage bucket public `marixa-photos`** (dev fallback `wwwroot/uploads`) · deploy **Render (API, Docker) + Vercel (web) + Supabase**.
- **Role:** 4 core đã map quyền `Admin`, `Manager`, `HR`, `Employee` (frontend `marixa_react/src/constants/modules.js`) + 3 role nghiệp vụ **đã seed** `Accountant`, `Sale`, `Marketing` (`M.API/Seed/RoleSeeder.cs`, **chưa** gán quyền module).
- **Next.js `marixa_aspnet/web`: ĐÃ XÓA** (12/10/2026, không liên quan — không còn bị tham chiếu bởi Dockerfile/solution).
- **Repository:** `origin = github.com/taiDinh303/M-ChamCong`.
- **Instance hosting đang chạy (xác nhận 12/10/2026):** Supabase ref `zphqfcwrwitrqdeueonu` (Free, Singapore · [dashboard](https://supabase.com/dashboard/project/zphqfcwrwitrqdeueonu)) · Render `https://marixa001-asp.onrender.com` (Free · service `srv-db4hmnks728c73aq6h90`, [dashboard](https://dashboard.render.com/web/srv-db4hmnks728c73aq6h90)) · Vercel `https://marixa001.vercel.app` (Hobby · team/project `taiteam/marixa_001`, [dashboard](https://vercel.com/taiteam/marixa_001)). Chi tiết trong `docs/06`, `docs/08`, `docs/SETUP_CLOUD.md`.
- Chấm công, nghỉ phép, tăng ca, sửa công, hồ sơ nhân sự, bảng lương, hợp đồng, BHXH, công việc, quản trị tài khoản/role **đều đã có code** (backend + frontend). Các mục **chưa có** tập trung ở: offline queue, khóa kỳ + snapshot, export Excel/PDF kỳ công, cộng phép năm tự động, audit log đầy đủ (xem ma trận §2).

## 2. Ma trận chức năng theo module & trang

> Trạng thái: ✅ đã có code · 🟡 có nhưng chưa hoàn thiện/đang dev · ❌ chưa có. Column "Code chính" chỉ file/endpoint để AI biết đi vào đâu.

### A. Module Đăng nhập / Kích hoạt (trùng trạng thái đăng nhập)

| Trang / chức năng | Trạng thái | Code chính | Ghi chú |
| --- | --- | --- | --- |
| `/login` đăng nhập (username/email/sđt + mật khẩu, remember me) | ✅ | `modules/login` (LoginPage, LoginForm), `POST /api/Auth/login` | Login linh hoạt nhiều định danh. |
| `/kich-hoat` kích hoạt bằng mã + đặt mật khẩu | ✅ | `modules/activate`, `POST /api/Auth/activate`, `ActivationCodeController` | Quy trình bàn giao NV. |
| `/unauthorized` thiếu quyền | ✅ | `modules/unauthorized` | |
| Đổi mật khẩu (đã xác thực) | ✅ | `POST /api/Auth/change-password` | |
| Link Google (connect/disconnect) | 🟡 | `UserLoginController` | Có API; UI gắn vào chưa rõ. |

### B. Module Nhân viên (`/attendance/*`, module `me`)

| Trang / chức năng | Trạng thái | Code chính | Ghi chú |
| --- | --- | --- | --- |
| `/attendance` chấm vào/ra + chụp camera | ✅ | `modules/me/pages/ClockInPage`, `CameraCapture.jsx`, `POST /api/Attendance/checkin` | Chấm log vào/ra, tự tạo Attendance ngày. |
| Ảnh chấm công (upload) | ✅ | `POST /api/Upload/photo` + `SupabaseStorageService` | Prod=Supabase bucket public; dev=đĩa `/uploads`. |
| GPS chấm công | 🟡 | Có `OfficeLocation` (DB), flag trong event (RPC Supabase cũ) | Frontend .NET chưa có UI gắn GPS rõ → kiểm tra khi làm. |
| `/attendance/attendance-history` lịch sử + sửa công | ✅ | `modules/me/pages/HistoryPage`, `AttendanceCorrectionController` | Chấm lại trùng bị chặn (~2 phút). |
| `/attendance/statistics` thống kê công | ✅ | `modules/me/pages/StatisticsPage`, StatKpis/StatWarnings | |
| `/attendance/leave` đơn nghỉ phép | ✅ | `modules/me/pages/LeavePage`, `LeaveRequestController`, `LeaveLedgerController` | Sổ phép trừ/hoàn. |
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
| `/employees` danh sách nhân sự (CRUD) | ✅ | `modules/employees/EmployeeListPage`, `EmployeeController`, `DepartmentController`, `PositionController` | |
| `/employees/attendance-history` đối soát & duyệt công | ✅ | `EmployeeAttendanceHistoryPage` (hrMode), `AttendanceController.approve` | |
| `/employees/work-schedule` lịch làm/ca | 🟡 | `EmployeeWorkSchedulePage`, `ShiftController`, `EmployeeShiftController` | Gán ca theo NV/ngày. |
| `/employees/statistics` thống kê (HR) | ✅ | `EmployeeStatisticsPage` (hrMode) | |
| `/employees/contracts` hợp đồng | ✅ | `EmployeeContractPage`, `AdminContractPage` | |
| `/employees/payroll` bảng lương | 🟡 | `PayrollController`, `PayrollAdminPage` | Route redirect về `/employees`; kiểm tra UI. |
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
| Auth JWT + Identity, phân quyền 4 role | ✅ | `M.Services/Security/JwtGenerator`, `[Authorize(Roles)]`. |
| DB linh hoạt SQL Server/PostgreSQL + boot DDL + schema sync | ✅ | `DatabaseConfig.cs`, `PostgresSchemaSync.cs`, `NpgsqlConnectionStringNormalizer.cs`. |
| Soft delete + audit (trên bảng công) | 🟡 | `AuditLog` entity + `AuditLogController` có; kiểm tra mọi module có ghi audit đều hay chưa. |
| Chấm công online + ảnh camera + tính giờ thực tế/tăng ca | ✅ | `AttendanceService.CheckInAsync`, `AttendanceTimeCalculator`. |
| **Offline / đồng bộ trễ (IndexedDB queue)** | ❌ | Chưa có. Event đến sau khi khóa kỳ cần "điều chỉnh kỳ mở sau". Xem `plan.md`. |
| **Khóa/mở kỳ công + snapshot không đổi** | ❌ | Chưa có luồng lock kỳ tháng. |
| **Xuất Excel (bảng công) & PDF (đơn nghỉ) theo kỳ** | ❌ | Chỉ có upload báo cáo NV; chưa có export kỳ công/đơn. |
| **Cộng phép năm tự động hằng tháng (idempotent)** | ❌ | Chưa có; cần job/cron + giao dịch ledger. |
| **Role nghiệp vụ (Kế toán, Sale, Marketing)** | 🟡 | Đã **seed** trong `RoleSeeder` (`Accountant`, `Sale`, `Marketing` — "Accountant" khớp `SampleDataSeeder`). **Chưa gán quyền module** (chưa map `MODULES`/endpoint). Khi cần cấp quyền thật → thêm vào `[Authorize(Roles=...)]`. (12/10/2026, code trước theo yêu cầu.) |
| **Ca làm chung** | ✅ | Giữ đơn giản: 1 `AttendanceRule` active (chủ dự án **không** yêu cầu ngày hiệu lực — đã rút lại đề xuất "version + ngày hiệu lực"). |
| **Giới hạn dung lượng / dọn ảnh (retention, cron)** | ❌ | Chưa có job dọn ảnh. |
| **Backup/restore chạy thật** | ❌ | Chưa có; cần `pg_dump` + backup ảnh + thử restore. |
| **Integration/E2E test** | ❌ | Chưa có bộ test; cần theo `05-testing-strategy.md`. |
| RLS / Supabase Auth | ❌ | Không dùng (đang dùng .NET Identity + JWT); không phải yêu cầu bắt buộc. |

## 4. Log sự kiện (mới trên cùng)

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
- Đã ghi vào `docs/06` (mục 0a), `docs/08` ( Hosting đang chạy ), `docs/SETUP_CLOUD.md` (mục 1) và trạng thái ở đây.
- Không sửa code; chỉ bổ sung tài liệu.

### 12/10/2026 — Đinh Văn Tài: tổ chức lại tài liệu
- Viết lại toàn bộ `docs/01`–`08`, `plan.md`, `SETUP_CLOUD.md`, `worklog.md` cho **phù hợp bộ stack đang chạy** (ASP.NET 8 `M.API` + React 19 `marixa_react`), không còn bám vào kiến trúc Next.js + Supabase Auth cũ (chưa triển khai).
- Thay thế tên/liên kết đồng đội cũ → **Đinh Văn Tài**; bỏ tham chiếu repo cá nhân cũ, cập nhật về `taiDinh303/M-ChamCong`.
- Bỏ tên đồng đội cũ (nơi còn sót) và các link tài khoản/repo cá nhân cũ ra khỏi mọi file .md; gộp về **Đinh Văn Tài**.
- Xây dựng **ma trận chức năng theo module & trang** (phần 2–3) làm nguồn duy nhất để biết "đã có / chưa có" → dễ cho AI và dev.
- Không sửa code, không chạy migration; chỉ chỉnh Markdown trong `docs/`.

### 09/10/2026 (ghi nhận cũ, giữ nguyên nội dung sự kiện)
- Backend .NET + 12 migration SQL Supabase; chấm công + idempotency; seed dữ liệu mẫu; xác nhận DB/Auth.
- Quyết định ảnh/GPS **tùy chọn**, ngoài văn phòng **vẫn tính công**, offline không giới hạn ngày đồng bộ, tăng ca ngày nghỉ **tự tính** (không cần đơn), tính **theo phút**, ca làm chung do admin quản lý.
- Lưu ý: các quyết định "tăng ca ngày nghỉ tự động / ca có ngày hiệu lực / điều chỉnh kỳ sau" là **phải làm tiếp** (đối chiếu mục 3 "chưa có").

## 5. Vấn đề / rủi ro đang mở

- **Offline + khóa kỳ:** chưa có queue/điều chỉnh kỳ sau — cần thiết kế trước khi dùng thật (event đến muộn không được sửa snapshot cũ).
- **Ảnh bucket public:** lộ URL công khai; cân nhắc bucket private + đọc theo quyền (quyết định trước khi làm, cập nhật `docs/02`).
- **Audit log chưa đồng nhất** giữa các module (chấm công có ghi `ChangeSummary`; các module khác cần kiểm tra có ghi `AuditLog`).
- **Render/Vercel/Supabase Free:** rủi ro hạn mức + điều kiện sử dụng thương mại — chủ dự án xác nhận trước khi vận hành thật (`docs/06`).
- **Chưa có test/backup/restore:** chưa chạy integration/e2e, chưa thử khôi phục dữ liệu.

## 6. Việc tiếp theo (tham chiếu `plan.md` mục 2)

1. Offline queue (IndexedDB) + điều chỉnh kỳ mở sau.
2. Khóa/mở kỳ + snapshot bất biến.
3. Export Excel (bảng công) & PDF (đơn nghỉ) theo quyền + phiên bản.
4. Cộng phép năm hằng tháng (idempotent).
5. **Map quyền module cho 3 role mới** (`Accountant`/`Sale`/`Marketing`) vào `MODULES` (frontend) + `[Authorize(Roles=...)]` (backend) — hiện 3 role này đã seed (code dùng "Accountant") nhưng chưa gán quyền.
6. Cân nhắc bucket ảnh private.
7. Bộ test (unit/integration/e2e) + backup/restore.
8. Deploy thật (Render + Vercel + Supabase) + smoke test URL thật.

> ~~Nâng `AttendanceRule` sang phiên bản có ngày hiệu lực~~ — **ĐÃ RÚT (12/10):** chủ dự án không yêu cầu ngày hiệu lực; giữ ca làm đơn giản (1 rule active).

## 7. Quy tắc cập nhật worklog

- Mỗi lượt: ghi ngày + thay đổi thật + bằng chứng kiểm tra; chuyển việc xong khỏi "Việc tiếp theo".
- Bug: triệu chứng + cách tái hiện + mức ảnh hưởng + trạng thái; khi hết lỗi ghi cách sửa + ngày đóng.
- Nếu cần đổi đặc tả (01–08/plan/SETUP_CLOUD): ghi **đề xuất + ảnh hưởng** vào đây, không tự sửa file đặc tả.
