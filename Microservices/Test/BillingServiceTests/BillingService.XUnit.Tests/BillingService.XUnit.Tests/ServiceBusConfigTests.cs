using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillingService.Domain;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rethink.Services.Common.Infrastructure.Connection;
using Rethink.Services.Domain.Interfaces;
using Xunit;

namespace BillingService.XUnit.Tests
{
    public class ServiceBusConfigTests
    {
        private const string ConfigKey = "ConnectionStrings:ServiceBus:ConnectionString";
        private const string SecretName = "KeyVaultSecretName";

        private const string ValidSbConnectionString =
            "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";

        #region ConfigureServiceBus

        [Fact]
        public void ConfigureServiceBus_ShouldCallKeyVault_WithConfigurationValue_AndRegisterFactory_AndReturnBuilder()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    [ConfigKey] = SecretName
                })
                .Build();

            var services = new ServiceCollection();

            var keyVault = new Mock<IKeyVaultProviderService>(MockBehavior.Strict);
            keyVault.Setup(x => x.GetSecretAsync(SecretName))
                .ReturnsAsync(ValidSbConnectionString);

            // Act
            var builder = ServiceBusConfig.ConfigureServiceBus(services, configuration, keyVault.Object);

            // Assert: KeyVault called correctly
            keyVault.Verify(x => x.GetSecretAsync(SecretName), Times.Once);

            // Assert: Returned builder exists + endpoint set (Endpoint is string in your version)
            Assert.NotNull(builder);
            Assert.IsType<ServiceBusConnectionStringBuilder>(builder);
            Assert.False(string.IsNullOrWhiteSpace(builder.Endpoint));

            // Assert: Don’t compare raw ConnectionString (it gets normalized by the builder).
            // Instead, verify important tokens are present after normalization.
            var normalized = builder.ToString(); // safest across versions
            Assert.Contains("Endpoint=", normalized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SharedAccessKeyName=", normalized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SharedAccessKey=", normalized, StringComparison.OrdinalIgnoreCase);

            // Assert: DI registration works
            using var provider = services.BuildServiceProvider();
            var resolvedFactory = provider.GetService<IServiceBusConnectionFactory>();
            Assert.NotNull(resolvedFactory);
        }

        [Fact]
        public void ConfigureServiceBus_WhenConfigValueIsMissing_ShouldStillCallKeyVault_WithNullKey()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()) // missing => configuration[ConfigKey] == null
                .Build();

            var services = new ServiceCollection();

            var keyVault = new Mock<IKeyVaultProviderService>(MockBehavior.Strict);
            keyVault.Setup(x => x.GetSecretAsync(null))
                // return valid so method completes; we are asserting the call arg
                .ReturnsAsync(ValidSbConnectionString);

            // Act
            var builder = ServiceBusConfig.ConfigureServiceBus(services, configuration, keyVault.Object);

            // Assert
            keyVault.Verify(x => x.GetSecretAsync(null), Times.Once);
            Assert.NotNull(builder);
        }

        [Fact]
        public void ConfigureServiceBus_WhenKeyVaultReturnsNullSecret_ShouldNotThrow_DocumentsCurrentBehavior()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    [ConfigKey] = SecretName
                })
                .Build();

            var services = new ServiceCollection();

            var keyVault = new Mock<IKeyVaultProviderService>(MockBehavior.Strict);
            keyVault.Setup(x => x.GetSecretAsync(SecretName))
                .ReturnsAsync((string)null);

            // Act
            var builder = ServiceBusConfig.ConfigureServiceBus(services, configuration, keyVault.Object);

            // Assert (documents current production behavior: no explicit validation)
            keyVault.Verify(x => x.GetSecretAsync(SecretName), Times.Once);
            Assert.NotNull(builder);
        }

        [Fact]
        public void ConfigureServiceBus_WhenKeyVaultReturnsEmptySecret_ShouldNotThrow_DocumentsCurrentBehavior()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    [ConfigKey] = SecretName
                })
                .Build();

            var services = new ServiceCollection();

            var keyVault = new Mock<IKeyVaultProviderService>(MockBehavior.Strict);
            keyVault.Setup(x => x.GetSecretAsync(SecretName))
                .ReturnsAsync(string.Empty);

            // Act
            var builder = ServiceBusConfig.ConfigureServiceBus(services, configuration, keyVault.Object);

            // Assert (documents current production behavior: no explicit validation)
            keyVault.Verify(x => x.GetSecretAsync(SecretName), Times.Once);
            Assert.NotNull(builder);
        }

        #endregion

        #region CreateQueueIfNotExists

        [Fact]
        public async Task CreateQueueIfNotExists_WhenQueueDoesNotExist_ShouldCreateQueue()
        {
            // Arrange
            var queuePath = "new-queue";
            var fake = new FakeManagementClient(existingQueuePaths: new[] { "existing-1", "existing-2" });

            // Act
            await ServiceBusConfig.CreateQueueIfNotExists(fake, queuePath);

            // Assert
            Assert.Equal(1, fake.GetQueuesCallCount);
            Assert.Equal(1, fake.CreateQueueCallCount);
            Assert.Equal(queuePath, fake.LastCreatedQueuePath);
        }

        [Fact]
        public async Task CreateQueueIfNotExists_WhenQueueExists_ShouldNotCreateQueue()
        {
            // Arrange
            var queuePath = "existing-queue";
            var fake = new FakeManagementClient(existingQueuePaths: new[] { queuePath, "other-queue" });

            // Act
            await ServiceBusConfig.CreateQueueIfNotExists(fake, queuePath);

            // Assert
            Assert.Equal(1, fake.GetQueuesCallCount);
            Assert.Equal(0, fake.CreateQueueCallCount);
            Assert.Null(fake.LastCreatedQueuePath);
        }

        [Fact]
        public async Task CreateQueueIfNotExists_WhenMgmtClientIsNull_ShouldThrow()
        {
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                ServiceBusConfig.CreateQueueIfNotExists(null, "any-queue"));
        }

        #endregion

        /// <summary>
        /// Fake ManagementClient to unit test CreateQueueIfNotExists without hitting Azure.
        /// Matches your library signature: GetQueuesAsync returns Task&lt;IList&lt;QueueDescription&gt;&gt;.
        /// </summary>
        private sealed class FakeManagementClient : ManagementClient
        {
            private readonly IList<QueueDescription> _queues;

            public int GetQueuesCallCount { get; private set; }
            public int CreateQueueCallCount { get; private set; }
            public string LastCreatedQueuePath { get; private set; }

            private const string DummyConnectionString =
                "Endpoint=sb://dummy.servicebus.windows.net/;SharedAccessKeyName=dummy;SharedAccessKey=dummy";

            public FakeManagementClient(IEnumerable<string> existingQueuePaths)
                : base(DummyConnectionString)
            {
                _queues = (existingQueuePaths ?? Enumerable.Empty<string>())
                    .Select(p => new QueueDescription(p))
                    .ToList();
            }

            public override Task<IList<QueueDescription>> GetQueuesAsync(
                int top = 100,
                int skip = 0,
                CancellationToken cancellationToken = default)
            {
                GetQueuesCallCount++;
                return Task.FromResult(_queues);
            }

            public override Task<QueueDescription> CreateQueueAsync(
                string path,
                CancellationToken cancellationToken = default)
            {
                CreateQueueCallCount++;
                LastCreatedQueuePath = path;

                var created = new QueueDescription(path);
                _queues.Add(created);

                return Task.FromResult(created);
            }
        }
    }
}
