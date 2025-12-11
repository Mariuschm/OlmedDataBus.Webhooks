using Prosepo.Webhooks.Services;

var builder = WebApplication.CreateBuilder(args);

// Konfiguracja logowania - zachowanie domyœlnego logowania ASP.NET Core
builder.Logging.AddConsole();

// Dodanie logowania do pliku
var logsDirectory = builder.Configuration["Logging:File:Directory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Logs");
Directory.CreateDirectory(logsDirectory);

// Dodaj ³adowanie appsettings.Local.json jeœli istnieje (dla jawnoœci)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddHttpClient();

// Rejestracja FileLoggingService
builder.Services.AddSingleton<FileLoggingService>();

// Rejestracja CronSchedulerService jako Hosted Service
builder.Services.AddSingleton<CronSchedulerService>();
builder.Services.AddHostedService<CronSchedulerService>(provider => provider.GetService<CronSchedulerService>()!);

// Rejestracja ProductSyncConfigurationService
builder.Services.AddScoped<ProductSyncConfigurationService>();

// Rejestracja GracefulShutdownService jako Hosted Service
builder.Services.AddHostedService<GracefulShutdownService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
