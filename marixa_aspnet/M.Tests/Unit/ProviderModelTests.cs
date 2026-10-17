using M.Repositories.Context;
using Microsoft.EntityFrameworkCore;

namespace M.Tests.Unit;

public sealed class ProviderModelTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Both_provider_models_generate_required_constraints_without_connecting(bool postgres)
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>();
        if (postgres) options.UseNpgsql("Host=localhost;Database=unused_model_test;Username=model_test;Password=unused");
        else options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=unused_model_test;Integrated Security=True");
        using var db = new DatabaseContext(options.Options);
        var script = db.Database.GenerateCreateScript();
        Assert.Contains("UX_Attendances_Employee_Date", script);
        Assert.Contains("UX_AttendanceRules_Active", script);
        Assert.Contains(postgres ? "\"IsActive\" = TRUE" : "[IsActive] = 1", script);
    }
}
