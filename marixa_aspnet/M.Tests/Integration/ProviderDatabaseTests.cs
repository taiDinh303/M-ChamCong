using M.API.Export;
using M.Contract.Repositories.Entities;
using M.Core.Security;
using M.Repositories.Context;
using M.Repositories.Persistence;
using M.Services.Services.Timekeeping;
using M.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace M.Tests.Integration;

public sealed class ProviderDatabaseTests
{
    [LocalSqlServerFact]
    public async Task SqlServer_migrations_period_transactions_and_constraints_work_on_an_isolated_database()
    {
        var name = "Marixa_Verify_" + Guid.NewGuid().ToString("N");
        const string master = "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=True;Encrypt=False;Pooling=False;Connect Timeout=15";
        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();
        await using (var create = new SqlCommand($"CREATE DATABASE [{name}]", connection)) await create.ExecuteNonQueryAsync();
        try
        {
            var builder = new SqlConnectionStringBuilder(master) { InitialCatalog = name };
            await ExerciseAsync(new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(builder.ConnectionString).Options, migrate: true);
        }
        finally
        {
            Assert.StartsWith("Marixa_Verify_", name);
            await using var drop = new SqlCommand($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    [PostgresFact]
    public async Task PostgreSql_schema_period_transactions_and_constraints_work_on_an_isolated_database()
    {
        var master = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MARIXA_TEST_POSTGRES"));
        if (master.Host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException("Provider tests only accept a loopback PostgreSQL server.");
        master.Pooling = false;
        var name = "Marixa_Verify_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(master.ConnectionString);
        await connection.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection)) await create.ExecuteNonQueryAsync();
        try
        {
            master.Database = name;
            await ExerciseAsync(new DbContextOptionsBuilder<DatabaseContext>().UseNpgsql(master.ConnectionString).Options, migrate: false);
        }
        finally
        {
            Assert.StartsWith("Marixa_Verify_", name);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{name}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task ExerciseAsync(DbContextOptions<DatabaseContext> options, bool migrate)
    {
        var actor = new AdminActor();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-10-17T12:00:00Z"));
        await using var db = new DatabaseContext(options, actor, clock);
        if (migrate)
        {
            await db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync("20261016161225_EnforceSingleActiveAttendanceRule");
            await db.Database.ExecuteSqlRawAsync("INSERT INTO [AttendanceRules] ([Id],[Code],[Name],[StandardHours],[CheckInTime],[CheckOutTime],[LateGraceMinutes],[EarlyLeaveThresholdMinutes],[BreakMinutes],[PhotoRequired],[GpsRequired],[IsActive],[CreatedTime],[LastUpdatedTime]) VALUES (NEWID(),'LEGACY-VERIFY','Legacy rule',8,'08:00','17:00',0,0,60,0,0,0,SYSDATETIMEOFFSET(),SYSDATETIMEOFFSET())");
            await db.Database.MigrateAsync();
            var legacy = await db.AttendanceRules.SingleAsync(r => r.Code == "LEGACY-VERIFY");
            Assert.Equal(new TimeOnly(12, 0), legacy.BreakStartTime);
            db.Remove(legacy); await db.SaveChangesAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
            // Simulate an older ASP.NET database lacking only this additive release.
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"AttendancePeriodSnapshots\", \"AttendancePeriodEvents\", \"AttendancePeriods\"");
            var upgrade = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "20261017-attendance-periods.sql"));
            await db.Database.ExecuteSqlRawAsync(upgrade);
            await db.Database.ExecuteSqlRawAsync(upgrade);
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"AttendanceRules\" DROP COLUMN \"BreakStartTime\"; ALTER TABLE \"EmployeeHandovers\" DROP COLUMN \"AppliedAt\"; ALTER TABLE \"LeaveRequests\" DROP COLUMN \"CancelledBy\", DROP COLUMN \"CancelledAt\", DROP COLUMN \"CancellationReason\"; ALTER TABLE \"Attendances\" ALTER COLUMN \"ActualHours\" TYPE integer USING \"ActualHours\"::integer; DROP INDEX \"UX_LeaveLedger_Request_Year_Type\";");
            var policyUpgrade = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "20261017-workforce-policies.sql"));
            await db.Database.ExecuteSqlRawAsync(policyUpgrade);
            await db.Database.ExecuteSqlRawAsync(policyUpgrade);
            await db.Database.ExecuteSqlRawAsync("DROP INDEX \"UX_AttendanceRules_Active\", \"UX_Attendances_Employee_Date\"");
            var integrityUpgrade = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "20261017171012_attendance_integrity.sql"));
            await db.Database.ExecuteSqlRawAsync(integrityUpgrade);
            await db.Database.ExecuteSqlRawAsync(integrityUpgrade);
            Assert.Equal(3, await db.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM pg_class WHERE relname IN ('AttendancePeriods','AttendancePeriodSnapshots','AttendancePeriodEvents') AND relrowsecurity").SingleAsync());
        }
        var employee = new Employee { EmployeeCode = "VERIFY", GivenName = "Nhân viên", FamilyName = "Thử nghiệm" };
        db.Employees.Add(employee);
        db.AttendanceRules.Add(new AttendanceRule { Code = "VERIFY", Name = "Ca thử nghiệm", IsActive = true, CheckInTime = new(8, 0), CheckOutTime = new(17, 0), BreakMinutes = 60 });
        var attendance = new Attendance
        {
            EmployeeId = employee.Id, AttendanceDate = new DateTime(2026, 10, 17, 0, 0, 0, DateTimeKind.Utc),
            Status = AttendanceStatus.Present, ApprovalStatus = AttendanceApprovalStatus.Approved,
            CheckInTime = DateTimeOffset.Parse("2026-10-17T01:00:00Z"), CheckOutTime = DateTimeOffset.Parse("2026-10-17T10:00:00Z")
        };
        db.Attendances.Add(attendance);
        await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db);
        var periods = new AttendancePeriodService(uow, actor, clock, new WorkforceExportWriter());
        await periods.ReviewAsync(2026, 10, "Đã kiểm tra dữ liệu giả lập.");
        await periods.LockAsync(2026, 10, "Đã đủ điều kiện khóa kỳ.");
        var snapshot = await periods.SnapshotAsync(2026, 10);
        Assert.Equal(8, Assert.Single(snapshot.Data.Rows).ActualHours);
        attendance.Note = "must roll back";
        var locked = await Assert.ThrowsAsync<M.Core.Base.BaseException.ErrorException>(() => db.SaveChangesAsync());
        Assert.Equal("PERIOD_LOCKED", locked.ErrorDetail.ErrorCode);
        Assert.Null((await db.Attendances.SingleAsync()).Note);
        await periods.ReopenAsync(2026, 10, "Mở lại để kiểm thử phiên bản.");
        await periods.ReviewAsync(2026, 10, "Đã kiểm tra lại dữ liệu giả lập.");
        await periods.LockAsync(2026, 10, "Chốt lại phiên bản tiếp theo.");
        Assert.Equal(snapshot.Sha256, (await periods.SnapshotAsync(2026, 10, 1)).Sha256);
        Assert.Equal(2, await db.AttendancePeriodSnapshots.CountAsync());
        db.Attendances.AddRange(Enumerable.Range(0, 2).Select(_ => new Attendance
        {
            EmployeeId = employee.Id, AttendanceDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
        }));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private sealed class AdminActor : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => ApiFactory.UserA;
        public Guid? EmployeeId => ApiFactory.EmployeeA;
        public string Name => "isolated-provider-test";
        public bool IsInRole(string role) => role == RoleNames.Admin;
    }
}

public sealed class LocalSqlServerFactAttribute : FactAttribute
{
    public LocalSqlServerFactAttribute()
    {
        if (!OperatingSystem.IsWindows()) Skip = "SQL Server LocalDB requires Windows; tested locally, PostgreSQL runs in CI.";
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MARIXA_TEST_POSTGRES")))
            Skip = "Set MARIXA_TEST_POSTGRES to an isolated loopback PostgreSQL server; CI provides one.";
    }
}
