using BitcoinTracker.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BitcoinTracker.Services
{
    // Kombinuje volani externich API (CoinDesk, CNB) s praci s databazi.
    // BTC/EUR: CoinDesk (real-time, cache 15s)
    // EUR/CZK: CNB API (aktualizace jednou denne, cache 1h)
    public class BitcoinFacade : IBitcoinService
    {
        private readonly ICoinDeskClient _coinDeskClient;
        private readonly ICnbClient _cnbClient;
        private readonly IBitcoinRateRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BitcoinFacade> _logger;

        private const string LiveRateCacheKey = "LiveBitcoinRate";
        private const string EurCzkCacheKey = "EurCzkRate";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan EurCzkCacheDuration = TimeSpan.FromHours(1);

        public BitcoinFacade(
            ICoinDeskClient coinDeskClient,
            ICnbClient cnbClient,
            IBitcoinRateRepository repository,
            IMemoryCache cache,
            ILogger<BitcoinFacade> logger)
        {
            _coinDeskClient = coinDeskClient ?? throw new ArgumentNullException(nameof(coinDeskClient));
            _cnbClient = cnbClient ?? throw new ArgumentNullException(nameof(cnbClient));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Ziskani aktualnich kurzu s paralelnim dotazem na obe API.
        // Pokud EUR/CZK neni k dispozici, vrati se pouze BTC/EUR.
        public async Task<(decimal? EurPrice, decimal? CzkPrice)> GetCurrentRatesAsync()
        {
            if (_cache.TryGetValue(LiveRateCacheKey, out (decimal Eur, decimal Czk) cached))
            {
                _logger.LogInformation("Returning cached Bitcoin rate: {EurPrice} EUR / {CzkPrice} CZK",
                    cached.Eur, cached.Czk);
                return (cached.Eur, cached.Czk);
            }

            _logger.LogInformation("Cache miss – fetching fresh rates from external APIs");

            var btcTask = FetchBtcEurAsync();
            var czkTask = FetchEurCzkAsync();

            await Task.WhenAll(btcTask, czkTask);

            var eurPrice = btcTask.Result;
            if (eurPrice == null)
            {
                _logger.LogWarning("BTC/EUR rate unavailable – CoinDesk API unreachable");
                return (null, null);
            }

            var eurCzkRate = czkTask.Result;
            if (eurCzkRate == null)
            {
                _logger.LogWarning("EUR/CZK rate unavailable – returning BTC/EUR only");
                return (eurPrice, null);
            }

            var czkPrice = Math.Round(eurPrice.Value * eurCzkRate.Value, 2);
            _cache.Set(LiveRateCacheKey, (eurPrice.Value, czkPrice), CacheDuration);

            return (eurPrice.Value, czkPrice);
        }

        private async Task<decimal?> FetchBtcEurAsync()
        {
            try
            {
                var rate = await _coinDeskClient.GetBitcoinRateEurAsync();
                if (rate.HasValue)
                {
                    _logger.LogInformation("BTC/EUR from CoinDesk: {Rate}", rate.Value);
                    return rate.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CoinDesk API error");
            }
            return null;
        }

        private async Task<decimal?> FetchEurCzkAsync()
        {
            if (_cache.TryGetValue(EurCzkCacheKey, out decimal cachedRate))
            {
                _logger.LogInformation("Using cached EUR/CZK rate: {Rate}", cachedRate);
                return cachedRate;
            }

            try
            {
                var cnbRate = await _cnbClient.GetEurCzkRateAsync();
                if (cnbRate.HasValue)
                {
                    _logger.LogInformation("Caching EUR/CZK rate from CNB: {Rate}", cnbRate.Value);
                    _cache.Set(EurCzkCacheKey, cnbRate.Value, EurCzkCacheDuration);
                    return cnbRate.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CNB API error");
            }
            return null;
        }

        public Task<List<BitcoinRate>> GetSavedRatesAsync()
            => _repository.GetSavedRatesAsync();

        public Task<BitcoinRate> SaveRateAsync(decimal eurPrice, decimal czkPrice)
            => _repository.SaveRateAsync(eurPrice, czkPrice);

        public Task UpdateNoteAsync(int id, string note)
            => _repository.UpdateNoteAsync(id, note);

        public Task DeleteRatesAsync(List<int> ids)
            => _repository.DeleteRatesAsync(ids);
    }
}

