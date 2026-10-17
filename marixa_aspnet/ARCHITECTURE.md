# Kiến trúc backend Marixa hiện hành

> Cập nhật 2026-10-17. ASP.NET Core 8/Identity JWT/EF Core; không dùng Next.js hoặc Supabase Auth làm đường chạy.

## Phân lớp và cấu trúc

| Project / folder | Trách nhiệm |
| --- | --- |
| M.Core | BaseEntity/exception/pagination/BusinessTime, actor contract/options; không HTTP/EF |
| M.Contract.Repositories | Entity/Identity models và IUnitOfWork/IGenericRepository; không tham chiếu implementation persistence |
| M.ModelViews | DTO/request/response và model file transport-neutral |
| M.Contract.Services | Interface nghiệp vụ, storage/export/security |
| M.Repositories | EF context/model/migrations, repository/UoW, SaveChanges period/audit guards |
| M.Services | Business services theo Identity/HumanResources/Compensation/Timekeeping/Leave/Work/Audit/Shared; mappings/security |
| M.API | Controller theo feature, Configuration/Bootstrap/Filters/Middleware/Storage/Export/Assets; composition root |
| M.Tests | xUnit unit, HTTP/SQLite, SQL Server LocalDB/PostgreSQL fixtures và export QA |

Controller chuyển HTTP forms sang DTO/UploadedDocument, gọi service, trả JSON/file. Service dùng actor/clock/UoW/interfaces; M.API đăng ký adapters. M.Services còn dùng EF query extensions/IQueryable; đây là kiến trúc layered thực dụng, chưa phải domain model thuần hoặc DDD hoàn chỉnh.

## Bốn tính chất hướng đối tượng

Trừu tượng: ICurrentUser, IUnitOfWork, storage/export/service interfaces. Kế thừa: BaseEntity bảo trì audit, BaseService chia sẻ actor/clock/guards/transaction, BaseApiController envelope, PhotoStorageBase validation. Đóng gói: private repository cache/transaction, private paths, AttendancePeriod factory/private setters/Review/Lock/Reopen. Đa hình: local/Supabase photo adapters, private storage mock, export interface, TimeProvider test. Không ép mọi class phải có lớp con; CRUD entities cũ còn public setters và một số response object cần giảm dần.

## Dữ liệu và transaction

Một AttendanceRule active; BreakStartTime/BreakMinutes cấu hình nghỉ, công thường và OT approved tách riêng theo 01. Identity/password/role do .NET quản lý; Supabase chỉ DB/Storage. Raw AttendanceLog không sửa/xóa; correction có audit. Period Open→Reviewed→Locked, snapshot/event append-only; SaveChanges kiểm mọi tháng gốc/mới bị tác động. Rule đổi làm mất review kỳ chưa khóa; snapshot cũ không tính lại.

UoW dùng Serializable cho các luồng quan trọng; snapshot/source/job cùng DB transaction. Guard có tác dụng với SaveChanges, không chống DBA/raw SQL hoặc bulk bypass. DB unique constraints bảo vệ ngày công/rule/period/version/ledger. File và DB không có distributed transaction; write batch bù khi rollback, crash vẫn cần đối soát/backup.

Report/handover ở App_Data ngoài web root; legacy static path bị chặn, đọc cũ qua API có quyền. Ảnh public giữ quyết định. Worker ngày hiệu lực dùng AppliedAt, retry từng item/catch-up; không chạy trong test host. Các contract cũ chưa đồng nhất CancellationToken và Manager scope; xem rủi ro 34.

Tài liệu tiếp quản: [README](README.md), [AGENTS](AGENTS.md), [review](BACKEND_REVIEW.md), [API](../docs/15-api-contract.md), [migration](../docs/23-data-migration.md), [job](../docs/27-offline-realtime-jobs.md).
