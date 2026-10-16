# WORKLOG — Hệ thống chấm công Marixa

> **Tác giả & chủ tài liệu: Đinh Văn Tài.** (File này được Tài tổ chức lại 12/10/2026 thành "bản đồ chức năng" theo trang & module để AI/Agnes và dev đọc nhanh.)

Điểm vào tài liệu là [00-README.md](00-README.md). File này quản lý ma trận chức năng, bằng chứng thực hiện và lịch sử. Cập nhật hiện trạng: **16/10/2026** theo mã nguồn; chưa kiểm thử runtime/production.

**Quy tắc cập nhật:** theo [00-README.md §4](00-README.md#4-quy-tắc-cập-nhật-cho-codex-agnes-và-developer). Quyền chuẩn hóa tài liệu đã được chủ dự án cấp ngày 16/10/2026. Quy tắc “chỉ sửa worklog / đặc tả đông băng” trong lịch sử không còn là hướng dẫn hiện hành. **EN:** Code presence is not runtime proof; record evidence separately and preserve approved requirements.

## 1. Trạng thái hiện tại (cập nhật 16/10/2026, múi giờ Asia/Ho_Chi_Minh)

- **Stack trong mã nguồn (chưa xác minh live):** backend `marixa_aspnet/M.API` (ASP.NET Core 8, EF Core, JWT/Identity, Swagger) · frontend `marixa_react` (React 19 + Vite SPA, React Router 7) · DB linh hoạt **SQL Server (dev) / PostgreSQL Supabase (prod)** · ảnh **Supabase Storage bucket public `marixa-photos`** (dev fallback `wwwroot/uploads`) · deploy **Render (API, Docker) + Vercel (web) + Supabase**.
- **Role:** 4 core đã map quyền `Admin`, `Manager`, `HR`, `Employee` (frontend `marixa_react/src/constants/modules.js`) + 3 role nghiệp vụ **đã seed** `Accountant`, `Sale`, `Marketing` (`M.API/Seed/RoleSeeder.cs`, **chưa** có quyền module chuyên biệt; các chức năng access user vẫn theo logic hiện tại).
- **Next.js `marixa_aspnet/web`: ĐÃ XÓA** (12/10/2026, không liên quan — không còn bị tham chiếu bởi Dockerfile/solution).
- **Repository:** `origin = github.com/taiDinh303/M-ChamCong`.
- **Hosting snapshot (chủ dự án cung cấp 12/10, chưa xác minh live 16/10):** bảng đầy đủ ở [00-README.md §5](00-README.md#5-thông-tin-nhanh-và-hosting). Blueprint hiện khai báo plan standard, khác snapshot Free.
- Chấm công, nghỉ phép, tăng ca, sửa công, hồ sơ nhân sự, bảng lương, hợp đồng, BHXH, công việc, quản trị tài khoản/role **đều đã có code** (backend + frontend). Các mục **chưa có** tập trung ở: offline queue, khóa kỳ + snapshot, export Excel/PDF kỳ công, cộng phép năm tự động, audit log đầy đủ (xem ma trận §2).

## 2. Ma trận chức năng theo module & trang

> Trạng thái: ✅ có code · 🟡 có một phần/có sai khác · ❌ chưa có. **Không trạng thái nào thay bằng chứng chạy thực tế.** Ngày 16/10 chỉ đối chiếu source; test/runtime/production chưa chạy. Tên file không ghi extension là từ khóa tìm kiếm trong src, không phải đường dẫn đầy đủ.

### A. Module Đăng nhập / Kích hoạt (trùng trạng thái đăng nhập)

| Trang / chức năng | Trạng thái | Code chính | Ghi chú |
| --- | --- | --- | --- |
| `/login` đăng nhập (username/email/sđt + mật khẩu, remember me) | ✅ | `modules/login` (LoginPage, LoginForm), `POST /api/Auth/login` | Login linh hoạt nhiều định danh. |
| `/kich-hoat` kích hoạt bằng mã + đặt mật khẩu | ✅ | `modules/activate`, `POST /api/Auth/activate`, `ActivationCodeController` | Quy trình bàn giao NV. |
| `/unauthorized` thiếu quyền | ✅ | `modules/unauthorized` | |
| Đổi mật khẩu | 🟡 | `POST /api/Auth/change-password`, `AuthService.ChangePasswordAsync` | Action thiếu [Authorize]; service kiểm tra mật khẩu cũ. Cần kiểm tra contract xác thực/quyền sở hữu, không coi comment là bảo vệ token. |
| Link Google (connect/disconnect) | 🟡 | `UserLoginController` | Có API; UI gắn vào chưa rõ. |

### B. Module Nhân viên (`/attendance/*`, module `me`)

| Trang / chức năng | Trạng thái | Code chính | Ghi chú |
| --- | --- | --- | --- |
| `/attendance` chấm vào/ra + chụp camera | 🟡 | `modules/me/pages/ClockInPage`, `CameraCapture.jsx`, `POST /api/Attendance/checkin` | Có log vào/ra; UI bắt buộc ảnh, ngày công lấy UTC, kiểm tra quyền sở hữu/chống race còn mở. |
| Ảnh chấm công (upload) | ✅ | `POST /api/Upload/photo` + `SupabaseStorageService` | Storage khi đọc được cấu hình hợp lệ; thiếu cấu hình dùng đĩa /uploads. Env đúng SUPABASE__SERVICEKEY; xem08. Không có fallback đĩa khi Storage đã config nhưng upload lỗi. |
| GPS chấm công | 🟡 | `AttendanceCard.jsx`, `relatedApi.checkin`, `AttendanceService.CheckInAsync` | UI có geolocation; chưa gửi/lưu tọa độ hoặc tính cờ bán kính trong luồng .NET. |
| `/attendance/attendance-history` lịch sử + sửa công | ✅ | `modules/me/pages/HistoryPage`, `AttendanceCorrectionController` | Chấm lại trùng bị chặn (~2 phút). |
| `/attendance/statistics` thống kê công | ✅ | `modules/me/pages/StatisticsPage`, StatKpis/StatWarnings | |
| `/attendance/leave` đơn nghỉ phép | 🟡 | `modules/me/pages/LeavePage`, `LeaveRequestController`, `LeaveLedgerController` | Có ledger; hiện chỉ hủy Pending, chưa hủy/hoàn Approved qua luồng cancel. Quyền đọc danh sách cần rà soát. |
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
| Auth JWT + Identity, phân quyền 4 role core | 🟡 | Có cài đặt; cần kiểm tra endpoint/quyền sở hữu và route registry. [03-architecture.md](03-architecture.md), [06-testing.md](06-testing.md). |
| DB linh hoạt SQL Server/PostgreSQL + boot DDL + schema sync | ✅ | `DatabaseConfig.cs`, `PostgresSchemaSync.cs`, `NpgsqlConnectionStringNormalizer.cs`. |
| Soft delete + audit (trên bảng công) | 🟡 | `AuditLog` entity + `AuditLogController` có; kiểm tra mọi module có ghi audit đều hay chưa. |
| Chấm công online + ảnh camera + tính giờ thực tế/tăng ca | 🟡 | Có code; sai khác ảnh tùy chọn/ngày UTC/mốc ca/rounding/quyền sở hữu còn mở. |
| **Offline / đồng bộ trễ (IndexedDB queue)** | ❌ | Chưa có. Event đến sau khi khóa kỳ cần "điều chỉnh kỳ mở sau". Xem `01-plan.md`. |
| **Khóa/mở kỳ công + snapshot không đổi** | ❌ | Chưa có luồng lock kỳ tháng. |
| **XLSX bảng công snapshot & PDF đơn nghỉ theo kỳ** | ❌ | Chưa có; XLSX hồ sơ nhân sự và CSV công/danh sách đã có code, không phải export snapshot. |
| **Cộng phép năm tự động hằng tháng (idempotent)** | ❌ | Chưa có; cần job/cron + giao dịch ledger. |
| **Role nghiệp vụ (Kế toán, Sale, Marketing)** | 🟡 | Đã **seed** trong `RoleSeeder` (`Accountant`, `Sale`, `Marketing` — "Accountant" khớp `SampleDataSeeder`). **Chưa gán quyền module** (chưa map `MODULES`/endpoint). Khi cần cấp quyền thật → thêm vào `[Authorize(Roles=...)]`. (12/10/2026, code trước theo yêu cầu.) |
| **Ca làm chung** | 🟡 | Mục tiêu 1 rule active, không version/ngày hiệu lực; seeder ghi đè DEFAULT-CA khi restart và mốc evaluator/calculator chưa đồng nhất. |
| **Giới hạn dung lượng / dọn ảnh (retention, cron)** | ❌ | Chưa có job dọn ảnh. |
| **Backup/restore chạy thật** | 🟡 | Chưa có bằng chứng; cần quy trình DB + ảnh và thử restore trên môi trường test được chỉ định. |
| **Integration/E2E test** | ❌ | Chưa có bộ test; cần theo `06-testing.md`. |
| Supabase Auth / Data API và RLS | 🟡 | Ứng dụng dùng .NET Identity/JWT. Data API/grants/RLS của DB live chưa xác minh; không suy ra an toàn dữ liệu chỉ vì ứng dụng không gọi Supabase Auth. |

## 4. Log sự kiện (mới trên cùng)

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

**Hiện hành sau chuẩn hóa 16/10:** các lỗi mô tả được sửa trong docs; các sai khác ứng dụng chưa được sửa code. Backlog duy nhất ở [01-plan.md §2](01-plan.md#2-việc-tiếp-theo). Các đề xuất OT/tự duyệt/hủy Approved chưa có quyết định thống nhất phải giữ trạng thái mở.

- **Quyền dữ liệu:** kiểm tra quyền sở hữu ở chấm công/xem công/danh sách nghỉ; [Authorize] chỉ chứng minh xác thực hoặc role, không chứng minh giới hạn đúng nhân viên. Route guard thiếu registry entry trả true.
- **Cấu hình Storage:** code đọc Supabase:ServiceKey, cần SUPABASE__SERVICEKEY; tên __SERVICE_KEY còn ở blueprint/comment/mẫu env không tương đương. Kiểu key/header upload chưa được chứng minh chạy live.
- **Kết nối và startup:** normalizer URI không giữ đầy đủ SSL Mode/query options; startup có thể ghi warning rồi tiếp tục khi DB/seed lỗi. Swagger trả được không chứng minh DB sẵn sàng.
- **Chấm công:** ngày UTC, UI bắt buộc ảnh, GPS chỉ hiển thị, làm tròn giờ, mốc evaluator 16:30 khác rule17:00, seeder reset DEFAULT-CA; test concurrency/unique constraint còn thiếu.
- **Duyệt và dữ liệu:** ngoại lệ HR/Admin tự duyệt OT/sửa công, duyệt công nhận ApprovedBy từ client, quyền sửa bản ghi Approved cần kiểm chứng từng endpoint. Hai service tổng ledger khác cách tính Adjust và lọc DeletedTime; giữ chính sách phép, mở việc đối soát công thức.
- **Hạ tầng/Figma:** hosting snapshot khác plan trong blueprint; env examples có thông tin cũ; plugin60 variable definitions, UI Kit placeholder, chưa có bằng chứng chạy hoặc binding. Không suy ra trạng thái live từ file repo.


- **Offline + khóa kỳ:** chưa có queue/điều chỉnh kỳ sau — cần thiết kế trước khi dùng thật (event đến muộn không được sửa snapshot cũ).
- **Ảnh bucket public:** lộ URL công khai; cân nhắc bucket private + đọc theo quyền (quyết định trước khi làm, cập nhật `docs/02`).
- **Audit log chưa đồng nhất** giữa các module (chấm công có ghi `ChangeSummary`; các module khác cần kiểm tra có ghi `AuditLog`).
- **Hạ tầng:** đối chiếu plan/giới hạn/điều kiện hiện hành với dashboard và tài liệu chính thức; xem [07-deploy-overview.md](07-deploy-overview.md).
- **Chưa có test/backup/restore:** chưa chạy integration/e2e, chưa thử khôi phục dữ liệu.


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
