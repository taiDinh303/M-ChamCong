# Kế hoạch triển khai hệ thống chấm công Marixa V1

> Tác giả & chủ tài liệu: **Đinh Văn Tài**. Cập nhật: 12/10/2026 (viết lại theo bộ stack thật: ASP.NET 8 `M.API` + React 19 `marixa_react` + DB linh hoạt + Render/Vercel/Supabase).

## 0. Cách dùng kế hoạch (dành cho AI)

1. Đọc `docs/worklog.md` trước (bản đồ chức năng đã có/chưa có), rồi đọc `01`–`08` để nắm nghiệp vụ/kiến trúc/luồng.
2. **Quy tắc:** khi đặc tả `01`–`07` khác với thực tế code hoặc quyết định mới, **mục 1 dưới đây + code đang chạy** là chuẩn; ghi nhận khác biệt vào `worklog.md`.
3. Mỗi bước hoàn thành phải **đã kiểm chứng** (chạy được / có bằng chứng). Không đánh dấu `[x]` khi chưa kiểm tra. Ghi ngày, file/commit, kết quả vào `worklog.md`.
4. Secret (DB/JWT/Supabase) chỉ đặt trong Render/Vercel Dashboard, không commit; không gửi qua chat.
5. Docker chỉ dùng cho **build/chạy API trên Render**; dev local chạy .NET trực tiếp.

## 1. Quyết định & trạng thái hiện hành (đang chạy)

| Chủ đề | Trạng thái / quy tắc hiện hành |
| --- | --- |
| Backend | **ASP.NET Core 8** (`marixa_aspnet/M.API`), EF Core, JWT Bearer, Swagger. |
| Frontend | **React 19 + Vite SPA** (`marixa_react`), React Router 7, gọi API qua `VITE_API_BASE_URL`. |
| Auth | **ASP.NET Identity + JWT** (login username/email/sđt, kích hoạt bằng mã). Chưa dùng Supabase Auth/RLS. |
| Role | **4 role** đã map quyền: `Admin`, `Manager`, `HR`, `Employee` (seed `RoleSeeder.cs`) + **3 role nghiệp vụ đã seed** `Accounting`, `Sale`, `Marketing` (chưa gán quyền module, 12/10). |
| DB | **Linh hoạt**: SQL Server (dev) / PostgreSQL (Supabase, prod) qua `DatabaseConfig.cs` + `DATABASE__PROVIDER`, `CONNECTIONSTRINGS__MYCNN`. |
| Ảnh | **Supabase Storage bucket public `marixa-photos`** (prod, qua `SUPABASE__URL` + `SUPABASE__SERVICE_KEY`); dev fallback `wwwroot/uploads`. **Xác nhận 12/10: giữ public.** |
| Chấm công | 1 bản ghi/ngày; log vào/ra; tính giờ thực tế + tăng ca theo `AttendanceRule` active; camera + GPS **tùy chọn**, thiếu vẫn chấm. |
| Deploy | API → **Render** (`https://marixa001-asp.onrender.com`, Free); web → **Vercel** (`https://marixa001.vercel.app`, Hobby, team `taiteam/marixa_001`); DB/Storage → **Supabase** ref `zphqfcwrwitrqdeueonu` (Free, Singapore). |

> **Xác nhận 12/10/2026 (chủ dự án):** thư mục `marixa_aspnet/web` (Next.js) **ĐÃ XÓA** (không liên quan, không còn bị tham chiếu). Role nghiệp vụ `Accounting`, `Sale`, `Marketing` **ĐÃ seed** trong code (`RoleSeeder.cs`), chưa gán quyền module. Ca làm chung **giữ đơn giản** (1 rule active; **rút** đề xuất "version + ngày hiệu lực"). Offline/IndexedDB, khóa kỳ + snapshot + Excel/PDF **chưa làm**.

## 2. Việc tiếp theo (theo module, ưu tiên từ `worklog.md`)

- [ ] **Offline / đồng bộ trễ**: thêm hàng đợi IndexedDB cho chấm công khi mất mạng; event đến sau khi khóa kỳ → điều chỉnh kỳ mở sau (không sửa snapshot).
- [ ] **Khóa/mở kỳ công** theo tháng + snapshot không đổi; Excel kỳ mở = "bản tạm", kỳ khóa = snapshot.
- [ ] **Xuất Excel/PDF**: bảng công (Excel) và đơn nghỉ (PDF) theo quyền & phiên bản (hiện mới có upload báo cáo NV; chưa có export kỳ công).
- [ ] **Sổ phép tự động**: giao dịch cộng phép năm hằng tháng (idempotent) khi có chính sách.
- [ ] **Map quyền module cho 3 role mới** (`Accounting`/`Sale`/`Marketing`): hiện đã seed trong `RoleSeeder.cs` nhưng chưa gán quyền; cần thêm vào `MODULES` (frontend) + `[Authorize(Roles=...)]` (backend) khi muốn dùng.
- [ ] **Bảo mật ảnh**: cân nhắc bucket **private** + đọc ảnh theo quyền (hiện public). Quyết định + cập nhật `docs/02` trước khi làm.
- [ ] **Test**: unit (tính công), integration (API+DB+upload), e2e (NV→HR→Admin) theo `05-testing-strategy.md`.
- [ ] **Sao lưu/khôi phục**: `pg_dump` nghiệp vụ + backup ảnh; thử restore trên project test.
- [ ] **Deploy thật**: config Render + Vercel + Supabase theo `SETUP_CLOUD.md`; smoke test end-to-end URL thật.

> Danh sách chi tiết "đã có / chưa có" theo từng trang & module nằm ở **`docs/worklog.md`**.

## 3. Tài liệu nhà cung cấp (đối chiếu khi triển khai)

- PostgreSQL/Supabase: [connection (session pooler)](https://supabase.com/docs/guides/database/connecting-to-postgres), [storage](https://supabase.com/docs/guides/storage/buckets/fundamentals), [billing](https://supabase.com/docs/guides/platform/billing-on-supabase).
- Vercel: [environment variables](https://vercel.com/docs/environment-variables), [cron](https://vercel.com/docs/cron-jobs/usage-and-pricing).
- Render: [blueprint](https://render.com/docs/blueprint-spec).

Hạn mức/giá/điều khoản có thể đổi — kiểm tra lại tại thời điểm triển khai; ghi thay đổi vào `worklog.md`.
