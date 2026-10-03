using KutuphaneApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace KutuphaneApi.Tests.Integration;

// WebApplicationFactory: Program.cs'i gerçek sunucu açmadan bellekte çalıştırır ve ona istek atan bir HttpClient verir.
// İstek; middleware, routing, controller, servis ve veritabanından oluşan tüm zinciri gerçekten geçer.
public sealed class KutuphaneApiFactory : WebApplicationFactory<Program>
{
    // Her factory kendine özel, paylaşımlı bir in-memory SQLite veritabanı kullanır.
    // "cache=shared": aynı adla açılan tüm bağlantılar aynı veritabanını görür.
    private readonly string _connectionString = $"DataSource=file:test-{Guid.NewGuid()}?mode=memory&cache=shared";

    // Paylaşımlı in-memory veritabanı, en az bir bağlantı açık kaldığı sürece yaşar.
    private readonly SqliteConnection _keepAliveConnection;

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero));

    public KutuphaneApiFactory()
    {
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" ortamı: Program.cs'teki Development bloğu (migration + seed, Scalar) çalışmaz.
        builder.UseEnvironment("Testing");

        // appsettings.json'daki bağlantı cümlesini test veritabanıyla ezer.
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);

        // ConfigureTestServices: uygulamanın DI kayıtlarından SONRA çalışır, kayıtları değiştirmemizi sağlar.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
        });
    }

    // Veritabanı tablolarını oluşturur ve istemciyi döner.
    public HttpClient CreateClientWithDatabase()
    {
        var client = CreateClient();

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAliveConnection.Dispose();
        }
    }
}
