using BitcoinTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace BitcoinTracker.Services;

// Stara se o vytvoreni databaze s opakovanim pri selhani.
// Pouziva se pro Docker prostredi, kde SQL Server nemusi byt hned ready.
// Kazdy pokus vytvori novy DbContext pres IServiceScopeFactory.
public class DatabaseInitializer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    private const int MaxRetries = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public DatabaseInitializer(IServiceScopeFactory scopeFactory, ILogger<DatabaseInitializer> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Zajisti, ze databaze existuje. Pri neuspechu opakuje az MaxRetries pokusu.
    public async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
    {
        var retriesRemaining = MaxRetries;

        while (retriesRemaining > 0)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await db.Database.EnsureCreatedAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Database created or already exists. Connection established (used {UsedRetries} of {MaxRetries} attempts).",
                    MaxRetries - retriesRemaining + 1,
                    MaxRetries);
                return;
            }
            catch (Exception ex) when (retriesRemaining > 1 && !cancellationToken.IsCancellationRequested)
            {
                retriesRemaining--;
                _logger.LogWarning(
                    "Database connection failed. Retrying in {RetryDelay}s... ({RetriesRemaining} retries left). Error: {Error}",
                    RetryDelay.TotalSeconds,
                    retriesRemaining,
                    ex.Message);
                await Task.Delay(RetryDelay, cancellationToken);
            }
            catch (Exception ex) when (retriesRemaining <= 1 || cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "Failed to connect to database after {MaxRetries} attempts.",
                    MaxRetries);
                throw;
            }
        }
    }
}
