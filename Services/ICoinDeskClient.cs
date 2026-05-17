using System.Threading.Tasks;

namespace BitcoinTracker.Services
{
    /// <summary>
    /// Client for fetching Bitcoin/EUR price from the CoinDesk API.
    /// </summary>
    public interface ICoinDeskClient
    {
        /// <summary>
        /// Fetches the current Bitcoin price in EUR from CoinDesk.
        /// Returns null if the API call fails or response cannot be parsed.
        /// </summary>
        Task<decimal?> GetBitcoinRateEurAsync();
    }
}
