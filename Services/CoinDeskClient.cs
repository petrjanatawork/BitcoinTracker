using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BitcoinTracker.Services;
    // Client pro ziskani BTC/EUR kurzu z CoinDesk API.
    // HttpClient ma nakonfigurovanou Polly retry politiku z Program.cs.
    public class CoinDeskClient : ICoinDeskClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CoinDeskClient> _logger;

        private const string ApiUrl = "https://data-api.coindesk.com/spot/v1/latest/tick?market=coinbase&instruments=BTC-EUR";

        public CoinDeskClient(HttpClient httpClient, ILogger<CoinDeskClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Ziska aktualni cenu Bitcoinu v EUR z CoinDesk.
    // Ocekava JSON: {"Data":{"BTC-EUR":{"PRICE":45000.50}}}
        public async Task<decimal?> GetBitcoinRateEurAsync()
        {
            try
            {
                _logger.LogInformation("Fetching Bitcoin rate from {ApiName}", "CoinDesk");

                var response = await _httpClient.GetStringAsync(ApiUrl);
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
    }

