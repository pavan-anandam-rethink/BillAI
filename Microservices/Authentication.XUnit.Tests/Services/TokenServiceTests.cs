using Authentication.Models;
using Authentication.Services;
using Moq;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace Authentication.XUnit.Tests.Services
{
    public class TokenServiceTests
    {
        private const string Key = "12345678901234567890123456789012";
        private const string Issuer = "TestIssuer";
        private const string Audience = "TestIssuer";

        private static AuthenticateRequest CreateAuthRequest()
        {
            return new AuthenticateRequest
            {
                AccountInfoId = "1",
                MemberId = "100",
                MemberName = "Test User",
                MemberRole = "Admin",
                Permissions = new Dictionary<string, bool>
            {
                { "BillingView", true },
                { "BillingPostPayments", false },
                { "BillingCloseEncounters", true }
            }
            };
        }

        private static TokenService CreateService(bool osbEnabled)
        {
            var rethinkMock = new Mock<IRethinkMasterDataMicroServices>();

            rethinkMock
                .Setup(x => x.GetAccountReturningEntityAsync(1, false))
                .ReturnsAsync(new AccountInfoEntityModel
                {
                    subscriptionFeatures = new Dictionary<string, object>
                    {
                    { "showOSBFlag", osbEnabled }
                    }
                });

            return new TokenService(rethinkMock.Object);
        }

        [Fact]
        public async Task GenerateAccessToken_Returns_Empty_When_Request_Is_Null()
        {
            // Arrange
            var service = CreateService(false);

            // Act
            var token = await service.GenerateAccessToken(Key, Issuer, null);

            // Assert
            Assert.Equal(string.Empty, token);
        }

        [Fact]
        public async Task GenerateAccessToken_Returns_Valid_Jwt()
        {
            // Arrange
            var service = CreateService(osbEnabled: true);
            var request = CreateAuthRequest();

            // Act
            var token = await service.GenerateAccessToken(Key, Issuer, request);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token));

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            Assert.Equal(Issuer, jwt.Issuer);
            Assert.Contains(jwt.Claims, c => c.Type == "MemberId");
            Assert.Contains(jwt.Claims, c => c.Type == "OsbEnabled");
            Assert.Contains(jwt.Claims, c => c.Type == "Permissions");
        }

        [Fact]
        public void IsTokenValid_Returns_True_For_Valid_Token()
        {
            // Arrange
            var service = CreateService(false);
            var request = CreateAuthRequest();

            var token = service
                .GenerateAccessToken(Key, Issuer, request)
                .GetAwaiter()
                .GetResult();

            // Act
            var isValid = service.IsTokenValid(Key, Issuer, Audience, token);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsTokenValid_Returns_False_For_Invalid_Token()
        {
            // Arrange
            var service = CreateService(false);

            // Act
            var isValid = service.IsTokenValid(Key, Issuer, Audience, "invalid.jwt.token");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void GenerateRefreshToken_Returns_Base64_String()
        {
            // Arrange
            var service = CreateService(false);

            // Act
            var refreshToken = service.GenerateRefreshToken();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(refreshToken));
            var bytes = Convert.FromBase64String(refreshToken);
            Assert.Equal(32, bytes.Length);
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_Returns_Principal()
        {
            // Arrange
            var service = CreateService(false);
            var request = CreateAuthRequest();

            var token = service
                .GenerateAccessToken(Key, Issuer, request)
                .GetAwaiter()
                .GetResult();

            // Act
            var principal = service.GetPrincipalFromExpiredToken(Key, Issuer, Audience, token);

            // Assert
            Assert.NotNull(principal);
            Assert.Equal("100", principal.FindFirst("MemberId")?.Value);
        }

        [Fact]
        public void DecryptString_Returns_Original_Text()
        {
            // Arrange
            var service = CreateService(false);
            var originalText = "SensitiveData123";

            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(Key);

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);
            sw.Write(originalText);
            sw.Close();

            var cipherText = Convert.ToBase64String(ms.ToArray());

            // Act
            var decrypted = service.DecryptString(Key, cipherText);

            // Assert
            Assert.Equal(originalText, decrypted);
        }

        [Fact]
        public void GenerateJWTToken_Returns_Valid_Jwt()
        {
            // Arrange
            var key = Key;
            var issuer = Issuer;
            var authRequest = CreateAuthRequest();
            var osbEnabled = true;
            var accountDetail = "TestAccount (1)";

            // Use reflection to invoke the private static method
            var method = typeof(TokenService).GetMethod("GenerateJWTToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var token = (string)method.Invoke(null, new object[] { key, issuer, authRequest, osbEnabled, accountDetail });

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token));
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            Assert.Equal(issuer, jwt.Issuer);
            Assert.Contains(jwt.Claims, c => c.Type == "MemberId");
            Assert.Contains(jwt.Claims, c => c.Type == "OsbEnabled");
            Assert.Contains(jwt.Claims, c => c.Type == "Permissions");
        }
    }

}
