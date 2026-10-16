# 01 — Quyết định và kế hoạch triển khai Marixa V1

> Chủ tài liệu: **Đinh Văn Tài**. Cập nhật: **16/10/2026**.
> Đối chiếu mã nguồn; chưa nghiệm thu runtime/production trong lần chuẩn hóa tài liệu.

Đọc [00-README.md](00-README.md) trước. File này quản lý quyết định và backlog; ma trận chức năng/bằng chứng ở [worklog.md](worklog.md). **EN:** Preserve approved decisions, distinguish source-confirmed behavior from requirements, and close tasks only with relevant evidence.

## 1. Quyết định đang có hiệu lực

Quyết định 12/10/2026 được giữ từ lịch sử chủ dự án; chuẩn hóa ngày 16/10 không phải quyết định đổi nghiệp vụ.

| Chủ đề | Quyết định / phạm vi |
| --- | --- |
| Stack | ASP.NET Core 8 M.API + React 19/Vite; SQL Server local, PostgreSQL cloud; API Render, frontend Vercel. Hosting snapshot ở [00-README §5](00-README.md#5-thông-tin-nhanh-và-hosting). |
| Danh tính | ASP.NET Identity + JWT. Không chuyển sang Supabase Auth/RLS trong phạm vi hiện hành. |
| Role | Core Admin, Manager, HR, Employee; đã seed thêm Accountant, Sale, Marketing. Chưa có quyền nghiệp vụ chuyên biệt cho 3 role mới. |
| Ảnh và bằng chứng | Giữ bucket **public** marixa-photos. Quyết định nghiệp vụ cho ảnh/GPS tùy chọn, ngoài văn phòng vẫn ghi nhận công. UI hiện bắt buộc ảnh là sai khác cần xử lý. |
| Ca làm chung | Giữ đơn giản: một AttendanceRule active cho công ty. Đề xuất version + ngày hiệu lực đã rút ngày 12/10. Không tự bổ sung hệ đa ca riêng theo nhân viên vào V1. |
| Phép năm | Giữ chính sách hiện hành; thay đổi cách cộng/trừ phép cần căn cứ chính sách đã chốt. |
| Múi giờ | Nghiệp vụ Asia/Ho_Chi_Minh (UTC+7), lưu thời điểm UTC. Code dùng ngày UTC là sai khác cần sửa. |
| Next.js cũ | marixa_aspnet/web đã xóa; không thuộc đường chạy. SQL/RPC cũ phải phân loại trước khi sử dụng. |
| Hạ tầng | Một project Supabase theo snapshot; chưa có project test cloud riêng được xác nhận. |
| Figma | Dev Mode copy; UI Kit trước; màn hình được chọn có frame mobile 375px + desktop 1440px. [13-figma-sync.md](13-figma-sync.md) định nghĩa coverage/scaffold. |

**Các điểm cần thống nhất trước khi thực hiện:** OT hiện được code tự tính sau mốc ra ca, tài liệu cũ lại mô tả lấy giao với đơn đã duyệt; ngoại lệ HR/Admin tự duyệt OT/sửa công khác nguyên tắc cấm tự duyệt; hủy đơn nghỉ Approved chưa hỗ trợ. Ghi hiện trạng/đề xuất ở worklog §5, không tự chọn một phương án nghiệp vụ từ các mô tả cũ mâu thuẫn.

## 2. Việc tiếp theo

P0: ưu tiên trước nghiệm thu sử dụng thật. P1: hoàn thiện độ đúng và vận hành. P2: mở rộng theo nhu cầu. **Backlog không tự cấp quyền thực hiện các hạng mục trong nhiệm vụ chỉ sửa tài liệu.**

### 2.1 Độ đúng, cấu hình và phân quyền

- [ ] **P0 — Quyền phía server:** lập ma trận endpoint × role × quyền sở hữu; xác minh/sửa việc truyền EmployeeId người khác ở chấm công/xem công/đơn nghỉ. Test 401/403 và truy cập chéo dữ liệu; không coi sidebar là bảo vệ dữ liệu.
- [ ] **P0 — Auth và bootstrap:** rà soát register/change-password, seed tài khoản trên production, bảo vệ admin hoạt động cuối và route guard thiếu registry entry. Chỉ kết luận sau kiểm tra API thực tế.
- [ ] **P0 — Storage và kết nối DB:** sửa/đồng bộ SUPABASE__SERVICEKEY với Supabase:ServiceKey; kiểm tra header cho loại key thực tế; xác minh SSL options vì URI normalizer chưa giữ đầy đủ sslmode. Xem [08-deploy-detail.md](08-deploy-detail.md).
- [ ] **P0 — Ngày công Việt Nam và chống trùng:** sửa boundary ngày UTC/VN; đảm bảo một NV/ngày ở DB/transaction khi hai request đồng thời, kể cả cửa sổ chặn log 2 phút.
- [ ] **P1 — Ảnh/GPS:** thống nhất UI/API theo quyết định bằng chứng tùy chọn; phân biệt GPS hiển thị với dữ liệu gửi/lưu và cờ trong/ngoài văn phòng. Giữ bucket public.
- [ ] **P1 — Ca làm ổn định:** xử lý seeder ghi đè DEFAULT-CA khi restart; bảo đảm một rule active; thống nhất mốc tính trạng thái/tính giờ, nghỉ trưa và rounding. Không thêm version/ngày hiệu lực.
- [ ] **P1 — OT và luồng duyệt:** chốt quy tắc tự tính hay theo đơn; thống nhất tự duyệt công/OT/sửa công; ghi tiêu chí chấp nhận trước khi đổi công thức.
- [ ] **P1 — Nghỉ phép/ledger:** thống nhất hai cách tổng hợp Adjust/xóa mềm giữa LeaveLedgerService và LeaveRequestService; xác minh số dư, chống trừ/hoàn trùng và quyền sửa/xóa; chốt yêu cầu hủy Approved trước khi thêm luồng mới.

### 2.2 Chức năng và vận hành còn thiếu

- [ ] **P0 — Khóa/mở kỳ tháng + snapshot:** thiết kế trạng thái/quyền/audit/lý do mở lại và bất biến snapshot; chưa có trong API hiện hành.
- [ ] **P0 — Export theo kỳ:** XLSX bảng công khóa/mở và PDF đơn nghỉ theo quyền/phiên bản. CSV công hiện thời và XLSX hồ sơ đã có code, không thay export snapshot.
- [ ] **P0 — Bộ kiểm thử:** xây unit/integration/E2E cho luồng hiện có, mở rộng khi khóa kỳ/export được triển khai; xem [06-testing.md](06-testing.md). Chưa có suite tự động.
- [ ] **P0 — Xác minh deployment live:** đối chiếu env/seeder/schema/Storage, khác biệt plan standard trong render.yaml và snapshot Free; chạy smoke test trên dữ liệu được phép. Ghi bằng chứng riêng.
- [ ] **P1 — Backup/restore:** quy trình DB + ảnh/retention và thử restore trên môi trường test được chỉ định; chưa có bằng chứng restore.
- [ ] **P1 — Offline/IndexedDB:** hàng đợi đồng bộ trễ; thiết kế quan hệ kỳ khóa trước khi triển khai, tránh âm thầm sửa snapshot.
- [ ] **P1 — Cộng phép định kỳ:** job idempotent theo chính sách đã xác nhận; chưa có job hiện hành.
- [ ] **P1 — Audit và schema:** xác minh coverage audit; quy trình migration/backfill/rollback cho thay đổi vượt khả năng ADD COLUMN của schema sync.
- [ ] **P2 — Role nghiệp vụ:** thiết kế quyền riêng Accountant/Sale/Marketing trước khi cấp thêm endpoint/module.
- [ ] **P2 — Figma:** hoàn thiện token binding/UI Kit/coverage; chỉ đánh dấu đồng bộ khi có kiểm tra thiết kế và code.
- [ ] **P2 — Ảnh:** retention/dọn ảnh/giới hạn vận hành; private chỉ khi có quyết định thay thế lựa chọn public.

## 3. Cách đóng một hạng mục

1. Chỉ rõ yêu cầu/hiện trạng và phạm vi được giao.
2. Thay đổi cùng tài liệu liên quan; không sửa lịch sử thành công giả.
3. Chạy kiểm tra phù hợp, ghi môi trường/ngày/lệnh/kết quả và giới hạn ở worklog §4.
4. Cập nhật worklog §2–§3; đánh dấu checkbox khi tiêu chí thật sự đạt. Nếu chỉ sửa tài liệu, giữ lỗi code/chức năng thiếu ở trạng thái mở.

Đề xuất chưa chốt ở [worklog §5](worklog.md#5-vấn-đề--rủi-ro-đang-mở). Triển khai: [07-deploy-overview.md](07-deploy-overview.md). File này không quản lý giá/giới hạn nhà cung cấp hoặc secret.
