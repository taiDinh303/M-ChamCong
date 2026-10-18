# Dữ liệu DEMO Marixa

> Bộ dữ liệu giả tháng 10/2026 do chủ dự án yêu cầu ngày 18/10. Tool chỉ chạy khi gọi trực tiếp; không chạy lúc API khởi động. Trạng thái apply/build/test live được ghi riêng tại [worklog](../../../docs/worklog.md).

## 1. Phạm vi

- Sáu nhân viên DEMO-001…006, ba phòng ban, bốn chức vụ; hợp đồng sắp hết hạn/thử việc, lương, bảo hiểm, ngân hàng giả, người phụ thuộc, ca/lịch làm và bảng lương nháp/đã tính.
- Tháng 10: 72 ngày công, 143 log vào/ra, có trễ/về sớm/thiếu ra; 12 đơn OT gồm Approved/Pending/Rejected; một yêu cầu sửa công Pending. Giờ thường/OT tính bằng calculator hiện hành, không cộng OT khi chưa được duyệt.
- Bốn đơn nghỉ Pending/Approved/Rejected/Cancelled; sổ phép có sáu Grant, một Adjust +0.5, hai Deduct và một Refund. Những khoản cấp là fixture thủ công, không thay chính sách cộng phép.
- Ba đề xuất thăng chức và ba bàn giao Pending/Approved-chờ-hiệu-lực/Rejected, ngày hiệu lực tháng 11; job không tác động hồ sơ thật.
- Một dự án, bốn thành viên, ba task/bình luận/đánh giá; ba báo cáo có tệp PDF thật nộp qua API để tạo version/attachment/event hợp lệ.
- Kỳ 9/2026 chỉ tạo khi chưa có kỳ hoặc ngày công: một ngày công DEMO đã duyệt, snapshot SHA-256 và lịch sử review/lock. Không khóa kỳ 10/2026. Snapshot cần chọn tháng 9 trên trang Thống kê.

Các hồ sơ mang mã/tên DEMO, CreatedBy `MARIXA-DEMO-202610-V1`; email dùng example.invalid, không dùng CCCD/điện thoại/ngân hàng thật. Không đổi/xóa hồ sơ cũ, rule active, Identity role hiện có hoặc schema. Không fake token, claims, external logins, activation code hay lịch sử migration. Bảng kỹ thuật trống có thể là trạng thái đúng của ứng dụng.

## 2. Chạy tool

Từ root repo, plan không ghi dữ liệu:

```powershell
dotnet run --project marixa_aspnet/tools/Marixa.Demo -c Release -- --supabase-marixa001
```

Apply chỉ sau quyền chủ dự án, backup và kiểm thử tương đương. Thêm `--apply` để ghi. Tool đọc connection từ User Secrets của M.API hoặc `MARIXA_DEMO_ConnectionString`; không đưa connection/password vào tham số hay Git. Live chỉ nhận đúng project marixa001, bắt buộc TLS. Local dùng `--local`, chỉ nhận LocalDB có tên `Marixa_Demo_*`; chỉ local mới EnsureCreated/seed reviewer Admin cô lập. Không dùng database của chủ dự án cho tests.

`MARIXA_DEMO_Password` là password riêng >=16 ký tự cho sáu tài khoản `demo.an`, `demo.binh`, `demo.chi`, `demo.dung`, `demo.ha`, `demo.linh`, chỉ role Employee. Lưu credential ngoài Git; không đặt password production vào cấu hình seed. Đã tạo thì không tự reset password/role khi chạy lại. Role/profile reviewer có sẵn cần hợp lệ; bootstrap Admin không có EmployeeId được bỏ qua khi chọn người duyệt fixture, không tự liên kết hồ sơ thật.

`--verify-only` kiểm tra không seed: đăng nhập DEMO qua API, tải PDF private và kiểm API công/phép của chủ hồ sơ. Không kết hợp với `--apply`. Xác minh live 18/10: ba tài khoản đầu đều nhận 200 cho PDF/công/phép; 42/46 bảng có dữ liệu, chỉ bốn bảng kỹ thuật claims/external-logins/user-tokens còn trống đúng mục đích. Bộ nghiệp vụ không yêu cầu bịa dữ liệu bảo mật.

Đặt `MARIXA_DEMO_ApiUrl=https://marixa001-asp.onrender.com/` khi nộp báo cáo live, hoặc URL loopback ở local. Tool đăng nhập tài khoản DEMO và gửi hai PDF mỗi báo cáo qua endpoint private; không chế đường dẫn file. Hồ sơ report draft có mã ngắn hợp lệ để có thể tiếp tục bước nộp khi API phiên bản cũ còn lỗi tạo mã báo cáo >30 ký tự. Bản sửa mã mới <=30 ký tự đã merge PR #3 và được kiểm chứng nộp mới 200 trên API live ngày 18/10.

Seeder dùng EF SaveChanges và transaction Serializable; không bulk/raw SQL để vượt guard. Bước chuẩn bị dữ liệu là một transaction, nộp file/API là bước tiếp theo có thể tiếp tục nếu gián đoạn. Marker/ID cố định chống thêm trùng; không sửa fixture đã được người dùng test để ép về trạng thái ban đầu. Tool đối chiếu hash các dòng PostgreSQL đã tồn tại trước bước seed; chỉ in số lượng, không in dữ liệu nhân viên.

## 3. Các bài thử

1. HR → Danh sách: lọc DEMO, xem hồ sơ/hợp đồng/lương; xem thử việc và hợp đồng sắp hết hạn.
2. Duyệt công → tháng 10 → Xem tất cả: công chờ duyệt, thiếu chấm ra của DEMO-003 ngày 16/10. Tab sửa công có yêu cầu; tab OT có nhiều trạng thái. Sau duyệt OT, kiểm phần OT riêng thay đổi.
3. Nghỉ phép: xem cả bốn trạng thái, sổ phép; hủy đơn Approved bằng người khác và kiểm chỉ có một Refund.
4. Thống kê → 9/2026: xem lịch sử/snapshot, tải XLSX, thử guard tháng khóa; 10/2026 dùng để test review/lock sau khi xử lý toàn bộ công/đơn Pending.
5. Bàn giao/thăng chức: Pending để thử duyệt/từ chối, Approved tháng 11 để xem chờ hiệu lực. Khi đến ngày hiệu lực job sẽ áp với nhân viên DEMO; đây là dữ liệu test có tác động thật lên chính tài khoản DEMO.
6. Báo cáo: mở/tải PDF riêng tư, duyệt/yêu cầu chỉnh sửa; tài khoản DEMO khác không được tải file không thuộc quyền. Công việc: tiến độ, bình luận, dự án và đánh giá.

Không xóa cứng log, ledger Deduct/Refund, event/snapshot để dọn fixture. Đối với database test cần làm mới, tạo DB cô lập mới. Với live, thống nhất phương án kết thúc/ẩn DEMO và bảo toàn audit trước khi dọn. Snapshot DEMO cũ có SHA-256 dạng lowercase: API chấp nhận cùng digest khác hoa/thường, seed mới tạo uppercase; không cập nhật lại snapshot bất biến.

**Rủi ro xác nhận 18/10:** file private trong App_Data đã trả 404 sau Render redeploy dù DB còn metadata. Đã khôi phục PDF DEMO qua luồng nộp API và ghi sự kiện kỹ thuật; chỉ áp cho fixture DEMO chưa được người dùng sửa. Đây không phải sửa độ bền storage. Trước dùng hồ sơ thật cần volume bền hoặc adapter object storage private và backup/restore tệp; không đưa tài liệu HR vào bucket ảnh public.
