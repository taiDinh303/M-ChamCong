# SQL kiến trúc Next.js cũ

> Cập nhật 2026-10-17. LEGACY — chỉ giữ để đối chiếu/khôi phục có chỉ định.

Các migration ở đây dùng schema snake_case, Supabase Auth/RPC/RLS của kiến trúc đã bỏ. Chúng không phải migration của ASP.NET Identity/EF hiện tại và không chứng minh backend .NET có những chức năng tương ứng. Không áp vào database Marixa đang chạy.

Đường nâng schema hiện hành: [23-data-migration](../../docs/23-data-migration.md); SQL Server dùng M.Repositories/Migrations, PostgreSQL dùng script additive đã review dưới sql/postgresql. Script apply-migrations.ps1 mặc định từ chối chạy SQL cũ; cờ LegacyNextSchema chỉ phục vụ nhiệm vụ khôi phục legacy đã được giao rõ ràng.
