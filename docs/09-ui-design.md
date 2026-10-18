# 09 — Design system và giao diện Marixa

> Chủ tài liệu: **Đinh Văn Tài**. Đối chiếu mã nguồn: **18/10/2026**.
> **CURRENT** theo source và browser smoke cô lập tại worklog; **TARGET** là tiêu chí thiết kế; thiết bị thật/Figma/hosting live vẫn **NOT VERIFIED**.
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

Quyết định UI 18/10: Home bỏ nhãn ngày. Các layout đã đăng nhập dùng [AuthenticatedShell](../marixa_react/src/components/layout/AuthenticatedShell.jsx): tiêu đề/mô tả trang nằm trong header trên desktop, thành hàng toàn chiều rộng dưới logo/nút tài khoản trên mobile. [HrHero](../marixa_react/src/modules/hr/HrHero.jsx) chuyển tiêu đề qua portal, giữ các ô KPI ở nội dung; tiêu đề dự phòng theo route bảo đảm có tên trang cả khi đang tải. Theo yêu cầu tiếp theo cùng ngày, điện thoại <=768px dùng nút cạnh logo mở dialog điều hướng **toàn màn hình**, trượt từ trái; nút đóng nằm cố định phía trên khi cuộn, chừa safe area và đóng khi chọn trang hoặc Escape. Native dialog quản lý focus, shell khóa cuộn nền và trả focus về nút menu khi đóng. Sidebar desktop vẫn thu gọn được. Bỏ thanh sidebar kéo ngang và hook tự cuộn pill cũ; bảng dữ liệu rộng vẫn được cuộn riêng. Menu phân mục Home trên điện thoại là danh sách thả xuống từ nút cạnh logo. Chưa coi browser viewport là kiểm thử thiết bị thật.

Nút ngôn ngữ dạng chỉ cờ trên Header căn giữa cờ trong khung 42×42 desktop/40×40 mobile; không dành khoảng trống cho caret. Picker có nhãn ở login/kích hoạt vẫn hiển thị tên và caret. Kiểm bằng vị trí thực trên browser và thao tác chọn cả ba ngôn ngữ; không suy từ CSS rằng đã đạt trên thiết bị thật.

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

Inventory route đầy đủ và các redirect: [03](03-architecture.md), [`figma/files-map.md`](../figma/files-map.md). Route nội bộ có RequireSession, menu/guard áp quyền prefix dài nhất; thiết kế “ẩn nút” không thay thế quyền API.

## 4. Màn hình: hiện trạng và tiêu chí cần đạt

| Màn hình / nhóm | CURRENT — có mã nguồn | TARGET / sai khác cần xử lý |
| --- | --- | --- |
| `/login`, `/kich-hoat` | Form đăng nhập / kích hoạt và gọi API tương ứng. | Có loading, lỗi cạnh trường, lỗi mạng, trạng thái kích hoạt; không tuyên bố đã kiểm thử đủ khóa tài khoản/keyboard. |
| `/home` | Trang tổng quan/module cards. | Chỉ hiện hành động phù hợp phiên và quyền; không bỏ route này khỏi mapping thiết kế. |
| `/attendance` | Đồng hồ Việt Nam, camera, vào/ra, vị trí GPS dạng tọa độ, lịch tuần. Ảnh/GPS bắt buộc theo rule, camera tùy chọn khi rule không yêu cầu; UI gửi tọa độ đã lấy. | Ảnh/GPS theo quyết định [02](02-business-analysis.md); UI gửi latitude/longitude khi người dùng bật vị trí. Chưa có geofence. Không hiển thị “Trong/Ngoài văn phòng” như kết quả thật khi chưa tính. |
| `/attendance/attendance-history`, `/attendance/statistics` | Lịch sử/thống kê, xem dữ liệu công; một số luồng có CSV dữ liệu hiện thời. | Loading, rỗng, thiếu mốc, lỗi API rõ ràng. Thẻ kỳ/version/XLSX nằm trong Thống kê; tab OT và sửa công nằm trong Lịch sử; PDF đơn nghỉ ở Nghỉ phép. Live cần nghiệm thu riêng. |
| `/attendance/leave` | Tạo/xem đơn; API dùng Pending/Approved/Rejected/Cancelled theo transition thực tế. | Chủ hủy Pending; HR/Admin khác chủ có nút hủy Approved bằng lý do và hoàn số đã trừ; nháp UI không phải trạng thái DB nếu chưa hỗ trợ. |
| Hợp đồng/lương/bảo hiểm/ngân hàng/quy định cá nhân | Các route/component hiện có dưới `/attendance/*`. | Phân biệt dữ liệu rỗng, thiếu liên kết nhân viên và thiếu quyền; chưa nghiệm thu đầy đủ màn hình. |
| Báo cáo/thăng chức/bàn giao | Có route và quy trình API theo module. | Thể hiện người xử lý/trạng thái đúng API; báo cáo file không đồng nghĩa export bảng công. |
| `/employees/*` | Bảng nhân sự, đối soát, lịch làm, đơn, tài khoản/báo cáo; HR có Excel import/export hồ sơ. | Lọc, sort, phân trang, trạng thái duyệt và quyền theo thao tác; UI hiện có không chứng minh scope phòng ban được enforce tại API. |
| `/admin/*` | Dashboard, tài khoản, role, hợp đồng, phép, lương, nghỉ việc, công việc và báo cáo. | Subroute Admin kế thừa quyền prefix /admin, menu/header lọc quyền; cảnh báo cấu hình/storage chỉ là mục tiêu khi chưa có UI cụ thể. |
| `/work`, `/profile`, `/unauthorized` | Component/route tương ứng đã đăng ký. | Quyền, lỗi rỗng/loading, hành vi responsive phải kiểm tra riêng. |

Nguồn luồng chấm công: [`AttendanceCard.jsx`](../marixa_react/src/modules/me/components/AttendanceCard.jsx), [`relatedApi.js`](../marixa_react/src/modules/me/api/relatedApi.js). `ClockInPage`/lịch tuần dùng nhãn ngày VN; thống kê dùng actualHours/overtimeHours và số dư ledger của API. Giờ tay HR dùng UTC+7 và rollover tháng/năm. Không tự suy OT hoặc cố định giờ nghỉ trong form.

Admin có nút Chỉnh quy định (giờ ca/nghỉ/ảnh/GPS) tại Quy định. HR/Admin có sổ phép và hộp thoại Grant/Adjust có lý do tại Nghỉ phép; Employee chỉ xem lịch sử/số dư. Tự cấp phép hằng tháng vẫn chưa làm. Không thay đổi bố cục/menu chính để thêm các luồng này.

## 5. Nội dung và phản hồi cho người dùng

**TARGET:** dùng tiếng Việt, ngày `dd/MM/yyyy`, giờ 24h theo `Asia/Ho_Chi_Minh`; trạng thái luôn có chữ. Khi đang gửi thì khóa hành động, giữ dữ liệu để có thể thử lại nếu lỗi. Không thông báo thành công trước khi API xác nhận.

| Tình huống | Cách thể hiện |
| --- | --- |
| Chấm thành công | API hiện có thông báo “Đã ghi nhận VÀO CA.” / “Đã ghi nhận RA CA.”; cần làm mới bản ghi. |
| Chấm lặp gần đây | API trả `alreadyRecorded = true`; diễn đạt “Đã có lần chấm cùng loại gần đây. Không cần bấm lại.” |
| Camera bị chặn / không có ảnh | Giải thích cách cấp quyền, chụp lại; chỉ chặn chấm khi rule bắt ảnh. Khi ảnh tùy chọn có thể bỏ qua; GPS cũng theo rule, không gọi tọa độ là kết quả geofence. |
| Lỗi mạng/máy chủ | “Không kết nối được máy chủ. Vui lòng thử lại sau.” Thông tin cổng/backend/biến môi trường dành cho log và tài liệu dev, không đưa vào thông báo người dùng cuối. |
| Thiếu quyền / hết phiên | Phân biệt thiếu quyền với hết phiên; không hiển thị dữ liệu của người khác hoặc dùng “rỗng” để che lỗi quyền. |

**TARGET về dữ liệu ảnh/GPS:** danh sách nghiệp vụ không cần phơi tọa độ thô; khi geofence được triển khai có thể dùng nhãn kèm chi tiết theo quyền. Tải ảnh theo nhu cầu thay vì toàn bộ danh sách. Đây là hướng UI cần thực hiện, không chứng nhận hành vi hiện tại. Quyết định bucket public đang được giữ; không tự đổi thành private trong task giao diện.

## 6. Responsive, tiếp cận và nghiệm thu

Frame thiết kế chính theo quyết định chủ dự án: **mobile 375px**, **desktop 1440px**; kiểm tra bổ sung 768/1024px khi triển khai. **TARGET:** không cuộn ngang toàn trang mobile; bảng rộng có vùng cuộn riêng; form có label/lỗi; focus nhìn rõ, dialog hỗ trợ keyboard/Esc và trả focus; toast/lỗi được công nghệ hỗ trợ nhận biết. Dùng icon nhất quán, trạng thái không chỉ dựa vào màu. Emoji còn có trong code; không ghi như đã được thay hết.

Trước khi đánh dấu UI hoàn thành: đối chiếu API/quyền và trạng thái lỗi, chạy build/lint phù hợp, kiểm tra browser ở kích thước mục tiêu và lưu bằng chứng. Kiểm tra camera/GPS cần thiết bị/quyền thực tế. Đợt **17/10/2026** giữ layout chính, bổ sung luồng đã được chủ dự án đồng ý. Native dialog mới có focus/Escape/busy/error; không xác nhận pixel match, WCAG toàn app hoặc Figma đã đồng bộ. Kịch bản kiểm thử: [06-testing.md](06-testing.md).
