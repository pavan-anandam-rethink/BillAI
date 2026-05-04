using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Models.Claims;
using ClearingHouseService.Web.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models.EligibilityRequest;
using Rethink.Services.Domain.Interfaces;
using System.Net;
using System.Text;

namespace ClearingHouseService.Tests.Helpers
{
    public class CommonHelperTests
    {

        private class TestCommonHelper : CommonHelper
        {
            private readonly HttpClient _client;

            public TestCommonHelper(
                IConfiguration config,
                IRethinkMasterDataMicroServices rethink,
                IKeyVaultProviderService keyVault,
                HttpClient client)
                : base(config, rethink, keyVault)
            {
                _client = client;
            }

            protected override HttpClient CreateHttpClient()
            {
                return _client;
            }
        }

        private class SingleResponseHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;

            public SingleResponseHandler(params HttpResponseMessage[] responses)
            {
                _responses = new Queue<HttpResponseMessage>(responses);
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek());
            }
        }

        private static TestCommonHelper CreateHelper(
            HttpClient client,
            out Mock<IRethinkMasterDataMicroServices> rethinkMock)
        {
            return CreateHelper(client, out rethinkMock, out _);
        }

        private static TestCommonHelper CreateHelper(
            HttpClient client,
            out Mock<IRethinkMasterDataMicroServices> rethinkMock,
            out Mock<IBillingFilePath> billingFilePathMock)
        {
            rethinkMock = new Mock<IRethinkMasterDataMicroServices>();
            var configMock = new Mock<IConfiguration>();
            var keyVaultMock = new Mock<IKeyVaultProviderService>();
            billingFilePathMock = new Mock<IBillingFilePath>();

            configMock.Setup(x => x["BillingApiUrl"]).Returns("BillingApiUrl");
            configMock.Setup(x => x["BillingApiKey"]).Returns("BillingApiKey");
            configMock.Setup(x => x["Sftp:port"]).Returns("22");

            keyVaultMock.Setup(x => x.GetSecretAsync(It.IsAny<string>()))
                .ReturnsAsync("http://localhost");

            return new TestCommonHelper(
                configMock.Object,
                rethinkMock.Object,
                keyVaultMock.Object,
                client);
        }

        [Fact]
        public async Task UploadfileToBlobStorage_Success()
        {
            var handler = new SingleResponseHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("uploaded")
                });

            var helper = CreateHelper(new HttpClient(handler), out _);

            var result = await helper.UploadfileToBlobStorage(
                new ClaimUploadModelWithUserInfo());

            Assert.True(result.success);
            Assert.Equal("uploaded", result.result);
        }

        [Fact]
        public async Task UploadfileToBlobStorage_Failure()
        {
            var handler = new SingleResponseHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("error")
                });

            var helper = CreateHelper(new HttpClient(handler), out _);

            var result = await helper.UploadfileToBlobStorage(
                new ClaimUploadModelWithUserInfo());

            Assert.False(result.success);
            Assert.Equal("error", result.result);
        }

        [Fact]
        public async Task UploadSFTPfilesToBlobStorage_Success()
        {
            var handler = new SingleResponseHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("sftp-ok")
                });

            var helper = CreateHelper(new HttpClient(handler), out _);

            var result = await helper.UploadSFTPfilesToBlobStorage(
                new DownloadSftpDataModel());

            Assert.True(result.success);
            Assert.Equal("sftp-ok", result.result);
        }

        [Fact]
        public async Task UploadSFTPfilesToBlobStorage_BadRequest()
        {
            var handler = new SingleResponseHandler(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("bad")
                });

            var helper = CreateHelper(new HttpClient(handler), out _);

            var result = await helper.UploadSFTPfilesToBlobStorage(
                new DownloadSftpDataModel());

            Assert.False(result.success);
            Assert.Equal("bad", result.result);
        }

        [Fact]
        public async Task UploadSFTPfilesToBlobStorage_OtherStatus()
        {
            var handler = new SingleResponseHandler(
                new HttpResponseMessage(HttpStatusCode.Forbidden));

            var helper = CreateHelper(new HttpClient(handler), out _);

            var result = await helper.UploadSFTPfilesToBlobStorage(
                new DownloadSftpDataModel());

            Assert.False(result.success);
            Assert.Null(result.result);
        }

        [Fact]
        public async Task ReapplyPRAdjustment_ReturnsTrue()
        {
            var handler = new SingleResponseHandler(
                new HttpResponseMessage(HttpStatusCode.OK));

            var helper = CreateHelper(new HttpClient(handler), out _);

            Assert.True(await helper.ReapplyPRAdjustmentAfterSecondaryBilling(1));
        }

        [Fact]
        public async Task ReapplyPRAdjustment_ReturnsFalse()
        {
            var handler = new SingleResponseHandler(
                new HttpResponseMessage(HttpStatusCode.BadRequest));

            var helper = CreateHelper(new HttpClient(handler), out _);

            Assert.False(await helper.ReapplyPRAdjustmentAfterSecondaryBilling(1));
        }

        [Fact]
        public async Task Generate270EDIData_Success()
        {
            var handler = new SingleResponseHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonConvert.SerializeObject("\"EDI270\""),
                        Encoding.UTF8,
                        "application/json")
                });

            var helper = CreateHelper(new HttpClient(handler), out _);

            var result = await helper.Generate270EDIData(new Eligibility270Request());

            Assert.True(result.success);
            Assert.Equal("EDI270", result.result);
        }

        [Fact]
        public async Task Generate270EDIData_BadRequest()
        {
            var handler = new SingleResponseHandler(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        JsonConvert.SerializeObject("\"BAD270\""),
                        Encoding.UTF8,
                        "application/json")
                });

            var helper = CreateHelper(new HttpClient(handler), out _);

            var result = await helper.Generate270EDIData(new Eligibility270Request());

            Assert.False(result.success);
            Assert.Equal("BAD270", result.result);
        }        
    }
}
