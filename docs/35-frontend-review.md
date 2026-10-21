# 35 — Rà soát frontend Home, HR, Me và Admin

> 20/10/2026. CURRENT là tính năng tìm thấy trong source; kiểm thử cô lập được ghi riêng ở worklog. Không gọi toàn bộ nghiệp vụ đã nghiệm thu chỉ vì trang mở được. Báo cáo tập trung chức năng thiết thực theo yêu cầu chủ dự án.

## 1. Phạm vi và thay đổi

- URL chính: `/home`, `/me/*`, `/hr/*`, `/admin/*`; `/work` và `/profile` là trang dùng chung. Kiểm kê ban đầu có 33 trang chính và hai trang dùng chung; bổ sung module Kế toán 21/10 tại [36](36-workflow-improvements.md), không tính alias thành chức năng riêng.
- `/attendance/*` → `/me/*`, `/employees/*` → `/hr/*`, giữ query/hash và quyền; các URL ngắn cũ vẫn chuyển đến trang mới. Endpoint backend không đổi.
- Xóa app-page-heading, portal và CSS tương ứng. Giữ KPI, bộ lọc, thao tác và tiêu đề hộp thoại; tên tab trình duyệt/nhãn main nhận diện trang. Menu mobile vẫn toàn màn hình, không có thanh kéo ngang.
- Bổ sung ba liên kết Admin và dùng chung trang Nghỉ phép với HR. Thông báo nhấn trực tiếp phần chữ theo role, bỏ “Mở →”, đóng bằng Escape, vẫn giữ trạng thái đã đọc theo tài khoản trên thiết bị.
- Sửa bốn trang dữ liệu cá nhân dùng EntityDetailPage: lỗi API không bị coi là dữ liệu rỗng, số lượng chưa biết hiển thị dấu gạch và có Thử lại.
- Trang nhân sự nghỉ việc đọc cả danh sách đang lưu và đã lưu trữ, gộp theo ID và lọc đúng status nghỉ/chấm dứt; không ép mọi hồ sơ đã lưu trữ thành nghỉ việc. Fetcher ổn định để tránh vòng lặp tải khi render.
- Từ20/10, login tải trước danh sách theo quyền, cacheJSON trong RAM theo phiên và animation chờ API/reconnect; invalidation sau ghi/logout và chuyển trang SPA. Tệp/nhóm dữ liệu chưa tải vẫn theo nhu cầu; không xây offline queue hoặc ping giữ Render thức. Chính sách tại [25](25-performance-capacity.md), bằng chứng tại worklog.

Nguồn chính: [router](../marixa_react/src/App.jsx), [registry quyền](../marixa_react/src/constants/modules.js), [shell](../marixa_react/src/components/layout/AuthenticatedShell.jsx), [thông báo](../marixa_react/src/components/layout/NotificationBell.jsx). Sidebar chỉ dẫn đường; API vẫn quyết định quyền dữ liệu.

## 2. Ma trận trang chính — CURRENT theo source

| Trang | Chức năng cơ bản hiện có | Kết luận / giới hạn |
| --- | --- | --- |
| `/home` | Thẻ module lọc theo role, vào chấm công, tới Admin, thông báo/tài khoản | Kế toán mở `/accounting`, chỉ hiện cho Admin/Accountant theo quyết định 21/10; lương cá nhân giữ ở Me. Thẻ Kích hoạt còn hiển thị cho người đã đăng nhập. |
| `/me` | Vào/ra, ảnh/GPS theo quy định, công hôm nay/lịch tuần | Camera/GPS thiết bị thật chưa được nghiệm thu trong lượt này. |
| `/me/attendance-history` | Lịch sử, gửi/xem OT, yêu cầu sửa công | Có luồng cơ bản; chính sách tự duyệt ở API giữ nguyên. |
| `/me/statistics` | Công thường/OT, cảnh báo, lịch/biểu đồ theo kỳ | Không suy số giờ lương từ thời gian ở văn phòng. |
| `/me/leave` | Tạo đơn, xem số dư/sổ phép, hủy Pending, tải PDF | Tự cấp phép định kỳ chưa có và không thêm khi thiếu chính sách. |
| `/me/salary` | Xem dữ liệu lương cá nhân | Chỉ đọc theo quyền; đã sửa hiển thị lỗi/thử lại. |
| `/me/contracts` | Xem hợp đồng cá nhân | Chỉ đọc; không tạo giả chức năng ký điện tử. |
| `/me/insurance` | Xem bảo hiểm/thuế | Chỉ đọc; đã sửa hiển thị lỗi/thử lại. |
| `/me/bank-accounts` | Xem tài khoản ngân hàng | Không mở quyền tự sửa thông tin chi trả khi chưa có quyết định. |
| `/me/attendance-rules` | Xem ca/nghỉ/ảnh/GPS, Admin có hộp thoại chỉnh | Ca làm chung hiện hành, không thêm version nghiệp vụ mới. |
| `/me/reports` | Nộp lại báo cáo, tải bản/file đính kèm | File private cần storage bền trên hosting. |
| `/me/promotions` | Gửi/xem đề xuất thăng chức cá nhân | Ngày hiệu lực và quyền vẫn do API xử lý. |
| `/me/handover` | Tạo/xem bàn giao, đính kèm | Không thay quá trình nghỉ việc bằng thao tác xóa tài khoản. |
| `/hr` | Hồ sơ, tạo/sửa, tìm/lọc/phân trang, nhập/xuất, lưu trữ/khôi phục, dữ liệu liên quan | Có nền tảng cơ bản; thiếu màn hình quản lý danh mục dùng chung, xem mục 3. |
| `/hr/attendance-history` | Đối soát, tạo/sửa công, duyệt, OT và sửa công | Giữ ràng buộc kỳ khóa và self-review của API. |
| `/hr/statistics` | Tổng hợp, review kỳ, Admin khóa/mở, snapshot/history và XLSX | Giữ export theo snapshot/version. |
| `/hr/work-schedule` | Xem lịch, gán ca hàng loạt, sửa gán ca/giờ ca | Có danh mục tạo/ngừng ca cho Admin/HR, gán 1+ người/31 ngày/bỏ Chủ nhật theo [36](36-workflow-improvements.md). |
| `/hr/contracts` | Danh sách/tìm/lọc, tạo và xem hợp đồng | Chưa có thao tác sửa hợp đồng ngay trong trang danh sách; API đã có update. |
| `/hr/leaves` | Duyệt/từ chối, hủy Approved đúng quyền, PDF, cấp/điều chỉnh/sổ phép | Dùng chung với Admin; không tự duyệt đơn nghỉ của mình. |
| `/hr/reports` | Xem/tải/review/chuyển cấp trên/yêu cầu sửa/hoàn thành báo cáo | Không thay workflow nhiều bước bằng một nút duyệt giả. |
| `/hr/accounts` | Chọn nhân viên, cấp/cấp lại mã kích hoạt | Không mở quản lý role Admin cho HR. |
| `/hr/promotions` | Đề cử, xem, duyệt/từ chối | Giữ ngày hiệu lực/guard Admin cuối. |
| `/hr/handover` | Xem/xử lý hồ sơ bàn giao và file | Quyền tải file phải được API kiểm tra. |
| `/admin` | Shortcut nghiệp vụ, biểu đồ chấm công | Có loading/lỗi/Thử lại và số liệu dạng chữ; lỗi không bị đổi thành dãy 0. |
| `/admin/contracts` | Cùng bảng hợp đồng như HR trong layout Admin | Không phát triển hai workflow hợp đồng khác nhau. |
| `/admin/leaves` | Cùng component và thao tác Nghỉ phép của HR | Sidebar Admin giữ đúng phân khu; đã bỏ bản UI trùng. |
| `/accounting`, `/accounting/payroll` | Tổng quan, lọc nhân viên/kỳ/trạng thái, chi tiết và CSV | Admin/Accountant chỉ đọc. Các URL bảng lương cũ chuyển về đây; chưa có tự tính/duyệt/chi. |
| `/admin/resigned` | Xem/tìm/xuất hồ sơ nghỉ/chấm dứt | Đã gồm hồ sơ lưu trữ và sửa vòng lặp fetch. |
| `/admin/reports` | Cùng workflow báo cáo, quyền Admin | Không giảm quyền kiểm tra file. |
| `/admin/work` | Thêm/sửa/xóa quá trình công tác, tìm/lọc/xuất | Chức năng cơ bản đã có theo source. |
| `/admin/roles` | Tạo/sửa/xóa role, gán/bỏ role | Giữ các guard tài khoản/role hệ thống phía server. |
| `/admin/block-accounts` | Chặn/mở chặn tài khoản | Không coi ẩn nút là bảo vệ Admin cuối; API vẫn enforce. |
| `/admin/accounts` | Cấp/cấp lại mã kích hoạt và xem liên kết tài khoản | Dùng component đã có, không tạo thêm nguồn danh tính. |
| `/work` | Dự án/task, nhận/giao việc, trạng thái, bình luận, hiệu suất | Trang dùng chung, giữ quyền API. |
| `/profile` | Xem hồ sơ, ảnh đại diện, tài khoản | Avatar hiện lưu localStorage; chưa đồng bộ ảnh giữa thiết bị. Không tự mở sửa hồ sơ HR. |

Các bản component cũ không còn được router dùng không được tính thành trang hoạt động. `/hr/payroll`, `/finance`, `/admin/payroll` và `/admin/employees` là redirect, không phải màn hình mới; các alias bảng lương kiểm quyền Kế toán. Đăng nhập/kích hoạt/thiếu quyền là các trang phiên được kiểm tra riêng theo [06](06-testing.md).

## 3. Chức năng thực tế còn thiếu — PROPOSED

| Ưu tiên | Đề xuất | Giới hạn / trạng thái |
| --- | --- | --- |
| P1 | Admin quản lý danh mục phòng ban, chức vụ, ca, loại phép, ngày lễ, ngân hàng | Các controller có CRUD nhưng chưa có trang tương ứng. Cần chốt role và quy tắc xóa/khóa danh mục đang được sử dụng; chưa tự thêm quyền/xóa dữ liệu. |
| P1 | Hoàn thiện thao tác bảng lương thủ công và sửa hợp đồng | API có create/update; cần xác nhận trường được sửa theo trạng thái/ai thao tác trước khi làm UI ghi. Không tự xây công thức thuế/lương. |
| P1 vận hành | Lưu bền báo cáo/bàn giao private và backup binary | Không phải chức năng cao siêu; gate dùng hồ sơ thật còn mở ở [18](18-release-checklist.md). Không chuyển file HR sang bucket ảnh public. |
| P2 | Home hiển thị đúng tên module thực; thông báo bổ sung OT/sửa công cần xử lý | Thẻ Kế toán đã tách khỏi Me theo quyết định 21/10; thông báo OT/sửa công và thẻ Kích hoạt vẫn là đề xuất chưa chọn. |
| P2 | Đồng bộ avatar giữa thiết bị | Avatar server cần storage/quyền upload phù hợp, chưa triển khai. Retry chart đã làm trong đợt UI 19/10. |

Không coi xem-only dữ liệu ngân hàng/bảo hiểm/hồ sơ cá nhân là lỗi thiếu CRUD của nhân viên. Quyền chỉnh dữ liệu nhạy cảm cần đúng vai trò nghiệp vụ.

## 4. Không đưa vào đợt này

Nhận diện khuôn mặt/AI, tính thuế và lương tự động đầy đủ, đa công ty, chữ ký số, geofence, app native, queue offline và realtime phức tạp. Đây là loại mở rộng chủ dự án cho phép bỏ qua; không xóa tính năng đã có để làm báo cáo đẹp hơn.

## 5. Bằng chứng và giới hạn

Đợt UI tiếp theo 19/10 theo quyền chủ dự án: thống nhất trình bày nền sáng/KPI/bảng/thẻ/biểu đồ và hướng dẫn cuối 20 trang HR/Admin; 28 vị trí bảng chuyển qua ResponsiveTable, giữ ma trận phân quyền cuộn riêng. Mobile có chi tiết mở rộng, bộ lọc nhân viên gọn và lịch xem theo ngày. Duyệt/từ chối nghỉ phép dùng hộp thoại trong trang và hiện lý do đã có trên API; không thêm policy hoặc quyền mới. Quy tắc bảo trì tại [09 §6](09-ui-design.md#6-responsive-tiếp-cận-và-nghiệm-thu). Chạy qua 35 trang với dữ liệu cô lập không thay nghiệm thu mọi tổ hợp trạng thái, quyền, form ghi, thiết bị thật hoặc hosting.

Đọc source/router/menu/API wrapper và controller tương ứng để lập ma trận. Regression tests về alias/quyền và đích thông báo nằm trong `marixa_react/tests`; browser smoke dùng API fixture SQLite riêng, không dùng Supabase hoặc bucket thật. Số lượng chạy thực tế và các ca browser được ghi tại [worklog](worklog.md), không suy nghiệm thu production từ build.
