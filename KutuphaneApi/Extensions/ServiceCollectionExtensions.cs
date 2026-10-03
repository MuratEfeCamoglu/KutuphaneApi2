using System.Globalization;
using FluentValidation;
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
        // CustomizeProblemDetails: üretilen her ProblemDetails yanıtına son dokunuşu yapar. ASP.NET Core'un kendi
        // model binding hataları (bozuk JSON, geçersiz enum) İngilizce başlıkla gelir; FluentValidation hatalarıyla aynı olsun.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            if (context.ProblemDetails is HttpValidationProblemDetails)
            {
                context.ProblemDetails.Title = "Doğrulama hatası";
            }
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // TimeProvider: "şu an"ı veren soyutlama. Gerçekte sistem saati, testlerde ileri sarılabilen sahte saat verilir.
        services.AddSingleton(TimeProvider.System);

        // Bu projedeki tüm AbstractValidator<T> sınıflarını bulup IValidator<T> olarak kaydeder.
        services.AddValidatorsFromAssemblyContaining<AppDbContext>();

        // Varsayılan doğrulama mesajları sunucunun diline göre değişmesin, her zaman Türkçe olsun.
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("tr");

        // Scoped: her HTTP isteği için bir servis örneği oluşur (DbContext ile aynı yaşam süresi).
        services.AddScoped<IAuthorService, AuthorService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<ILoanService, LoanService>();

        return services;
    }
}
