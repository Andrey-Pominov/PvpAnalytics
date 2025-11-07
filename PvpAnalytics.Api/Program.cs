using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Infrastructure;
using PvpAnalytics.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope()) {
    var db = scope.ServiceProvider.GetRequiredService<PvpAnalyticsDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();

app.UseAuthorization();


app.MapControllers();

app.Run();