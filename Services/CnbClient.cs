using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BitcoinTracker.Services
{
    // Client pro ziskani EUR/CZK kurzu z CNB API (api.cnb.cz).
    // Pouziva JSON endpoint, ktery je dostupny i z TOR site.
    public class CnbClient : ICnbClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CnbClient> _logger;

        private const string ApiUrl = "https://api.cnb.cz/cnbapi/exrates/daily?lang=EN";

        public CnbClient(HttpClient httpClient, ILogger<CnbClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Ziska aktualni kurz EUR/CZK z CNB REST API.
        // Parsuje JSON rucne pomoci JsonDocument.
        public async Task<decimal?> GetEurCzkRateAsync()
        {
            try
            {
                _logger.LogInformation("Fetching EUR/CZK rate from CNB API");

                var response = await _httpClient.GetStringAsync(ApiUrl);
                _logger.LogInformation("CNB API response length: {Len} chars", response.Length);

                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (!root.TryGetProperty("rates", out var ratesArray))
                {
                    _logger.LogWarning("CNB API response missing 'rates' property");
                    return null;
                }

                foreach (var rate in ratesArray.EnumerateArray())
                {
                    var currencyCode = rate.GetProperty("currencyCode").GetString();
                    if (currencyCode == "EUR")
                    {
                        var amount = rate.GetProperty("amount").GetInt32();
                        var rateValue = rate.GetProperty("rate").GetDecimal();
                        var eurRate = rateValue / amount;

                        _logger.LogInformation("Retrieved EUR/CZK rate: {Rate} from CNB API", eurRate);
                        return eurRate;
                    }
                }

                _logger.LogWarning("EUR rate not found in CNB API response");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse CNB API JSON response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching EUR/CZK rate from CNB API");
            }

            return null;
        }
    }
}
