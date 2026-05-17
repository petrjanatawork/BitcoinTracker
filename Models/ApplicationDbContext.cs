using Microsoft.EntityFrameworkCore;

namespace BitcoinTracker.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BitcoinRate> BitcoinRates { get; set; }
    }
}
