# Marixa working agreement

Read docs/00-README.md, docs/PROJECT-INTAKE.md, docs/01-plan.md and the current worklog before edits. The human user's active instruction takes precedence. Backend changes also follow marixa_aspnet/AGENTS.md; documentation follows docs/AGENTS.md.

- Inspect current Git/source and preserve user changes. Maintain relevant docs with behavior changes; keep CURRENT/TARGET/NOT VERIFIED separate.
- Preserve ASP.NET Identity/JWT, React/Vite, SQL Server/PostgreSQL, one active company rule and public attendance photos. HR documents remain private.
- Follow the October 17 business decisions for separate approved OT, configured breaks, self-review, approved leave cancellation and effective dates. Ask about missing business policy rather than guessing.
- Use meaningful regression/provider tests. Never point tests/migrations at the owner's live DB or bucket without authorization for that action. Push authorization does not imply deploy or destructive data migration.
- Do not bypass SaveChanges guards with bulk writes/raw SQL. Preserve old migration bodies and immutable history. Keep credentials and real employee files outside Git.
- Report actual verification counts, skipped scopes and remaining risks. Follow docs/18-release-checklist.md before rollout.

- Configuration changes follow docs/CONFIGURATION-STANDARD.md and deploy/project.json. Keep safe Production JSON separate from local secrets; maintain configuration checks and update worklog with actual verification.
