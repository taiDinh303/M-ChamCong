using M.Contract.Repositories.Persistence;
using M.Repositories.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace M.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
