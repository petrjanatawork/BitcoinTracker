using BitcoinTracker.Controllers;
using BitcoinTracker.Models;
using BitcoinTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BitcoinTracker.Tests
{
    public class BitcoinApiControllerTests
    {
        private readonly Mock<IBitcoinService> _mockBitcoinService;

        public BitcoinApiControllerTests()
        {
            _mockBitcoinService = new Mock<IBitcoinService>();
        }

        #region GetLiveRate Tests

        [Fact]
        public async Task GetLiveRate_ReturnsOk_WhenRatesAvailable()
        {
            // Arrange
            _mockBitcoinService.Setup(s => s.GetCurrentRatesAsync())
                .ReturnsAsync((45000.50m, 1100000.25m));

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.GetLiveRate();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetLiveRate_Returns503_WhenRatesNotAvailable()
        {
            // Arrange
            _mockBitcoinService.Setup(s => s.GetCurrentRatesAsync())
                .ReturnsAsync((null, null));

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.GetLiveRate();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, statusResult.StatusCode);
        }

        #endregion

        #region SaveLiveRate Tests

        [Fact]
        public async Task SaveLiveRate_ReturnsCreated_WhenRatesAvailable()
        {
            // Arrange
            var savedRate = new BitcoinRate
            {
                Id = 1,
                Timestamp = DateTime.UtcNow,
                PriceEur = 45000.50m,
                PriceCzk = 1100000.25m
            };

            _mockBitcoinService.Setup(s => s.GetCurrentRatesAsync())
                .ReturnsAsync((45000.50m, 1100000.25m));
            _mockBitcoinService.Setup(s => s.SaveRateAsync(It.IsAny<decimal>(), It.IsAny<decimal>()))
                .ReturnsAsync(savedRate);

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.SaveLiveRate();

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(BitcoinApiController.GetSavedRates), createdResult.ActionName);
        }

        [Fact]
        public async Task SaveLiveRate_Returns503_WhenRatesNotAvailable()
        {
            // Arrange
            _mockBitcoinService.Setup(s => s.GetCurrentRatesAsync())
                .ReturnsAsync((null, null));

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.SaveLiveRate();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, statusResult.StatusCode);
        }

        #endregion

        #region GetSavedRates Tests

        [Fact]
        public async Task GetSavedRates_ReturnsAllRates()
        {
            // Arrange
            var rates = new List<BitcoinRate>
            {
                new BitcoinRate { Id = 1, Timestamp = DateTime.UtcNow, PriceEur = 45000, PriceCzk = 1100000 },
                new BitcoinRate { Id = 2, Timestamp = DateTime.UtcNow.AddHours(-1), PriceEur = 44000, PriceCzk = 1078000 }
            };

            _mockBitcoinService.Setup(s => s.GetSavedRatesAsync())
                .ReturnsAsync(rates);

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.GetSavedRates();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRates = Assert.IsAssignableFrom<List<BitcoinRate>>(okResult.Value);
            Assert.Equal(2, returnedRates.Count);
        }

        [Fact]
        public async Task GetSavedRates_ReturnsEmptyList_WhenNoRates()
        {
            // Arrange
            _mockBitcoinService.Setup(s => s.GetSavedRatesAsync())
                .ReturnsAsync(new List<BitcoinRate>());

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.GetSavedRates();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRates = Assert.IsAssignableFrom<List<BitcoinRate>>(okResult.Value);
            Assert.Empty(returnedRates);
        }

        #endregion

        #region UpdateNote Tests

        [Fact]
        public async Task UpdateNote_ReturnsOk_WhenUpdateSucceeds()
        {
            // Arrange
            _mockBitcoinService.Setup(s => s.UpdateNoteAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.UpdateNote(1, new UpdateNoteRequest("Updated note"));

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task UpdateNote_ReturnsBadRequest_WhenNoteIsEmpty()
        {
            // Arrange
            _mockBitcoinService.Setup(s => s.UpdateNoteAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Poznámka nesmí být prázdná."));

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.UpdateNote(1, new UpdateNoteRequest(""));

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateNote_ReturnsNotFound_WhenIdDoesNotExist()
        {
            // Arrange
            _mockBitcoinService.Setup(s => s.UpdateNoteAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new KeyNotFoundException("Not found"));

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.UpdateNote(999, new UpdateNoteRequest("Note"));

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateNote_Returns500_WhenGenericExceptionOccurs()
        {
            // Arrange
            _mockBitcoinService.Setup(s => s.UpdateNoteAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Generic error"));

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.UpdateNote(1, new UpdateNoteRequest("Note"));

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region DeleteRate Tests

        [Fact]
        public async Task DeleteRate_ReturnsNoContent_WhenDeleteSucceeds()
        {
            // Arrange
            _mockBitcoinService.Setup(s => s.DeleteRatesAsync(It.IsAny<List<int>>()))
                .Returns(Task.CompletedTask);

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.DeleteRate(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteRate_CallsDeleteWithCorrectId()
        {
            // Arrange
            List<int>? capturedIds = null;
            _mockBitcoinService.Setup(s => s.DeleteRatesAsync(It.IsAny<List<int>>()))
                .Callback<List<int>>(ids => capturedIds = ids)
                .Returns(Task.CompletedTask);

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            await controller.DeleteRate(42);

            // Assert
            Assert.NotNull(capturedIds);
            Assert.Single(capturedIds);
            Assert.Contains(42, capturedIds);
        }

        #endregion

        #region DeleteRatesBatch Tests

        [Fact]
        public async Task DeleteRatesBatch_ReturnsNoContent_WhenDeleteSucceeds()
        {
            // Arrange
            _mockBitcoinService.Setup(s => s.DeleteRatesAsync(It.IsAny<List<int>>()))
                .Returns(Task.CompletedTask);

            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.DeleteRatesBatch(new DeleteRatesRequest(new List<int> { 1, 2, 3 }));

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteRatesBatch_ReturnsBadRequest_WhenIdsAreNull()
        {
            // Arrange
            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.DeleteRatesBatch(new DeleteRatesRequest(null!));

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteRatesBatch_ReturnsBadRequest_WhenIdsAreEmpty()
        {
            // Arrange
            var controller = new BitcoinApiController(_mockBitcoinService.Object);

            // Act
            var result = await controller.DeleteRatesBatch(new DeleteRatesRequest(new List<int>()));

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion
    }
}
