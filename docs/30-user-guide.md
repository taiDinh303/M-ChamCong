# 30 — Hướng dẫn sử dụng theo vai trò

> Cập nhật 2026-10-17. CURRENT theo source; kiểm thử cô lập ghi tại [worklog](worklog.md). Chưa nghiệm thu production, camera/GPS trên thiết bị thật hoặc Figma.

## 1. Nhân viên

Đăng nhập hoặc kích hoạt bằng mã được cấp. Không chọn Ghi nhớ đăng nhập thì phiên nằm trong tab; chọn thì lưu phiên qua lần mở trình duyệt theo hạn JWT, tối đa 7 ngày. Đăng xuất xóa cả hai nơi lưu. Không gửi mật khẩu/JWT cho người khác.

Tại Chấm công, chọn Vào ca/Ra ca. Ảnh/GPS chỉ bắt buộc khi quy định đang áp dụng yêu cầu; ảnh là tùy chọn thì có thể chấm trực tiếp hoặc mở camera. Nút Bật định vị chỉ đọc vị trí khi bạn chọn; khi cần GPS phải có tọa độ hợp lệ mới gửi. Chưa có đánh giá trong/ngoài văn phòng bằng bán kính. Kiểm tra kết quả và lịch sử, không bấm lặp khi đang gửi hoặc máy chủ báo 429/409.

Lịch sử công gồm ba tab: Chấm công, Tăng ca, Yêu cầu sửa công. Nút Yêu cầu sửa công trên bản ghi điền sẵn ngày/giờ; cũng có thể tạo yêu cầu cho ngày thiếu bản ghi. Đề nghị chỉ áp dụng sau khi được duyệt, không sửa log chấm gốc. Đăng ký OT bằng thời điểm bắt đầu/kết thúc theo giờ VN; đơn Pending của mình có thể chỉnh sửa. Chỉ phần giao giữa thời gian làm thực tế và đơn OT Approved được công nhận, tách khỏi công thường. Ví dụ làm 08–18, nghỉ 12–13, đơn OT 17–19 Approved: 8 giờ thường và 1 giờ OT.

Nghỉ phép: tạo loại/ngày/cả ngày hoặc nửa ngày/lý do; theo dõi Pending/Approved/Rejected/Cancelled. Pending của mình có thể hủy; không tự duyệt nghỉ. Approved cần HR/Admin khác chủ hủy có lý do, hoàn đúng số đã trừ một lần. Xem sổ phép để đối chiếu số dư và lịch sử; nút PDF ở chi tiết đơn tải bản dữ liệu tại thời điểm xuất.

Thống kê công hiển thị công thường/OT riêng và số dư từ sổ phép. Phần Kỳ công cho chọn phiên bản và tải XLSX khi đã có bản chốt; nhân viên chỉ nhận dữ liệu của mình. Kỳ mở lại vẫn giữ các bản chốt cũ; bản xuất không phải số liệu live đang sửa.

## 2. HR, Manager và Admin

Trong Lịch sử công của nhân sự, các tab Tăng ca và Duyệt sửa công hiển thị hàng chờ theo quyền API. HR/Admin được tự duyệt công/OT/sửa công; Manager không tự duyệt. Đơn nghỉ luôn cần người khác xử lý. Màn hình Admin Nghỉ phép dùng riêng endpoint duyệt/từ chối, không dùng cập nhật đơn để đổi trạng thái.

HR/Admin có nút Sổ phép · Cấp / điều chỉnh trong trang Nghỉ phép: chọn nhân viên/năm, xem lịch sử, ghi giao dịch kèm lý do 5–500 ký tự. Cấp phép dùng số dương; điều chỉnh dùng số dương hoặc âm khác 0. Trừ/hoàn phép của đơn do quy trình duyệt/hủy xử lý. Chưa tự cấp phép hằng tháng.

Tại Thống kê công, giải quyết công/đơn Pending, HR/Admin Đánh dấu đã kiểm tra. Admin Khóa kỳ có lý do để tạo snapshot/version bất biến; XLSX của staff chứa toàn kỳ. Khi cần sửa: Admin Mở lại kỳ có lý do, sửa nguồn, kiểm tra lại và khóa phiên bản mới; giữ bản cũ. API kiểm tiền điều kiện và có thể trả 409, không tự bỏ qua để chốt.

Admin có nút Chỉnh quy định trong trang Quy định: giờ vào/ra, giờ nghỉ/phút nghỉ, số giờ chuẩn, dung sai, ảnh/GPS. Một rule active, không thêm trang hoặc version ca. Thay rule có thể làm mất trạng thái đã kiểm tra của kỳ chưa khóa; snapshot cũ giữ số liệu đã chốt.

Promotion/handover Approved có thể chưa AppliedAt. Promotion áp từ ngày hiệu lực VN, nghỉ việc sau ngày làm cuối; job xử lý khi app chạy/catch-up. Không chỉnh AppliedAt để che lỗi, không chuyển file nhân sự private thành link public.

## 3. Khi cần hỗ trợ

Gửi thời điểm, màn hình/thao tác, mã lỗi và X-Correlation-ID; không gửi credential hoặc hồ sơ thật vào kênh công khai. 403 là quyền; 409 cần đọc trạng thái/lịch sử; 429 đợi Retry-After. API gốc `/` trả 404 là fallback bình thường; Swagger ở `/swagger`, giao diện React có hosting riêng. PDF là bản xuất dữ liệu, không phải chữ ký số.

Lỗi 500 thiếu bảng/cột do schema phải xử lý theo [21](21-operations-runbook.md) và [23](23-data-migration.md). Trong đợt này chủ dự án tự chạy SQL live; kết quả trên LocalDB không chứng minh database thật đã được nâng.
