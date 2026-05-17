using BitcoinTracker.Models;

namespace BitcoinTracker.Services
{
    public interface IBitcoinService
    {
        Task<(decimal? EurPrice, decimal? CzkPrice)> GetCurrentRatesAsync();
        Task<List<BitcoinRate>> GetSavedRatesAsync();
        Task<BitcoinRate> SaveRateAsync(decimal eurPrice, decimal czkPrice);
        Task UpdateNoteAsync(int id, string note);
        Task DeleteRatesAsync(List<int> ids);
    }
}
