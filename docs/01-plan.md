# 01 — Quyết định và kế hoạch Marixa

> Chủ dự án Đinh Văn Tài. Cập nhật 2026-10-20; quyết định 12/10 và phỏng vấn 17/10 được giữ. Nâng schema live đã có bằng chứng riêng ở worklog; production chưa nghiệm thu toàn bộ.

## 1. Quyết định đang có hiệu lực

| Chủ đề | Quyết định / nguồn |
| --- | --- |
| Stack/danh tính | ASP.NET Core 8 + Identity JWT + React/Vite; SQL Server local/PostgreSQL cloud. Không dùng Supabase Auth/khôi phục Next.js. Quyết định 12/10. |
| Role | Admin/Manager/HR/Employee core; Accountant đọc bảng lương toàn công ty theo quyết định 21/10; Sale/Marketing chưa có quyền module riêng. |
| Ảnh/GPS | Bằng chứng tùy chọn theo rule, ngoài văn phòng vẫn ghi nhận. Ảnh chấm công public marixa-photos. UI đọc yêu cầu ảnh/GPS theo rule; quyết định 12/10, hoàn thiện UI 17/10. |
| Ca chung | Một AttendanceRule active, không version/ngày hiệu lực cho rule. Snapshot lưu bản rule đã dùng khi khóa, không thêm hệ version ca. |
| Giờ công | Công thường trong khung ca trừ phần nghỉ thực sự giao với thời gian làm; tách OT; tính theo phút, trả decimal 2 chữ số. Chủ dự án đồng ý 17/10. |
| Nghỉ giữa ca | Mặc định 12:00–13:00; BreakStartTime/BreakMinutes cấu hình theo ca. Chủ dự án xác nhận 17/10. |
| OT | Chỉ phần giao thời gian làm thực tế và cửa sổ đơn Approved; union khoảng chồng để không cộng đôi. OT nhập tay không là nguồn công nhận. Xác nhận 17/10. |
| Tự duyệt | HR/Admin tự duyệt công/OT/sửa công; Manager không tự duyệt. Đơn nghỉ luôn cần người khác. Xác nhận 17/10. |
| Hủy nghỉ Approved | HR/Admin khác chủ hủy có lý do 5–500 ký tự, giữ phê duyệt cũ/ghi metadata hủy/hoàn đúng Deduct một lần. Chủ dự án giao Codex quyết định 17/10. |
| Cấp phép | Giữ MaxDays/ledger hiện hành, HR quản lý Grant/Adjust; chưa bật cấp phép tự động khi thiếu mức/lịch/đối tượng/carryover. Lựa chọn trong quyền được giao 17/10. |
| Ngày hiệu lực HR | Promotion từ ngày hiệu lực VN; nghỉ việc sau ngày làm cuối; worker mỗi phút khi app chạy và catch-up, AppliedAt chống áp lặp. Chủ dự án giao Codex quyết định 17/10. |
| Kỳ công | HR/Admin review; Admin lock/reopen có lý do, snapshot version bất biến. Open→Reviewed→Locked; thay nguồn/rule ở kỳ Reviewed phải review lại. |
| Múi giờ | Asia/Ho_Chi_Minh UTC+7; timestamp UTC, nhãn ngày VN. Không tự dịch/backfill toàn bộ ngày cũ. |
| Bố trí UI 17/10 | Chủ dự án đồng ý: kỳ/XLSX trong Thống kê; tab OT và sửa công trong Lịch sử; Admin sửa rule bằng hộp thoại; HR/Admin cấp/điều chỉnh phép thủ công trong Nghỉ phép. Giữ layout chính, chưa cấp phép tự động. |
| Điều hướng và dữ liệu test 18/10 | Bỏ ngày Home; tiêu đề ở header desktop/dưới hàng logo mobile, menu module trên điện thoại mở toàn màn hình từ nút cạnh logo, không dùng sidebar ngang. Quyết định 19/10 bên dưới thay phần đưa tiêu đề lên header: bỏ app-page-heading. Home bỏ thanh phân mục và nút mục lục theo yêu cầu tiếp, dùng các thẻ module làm điều hướng chính. Thêm DEMO vào Supabase hiện tại theo xác nhận trực tiếp, giữ dữ liệu cũ; tài khoản DEMO chỉ Employee, không fake token/claim/migration history. |
| Hạ tầng/Figma | Snapshot chỉ tại 00, một Supabase project; staging riêng chưa xác nhận. Figma Dev Mode copy, UI Kit trước, frames 375/1440. |

Không suy OT ngày lễ/hệ số lương, ca biên, phép qua năm hoặc retention từ tên entity. Các chính sách đó cần tiếp tục chốt trước mở rộng; rủi ro 34 ghi phạm vi chưa hoàn tất.

## 2. Việc tiếp theo

Quyết định bổ sung 21/10/2026 sau khi tham khảo kiểm thử MARIXA: ưu tiên ca/chấm công; gán từ 1 người, tối đa 31 ngày/lượt, tùy chọn bỏ Chủ nhật. Admin/HR tạo và ngừng nhận lịch mới cho ca, giữ lịch sử. Module Kế toán riêng cho Admin/Accountant chỉ xem bảng lương, tìm/lọc kỳ/trạng thái/chi tiết; HR giữ nhập lương cơ bản, nhân viên xem mức lương cá nhân. Chưa làm tự tính/duyệt/chi lương. Phạm vi, API và tiêu chí tại [36](36-workflow-improvements.md); bằng chứng tại worklog.

Quyết định UI 19/10/2026: chuyển prefix `/attendance` → `/me`, `/employees` → `/hr` cùng các trang con, giữ alias cho liên kết cũ; bỏ app-page-heading toàn ứng dụng, giữ KPI/thao tác. Chủ dự án đồng ý bổ sung menu Admin Nghỉ phép/Bảng lương/Nhân sự nghỉ việc (trang dùng chung với HR giữ cùng phong cách) và liên kết mở trang xử lý trong thông báo. Rà soát chức năng cơ bản tại [35](35-frontend-review.md); đề xuất chưa được chọn không tự triển khai, bỏ qua mở rộng quá phức tạp.

UI 19/10 được chủ dự án giao tiếp: áp dụng thẻ mở rộng trên điện thoại, giữ bảng desktop/ma trận đối chiếu cuộn riêng; thống nhất phong cách Home/HR/Me/Admin, thêm hướng dẫn cuối HR/Admin và tham khảo cách trình bày MISA AMIS phù hợp. Agent quyết định các lựa chọn UX thường lệ, giữ nghiệp vụ/quyền/API và không ép người dùng xoay màn hình. Hiện trạng và giới hạn ở [09 §6](09-ui-design.md#6-responsive-tiếp-cận-và-nghiệm-thu), bằng chứng tại worklog; quyền sửa UI không tự cấp quyền rollout.

### 2.1 Đã có backend và kiểm thử trong nhiệm vụ 17/10

- Tách HR controller qua service, actor/clock/storage/export abstractions, guards tài khoản Admin và JWT/lockout; private report/handover, log gốc bất biến.
- Khóa/mở/review kỳ, snapshot/version/hash, XLSX theo quyền và PDF tiếng Việt; chống ghi tháng khóa và mất review khi nguồn đổi.
- Công/OT/nghỉ theo phỏng vấn, hủy/hoàn phép một lần, unique ledger; promotion/handover ngày hiệu lực và guard admin cuối.
- Rate limit/correlation/readiness, migration/scripts đúng provider, CI và docs chọn lọc. Số ca/lệnh/kết quả chính thức ở worklog.

### 2.2 Còn mở trước dùng thật hoặc mở rộng

| Ưu tiên | Việc / điều kiện đóng |
| --- | --- |
| P0 | Hoàn tất env/secret/CORS/volume, backup/restore binary và bản ngoài máy, smoke nghiệp vụ có ghi trên môi trường được phép. Schema PostgreSQL đã nâng 18/10, dữ liệu public đã restore thử; phạm vi tại worklog và 22/23. |
| P1 | Nghiệm thu các luồng frontend mới trên hosting và thiết bị thật sau nâng schema; kiểm camera/GPS, responsive và các role còn lại. Code UI/bằng chứng cô lập xem worklog. |
| P1 | Phạm vi Manager toàn bộ module; chính sách biên/ca qua đêm, ngày nghỉ/lễ và phép qua năm; đối soát dữ liệu cũ. |
| P1 | RPO/RTO/on-call/retention/cấp phép định kỳ và cảnh báo job; chỉ triển khai sau quyết định cần thiết. |
| P1 | Offline queue/idempotency/late adjustment kỳ sau; hiện API trả 409 khi ghi kỳ khóa. |
| P2 | Giảm service lớn/response object/IQueryable, bổ sung CancellationToken theo rủi ro; quyền role chuyên biệt và Figma coverage. |

## 3. Cách đóng một hạng mục

Gắn REQ/API/TC và nguồn quyết định, sửa đúng layer cùng tài liệu, chạy kiểm tra phù hợp và ghi bằng chứng/phạm vi chưa test. Checkbox hoặc điểm không thay chứng nhận production. Quy tắc chung tại [00](00-README.md), rủi ro tại [34](34-risks-changes.md), phát hành tại [18](18-release-checklist.md).
