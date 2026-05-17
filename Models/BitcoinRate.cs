using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitcoinTracker.Models
{
    /// <summary>
    /// Entity representing a saved Bitcoin rate snapshot with optional user note.
    /// Mapped via EF Core Code First. SQL Server uses decimal(18,2) for Price fields.
    /// </summary>
    public class BitcoinRate
    {
        public int Id { get; set; }

        /// <summary>UTC timestamp when this rate was saved.</summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>Bitcoin price in EUR obtained from CoinDesk API.</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceEur { get; set; }

        /// <summary>Bitcoin price in CZK (EUR price × CNB EUR/CZK rate).</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceCzk { get; set; }

        /// <summary>Optional user-editable note attached to this record.</summary>
        public string? Note { get; set; }
    }
}
