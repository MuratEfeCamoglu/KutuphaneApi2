using System.Text.Json.Serialization;
using KutuphaneApi.Data.Seed;
using KutuphaneApi.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Nullable olmayan string alanlar için ASP.NET Core'un kendi gizli [Required] kontrolünü kapatırız;
// böylece tüm doğrulama mesajları tek kaynaktan, FluentValidation'dan gelir.
builder.Services.AddControllers(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    // Enum'lar JSON'da sayı yerine adıyla yazılır: "status": "Overdue".
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// Pipeline'ın en başında: sonraki adımlarda fırlayan exception'ları yakalar.
app.UseExceptionHandler();

// Gövdesiz 4xx/5xx yanıtlarına (örn. olmayan bir adres → 404) ProblemDetails gövdesi ekler.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    await DbInitializer.InitializeAsync(app.Services);
    app.MapOpenApi();

    // Scalar: /openapi/v1.json dokümanını /scalar adresinde tarayıcıdan denenebilir bir arayüzle gösterir.
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
