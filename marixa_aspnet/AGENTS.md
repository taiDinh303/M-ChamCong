# Backend working agreement

Read `../docs/00-README.md`, `../docs/01-plan.md` and `ARCHITECTURE.md` before changing this backend. Owner instructions and the active task define scope.

- Preserve ASP.NET Core 8 + Identity/JWT, SQL Server/PostgreSQL support, existing HTTP routes and payload names. Do not import booking/payment features from the reference project.
- Keep `M.Core` independent of HTTP/EF. Contracts must not reference `M.Repositories`. Business services depend on contracts; persistence registration belongs to `M.Repositories`, composition to `M.API`.
- Organize controllers/services by feature. Use `ICurrentUser`, `TimeProvider` and existing shared guards. Never trust a client-supplied employee/reviewer/user ID without checking ownership and role.
- Use inheritance for shared behavior (`BaseService`, `BaseApiController`, `BaseEntity`); use interfaces/implementations for replaceable infrastructure. Do not build a generic CRUD framework or add empty layers just to demonstrate OOP.
- Keep secrets outside Git. Production bootstrap/migration/seeding must be explicit. Do not execute tests or migrations against the owner's live database unless that action is part of the authorized task.
- Add focused regression tests for behavior/security changes. M.Tests uses isolated SQLite/fake HTTP, SQL Server LocalDB fixtures and optional loopback PostgreSQL fixtures. Record provider, pass/fail/skip and CI evidence; never use a live DB.
- Follow October 17 policy decisions in docs/01-plan.md. Keep normal hours separate from approved OT, configured breaks, immutable raw logs/snapshots/ledger, approved cancellation audit and effective-date processing. Do not bypass guards with ExecuteUpdate/raw SQL. Job AppliedAt must only mark a committed application.
- From the repo root: `dotnet build marixa_aspnet/M.BE.sln -c Release -warnaserror`, then `dotnet test marixa_aspnet/M.BE.sln -c Release --no-build`.
- From `marixa_aspnet`: restore the local tool with `dotnet tool restore`; inspect migration SQL before applying it. Keep historical migration IDs/Up/Down operations intact. New uniqueness constraints require duplicate-data preflight and a tested rollback.
- Update the relevant docs and `../docs/worklog.md` with actual evidence. Record unresolved business rules and unverified runtime work instead of calling them complete.
