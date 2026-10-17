# 30 — Hướng dẫn sử dụng và đối soát

> Cập nhật 2026-10-17. Phân biệt UI đã có và API backend mới; chưa nghiệm thu browser/mobile/live.

## 1. Nhân viên

Đăng nhập hoặc kích hoạt bằng mã được cấp, đặt password mạnh; RememberMe kéo dài thời hạn theo cấu hình. Vào màn hình chấm công chọn vào/ra và kiểm kết quả/lịch sử. API cho ảnh/GPS tùy chọn theo rule, nhưng UI AttendanceCard vẫn bắt ảnh và chưa gửi tọa độ; đây là việc UI còn mở. Không gửi lại liên tục khi 429 hoặc 409.

Tạo đơn nghỉ với loại/ngày/số ngày đúng, theo dõi Pending/Approved/Rejected/Cancelled; không tự duyệt nghỉ. Pending có thể sửa/hủy theo quyền. Approved muốn hủy cần HR/Admin khác chủ ghi lý do; hệ thống hoàn số đã trừ một lần. Sổ phép thủ công do staff quản lý, chưa cấp phép tự động.

OT nhập ngày, StartAt/EndAt và lý do; chỉ phần đã duyệt và có làm thực tế mới được công nhận. Làm 08–18, nghỉ 12–13, đơnOT 17–19 đã duyệt:8 giờ thường+1 giờOT. Đơn chưa duyệt/ra muộn tự phát không cộng OT. Sửa công dùng yêu cầu và review, không chỉnh/xóa log gốc.

## 2. HR và Admin

Kiểm nhân viên/trạng thái công, giải quyết các đơn Pending rồi HR/Admin review kỳ. Admin khóa có lý do, tạo snapshot/version; nhân viên xem/xuất chỉ dữ liệu của mình, staff toàn kỳ. Khi sai: Admin reopen có lý do, sửa dữ liệu, review lại, lock version mới; version cũ giữ nguyên. API tại 15; UI khóa kỳ/export/PDF mới cần nối thêm, không giả định nút đã tồn tại.

Promotion/handover Approved có thể chưa AppliedAt. Promotion áp từ ngày hiệu lực, nghỉ sau ngày làm cuối; job xử lý khi app hoạt động và catch-up sau restart. Khi lỗi áp, xem 21; không đổi ngày hiệu lực hoặc AppliedAt để che lỗi. Không tự chuyển tài liệu private sang link public.

## 3. Khi cần hỗ trợ

Gửi thời điểm, màn hình/thao tác, mã lỗi và X-Correlation-ID; không gửi password/JWT/file nhân sự thật vào kênh công khai.403 là quyền,409 cần đọc state/history,429 đợi Retry-After. PDF là bản xuất dữ liệu tại thời điểm ghi, không chữ ký phê duyệt pháp lý; dữ liệu được xác nhận từ quy trình trong hệ thống.
