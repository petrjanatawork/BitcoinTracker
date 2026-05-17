using BitcoinTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace BitcoinTracker.Services;

// Repository pro praci s BitcoinRate v databazi.
// Pro cteni pouziva AsNoTracking pro lepsi vykon.
public class BitcoinRateRepository : IBitcoinRateRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BitcoinRateRepository> _logger;

    public BitcoinRateRepository(ApplicationDbContext context, ILogger<BitcoinRateRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Vraci vsechny ulozene kurzy, serazene od nejnovejsich.
    public async Task<List<BitcoinRate>> GetSavedRatesAsync()
    {
        _logger.LogInformation("Fetching all saved Bitcoin rates ordered by timestamp descending");
        return await _context.BitcoinRates
            .OrderByDescending(r => r.Timestamp)
            .AsNoTracking()
            .ToListAsync();
    }

    // Ulozi novy zaznam kurzu do databaze.
    public async Task<BitcoinRate> SaveRateAsync(decimal eurPrice, decimal czkPrice)
    {
        _logger.LogInformation(
            "Saving new Bitcoin rate: {EurPrice} EUR, {CzkPrice} CZK",
            eurPrice,
            czkPrice);

        var rate = new BitcoinRate
        {
            Timestamp = DateTime.UtcNow,
            PriceEur = eurPrice,
            PriceCzk = czkPrice
        };

        _context.BitcoinRates.Add(rate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rate saved successfully with ID {RateId}", rate.Id);
        return rate;
    }

    // Aktualizuje poznamku u konkretniho zaznamu.
    // Kontroluje, ze poznamka neni prazdna a zaznam existuje.
    public async Task UpdateNoteAsync(int id, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            _logger.LogWarning("Validation failed: Note cannot be empty for RateId={RateId}", id);
            throw new ArgumentException("Poznámka nesmí být prázdná.");
        }

        var rate = await _context.BitcoinRates.FindAsync(id);
        if (rate == null)
        {
            _logger.LogWarning("Attempted to update note for non-existent rate ID {RateId}", id);
            throw new KeyNotFoundException($"Záznam s ID {id} nebyl nalezen.");
        }

        rate.Note = note;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated note for rate ID {RateId}", id);
    }

    // Smaze vicero zaznamu podle ID. Neexistujici ID se ignoruji.
    public async Task DeleteRatesAsync(List<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            _logger.LogWarning("DeleteRatesAsync called with empty ID list.");
            return;
        }

        _logger.LogInformation("Deleting rates with IDs: {RateIds}", string.Join(", ", ids));

        var ratesToDelete = await _context.BitcoinRates
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();

        if (ratesToDelete.Count == 0)
        {
            _logger.LogWarning("No rates found to delete for IDs: {RateIds}", string.Join(", ", ids));
            return;
        }

        _context.BitcoinRates.RemoveRange(ratesToDelete);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted {DeletedCount} rates (requested {RequestedCount}).",
            ratesToDelete.Count,
            ids.Count);
    }
}
