using OutBoxPattern;
using OutBoxPattern.Model;
using OutBoxPattern.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<Migrations>();
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddTransient<TelemetryService>();
builder.Services.AddHostedService<OutBoxService>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var migrations =
        scope.ServiceProvider.GetRequiredService<Migrations>();

    await migrations.CreateTables();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.MapPost("/telemetry", async (
    Telemetry telemetry, TelemetryService telemetryService) =>
{
    await telemetryService.InsertTelemetryAsync(telemetry);
    return Results.Ok();
});

app.Run();
