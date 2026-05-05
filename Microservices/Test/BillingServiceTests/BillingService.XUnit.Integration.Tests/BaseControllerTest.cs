using BillingService.XUnit.Tests.Common;
using Moq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Integration.Tests
{
    [Trait("BaseControllerTest", "Integration")]
    public abstract class BaseControllerTest : BaseTest, IAsyncLifetime
    {
        private readonly HttpClient _client;
        private readonly TestServerFixture _fixture;
        private readonly string _apiKey;

        protected BaseControllerTest(TestServerFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.Client;
            _apiKey = fixture.XApiKey;
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("XApiKey", _apiKey);
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(request);
            return response;
        }

        public async Task InitializeAsync()
        {
            _fixture.ClaimRepository.Reset();
            _fixture.ClaimAppointmentLinkRepository.Reset();
            _fixture.LinkChargeEntryRepository.Reset();
            _fixture.ClaimValidationErrorRepository.Reset();
            _fixture.ClaimAttachmentRepository.Reset();
            _fixture.ClaimErrorCategoryRepository.Reset();
            _fixture.ClaimHistoryRepository.Reset();
            _fixture.MemberViewSettingsRepository.Reset();
            _fixture.ClaimChargeEntryRepository.Reset();
            _fixture.BillingDbHelper.Reset();
            _fixture.PaymentClaimRepository.Reset();
            _fixture.PaymentRepository.Reset();
            _fixture.PaymentNoteRepository.Reset();
            _fixture.ClaimSubmissionRepository.Reset();
            _fixture.ClaimDiagnosisCodeRepository.Reset();
            _fixture.ClaimNoteRepository.Reset();
            _fixture.ClaimHistoryActionRepository.Reset();
            _fixture.PaymentRepository.Reset();
            _fixture.PaymentClaimServiceLineRepository.Reset();
            _fixture.ClaimVersionRepository.Reset();

            //for tests
            _fixture.ClaimManagerService.Reset();
            _fixture.RethinkServices.Reset();
            _fixture.ClientService.Reset();
            _fixture.ProviderLocationService.Reset();
            _fixture.MemberAccountService.Reset();
            _fixture.CommonService.Reset();
            _fixture.ClaimService.Reset();

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await Task.CompletedTask;
        }
    }
}
