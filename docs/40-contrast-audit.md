# 40 — Rà soát tương phản toàn frontend

> 14/11/2026. Yêu cầu chủ dự án: chữ/nền phải đọc rõ ở sáng và tối trên toàn project; bỏ mục Kế toán · Bảng lương khỏi sidebar Admin. CURRENT theo source và UI fixture local, chưa nghiệm thu toàn bộ dữ liệu/trạng thái production.

## 1. Thay đổi

REQ-CONTRAST-01: di chuyển 2.300 giá trị màu trong 33 stylesheet sang các vai trò ink/surface/line dùng palette sáng/tối. [contrast-palette.css](../marixa_react/src/contrast-palette.css) định nghĩa 626 token; tên token giữ màu nguồn để truy vết. Light ink phụ được làm đậm khi cần; dark ink sáng và surface tối, không lấy màu chữ light áp lên surface dark. Các màu ngữ nghĩa, vùng header/hero, đồng hồ kim và status có quy tắc riêng tại erp-theme.css. Không đổi hình cờ SVG, ảnh nhân viên hoặc dữ liệu nghiệp vụ.

REQ-CONTRAST-02: sửa menu tài khoản cả thường/hover/danger, panel/bảng/search/filter/count của cấp tài khoản; clock/tile dashboard; lịch ngày nghỉ và header lịch; nhãn/empty/pagination; nút và hướng dẫn; chart series/grid/tick/tooltip. Disabled control vẫn có màu chữ đọc được, không chỉ giảm opacity. Các reference màu primary dùng cho chữ được tách sang link ink thay vì đổi màu nền của nút primary.

REQ-CONTRAST-03: màu người dùng chọn cho icon dự án/preview, avatar cấp tài khoản/role/nghỉ phép và icon quy định chọn chữ đen hoặc trắng có contrast cao hơn bằng [readableInk](../marixa_react/src/utils/readableInk.js). Avatar trang chặn tài khoản và số thành viên role dùng cặp surface/text theo theme; badge phòng ban Profile bỏ cặp màu inline cố định. Header thẻ dự án bỏ nền alpha tùy ý để dùng surface-alt, tránh chữ sáng trên nền dự án quá nhạt ở dark.

REQ-NAV-1114: xóa riêng link Kế toán · Bảng lương trong AdminSidebar. Route/quyền Kế toán và thẻ Home giữ nguyên; không thay quyền backend.

## 2. Phạm vi UI đã đo

Dùng App/router thật với adapter API giả, employee mẫu example.test và health stub local. Không đăng nhập API thật, không đọc/ghi dữ liệu live. Fixture tạm và dev server được gỡ/dừng sau kiểm tra. Ban đầu fixture reports trả sai shape khiến trang lỗi; đã sửa shape adapter, không sửa nghiệp vụ để che lỗi fixture.

32 route được quét ở mỗi theme:

- Home; Admin dashboard/accounts/contracts/leaves/resigned/reports/work/roles/block-accounts.
- HR danh sách/attendance-history/work-schedule/statistics/catalogs/handover/promotions.
- Me chấm công/attendance-history/statistics/leave/salary/contracts/insurance/bank-accounts/attendance-rules/reports/promotions/handover.
- Work, Profile, Accounting payroll.

TC-CONTRAST-DOM đo phần tử có text trực tiếp, có layout, fg/background đặc xác định được qua ancestor; ngưỡng 4.5:1 cho text thường, 3:1 cho chữ lớn. Dark lượt cuối 32 route/1.633 phần tử không còn lỗi trong vùng đo. Light lượt 32 route phát hiện 4 vị trí thuộc accounts/block/promotions; sau sửa quét lại bốn route không còn lỗi. Menu tài khoản và hộp thoại đổi mật khẩu được đo riêng; nút Lưu mật khẩu được sửa sau phát hiện nền sáng/chữ trắng. Screenshot desktop quan sát bảng/menu tối, xác nhận sidebar không còn link bị yêu cầu bỏ.

Không đưa kết quả này thành cam kết mọi pixel đạt WCAG: phép đo bỏ qua SVG text, gradient/ảnh, alpha compositing phức tạp, disabled và các nội dung chưa có trong fixture. Bộ dữ liệu mẫu phần lớn ít/không có bản ghi. Chưa kiểm mọi popup/trạng thái/hover/dữ liệu thật, mobile hoặc live. Những trang trùng component và alias không được tính thành coverage riêng.

## 3. Kiểm tra và bảo trì

- Frontend 87/87 test đạt, lint/build đạt. Hai regression mới kiểm lựa chọn chữ trên 4.096 màu RGB và tính đầy đủ light/dark/reference của palette. Test palette không thay browser QA.
- Build vẫn có cảnh báo bundle lớn. CSS tăng vì khai báo hai mode cho palette; không nâng ngưỡng để giấu cảnh báo.
- Trước thêm màu: dùng vai trò surface/ink/line hoặc token ngữ nghĩa có đủ hai theme. Màu dự án nhập tay phải có foreground tương ứng; không hardcode light ink lên light surface hoặc dark ink lên dark surface.
- Backend/config/schema không đổi, không chạy lại test backend/migration/deploy trong việc này. Evidence thực thi và publication tại [worklog](worklog.md).
