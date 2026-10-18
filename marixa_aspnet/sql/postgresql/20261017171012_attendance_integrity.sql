-- Restore the two uniqueness guards used by the current EF model.
-- Apply after a verified backup and the attendance-period/workforce-policy upgrades.
-- Existing rows are never merged, deleted, or rewritten by this migration.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtext('marixa:attendance-integrity:20261017'));
LOCK TABLE public."AttendanceRules", public."Attendances" IN SHARE ROW EXCLUSIVE MODE;

DO $preflight$
BEGIN
    IF (SELECT count(*) FROM public."AttendanceRules" WHERE "IsActive" = TRUE AND "DeletedTime" IS NULL) > 1 THEN
        RAISE EXCEPTION 'Multiple active attendance rules. Reconcile with an audit before applying the unique index.';
    END IF;
    IF EXISTS (
        SELECT 1 FROM public."Attendances" WHERE "DeletedTime" IS NULL
        GROUP BY "EmployeeId", "AttendanceDate" HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'Duplicate active attendance days. Reconcile with an audit before applying the unique index.';
    END IF;
END;
$preflight$;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_AttendanceRules_Active"
    ON public."AttendanceRules" ("IsActive")
    WHERE "IsActive" = TRUE AND "DeletedTime" IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Attendances_Employee_Date"
    ON public."Attendances" ("EmployeeId", "AttendanceDate")
    WHERE "DeletedTime" IS NULL;
COMMIT;
