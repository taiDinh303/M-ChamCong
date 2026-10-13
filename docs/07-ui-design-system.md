# 07 — Design system và giao diện chấm công Marixa

> Tác giả & chủ tài liệu: **Đinh Văn Tài**.
> Design system **gắn với mã nguồn thật**: token màu nằm ở `marixa_react/src/theme.css` (nguồn duy nhất), cấu trúc module nằm ở `marixa_react/src/constants/modules.js` và `App.jsx`, layout ở `marixa_react/src/components/layout`.

## 0. Cách đọc (dành cho AI)

- Token màu/số: sửa **một chỗ duy nhất** `theme.css`, toàn app đổi theo.
- Quyền module/sidebar: đọc `MODULES` trong `modules.js` (nguồn quyền giao diện).
- Layout: `AppLayout.jsx` (NV), `AdminLayout`, `HrLayout` trong `components/layout` & các module.

## 1. Hướng thiết kế

Thiết kế bám logo **Marixa** (chữ M xanh lam chuyển cyan, ngôi sao vàng). Nền sáng, bảng rõ, ít trang trí; hiệu ứng chuyển sắc chỉ dùng cho logo/điểm nhấn thương hiệu, không phủ lên bảng dữ liệu.

Nguyên tắc:
- Mobile: một màn một hành động chính (chấm công), vùng chạm đủ lớn.
- Desktop (HR/Admin): bảng rõ, lọc/sort/phân trang, priority là "việc cần xử lý".
- Mọi trạng thái luôn kèm **chữ**, không chỉ màu/icon.
- Giao diện tiếng Việt, ngày `dd/MM/yyyy`, giờ 24h (hiển thị `Asia/Ho_Chi_Minh` qua `vnTime.js`).

## 2. Design tokens (từ `theme.css`)

| Token (CSS var) | Giá trị | Cách dùng |
| --- | --- | --- |
| `--th-primary` | `#1647C8` | Nút chính, link, focus. |
| `--th-primary-strong` | `#0F34A0` | Hover/active của primary. |
| `--th-cyan` | `#00BFD0` | Điểm nhấn phụ, đồ họa. |
| `--th-gold` | `#F3B928` | Nhấn thương hiệu, biểu tượng chú ý. |
| `--th-text` | `#172033` | Nội dung chính. |
| `--th-text-2` | `#526174` | Nội dung phụ. |
| `--th-bg` | `#F5F6F8` | Nền trang. |
| `--th-surface` | `#FFFFFF` | Card/bảng/form. |
| `--th-border` | `#DCE3EC` | Viền. |
| `--th-ok` / `--th-warn` / `--th-err` | `#167347` / `#9A5A00` / `#B42318` | Trạng thái, luôn kèm chữ. |
| `--th-font` | `"Be Vietnam Pro", system-ui` | Font chính, hỗ trợ tiếng Việt. |

Chữ: nội dung 16px, nhãn phụ 14px, tiêu đề 24px, section 18–20px; thời gian dùng chữ số tabular. Khoảng cách 4/8/12/16/24/32px. Bo góc: input/nút 8px, card 12px. Phần tử chạm tối thiểu 44×44px. Tương phản chữ tối thiểu ~4.5:1.

> Kiểm tra: màu logo cần đối chiếu với asset gốc (`marixa_react/public/marixa-logo.png`) khi làm UI; test tương phản từng cặp chữ/nền.

## 3. Cấu trúc điều hướng

- **Nhân viên (module `me`)**: sidebar `attendance` nhóm (Chấm công, Lịch sử, Thống kê, Nghỉ phép, Bảng lương, Hợp đồng, BHXH, Tài khoản NG, Quy định, Báo cáo, Thăng chức, Bàn giao). Lọc `hideInUserSidebar` khỏi sidebar NV.
- **HR/Manager/Admin**: `Sidebar`/`AdminSidebar` khu `/employees` và `/admin`; header gọn + notification + language picker.
- Module đặc biệt: `/work` (Công việc), `/profile` (Hồ sơ), `/kich-hoat` (Kích hoạt), `/unauthorized` (Thiếu quyền).

## 4. Màn hình & trạng thái chính

| Màn hình (route) | Ưu tiên nội dung | Trạng thái cần |
| --- | --- | --- |
| `/login` | Logo, form (tài khoản + mật khẩu + remember), nút | Loading, sai thông tin, khóa tài khoản, lỗi mạng, cần kích hoạt. |
| `/attendance` | Giờ + trạng thái, nút chấm lớn, camera preview, ảnh/GPS | Chưa chấm, đã vào, đã ra, xin quyền camera, ngoài văn phòng, trùng log. |
| `/attendance/attendance-history` | Lịch công, vào/ra, công/ngoại lệ, sửa | Chưa có dữ liệu, thiếu mốc, kỳ khóa. |
| `/attendance/statistics` | KPI thống kê, cảnh báo, bảng chi tiết | Loading, rỗng. |
| `/attendance/leave` | Đơn nghỉ, số phép còn lại | Nháp/chờ/duyệt/từ chối/hủy. |
| `/employees/*` (HR/Admin) | Bảng nhân sự, đối soát, duyệt | Lọc, phân trang, trạng thái duyệt. |
| `/admin` + `/admin/*` | Dashboard, tài khoản, role, khóa tài khoản, hợp đồng, lương, nghỉ việc, báo cáo | Cảnh báo storage, thiếu cấu hình. |

Không hiện tọa độ GPS thô trên danh sách HR; dùng "Trong/Ngoài văn phòng" + chi tiết khi mở record. Ảnh chấm công chỉ tải khi mở chi tiết.

## 5. Nội dung thông báo mẫu

| Tình huống | Câu hiển thị |
| --- | --- |
| Chấm thành công | "Đã ghi nhận VÀO CA." / "Đã ghi nhận RA CA." |
| Chấm trùng gần đây | "Đã có lần chấm cùng loại gần đây. Không cần bấm lại." |
| Camera lỗi | "Không truy cập được camera. Hãy cấp quyền và thử lại." |
| Lỗi API | "Không kết nối được máy chủ API. Hãy bật backend hoặc kiểm tra VITE_API_BASE_URL." |

Nút đang gửi phải khóa tạm; không khiến người dùng bấm lặp.

## 6. Tiếp cận & responsive

Kiểm tra 375/768/1024/1440px, không cuộn ngang ở mobile. Tôn trọng safe area; input có nhãn + lỗi cạnh trường; dialog có Esc + focus hợp lý; bảng có tiêu đề cột + trạng thái bằng chữ. Dùng bộ icon nhất quán, không emoji trong nội dung nghiệp vụ (đổi dần các icon emoji còn sót).

Luồng nghiệp vụ đối chiếu [03-workflow.md](03-workflow.md); quyền module đối chiếu [01-business-analysis.md](01-business-analysis.md) và `MODULES`.
