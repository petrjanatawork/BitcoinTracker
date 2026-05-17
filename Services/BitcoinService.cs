using BitcoinTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace BitcoinTracker.Services
{
    /// <summary>
    /// Service implementing IBitcoinService with external API calls (CoinDesk, CNB),
    /// database CRUD operations, and in-memory caching for live rates.
    /// Uses IMemoryCache to reduce external API calls and avoid rate limiting.
    /// </summary>
    public class BitcoinService : IBitcoinService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<BitcoinService> _logger;
        private readonly IMemoryCache _cache;

        private const string CoinDeskUrl = "https://data-api.coindesk.com/spot/v1/latest/tick?market=coinbase&instruments=BTC-EUR";
        private const string CnbUrl = "https://www.cnb.cz/cs/financni-trhy/devizovy-trh/kurzy-devizoveho-trhu/kurzy-devizoveho-trhu/denni_kurz.txt";
        private const string LiveRateCacheKey = "LiveBitcoinRate";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);

        public BitcoinService(
            ApplicationDbContext context,
            HttpClient httpClient,
            ILogger<BitcoinService> logger,
            IMemoryCache cache)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Gets the current live Bitcoin rate (EUR + CZK).
        /// Returns cached data if available and not expired (TTL: 15s).
        /// </summary>
        public async Task<(decimal? EurPrice, decimal? CzkPrice)> GetCurrentRatesAsync()
        {
            // Try cache first to avoid hitting external API rate limits
            if (_cache.TryGetValue(LiveRateCacheKey, out (decimal Eur, decimal Czk) cached))
            {
                _logger.LogInformation("Returning cached Bitcoin rate: {EurPrice} EUR / {CzkPrice} CZK",
                    cached.Eur, cached.Czk);
                return (cached.Eur, cached.Czk);
            }

            _logger.LogInformation("Cache miss – fetching fresh rates from external APIs");

            var eurPrice = await GetBitcoinRateEurAsync();
            var eurCzkRate = await GetEurCzkRateAsync();

            if (eurPrice.HasValue && eurCzkRate.HasValue)
            {
                var czkPrice = Math.Round(eurPrice.Value * eurCzkRate.Value, 2);

                // Store in cache for next request
                _cache.Set(LiveRateCacheKey, (eurPrice.Value, czkPrice), CacheDuration);

                return (eurPrice.Value, czkPrice);
            }

            _logger.LogWarning(
                "Failed to retrieve complete rate data. BTC/EUR valid: {BitcoinRateValid}, EUR/CZK valid: {EurCzkRateValid}",
                eurPrice.HasValue,
                eurCzkRate.HasValue);

            return (null, null);
        }

        /// <summary>
        /// Fetches the Bitcoin/EUR price from CoinDesk API.
        /// Returns null if the API call fails or response cannot be parsed.
        /// </summary>
        private async Task<decimal?> GetBitcoinRateEurAsync()
        {
            try
            {
                _logger.LogInformation("Fetching Bitcoin rate from {ApiName}", "CoinDesk");

                var response = await _httpClient.GetStringAsync(CoinDeskUrl);
                var json = JObject.Parse(response);
                var price = json["Data"]?["BTC-EUR"]?["PRICE"]?.Value<decimal>();

                if (price == null)
                {
                    _logger.LogWarning(
                        "Bitcoin price not found in {ApiName} response. Raw length: {ResponseLength}",
                        "CoinDesk",
                        response.Length);
                }

                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while fetching Bitcoin rate from {ApiName}",
                    "CoinDesk");
                return null;
            }
        }

        /// <summary>
        /// Fetches the EUR/CZK exchange rate from the Czech National Bank (CNB).
        /// Parses the daily text file format: "|EUR|1|...|rate|".
        /// Returns null if the API call fails or response cannot be parsed.
        /// </summary>
        private async Task<decimal?> GetEurCzkRateAsync()
        {
            try
            {
                _logger.LogInformation("Fetching EUR/CZK rate from {ApiName}", "CNB");

                var response = await _httpClient.GetStringAsync(CnbUrl);
                var lines = response.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (!line.Contains("|EUR|", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var parts = line.Split('|');
                    if (parts.Length < 5)
                    {
                        _logger.LogWarning(
                            "CNB EUR line has insufficient parts: expected 5, got {PartCount}. Line: {Line}",
                            parts.Length,
                            line);
                        continue;
                    }

                    var amountStr = parts[2].Trim();
                    var rateStr = parts[4].Replace(',', '.').Trim();

                    if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) &&
                        decimal.TryParse(rateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                    {
                        _logger.LogInformation(
                            "Retrieved EUR/CZK rate: {Rate} from {ApiName}",
                            rate / amount,
                            "CNB");
                        return rate / amount;
                    }

                    _logger.LogWarning(
                        "Could not parse CNB amount/rate. Raw amount: {RawAmount}, raw rate: {RawRate}",
                        amountStr,
                        rateStr);
                }

                _logger.LogWarning(
                    "Could not parse EUR/CZK rate from {ApiName} - no valid EUR line found. Response length: {ResponseLength}",
                    "CNB",
                    response.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while fetching EUR/CZK rate from {ApiName}",
                    "CNB");
            }

            return null;
        }

        /// <summary>
        /// Returns all saved Bitcoin rates ordered by timestamp (newest first).
        /// Uses AsNoTracking() for read-only performance optimization.
        /// </summary>
        public async Task<List<BitcoinRate>> GetSavedRatesAsync()
        {
            _logger.LogInformation("Fetching all saved Bitcoin rates ordered by timestamp descending");
            return await _context.BitcoinRates
                .OrderByDescending(r => r.Timestamp)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Saves a new Bitcoin rate snapshot to the database.
        /// </summary>
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

        /// <summary>
        /// Updates the note for a specific saved rate.
        /// Validates that the note is not empty/whitespace and the rate exists.
        /// </summary>
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

        /// <summary>
        /// Deletes multiple saved rates by their IDs.
        /// Silently ignores IDs that don't exist in the database.
        /// </summary>
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
}
