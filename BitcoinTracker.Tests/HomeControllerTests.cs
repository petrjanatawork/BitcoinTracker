using BitcoinTracker.Controllers;
using BitcoinTracker.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;

namespace BitcoinTracker.Tests
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;

        public HomeControllerTests()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();
        }

        #region Index Tests

        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Arrange
            var controller = new HomeController(_mockLogger.Object);

            // Act
            var result = controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Index_ReturnsDefaultView()
        {
            // Arrange
            var controller = new HomeController(_mockLogger.Object);

            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.Null(result?.ViewName);
        }

        #endregion

        #region Privacy Tests

        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            // Arrange
            var controller = new HomeController(_mockLogger.Object);

            // Act
            var result = controller.Privacy();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Privacy_ReturnsDefaultView()
        {
            // Arrange
            var controller = new HomeController(_mockLogger.Object);

            // Act
            var result = controller.Privacy() as ViewResult;

            // Assert
            Assert.Null(result?.ViewName);
        }

        #endregion

        #region Error Tests

        [Fact]
        public void Error_ReturnsViewResult_WithErrorViewModel()
        {
            // Arrange
            var controller = new HomeController(_mockLogger.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.NotNull(model);
        }

        [Fact]
        public void Error_SetsRequestId_FromTraceIdentifier()
        {
            // Arrange
            var controller = new HomeController(_mockLogger.Object);
            
            // Set up a mock HttpContext with a trace identifier
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            httpContext.TraceIdentifier = "test-trace-id-12345";
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal("test-trace-id-12345", model.RequestId);
        }

        [Fact]
        public void Error_UsesActivityId_WhenTraceIdentifierIsNull()
        {
            // Arrange
            var controller = new HomeController(_mockLogger.Object);
            
            // Clear any existing activity
            Activity.Current = null;
            
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
#pragma warning disable CS8625
            httpContext.TraceIdentifier = null;
#pragma warning restore CS8625
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.NotNull(model.RequestId);
        }

        [Fact]
        public void Error_SetsRequestId_FromActivityCurrentId()
        {
            // Arrange
            var controller = new HomeController(_mockLogger.Object);
            
            // Set up an activity with a specific ID
            var activity = new Activity("TestActivity");
            activity.Start();
            var activityId = activity.Id;
            
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
#pragma warning disable CS8625
            httpContext.TraceIdentifier = null;
#pragma warning restore CS8625
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal(activityId, model.RequestId);
            
            activity.Stop();
        }

        [Fact]
        public void Error_HasNoStoreCacheAttribute()
        {
            // Arrange
            var controller = new HomeController(_mockLogger.Object);
            var methodInfo = typeof(HomeController).GetMethod(nameof(HomeController.Error));
            
            // Act
            var responseCacheAttribute = methodInfo?.GetCustomAttributes(
                typeof(ResponseCacheAttribute), true).FirstOrDefault() as ResponseCacheAttribute;

            // Assert
            Assert.NotNull(responseCacheAttribute);
            Assert.Equal(ResponseCacheLocation.None, responseCacheAttribute.Location);
            Assert.True(responseCacheAttribute.NoStore);
        }

        #endregion
    }
}