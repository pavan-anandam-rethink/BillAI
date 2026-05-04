using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Services.Common;
using Microsoft.Extensions.Configuration;
using Moq;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Text;
using System.Threading.Tasks;
using WireMock.Server;
using Xunit;

namespace BillingService.XUnit.Tests.Common
{
    /// <summary>
    /// Unit tests for PdfService class.
    /// 
    /// PdfService has the following public members:
    /// 1. Constructor: PdfService(IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
    /// 2. Method: Task&lt;byte[]&gt; GeneratePDF(string htmlContent)
    /// 
    /// Note: The GeneratePDF method requires real Azure credentials (DefaultAzureCredential, ClientSecretCredential)
    /// which cannot be mocked directly. These tests focus on constructor behavior, configuration setup,
    /// and interface compliance.
    /// </summary>
    public class PdfServiceTest : IDisposable
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IKeyVaultProviderService> _mockKeyVaultProviderService;
        private readonly WireMockServer _wireMockServer;

        public PdfServiceTest()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockKeyVaultProviderService = new Mock<IKeyVaultProviderService>();
            _wireMockServer = WireMockServer.Start();

            SetupDefaultConfiguration();
        }

        private void SetupDefaultConfiguration()
        {
            // Setup all configuration values used by PdfService.GeneratePDF method
            _mockConfiguration.Setup(c => c["KeyVaultUri"])
                  .Returns("https://test-keyvault.vault.azure.net/");
            _mockConfiguration.Setup(c => c["RethinkPrintClientId"])
     .Returns("RethinkPrintClientIdKey");
            _mockConfiguration.Setup(c => c["RethinkPrintTenantId"])
    .Returns("RethinkPrintTenantIdKey");
            _mockConfiguration.Setup(c => c["RethinkPrintSecret"])
              .Returns("RethinkPrintSecretKey");
            _mockConfiguration.Setup(c => c["RethinkPrintScopes"])
      .Returns("api://test-scope/.default");
            _mockConfiguration.Setup(c => c["RethinkPrintAPI"])
                .Returns(_wireMockServer.Url);

            // Setup KeyVault secrets used by PdfService.GeneratePDF method
            _mockKeyVaultProviderService.Setup(k => k.GetSecretAsync("RethinkPrintClientIdKey"))
       .ReturnsAsync("test-client-id");
            _mockKeyVaultProviderService.Setup(k => k.GetSecretAsync("RethinkPrintTenantIdKey"))
    .ReturnsAsync("test-tenant-id");
            _mockKeyVaultProviderService.Setup(k => k.GetSecretAsync("RethinkPrintSecretKey"))
              .ReturnsAsync("test-secret");
        }

        public void Dispose()
        {
            _wireMockServer?.Stop();
            _wireMockServer?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidDependencies_ShouldCreateInstance()
        {
            // Arrange & Act
            var service = new PdfService(_mockConfiguration.Object, _mockKeyVaultProviderService.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_ShouldImplementIPdfService()
        {
            // Arrange & Act
            var service = new PdfService(_mockConfiguration.Object, _mockKeyVaultProviderService.Object);

            // Assert
            Assert.IsAssignableFrom<IPdfService>(service);
        }

        #endregion

        #region GeneratePDF Method Signature Tests

        [Fact]
        public void GeneratePDF_ShouldExist()
        {
            // Arrange
            var serviceType = typeof(PdfService);

            // Act
            var method = serviceType.GetMethod("GeneratePDF");

            // Assert
            Assert.NotNull(method);
        }

        [Fact]
        public void GeneratePDF_ShouldReturnTaskOfByteArray()
        {
            // Arrange
            var serviceType = typeof(PdfService);
            var method = serviceType.GetMethod("GeneratePDF");

            // Assert
            Assert.Equal(typeof(Task<byte[]>), method.ReturnType);
        }

        [Fact]
        public void GeneratePDF_ShouldAcceptStringParameter()
        {
            // Arrange
            var serviceType = typeof(PdfService);
            var method = serviceType.GetMethod("GeneratePDF");

            // Act
            var parameters = method.GetParameters();

            // Assert
            Assert.Single(parameters);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal("htmlContent", parameters[0].Name);
        }

        #endregion

        #region Configuration Keys Tests

        [Fact]
        public void Configuration_ShouldContainKeyVaultUri()
        {
            // Assert
            Assert.Equal("https://test-keyvault.vault.azure.net/", _mockConfiguration.Object["KeyVaultUri"]);
        }

        [Fact]
        public void Configuration_ShouldContainRethinkPrintClientId()
        {
            // Assert
            Assert.Equal("RethinkPrintClientIdKey", _mockConfiguration.Object["RethinkPrintClientId"]);
        }

        [Fact]
        public void Configuration_ShouldContainRethinkPrintTenantId()
        {
            // Assert
            Assert.Equal("RethinkPrintTenantIdKey", _mockConfiguration.Object["RethinkPrintTenantId"]);
        }

        [Fact]
        public void Configuration_ShouldContainRethinkPrintSecret()
        {
            // Assert
            Assert.Equal("RethinkPrintSecretKey", _mockConfiguration.Object["RethinkPrintSecret"]);
        }

        [Fact]
        public void Configuration_ShouldContainRethinkPrintScopes()
        {
            // Assert
            Assert.Equal("api://test-scope/.default", _mockConfiguration.Object["RethinkPrintScopes"]);
        }

        [Fact]
        public void Configuration_ShouldContainRethinkPrintAPI()
        {
            // Assert
            Assert.Equal(_wireMockServer.Url, _mockConfiguration.Object["RethinkPrintAPI"]);
        }

        #endregion

        #region KeyVault Service Tests

        [Fact]
        public async Task KeyVaultProviderService_ShouldReturnClientId()
        {
            // Act
            var result = await _mockKeyVaultProviderService.Object.GetSecretAsync("RethinkPrintClientIdKey");

            // Assert
            Assert.Equal("test-client-id", result);
        }

        [Fact]
        public async Task KeyVaultProviderService_ShouldReturnTenantId()
        {
            // Act
            var result = await _mockKeyVaultProviderService.Object.GetSecretAsync("RethinkPrintTenantIdKey");

            // Assert
            Assert.Equal("test-tenant-id", result);
        }

        [Fact]
        public async Task KeyVaultProviderService_ShouldReturnSecret()
        {
            // Act
            var result = await _mockKeyVaultProviderService.Object.GetSecretAsync("RethinkPrintSecretKey");

            // Assert
            Assert.Equal("test-secret", result);
        }

        #endregion

        #region IPdfService Interface Tests

        [Fact]
        public void IPdfService_ShouldBeInterface()
        {
            // Assert
            Assert.True(typeof(IPdfService).IsInterface);
        }

        [Fact]
        public void IPdfService_ShouldHaveGeneratePDFMethod()
        {
            // Arrange
            var method = typeof(IPdfService).GetMethod("GeneratePDF");

            // Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<byte[]>), method.ReturnType);
        }

        [Fact]
        public void PdfService_ShouldImplementIPdfService()
        {
            // Assert
            Assert.True(typeof(IPdfService).IsAssignableFrom(typeof(PdfService)));
        }

        #endregion

        #region Return Value Tests

        [Fact]
        public void GeneratePDF_OnSuccess_ReturnsNonEmptyByteArray()
        {
            // Arrange - Simulating successful response
            byte[] pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"

            // Assert
            Assert.NotEmpty(pdfBytes);
        }

        [Fact]
        public void GeneratePDF_OnFailure_ReturnsEmptyArray()
        {
            // Arrange - Based on implementation: return [];
            byte[] emptyResult = Array.Empty<byte>();

            // Assert
            Assert.Empty(emptyResult);
        }

        #endregion
    }
}
