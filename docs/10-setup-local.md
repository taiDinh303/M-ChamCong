# 10— Setup môi trường dev local (end-to-end) cho Marixa

> Tác giả & chủ tài liệu: **Đinh Văn Tài**. Cập nhật: 14/10/2026.
> Cách chạy bộ stack **đang chạy** (ASP.NET 8 `M.API` + React 19 `marixa_react`) trên máy dev từ đầu. Dev local dùng **SQL Server** (không dùng Supabase — Supabase chỉ cho prod).

## 0. Cách đọc (dành cho AI)

- Dev local: **SQL Server** + `.NET` chạy trực tiếp (không Docker), frontend `npm run dev` + Vite.
- Chi tiết config prod (Render/Vercel/Supabase): xem `08-deploy-detail.md` (đã gộp nội dung `06` vào).
- Bảng instance hosting đang chạy: xem `00-README.md` §Quick facts.
- Ma trận chức năng đã có/chưa có: xem `worklog.md` §2.

## 1. Yêu cầu phần mềm

| Phần mềm | Phiên bản | Ghi chú |
| --- | --- | --- |
| .NET SDK | 8.0.x | Build `M.BE.sln`. |
| SQL Server | 2019+ (hoặc LocalDB / SQL Server Express) | Dev local. |
| Node.js | 20.x LTS | Build/chạy `marixa_react` (Vite). |
| Visual Studio / Rider / VS Code + C# extension | Tuỳ chọn | Chạy debug .NET. |
| Git | Bất kỳ | Clone repo. |

## 2. Clone & build

```bash
git clone https://github.com/taiDinh303/M-ChamCong.git
cd M-ChamCong

# Backend (solution .NET 8, nằm trong marixa_aspnet/)
cd marixa_aspnet
dotnet restore
dotnet build M.BE.sln -c Debug
cd ..

# Frontend (React 19 + Vite)
cd marixa_react
npm install
cd ..
```

## 3. Cấu hình CSDL dev (SQL Server)

- Chỉnh `marixa_aspnet/M.API/appsettings.json` → `ConnectionStrings:MyCnn` về SQL Server local của bạn (ví dụ):
  `Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Marixa_001;Integrated Security=True;Encrypt=False`
- `appsettings.json` hiện có `Database:Provider = "SqlServer"` (dev mặc định). **Không cần** env var `DATABASE__PROVIDER` / `CONNECTIONSTRINGS__MYCNN` cho dev (chúng chỉ dùng khi prod/PostgreSQL).
- Khi khởi động, `M.API/Program.cs` tự chạy:
  - **SQL Server (dev)**: `db.Database.MigrateAsync()` (thực thi migration T-SQL có sẵn trong `M.Repositories/Migrations/`).
  - **PostgreSQL (prod)**: chạy DDL idempotent + `PostgresSchemaSync.EnsureColumnsAsync` (không chạy migration T-SQL).
- Sau đó chạy seeder idempotent: `RoleSeeder`, `AttendanceRuleSeeder`, `AdminSeeder`, `SampleDataSeeder` (dữ liệu mẫu dev).
- `AdminSeeder` tạo/không có tài khoản admin (config `Seed:Admin:Username/Email/Password` trong appsettings nếu cần; mặc định theo code).

> **Lưu ý:** schema Postgres được tạo bằng `GenerateCreateScript()` (không phải migration T-SQL). Khi thêm/sửa entity mới ở `M.Contract.Repositories/Entities/*.cs`, Postgres tự thêm cột ở boot lần sau (schema sync); SQL Server dev thì cần tạo migration mới (`dotnet ef migrations add <ten>` trong `M.Repositories`).

## 4. Chạy dev (2 terminal)

```bash
# Terminal 1 — backend .NET (API + Swagger)
cd marixa_aspnet
dotnet run --project M.API
# API: http://localhost:5xxxx (xem log) · Swagger: .../swagger
# Ảnh dev: lưu đĩa wwwroot/uploads → serve /uploads

# Terminal 2 — frontend React (Vite dev server)
cd marixa_react
npm run dev
# Vite dev server (thường http://localhost:5173 hoặc theo vite.config.js)
# Frontend gọi backend qua VITE_API_BASE_URL (xem .env.example)
```

- Kiểm tra: mở Swagger → login (dùng tài khoản admin/employee mẫu đã seed) → chấm công → upload ảnh (dev lưu đĩa `wwwroot/uploads`, không cần Supabase).
- Nếu cần dữ liệu mẫu: `SampleDataSeeder` tạo sẵn ~8 tài khoản (NV/NH/QT/Admin/KT) — xem `M.API/Seed/SampleDataSeeder.cs`.

## 5. Chạy test (theo `06-testing.md`)

```bash
# Unit (tính công/tăng ca, ledger phép, quy tắc ca)
cd marixa_aspnet
dotnet test

# Frontend (nếu có)
cd marixa_react
npm test   # (hỗ trợ khi đã cấu hình; hiện chưa có bộ test e2e — xem worklog §3)
```

> Dev local **không dùng Docker**. Docker chỉ dùng cho build/chạy API trên Render (prod) — xem `08`.

## 6. Kiểm tra trước khi commit

- `dotnet build M.BE.sln` không lỗi compile.
- `git status` không đưa secret (DB password, JWT key, Supabase service key) vào commit.
- Chạy 1 smoke test end-to-end (login → chấm → upload ảnh → xem ảnh) trước khi đánh dấu "xong".
- Ghi sự kiện (ngày + thay đổi + bằng chứng) vào `worklog.md` §4.
