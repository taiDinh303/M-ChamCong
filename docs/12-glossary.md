# 12 — Thuật ngữ dùng chung Marixa

> Chủ tài liệu: **Đinh Văn Tài**. Cập nhật: **17/10/2026**.
> Quy ước trạng thái/cập nhật: [00-README.md](00-README.md). **EN:** A definition does not prove implementation. Read the status and source before assuming a workflow exists.

| Thuật ngữ | Ý nghĩa trong dự án | Trạng thái / lưu ý |
| --- | --- | --- |
| CURRENT / Hiện trạng | Hành vi tìm thấy trong mã nguồn | Chưa đồng nghĩa đã chạy/đạt nghiệm thu |
| TARGET / Mục tiêu | Kết quả nghiệp vụ hoặc thiết kế cần đạt | Sai khác với code được ghi thành việc cần làm |
| PENDING / Chưa triển khai | Chưa có luồng đầy đủ trong stack hiện hành | Backlog ở [01-plan.md](01-plan.md) |
| NOT VERIFIED / Chưa xác minh | Chưa có bằng chứng đủ cho kết luận | Áp dụng cho runtime, cloud, Figma và test chưa chạy |
| Ca làm chung / AttendanceRule | Một rule active, không version/ngày hiệu lực | Default 08:00–17:00/nghỉ 60 phút; seeder giữ cấu hình; unique filtered active + replacement transaction trong model/service |
| Shift / EmployeeShift | Entity ca kế hoạch và gán ca theo nhân viên | Có code, không suy ra V1 có hệ đa ca hoàn chỉnh |
| Ngày công / AttendanceDate | Nhãn ngày VN, không phải thời điểm | BusinessTime UTC+7, DateTime UTC-kind để tương thích PG; chưa backfill dữ liệu cũ |
| Attendance | Tổng hợp NV/ngày | Unique EmployeeId+date khi chưa xóa; DB live cần áp constraint sau preflight |
| AttendanceLog | Lượt vào/ra, thời điểm, phương pháp, ảnh, ghi chú | Sửa công giữ log gốc; không có nghĩa mọi API CRUD bất biến |
| AttendanceCorrection | Yêu cầu sửa mốc chấm và kết quả duyệt | Có code; ngoại lệ tự duyệt xem [02-business-analysis.md](02-business-analysis.md) |
| ActualHours / Giờ công thường | Trong khung ca sau trừ nghỉ và tránh đếm đôi OT | Decimal 2 số, tính theo phút; quyết định 17/10 |
| OvertimeHours / Giờ OT công nhận | Giao actual×window Approved, union khoảng chồng, trừ nghỉ | Tách công thường, decimal 2 số; không tự cộng giờ ra muộn |
| OvertimeRequest / Đơn OT | Đề xuất window và xét duyệt | Chỉ Approved window hợp lệ dùng vào calculator/snapshot |
| LeaveRequest / Đơn nghỉ | Loại/ngày/trạng thái | Approved hủy bởi HR/Admin khác chủ có lý do, giữ audit/hoàn đúng Deduct |
| LeaveLedger / Sổ phép | Giao dịch cấp/trừ/hoàn/điều chỉnh | Số dư theo loại giao dịch; chưa có job cộng phép tháng |
| Kỳ công / Snapshot | Open→Reviewed→Locked; version bất biến khi khóa | CURRENT backend, UI/live chưa nghiệm thu |
| Export hiện có | XLSX hồ sơ/CSV hiện thời ở UI; XLSX snapshot và PDF nghỉ ở backend | Quyền/version khác nhau, xem 15 |
| Idempotent | Gọi lặp không tạo hiệu ứng ngoài lần đầu | Test từng thao tác/concurrency; không suy từ check-then-insert hoặc một ca HTTP tuần tự |
| Soft delete | Đánh dấu DeletedTime thay xóa vật lý | Không đồng nghĩa mọi truy vấn tự lọc bản ghi xóa |
| Identity / JWT | ASP.NET Identity quản lý tài khoản/role; JWT xác thực request | Nguồn danh tính ứng dụng, không phải Supabase Auth |
| Role core / nghiệp vụ | Core: Admin/Manager/HR/Employee; nghiệp vụ: Accountant/Sale/Marketing | 7 role seed; 3 role mới chưa có quyền module riêng |
| Module / Route guard | Registry điều hướng/quyền hiển thị frontend | Không thay backend authorization/quyền sở hữu |
| GPS / Geofence | Vị trí thiết bị / so với tọa độ và bán kính văn phòng | UI hiển thị vị trí; luồng chấm chưa gửi/lưu hoặc đánh cờ bán kính |
| Bucket public marixa-photos | Nơi lưu ảnh có URL public khi Storage cấu hình hợp lệ | Giữ public; thiếu cấu hình dùng đĩa. Config đúng Supabase:ServiceKey ↔ SUPABASE__SERVICEKEY |
| Session pooler | Endpoint PostgreSQL của Supabase | Lấy host/user/port từ Connect của project; không suy ra host từ region |
| DB provider | Lựa chọn SqlServer/PostgreSQL qua DatabaseConfig | Mỗi provider có cơ chế schema khác nhau |
| Schema sync | Đối chiếu EF/PostgreSQL, thêm cột thiếu | Không đổi kiểu/xóa cột/thay migration, backfill hay rollback |
| Offline queue | Lưu thao tác bền khi mất mạng rồi đồng bộ | PENDING; thông báo retry không chứng minh đã lưu hàng đợi |
| Blueprint / SPA rewrite | Render đọc service config; Vercel trả index.html cho route SPA | Config trong repo không chứng minh cloud đã áp dụng |
| SSoT / Snapshot thiết kế | Nguồn chuẩn / bản sao để đối chiếu | theme.css là token code; tokens.json là mirror |
| Scaffold | Frame/component/cấu trúc ban đầu | Không phải UI hoàn chỉnh hay bằng chứng plugin/Figma sync chạy thành công |

Chi tiết: [03-architecture.md](03-architecture.md), [04-database-design.md](04-database-design.md), [05-workflow.md](05-workflow.md), [06-testing.md](06-testing.md), [08-deploy-detail.md](08-deploy-detail.md), [13-figma-sync.md](13-figma-sync.md). Bằng chứng thực hiện ở [worklog.md](worklog.md).
