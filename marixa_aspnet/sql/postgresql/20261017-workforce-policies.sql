-- Run after the attendance-period additive upgrade and a verified backup.
-- No automatic correction of past approvals or recalculation of stored snapshots.
BEGIN;
SELECT pg_advisory_xact_lock(hashtext('marixa:workforce-policies:20261017'));
ALTER TABLE public."AttendanceRules" ADD COLUMN IF NOT EXISTS "BreakStartTime" time without time zone NOT NULL DEFAULT TIME '12:00';
ALTER TABLE public."Attendances" ALTER COLUMN "ActualHours" TYPE numeric(10,2) USING "ActualHours"::numeric(10,2);
ALTER TABLE public."EmployeeHandovers" ADD COLUMN IF NOT EXISTS "AppliedAt" timestamp with time zone NULL;
ALTER TABLE public."LeaveRequests" ADD COLUMN IF NOT EXISTS "CancelledBy" uuid NULL;
ALTER TABLE public."LeaveRequests" ADD COLUMN IF NOT EXISTS "CancelledAt" timestamp with time zone NULL;
ALTER TABLE public."LeaveRequests" ADD COLUMN IF NOT EXISTS "CancellationReason" character varying(500) NULL;
DO $preflight$
BEGIN
    IF EXISTS (SELECT 1 FROM public."LeaveLedger" WHERE "LeaveRequestId" IS NOT NULL AND "EntryType" IN (3,4)
        GROUP BY "LeaveRequestId", "Year", "EntryType" HAVING count(*) > 1) THEN
        RAISE EXCEPTION 'Duplicate leave deduction/refund history. Reconcile with an audit before applying the unique index.';
    END IF;
END;
$preflight$;
CREATE UNIQUE INDEX IF NOT EXISTS "UX_LeaveLedger_Request_Year_Type"
    ON public."LeaveLedger" ("LeaveRequestId", "Year", "EntryType")
    WHERE "LeaveRequestId" IS NOT NULL AND "EntryType" IN (3,4);
COMMIT;
