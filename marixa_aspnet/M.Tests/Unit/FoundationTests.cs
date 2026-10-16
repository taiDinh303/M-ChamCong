using M.Core.Base;
using M.Core.Time;
using M.Repositories.Configuration;
using M.Services.Services;
using Npgsql;

namespace M.Tests.Unit;

public sealed class FoundationTests
{
    [Theory]
    [InlineData("2026-10-16T16:59:59Z", 16)]
    [InlineData("2026-10-16T17:00:00Z", 17)]
    [InlineData("2026-10-16T18:00:00Z", 17)]
    public void Business_date_changes_at_Vietnam_midnight(string instant, int day) =>
        Assert.Equal(day, BusinessTime.DateAt(DateTimeOffset.Parse(instant)).Day);

    [Fact]
    public void Postgres_URI_preserves_TLS_options_and_escapes_password()
    {
        var value = PostgresConnectionString.Normalize("postgresql://postgres.test:p%3Bass%3Dx@localhost:5432/postgres?sslmode=require&connect_timeout=12");
        var parsed = new NpgsqlConnectionStringBuilder(value);
        Assert.Equal("p;ass=x", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
        Assert.Equal(12, parsed.Timeout);
        Assert.Equal("localhost", parsed.Host);
    }

    [Fact]
    public void Unknown_URI_option_is_not_silently_dropped() =>
        Assert.Throws<FormatException>(() => PostgresConnectionString.Normalize("postgresql://user:test@localhost/db?unknown=true"));

    [Fact]
    public void Pagination_defends_boundaries_and_copies_items()
    {
        Assert.Throws<BaseException.ErrorException>(() => Pagination.Normalize(0, 5));
        Assert.Equal(200, Pagination.Normalize(1, 10000).PageSize);
        var input = new List<int> { 1 };
        var page = new BasePaginatedList<int>(input, 1, 1, 10);
        input.Clear();
        Assert.Single(page.Items);
    }

    [Fact]
    public void Calculator_retains_existing_OT_policy()
    {
        var result = AttendanceTimeCalculator.Compute(DateTimeOffset.Parse("2026-10-16T01:00:00Z"),
            DateTimeOffset.Parse("2026-10-16T11:00:00Z"), 60, new TimeOnly(17, 0));
        Assert.Equal(9, result.ActualHours);
        Assert.Equal(1m, result.OvertimeHours);
    }

    [Fact]
    public void Leave_balance_includes_negative_adjustments_and_ignores_soft_deleted_entries()
    {
        var entries = new[]
        {
            new M.Contract.Repositories.Entities.LeaveLedger { EntryType = M.Contract.Repositories.Entities.LeaveLedgerEntryType.Grant, Days = 12 },
            new M.Contract.Repositories.Entities.LeaveLedger { EntryType = M.Contract.Repositories.Entities.LeaveLedgerEntryType.Adjust, Days = -2 },
            new M.Contract.Repositories.Entities.LeaveLedger { EntryType = M.Contract.Repositories.Entities.LeaveLedgerEntryType.Deduct, Days = 4 },
            new M.Contract.Repositories.Entities.LeaveLedger { EntryType = M.Contract.Repositories.Entities.LeaveLedgerEntryType.Refund, Days = 1 },
            new M.Contract.Repositories.Entities.LeaveLedger { EntryType = M.Contract.Repositories.Entities.LeaveLedgerEntryType.Grant, Days = 99, DeletedTime = DateTimeOffset.UtcNow }
        };
        Assert.Equal(new LeaveBalance(10, 3, 7), LeaveBalanceCalculator.Calculate(entries));
    }

    [Fact]
    public void Soft_delete_and_restore_preserve_audit_invariants()
    {
        var entity = new M.Contract.Repositories.Entities.Department();
        var now = DateTimeOffset.Parse("2026-10-16T00:00:00Z");
        entity.SoftDelete("admin", now);
        entity.SoftDelete("other", now.AddDays(1));
        Assert.Equal(now, entity.DeletedTime);
        Assert.Equal("admin", entity.DeletedBy);
        entity.Restore("hr", now.AddDays(2));
        Assert.Null(entity.DeletedTime);
        Assert.Null(entity.DeletedBy);
        Assert.Equal("hr", entity.LastUpdatedBy);
    }

    [Fact]
    public void Contract_assemblies_do_not_reference_persistence_implementation()
    {
        var contracts = typeof(M.Contract.Services.Interface.IAttendanceService).Assembly;
        Assert.DoesNotContain(contracts.GetReferencedAssemblies(), a => a.Name == "M.Repositories");
        Assert.DoesNotContain(typeof(M.Contract.Repositories.Persistence.IUnitOfWork).Assembly.GetReferencedAssemblies(),
            a => a.Name == "Microsoft.EntityFrameworkCore");
    }
}
