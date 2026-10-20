# FILES-MAP — Route React và Page Figma mục tiêu

> Chủ tài liệu: **Đinh Văn Tài**. Route React cập nhật **19/10/2026** (`/me`, `/hr`); scaffold plugin vẫn là mốc **16/10/2026**, chưa đồng bộ node Figma live.
> **EN — Codex / Agnes:** `App.jsx` owns actual routes; `modules.js` owns the UI registry. The plugin has 11 scaffold entries, including route groups. The tables below describe coverage and target pages, not live Figma nodes. All live design state is **NOT VERIFIED**.

## 1. File đích và quy ước đã chọn

| Nội dung | Giá trị |
| --- | --- |
| File đích | [Marixa001](https://www.figma.com/design/2TYFTZweE1j2enGLUNXaq3/Marixa001) |
| File key | `2TYFTZweE1j2enGLUNXaq3` |
| Team / owner | `Marixa_001` / `taiDz`; Free plan là ghi nhận trước, chưa xác minh hiện tại. |
| Workflow | Dev Mode copy (Q4=C); UI Kit trước (Q6=B). |
| Target mapping | Một Page cho mỗi route hiển thị; hai frame `PageName-mobile` (rộng 375px) và `PageName-desktop` (rộng 1440px). |
| Source scaffold | [`setup-design-tokens.ts`](plugins/setup-design-tokens.ts) định nghĩa 11 mục Page, 2 frame/mục (375×812, 1440×900); `/hr/*` và `/admin/*` là nhóm. |
| Live evidence | Chưa có node ids/ảnh review/bằng chứng chạy plugin được kiểm tra trong lượt này. |

## 2. Inventory 42 route hiển thị

Nguồn: [`App.jsx`](../marixa_react/src/App.jsx). Tên Page ở cột 2 là **TARGET**; tên khớp plugin chỉ là định nghĩa trong source, không xác nhận Page tồn tại. Mỗi dòng đều **NOT VERIFIED** về live Figma. Quyền thực tế và khoảng trống registry/guard: [`03-architecture.md`](../docs/03-architecture.md).

| Route React | Page mục tiêu | Coverage trong plugin local | Ghi chú |
| --- | --- | --- | --- |
| `/login` | Login | Tên/route khớp | Public login. |
| `/kich-hoat` | Activate | Tên/route khớp | Kích hoạt. |
| `/unauthorized` | Unauthorized | Tên/route khớp | Thiếu quyền. |
| `/home` | Home | **not-mapped** | Tổng quan/module cards. |
| `/me` | ClockIn | Tên/route khớp | GPS chỉ hiển thị; ảnh hiện bắt buộc tại UI. |
| `/me/attendance-history` | AttendanceHistory | Tên/route khớp | Chỉ route cá nhân; HR có route riêng. |
| `/me/attendance-rules` | AttendanceRules | **not-mapped** | Quy định. |
| `/me/statistics` | Statistics | Tên/route khớp | Thống kê cá nhân. |
| `/me/leave` | Leave | Tên/route khớp | Transition theo API, không giả định draft/Approved cancellation. |
| `/me/reports` | MyReports | **not-mapped** | Báo cáo cá nhân. |
| `/me/promotions` | MyPromotions | **not-mapped** | Đề xuất thăng chức. |
| `/me/handover` | MyHandover | **not-mapped** | Bàn giao. |
| `/me/contracts` | MyContracts | **not-mapped** | Hợp đồng. |
| `/me/salary` | MySalary | **not-mapped** | Lương cá nhân. |
| `/me/insurance` | MyInsurance | **not-mapped** | Bảo hiểm/thuế. |
| `/me/bank-accounts` | MyBankAccounts | **not-mapped** | Ngân hàng. |
| `/work` | Work | Tên/route khớp | Công việc. |
| `/profile` | Profile | Tên/route khớp | Hồ sơ. |
| `/hr` | Employees | **group only**: Employees | Nhóm `/hr/*` không phải mapping từng màn hình. |
| `/hr/attendance-history` | EmployeeAttendanceHistory | **group only**: Employees | Đối soát/duyệt công. |
| `/hr/work-schedule` | EmployeeWorkSchedule | **group only**: Employees | Lịch làm. |
| `/hr/statistics` | EmployeeStatistics | **group only**: Employees | Thống kê HR. |
| `/hr/contracts` | EmployeeContracts | **group only**: Employees | Hợp đồng HR. |
| `/hr/leaves` | EmployeeLeaves | **group only**: Employees | Nghỉ phép HR. |
| `/hr/reports` | EmployeeReports | **group only**: Employees | Báo cáo HR. |
| `/hr/accounts` | EmployeeAccounts | **group only**: Employees | Cấp tài khoản. |
| `/hr/promotions` | EmployeePromotions | **group only**: Employees | Thăng chức. |
| `/hr/handover` | EmployeeHandover | **group only**: Employees | Bàn giao HR. |
| `/admin` | Admin | **group only**: Admin | Dashboard. |
| `/admin/contracts` | AdminContracts | **group only**: Admin | Hợp đồng. |
| `/admin/leaves` | AdminLeaves | **group only**: Admin | Nghỉ phép. |
| `/admin/payroll` | AdminPayroll | **group only**: Admin | Lương. |
| `/admin/resigned` | AdminResigned | **group only**: Admin | Nghỉ việc. |
| `/admin/reports` | AdminReports | **group only**: Admin | Báo cáo. |
| `/admin/accounts` | AdminAccounts | **group only**: Admin | Cấp tài khoản. |
| `/admin/employees/leaves` | AdminEmployeeLeaves | **group only**: Admin | Route riêng, chưa có registry entry. |
| `/admin/employees/handover` | AdminEmployeeHandover | **group only**: Admin | Route riêng, chưa có registry entry. |
| `/admin/employees/reports` | AdminEmployeeReports | **group only**: Admin | Route riêng, chưa có registry entry. |
| `/admin/employees/contracts` | AdminEmployeeContracts | **group only**: Admin | Route riêng, chưa có registry entry. |
| `/admin/work` | AdminWork | **group only**: Admin | Chưa có registry entry. |
| `/admin/roles` | AdminRoles | **group only**: Admin | Chưa có registry entry. |
| `/admin/block-accounts` | AdminBlockAccounts | **group only**: Admin | Chưa có registry entry. |

**Coverage tĩnh:** 9 route khớp tên/route trong source plugin; 24 route chỉ nằm trong 2 nhóm Employees/Admin; 9 route chưa được map. Tổng 42 màn hình hiện có trong `App.jsx`. Đây không phải số Page đã được tạo trong Figma. Nếu sau này chủ dự án muốn gộp nhiều route cùng một Page/component, ghi quyết định mới trước khi thay quy ước một Page/màn hình.

## 3. Redirect và alias — không đếm là màn hình

| Route / pattern | Target |
| --- | --- |
| `/`, `*` | `/home` |
| `/finance` | `/me/salary` |
| `/hr/payroll` | `/hr` |
| `/admin/employees` | `/hr` |
| `/admin/employees/accounts` | `/admin/accounts` |
| `/admin/employees/promotions` | `/hr/promotions` |
| `/attendance-history`, `/attendance-rules`, `/statistics`, `/leave`, `/reports`, `/promotions`, `/handover`, `/contracts`, `/salary`, `/insurance`, `/bank-accounts` | Thêm prefix `/me` vào đường dẫn cũ, ví dụ `/leave` → `/me/leave`. |

Registry có `/finance` và `/hr/payroll` nhưng chúng chỉ redirect. Không dùng `MODULES.length` hoặc 11 mục `ROUTES` trong plugin làm số màn hình sản phẩm.

## 4. Token / UI Kit trước khi thiết kế màn hình

Tên Variable theo [`13 §4`](../docs/13-figma-sync.md#4-mapping-variable-chính-xác): `color/brand/th-primary`, `color/bg-surface/th-bg`, `color/text/th-text`, `color/status/th-ok`, `color/card/th-card-bg`, `typo/font`. JSON groups `bg_surface`/`attendance_card` không phải tên Figma groups nguyên văn. Shadow là Effect; plugin chỉ khai báo Light mode.

Plugin local có **60 định nghĩa Variables**, **10 component placeholders**; comment56/46 cũ cần đọc theo đính chính ở [`changelog`](tokens/changelog.md). Chưa có binding/variant set hoàn chỉnh và chưa có bằng chứng chạy trong file đích. Kiểm tra build/package/API trước task thực thi; không chạy plugin từ hướng dẫn chưa xác minh.

## 5. Cập nhật và trạng thái sau này

| Trạng thái | Ý nghĩa |
| --- | --- |
| `not-mapped` | Chưa có mục tương ứng trong scaffold/mapping thiết kế được xác minh. |
| `group only` | Có mục nhóm trong source plugin; chưa map riêng route. |
| `draft` | Đã xác nhận node/Page thực tế, nội dung còn nháp. |
| `review` | Hai frame và trạng thái đã có, chờ review. |
| `done` | Có node ids, review thiết kế và kiểm tra code trong phạm vi task; ghi bằng chứng. |
| `outdated` | Code hoặc thiết kế đã thay đổi, bên kia chưa đồng bộ. |
| `deprecated` | Route bị loại, giữ tham chiếu qua một chu kỳ review. |
| `NOT VERIFIED` | Chưa kiểm tra live; có thể đi kèm bất kỳ trạng thái nguồn local nào. |

Khi thêm/đổi/xóa route: đối chiếu App/registry, cập nhật dòng inventory/redirect và [`changelog`](tokens/changelog.md). Khi thực hiện trong Figma, bổ sung URL/node id và thời điểm xác minh; không tự đổi toàn bảng sang `done` từ việc chạy scaffold hoặc build React. Giữ phạm vi được giao và quy tắc cập nhật tại [00](../docs/00-README.md)/[13](../docs/13-figma-sync.md).
