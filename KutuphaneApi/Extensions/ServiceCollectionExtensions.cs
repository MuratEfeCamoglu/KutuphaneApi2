using KutuphaneApi.Data;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneApi.Extensions;

// Extension metot: DI kayıtlarını tek yerde toplar, Program.cs sade kalır.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bağlantı cümlesi appsettings.json → "ConnectionStrings:DefaultConnection" içinden okunur.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
