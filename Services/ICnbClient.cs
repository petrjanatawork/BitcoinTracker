namespace BitcoinTracker.Services
{
    /// <summary>
    /// Client for fetching EUR/CZK exchange rate from the Czech National Bank (CNB).
    /// </summary>
    public interface ICnbClient
    {
        /// <summary>
        /// Fetches the current EUR/CZK exchange rate from CNB.
        /// Returns null if the API call fails or response cannot be parsed.
        /// </summary>
        Task<decimal?> GetEurCzkRateAsync();
    }
}
