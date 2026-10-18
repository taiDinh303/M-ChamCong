using M.Contract.Repositories.Entities;
using M.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace M.Tests.Integration;

public sealed class PostgresAttendanceIntegrityTests
{
    [PostgresFact]
    public async Task Upgrade_rejects_duplicates_preserves_history_and_enforces_both_unique_guards()
    {
        var master = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MARIXA_TEST_POSTGRES"));
        if (master.Host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException("Provider tests only accept a loopback PostgreSQL server.");
        master.Pooling = false;
        var name = "Marixa_Verify_Integrity_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(master.ConnectionString);
        await connection.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection)) await create.ExecuteNonQueryAsync();
        try
        {
            master.Database = name;
            await using var db = new DatabaseContext(new DbContextOptionsBuilder<DatabaseContext>().UseNpgsql(master.ConnectionString).Options);
            await db.Database.EnsureCreatedAsync();
            await db.Database.ExecuteSqlRawAsync("DROP INDEX \"UX_AttendanceRules_Active\", \"UX_Attendances_Employee_Date\"");
            var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "20261017171012_attendance_integrity.sql"));
            var employee = new Employee { EmployeeCode = "INTEGRITY", GivenName = "Thử", FamilyName = "Nghiệm" };
            var firstRule = Rule("INTEGRITY-1");
            var secondRule = Rule("INTEGRITY-2");
            db.Employees.Add(employee);
            db.AttendanceRules.AddRange(firstRule, secondRule);
            await db.SaveChangesAsync();

            var invalidRules = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(sql));
            Assert.Equal("P0001", invalidRules.SqlState);
            await db.Database.ExecuteSqlRawAsync("ROLLBACK");
            Assert.Equal(2, await db.AttendanceRules.CountAsync());
            secondRule.IsActive = false;
            await db.SaveChangesAsync();

            var day = new DateTime(2030, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            var firstAttendance = new Attendance { EmployeeId = employee.Id, AttendanceDate = day };
            var secondAttendance = new Attendance { EmployeeId = employee.Id, AttendanceDate = day };
            db.Attendances.AddRange(firstAttendance, secondAttendance);
            await db.SaveChangesAsync();
            var invalidDays = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(sql));
            Assert.Equal("P0001", invalidDays.SqlState);
            await db.Database.ExecuteSqlRawAsync("ROLLBACK");
            Assert.Equal(2, await db.Attendances.CountAsync());

            secondAttendance.DeletedTime = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync(sql);
            await db.Database.ExecuteSqlRawAsync(sql);
            Assert.Equal(2, await db.Attendances.CountAsync());
            Assert.Equal(2, await db.AttendanceRules.CountAsync());

            db.AttendanceRules.Add(Rule("INTEGRITY-3"));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            db.Attendances.Add(new Attendance { EmployeeId = employee.Id, AttendanceDate = day });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            Assert.Equal(1, await db.AttendanceRules.CountAsync(r => r.IsActive && r.DeletedTime == null));
            Assert.Equal(2, await db.Attendances.CountAsync());
        }
        finally
        {
            Assert.StartsWith("Marixa_Verify_Integrity_", name);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{name}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static AttendanceRule Rule(string code) => new()
    {
        Code = code, Name = "Ca kiểm thử", IsActive = true,
        CheckInTime = new(8, 0), CheckOutTime = new(17, 0), BreakMinutes = 60
    };
}
