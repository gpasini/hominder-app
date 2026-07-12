using Hominder.Application.Common.Persistence;
using Hominder.Infrastructure.Persistence;
using Hominder.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hominder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Hominder")
            ?? throw new InvalidOperationException("La chaîne de connexion 'Hominder' est absente.");

        services.AddDbContext<HominderDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<HominderDbContext>());
        services.AddScoped<IMaintenanceTaskRepository, MaintenanceTaskRepository>();
        services.AddScoped<IHouseholdMemberRepository, HouseholdMemberRepository>();

        return services;
    }
}
