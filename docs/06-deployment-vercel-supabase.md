# 06 — Triển khai Render + Vercel + Supabase cho Marixa

> Tác giả & chủ tài liệu: **Đinh Văn Tài**.
> Bộ stack triển khai **đang chạy**: API .NET (Render, Docker), web SPA React (Vercel), PostgreSQL + Storage (Supabase). Tài liệu này mô tả cách cấu hình, biến môi trường và các điểm cần theo dõi.

## 0. Cách đọc (dành cho AI)

- Chi tiết từng bước Render/Vercel/Supabase: xem `08-database-flexible-deployment.md` và `SETUP_CLOUD.md`.
- File blueprint deploy: `render.yaml` (repo root), `marixa_react/vercel.json`, `marixa_aspnet/Dockerfile`.

## 0a. Hosting đang chạy (xác nhận 12/10/2026 — Đinh Văn Tài)

Các instance **đang hosting thật** (liên kết dashboard/URL public, không chứa secret):

| Nền tảng | Role | Instance / URL | Plan | Region |
| --- | --- | --- | --- | --- |
| Render | ASP.NET 8 API | `https://marixa001-asp.onrender.com` (swagger: `/swagger`) · [dashboard](https://dashboard.render.com/web/srv-db4hmnks728c73aq6h90) · service `srv-db4hmnks728c73aq6h90` | Free | — |
| Vercel | React SPA | `https://marixa001.vercel.app/` · [dashboard](https://vercel.com/taiteam/marixa_001) (team `taiteam`, project `marixa_001`) | Hobby | — |
| Supabase | PostgreSQL + Storage | ref `zphqfcwrwitrqdeueonu` · [dashboard](https://supabase.com/dashboard/project/zphqfcwrwitrqdeueonu) | Free | Singapore |

> **Quyết định 12/10/2026:** chỉ còn **1 project Supabase** `zphqfcwrwitrqdeueonu` (Singapore, Free). Bỏ hẳn các ref cũ (test/prod ghi trước đó) khỏi tài liệu. Biến `SUPABASE__URL` trong `render.yaml` đã trỏ tới project này.
> API public: `https://marixa001-asp.onrender.com/api` · Web public: `https://marixa001.vercel.app`

## 1. Kiến trúc triển khai

```
marixa_react (React 19 SPA, static dist)  --deploy-->  Vercel
        │  HTTPS, CORS (VITE_API_BASE_URL)
        ▼
marixa_aspnet/M.API (ASP.NET 8, Docker)    --deploy-->  Render (web service)
        │  Npgsql (session pooler, sslmode=require)
        ▼
Supabase: PostgreSQL (DB) + Storage (ảnh bucket public marixa-photos)
```

- Frontend là SPA static: Vercel build `marixa_react` (`outputDirectory: dist`, SPA rewrite).
- Backend: Render Blueprint đọc `render.yaml` → service `marixa-api` (Docker build context `marixa_aspnet`, Dockerfile `marixa_aspnet/Dockerfile`, health check `/swagger`).

## 2. Biến môi trường

### Render (backend)
| Biến | Giá trị | Ghi chú |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | |
| `DATABASE__PROVIDER` | `PostgreSQL` | |
| `CONNECTIONSTRINGS__MYCNN` | `postgresql://postgres.REF:PASS@host:5432/postgres?sslmode=require` | **Secret**, đặt trong Render Dashboard. |
| `JWTSETTINGS__KEY` | 32+ ký tự | **Secret**. |
| `SUPABASE__URL` | `https://<ref>.supabase.co` | Ảnh storage. |
| `SUPABASE__SERVICE_KEY` | secret key | **Secret** (upload ảnh, storage). |

### Vercel (frontend)
| Biến | Giá trị | Ghi chú |
| --- | --- | --- |
| `VITE_API_BASE_URL` | `https://marixa001-asp.onrender.com/api` | Production + Preview (đúng URL Render đang chạy). |

CORS phía backend: `Cors:AllowedOrigins` trong `appsettings.Production.json` (thêm domain Vercel thật) + wildcard cho preview. Xem `M.API/DependencyInjection.cs`.

## 3. Trình tự thiết lập

1. **Supabase**: tạo project (region phù hợp), bật Email/Password, tắt đăng ký công khai; tạo bucket `marixa-photos` (đang dùng public theo code; nếu cần private, đổi bucket policy + code đọc ảnh). Lấy session pooler connection (user `postgres.<REF>`), lưu `CONNECTIONSTRINGS__MYCNN`.
2. **Render**: import repo → Blueprint → tạo `marixa-api`; đặt biến §2; health check `/swagger`; ghi nhận URL Render.
3. **Vercel**: import repo → Root Directory `marixa_react`, build `npm run build`, output `dist`; đặt `VITE_API_BASE_URL`; ghi nhận URL Vercel.
4. **Smoke test**: login (JWT), chấm công (upload ảnh), xem ảnh, đối soát, khóa kỳ, export.

## 4. Ảnh, dung lượng & retention

- Ảnh nén client trước upload, giới hạn 5 MB, `jpg/png/webp`, bucket public `marixa-photos`.
- Postgres chỉ lưu URL, không blob.
- Theo dõi dung lượng Storage/DB của gói Supabase đang dùng; đặt retention/dọn ảnh nếu vượt hạn mức (chưa có job cron dọn ảnh trong V1 — xem `worklog.md`).

## 5. Sao lưu & giám sát

- Supabase (nếu Free) không có backup tự động: dùng `pg_dump`/`pg_restore` (client trực tiếp) sao lưu **schema + dữ liệu nghiệp vụ** định kỳ + trước migration; lưu mã hóa ngoài Supabase. Ảnh Storage cần sao lưu riêng.
- Theo dõi: dung lượng DB/Storage, lượt chấm lỗi, upload ảnh lỗi, trạng thái project (có thể tạm dừng nếu ít hoạt động).

## 6. Checklist nghiệm thu triển khai

- [ ] Render API deploy, `/swagger` hoạt động, kết nối đúng PostgreSQL (session pooler, sslmode).
- [ ] Vercel web build, `VITE_API_BASE_URL` trỏ đúng Render; CORS không lỗi.
- [ ] Login/chấm/upload ảnh/duyệt/khóa kỳ chạy được end-to-end bằng URL thật.
- [ ] Không có secret (DB/JWT/Supabase) trong Git; chỉ `.env.example` mô tả.
- [ ] Có bản sao lưu DB + ảnh, đã thử restore trên môi trường test.

> Rủi ro điều kiện sử dụng (gói Free/Hobby) và chi phí hosting cần được chủ dự án xác nhận trước khi vận hành thật. Hạn mức kỹ thuật có thể thay đổi — kiểm tra lại trang nhà cung cấp tại thời điểm triển khai.

Xem test chi tiết ở [05-testing-strategy.md](05-testing-strategy.md), cấu hình DB linh hoạt ở [08-database-flexible-deployment.md](08-database-flexible-deployment.md).
