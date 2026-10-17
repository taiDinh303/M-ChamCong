# Marixa — Backend ASP.NET Core

> Cập nhật 2026-10-17. Solution M.BE.sln, entry M.API, target `net8.0`. DB SQL Server local hoặc PostgreSQL. Frontend React/Vite ở ../marixa_react.

Đọc [docs00](../docs/00-README.md), [quyết định](../docs/01-plan.md), [kiến trúc](ARCHITECTURE.md), [AGENTS](AGENTS.md) và [review](BACKEND_REVIEW.md) trước khi sửa. README/SQL Next.js cũ đã được phân loại legacy; không chạy supabase/migrations vào database EF hiện hành.

## Build và test

Từ repo root, PowerShell:

```powershell
dotnet restore .\marixa_aspnet\M.BE.sln
dotnet build .\marixa_aspnet\M.BE.sln -c Release --no-restore -warnaserror
dotnet test .\marixa_aspnet\M.BE.sln -c Release --no-build
```

Tests dùng SQLite riêng, LocalDB fixture tự tạo/drop DB Marixa_Verify_GUID; PostgreSQL runtime chỉ chạy khi MARIXA_TEST_POSTGRES trỏ loopback. CI có PostgreSQL container. Không trỏ test vào DB thật. Bằng chứng cuối ở worklog; build pass không thay E 2 E/cloud/restore.

## Chạy dev và phát hành

Setup/cổng/env theo [10](../docs/10-setup-local.md), registry tại [17](../docs/17-environment-config.md). Development mặc định init schema + role/rule, admin/sample opt-in có password riêng; Production mặc định không init/seed. .env không tự nạp. JWT key production tối thiểu 32 byte, khác marker local; Storage key chỉ server.

Schema mới theo [23](../docs/23-data-migration.md), backup trước apply. /health/ready kiểm DB/schema, /health/live chỉ process. Docker chạy user app; App_Data và local uploads cần storage bền trước production. Worker áp approved promotion/handover theo ngày VN mỗi phút/catch-up, xem 27.

API route/quyền tại [15](../docs/15-api-contract.md) và inventory; ảnh public, HR docs private. Khóa kỳ tạo snapshot/version để XLSX; PDF đơn nghỉ theo trạng thái hiện tại. UI backend mới chưa được nối/kiểm browser trong nhiệm vụ này.
