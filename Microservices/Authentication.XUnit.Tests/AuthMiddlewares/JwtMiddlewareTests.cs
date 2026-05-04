using Authentication.Interfaces;
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
    public class JwtMiddlewareTests
    {
        private const string AuthorizationHeader = "Authorization";
        private const string ValidJwt = "valid.jwt.token";
        private const string JwtSecret = "jwt-secret";

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

        private static JwtMiddleware CreateMiddleware(
            RequestDelegate next,
            Mock<ITokenService> tokenServiceMock,
            bool tokenValid = true)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Jwt:Key", "JwtKeyName" },
                    { "Jwt:Issuer", "TestIssuer" },
                    { "Jwt:Audience", "TestAudience" }
                })
                .Build();

            var keyVaultMock = new Mock<IKeyVaultProviderService>();
            keyVaultMock
                .Setup(x => x.GetSecretAsync("JwtKeyName"))
                .ReturnsAsync(JwtSecret);

            tokenServiceMock
                .Setup(x => x.IsTokenValid(
                    JwtSecret,
                    "TestIssuer",
                    "TestAudience",
                    It.IsAny<string>()))
                .Returns(tokenValid);

            return new JwtMiddleware(next, config, keyVaultMock.Object);
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
            var tokenServiceMock = new Mock<ITokenService>();

            bool nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = CreateMiddleware(next, tokenServiceMock);

            // Act
            await middleware.Invoke(context, tokenServiceMock.Object);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_Returns_401_When_Jwt_Missing()
        {
            // Arrange
            var context = CreateHttpContext();
            var tokenServiceMock = new Mock<ITokenService>();

            var middleware = CreateMiddleware(_ => Task.CompletedTask, tokenServiceMock);

            // Act
            await middleware.Invoke(context, tokenServiceMock.Object);

            // Assert
            Assert.Equal(401, context.Response.StatusCode);
            var body = await ReadResponseBody(context.Response);
            Assert.Equal("JWT token was not provided", body);
        }

        [Fact]
        public async Task Invoke_Returns_401_When_Jwt_Invalid()
        {
            // Arrange
            var context = CreateHttpContext();
            context.Request.Headers.Add(AuthorizationHeader, $"Bearer {ValidJwt}");

            var tokenServiceMock = new Mock<ITokenService>();
            var middleware = CreateMiddleware(_ => Task.CompletedTask, tokenServiceMock, tokenValid: false);

            // Act
            await middleware.Invoke(context, tokenServiceMock.Object);

            // Assert
            Assert.Equal(401, context.Response.StatusCode);
            var body = await ReadResponseBody(context.Response);
            Assert.Equal("JWT token invalid", body);
        }

        [Fact]
        public async Task Invoke_Calls_Next_When_Jwt_Valid()
        {
            // Arrange
            var context = CreateHttpContext();
            context.Request.Headers.Add(AuthorizationHeader, $"Bearer {ValidJwt}");

            bool nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var tokenServiceMock = new Mock<ITokenService>();
            var middleware = CreateMiddleware(next, tokenServiceMock);

            // Act
            await middleware.Invoke(context, tokenServiceMock.Object);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public void ValidateJwtToken_Returns_Error_When_Whitespace()
        {
            // Arrange
            var tokenServiceMock = new Mock<ITokenService>();
            var middleware = CreateMiddleware(_ => Task.CompletedTask, tokenServiceMock);

            // Act
            var result = middleware.ValidateJwtToken(new StringValues(" "), tokenServiceMock.Object);

            // Assert
            Assert.Equal("Authorization is null or whitespace", result);
        }

        [Fact]
        public void ValidateJwtToken_Returns_Error_When_Multiple_Tokens()
        {
            // Arrange
            var tokenServiceMock = new Mock<ITokenService>();
            var middleware = CreateMiddleware(_ => Task.CompletedTask, tokenServiceMock);

            // Act
            var result = middleware.ValidateJwtToken(
                new StringValues(new[] { "token1", "token2" }),
                tokenServiceMock.Object);

            // Assert
            Assert.Equal("Request returned multiple headers for Authorization", result);
        }

        [Fact]
        public void ValidateJwtToken_Returns_Error_When_Invalid_Token()
        {
            // Arrange
            var tokenServiceMock = new Mock<ITokenService>();
            tokenServiceMock
                .Setup(x => x.IsTokenValid(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(false);

            var middleware = CreateMiddleware(_ => Task.CompletedTask, tokenServiceMock, tokenValid: false);

            // Fix: Set the private _tokenService field
            typeof(JwtMiddleware)
                .GetField("_tokenService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(middleware, tokenServiceMock.Object);

            // Act
            var result = middleware.ValidateJwtToken(new StringValues("invalid.jwt.token"), tokenServiceMock.Object);

            // Assert
            Assert.Equal("JWT token invalid", result);
        }

        [Fact]
        public void ValidateJwtToken_Returns_Empty_When_Valid_Token()
        {
            // Arrange
            var tokenServiceMock = new Mock<ITokenService>();
            tokenServiceMock
                .Setup(x => x.IsTokenValid(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(true);

            var middleware = CreateMiddleware(_ => Task.CompletedTask, tokenServiceMock, tokenValid: true);

            // Fix: Set the private _tokenService field
            typeof(JwtMiddleware)
                .GetField("_tokenService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(middleware, tokenServiceMock.Object);

            // Act
            var result = middleware.ValidateJwtToken(new StringValues(ValidJwt), tokenServiceMock.Object);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}