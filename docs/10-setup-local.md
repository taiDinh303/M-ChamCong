# 10 — Thiết lập và chạy môi trường local

> Chuẩn cấu hình hiện hành: [CONFIGURATION-STANDARD](CONFIGURATION-STANDARD.md) — JSON Production sạch, sáu biến triển khai cơ bản, kiểm cấu hình không chạm DB và script chung cho NTC/Marixa.

> Chủ tài liệu: **Đinh Văn Tài**. Đối chiếu mã nguồn: **18/10/2026**.
> Hướng dẫn PowerShell cho ASP.NET Core 8 + React 19/Vite, SQL Server local. Development có thể init/seed theo §3; Supabase qua User Secrets đã kiểm kết nối chỉ đọc 18/10, không khởi động API hoặc migrate/seed trong lượt cấu hình này.

**CURRENT** = cấu hình/lệnh phù hợp checkout; **NOT VERIFIED** = kết quả chạy trên máy dev; **PENDING** = nghiệm thu DB/provider/browser còn thiếu.

## 1. Yêu cầu và phạm vi

| Thành phần | Yêu cầu từ repo |
| --- | --- |
| .NET SDK | 8.x; solution [M.BE.sln](../marixa_aspnet/M.BE.sln), target `net8.0` |
| SQL Server | SQL Server/Express hoặc LocalDB tương thích provider EF Core SQL Server 8; có quyền tạo/migrate DB local |
| Node.js + npm | Tương thích Vite trong lockfile; tại lần rà soát, Vite 6.4.3 khai báo `^18.0.0 \|\| ^20.0.0 \|\| >=22.0.0`. Đây là range package, không cam kết các bản đó còn được hỗ trợ bảo mật. |
| Git | Để quản lý source; IDE tùy chọn |

Repo chưa pin phiên bản Node dùng chung. Kiểm tra runtime của máy và dùng cùng phiên bản với môi trường build đã xác nhận; không tự nâng dependencies để làm theo tài liệu này. Local không cần Docker hoặc Supabase để chạy luồng cơ bản.

Đọc [00-README.md](00-README.md) trước khi sửa; [08-deploy-detail.md](08-deploy-detail.md) giải thích provider, startup và Storage; [06-testing.md](06-testing.md) chứa ca kiểm thử.

## 2. Restore và build từ đúng thư mục

Các ví dụ dùng checkout hiện tại; thay đường dẫn nếu đặt repo ở nơi khác.

```powershell
Set-Location -LiteralPath 'D:\My Project\marixa_aspnet_react\marixa_aspnet'
dotnet restore .\M.BE.sln
dotnet build .\M.BE.sln -c Release --no-restore -warnaserror
dotnet test .\M.BE.sln -c Release --no-build
dotnet tool restore
```

```powershell
Set-Location -LiteralPath 'D:\My Project\marixa_aspnet_react\marixa_react'
npm ci
npm run build
```

`npm ci` dùng `package-lock.json`; nếu lockfile không khớp package.json, xử lý sai khác có chủ đích thay vì xóa lockfile. Build thành công chỉ xác nhận biên dịch, chưa xác nhận DB/nghiệp vụ.

## 3. Cấu hình API local

Đặt biến trong **terminal backend**. Chọn database local riêng, không dùng connection string production để thử.

```powershell
Set-Location -LiteralPath 'D:\My Project\marixa_aspnet_react\marixa_aspnet'
$env:DATABASE__PROVIDER = 'SqlServer'
$env:CONNECTIONSTRINGS__MYCNN = 'Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Marixa_Local;Integrated Security=True;Encrypt=False'
$env:JWTSETTINGS__KEY = '<LOCAL_RANDOM_SIGNING_KEY_AT_LEAST_32_CHARACTERS>'
```

**Thay JWT placeholder bằng khóa riêng trước khi chạy.** Connection string trên dành cho LocalDB + Windows authentication; đổi server/authentication theo SQL Server máy bạn. Không commit mật khẩu hoặc khóa local vào `appsettings.json`; project `M.API` đã có `UserSecretsId` nếu cần lưu cấu hình development ngoài repo. `.env.example` backend không được .NET tự đọc.

Để dùng nhánh lưu ảnh đĩa, bảo đảm cấu hình hiệu lực không có đủ `Supabase:Url` + `Supabase:ServiceKey`. Nếu máy đã cấu hình Storage qua env/user secrets, kiểm tra nguồn đó trước khi chạy. Quy tắc tên biến đúng và lỗi tên cũ nằm tại `08` §2.

**Tác động startup:** Development mặc định init DB + seed role/rule; có thể tắt bằng DATABASEINITIALIZATION__ENABLED=false và SEED__ENABLED=false. Admin/sample mặc định không tạo. Muốn bootstrap admin local: đặt SEED__ENABLED=true, SEED__ADMIN__ENABLED=true và password riêng SEED__ADMIN__PASSWORD (10+ ký tự mạnh), username/email qua cấu hình riêng tư. Sample cần SEED__SAMPLE__ENABLED + SEED__SAMPLE__PASSWORD, chỉ Development. Production mặc định tắt cả init/seed. Seeder không ghi đè rule tùy chỉnh, lỗi quan trọng fail startup.

Không có mật khẩu admin/sample mặc định. Trước khi init DB có dữ liệu cũ, kiểm trùng công/ngày và rule active theo [08](08-deploy-detail.md).

### 3.1 Dùng Supabase cùng cách với NTC

Quy ước chung: `dotnet user-secrets` trong Development, `Database:Provider=PostgreSQL`, `ConnectionStrings:MyCnn` dạng Npgsql keyword/value; Session pooler 5432, `SSL Mode=VerifyFull`, `Root Certificate` trỏ CA local và `Maximum Pool Size=10`. Cấu hình Marixa hiện tại đã chuyển từ URI sang dạng này, giữ password/target cũ; truy vấn metadata trong transaction read-only PASS. CA ở `%LOCALAPPDATA%/Marixa/certificates/supabase-prod-ca-2021.crt`.

Chỉ giữ một khối lệnh dùng lại cho cả hai project trong file cá nhân `D:\My Project\NTC\docs\SUPABASE-DOTNET-COMMANDS.md`: chọn `Marixa` hoặc `NTC`, nhập password ẩn, builder xử lý ký tự đặc biệt. Không sao chép password/connection thật vào tài liệu. Máy khác cần lấy CA của đúng project và điều chỉnh root/API theo checkout; đường dẫn CA Windows không dùng trực tiếp trong Render.

Khi local trỏ cloud, User Secrets Marixa đặt `DatabaseInitialization:Enabled=false` và `Seed:Enabled=false` để tránh mặc định Development ghi schema/seed. Environment/CLI vẫn ưu tiên hơn User Secrets: terminal còn các biến SQL Server ở ví dụ §3 sẽ ghi đè profile Supabase. Lưu cấu hình không tạo tài khoản/migrate; khởi động API có thể chạy worker ngày hiệu lực. Tests vẫn dùng database cô lập. JWT và Storage cấu hình riêng, publishable key không phải DB password hay server upload key. Hosting/Storage chưa được đổi bởi thao tác local này.

## 4. Cấu hình frontend

```powershell
Set-Location -LiteralPath 'D:\My Project\marixa_aspnet_react\marixa_react'
if (-not (Test-Path -LiteralPath '.env.local')) {
    Copy-Item -LiteralPath '.env.example' -Destination '.env.local'
}
```

Kiểm tra `.env.local` có cấu hình dev sau và **không bật `VITE_API_BASE_URL` tới cloud**:

```dotenv
API_PROXY_TARGET=http://localhost:57432
# VITE_API_BASE_URL không đặt khi dùng dev proxy.
```

- [apiConfig.js](../marixa_react/src/services/api/apiConfig.js) dùng `/api` khi không có base URL tuyệt đối.
- [vite.config.js](../marixa_react/vite.config.js) proxy cả `/api` và `/uploads` tới API; mặc định target `http://localhost:57432`.
- `.env.example` frontend có ví dụ domain cloud cũ; không dùng nó làm nguồn xác định hosting hiện hành.
- Restart Vite sau khi đổi `.env.local`. Nếu chọn base URL tuyệt đối, trình duyệt gọi API trực tiếp và chịu CORS; đường dẫn ảnh tương đối lấy origin API.

## 5. Chạy bằng hai terminal độc lập

**Terminal 1 — backend**, dùng cấu hình đã đặt tại §3:

```powershell
Set-Location -LiteralPath 'D:\My Project\marixa_aspnet_react\marixa_aspnet'
dotnet run --project .\M.API\M.API.csproj --launch-profile M.API --urls http://localhost:57432
```

Lệnh chỉ định HTTP để khớp Vite proxy. Profile gốc trong [launchSettings.json](../marixa_aspnet/M.API/Properties/launchSettings.json) có HTTPS `57431` và HTTP `57432`, đặt environment `Development`. Swagger: [http://localhost:57432/swagger](http://localhost:57432/swagger). Đọc log thực tế nếu cổng bị chiếm hoặc bạn override cấu hình.

**Terminal 2 — frontend**:

```powershell
Set-Location -LiteralPath 'D:\My Project\marixa_aspnet_react\marixa_react'
npm run dev
```

Mở URL Vite in ra terminal (thường port `5173`; có thể đổi khi bị chiếm). Nhánh ảnh đĩa lưu dưới `marixa_aspnet/M.API/wwwroot/uploads/attendance/...` và được serve qua `/uploads/attendance/...`.

## 6. Kiểm tra kết quả và trạng thái test

1. Đọc log backend để xác nhận migration/seeder không lỗi; Swagger mở được chưa chứng minh DB sẵn sàng.
2. Đăng nhập bằng tài khoản local đã chuẩn bị, kiểm tra JWT và dữ liệu nhân viên.
3. Upload ảnh hợp lệ → kiểm tra body thành công và URL đọc được → check-in/check-out → lịch sử công. Các bước ghi DB phải dùng dữ liệu giả; xem `06` cho sai khác ảnh/GPS/ngày công.
4. Kiểm tra Network: request `/api`/`/uploads` qua Vite đúng target; không nhận HTML SPA thay JSON.

**Backend có M.Tests** với xUnit/HTTP SQLite riêng, LocalDB fixture tự tạo/drop DB thử và PostgreSQL loopback khi được cấu hình; không ghi DB ứng dụng/live. Frontend có Node test runner `npm test`, không cần API/secret cho các test này. Chạy:

```powershell
Set-Location -LiteralPath 'D:\My Project\marixa_aspnet_react\marixa_aspnet'
dotnet build .\M.BE.sln -c Release -warnaserror
dotnet test .\M.BE.sln -c Release --no-build
```

```powershell
Set-Location -LiteralPath 'D:\My Project\marixa_aspnet_react\marixa_react'
npm test
npm run lint
npm run build
```

Ghi kết quả thực tế; lint/build là kiểm tra tĩnh, không thay thế các ca nghiệp vụ trong `06`. SQL Server fixture đã có; PostgreSQL theo CI và browser E 2 E/live còn mở theo [01-plan.md](01-plan.md).

## 7. Lỗi local thường gặp

| Triệu chứng | Cách kiểm tra |
| --- | --- |
| Vite proxy `ECONNREFUSED` | API đã chạy đúng HTTP `57432`; target trong `.env.local` khớp log; restart Vite. |
| SQL login/migration lỗi | SQL Server đã cài/chạy, connection string và quyền DB local đúng; không đổi sang production để chữa lỗi. |
| Ảnh không hiện | Body upload có thành công, file local tồn tại, `/uploads` được proxy; nếu trả URL Supabase thì Storage đang được cấu hình. |
| Upload 400/502 |400: file/size/signature;502: credential/bucket/network Storage, xem 08. |
| Tài khoản mẫu không đăng nhập được | Kiểm tra seeder/log và dữ liệu local; không suy ra mật khẩu hoặc dùng credential production. |

## 8. Contract for Codex / Agnes

Run each command from its stated directory. Use a local SQL Server database and the HTTP API at `57432` with Vite's `/api` and `/uploads` proxy. Development bootstrap is configurable; production defaults off. M.Tests uses isolated SQLite and a per-host clock. Report unit/HTTP test results separately from real-provider, browser and cloud checks. Do not claim a successful runtime check from documentation review alone.
