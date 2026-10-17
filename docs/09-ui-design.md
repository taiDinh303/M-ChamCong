# 09 — Design system và giao diện Marixa

> Chủ tài liệu: **Đinh Văn Tài**. Đối chiếu mã nguồn: **17/10/2026**.
> **CURRENT** mô tả UI có mã nguồn; **TARGET** là tiêu chí thiết kế cần đạt; **NOT VERIFIED** nghĩa chưa có kiểm tra browser/thiết bị/Figma live trong lượt này.
> **EN — Codex / Agnes:** Implement only authorized UI work. Keep current behavior and target UX separate. `theme.css` owns code token values; do not infer permissions or completed features from a design frame.

## 1. Nguồn thiết kế và phạm vi

| Nội dung | Nguồn chuẩn | Cách dùng |
| --- | --- | --- |
| Giá trị CSS token | [`theme.css`](../marixa_react/src/theme.css) | Nguồn giá trị màu/font/shadow đang có trong code. Không bảo đảm mọi CSS trong app đã dùng token. |
| Route và module | [`App.jsx`](../marixa_react/src/App.jsx), [`modules.js`](../marixa_react/src/constants/modules.js) | Route đăng ký thực tế và cấu hình menu/quyền UI; các khoảng trống quyền được ghi ở [03](03-architecture.md). |
| Layout | [`AppLayout.jsx`](../marixa_react/src/components/layout/AppLayout.jsx), [`HrAppLayout.jsx`](../marixa_react/src/modules/hr/layout/HrAppLayout.jsx), layout của module Admin | Đối chiếu component/CSS trước khi tạo thêm layout. |
| Figma và snapshot token | [13-figma-sync.md](13-figma-sync.md), [`figma/README.md`](../figma/README.md) | Quy trình đồng bộ; scaffold chưa chứng minh thiết kế đã có trong Figma. |
| Nghiệp vụ / trạng thái | [02-business-analysis.md](02-business-analysis.md), [05-workflow.md](05-workflow.md) | Không thêm trạng thái có thể thao tác nếu API chưa hỗ trợ. |

Thiết kế hướng tới nhận diện Marixa: xanh lam/cyan, điểm nhấn vàng; nền sáng, nội dung rõ, ưu tiên thao tác chấm công trên mobile và đối soát/bảng dữ liệu trên desktop. Logo tham chiếu: [`marixa-logo.png`](../marixa_react/public/marixa-logo.png). Màu asset và tương phản thực tế **NOT VERIFIED** trong lượt này.

## 2. Token hiện tại và chuẩn đo lường mục tiêu

Các giá trị dưới đây đã đối chiếu [`theme.css`](../marixa_react/src/theme.css); đầy đủ hiện có **55 biến `--th-*`** (50 màu, 4 shadow, 1 font). [`figma/tokens/tokens.json`](../figma/tokens/tokens.json) là bản mirror, không thay thế CSS trong app.

| Token | CURRENT — giá trị | Mục đích |
| --- | --- | --- |
| `--th-primary` / `--th-primary-strong` | `#1647C8` / `#0F34A0` | Hành động chính / trạng thái nhấn. |
| `--th-cyan` / `--th-gold` | `#00BFD0` / `#F3B928` | Điểm nhấn thương hiệu. |
| `--th-text` / `--th-text-2` | `#172033` / `#526174` | Nội dung chính / phụ. |
| `--th-bg` / `--th-surface` | `#F5F6F8` / `#FFFFFF` | Nền trang / bề mặt. |
| `--th-border` | `#DCE3EC` | Viền chung. |
| `--th-ok` / `--th-warn` / `--th-err` | `#167347` / `#9A5A00` / `#B42318` | Thành công / cảnh báo / lỗi; cần kèm chữ. |
| `--th-font` | `"Be Vietnam Pro", "Inter", ui-sans-serif, system-ui, sans-serif` | Font stack trong CSS; việc tải/render đúng font cần kiểm tra browser. |

**TARGET — chuẩn UI, chưa phải toàn bộ CSS token đã triển khai:** nội dung 16px, nhãn phụ 14px, tiêu đề 24px, section 18–20px; số giờ dùng `tabular-nums`. Spacing 4/8/12/16/24/32px; radius input/nút 8px, card 12px; vùng chạm tối thiểu 44×44px. Tương phản chữ thường tối thiểu 4.5:1. Đây là UI metrics trong `tokens.json`; không suy ra các biến spacing/radius tương ứng đã tồn tại trong `theme.css`.

Đổi token theo [13](13-figma-sync.md): sửa CSS và mirror cùng thay đổi, ghi hướng đồng bộ/bằng chứng. Không tự chuyển theme Dark; plugin scaffold hiện chỉ khai báo mode `Light`.

## 3. Điều hướng hiện tại

- Cá nhân: nhóm `/attendance/*` cho công, thống kê, phép, hợp đồng, lương, bảo hiểm, tài khoản ngân hàng và hành chính; `hideInUserSidebar` ẩn mục khỏi sidebar cá nhân.
- Nhân sự: `/employees/*`, registry hiện cho `HR`, `Manager`, `Admin`; Admin có nhóm `/admin/*`.
- Trang dùng chung: `/home`, `/work`, `/profile`; `/login`, `/kich-hoat`, `/unauthorized` cho truy cập/phiên/quyền.
- `/finance` chỉ chuyển tới `/attendance/salary`; `/employees/payroll` chuyển tới `/employees`. Không thiết kế chúng như màn hình mới đang hoạt động.

Inventory route đầy đủ và các redirect: [03](03-architecture.md), [`figma/files-map.md`](../figma/files-map.md). Wrapper route/menu hiện còn thiếu kiểm tra; thiết kế “ẩn nút” không thay thế quyền API.

## 4. Màn hình: hiện trạng và tiêu chí cần đạt

| Màn hình / nhóm | CURRENT — có mã nguồn | TARGET / sai khác cần xử lý |
| --- | --- | --- |
| `/login`, `/kich-hoat` | Form đăng nhập / kích hoạt và gọi API tương ứng. | Có loading, lỗi cạnh trường, lỗi mạng, trạng thái kích hoạt; không tuyên bố đã kiểm thử đủ khóa tài khoản/keyboard. |
| `/home` | Trang tổng quan/module cards. | Chỉ hiện hành động phù hợp phiên và quyền; không bỏ route này khỏi mapping thiết kế. |
| `/attendance` | Đồng hồ Việt Nam, camera, vào/ra, vị trí GPS dạng tọa độ, lịch tuần. UI yêu cầu ảnh mới và upload thành công trước mỗi lần chấm. | Chính sách nghiệp vụ ảnh/GPS tùy chọn đang khác UI; xem [02](02-business-analysis.md). Frontend chưa gửi GPS; backend đã nhận/lưu optional latitude/longitude/deviceId. Chưa có geofence. Không hiển thị “Trong/Ngoài văn phòng” như kết quả thật khi chưa tính. |
| `/attendance/attendance-history`, `/attendance/statistics` | Lịch sử/thống kê, xem dữ liệu công; một số luồng có CSV dữ liệu hiện thời. | Loading, rỗng, thiếu mốc, lỗi API rõ ràng. “Kỳ khóa”, snapshot bất biến và export snapshot XLSX/PDF chưa triển khai. |
| `/attendance/leave` | Tạo/xem đơn; API dùng Pending/Approved/Rejected/Cancelled theo transition thực tế. | Không thêm nút hủy đơn Approved hoặc gọi “hoàn phép” là hiện có; nháp UI không phải trạng thái DB nếu chưa hỗ trợ. |
| Hợp đồng/lương/bảo hiểm/ngân hàng/quy định cá nhân | Các route/component hiện có dưới `/attendance/*`. | Phân biệt dữ liệu rỗng, thiếu liên kết nhân viên và thiếu quyền; chưa nghiệm thu đầy đủ màn hình. |
| Báo cáo/thăng chức/bàn giao | Có route và quy trình API theo module. | Thể hiện người xử lý/trạng thái đúng API; báo cáo file không đồng nghĩa export bảng công. |
| `/employees/*` | Bảng nhân sự, đối soát, lịch làm, đơn, tài khoản/báo cáo; HR có Excel import/export hồ sơ. | Lọc, sort, phân trang, trạng thái duyệt và quyền theo thao tác; UI hiện có không chứng minh scope phòng ban được enforce tại API. |
| `/admin/*` | Dashboard, tài khoản, role, hợp đồng, phép, lương, nghỉ việc, công việc và báo cáo. | Đồng bộ registry/guard cho route còn thiếu; cảnh báo cấu hình/storage chỉ là mục tiêu khi chưa có UI cụ thể. |
| `/work`, `/profile`, `/unauthorized` | Component/route tương ứng đã đăng ký. | Quyền, lỗi rỗng/loading, hành vi responsive phải kiểm tra riêng. |

Nguồn luồng chấm công: [`AttendanceCard.jsx`](../marixa_react/src/modules/me/components/AttendanceCard.jsx), [`relatedApi.js`](../marixa_react/src/modules/me/api/relatedApi.js). `ClockInPage` và lịch tuần còn dùng ngày của trình duyệt ở một số phép so sánh; không khẳng định mọi phép ngày đã dùng `Asia/Ho_Chi_Minh`.

## 5. Nội dung và phản hồi cho người dùng

**TARGET:** dùng tiếng Việt, ngày `dd/MM/yyyy`, giờ 24h theo `Asia/Ho_Chi_Minh`; trạng thái luôn có chữ. Khi đang gửi thì khóa hành động, giữ dữ liệu để có thể thử lại nếu lỗi. Không thông báo thành công trước khi API xác nhận.

| Tình huống | Cách thể hiện |
| --- | --- |
| Chấm thành công | API hiện có thông báo “Đã ghi nhận VÀO CA.” / “Đã ghi nhận RA CA.”; cần làm mới bản ghi. |
| Chấm lặp gần đây | API trả `alreadyRecorded = true`; diễn đạt “Đã có lần chấm cùng loại gần đây. Không cần bấm lại.” |
| Camera bị chặn / không có ảnh | Giải thích cách cấp quyền, chụp lại; hiện UI không cho chấm khi thiếu ảnh. Việc hỗ trợ bỏ qua ảnh cần sửa theo quyết định nghiệp vụ. |
| Lỗi mạng/máy chủ | “Không kết nối được máy chủ. Vui lòng thử lại sau.” Thông tin cổng/backend/biến môi trường dành cho log và tài liệu dev, không đưa vào thông báo người dùng cuối. |
| Thiếu quyền / hết phiên | Phân biệt thiếu quyền với hết phiên; không hiển thị dữ liệu của người khác hoặc dùng “rỗng” để che lỗi quyền. |

**TARGET về dữ liệu ảnh/GPS:** danh sách nghiệp vụ không cần phơi tọa độ thô; khi geofence được triển khai có thể dùng nhãn kèm chi tiết theo quyền. Tải ảnh theo nhu cầu thay vì toàn bộ danh sách. Đây là hướng UI cần thực hiện, không chứng nhận hành vi hiện tại. Quyết định bucket public đang được giữ; không tự đổi thành private trong task giao diện.

## 6. Responsive, tiếp cận và nghiệm thu

Frame thiết kế chính theo quyết định chủ dự án: **mobile 375px**, **desktop 1440px**; kiểm tra bổ sung 768/1024px khi triển khai. **TARGET:** không cuộn ngang toàn trang mobile; bảng rộng có vùng cuộn riêng; form có label/lỗi; focus nhìn rõ, dialog hỗ trợ keyboard/Esc và trả focus; toast/lỗi được công nghệ hỗ trợ nhận biết. Dùng icon nhất quán, trạng thái không chỉ dựa vào màu. Emoji còn có trong code; không ghi như đã được thay hết.

Trước khi đánh dấu UI hoàn thành: đối chiếu API/quyền và trạng thái lỗi, chạy build/lint phù hợp, kiểm tra browser ở kích thước mục tiêu và lưu bằng chứng. Kiểm tra camera/GPS cần thiết bị/quyền thực tế. Lượt **16/10/2026 sửa backend/tài liệu, không sửa giao diện**, không xác nhận pixel match, font, tương phản hoặc accessibility đã đạt. Kịch bản kiểm thử: [06-testing.md](06-testing.md).
