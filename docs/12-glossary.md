# 12 — Thuật ngữ dùng chung Marixa

> Chủ tài liệu: **Đinh Văn Tài**. Cập nhật: **16/10/2026**.
> Quy ước trạng thái/cập nhật: [00-README.md](00-README.md). **EN:** A definition does not prove implementation. Read the status and source before assuming a workflow exists.

| Thuật ngữ | Ý nghĩa trong dự án | Trạng thái / lưu ý |
| --- | --- | --- |
| CURRENT / Hiện trạng | Hành vi tìm thấy trong mã nguồn | Chưa đồng nghĩa đã chạy/đạt nghiệm thu |
| TARGET / Mục tiêu | Kết quả nghiệp vụ hoặc thiết kế cần đạt | Sai khác với code được ghi thành việc cần làm |
| PENDING / Chưa triển khai | Chưa có luồng đầy đủ trong stack hiện hành | Backlog ở [01-plan.md](01-plan.md) |
| NOT VERIFIED / Chưa xác minh | Chưa có bằng chứng đủ cho kết luận | Áp dụng cho runtime, cloud, Figma và test chưa chạy |
| Ca làm chung / AttendanceRule | Quy tắc giờ làm chung; mục tiêu một rule active | Seed 08:00–17:00, nghỉ 60 phút; hiện có ghi đè khi restart và mốc trong code chưa đồng nhất. Không version/ngày hiệu lực |
| Shift / EmployeeShift | Entity ca kế hoạch và gán ca theo nhân viên | Có code, không suy ra V1 có hệ đa ca hoàn chỉnh |
| Ngày công / AttendanceDate | Ngày nghiệp vụ của bản ghi công | Mục tiêu ngày VN; check-in hiện lấy ngày UTC |
| Attendance | Bản ghi tổng hợp trạng thái/duyệt/giờ công theo NV/ngày | Một NV/ngày là invariant cần đảm bảo, chưa chứng minh unique constraint/concurrency an toàn |
| AttendanceLog | Lượt vào/ra, thời điểm, phương pháp, ảnh, ghi chú | Sửa công giữ log gốc; không có nghĩa mọi API CRUD bất biến |
| AttendanceCorrection | Yêu cầu sửa mốc chấm và kết quả duyệt | Có code; ngoại lệ tự duyệt xem [02-business-analysis.md](02-business-analysis.md) |
| ActualHours / Giờ thực | Giờ net calculator trả | Hiện làm tròn số giờ nguyên, không phải số phút chính xác |
| OvertimeHours / Giờ tăng ca | Phần giờ ra vượt mốc kết thúc theo calculator | Làm tròn 1 chữ số thập phân; chưa lấy giao với đơn OT duyệt |
| OvertimeRequest / Đơn OT | Yêu cầu ngày/khoảng giờ tăng ca và xét duyệt | Có service riêng; không chứng minh calculator sử dụng đơn |
| LeaveRequest / Đơn nghỉ | Loại nghỉ, thời gian và trạng thái | Chỉ Pending được hủy; chưa có luồng hủy Approved |
| LeaveLedger / Sổ phép | Giao dịch cấp/trừ/hoàn/điều chỉnh | Số dư theo loại giao dịch; chưa có job cộng phép tháng |
| Kỳ công / Snapshot | Kỳ tháng và bản tổng hợp cố định khi khóa | TARGET/PENDING; chưa có khóa/mở kỳ + snapshot trong API |
| Export hiện có | XLSX hồ sơ nhân sự, CSV công/một số danh sách | Khác XLSX bảng công snapshot/PDF đơn nghỉ chưa triển khai |
| Idempotent | Gọi lặp không tạo tác động nghiệp vụ ngoài lần đầu | Kiểm tra theo thao tác/concurrency; seeder không tạo trùng vẫn có thể ghi đè cấu hình |
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
