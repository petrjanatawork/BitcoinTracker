using BitcoinTracker.Models;
using BitcoinTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace BitcoinTracker.Tests
{
    public class BitcoinServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<BitcoinService>> _mockLogger;
        private readonly IMemoryCache _cache;

        public BitcoinServiceTests()
        {
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object);
            _mockLogger = new Mock<ILogger<BitcoinService>>();
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        private ApplicationDbContext CreateInMemoryContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenContextIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new BitcoinService(null!, _httpClient, _mockLogger.Object, _cache));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenHttpClientIsNull()
        {
            using var context = CreateInMemoryContext("TestDb_Constructor_NullHttp");
            Assert.Throws<ArgumentNullException>(() =>
                new BitcoinService(context, null!, _mockLogger.Object, _cache));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            using var context = CreateInMemoryContext("TestDb_Constructor_NullLogger");
            Assert.Throws<ArgumentNullException>(() =>
                new BitcoinService(context, _httpClient, null!, _cache));
        }

        [Fact]
        public async Task GetCurrentRatesAsync_ReturnsBothRates_WhenBothApisSucceed()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"Data\":{\"BTC-EUR\":{\"PRICE\":45000.50}}}")
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("01.01.2024 #1\nZemě|měna|množství|kód|kurz\nEMU|euro|1|EUR|25.50\n")
                });

            using var context = CreateInMemoryContext("TestDb_Success");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            var result = await service.GetCurrentRatesAsync();

            // Assert
            Assert.Equal(45000.50m, result.EurPrice);
            Assert.Equal(Math.Round(45000.50m * 25.50m, 2), result.CzkPrice);
        }

        [Fact]
        public async Task GetCurrentRatesAsync_ReturnsNull_WhenCoinDeskFails()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            using var context = CreateInMemoryContext("TestDb_CoinDeskFail");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            var result = await service.GetCurrentRatesAsync();

            // Assert
            Assert.Null(result.EurPrice);
            Assert.Null(result.CzkPrice);
        }

        [Fact]
        public async Task GetCurrentRatesAsync_ReturnsNull_WhenEurCzkRateFails()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"Data\":{\"BTC-EUR\":{\"PRICE\":45000.50}}}")
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            using var context = CreateInMemoryContext("TestDb_EurCzkFail");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            var result = await service.GetCurrentRatesAsync();

            // Assert
            Assert.Null(result.EurPrice);
            Assert.Null(result.CzkPrice);
        }

        [Fact]
        public async Task GetCurrentRatesAsync_ReturnsNull_WhenCoinDeskResponseIsMalformed()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"invalid\": \"json\"}")
                });

            using var context = CreateInMemoryContext("TestDb_MalformedCoinDesk");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            var result = await service.GetCurrentRatesAsync();

            // Assert
            Assert.Null(result.EurPrice);
        }

        [Fact]
        public async Task GetEurCzkRateAsync_ReturnsNull_WhenFormatIsInvalid()
        {
            // Arrange
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Invalid format content")
                });

            using var context = CreateInMemoryContext("TestDb_InvalidFormat");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            var result = await service.GetCurrentRatesAsync();

            // Assert
            Assert.Null(result.CzkPrice);
        }

        [Fact]
        public async Task GetEurCzkRateAsync_HandlesCnbLineWithAmountGreaterThanOne()
        {
            // Arrange - CNB format where amount > 1 (e.g. JPY has amount=100)
            _mockHttpHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"Data\":{\"BTC-EUR\":{\"PRICE\":10000.00}}}")
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("01.01.2024 #1\nZemě|měna|množství|kód|kurz\nEMU|euro|1|EUR|25.50\nJaponsko|jen|100|JPY|15.80\n")
                });

            using var context = CreateInMemoryContext("TestDb_CnbAmount");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            var result = await service.GetCurrentRatesAsync();

            // Assert - should still find EUR with amount=1 correctly
            Assert.Equal(10000.00m, result.EurPrice);
            Assert.Equal(Math.Round(10000.00m * 25.50m, 2), result.CzkPrice);
        }

        [Fact]
        public async Task SaveRateAsync_SavesToDatabase()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_Save");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            var savedRate = await service.SaveRateAsync(45000.50m, 1100000m);

            // Assert
            Assert.NotEqual(0, savedRate.Id);
            Assert.Equal(1, await context.BitcoinRates.CountAsync());
            Assert.Equal(45000.50m, savedRate.PriceEur);
        }

        [Fact]
        public async Task GetSavedRatesAsync_ReturnsRatesOrderedByTimestampDescending()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_List");
            var now = DateTime.UtcNow;
            context.BitcoinRates.AddRange(
                new BitcoinRate { Timestamp = now.AddMinutes(-10), PriceEur = 100, PriceCzk = 2500 },
                new BitcoinRate { Timestamp = now, PriceEur = 200, PriceCzk = 5000 },
                new BitcoinRate { Timestamp = now.AddMinutes(-5), PriceEur = 150, PriceCzk = 3750 }
            );
            await context.SaveChangesAsync();

            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            var rates = await service.GetSavedRatesAsync();

            // Assert
            Assert.Equal(3, rates.Count);
            Assert.Equal(200, rates[0].PriceEur);
            Assert.Equal(150, rates[1].PriceEur);
            Assert.Equal(100, rates[2].PriceEur);
        }

        [Fact]
        public async Task GetSavedRatesAsync_UsesAsNoTracking()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_NoTracking");
            context.BitcoinRates.Add(new BitcoinRate { Timestamp = DateTime.UtcNow, PriceEur = 100, PriceCzk = 2500 });
            await context.SaveChangesAsync();

            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            var rates = await service.GetSavedRatesAsync();

            // Assert - AsNoTracking means modifying returned entities won't affect context
            Assert.Single(rates);
        }

        [Fact]
        public async Task UpdateNoteAsync_UpdatesNote()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_UpdateNote");
            var rate = new BitcoinRate { Timestamp = DateTime.UtcNow, PriceEur = 100, PriceCzk = 2500, Note = "Old" };
            context.BitcoinRates.Add(rate);
            await context.SaveChangesAsync();

            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            await service.UpdateNoteAsync(rate.Id, "New");

            // Assert
            var updatedRate = await context.BitcoinRates.FindAsync(rate.Id);
            Assert.Equal("New", updatedRate?.Note);
        }

        [Fact]
        public async Task UpdateNoteAsync_Throws_WhenNoteIsEmpty()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_UpdateNote_Empty");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateNoteAsync(1, ""));
        }

        [Fact]
        public async Task UpdateNoteAsync_Throws_WhenNoteIsWhitespace()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_UpdateNote_Whitespace");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateNoteAsync(1, "   "));
        }

        [Fact]
        public async Task UpdateNoteAsync_ThrowsKeyNotFound_WhenIdDoesNotExist()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_UpdateNote_NotFound");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateNoteAsync(999, "Some note"));
        }

        [Fact]
        public async Task DeleteRatesAsync_DeletesRates()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_Delete");
            context.BitcoinRates.AddRange(
                new BitcoinRate { Id = 1, Timestamp = DateTime.UtcNow, PriceEur = 100, PriceCzk = 2500 },
                new BitcoinRate { Id = 2, Timestamp = DateTime.UtcNow, PriceEur = 200, PriceCzk = 5000 },
                new BitcoinRate { Id = 3, Timestamp = DateTime.UtcNow, PriceEur = 300, PriceCzk = 7500 }
            );
            await context.SaveChangesAsync();

            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            await service.DeleteRatesAsync(new List<int> { 1, 3 });

            // Assert
            Assert.Equal(1, await context.BitcoinRates.CountAsync());
            Assert.Null(await context.BitcoinRates.FindAsync(1));
            Assert.NotNull(await context.BitcoinRates.FindAsync(2));
            Assert.Null(await context.BitcoinRates.FindAsync(3));
        }

        [Fact]
        public async Task DeleteRatesAsync_HandlesNonExistentIds()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_Delete_NonExistent");
            var rate = new BitcoinRate { Id = 1, Timestamp = DateTime.UtcNow, PriceEur = 100, PriceCzk = 2500 };
            context.BitcoinRates.Add(rate);
            await context.SaveChangesAsync();

            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            await service.DeleteRatesAsync(new List<int> { 1, 999 });

            // Assert
            Assert.Equal(0, await context.BitcoinRates.CountAsync());
        }

        [Fact]
        public async Task DeleteRatesAsync_ThrowsArgumentNullException_WhenIdsIsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_Delete_Null");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.DeleteRatesAsync(null!));
        }

        [Fact]
        public async Task DeleteRatesAsync_DoesNotThrow_WhenIdsIsEmpty()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_Delete_Empty");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act (should not throw)
            var exception = await Record.ExceptionAsync(() =>
                service.DeleteRatesAsync(new List<int>()));
            Assert.Null(exception);
        }

        [Fact]
        public async Task DeleteRatesAsync_DeletesOnlyExistingIds()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_Delete_Partial");
            context.BitcoinRates.AddRange(
                new BitcoinRate { Id = 1, Timestamp = DateTime.UtcNow, PriceEur = 100, PriceCzk = 2500 },
                new BitcoinRate { Id = 2, Timestamp = DateTime.UtcNow, PriceEur = 200, PriceCzk = 5000 }
            );
            await context.SaveChangesAsync();

            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act - delete only ID 1, but pass 1, 999
            await service.DeleteRatesAsync(new List<int> { 1, 999 });

            // Assert
            Assert.Single(await context.BitcoinRates.ToListAsync());
            Assert.NotNull(await context.BitcoinRates.FindAsync(2));
        }

        [Fact]
        public async Task GetCurrentRatesAsync_ReturnsCachedValue_WhenCacheIsValid()
        {
            // Arrange - seed the cache with a value
            _cache.Set("LiveBitcoinRate", (50000.00m, 1250000.00m), TimeSpan.FromSeconds(15));

            using var context = CreateInMemoryContext("TestDb_CacheHit");
            var service = new BitcoinService(context, _httpClient, _mockLogger.Object, _cache);

            // Act
            var result = await service.GetCurrentRatesAsync();

            // Assert - should return cached value, not call HTTP
            Assert.Equal(50000.00m, result.EurPrice);
            Assert.Equal(1250000.00m, result.CzkPrice);

            // Verify no HTTP calls were made
            _mockHttpHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
