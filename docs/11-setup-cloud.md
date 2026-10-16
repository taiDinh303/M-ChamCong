# 11 — Thiết lập project cloud và bàn giao vận hành

> Chủ tài liệu: **Đinh Văn Tài**. Đối chiếu tài liệu nhà cung cấp và cấu hình repo: **16/10/2026**.
> Hướng dẫn thao tác, chưa thực hiện trên Dashboard trong lần cập nhật này. Tên menu có thể thay đổi.

**CURRENT** = cấu hình đã đọc trong repo; **TARGET** = quy trình thiết lập/nghiệm thu; **NOT VERIFIED** = trạng thái hạ tầng live. Quy ước đầy đủ ở [00-README.md](00-README.md).

## 1. Trước khi cấu hình

- Đọc [00-README.md](00-README.md) để xác định instance đã được chủ dự án cung cấp; **tái sử dụng một project Supabase hiện hành**, không tạo lại cặp test/prod theo hướng dẫn cũ.
- Đọc [08-deploy-detail.md](08-deploy-detail.md) để lấy **tên biến chính xác**, giới hạn schema sync, seeder và tương thích Storage; file này không lặp bảng env.
- API .NET dùng Identity/JWT. **Không cần bật Supabase Email/Password Auth** để triển khai ứng dụng hiện tại. Next.js đã xóa.
- Chọn đúng repo/branch/commit và project trong Dashboard. Thay đổi project, phương thức Auth, bucket public/private hoặc kiến trúc môi trường phải có quyết định được ghi theo [01-plan.md](01-plan.md).

## 2. Supabase: Database và Storage

1. Mở project đã xác định tại `00`; đối chiếu project ref trước khi sao chép URL/credential. Không tạo thêm project trong quy trình này.
2. Tại **Connect**, lấy connection string session pooler phù hợp backend. Copy đúng host/port/user; giữ credential trong nơi lưu secret của API. Xem [Supabase kết nối PostgreSQL](https://supabase.com/docs/guides/database/connecting-to-postgres).
3. Kiểm tra bucket **public `marixa-photos`** khớp code. Người có URL public có thể đọc ảnh; giữ quyết định đã chốt cho đến khi có thay đổi được ghi nhận. Xem [bucket access model](https://supabase.com/docs/guides/storage/buckets/fundamentals).
4. Chuẩn bị credential Storage phía server và đặt bằng tên `SUPABASE__SERVICEKEY` theo `08`. Code hiện gửi Bearer và chưa tích hợp header cho new secret key; không thay loại credential theo tên menu rồi coi upload đã được xác minh.
5. Kiểm tra exposure/grants/RLS của các bảng qua Data API theo phạm vi sử dụng. Ứng dụng hiện gọi PostgreSQL từ .NET; JWT/role .NET không tự tương đương `auth.uid()` của Supabase. Không áp lại policy/RPC kiến trúc cũ để chữa lỗi .NET.

Không ghi key, mật khẩu DB, connection string đầy đủ hoặc token vào Git/worklog/ảnh chụp bàn giao. React hiện không cần khóa Supabase để đăng nhập hoặc upload ảnh qua API .NET.

## 3. Render: API Docker

1. Chọn service/Blueprint hiện có; với lần thiết lập mới được yêu cầu, dùng `render.yaml` ở root theo [Render Blueprint reference](https://render.com/docs/blueprint-spec).
2. Đối chiếu Dockerfile `marixa_aspnet/Dockerfile` và context `marixa_aspnet`. Blueprint hiện có `plan: standard`, `autoDeploy: true`; xác nhận plan/auto deploy phù hợp trước khi áp cấu hình, không coi đây là trạng thái hosting đã xác minh.
3. Điền biến API theo bảng `08` §2. Kiểm tra riêng tên `SUPABASE__SERVICEKEY`, DB của đúng project, JWT key riêng và cấu hình bootstrap admin.
4. Đánh giá seeder chạy cả Production và startup DDL trước khi khởi động bản mới. Lập phương án sao lưu/khôi phục dữ liệu ảnh hưởng; startup có thể ghi lại ca mặc định và thêm tài khoản mẫu.
5. Health check cấu hình hiện là `/swagger`; đọc log và kiểm tra query nghiệp vụ riêng, vì lỗi DB/seeder có thể chỉ được ghi warning.
6. Ghi URL service và commit triển khai vào worklog sau khi thực hiện. Cập nhật bảng hosting duy nhất tại `00` nếu instance thay đổi.

Khi thực hiện công việc triển khai đã được giao, kiểm tra tác động cấu hình rồi ghi kết quả thực. Lần chuẩn hóa tài liệu này chưa tạo/nâng gói hoặc thay cấu hình cloud.

## 4. Vercel: React/Vite SPA

1. Chọn project hiện hành; khi thiết lập mới theo yêu cầu, import đúng Git repository.
2. Đặt Root Directory **`marixa_react`**, Build Command **`npm run build`**, Output Directory **`dist`**; đối chiếu `vercel.json`. Dùng tích hợp Vite phù hợp theo [Vercel Vite guide](https://vercel.com/docs/frameworks/frontend/vite); hướng dẫn cũ ép preset `Other` không phải yêu cầu của repo.
3. Đặt `VITE_API_BASE_URL=https://<API_HOST>/api` trong môi trường cần build. Không đưa DB/JWT/Storage credential vào env frontend.
4. Preview phải được cấu hình có chủ đích: nếu dùng cùng URL API thì dùng cùng dữ liệu; không có DB test riêng chỉ vì URL frontend là preview.
5. Đổi build env rồi tạo deployment mới; giá trị không tự cập nhật deployment cũ. Xem [Vercel environment variables](https://vercel.com/docs/environment-variables).
6. Mở trang login và refresh route con để kiểm tra SPA rewrite. `vercel.json` hiện không proxy `/api`; thiếu API base tuyệt đối có thể trả HTML thay JSON.

## 5. Nghiệm thu và vận hành

Chạy checklist [07-deploy-overview.md](07-deploy-overview.md) cùng các ca [06-testing.md](06-testing.md). Smoke test hiện tại: đăng nhập → upload ảnh → chấm công → lịch sử → tạo/duyệt đơn theo quyền. Kiểm tra cả HTTP status lẫn body; ảnh cloud cần URL Supabase khi đúng cấu hình. Khóa kỳ/snapshot/XLSX bảng công khóa/PDF đơn nghỉ là tiêu chí tương lai, không phải bước smoke test đang có.

Sao lưu cần bao phủ **database và nội dung file Storage**; thử restore với dữ liệu giả và ghi bằng chứng trước khi coi phương án khôi phục hoàn tất. Không suy ra file ảnh đã được backup từ một bản backup DB. Xem [Supabase backups](https://supabase.com/docs/guides/platform/backups).

Hạn mức, backup đi kèm, thời gian ngủ/tạm dừng, giá và điều kiện gói phụ thuộc nhà cung cấp. Kiểm tra trực tiếp nguồn chính thức khi vận hành; không dùng các số giá/giới hạn cũ trong lịch sử tài liệu để quyết định.

## 6. Bằng chứng bàn giao

Sau thao tác thực, ghi vào [worklog.md](worklog.md): ngày, commit, môi trường/project, cấu hình **tên biến đã kiểm tra** (không có giá trị secret), kết quả build/startup/query/upload, URL ảnh đã che dữ liệu cá nhân nếu cần, tình trạng backup/restore và các lỗi còn mở.

Nếu chưa kiểm tra được mục nào, ghi **NOT VERIFIED / chưa xác minh**. Không biến snapshot chủ dự án báo ngày 12/10/2026 thành tuyên bố hạ tầng đã được kiểm tra ngày 16/10/2026.

## 7. Contract for Codex / Agnes

Reuse the single owner-designated Supabase project. Authentication belongs to ASP.NET Identity/JWT. Follow `08` for exact environment names and current Storage credential compatibility. Dashboard actions, deploys and database changes require the scope of the active implementation task; this documentation update performs none of them. Record evidence before claiming cloud acceptance.
