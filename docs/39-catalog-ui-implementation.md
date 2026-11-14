# 39 — Danh mục, hợp đồng và giao diện sáng/tối

> 10/11/2026. CURRENT trong source local; chưa push/deploy, chưa nghiệm thu provider/live. Quyết định tại [38](38-priorities-interview.md), evidence tại [worklog](worklog.md).

## 1. Cài đặt

- `/hr/catalogs`: phòng ban, chức vụ, loại phép, lịch nghỉ/làm bù, ngân hàng. Tìm tên/mã, lọc active/inactive/lưu trữ, phân trang UI 15 dòng, thêm/sửa/lưu trữ và lịch sử. Sidebar HR và Home có liên kết, route guard theo module HR.
- `/hr/contracts`, `/admin/contracts`: bảng hợp đồng dùng chung, giữ layout tương ứng. Thêm/sửa/lưu trữ, tìm số/tên nhân viên, xem bản lưu trữ và history. Server cấm đổi EmployeeId; sửa hồi tố được phép, chồng lấn chỉ cảnh báo UI, không tính lại công/lương.
- Admin/Manager/HR được create/update/soft-delete sáu entity trên; hard-delete bị chặn. Employee giữ self-read hợp đồng, Accountant không được ghi từ role kế toán. HR không được hard-delete Employee; không mở quản trị role/tài khoản cho Manager.
- Department/Position/Bank inactive không được gán mới qua Employee/EmployeeBankAccount. Sửa hồ sơ giữ nguyên tham chiếu cũ được phép; LeaveRequest giữ guard active có sẵn. Không cascade xóa hồ sơ khi ngừng danh mục.
- AuditLog lưu scalar before/after, actor, thời gian và lý do cùng SaveChanges. Persistence chặn sửa/xóa AuditLog và hard-delete sáu entity; không dựng lại history giả cho dữ liệu cũ.

## 2. Contract

Giữ endpoint/DTO hiện có; create/update/soft-delete cần `X-Change-Reason`: encodeURIComponent tiếng Việt, server giải mã/trim, 5–500 ký tự. Update/soft-delete cần `X-Record-Version` là LastUpdatedTime của lần đọc nguồn. Thiếu version trả 428 RECORD_VERSION_REQUIRED, cũ trả 409 CONCURRENT_CHANGE, thiếu lý do trả 400 CHANGE_REASON_REQUIRED, đổi chủ hợp đồng trả 400 IMMUTABLE_EMPLOYEE. Conflict yêu cầu tải lại; không tự gửi lại mutation.

Sáu entity dùng LastUpdatedTime làm EF concurrency token, timestamp tăng cả khi clock cố định. Mutation dùng transaction Serializable; request version cũ không ghi thêm revision. Không phải durable idempotency/offline protocol. Không thay cột/bảng; model snapshot cập nhật metadata concurrency, migration lịch sử giữ nguyên. Transaction/precision trên SQL Server/PostgreSQL cần kiểm fixture provider trước phát hành.

Thêm GET `/api/{Department|Position|Bank|LeaveType|HolidayCalendar|EmployeeContract}/get-archived`: list chỉ Admin/Manager/HR, hiện chưa phân trang server (quy mô dự án dưới 15 người), UI phân trang local. History qua `/api/AuditLog/query?entityType=...&entityId=...`, có phân trang. [Inventory](api-inventory.md) được sinh lại từ TestServer.

Form hợp đồng liên quan trong hồ sơ nhân viên tiếp tục dùng wrapper có version nguồn; lý do mặc định ghi thao tác từ hồ sơ nhân sự. Trang hợp đồng chuyên trách yêu cầu nhập lý do. Không fetch version mới để bỏ qua conflict.

## 3. UI và tham khảo

Chủ dự án chọn cả sáng/tối, nút mặt trời/mặt trăng trên header, nhớ localStorage và đồng bộ storage event; lần đầu theo hệ điều hành. Header/footer xanh đậm tách nội dung; token bề mặt/chữ cho hai theme. Dialog có Escape/focus return, lỗi và đang lưu; bảng ResponsiveTable, CSS responsive/reduced-motion.

Tham khảo công khai 10/11: [Personio](https://www.personio.com/) về nhóm workspace/Organization/Documents; [BambooHR](https://www.bamboohr.com/) về hướng màu xanh và nhóm dữ liệu nhân sự. Đây là lựa chọn thiết kế Marixa, không sao chép mã/brand hoặc chứng nhận sản phẩm tham khảo. Home không thêm KPI giả.

Đã quan sát desktop Home, danh mục và modal tối bằng fixture UI local có dữ liệu giả, không kết nối DB/API thật; fixture tạm đã gỡ. Chưa browser E2E form→API thật, viewport mobile hoặc mọi trang legacy ở hai theme. Các trang cũ cần audit màu hardcode; global token không chứng minh toàn bộ UI đã hoàn thiện.

## 4. Còn mở

Bổ sung theo yêu cầu tiếp 14/11: Home thêm hướng dẫn ba việc hằng ngày, FAQ mở/đóng và khối CTA Công việc để tạo khoảng cuộn có nội dung trước footer. Footer Home ba cột (giới thiệu, module theo quyền, hướng dẫn/tài khoản) cùng dòng năm/UTC+7; liên kết tới route thật và anchor FAQ, không tạo mục hỗ trợ/chính sách giả. Tham khảo ảnh NoteTatCa/PublicPages.tsx và cách chia nhóm nội dung tại [BambooHR](https://www.bamboohr.com/). Footer shell trong các module nghiệp vụ không bị kéo dài theo Home.

Cập nhật UI 14/11/2026 theo ảnh chủ dự án: login chuyển sang token canvas/surface/ink/input riêng và màu autofill; footer Home nằm ngoài main để phủ ngang/sát đáy; avatar fallback nền trong suốt và nút padding 0; hoàn thiện text-2/text-3 và màu trạng thái dark, bỏ gradient sáng trên metric, sửa nhãn/bảng/bộ lọc Nghỉ phép và nút phụ Home. Hình tròn Home cùng phần tử con tắt animation/transition hoàn toàn, không phụ thuộc reduced-motion. Tham khảo source token sáng/tối NoteTatCa local, không sửa repo tham khảo.

QA local với adapter giả: login sáng/tối, Home/footer/avatar/hình tĩnh, Nghỉ phép dark. Các cặp màu được đo gồm login sáng 14.65:1, login tối 14.47:1, chữ phụ tối 9.13:1, tiêu đề bảng 8.86:1, nút phụ Home 9.96:1; chỉ là các cặp đã kiểm, không phải chứng nhận toàn bộ app. Không đăng nhập hoặc ghi API/live; mobile và browser autofill thực tế vẫn chưa nghiệm thu.

- Bản lưu trữ xem được/history; chưa có restore UI hoặc tự active lại. Chốt restore/re-enable trước khi bổ sung.
- Grant động, inbox/job console, backup D:\backup và offline giữ phụ thuộc 38; chưa thực thi.
- Provider concurrency/race tests, mobile và browser/API E2E; private storage/backup vẫn là gate release.
- Bundle lớn vẫn có cảnh báo; không tăng ngưỡng để che lỗi. Code mới đang local.
