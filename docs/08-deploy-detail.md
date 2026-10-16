# 08 — Cấu hình triển khai, dữ liệu và xử lý sự cố

> Chủ tài liệu: **Đinh Văn Tài**. Đối chiếu mã nguồn và tài liệu nhà cung cấp: **16/10/2026**.
> Đây là hợp đồng cấu hình của checkout hiện tại. Không xác nhận biến môi trường, schema hoặc trạng thái service live.

**CURRENT** = hành vi đọc từ code; **NOT VERIFIED** = cấu hình/schema/runtime live; **PENDING** = các vấn đề triển khai còn mở. Quy ước đầy đủ ở [00-README.md](00-README.md).

## 1. Phạm vi và nguồn bằng chứng

API ASP.NET Core 8 chạy Docker trên Render; React/Vite build static trên Vercel; PostgreSQL và ảnh chấm công dùng Supabase. Identity/JWT do .NET quản lý, không dùng Supabase Auth cho luồng đăng nhập hiện tại. Thông tin instance chỉ ghi tại [00-README.md](00-README.md).

Nguồn code: [DatabaseConfig.cs](../marixa_aspnet/M.API/Configuration/DatabaseConfig.cs), [Program.cs](../marixa_aspnet/M.API/Program.cs), [DependencyInjection.cs](../marixa_aspnet/M.API/Configuration/DependencyInjection.cs), [render.yaml](../render.yaml), [Dockerfile](../marixa_aspnet/Dockerfile), [vercel.json](../marixa_react/vercel.json).

Hướng dẫn Dashboard: [11-setup-cloud.md](11-setup-cloud.md). Nghiệm thu: [07-deploy-overview.md](07-deploy-overview.md). Chạy local: [10-setup-local.md](10-setup-local.md).

## 2. Hợp đồng biến môi trường

ASP.NET Core chuyển `__` thành `:`; key không phân biệt hoa/thường. Biến môi trường ghi đè appsettings theo cấu hình mặc định. File `.env.example` backend chỉ là tài liệu tham khảo, **không tự được .NET nạp**. Xem [Microsoft Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0).

| Biến | Nơi đặt | Giá trị mẫu / mục đích | Bí mật |
| --- | --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | API | `Production` | Không |
| `DATABASE__PROVIDER` | API | `PostgreSQL` trên cloud; `SqlServer` local | Không |
| `CONNECTIONSTRINGS__MYCNN` | API | Connection string của đúng DB | **Có** nếu chứa thông tin xác thực |
| `JWTSETTINGS__KEY` | API | `<RANDOM_JWT_SIGNING_KEY_AT_LEAST_32_CHARACTERS>`; phải thay placeholder | **Có** |
| `JWTSETTINGS__ISSUER` | API, khi cần override | Mặc định appsettings: `API`; phải khớp bên cấp/kiểm tra token | Không |
| `JWTSETTINGS__AUDIENCE` | API, khi cần override | Mặc định appsettings: `APIUser` | Không |
| `SUPABASE__URL` | API | `https://<PROJECT_REF>.supabase.co` | Không |
| **`SUPABASE__SERVICEKEY`** | API | `<SERVER_STORAGE_CREDENTIAL>` đọc vào `Supabase:ServiceKey`; xem §4 | **Có** |
| `CORS__ALLOWEDORIGINS__0` | API, khi thêm domain | `https://<WEB_DOMAIN>`; dùng `__1`, `__2` cho phần tử tiếp theo | Không |
| `SEED__ADMIN__USERNAME` / `SEED__ADMIN__EMAIL` | API | Thông tin bootstrap admin do chủ dự án quản lý | Dữ liệu hạn chế |
| `SEED__ADMIN__PASSWORD` | API | `<BOOTSTRAP_ADMIN_PASSWORD>`; không dùng mặc định trong code | **Có** |
| `VITE_API_BASE_URL` | Build frontend/Vercel | `https://<API_HOST>/api` | Không |
| `VITE_API_PROXY_TARGET` | Vite dev local | `http://localhost:57432`; không phải proxy trên Vercel | Không |

**Sai khác tên biến cần chú ý:** comment trong code/Blueprint cũ ghi `SUPABASE__SERVICE_KEY`, nhưng service đọc `Supabase:ServiceKey`. Tên cũ tạo key `Supabase:SERVICE_KEY` và không khớp. Hướng dẫn này dùng **`SUPABASE__SERVICEKEY`**; việc sửa comment/template là việc code/config riêng. Không suy ra Storage đã cấu hình đúng chỉ vì Dashboard có tên biến cũ.

Các `.env.example` hiện còn domain/project ref cũ; chỉ tham khảo tên biến, lấy đích từ `00` và hợp đồng này. Tuyệt đối không đưa credential DB, khóa ký JWT hoặc khóa Storage vào `VITE_*`; các giá trị đó có thể được đưa vào bundle trình duyệt. Xem [Vite env](https://vite.dev/guide/env-and-mode).

## 3. Database và hành vi startup

### 3.1. Chọn provider và connection string

`DatabaseConfig.FromConfiguration` đọc `Database:Provider` và `ConnectionStrings:MyCnn` từ `IConfiguration`, rồi fallback trực tiếp sang env nếu key thiếu. Provider mặc định là `SqlServer`; chỉ giá trị `PostgreSQL` (không phân biệt hoa/thường) bật Npgsql. Mọi giá trị khác đi nhánh SQL Server, không có kiểm tra allowlist.

Mẫu session pooler, **chỉ chứa placeholder**:

```text
postgresql://postgres.<PROJECT_REF>:<URL_ENCODED_PASSWORD>@<POOLER_HOST>:5432/postgres?sslmode=require
```

Lấy host, port và user từ **Connect** của đúng Supabase project; không suy ra host từ region. Session pooler dùng user dạng `postgres.<PROJECT_REF>`. Xem [Supabase connection guide](https://supabase.com/docs/guides/database/connecting-to-postgres).

**Giới hạn normalizer hiện tại:** [NpgsqlConnectionStringNormalizer.cs](../marixa_aspnet/M.API/Configuration/NpgsqlConnectionStringNormalizer.cs) đổi URI sang keyword/value nhưng chỉ chuyển `sslmode=require` thành `TrustServerCertificate=true`, bỏ các query options khác. Vì vậy không được khẳng định SSL Mode đã được enforce/giữ nguyên từ URI. Cần sửa và kiểm thử riêng; dạng keyword/value được chuyển nguyên trạng và có thể đặt rõ `SSL Mode=Require` hoặc cấu hình xác minh certificate theo môi trường.

### 3.2. Startup có ghi DB

| Provider | Hành vi code hiện tại |
| --- | --- |
| SQL Server | `MigrateAsync()` chạy EF migrations trong `M.Repositories/Migrations`; Development còn có lần gọi migrate bổ sung. |
| PostgreSQL | Lấy `GenerateCreateScript()`, tách/chạy từng DDL; bỏ qua một số lỗi dựa trên message. Sau đó `PostgresSchemaSync.EnsureColumnsAsync` thêm cột thiếu. Không chạy migration T-SQL. |
| Cả hai | Gọi `RoleSeeder`, `AttendanceRuleSeeder`, `AdminSeeder`, `SampleDataSeeder`; chưa có guard chỉ chạy seeder mẫu trong Development. |

`PostgresSchemaSync` **chỉ ADD cột thiếu** với mapping kiểu đơn giản. Nó không đổi kiểu/rename/drop, backfill, đồng bộ đầy đủ nullability/default/index/FK/unique constraint, hay cung cấp lịch sử migration/rollback. Không xóa database để chữa thiếu bảng theo gợi ý trong log.

Các lỗi khởi tạo DB/seeder phần lớn được catch và ghi warning rồi app tiếp tục chạy. `/swagger` phản hồi không chứng minh DB hoặc nghiệp vụ sẵn sàng. Phải kiểm tra query nghiệp vụ và log tương ứng.

`AttendanceRuleSeeder` có thể ghi lại ca `DEFAULT-CA` về 08:00–17:00, nghỉ 60 phút, 8 giờ chuẩn và active khi restart. `AdminSeeder` có giá trị fallback trong code; `SampleDataSeeder` có tài khoản/mật khẩu mẫu và chạy cả Production. Cần xử lý bootstrap/seeding trước nghiệm thu production; không chép các giá trị mặc định vào tài liệu/log.

### 3.3. Phân biệt hai lịch sử schema

- [M.Repositories/Migrations](../marixa_aspnet/M.Repositories/Migrations): lịch sử EF của ứng dụng .NET, hiện chứa SQL Server/T-SQL.
- [supabase/migrations](../marixa_aspnet/supabase/migrations): SQL/RPC/RLS của kiến trúc Supabase trước đây, có tên bảng và danh tính khác. Sự tồn tại các file này không chứng minh khóa kỳ/snapshot/RPC đang nối vào ứng dụng .NET.
- Không chạy lại bộ SQL cũ để chữa lỗi EF thiếu cột. Với thay đổi schema, cần script PostgreSQL có review, kế hoạch backfill/rollback và kiểm tra ở DB dùng dữ liệu giả trước khi áp lên dữ liệu thật. Xem [04-database-design.md](04-database-design.md).

## 4. Ảnh chấm công và Storage

Nguồn: [SupabaseStorageService.cs](../marixa_aspnet/M.API/Storage/SupabasePhotoStorage.cs), [UploadController.cs](../marixa_aspnet/M.API/Controllers/Files/UploadController.cs), [PhotoStore.cs](../marixa_aspnet/M.API/Storage/PhotoStore.cs), [photoUrl.js](../marixa_react/src/utils/photoUrl.js).

| Điều kiện thực tế | Nơi lưu và URL trả về |
| --- | --- |
| Đọc được cả `Supabase:Url` và `Supabase:ServiceKey` | Bucket public `marixa-photos`; object `{checkin\|checkout}/{yyyyMM}/{guid}.{ext}`; URL tuyệt đối `https://<REF>.supabase.co/storage/v1/object/public/marixa-photos/...`. |
| Thiếu một trong hai key | Đĩa `wwwroot/uploads/attendance/{checkin\|checkout}/{yyyyMM}/...`; đường dẫn `/uploads/attendance/...`. |
| Đã cấu hình nhưng upload Supabase lỗi | Trả lỗi `UPLOAD_FAILED` trong body; **không fallback xuống đĩa**. |

API ảnh yêu cầu JWT, chỉ nhận `.jpg/.jpeg/.png/.webp`, tối đa 5 MiB theo code. Bucket public cho phép người có URL đọc ảnh; quyết định hiện hành giữ public. Chuyển private cần thay đổi cách đọc URL và được ghi nhận là quyết định kiến trúc. Xem [Supabase bucket access model](https://supabase.com/docs/guides/storage/buckets/fundamentals).

**Tương thích credential:** service hiện chỉ gửi `Authorization: Bearer <ServiceKey>`, không gửi `apikey`; đây là cách code hiện dùng credential JWT legacy `service_role`. Không hướng dẫn thay trực tiếp bằng `sb_secret_*` và coi đã hoạt động: tài liệu Supabase yêu cầu new API key qua header `apikey`. Cần sửa/tích hợp và thử upload trước khi chuyển loại key. Mọi credential đặc quyền chỉ nằm ở backend. Xem [Supabase API keys](https://supabase.com/docs/guides/getting-started/api-keys).

`UploadController` hiện trả **HTTP 200** cả khi file không hợp lệ hoặc upload thất bại; phải kiểm tra `statusCode`, `code`, `data` trong JSON. Đây là điểm cần thống nhất HTTP contract trong code, không dùng HTTP 200 đơn lẻ làm tiêu chí thành công.

`resolvePhotoUrl` giữ nguyên URL http(s); với path tương đối, ghép origin của `VITE_API_BASE_URL`, hoặc origin frontend khi base `/api`. Dev proxy chuyển `/uploads` sang API. Nhánh đĩa trên container cloud chưa có cấu hình volume bền trong repo, không coi đó là kho ảnh production đã nghiệm thu.

## 5. Render và Vercel theo cấu hình repo

| Cấu hình | Giá trị trong checkout |
| --- | --- |
| Render runtime/context | Docker / `marixa_aspnet` |
| Render Dockerfile | `marixa_aspnet/Dockerfile` |
| Render service name / plan | `marixa-api` / `standard` trong Blueprint; không phải xác nhận plan live |
| Render health check / auto deploy | `/swagger` / `autoDeploy: true` |
| Container listener | HTTP trên biến `PORT`, fallback `10000`; HTTPS phía reverse proxy |
| Vercel root / build / output | `marixa_react` / `npm run build` / `dist` |
| Vercel rewrite | Mọi path không bắt đầu `assets/` → `/index.html`; không có proxy API trong `vercel.json` |

Frontend cloud cần `VITE_API_BASE_URL` tuyệt đối và đúng `/api`; nếu thiếu, `/api` đi vào hosting SPA. Vite proxy chỉ hoạt động khi chạy dev server. Thay env build frontend cần deployment mới; xem [Vercel environment variables](https://vercel.com/docs/environment-variables).

CORS hiện cho HTTP localhost/loopback, danh sách hardcoded trong `ProductionOrigins`, mọi origin kết thúc `.vercel.app` và `Cors:AllowedOrigins`. Đây là allowlist rộng cho preview; CORS không thay thế xác thực/phân quyền. Domain custom cần được cấu hình rõ. Token sai → 401; đúng token nhưng thiếu role → 403. Lỗi CORS là trình duyệt chặn đọc phản hồi do header/origin, không đồng nghĩa mọi 403 là CORS.

## 6. Chẩn đoán theo bằng chứng

| Triệu chứng | Kiểm tra trước / hướng xử lý |
| --- | --- |
| Swagger mở được, API nghiệp vụ lỗi | Log DDL/schema sync/seeder và query DB; startup có thể tiếp tục sau warning. |
| DB authentication/connection lỗi | Provider, đúng host/port/user và credential của project; không in connection string vào log. |
| Thiếu bảng/cột trên PostgreSQL | So sánh EF model với schema thực; tạo script đúng kiến trúc, không chạy SQL cũ hoặc xóa DB. |
| Ảnh trả `/uploads/...` dù muốn dùng Supabase | Kiểm tra binding `SUPABASE__SERVICEKEY` và URL; tên `SUPABASE__SERVICE_KEY` cũ không khớp code. |
| `UPLOAD_FAILED` nhưng HTTP 200 | Đọc body và log backend đã che secret; kiểm tra credential/header, bucket, policy và giới hạn file. |
| Web nhận HTML thay JSON | Kiểm tra API base URL, `/api` và build env; Vercel hiện chỉ có SPA rewrite. |
| 401 / 403 | Kiểm tra JWT issuer/audience/expiry, role và quyền sở hữu; chẩn đoán CORS riêng qua Network/preflight. |
| Ca làm đổi sau restart | Đối chiếu `AttendanceRuleSeeder`; hành vi ghi lại rule mặc định vẫn tồn tại. |

## 7. Hợp đồng bàn giao / Handoff contract

Codex / Agnes must preserve the distinction between repository configuration and live infrastructure. Use `SUPABASE__SERVICEKEY` for the current configuration reader. Storage API-key header compatibility and PostgreSQL SSL normalization remain implementation issues. Startup schema sync is additive only; do not replay legacy Supabase migrations as EF migrations. Check response bodies, database behavior and upload results before marking deployment acceptance complete.
