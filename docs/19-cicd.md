# 19 — CI và kiểm tra lặp lại

> Cập nhật 2026-10-17. Nguồn [backend.yml](../.github/workflows/backend.yml). Kết quả từng lần ở worklog; cấu hình workflow không chứng minh job remote đã chạy.

## 1. Workflow

Push/PR chạm backend/docs/validator chạy GitHub Actions; có workflow_dispatch. Quyền contents:read, timeout 15 phút. Runner Ubuntu, SDK 8, PostgreSQL 17 container riêng với credential thử nghiệm. Restore → Release build -warnaserror → tests/TRX/artifacts → Markdown → publish → Docker build. Artifacts gồm TRX, API inventory và sample export giả. Test PostgreSQL chỉ nhận loopback, tạo/drop DB có prefix Marixa_Verify_; không dùng GitHub production secret.

## 2. Chạy local

Từ repo root, PowerShell:

```powershell
dotnet restore .\marixa_aspnet\M.BE.sln -warnaserror
dotnet build .\marixa_aspnet\M.BE.sln -c Release --no-restore -warnaserror
dotnet test .\marixa_aspnet\M.BE.sln -c Release --no-build --logger trx
python .\scripts\verify_docs.py
dotnet publish .\marixa_aspnet\M.API\M.API.csproj -c Release --no-build -o "$env:TEMP\marixa-api-publish"
```

Windows cần LocalDB để chạy SqlServer provider fixture; thiếu LocalDB là lỗi môi trường cần xử lý, không gọi cả suite pass. PostgreSQL local không có thì test ghi SKIP; CI chạy test này. Muốn sinh inventory/export, đặt MARIXA_ARTIFACT_DIR vào thư mục artifact riêng rồi chạy các test ContractInventoryTests/ExportTests; đồng bộ api-inventory.md và kiểm diff trước commit.

EF tools chạy từ marixa_aspnet: dotnet tool restore, dotnet ef migrations has-pending-model-changes --project M.Repositories --startup-project M.API; môi trường Testing, init/seed tắt. SQL migration chỉ sinh/đọc ở bước này, không apply vào DB thật. NuGet advisory audit gồm transitive; không gọi không-có-advisory là chứng nhận an toàn toàn bộ. Frontend build/lint/E 2 E là phạm vi riêng nếu sửa UI.
