# 00 — Index / Bảng chỉ dẫn tài liệu Marixa (bắt đầu đọc ở đây)

> **Tác giả & chủ tài liệu: Đinh Văn Tài.** Cập nhật: 12/10/2026.
>
> **EN (for AI / Agnes):** This file is the single entry point. Read order + quick facts + which doc to open for which task. All docs are bilingual: Vietnamese headings/labels with English annotations for AI. The live system is **ASP.NET 8 (`M.API`) + React 19 (`marixa_react`)** — **not** the old Next.js design (decommissioned 12/10/2026).

## How to use (EN, for AI)
1. Read this file first (5 min).
2. Then `01` (business), `04` (architecture), `worklog` (what's done / not done).
3. Open only the doc that matches your task (table below). Don't read all 10.

## Read order / which file for which task (EN)

| Task | Open |
| --- | --- |
| Who can do what, roles, rules | `01-business-analysis.md` |
| Tables / entities / DB | `02-database-design.md` |
| Business flows (check-in, leave, lock period) | `03-workflow.md` |
| System / routes / API shape | `04-system-architecture.md` |
| Testing | `05-testing-strategy.md` |
| Deploy / config / hosting | `06-deployment-vercel-supabase.md`, `08-database-flexible-deployment.md`, `SETUP_CLOUD.md` |
| UI / colors / screens | `07-ui-design-system.md` |
| **Done / not-done feature matrix + event log** | `worklog.md` |
| Next steps / decisions | `plan.md` |

## Trật tự đọc / file nào cho việc nào (VI)

| Việc | Mở file |
| --- | --- |
| Ai làm gì, role, quy tắc | `01-business-analysis.md` |
| Bảng / entity / DB | `02-database-design.md` |
| Luồng nghiệp vụ (chấm công, nghỉ phép, khóa kỳ) | `03-workflow.md` |
| Kiến trúc / route / API | `04-system-architecture.md` |
| Kiểm thử | `05-testing-strategy.md` |
| Triển khai / config / hosting | `06`, `08`, `SETUP_CLOUD.md` |
| UI / màu / màn hình | `07-ui-design-system.md` |
| **Ma trận đã làm / chưa làm + log sự kiện** | `worklog.md` |
| Việc tiếp theo / quyết định | `plan.md` |

## Quick facts (EN — confirmed 12/10/2026)

- **Stack (live):** ASP.NET Core 8 `M.API` (EF Core, JWT + Identity) · React 19 + Vite `marixa_react` · flexible DB (SQL Server dev / PostgreSQL Supabase prod) · photos via Supabase Storage bucket **public** `marixa-photos` (dev fallback `wwwroot/uploads`).
- **Roles (live):** `Admin`, `Manager`, `HR`, `Employee` (4, mapped to modules). **Seeded 12/10 (in `RoleSeeder.cs`, not yet mapped to modules):** `Accountant`, `Sale`, `Marketing`. See `worklog.md`.
- **Hosting (live, no secrets in docs):**
  | Platform | URL / instance | Plan | Region |
  | --- | --- | --- | --- |
  | Render (API .NET) | `https://marixa001-asp.onrender.com` (swagger: `/swagger`) · service `srv-db4hmnks728c73aq6h90` | Free | — |
  | Vercel (React SPA) | `https://marixa001.vercel.app` · team/project `taiteam/marixa_001` | Hobby | — |
  | Supabase (PG + Storage) | ref `zphqfcwrwitrqdeueonu` | Free | Singapore |
- **Repo:** `github.com/taiDinh303/M-ChamCong` (origin).
- **Decisions 12/10:** photos stay **public**; leave accrual keeps current rule; shared shift stays **simple** (single active `AttendanceRule`; "version + effective-date" proposal withdrawn 12/10); offline/IndexedDB = pending; period lock + snapshot + Excel/PDF export = not built; Next.js `marixa_aspnet/web` **deleted** 12/10; roles `Accountant`/`Sale`/`Marketing` **seeded** in `RoleSeeder` (no module permissions yet); GPS in .NET UI not wired.

## Quy tắc cho AI (EN)
- `worklog.md` is the only file AI may freely edit; specs `01`–`08`, `plan.md`, `SETUP_CLOUD.md` are frozen. To change a spec → write a proposal in `worklog.md`, don't edit the spec file.
- Never commit secrets (DB password, JWT key, Supabase service key, connection string). Docs hold **no** secrets — only IDs/URLs/plans.
- Mark a task "done" only after it is verified. Record day + evidence in `worklog.md`.

## Quy tắc cho AI (VI)
- `worklog.md` là file duy nhất AI được phép sửa tự do; `01`–`08`, `plan.md`, `SETUP_CLOUD.md` là đặc tả. Muốn đổi đặc tả → đề xuất trong `worklog.md`, không sửa thẳng.
- Không commit secret (mật khẩu DB, JWT key, Supabase service key, connection string). Tài liệu chỉ chứa ID/URL/plan, không secret.
- Đánh dấu "xong" chỉ khi đã kiểm chứng; ghi ngày + bằng chứng vào `worklog.md`.
