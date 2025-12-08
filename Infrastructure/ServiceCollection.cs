using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ServiceCollection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string postgresConnectionString = configuration.GetConnectionString("Postgres");
            
        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            services.AddDbContext<OCPPCoreContext>(options => 
                options.UseNpgsql(postgresConnectionString, 
                    b => b.MigrationsAssembly(typeof(OCPPCoreContext).Assembly.FullName)), ServiceLifetime.Transient);
        }

        services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>()!);
        return services;
    }
}
