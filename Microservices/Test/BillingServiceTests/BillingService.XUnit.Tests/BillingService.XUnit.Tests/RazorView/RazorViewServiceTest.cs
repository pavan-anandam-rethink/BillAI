using BillingService.Domain.Interfaces;
using BillingService.Domain.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.RazorView
{
    public class RazorViewServiceTest
    {
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<IServiceProvider> _serviceProvider;
        private readonly Mock<IHostingEnvironment> _hostingEnvironment;
        private readonly Mock<IRazorViewEngine> _razorViewEngine;
        private readonly Mock<ITempDataProvider> _tempDataProvider;
        private readonly RazorViewService _razorViewService;

        public RazorViewServiceTest()
        {
            _configuration = new Mock<IConfiguration>();
            _serviceProvider = new Mock<IServiceProvider>();
            _hostingEnvironment = new Mock<IHostingEnvironment>();
            _razorViewEngine = new Mock<IRazorViewEngine>();
            _tempDataProvider = new Mock<ITempDataProvider>();

            var baseTemplatePathSection = new Mock<IConfigurationSection>();
            baseTemplatePathSection.Setup(x => x.Value).Returns("/Views/Templates/");

            _configuration.Setup(x => x.GetSection("BaseTemplatePath"))
                .Returns(baseTemplatePathSection.Object);

            _razorViewService = new RazorViewService(
                _configuration.Object,
                _serviceProvider.Object,
                _hostingEnvironment.Object,
                _razorViewEngine.Object,
                _tempDataProvider.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeAllDependencies()
        {
            // Assert
            Assert.NotNull(_razorViewService);
        }

        [Fact]
        public async Task RenderViewToStringAsync_ShouldReturnRenderedString_WhenGetViewSucceeds()
        {
            // Arrange
            var viewName = "TestView";
            var model = new TestViewModel { Name = "Test" };
            var expectedOutput = "Rendered Content";
            var viewPath = $"/Views/Templates/{viewName}/{viewName}.cshtml";

            var mockView = new Mock<IView>();
            mockView.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback<ViewContext>(vc => vc.Writer.Write(expectedOutput))
                .Returns(Task.CompletedTask);

            var viewEngineResult = ViewEngineResult.Found(viewPath, mockView.Object);
            _razorViewEngine.Setup(x => x.GetView(viewPath, viewPath, false))
                .Returns(viewEngineResult);

            // Act
            var result = await _razorViewService.RenderViewToStringAsync(viewName, model);

            // Assert
            Assert.Equal(expectedOutput, result);
            _razorViewEngine.Verify(x => x.GetView(viewPath, viewPath, false), Times.Once);
        }

        [Fact]
        public async Task RenderViewToStringAsync_ShouldUseFindView_WhenGetViewFails()
        {
            // Arrange
            var viewName = "FallbackView";
            var model = new TestViewModel { Name = "Fallback" };
            var expectedOutput = "Fallback Output";
            var viewPath = $"/Views/Templates/{viewName}/{viewName}.cshtml";

            var getViewResult = ViewEngineResult.NotFound(viewPath, new[] { "search1" });
            _razorViewEngine.Setup(x => x.GetView(viewPath, viewPath, false))
                .Returns(getViewResult);

            var mockView = new Mock<IView>();
            mockView.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback<ViewContext>(vc => vc.Writer.Write(expectedOutput))
                .Returns(Task.CompletedTask);

            var findViewResult = ViewEngineResult.Found(viewPath, mockView.Object);
            _razorViewEngine.Setup(x => x.FindView(It.IsAny<ActionContext>(), viewPath, true))
                .Returns(findViewResult);

            // Act
            var result = await _razorViewService.RenderViewToStringAsync(viewName, model);

            // Assert
            Assert.Equal(expectedOutput, result);
            _razorViewEngine.Verify(x => x.GetView(viewPath, viewPath, false), Times.Once);
            _razorViewEngine.Verify(x => x.FindView(It.IsAny<ActionContext>(), viewPath, true), Times.Once);
        }

        [Fact]
        public async Task RenderViewToStringAsync_ShouldThrowInvalidOperationException_WhenViewNotFound()
        {
            // Arrange
            var viewName = "NonExistentView";
            var model = new TestViewModel { Name = "Missing" };
            var viewPath = $"/Views/Templates/{viewName}/{viewName}.cshtml";

            var getViewResult = ViewEngineResult.NotFound(viewPath, new[] { "location1", "location2" });
            _razorViewEngine.Setup(x => x.GetView(viewPath, viewPath, false))
                .Returns(getViewResult);

            var findViewResult = ViewEngineResult.NotFound(viewPath, new[] { "location3" });
            _razorViewEngine.Setup(x => x.FindView(It.IsAny<ActionContext>(), viewPath, true))
                .Returns(findViewResult);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _razorViewService.RenderViewToStringAsync(viewName, model));

            Assert.Contains($"Unable to find view '{viewName}'", exception.Message);
            Assert.Contains("location1", exception.Message);
            Assert.Contains("location2", exception.Message);
            Assert.Contains("location3", exception.Message);
        }

        [Fact]
        public async Task RenderViewToStringAsync_ShouldReturnEmptyString_WhenViewRendersNothing()
        {
            // Arrange
            var viewName = "EmptyView";
            var model = new TestViewModel();
            var viewPath = $"/Views/Templates/{viewName}/{viewName}.cshtml";

            var mockView = new Mock<IView>();
            mockView.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Returns(Task.CompletedTask);

            var viewEngineResult = ViewEngineResult.Found(viewPath, mockView.Object);
            _razorViewEngine.Setup(x => x.GetView(viewPath, viewPath, false))
                .Returns(viewEngineResult);

            // Act
            var result = await _razorViewService.RenderViewToStringAsync(viewName, model);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task RenderViewToStringAsync_ShouldPassModelToViewContext()
        {
            // Arrange
            var viewName = "ModelView";
            var model = new TestViewModel { Name = "ModelTest", Value = 42 };
            var viewPath = $"/Views/Templates/{viewName}/{viewName}.cshtml";

            ViewContext capturedViewContext = null;
            var mockView = new Mock<IView>();
            mockView.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback<ViewContext>(vc => capturedViewContext = vc)
                .Returns(Task.CompletedTask);

            var viewEngineResult = ViewEngineResult.Found(viewPath, mockView.Object);
            _razorViewEngine.Setup(x => x.GetView(viewPath, viewPath, false))
                .Returns(viewEngineResult);

            // Act
            await _razorViewService.RenderViewToStringAsync(viewName, model);

            // Assert
            Assert.NotNull(capturedViewContext);
            Assert.Equal(model, capturedViewContext.ViewData.Model);
        }

        [Fact]
        public async Task RenderViewToStringAsync_ShouldWorkWithNullModel()
        {
            // Arrange
            var viewName = "NullModelView";
            var viewPath = $"/Views/Templates/{viewName}/{viewName}.cshtml";

            var mockView = new Mock<IView>();
            mockView.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback<ViewContext>(vc => vc.Writer.Write("null model"))
                .Returns(Task.CompletedTask);

            var viewEngineResult = ViewEngineResult.Found(viewPath, mockView.Object);
            _razorViewEngine.Setup(x => x.GetView(viewPath, viewPath, false))
                .Returns(viewEngineResult);

            // Act
            var result = await _razorViewService.RenderViewToStringAsync<TestViewModel>(viewName, null);

            // Assert
            Assert.Equal("null model", result);
        }

        [Fact]
        public async Task RenderViewToStringAsync_ShouldCreateActionContextWithServiceProvider()
        {
            // Arrange
            var viewName = "ContextView";
            var model = new TestViewModel();
            var viewPath = $"/Views/Templates/{viewName}/{viewName}.cshtml";

            ViewContext capturedViewContext = null;
            var mockView = new Mock<IView>();
            mockView.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback<ViewContext>(vc => capturedViewContext = vc)
                .Returns(Task.CompletedTask);

            var viewEngineResult = ViewEngineResult.Found(viewPath, mockView.Object);
            _razorViewEngine.Setup(x => x.GetView(viewPath, viewPath, false))
                .Returns(viewEngineResult);

            // Act
            await _razorViewService.RenderViewToStringAsync(viewName, model);

            // Assert
            Assert.NotNull(capturedViewContext);
            Assert.NotNull(capturedViewContext.HttpContext);
            Assert.Equal(_serviceProvider.Object, capturedViewContext.HttpContext.RequestServices);
        }

        [Fact]
        public async Task RenderViewToStringAsync_ShouldUseBaseTemplatePathFromConfiguration()
        {
            // Arrange
            var customConfig = new Mock<IConfiguration>();
            var customSection = new Mock<IConfigurationSection>();
            customSection.Setup(x => x.Value).Returns("/Custom/Path/");
            customConfig.Setup(x => x.GetSection("BaseTemplatePath")).Returns(customSection.Object);

            var service = new RazorViewService(
                customConfig.Object,
                _serviceProvider.Object,
                _hostingEnvironment.Object,
                _razorViewEngine.Object,
                _tempDataProvider.Object);

            var viewName = "CustomView";
            var expectedViewPath = $"/Custom/Path/{viewName}/{viewName}.cshtml";

            var mockView = new Mock<IView>();
            mockView.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Returns(Task.CompletedTask);

            var viewEngineResult = ViewEngineResult.Found(expectedViewPath, mockView.Object);
            _razorViewEngine.Setup(x => x.GetView(expectedViewPath, expectedViewPath, false))
                .Returns(viewEngineResult);

            // Act
            await service.RenderViewToStringAsync(viewName, new TestViewModel());

            // Assert
            _razorViewEngine.Verify(x => x.GetView(expectedViewPath, expectedViewPath, false), Times.Once);
        }

        [Fact]
        public async Task RenderViewToStringAsync_ErrorMessage_ShouldConcatenateAllSearchedLocations()
        {
            // Arrange
            var viewName = "MissingView";
            var viewPath = $"/Views/Templates/{viewName}/{viewName}.cshtml";

            var getViewSearched = new[] { "pathA", "pathB" };
            var findViewSearched = new[] { "pathC", "pathD" };

            var getViewResult = ViewEngineResult.NotFound(viewPath, getViewSearched);
            var findViewResult = ViewEngineResult.NotFound(viewPath, findViewSearched);

            _razorViewEngine.Setup(x => x.GetView(viewPath, viewPath, false)).Returns(getViewResult);
            _razorViewEngine.Setup(x => x.FindView(It.IsAny<ActionContext>(), viewPath, true)).Returns(findViewResult);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _razorViewService.RenderViewToStringAsync(viewName, new TestViewModel()));

            // Assert - all 4 locations should be in error message
            Assert.Contains("pathA", ex.Message);
            Assert.Contains("pathB", ex.Message);
            Assert.Contains("pathC", ex.Message);
            Assert.Contains("pathD", ex.Message);
        }

        [Fact]
        public async Task RenderViewToStringAsync_ShouldRenderMultiLineOutput()
        {
            // Arrange
            var viewName = "MultiLineView";
            var model = new TestViewModel { Name = "Multi" };
            var viewPath = $"/Views/Templates/{viewName}/{viewName}.cshtml";
            var expectedOutput = "Line1\nLine2\nLine3";

            var mockView = new Mock<IView>();
            mockView.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback<ViewContext>(vc => vc.Writer.Write(expectedOutput))
                .Returns(Task.CompletedTask);

            var viewEngineResult = ViewEngineResult.Found(viewPath, mockView.Object);
            _razorViewEngine.Setup(x => x.GetView(viewPath, viewPath, false))
                .Returns(viewEngineResult);

            // Act
            var result = await _razorViewService.RenderViewToStringAsync(viewName, model);

            // Assert
            Assert.Equal(expectedOutput, result);
        }

        public class TestViewModel
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}
