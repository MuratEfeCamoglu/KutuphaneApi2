using KutuphaneApi.Data;
using KutuphaneApi.Infrastructure;
using KutuphaneApi.Services;
using KutuphaneApi.Services.Interfaces;
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

        // Üretilen URL'ler (örn. 201 yanıtındaki Location başlığı) /api/Authors yerine /api/authors olur.
        services.AddRouting(options => options.LowercaseUrls = true);

        // Hata yanıtlarını ProblemDetails formatında üretir ve exception'ları GlobalExceptionHandler'a yönlendirir.
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Scoped: her HTTP isteği için bir servis örneği oluşur (DbContext ile aynı yaşam süresi).
        services.AddScoped<IAuthorService, AuthorService>();

        return services;
    }
}
