# 01 — Quyết định và kế hoạch Marixa

> Chủ dự án Đinh Văn Tài. Cập nhật 2026-10-17; quyết định 12/10 được giữ, bổ sung kết quả phỏng vấn trực tiếp 17/10. Kiểm chứng ở worklog, production chưa nghiệm thu.

## 1. Quyết định đang có hiệu lực

| Chủ đề | Quyết định / nguồn |
| --- | --- |
| Stack/danh tính | ASP.NET Core 8 + Identity JWT + React/Vite; SQL Server local/PostgreSQL cloud. Không dùng Supabase Auth/khôi phục Next.js. Quyết định 12/10. |
| Role | Admin/Manager/HR/Employee core; Accountant/Sale/Marketing đã seed, chưa có quyền quản lý chuyên biệt. |
| Ảnh/GPS | Bằng chứng tùy chọn theo rule, ngoài văn phòng vẫn ghi nhận. Ảnh chấm công public marixa-photos. UI còn sai khác; quyết định 12/10. |
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
| Hạ tầng/Figma | Snapshot chỉ tại 00, một Supabase project; staging riêng chưa xác nhận. Figma Dev Mode copy, UI Kit trước, frames 375/1440. |

Không suy OT ngày lễ/hệ số lương, ca biên, phép qua năm hoặc retention từ tên entity. Các chính sách đó cần tiếp tục chốt trước mở rộng; rủi ro 34 ghi phạm vi chưa hoàn tất.

## 2. Việc tiếp theo

### 2.1 Đã có backend và kiểm thử trong nhiệm vụ 17/10

- Tách HR controller qua service, actor/clock/storage/export abstractions, guards tài khoản Admin và JWT/lockout; private report/handover, log gốc bất biến.
- Khóa/mở/review kỳ, snapshot/version/hash, XLSX theo quyền và PDF tiếng Việt; chống ghi tháng khóa và mất review khi nguồn đổi.
- Công/OT/nghỉ theo phỏng vấn, hủy/hoàn phép một lần, unique ledger; promotion/handover ngày hiệu lực và guard admin cuối.
- Rate limit/correlation/readiness, migration/scripts đúng provider, CI và docs chọn lọc. Số ca/lệnh/kết quả chính thức ở worklog.

### 2.2 Còn mở trước dùng thật hoặc mở rộng

| Ưu tiên | Việc / điều kiện đóng |
| --- | --- |
| P0 | Preflight/migration/backup-restore, grants/RLS bảng cũ, env/secret/CORS/volume live và smoke trên môi trường được phép. |
| P1 | UI ảnh tùy chọn/GPS, form BreakStartTime/số lẻ, period/export/PDF và trạng thái Approved chờ AppliedAt; browser/mobile E 2 E. |
| P1 | Phạm vi Manager toàn bộ module; chính sách biên/ca qua đêm, ngày nghỉ/lễ và phép qua năm; đối soát dữ liệu cũ. |
| P1 | RPO/RTO/on-call/retention/cấp phép định kỳ và cảnh báo job; chỉ triển khai sau quyết định cần thiết. |
| P1 | Offline queue/idempotency/late adjustment kỳ sau; hiện API trả 409 khi ghi kỳ khóa. |
| P2 | Giảm service lớn/response object/IQueryable, bổ sung CancellationToken theo rủi ro; quyền role chuyên biệt và Figma coverage. |

## 3. Cách đóng một hạng mục

Gắn REQ/API/TC và nguồn quyết định, sửa đúng layer cùng tài liệu, chạy kiểm tra phù hợp và ghi bằng chứng/phạm vi chưa test. Checkbox hoặc điểm không thay chứng nhận production. Quy tắc chung tại [00](00-README.md), rủi ro tại [34](34-risks-changes.md), phát hành tại [18](18-release-checklist.md).
