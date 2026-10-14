# 12— Glossary / Thuật ngữ hệ thống chấm công Marixa

> Tác giả & chủ tài liệu: **Đinh Văn Tài**. Cập nhật: 14/10/2026.
> Bảng thuật ngữ dùng chung cho AI/dev đọc dự án. Khi có thuật ngữ mới → thêm vào đây.

| Thuật ngữ | Ý nghĩa | Ghi chú / nơi dùng |
| --- | --- | --- |
| **Ca làm (shift)** | Khoảng giờ làm việc chung cho toàn công ty | V1 dùng **1 `AttendanceRule` active** (seed 08:00–17:00). Không dùng "version + ngày hiệu lực" (đã rút 12/10). |
| **Kỳ công** | Chu kỳ tính công (V1 = **tháng dương lịch**). Trạng thái: mở → kiểm tra (HR) → khóa (Admin). | Chưa có luồng khóa kỳ + snapshot (xem `worklog.md` §3). |
| **Snapshot (kỳ khóa)** | Bản ghi cố định của kỳ đã khóa — không đổi khi có event sau đó | Chưa làm (chỉ có "bản tạm" kỳ mở). |
| **Attendance (1 bản ghi/ngày)** | Trạng thái công của 1 NV trong 1 ngày (1 bản ghi duy nhất, chặn trùng `EmployeeId + AttendanceDate`). | |
| **AttendanceLog** | Mỗi lần chấm (vào/ra): log time, phương pháp, ảnh, ghi chú | Nguyên bản, không sửa khi điều chỉnh. |
| **Sửa công (AttendanceCorrection)** | Đề xuất mốc đúng + lý do; khi duyệt tạo record riêng, giữ event gốc | |
| **Sổ phép (LeaveLedger)** | Giao dịch cấp/trừ/hoàn/điều chỉnh phép; số dư = tổng giao dịch | Idempotent theo đơn/phiên bản. |
| **Tăng ca (OvertimeRequest)** | Đơn tăng ca: ngày, khoảng giờ, lý do; duyệt trước khi tính | |
| **Idempotent** | Chạy nhiều lần cho kết quả giống 1 lần (không nhân đôi dữ liệu) | Seeder, ledger phép, DDL boot. |
| **Session pooler** | Endpoint kết nối PostgreSQL của Supabase (user `postgres.<REF>`, `sslmode=require`) | Dùng làm `CONNECTIONSTRINGS__MYCNN` cho prod. |
| **DB linh hoạt** | Cơ chế chạy được cả **SQL Server (dev)** và **PostgreSQL (prod)** qua `DatabaseConfig.cs` + env `DATABASE__PROVIDER` | |
| **Soft delete** | Đánh dấu `DeletedTime` thay vì vật lý xóa | Hầu hết bảng nghiệp vụ. |
| **Bucket public `marixa-photos`** | Chứa ảnh chấm công trên Supabase Storage (prod, public); dev fallback `wwwroot/uploads` | Quyết định 12/10: **giữ public** (chưa chuyển private). |
| **Module (frontend)** | Nhóm trang/quyền trong `marixa_react/src/constants/modules.js` (`access: "user"|"admin"` hoặc `roles:[...]`) | Nguồn quyền giao diện; backend vẫn là nguồn quyền thật. |
| **Role (4 core)** | `Admin`, `Manager`, `HR`, `Employee` — đã map quyền module | Seed trong `RoleSeeder.cs`. |
| **Role nghiệp vụ (3, seed chưa map)** | `Accountant`, `Sale`, `Marketing` — đã seed 12/10, **chưa gán quyền module** | Cần thêm vào `MODULES` + `[Authorize(Roles=...)]` khi dùng. |
| **Offline/IndexedDB queue** | Hàng đợi chấm công khi mất mạng (đồng bộ trễ) | Chưa làm (V1 = chấm online). |
| **Export kỳ công (Excel/PDF)** | Xuất bảng công (Excel) + đơn nghỉ (PDF) theo quyền & phiên bản | Chưa làm (hiện chỉ có upload báo cáo NV). |
| **Deploy blueprint** | Cấu hình Render đọc `render.yaml` (repo root) để tạo web service Docker `marixa-api` | |
| **SPA fallback / rewrite** | Mọi route không `/assets` → `index.html` (React Router) | `marixa_react/vercel.json` (prod) + `MapFallbackToFile` (backend). |
