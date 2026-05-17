using BitcoinTracker.Models;
using BitcoinTracker.Services;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/bitcointracker-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Bitcoin Tracker application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Framework services
    builder.Services.AddControllersWithViews().AddNewtonsoftJson();
    builder.Services.AddMemoryCache();

    // Databaze: Development -> SQLite, Production -> SQL Server
    if (builder.Environment.IsDevelopment())
    {
        var sqlitePath = Path.Combine(builder.Environment.ContentRootPath, "BitcoinTracker.db");
        Log.Information("DEVELOPMENT mode: Using SQLite database at '{SqlitePath}'", sqlitePath);
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite($"Data Source={sqlitePath}"));
    }
    else
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is required in Production mode. " +
                "Set ConnectionStrings__DefaultConnection environment variable.");
        Log.Information("PRODUCTION mode: Using SQL Server");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
    }

    // Aplikacni sluzby
    builder.Services.AddTransient<DatabaseInitializer>();
    builder.Services.AddScoped<IBitcoinRateRepository, BitcoinRateRepository>();

    // HttpClient s Polly retry politikou
    // CoinDesk: 60s timeout + 2 retry (2s, 4s backoff)
    // CNB: 30s timeout + 2 retry (2s, 4s backoff)
    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(2, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    // CoinDesk API client (BTC/EUR)
    builder.Services.AddHttpClient<ICoinDeskClient, CoinDeskClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    })
    .AddPolicyHandler(retryPolicy);

    // CNB API client (EUR/CZK)
    builder.Services.AddHttpClient<ICnbClient, CnbClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddPolicyHandler(retryPolicy);

    // Bitcoin Facade
    builder.Services.AddScoped<IBitcoinService, BitcoinFacade>();

    var app = builder.Build();

    // HTTP pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
    }
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();
    app.MapControllers();
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // Inicializace databaze
    if (builder.Environment.IsDevelopment())
    {
        Log.Information("Initializing SQLite database...");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        }
    }
    else
    {
        Log.Information("Initializing SQL Server database with retry logic...");
        var initializer = app.Services.GetRequiredService<DatabaseInitializer>();
        await initializer.EnsureDatabaseCreatedAsync();
    }

    Log.Information("Database initialized. Application is ready.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

