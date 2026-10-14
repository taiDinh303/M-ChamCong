2 — Thiết kế dữ liệu hệ thống chấm công Marixa

> Tác giả & chủ tài liệu: **Đinh Văn Tài**.
> Hệ thống dùng **Entity Framework Core** (context: `M.Repositories/Context/DatabaseContext.cs`), linh hoạt giữa **SQL Server** (dev local) và **PostgreSQL/Supabase** (prod). Danh sách bảng thật nằm trong `M.Contract.Repositories/Entities/`.

## 0. Cách đọc (dành cho AI)

- Mỗi file entity trong `marixa_aspnet/M.Contract.Repositories/Entities/*.cs` = một bảng. Model view tương ứng nằm ở `M.ModelViews/*ModelView.cs`, mapping EF↔VM nằm ở `M.Services/Mapping/*Mapping.cs`.
- Cơ chế DB linh hoạt (dev/production) xem `08-deploy-detail.md`.
- Khởi tạo schema: Postgres chạy DDL idempotent khi boot + `PostgresSchemaSync.EnsureColumnsAsync`; SQL Server dev chạy `MigrateAsync`.

## 1. Nguyên tắc dữ liệu

- Thời điểm lưu `timestamptz`/UTC; hiển thị & xác định ngày công theo `Asia/Ho_Chi_Minh`. Input model bị chuẩn hóa UTC qua `M.API/Filters/DateTimeUtcKindFilter.cs`.
- Soft delete: các bảng nghiệp vụ dùng `DeletedTime` (không vật lý xóa ngay), trừ trường hợp admin xóa hẳn.
- Event chấm công gốc bất biến khi sửa; điều chỉnh/điều chỉnh lại là record riêng (`AttendanceCorrection`), audit ghi lại trước/sau.
- Ảnh chấm công lưu trong **Supabase Storage** (bucket public `marixa-photos`) khi prod; DB chỉ lưu URL/path, không lưu blob.

## 2. Các bảng chính (EF Core, theo thư mục Entities)

| Bảng / Entity | Vai trò | Ghi chú |
| --- | --- | --- |
| `Employee` | Hồ sơ nhân viên: mã, họ tên, email, sđt, phòng ban, vị trí, ngày vào, trạng thái | Hồ sơ độc lập với tài khoản Auth; không xóa khi nghỉ việc. |
| `ApplicationUser` (Identity) | Tài khoản đăng nhập (ASP.NET Identity) | Liên kết `Employee` bằng mã/sđt; role gắn theo `ApplicationUserRole`. |
| `ApplicationRole` | Role: `Admin`, `Manager`, `HR`, `Employee` | Seed idempotent ở `M.API/Seed/RoleSeeder.cs`. |
| `Department` | Phòng ban | Cấp trên `Employee`. |
| `Position` | Vị trí/CB | Liên kết `Employee`. |
| `AttendanceRule` | **Ca làm chung**: giờ bắt đầu/kết thúc, nghỉ giữa ca (phút), cờ active | Một quy tắc active cho toàn công ty; thay đổi có audit. |
| `Shift` | Ca/turn lịch theo ngày | Dùng khi cần ca riêng (phục vụ bảng lịch, chưa gán per-nv trong V1). |
| `EmployeeShift` | Gán ca theo nhân viên/ngày (nếu có) | |
| `Attendance` | Bản ghi công **theo ngày** (1 nv/ngày): status, approval, giờ thực tế, tăng ca, ảnh vào/ra, người duyệt | Chấm vào/ra sinh `AttendanceLog`. |
| `AttendanceLog` | Mỗi lần chấm (vào/ra): log time, phương pháp, ảnh, ghi chú | Nguyên bản; không sửa khi điều chỉnh. |
| `AttendanceCorrection` | Yêu cầu/điều chỉnh công: mốc đề xuất, lý do, trạng thái, người duyệt | Tạo record riêng, giữ event gốc. |
| `AuditLog` | Nhật ký: ai, hành động, entity, before/after, lý do | |
| `OfficeLocation` | Tọa độ/bán kính văn phòng (tham chiếu GPS) | |
| `HolidayCalendar` | Ngày lễ/ngày nghỉ/làm bù | |
| `LeaveType` | Loại nghỉ (cờ: có trừ phép năm hay không) | |
| `LeaveRequest` | Đơn nghỉ: loại, khoảng ngày, lý do, trạng thái, người duyệt | |
| `LeaveLedger` | Sổ phép: giao dịch cấp/trừ/hoàn/điều chỉnh, số ngày | Số dư = tổng giao dịch. |
| `OvertimeRequest` | Đơn tăng ca: ngày, khoảng giờ, lý do, trạng thái | |
| `Payroll` | Bảng lương kỳ | |
| `SalaryGroup` | Nhóm lương | |
| `EmployeeSalary` | Lương theo nhân viên | |
| `EmployeeBankAccount` / `Bank` | Tài khoản & ngân hàng | |
| `EmployeeContract` | Hợp đồng | |
| `EmployeeInsurance` | BHXH/thuế | |
| `EmployeeDependent` | Thân nhân (phụ thuộc) | |
| `EmployeePromotion` | Thăng chức | |
| `EmployeeHandover` | Bàn giao khi nghỉ việc | |
| `EmployeeReport` | Báo cáo của nhân viên (theo kỳ) | |
| `WorkProject` / `WorkProjectMember` / `WorkTask` / `WorkPerformance` | Công việc: dự án, thành viên, task, hiệu suất | |
| `ActivationCode` | Mã kích hoạt tài khoản (quy trình bàn giao NV) | |

> Các migration EF (T-SQL, SQL Server) nằm ở `M.Repositories/Migrations/`. Khi chạy Postgres, code dùng `GenerateCreateScript()` + schema sync thay vì chạy migration T-SQL (không tương thích Postgres) — xem `M.API/Program.cs`.

## 3. Ràng buộc toàn vẹn cần đảm bảo

- Một `Attendance` mỗi nhân viên mỗi ngày (chặn trùng `EmployeeId + AttendanceDate`).
- Chấm lại cùng loại trong cửa sổ ngắn được giữ nguyên (không nhân đôi log).
- Admin: luôn giữ tối thiểu 1 tài khoản admin hoạt động (chặn khóa admin cuối).
- Duyệt: người duyệt ≠ người gửi đơn (trừ khi quy trình cho phép); không sửa bản ghi đã khóa/duyệt.
- Ledger phép: giao dịch idempotent theo đơn/phiên bản, không trừ/hoàn trùng.

## 4. Ảnh chấm công & lưu trữ

- Bucket **public** `marixa-photos` trên Supabase (config: `M.API/SupabaseStorageService.cs`), đường dẫn `attendance/{checkin|checkout}/{yyyyMM}/{guid}{ext}`, không chứa họ tên.
- Dev local: lưu đĩa `wwwroot/uploads/attendance/{kind}/{yyyyMM}/{file}`, serve qua `/uploads` (StaticFiles).
- DB chỉ lưu URL; không lưu base64/blob vào Postgres.

## 5. Ghi chú chuyển đổi (đồ án)

> **Quyết định 12/10/2026:** Thư mục `marixa_aspnet/web` (kiến trúc **Next.js + Supabase Auth + RLS + bucket private**) do đồng đội thiết kế đã bị **đình chỉ (decommission)** — không thuộc đường chạy; chỉ giữ trong Git để đối chiếu, chờ xoá khi chủ dự án xác nhận.
> **EN (AI note):** The `marixa_aspnet/web` Next.js design (Supabase Auth + RLS + private bucket) is **decommissioned**; the live system is ASP.NET 8 + React 19 as above. It is kept in Git only for reference and will be deleted pending owner confirmation.

Tài liệu gốc (của đồng đội) từng đề xuất kiến trúc **Supabase Auth + RLS + bucket private**. Code đang chạy thực tế dùng **ASP.NET Identity + JWT** và **Supabase Storage bucket public** cho ảnh, DB **Supabase Postgres (session pooler)**.

Xem quy tắc nghiệp vụ ở [02-business-analysis.md](02-business-analysis.md) và luồng ở [05-workflow.md](05-workflow.md).
