-- ASP.NET/EF tables only. Not the obsolete snake_case Next.js schema.
-- Additive upgrade; review a backup and the existing schema before running.
-- The .NET connection must be the table owner or have an explicit backend RLS policy.
BEGIN;
SELECT pg_advisory_xact_lock(hashtext('marixa:attendance-periods:20261017'));

CREATE TABLE IF NOT EXISTS public."AttendancePeriods" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "YearMonth" integer NOT NULL,
    "State" integer NOT NULL,
    "CurrentVersion" integer NOT NULL,
    "ReviewedBy" uuid NULL,
    "ReviewedAt" timestamp with time zone NULL,
    "ReviewNote" character varying(1000) NULL,
    "LockedAt" timestamp with time zone NULL,
    "CreatedBy" text NULL,
    "LastUpdatedBy" text NULL,
    "DeletedBy" text NULL,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone NULL
);
CREATE TABLE IF NOT EXISTS public."AttendancePeriodSnapshots" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "PeriodId" uuid NOT NULL REFERENCES public."AttendancePeriods" ("Id") ON DELETE RESTRICT,
    "Version" integer NOT NULL,
    "PayloadJson" text NOT NULL,
    "Sha256" character varying(64) NOT NULL,
    "CreatedBy" text NULL,
    "LastUpdatedBy" text NULL,
    "DeletedBy" text NULL,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone NULL
);
CREATE TABLE IF NOT EXISTS public."AttendancePeriodEvents" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "PeriodId" uuid NOT NULL REFERENCES public."AttendancePeriods" ("Id") ON DELETE RESTRICT,
    "Action" character varying(40) NOT NULL,
    "ActorUserId" uuid NULL,
    "ActorName" character varying(200) NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    "CreatedBy" text NULL,
    "LastUpdatedBy" text NULL,
    "DeletedBy" text NULL,
    "CreatedTime" timestamp with time zone NOT NULL,
    "LastUpdatedTime" timestamp with time zone NOT NULL,
    "DeletedTime" timestamp with time zone NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_AttendancePeriods_YearMonth"
    ON public."AttendancePeriods" ("YearMonth");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_AttendancePeriodSnapshots_PeriodId_Version"
    ON public."AttendancePeriodSnapshots" ("PeriodId", "Version");
CREATE INDEX IF NOT EXISTS "IX_AttendancePeriodEvents_PeriodId_CreatedTime"
    ON public."AttendancePeriodEvents" ("PeriodId", "CreatedTime");

-- These personnel records are served by the authenticated .NET API.
-- Supabase Data API roles must not read/write them directly.
ALTER TABLE public."AttendancePeriods" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."AttendancePeriodSnapshots" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."AttendancePeriodEvents" ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON public."AttendancePeriods", public."AttendancePeriodSnapshots", public."AttendancePeriodEvents" FROM PUBLIC;
DO $hardening$
DECLARE role_name text;
BEGIN
    FOREACH role_name IN ARRAY ARRAY['anon', 'authenticated'] LOOP
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
            EXECUTE format('REVOKE ALL ON public."AttendancePeriods", public."AttendancePeriodSnapshots", public."AttendancePeriodEvents" FROM %I', role_name);
        END IF;
    END LOOP;
END;
$hardening$;
COMMIT;
