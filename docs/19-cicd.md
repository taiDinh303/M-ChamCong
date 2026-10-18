# 19 — CI và kiểm tra lặp lại

> Cập nhật 2026-10-18. Nguồn [backend.yml](../.github/workflows/backend.yml) và [frontend.yml](../.github/workflows/frontend.yml). Kết quả từng lần ở worklog; cấu hình workflow không chứng minh job remote đã chạy.

## 1. Workflow

Push/PR chạm backend/docs/scripts/deploy/render.yaml chạy GitHub Actions; có workflow_dispatch. Quyền contents:read, timeout 15 phút. Runner Ubuntu, SDK 8, PostgreSQL 17 container riêng với credential thử nghiệm. Restore → Release build -warnaserror → tests/TRX/artifacts → Markdown → exporter dùng secret giả → publish sạch/kiểm cấu hình tĩnh → Docker build. Artifacts gồm TRX, API inventory và sample export giả. Test PostgreSQL chỉ nhận loopback, tạo/drop DB có prefix Marixa_Verify_; không dùng GitHub production secret.

Frontend workflow khi push/PR chạm marixa_react hoặc workflow: Ubuntu, Node 24, npm ci → npm test → npm run lint -- --max-warnings 0 → npm run build → kiểm Markdown. Không dùng secret hoặc gọi API/DB cloud. Browser smoke cô lập là bằng chứng riêng; CI hiện chưa tự chạy browser.

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

EF tools chạy từ marixa_aspnet: dotnet tool restore, dotnet ef migrations has-pending-model-changes --project M.Repositories --startup-project M.API; môi trường Testing, init/seed tắt. SQL migration chỉ sinh/đọc ở bước này, không apply vào DB thật. NuGet advisory audit gồm transitive; không gọi không-có-advisory là chứng nhận an toàn toàn bộ. Frontend từ marixa_react: npm ci, npm test, npm run lint, npm run build; browser smoke theo 06. Không coi Node tests là kiểm thử camera/GPS thật.
