5 — Chiến lược kiểm thử hệ thống chấm công Marixa

> Tác giả & chủ tài liệu: **Đinh Văn Tài**.
> Kiểm thử cho hệ thống **ASP.NET 8 + React 19 + DB linh hoạt** đang chạy. Ưu tiên: không mất/nhân đôi công, đúng quyền, đúng số liệu, đúng kỳ.

## 0. Cách đọc (dành cho AI)

- Ma trận kịch bản §2 là "đơn hàng" test bắt buộc; chạy lại khi đổi ca làm/lịch/ảnh.
- Không dùng Docker để chạy test: chạy .NET test runner bằng `dotnet test`, chạy frontend bằng Node/`npm test` (nếu có); nối CSDL thật (SQL Server local hoặc Supabase test).

## 1. Mục tiêu và mức kiểm thử

| Mức | Phạm vi | Công cụ |
| --- | --- | --- |
| Unit | Hàm tính công/tăng ca (`AttendanceTimeCalculator`), ledger phép, quy tắc ca | `dotnet test` / xUnit. |
| Integration | Controller + service + DB (SQL Server/Postgres), upload ảnh, JWT | TestHost + CSDL test. |
| E2E | Luồng NV → HR → Admin trên web (login, chấm, duyệt, khóa kỳ) | Browser (mobile 375px, desktop 1440px). |

## 2. Ma trận kịch bản bắt buộc

Các mốc giờ dưới dùng **ca làm chung active** (seed 08:00–17:00), không phải giờ cố định trong code.

| Nhóm | Kịch bản | Kết quả mong đợi |
| --- | --- | --- |
| Chấm công | Vào 08:00, ra 17:00 thứ 2–7 | Giờ thực tế ≈ 8h (trừ nghỉ ca), đủ 2 log. |
| Trùng log | Chấm lại cùng loại trong cửa sổ ~2 phút | Không tạo log mới, trả "đã có lần chấm gần đây". |
| Đi trễ | Vào 08:30, ca bắt đầu 08:00 | Status = `Late`. |
| Tăng ca | Ra 18:00, ca kết thúc 17:00 | Overtime tính phần vượt mốc (theo rule). |
| Thiếu mốc | Chỉ có lượt vào | Ngày công chưa hoàn chỉnh, không tự sinh lượt ra. |
| Ảnh | Chụp camera + upload | Ảnh lưu Supabase (prod) hoặc đĩa (dev), gắn vào Attendance. |
| GPS | Trong/ngoài bán kính `OfficeLocation` | Gắn cờ vị trí, không chặn chấm. |
| Sửa công | NV đề nghị, HR duyệt | Tạo `AttendanceCorrection`, giữ log gốc, audit trước/sau. |
| Nghỉ phép | Duyệt đơn nghỉ phép năm | Trừ `LeaveLedger` đúng 1 lần; từ chối không trừ; hủy sinh hoàn. |
| Tăng ca (đơn) | Duyệt đơn OT | Chỉ cộng phần giao giữa OT duyệt và giờ chấm thực tế. |
| Quyền | NV gọi API dữ liệu người khác | Bị từ chối 403 (Backend `[Authorize(Roles)]` + logic service). |
| Tài khoản | Admin reset/khóa; NV kích hoạt mã | Kích hoạt đặt mật khẩu; chặn khóa admin cuối. |
| Kỳ công | HR kiểm tra, Admin khóa rồi có sửa công mới | Snapshot cũ không đổi; chỉ cập nhật khi mở lại có lý do. |
| Export | Excel/PDF theo kỳ & quyền | Đúng số liệu, đúng phiên bản, chặn trái quyền. |

## 3. Kiểm thử quyền & dữ liệu

- Test mọi endpoint với 4 role (`Admin`, `Manager`, `HR`, `Employee`) + chưa đăng nhập (401).
- Kiểm tra không khóa được admin hoạt động cuối; không cấp/tráo role trái quy tắc.
- Ảnh: bucket public `marixa-photos` — xác nhận đường dẫn không chứa tên NV, kiểm tra giới hạn 5 MB & MIME.
- DB: chạy trên cả SQL Server (dev) và PostgreSQL (prod) để đảm bảo CSDL linh hoạt không lệch schema (post-boot DDL + `PostgresSchemaSync`).

## 4. Kiểm thử giao diện & thiết bị

- Mobile 375px / 768px: nút chấm rõ, camera preview/chụp lại, không cuộn ngang.
- Desktop 1024px / 1440px: bảng HR lọc/sort/phân trang, refresh/Back/Forward giữ route + filter.
- Bàn phím + screen reader cơ bản; trạng thái luôn có chữ (không chỉ màu/icon).
- Thử camera bị chặn, mất mạng giữa upload, 2 thiết bị cùng chấm 1 ngày.

## 5. Cổng chấp nhận trước dùng thật

Không đưa thật nếu còn: mất/nhân đôi event, sai công/phép/tăng ca, vượt quyền, khóa kỳ không ổn định, lộ secret, restore thất bại. Đạt ma trận §2–§4, thử backup/restore ít nhất 1 lần, admin đã nhập lịch/địa điểm/chính sách trước vận hành. Hạn mức & điều kiện gói (Render/Vercel/Supabase Free) ghi rõ trong [07-deploy-overview.md](07-deploy-overview.md).
