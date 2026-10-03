using KutuphaneApi.Data.Seed;
using KutuphaneApi.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

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
