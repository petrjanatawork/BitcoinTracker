using BitcoinTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace BitcoinTracker.Tests
{
    public class ModelTests
    {
        #region BitcoinRate Tests

        [Fact]
        public void BitcoinRate_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var rate = new BitcoinRate();

            // Assert
            Assert.Equal(0, rate.Id);
            Assert.Equal(default, rate.Timestamp);
            Assert.Equal(0m, rate.PriceEur);
            Assert.Equal(0m, rate.PriceCzk);
            Assert.Null(rate.Note);
        }

        [Fact]
        public void BitcoinRate_CanBeCreatedWithAllProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var note = "Test note";

            // Act
            var rate = new BitcoinRate
            {
                Id = 1,
                Timestamp = timestamp,
                PriceEur = 45000.50m,
                PriceCzk = 1100000.25m,
                Note = note
            };

            // Assert
            Assert.Equal(1, rate.Id);
            Assert.Equal(timestamp, rate.Timestamp);
            Assert.Equal(45000.50m, rate.PriceEur);
            Assert.Equal(1100000.25m, rate.PriceCzk);
            Assert.Equal(note, rate.Note);
        }

        [Fact]
        public void BitcoinRate_NoteCanBeNull()
        {
            // Arrange & Act
            var rate = new BitcoinRate { Note = null };

            // Assert
            Assert.Null(rate.Note);
        }

        [Fact]
        public void BitcoinRate_NoteCanBeEmpty()
        {
            // Arrange & Act
            var rate = new BitcoinRate { Note = "" };

            // Assert
            Assert.Equal("", rate.Note);
        }

        [Fact]
        public void BitcoinRate_SupportsLargeDecimalValues()
        {
            // Arrange & Act
            var rate = new BitcoinRate
            {
                PriceEur = 999999999999.99m,
                PriceCzk = 99999999999999.99m
            };

            // Assert
            Assert.Equal(999999999999.99m, rate.PriceEur);
            Assert.Equal(99999999999999.99m, rate.PriceCzk);
        }

        [Fact]
        public void BitcoinRate_SupportsSmallDecimalValues()
        {
            // Arrange & Act
            var rate = new BitcoinRate
            {
                PriceEur = 0.01m,
                PriceCzk = 0.01m
            };

            // Assert
            Assert.Equal(0.01m, rate.PriceEur);
            Assert.Equal(0.01m, rate.PriceCzk);
        }

        [Fact]
        public void BitcoinRate_ZeroValues_AreValid()
        {
            // Arrange & Act
            var rate = new BitcoinRate
            {
                Timestamp = DateTime.UtcNow,
                PriceEur = 0m,
                PriceCzk = 0m
            };

            // Assert
            Assert.Equal(0m, rate.PriceEur);
            Assert.Equal(0m, rate.PriceCzk);
        }

        [Fact]
        public void BitcoinRate_NegativeValues_AreAllowed()
        {
            // Arrange & Act - In case of unusual market conditions
            var rate = new BitcoinRate
            {
                PriceEur = -100.00m,
                PriceCzk = -2500.00m
            };

            // Assert
            Assert.Equal(-100.00m, rate.PriceEur);
            Assert.Equal(-2500.00m, rate.PriceCzk);
        }

        #endregion

        #region BitcoinRate Validation Tests

        [Fact]
        public void BitcoinRate_Timestamp_RequiredAttributeIsSet()
        {
            // Arrange
            var timestampProperty = typeof(BitcoinRate).GetProperty(nameof(BitcoinRate.Timestamp));
            
            // Act
            var requiredAttribute = timestampProperty?.GetCustomAttributes(typeof(RequiredAttribute), true)
                .FirstOrDefault() as RequiredAttribute;

            // Assert
            Assert.NotNull(requiredAttribute);
        }

        [Fact]
        public void BitcoinRate_PriceEur_RequiredAttributeIsSet()
        {
            // Arrange
            var priceProperty = typeof(BitcoinRate).GetProperty(nameof(BitcoinRate.PriceEur));
            
            // Act
            var requiredAttribute = priceProperty?.GetCustomAttributes(typeof(RequiredAttribute), true)
                .FirstOrDefault() as RequiredAttribute;

            // Assert
            Assert.NotNull(requiredAttribute);
        }

        [Fact]
        public void BitcoinRate_PriceCzk_RequiredAttributeIsSet()
        {
            // Arrange
            var priceProperty = typeof(BitcoinRate).GetProperty(nameof(BitcoinRate.PriceCzk));
            
            // Act
            var requiredAttribute = priceProperty?.GetCustomAttributes(typeof(RequiredAttribute), true)
                .FirstOrDefault() as RequiredAttribute;

            // Assert
            Assert.NotNull(requiredAttribute);
        }

        [Fact]
        public void BitcoinRate_Note_DoesNotHaveRequiredAttribute()
        {
            // Arrange
            var noteProperty = typeof(BitcoinRate).GetProperty(nameof(BitcoinRate.Note));
            
            // Act
            var requiredAttribute = noteProperty?.GetCustomAttributes(typeof(RequiredAttribute), true)
                .FirstOrDefault() as RequiredAttribute;

            // Assert - Note should NOT be required
            Assert.Null(requiredAttribute);
        }

        #endregion

        #region ApplicationDbContext Tests

        [Fact]
        public void ApplicationDbContext_HasBitcoinRatesDbSet()
        {
            // Arrange
            var context = TestDbContextFactory.Create();

            // Act
            var dbSet = context.BitcoinRates;

            // Assert
            Assert.NotNull(dbSet);
        }

        [Fact]
        public void ApplicationDbContext_BitcoinRatesDbSet_IsOfCorrectType()
        {
            // Arrange
            var context = TestDbContextFactory.Create();

            // Assert
            Assert.IsAssignableFrom<DbSet<BitcoinRate>>(context.BitcoinRates);
        }

        #endregion

        #region ErrorViewModel Tests

        [Fact]
        public void ErrorViewModel_DefaultRequestId_IsNull()
        {
            // Arrange & Act
            var model = new ErrorViewModel();

            // Assert
            Assert.Null(model.RequestId);
        }

        [Fact]
        public void ErrorViewModel_CanSetRequestId()
        {
            // Arrange & Act
            var model = new ErrorViewModel { RequestId = "test-id" };

            // Assert
            Assert.Equal("test-id", model.RequestId);
        }

        [Fact]
        public void ErrorViewModel_ShowRequestId_IsTrue_WhenRequestIdIsSet()
        {
            // Arrange & Act
            var model = new ErrorViewModel { RequestId = "test-id" };

            // Assert
            Assert.True(model.ShowRequestId);
        }

        [Fact]
        public void ErrorViewModel_ShowRequestId_IsFalse_WhenRequestIdIsNull()
        {
            // Arrange & Act
            var model = new ErrorViewModel { RequestId = null };

            // Assert
            Assert.False(model.ShowRequestId);
        }

        [Fact]
        public void ErrorViewModel_ShowRequestId_IsFalse_WhenRequestIdIsEmpty()
        {
            // Arrange & Act
            var model = new ErrorViewModel { RequestId = "" };

            // Assert
            Assert.False(model.ShowRequestId);
        }

        #endregion
    }

    #region Test Helper Classes

    internal static class TestDbContextFactory
    {
        public static ApplicationDbContext Create(string databaseName = "TestDb")
        {
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;
            return new ApplicationDbContext(options);
        }
    }

    #endregion
}