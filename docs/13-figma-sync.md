# 13 — Đồng bộ Figma và mã nguồn

> Chủ tài liệu: **Đinh Văn Tài**. Chuẩn hóa theo checkout: **16/10/2026**.
> **CURRENT:** repo có snapshot token, mapping và mã plugin scaffold. **NOT VERIFIED:** chưa mở/đọc/chạy plugin trong Figma ở lượt này; chưa xác nhận Variables, UI Kit, frames hoặc Dev Mode live.
> **EN — Codex / Agnes:** This is a text-only sync contract. Read [00](00-README.md) → this file → [Figma README](../figma/README.md) → [changelog](../figma/tokens/changelog.md) → [tokens](../figma/tokens/tokens.json) → [files-map](../figma/files-map.md). Do not mark local scaffolding as a verified Figma result.

## 1. Quyết định đang giữ

| Chủ đề | Quyết định của chủ dự án đã ghi trong repo |
| --- | --- |
| File đích | [Marixa001](https://www.figma.com/design/2TYFTZweE1j2enGLUNXaq3/Marixa001), key `2TYFTZweE1j2enGLUNXaq3`; team/project `Marixa_001`, owner `taiDz`. Thông tin Free plan là ghi nhận trước; chưa xác minh quyền/plan hiện tại. |
| Workflow | **Dev Mode copy (Q4=C)**: đối chiếu/copy giá trị token và thông số layout để implement có kiểm soát. Không dùng codegen làm luồng chính. |
| Thứ tự thiết kế | **UI Kit trước (Q6=B)**, sau đó màn hình. |
| Layout | **Hai frame/màn hình (Q5=B)**: mobile rộng **375px**, desktop rộng **1440px**; target một Page cho một route hiển thị. |
| Lưu trong Git | Tham chiếu URL/node id, JSON/Markdown và mã nguồn công cụ. Không lưu file thiết kế binary `.fig`, không commit access token. |

Đây là các lựa chọn đã ghi nhận của chủ dự án, không phải bằng chứng đã áp dụng trong file Figma. Tên Page có thể dễ đọc như `Login`; mapping phải ghi chính xác route. Redirect không cần Page riêng trừ khi có yêu cầu thiết kế được ghi nhận.

## 2. Nguồn chuẩn và trạng thái dữ liệu

| Nội dung | Nguồn quyết định / hiện trạng | Vai trò của snapshot |
| --- | --- | --- |
| Token code | [`theme.css`](../marixa_react/src/theme.css) | [`tokens.json`](../figma/tokens/tokens.json) mirror giá trị CSS và thêm UI metrics từ [09 §2](09-ui-design.md#2-token-hiện-tại-và-chuẩn-đo-lường-mục-tiêu). Không dùng Figma âm thầm ghi đè code. |
| Route | [`App.jsx`](../marixa_react/src/App.jsx) xác định màn hình/redirect; [`modules.js`](../marixa_react/src/constants/modules.js) xác định registry menu/quyền UI. | [`files-map.md`](../figma/files-map.md) ghi đủ route và coverage thiết kế; không suy ra mọi route có trong registry. |
| Ý định UI | [09-ui-design.md](09-ui-design.md), nghiệp vụ [02](02-business-analysis.md)/[05](05-workflow.md) và quyết định chủ dự án. | Frame Figma mô tả thiết kế cần đạt, không chứng minh backend đã có chức năng. |
| Component | Component React/CSS và UI Kit Figma. | Cần đối chiếu tên, variants, states và bindings khi triển khai; placeholder không phải thư viện hoàn chỉnh. |

Thay đổi tài liệu cho đúng source/khối việc được giao được thực hiện cùng task theo [00](00-README.md). Thay đổi lựa chọn nghiệp vụ, bảo mật hoặc kiến trúc cần ghi quyết định của chủ dự án. Không áp dụng quy tắc “đông băng mọi file”; cũng không coi quyền sửa tài liệu là quyền thay đổi Figma live hay code ngoài phạm vi task.

## 3. Scaffold hiện có và thiếu sót

| Artifact | CURRENT — kiểm tra tĩnh ngày 16/10/2026 | Việc còn lại |
| --- | --- | --- |
| CSS/mirror | `theme.css` có **55 biến**: 50 màu + 4 shadow + 1 font; snapshot chứa thêm spacing/radius/typography/touch/contrast/breakpoints mục tiêu. | Khi đổi, so sánh từng key/value; không gọi tất cả UI metrics là CSS variables hiện có. |
| Plugin [`setup-design-tokens.ts`](../figma/plugins/setup-design-tokens.ts) | Mảng dữ liệu thực tế có **60 định nghĩa variable**: 50 COLOR + 9 FLOAT + 1 STRING; comment cũ ghi 56/46 không chính xác. Mode khai báo là `Light`. | Kiểm tra tương thích API, build/package và chạy thử trong task Figma được giao trước khi dùng. |
| UI Kit scaffold | 10 component placeholders: 4 button, Input, Card, 2 badge, Dialog, Toast. Fills/font dùng giá trị trực tiếp; chưa có binding variable/variant set đầy đủ. | Xây variants/states/auto-layout/bindings; bổ sung table và các component cần cho sản phẩm. |
| Page scaffold | 11 mục trong `ROUTES`, gồm `/employees/*` và `/admin/*` là **nhóm**; mỗi mục dự kiến 375×812 và 1440×900. | Không bao phủ 1:1 toàn bộ route. Tách từng route theo inventory và quyết định một Page/màn hình. |
| Runtime Figma | Không có node id/bằng chứng chạy plugin trong lượt này. | Xác minh collection, Variables, Kit và từng Page/frame khi có task Figma live. |

[`setup-design-tokens.manifest.json`](../figma/plugins/setup-design-tokens.manifest.json) hiện không khai báo `main`, source là TypeScript; repo chưa có bước build/package đã được xác nhận. Vì vậy **không coi “paste rồi Run” hoặc chạy lặp an toàn là hướng dẫn đã kiểm chứng**. Không chạy lại trên file đích trước khi hiểu tác động tới collection/Page đã tồn tại. Lượt chuẩn hóa này không sửa plugin/manifest/tokens/CSS.

## 4. Mapping variable chính xác

Tên variable phải theo mảng trong plugin, không rút gọn thành `color/th-primary`. Key JSON là snapshot path; một số tên nhóm không giống đường dẫn Figma.

| CSS / metric | JSON path | Figma variable dự kiến trong plugin |
| --- | --- | --- |
| `--th-primary` | `color.brand.th-primary` | `color/brand/th-primary` |
| `--th-header-from` | `color.header.th-header-from` | `color/header/th-header-from` |
| `--th-side-bg` | `color.sidebar.th-side-bg` | `color/sidebar/th-side-bg` |
| `--th-bg` | `color.bg_surface.th-bg` | `color/bg-surface/th-bg` |
| `--th-text` | `color.text.th-text` | `color/text/th-text` |
| `--th-ok` | `color.status.th-ok` | `color/status/th-ok` |
| `--th-card-bg` | `color.attendance_card.th-card-bg` | `color/card/th-card-bg` |
| `--th-font` | `typography.th-font` | `typo/font` (STRING) |
| Spacing 4/8/12/16/24/32px | `spacing_scale` | `spacing/4`, `spacing/8`, … (FLOAT, đơn vị px) |
| Radius 8px / 12px | `radius.input_button`, `radius.card` | `radius/input-button`, `radius/card` |
| Touch 44px | `touch.min_target` | `touch/min-target` |

Bốn shadow trong CSS/mirror không tạo thành COLOR variables; biểu diễn bằng Effect khi thiết kế và ghi tham chiếu tương ứng. Font size/contrast/breakpoints trong snapshot chưa được plugin tạo thành variable. Không yêu cầu Dark mode như thể đã triển khai.

## 5. Workflow cho task đồng bộ sau này

### 5.1 Chuẩn bị và phạm vi

1. Đọc changelog và mapping; xác định route/component/token và hướng thay đổi. Kiểm tra task cho phép sửa code, Figma hay chỉ tài liệu.
2. Giữ quyết định ở §1. Nếu task đổi quyết định, ghi lựa chọn/ảnh hưởng/owner decision trước khi áp dụng.
3. Với thao tác Figma live, dùng skill/tool Figma phù hợp khi thực sự triển khai; ghi URL + node id + thời điểm kiểm tra. Không dùng trạng thái trong Markdown làm bằng chứng live.

### 5.2 Token hoặc layout: Figma → code

1. Đối chiếu variable/component trong Figma với mapping. Dev Mode là workflow đã chọn; khả năng truy cập và CSS copy phụ thuộc phiên/quyền thực tế, **chưa được xác minh**. Không giả định luôn có `var(--th-primary)` trong kết quả copy.
2. Lấy giá trị thiết kế, cập nhật `theme.css`/component trong phạm vi task; cập nhật `tokens.json` nếu token đổi. Giá trị mới phải có quyết định phù hợp, không tự thay đổi chính sách nghiệp vụ từ layout.
3. Thêm changelog `token-change`/`layout-change`, hướng `Figma→code`, các file/node thay đổi và trạng thái còn chờ.
4. Build/lint và kiểm tra browser responsive/states theo [06](06-testing.md)/[09](09-ui-design.md); sửa lỗi trước khi ghi hoàn thành.
5. Ghi bằng chứng worklog; nếu chỉ code đã kiểm tra mà Figma chưa kiểm tra thì dùng `repo-verified; Figma NOT VERIFIED`.

### 5.3 Code → Figma hoặc thêm/đổi/xóa route

Đối chiếu `App.jsx` trước, sau đó cập nhật `files-map.md` và changelog. Với target một Page/màn hình, `/employees/*` hoặc `/admin/*` chỉ là nhãn nhóm coverage, không phải Page đã bao phủ mọi route con. Tạo/cập nhật Kit trước Page; giữ hai frame 375/1440. Redirect ghi target, không đếm là màn hình mới. Route xóa giữ trạng thái `deprecated` trong một chu kỳ review trước khi xóa mapping; Figma chỉ đổi khi task cho phép.

Đổi code có thể hoàn tất trước đồng bộ thiết kế; phải ghi `outdated`/`not-mapped` cho phần còn thiếu. Không đánh dấu `done` chỉ vì build pass hoặc plugin tạo frame rỗng.

### 5.4 Codegen — TARGET, không phải workflow chính

[`figma/generated/`](../figma/generated/README.md) chỉ nhận output nháp nếu sau này được giao task codegen. Review thủ công về component/token/quyền/accessibility và kiểm tra trước khi đưa vào `marixa_react/src/`; không tự merge hoặc auto-commit. Không tự kích hoạt codegen theo số lượng designer/màn hình.

## 6. Tiêu chí ghi nhận hoàn thành

- **Tài liệu đã xác minh tĩnh:** đường dẫn có thật, key/value/token/route khớp source, trạng thái không phóng đại; không cần giả lập build thành công cho thay đổi Markdown.
- **Repo/code đã xác minh:** ghi lệnh + kết quả build/lint/test/browser thực tế, chỉ trong phạm vi đã kiểm tra.
- **Figma đã xác minh:** ghi node id, hai frame có nội dung và trạng thái cần thiết, component/variable binding và người review. `done` cho một màn hình cần cả phần thiết kế và đối chiếu code được giao.
- **Chưa kiểm tra:** giữ `NOT VERIFIED`, nêu phần thiếu; một nhãn ✅ lịch sử không tự chứng minh kết quả hiện tại.

Tài liệu liên quan: [09 — UI](09-ui-design.md), [10 — Local](10-setup-local.md), [12 — Thuật ngữ](12-glossary.md), [worklog.md](worklog.md), [`figma/files-map.md`](../figma/files-map.md).
