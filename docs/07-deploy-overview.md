# 07 — Tổng quan triển khai và nghiệm thu

> Chủ tài liệu: **Đinh Văn Tài**. Đối chiếu mã nguồn: **17/10/2026**.
> Phạm vi: mô hình triển khai, điều kiện phát hành và bằng chứng nghiệm thu. Không chứng nhận hạ tầng đang hoạt động.

**CURRENT** = cấu trúc trong repo; **NOT VERIFIED** = hạ tầng/runtime; **PENDING** = các mục nghiệm thu chưa có bằng chứng. Quy ước đầy đủ ở [00-README.md](00-README.md).

## 1. Mô hình hiện tại

```text
Trình duyệt → React/Vite trên Vercel → ASP.NET Core 8 API trên Render
                                      ├─ PostgreSQL trên Supabase
                                      └─ Supabase Storage: ảnh chấm công
```

- Đăng nhập và phân quyền: **ASP.NET Identity + JWT do API .NET cấp**.
- Quyết định hiện hành: **một project Supabase**. Instance, URL và gói hosting chỉ ghi tại [00-README.md](00-README.md); đó là thông tin chủ dự án cung cấp ngày 12/10/2026, chưa xác minh live trong lần cập nhật này.
- `marixa_aspnet/web` (Next.js) đã xóa, không thuộc hệ thống hiện tại.
- Local mặc định dùng SQL Server; cloud cấu hình PostgreSQL. Luồng tạo schema hai provider khác nhau, xem [08-deploy-detail.md](08-deploy-detail.md).

## 2. Tài liệu và cấu hình chịu trách nhiệm

| Nhu cầu | Nguồn |
| --- | --- |
| Tên biến, Storage, startup DB, CORS, xử lý sự cố | [08-deploy-detail.md](08-deploy-detail.md) |
| Thao tác thiết lập tài khoản/project cloud | [11-setup-cloud.md](11-setup-cloud.md) |
| Chạy local | [10-setup-local.md](10-setup-local.md) |
| Ca kiểm thử và tiêu chí kết quả | [06-testing.md](06-testing.md) |
| Trạng thái chức năng và việc còn thiếu | [worklog.md](worklog.md), [01-plan.md](01-plan.md) |
| Render Blueprint | [render.yaml](../render.yaml) |
| Docker API | [Dockerfile](../marixa_aspnet/Dockerfile) |
| Build và SPA fallback Vercel | [vercel.json](../marixa_react/vercel.json) |

## 3. Điều kiện trước khi phát hành

- Xác định commit, project, môi trường và dữ liệu chịu ảnh hưởng. Preview frontend trỏ cùng API vẫn có thể ghi vào cùng database; preview không tạo môi trường dữ liệu riêng.
- Đối chiếu plan thực tế trước khi áp Blueprint: `render.yaml` mới không đặt plan và dùng autoDeployTrigger:off; giữ gói service hiện hữu, không tạo lại service từ template. Không tự suy ra plan live từ file này.
- Kiểm tra biến DB/JWT/Storage theo `08`; không commit secret hoặc đưa chúng vào `VITE_*`.
- Đánh giá tác động startup: production mặc định tắt init/seed; chỉ opt-in mới ghi schema/seed. Hai constraint mới cần duplicate preflight. Sao lưu và có phương án khôi phục trước thay đổi dữ liệu/schema.
- Xác định rõ phần được phát hành theo ma trận trạng thái. Khóa kỳ, snapshot bảng công và export theo snapshot vẫn là việc chưa triển khai.

## 4. Checklist nghiệm thu chức năng hiện có

Các ô dưới đây **chưa được xác nhận** bởi lần rà soát tài liệu; người thực hiện ghi commit, môi trường, ngày và kết quả vào worklog.

- [ ] API khởi động; đã kiểm tra riêng DB/query và log DDL/seeder, /health/ready phải 200; /health/live chỉ chứng minh process.
- [ ] Web build đúng API URL; refresh một route con vẫn mở SPA; Network không lỗi CORS.
- [ ] Đăng nhập trả JWT hợp lệ; tài khoản sai/token hết hạn/quyền thiếu được xử lý đúng.
- [ ] Upload ảnh trả kết quả thành công trong **response body**; ảnh được đọc từ đúng Storage hoặc nhánh local đã chọn.
- [ ] Check-in/check-out và lịch sử công phản ánh dữ liệu thực; xác minh ngày VN và UI ảnh/GPS theo rule trên thiết bị thật, theo 06.
- [ ] Tạo/duyệt đơn nghỉ và OT theo quyền thực tế; kiểm tra người không đủ quyền và tự duyệt theo từng loại đơn.
- [ ] Kiểm tra các dạng export đang có nếu thuộc phạm vi phát hành: CSV công hiện thời, Excel hồ sơ HR; không ghi nhận chúng là snapshot bảng công đã khóa.
- [ ] Đã xác minh secret không xuất hiện trong diff, log công khai, bundle frontend hoặc bằng chứng đính kèm.

## 5. Vận hành và khôi phục

- Ghi nguồn sao lưu DB và **file ảnh** riêng; lưu thời điểm, phạm vi và kết quả thử restore. Chưa có bằng chứng tự động backup/restore trong checkout hiện tại.
- Rollback mã nguồn không tự hoàn tác schema/dữ liệu. Kế hoạch khôi phục phải xét thay đổi DB, ảnh mới và seeders sẽ chạy lại khi restart.
- Hạn mức, giá và chính sách nhà cung cấp có thể đổi; kiểm tra nguồn chính thức trước chọn/nâng gói. Tài liệu này không cam kết chi phí hoặc năng lực vận hành.

## 6. Contract for Codex / Agnes

This is a release checklist, not proof of a successful deployment. Read `08` for the configuration contract and `00` for the owner-reported hosting snapshot. A passing Swagger check does not prove database readiness. Record runtime evidence before marking acceptance items complete. Do not treat pending period locks or snapshot exports as available features.
