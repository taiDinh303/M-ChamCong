# 11— Hướng dẫn tạo môi trường Supabase + Render + Vercel cho Marixa V1

> Tác giả & chủ tài liệu: **Đinh Văn Tài**. Cập nhật: 12/10/2026 (viết lại theo bộ stack thật).
> Bộ stack đang chạy: **API .NET (Render) + React SPA (Vercel) + PostgreSQL/Storage (Supabase)**. Chủ dự án tự quản trị GitHub, Supabase, Render và Vercel. Giao diện nhà cung cấp có thể đổi tên mục — dùng liên kết tài liệu chính thức bên dưới để đối chiếu.

## 0. Cách đọc (dành cho AI)

- File này là "hướng dẫn thao tác Dashboard" (tạo project, cấu hình Auth/Storage/DB, deploy). Không phải spec nghiệp vụ.
- Chi tiết config code: `docs/08-deploy-detail.md`; quy trình deploy blueprint: `render.yaml`, `marixa_react/vercel.json`.
- Tiến độ / chức năng đã làm: `docs/worklog.md`.
- **Quy tắc đọc/ghi (file nào được sửa, không commit secret, đánh dấu "xong" khi nào): xem [`00-README.md`](00-README.md) §Quy tắc cho AI (song ngữ).** File này không lặp quy tắc đó.

## 1. Repository & hạ tầng

- Repository: `github.com/taiDinh303/M-ChamCong` (remote `origin`). Chủ dự án tự quản trị Git.

### Instance đang hosting

> **Nguồn duy nhất cho bảng instance hosting là [`00-README.md`](00-README.md).** File này không lặp bảng — chỉ link về.

- Quyết định 12/10/2026: chỉ còn **1 project Supabase duy nhất** `zphqfcwrwitrqdeueonu` (Free, Singapore); bỏ hẳn các ref test/prod cũ.
- Thư mục `marixa_aspnet/web` (bản Next.js) bị **đình chỉ (decommission)**, không thuộc đường chạy — chỉ giữ trong Git để đối chiếu, chờ xoá khi chủ dự án xác nhận.
- Hai Supabase project (test/production) được tạo sẵn ở bước sau; **không gộp test và production vào cùng database**.
- **Không ghi** publishable key, secret key, connection string, mật khẩu vào Git hay worklog. Chỉ ghi project ref/region.

## 2. Supabase (Database + Storage + Auth)

1. Tạo 2 project Free (tên gợi ý `marixa-attendance-test` / `marixa-attendance-prod`), region theo nhu cầu (thường `ap-southeast-1` Singapore).
2. **Auth**: bật Email/Password, tắt "Allow new users to sign up" (tài khoản do admin cấp). V1 đang dùng **ASP.NET Identity + JWT** phía .NET làm nguồn danh tính chính; Supabase Auth chỉ dùng nếu quyết định đổi kiến trúc — xem `docs/02` mục ghi chú.
3. **Storage**: bucket cho ảnh chấm công. Code đang dùng bucket **public `marixa-photos`** (`M.API/SupabaseStorageService.cs`). Nếu cần private, đổi bucket policy + code đọc ảnh (ghi vào worklog trước khi làm).
4. **Database**: lấy **Session pooler** connection (user `postgres.<REF>`, `sslmode=require`) → dùng làm `CONNECTIONSTRINGS__MYCNN`.

| Môi trường | Supabase project (ref ghi trong worklog) | Ghi chú |
| --- | --- | --- |
| Test | tham chiếu `docs/worklog.md` (Phase 0) | dữ liệu giả, chạy migration/test. |
| Production | tham chiếu `docs/worklog.md` | để trống, chỉ áp khi triển khai thật. |


## 3. Render (API .NET)

1. Render Dashboard → New → **Blueprint** → chọn repo `M-ChamCong` (Render đọc `render.yaml` ở root).
2. Sau khi tạo service `marixa-api`, vào **Environment** đặt:

| Variable | Value | Secret? |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Không |
| `DATABASE__PROVIDER` | `PostgreSQL` | Không |
| `CONNECTIONSTRINGS__MYCNN` | `postgresql://postgres.REF:PASS@host:5432/postgres?sslmode=require` | **Có** |
| `JWTSETTINGS__KEY` | 32+ ký tự (tạo mới) | **Có** |
| `SUPABASE__URL` | `https://<ref>.supabase.co` | Không |
| `SUPABASE__SERVICE_KEY` | secret key (upload ảnh/storage) | **Có** |

3. Health check path: `/swagger`. URL Render đang chạy: `https://marixa001-asp.onrender.com` (swagger: `https://marixa001-asp.onrender.com/swagger`).


## 4. Vercel (React frontend)

1. Vercel Dashboard → New Project → import repo. Settings:
   - Root Directory: `marixa_react`
   - Build Command: `npm run build`
   - Output Directory: `dist`
2. Environment Variables:

| Variable | Value | Scope |
| --- | --- | --- |
| `VITE_API_BASE_URL` | `https://marixa001-asp.onrender.com/api` | Production + Preview |

3. `marixa_react/vercel.json` đã có SPA rewrite (mọi route không `/assets` → `index.html`). Deploy → ghi nhận URL Vercel.


## 5. CORS & smoke test

- Backend bật CORS cho domain Vercel thật + wildcard preview (`M.API/DependencyInjection.cs`, `appsettings.Production.json`).
- Smoke test: mở URL Vercel → login (JWT) → chấm công (upload ảnh) → xem ảnh → đối soát → khóa kỳ → export. Kiểm tra log Render: `DatabaseConfig` resolve đúng PostgreSQL, boot DDL OK.


## 6. Trạng thái & ràng buộc

- Supabase gói Free (nếu dùng): database/Storage có hạn mức, không backup tự động, project ít hoạt động có thể tạm dừng; cần `pg_dump` + backup ảnh riêng + thử restore (xem `docs/06`).
- Biến secret chỉ đặt trong Render/Vercel Dashboard, **không commit**. `.env.example` (nếu có) chỉ mô tả tên biến.
- Khi host hoặc user Supabase thay đổi, sao chép lại từ nút **Connect** của đúng project, không suy ra từ region.


## 7. Trạng thái Phase 0 (nếu dùng checklist cũ)

| Môi trường | Supabase project | Region |
| --- | --- | --- |
| Thử nghiệm | ref ghi trong `docs/worklog.md` | theo chủ dự án |
| Production | ref ghi trong `docs/worklog.md` | theo chủ dự án |

Chưa cần chạy lệnh Supabase CLI. Dự án dùng **PostgreSQL client trực tiếp** cho migration/backup và **Render (Docker) + Vercel** để deploy — không dùng Supabase local stack.
