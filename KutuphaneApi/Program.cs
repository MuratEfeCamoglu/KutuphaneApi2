using KutuphaneApi.Data.Seed;
using KutuphaneApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DbInitializer.InitializeAsync(app.Services);
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
