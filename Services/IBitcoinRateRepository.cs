using BitcoinTracker.Models;

namespace BitcoinTracker.Services
{
    public interface IBitcoinRateRepository
    {
        Task<List<BitcoinRate>> GetSavedRatesAsync();
        Task<BitcoinRate> SaveRateAsync(decimal eurPrice, decimal czkPrice);
        Task UpdateNoteAsync(int id, string note);
        Task DeleteRatesAsync(List<int> ids);
    }
}
