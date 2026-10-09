# 08 — Triển khai Marixa: Vercel + Render + Supabase

Cập nhật: 09/10/2026.

## 1. Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────────────┐
│  marixa_react (Vite SPA)                                            │
│  Deploy: Vercel                                                     │
│  Domain: https://marixa-web.vercel.app                              │
│                                                                     │
│  Env var: VITE_API_BASE_URL=https://marixa-api.onrender.com/api     │
│  → axiosClient.baseURL = URL Render (cross-origin, CORS cần bật)    │
│  → resolvePhotoUrl tự derive API origin (ảnh chấm công)            │
│                                                                     │
│  Vercel CDN (toàn cầu) serve React static files + SPA fallback      │
│  (vercel.json rewrites mọi route không có /assets → index.html)     │
└──────────────────────────┬──────────────────────────────────────────┘
                           │ HTTPS
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│  marixa_aspnet/M.API (ASP.NET 8)                                    │
│  Deploy: Render Web Service (Docker)                                │
│  Domain: https://marixa-api.onrender.com                            │
│                                                                     │
│  Env vars (Render Dashboard → Environment):                         │
│    ASPNETCORE_ENVIRONMENT=Production                                │
│    DATABASE__PROVIDER=PostgreSQL                                    │
│    CONNECTIONSTRINGS__MYCNN=postgresql://...                        │
│    JWTSETTINGS__KEY=(secret)                                        │
│                                                                     │
│  Dockerfile: build .NET 8 → run trên aspnet:8.0                     │
│  Health check: /swagger                                              │
│  CORS: bật cho *.vercel.app + production origins (hardcoded)       │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Supabase PostgreSQL (ap-southeast-1)                               │
│  Project ref: pkpwcpatuslfjyoivbuf (test)                           │
│                vmpsfwwfgoeritayfnvv (prod)                          │
│                                                                     │
│  Session pooler:                                                    │
│    host=aws-0-ap-southeast-1.pooler.supabase.com:5432             │
│    user=postgres.<REF>  (không phải "postgres")                    │
│    sslmode=require                                                  │
└─────────────────────────────────────────────────────────────────────┘
```

## 2. Cơ chế DB linh hoạt trong code

Tất cả đọc `Provider` + `ConnectionString` đi qua `M.API/DatabaseConfig.cs`:

| Thứ tự ưu tiên | Nguồn |
|---|---|
| 1 | `IConfiguration` (appsettings.json + appsettings.Production.json + env vars đã merge) |
| 2 | `Environment.GetEnvironmentVariable("DATABASE__PROVIDER")` |
| 3 | `Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__MYCNN")` |
| 4 | Default: `SqlServer` |

Khi provider = `PostgreSQL`, connection string URI (`postgresql://user:pass@host/db`)
được tự normalize qua `NpgsqlConnectionStringNormalizer` trước khi truyền vào `UseNpgsql`.

**Dev local (SQL Server)**: không cần env var nào, appsettings.json đủ.
**Prod (PostgreSQL)**: đặt 2 env var trong Render dashboard → auto-merge vào config.

## 3. File cấu hình đã tạo/sửa

| File | Đường dẫn | Mục đích |
|---|---|---|
| `DatabaseConfig.cs` | `marixa_aspnet/M.API/` | Tập trung đọc DB config (env var + appsettings) |
| `appsettings.Production.json` | `marixa_aspnet/M.API/` | Mặc định PostgreSQL + CORS Vercel + JWT placeholder |
| `Dockerfile` | `marixa_aspnet/` | Build .NET 8 multi-stage → Docker image |
| `.dockerignore` | `marixa_aspnet/` | Loại bin/obj/node_modules khỏi Docker build |
| `render.yaml` | **repo root** | Render Blueprint (web service Docker) |
| `DependencyInjection.cs` | `marixa_aspnet/M.API/` | CORS thêm wildcard `*.vercel.app` |
| `Program.cs` | `marixa_aspnet/M.API/` | Startup dùng `dbConfig.IsPostgres` |
| `vercel.json` | `marixa_react/` | Vite build → dist, SPA rewrite |
| `.env.example` (backend) | `marixa_aspnet/M.API/` | Hướng dẫn env var Render |
| `.env.example` (frontend) | `marixa_react/` | Hướng dẫn VITE_API_BASE_URL |

## 4. BƯỚC TRIỂN KHAI CHI TIẾT

### Step 1 — Supabase (Database + Auth)

1. Mở [Supabase Dashboard](https://supabase.com/dashboard) → tạo project (nếu chưa).
2. Region: **Southeast Asia (Singapore)** → `ap-southeast-1`.
3. Ghi lại **Project URL**: `https://<REF>.supabase.co`
4. Ghi lại **Publishable Key** (`sb_publishable...`) và **Secret Key** (`sb_secret...`).
5. Authentication → Settings:
   - Bật **Email / Password**
   - Tắt **Allow new users to sign up**
6. Database → Connect:
   - Ghi lại **Session pooler** connection:
     ```
     host=aws-0-ap-southeast-1.pooler.supabase.com
     port=5432
     user=postgres.<REF>      (VD: postgres.pkpwcpatuslfjyoivbuf)
     database=postgres
     password=<mật khẩu từ lúc tạo project>
     ```
   - Đây là source cho `CONNECTIONSTRINGS__MYCNN`.

### Step 2 — Render (API backend)

1. Mở [Render Dashboard](https://render.com/dashboard) → New → **Blueprint**.
2. Chọn Git repo → Render đọc `render.yaml` ở repo root.
3. Sau khi Blueprint tạo service `marixa-api`:
   - Vào **marixa-api → Environment** → thêm:

   | Variable | Value |
   |---|---|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `DATABASE__PROVIDER` | `PostgreSQL` |
   | `CONNECTIONSTRINGS__MYCNN` | `postgresql://postgres.REF:PASSWORD@aws-0-ap-southeast-1.pooler.supabase.com:5432/postgres?sslmode=require` |
   | `JWTSETTINGS__KEY` | `(secret 32+ ký tự, tạo mới cho prod)` |

4. **Health Check Path**: `/swagger` (đã cấu hình trong render.yaml)
5. Ghi lại **Render URL**: `https://marixa-api.onrender.com` (hoặc subdomain khác Render cấp)

> ⚠️ **Render Free tier**: idle timeout 15 phút → API "ngủ" giữa các request.
> Dùng plan **Starter ($7)** hoặc **Standard ($70)** để production không bị lag.
> Test preview có thể dùng Free.

### Step 3 — Vercel (React frontend)

1. Mở [Vercel Dashboard](https://vercel.com/dashboard) → New → Project.
2. Import Git repo (cùng repo với Render).
3. Settings:

   | Field | Value |
   |---|---|
   | Framework Preset | **Other** (không auto-detect Vite) |
   | Root Directory | `marixa_react` |
   | Build Command | `npm run build` |
   | Output Directory | `dist` |

4. **Environment Variables** (Vercel → Settings → Environment Variables):

   | Variable | Value | Scope |
   |---|---|---|
   | `VITE_API_BASE_URL` | `https://marixa-api.onrender.com/api` | Production + Preview |

5. Deploy → ghi lại **Vercel URL**: `https://marixa-web.vercel.app` (hoặc domain tùy chọn)

> `vercel.json` đã có `outputDirectory: "dist"` + SPA rewrite (React Router fallback).
> Khi đặt `VITE_API_BASE_URL`, axiosClient gọi Render bằng URL tuyệt đối.
> `resolvePhotoUrl` tự derive API origin → ảnh chấm công tải đúng.

### Step 4 — CORS kiểm tra

CORS đã cấu hình sẵn:
- `*.vercel.app` → wildcard (phủ cả preview deployments)
- `ProductionOrigins` hardcoded (Render domains)
- `Cors:AllowedOrigins` trong appsettings.Production.json (thêm domain Vercel thật)

**Checklist sau deploy:**
- [ ] Mở `https://YOUR-VERCEL.vercel.app` → trình duyệt Network tab → không có CORS error
- [ ] Login API: `https://YOUR-RENDER.onrender.com/api/Auth/login` → 200
- [ ] Chấm công + upload ảnh → ảnh tải từ Render `https://YOUR-RENDER.onrender.com/uploads/...`

### Step 5 — Smoke test

1. Truy cập Vercel URL → trang login
2. Đăng nhập (JWT từ Render API)
3. Chấm công (POST event → Render)
4. Upload ảnh (POST photo → Render `/uploads/...`)
5. Xem ảnh trong UI (GET photo → `resolvePhotoUrl` → Render origin)
6. Kiểm tra Render logs: `DatabaseConfig` resolve đúng PostgreSQL, `PostgresSchemaSync` OK

## 5. Khắc phục sự cố

| Lỗi | Nguyên nhân | Sửa |
|---|---|---|
| `Npgsql: connection string not valid` | URI chưa normalize / thiếu `sslmode` | Kiểm tra `CONNECTIONSTRINGS__MYCNN` có `?sslmode=require` |
| `Cannot connect to host:5432` | Supabase user sai (phải `postgres.REF`, không phải `postgres`) | Lấy đúng user từ Supabase → Database → Connect |
| `403 Forbidden` (CORS) | Vercel domain chưa trong `AllowedOrigins` | Đã có wildcard `*.vercel.app` — check xem Render deploy đúng |
| `500 - InvalidOperationException` | `CONNECTIONSTRINGS__MYCNN` chưa đặt trong Render | Render → marixa-api → Environment → thêm |
| API "chậm" lần đầu (Free tier) | Render idle timeout | Up plan hoặc đặt Render `keepAlive` |
| `PostgresSchemaSync FAILED` | Cột thiếu trong DB | Kiểm tra log Render → chạy migration Supabase |

## 6. Biến môi trường tổng hợp

| Biến | Nơi đặt | Giá trị | Secret? |
|---|---|---|---|
| `VITE_API_BASE_URL` | Vercel → Env | `https://marixa-api.onrender.com/api` | Không |
| `DATABASE__PROVIDER` | Render → Env | `PostgreSQL` | Không |
| `CONNECTIONSTRINGS__MYCNN` | Render → Env | `postgresql://...` | **Có** (password) |
| `JWTSETTINGS__KEY` | Render → Env | 32+ ký tự | **Có** |
| `ASPNETCORE_ENVIRONMENT` | Render → Env / Dockerfile | `Production` | Không |

> Không commit giá trị secret vào Git. Chỉ `.env.example` (mô tả) được commit.
