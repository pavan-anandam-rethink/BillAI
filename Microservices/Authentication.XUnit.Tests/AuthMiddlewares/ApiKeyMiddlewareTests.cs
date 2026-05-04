using Authentication.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Moq;
using Rethink.Services.Domain.Interfaces;
using System.Text;

namespace Authentication.XUnit.Tests.AuthMiddlewares
{

    public class ApiKeyMiddlewareTests
    {
        private const string ApiKeyHeader = "XApiKey";
        private const string ValidApiKey = "valid-key";

        private static DefaultHttpContext CreateHttpContext(bool allowAnonymous = false)
        {
            var context = new DefaultHttpContext();

            var endpoint = new Endpoint(
                _ => Task.CompletedTask,
                allowAnonymous
                    ? new EndpointMetadataCollection(new AllowAnonymousAttribute())
                    : new EndpointMetadataCollection(),
                "TestEndpoint");

            context.SetEndpoint(endpoint);
            context.Response.Body = new MemoryStream();

            return context;
        }

        private static ApiKeyMiddleware CreateMiddleware(
            RequestDelegate next,
            string configuredApiKey)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                        { "XApiKey", "VaultKeyName" }
                })
                .Build();

            var keyVaultMock = new Mock<IKeyVaultProviderService>();
            keyVaultMock
                .Setup(x => x.GetSecretAsync("VaultKeyName"))
                .ReturnsAsync(configuredApiKey);

            return new ApiKeyMiddleware(next, config, keyVaultMock.Object);
        }

        private static async Task<string> ReadResponseBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(response.Body, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        [Fact]
        public async Task Invoke_Allows_Request_When_AllowAnonymous()
        {
            // Arrange
            var context = CreateHttpContext(allowAnonymous: true);

            bool nextCalled = false;

            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = CreateMiddleware(next, ValidApiKey);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_Returns_401_When_ApiKey_Missing()
        {
            // Arrange
            var context = CreateHttpContext();
            var middleware = CreateMiddleware(_ => Task.CompletedTask, ValidApiKey);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(401, context.Response.StatusCode);
            var body = await ReadResponseBody(context.Response);
            Assert.Equal("API Key was not provided ", body);
        }

        [Fact]
        public async Task Invoke_Returns_401_When_ApiKey_Invalid()
        {
            // Arrange
            var context = CreateHttpContext();
            context.Request.Headers.Add(ApiKeyHeader, "invalid-key");

            var middleware = CreateMiddleware(_ => Task.CompletedTask, ValidApiKey);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(401, context.Response.StatusCode);
            var body = await ReadResponseBody(context.Response);
            Assert.Equal("XApiKey is not a valid", body);
        }

        [Fact]
        public async Task Invoke_Returns_401_When_Multiple_ApiKeys_Present()
        {
            // Arrange
            var context = CreateHttpContext();
            context.Request.Headers.Add(ApiKeyHeader, new StringValues(new[] { "key1", "key2" }));

            var middleware = CreateMiddleware(_ => Task.CompletedTask, ValidApiKey);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(401, context.Response.StatusCode);
            var body = await ReadResponseBody(context.Response);
            Assert.Equal("Request returned multiple headers for XApiKey", body);
        }

        [Fact]
        public async Task Invoke_Calls_Next_When_ApiKey_Valid()
        {
            // Arrange
            var context = CreateHttpContext();
            context.Request.Headers.Add(ApiKeyHeader, ValidApiKey);

            bool nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = CreateMiddleware(next, ValidApiKey);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public void ValidateApiKey_Returns_Error_When_Whitespace()
        {
            // Arrange
            var middleware = CreateMiddleware(_ => Task.CompletedTask, ValidApiKey);

            // Act
            var result = middleware.ValidateApiKey(new StringValues(" "));

            // Assert
            Assert.Equal("XApiKey is null or whitespace", result);
        }

        [Fact]
        public void ValidateApiKey_Returns_Error_When_Multiple_Keys()
        {
            // Arrange
            var middleware = CreateMiddleware(_ => Task.CompletedTask, ValidApiKey);

            // Act
            var result = middleware.ValidateApiKey(new StringValues(new[] { "key1", "key2" }));

            // Assert
            Assert.Equal("Request returned multiple headers for XApiKey", result);
        }

        [Fact]
        public void ValidateApiKey_Returns_Error_When_Invalid_Key()
        {
            // Arrange
            var middleware = CreateMiddleware(_ => Task.CompletedTask, ValidApiKey);

            // Act
            var result = middleware.ValidateApiKey(new StringValues("invalid-key"));

            // Assert
            Assert.Equal("XApiKey is not a valid", result);
        }

        [Fact]
        public void ValidateApiKey_Returns_Empty_When_Valid_Key()
        {
            // Arrange
            var middleware = CreateMiddleware(_ => Task.CompletedTask, ValidApiKey);

            // Act
            var result = middleware.ValidateApiKey(new StringValues(ValidApiKey));

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}
