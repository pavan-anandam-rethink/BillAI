using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.DataObjects.CompanyAccount;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Clients;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Services.Client;
using ClientService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ClientService.Web.Helpers.HttpClients;
using Rethink.Services.Common.Models.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.Web.XUnit.Tests.Client.Controller
{
    public class ClientControllerTests
    {
        private readonly Mock<IBaseHttpClient> _httpClientMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IClientService> _clientServiceMock = new();
        private readonly Mock<ICommonService> _commonServiceMock = new();
        private readonly Mock<ILogger<ClientController>> _loggerMock = new();

        private ClientController CreateController()
        {
            return new ClientController(
                _httpClientMock.Object,
                _configMock.Object,
                _clientServiceMock.Object,
                _loggerMock.Object,
                _commonServiceMock.Object
            );
        }

        [Fact]
        public async Task GetClientsForClaim_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var model = new UserInfo { AccountInfoId = 1, MemberId = 2 };
            var clients = new List<ClientOptionModel> { new ClientOptionModel { Id = 10, Name = "A" } };

            _clientServiceMock.Setup(s => s.GetClientsListForClaimAsync(model.AccountInfoId, model.MemberId))
                .ReturnsAsync(clients);

            var result = await controller.GetClientsForClaim(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(clients, ok.Value);
        }

        [Fact]
        public async Task GetClientsForClaim_Returns500_OnException()
        {
            var controller = CreateController();
            var model = new UserInfo { AccountInfoId = 1, MemberId = 2 };

            _clientServiceMock.Setup(s => s.GetClientsListForClaimAsync(model.AccountInfoId, model.MemberId))
                .ThrowsAsync(new Exception("boom"));

            var result = await controller.GetClientsForClaim(model);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            Assert.Equal("Internal server error", obj.Value);
        }

        [Fact]
        public async Task SearchDiagnosis_ReturnsOk_WithNoResultsFlag()
        {
            var controller = CreateController();
            var model = new SearchDiagnosisModel { AccountInfoId = 1, DiagnosisTypeId = 5, SearchTerm = "DX" };
            var empty = new List<ClientDiagnosis>();

            _clientServiceMock.Setup(s => s.SearchDiagnosis(model.SearchTerm, model.DiagnosisTypeId, model.AccountInfoId, null))
                .ReturnsAsync(empty);

            var result = await controller.SearchDiagnosis(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = ok.Value!;
            var propInfo = payload.GetType().GetProperty("NoResults");
            Assert.NotNull(propInfo);
            Assert.True((bool)propInfo!.GetValue(payload)!);

            var diagProp = payload.GetType().GetProperty("DiagnosisInfo");
            var diag = Assert.IsAssignableFrom<IEnumerable<ClientDiagnosis>>(diagProp!.GetValue(payload));
            Assert.Empty(diag);
        }

        [Fact]
        public async Task SearchDiagnosis_Returns500_OnException()
        {
            var controller = CreateController();
            var model = new SearchDiagnosisModel { AccountInfoId = 1, DiagnosisTypeId = 5, SearchTerm = "DX" };

            _clientServiceMock.Setup(s => s.SearchDiagnosis(model.SearchTerm, model.DiagnosisTypeId, model.AccountInfoId, null))
                .ThrowsAsync(new Exception("err"));

            var result = await controller.SearchDiagnosis(model);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            Assert.Equal("Internal server error", obj.Value);
        }

        [Fact]
        public async Task GetClaimCreateInfo_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var model = new ClaimCreateInfoGetModel { AccountInfoId = 1, ClientId = 2, FunderId = 3, ServiceId = 4 };
            var response = new ClaimCreateInfoModel { BillingCodes = new List<ClientAuthorizationBillingCodeSmall>() };

            _clientServiceMock.Setup(s => s.GetClaimCreateInfoAsync(model, model.AccountInfoId)).ReturnsAsync(response);

            var result = await controller.GetClaimCreateInfo(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(response, ok.Value);
        }

        [Fact]
        public async Task GetClaimCreateInfo_Returns500_OnException()
        {
            var controller = CreateController();
            var model = new ClaimCreateInfoGetModel { AccountInfoId = 1, ClientId = 2, FunderId = 3, ServiceId = 4 };

            _clientServiceMock.Setup(s => s.GetClaimCreateInfoAsync(model, model.AccountInfoId))
                .ThrowsAsync(new Exception("fail"));

            var result = await controller.GetClaimCreateInfo(model);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            Assert.Equal("Internal server error", obj.Value);
        }

        [Fact]
        public async Task GetDiagnosisForClaimWithoutAuth_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var model = new AuthDiagnosisRequest { AccountInfoId = 1, ChildProfileId = 2, ServiceLineId = 3, IncludeInactive = false };
            var diag = new List<DiagnosisCodeForClaimWithoutAuthModel> { new DiagnosisCodeForClaimWithoutAuthModel { DiagnosisId = 1 } };

            _clientServiceMock
                .Setup(s => s.GetDiagnosisForClaimWithoutAuthAsync(model.ChildProfileId, model.ServiceLineId, model.AccountInfoId))
                .ReturnsAsync(diag);

            var result = await controller.GetDiagnosisForClaimWithoutAuth(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(diag, ok.Value);
        }

        [Fact]
        public async Task GetDiagnosisForClaimWithoutAuth_Returns500_OnException()
        {
            var controller = CreateController();
            var model = new AuthDiagnosisRequest { AccountInfoId = 1, ChildProfileId = 2, ServiceLineId = 3, IncludeInactive = false };

            _clientServiceMock
                .Setup(s => s.GetDiagnosisForClaimWithoutAuthAsync(model.ChildProfileId, model.ServiceLineId, model.AccountInfoId))
                .ThrowsAsync(new Exception("bad"));

            var result = await controller.GetDiagnosisForClaimWithoutAuth(model);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            Assert.Equal("Internal server error", obj.Value);
        }

        [Fact]
        public async Task GetFunderServiceLines_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var model = new FunderServiceLineRequestModel { AccountInfoId = 1, ClientId = 2, FunderId = 3, Id = 4 };
            var list = new List<FunderServiceLineModel> { new FunderServiceLineModel { ServiceId = 111, Name = "S" } };

            _clientServiceMock
                .Setup(s => s.GetFunderServiceLinesAsync(model.Id, model.FunderId, model.AccountInfoId, model.ClientId))
                .ReturnsAsync(list);

            var result = await controller.GetFunderServiceLines(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(list, ok.Value);
        }

        [Fact]
        public async Task GetFunderServiceLines_Returns500_OnException()
        {
            var controller = CreateController();
            var model = new FunderServiceLineRequestModel { AccountInfoId = 1, ClientId = 2, FunderId = 3, Id = 4 };

            _clientServiceMock
                .Setup(s => s.GetFunderServiceLinesAsync(model.Id, model.FunderId, model.AccountInfoId, model.ClientId))
                .ThrowsAsync(new Exception("oops"));

            var result = await controller.GetFunderServiceLines(model);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            Assert.Equal("Internal server error", obj.Value);
        }

        [Fact]
        public async Task GetClientFacilityId_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var model = new ClientFacilityIdModel { AccountInfoId = 1, childProfileId = 2 };
            var facilityId = 999;

            _clientServiceMock.Setup(s => s.GetClientFacilityIdAsync(model.childProfileId, model.AccountInfoId))
                .ReturnsAsync(facilityId);

            var result = await controller.GetClientFacilityId(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(facilityId, ok.Value);
        }

        [Fact]
        public async Task GetClientFacilityId_Returns500_OnException()
        {
            var controller = CreateController();
            var model = new ClientFacilityIdModel { AccountInfoId = 1, childProfileId = 2 };

            _clientServiceMock.Setup(s => s.GetClientFacilityIdAsync(model.childProfileId, model.AccountInfoId))
                .ThrowsAsync(new Exception("err"));

            var result = await controller.GetClientFacilityId(model);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            Assert.Equal("Internal server error", obj.Value);
        }

        [Fact]
        public async Task GetClientFunderResponsibleParties_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var model = new ClientFunderResponsiblePartyRequest { AccountInfoId = 1, MemberId = 2, ChildProfileId = 3, ClientFunderId = 4 };
            var parties = new ClientFunderResponsiblePartiesModel();

            _clientServiceMock
                .Setup(s => s.GetClientFunderResponsiblePartiesAsync(model.MemberId, model.AccountInfoId, model.ChildProfileId, model.ClientFunderId))
                .ReturnsAsync(parties);

            var result = await controller.GetClientFunderResponsibleParties(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(parties, ok.Value);
        }

        [Fact]
        public async Task GetClientFunderResponsibleParties_Returns500_OnException()
        {
            var controller = CreateController();
            var model = new ClientFunderResponsiblePartyRequest { AccountInfoId = 1, MemberId = 2, ChildProfileId = 3, ClientFunderId = 4 };

            _clientServiceMock
                .Setup(s => s.GetClientFunderResponsiblePartiesAsync(model.MemberId, model.AccountInfoId, model.ChildProfileId, model.ClientFunderId))
                .ThrowsAsync(new Exception("bad"));

            var result = await controller.GetClientFunderResponsibleParties(model);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            Assert.Equal("Internal server error", obj.Value);
        }

        [Fact]
        public async Task GetClientAuthorization_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var model = new ClientAuthorizationRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ChildProfileId = 3,
                AuthorizationId = 4,
                LocaleString = "en-US"
            };
            var auth = new ClientAuthorizationModel { Id = 4, AuthorizationNumber = "AUTH-1" };

            _clientServiceMock
                .Setup(s => s.GetClientAuthorization(model.AuthorizationId, model.ChildProfileId, model.MemberId, model.AccountInfoId, model.LocaleString))
                .ReturnsAsync(auth);

            var result = await controller.GetClientAuthorization(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(auth, ok.Value);
        }

        [Fact]
        public async Task GetClientAuthorization_Returns500_OnException()
        {
            var controller = CreateController();
            var model = new ClientAuthorizationRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ChildProfileId = 3,
                AuthorizationId = 4,
                LocaleString = "en-US"
            };

            _clientServiceMock
                .Setup(s => s.GetClientAuthorization(model.AuthorizationId, model.ChildProfileId, model.MemberId, model.AccountInfoId, model.LocaleString))
                .ThrowsAsync(new Exception("fail"));

            var result = await controller.GetClientAuthorization(model);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            Assert.Equal("Internal server error", obj.Value);
        }

        [Fact]
        public async Task GetClientAuthorizationsForClaim_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var model = new ClientAuthorizationsForClaimRequest
            {
                AccountInfoId = 1,
                ChildProfileId = 2,
                FunderId = 3,
                ClientFunderServiceLineId = 4
            };
            var auths = new List<BaseNameOption> { new BaseNameOption { Id = 1, Name = "A" } };

            _clientServiceMock
                .Setup(s => s.GetClientAuthorizationsForClaimAsync(model.ChildProfileId, model.FunderId, model.ClientFunderServiceLineId, model.AccountInfoId))
                .ReturnsAsync(auths);

            var result = await controller.GetClientAuthorizationsForClaim(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(auths, ok.Value);
        }

        [Fact]
        public async Task GetClientAuthorizationsForClaim_Returns500_OnException()
        {
            var controller = CreateController();
            var model = new ClientAuthorizationsForClaimRequest
            {
                AccountInfoId = 1,
                ChildProfileId = 2,
                FunderId = 3,
                ClientFunderServiceLineId = 4
            };

            _clientServiceMock
                .Setup(s => s.GetClientAuthorizationsForClaimAsync(model.ChildProfileId, model.FunderId, model.ClientFunderServiceLineId, model.AccountInfoId))
                .ThrowsAsync(new Exception("boom"));

            var result = await controller.GetClientAuthorizationsForClaim(model);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            Assert.Equal("Internal server error", obj.Value);
        }

        [Fact]
        public async Task GetClientFundersSmall_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var model = new ClientFundersSmallModel { AccountInfoId = 1, childProfileId = 2 };
            var funders = new List<BillingService.Domain.Models.Clients.ClientFunderModel>();

            _clientServiceMock
                .Setup(s => s.GetClientFundersSmallAsync(model.childProfileId, model.AccountInfoId, true))
                .Returns(Task.FromResult(funders));

            var result = await controller.GetClientFundersSmall(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(funders, ok.Value);
        }

        [Fact]
        public async Task GetClientFundersSmall_ReturnsBadRequest_OnException()
        {
            var controller = CreateController();
            var model = new ClientFundersSmallModel { AccountInfoId = 1, childProfileId = 2 };

            _clientServiceMock
                .Setup(s => s.GetClientFundersSmallAsync(model.childProfileId, model.AccountInfoId, true))
                .ThrowsAsync(new Exception("err"));

            var result = await controller.GetClientFundersSmall(model);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("err", bad.Value);
        }

        [Fact]
        public async Task GetPlacesOfService_ReturnsOk_OnSuccess()
        {
            var controller = CreateController();
            var model = new UserInfo { AccountInfoId = 1 };
            var locs = new List<LocationCodeData> { new LocationCodeData { Id = 1, Code = "11" } };

            _commonServiceMock.Setup(s => s.GetLocationCodes(model.AccountInfoId)).ReturnsAsync(locs);

            var result = await controller.GetPlacesOfService(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(locs, ok.Value);
        }

        [Fact]
        public async Task GetPlacesOfService_ReturnsBadRequest_OnException_UsesInnerMessageIfPresent()
        {
            var controller = CreateController();
            var model = new UserInfo { AccountInfoId = 1 };
            var inner = new Exception("inner");
            var ex = new Exception("outer", inner);

            _commonServiceMock.Setup(s => s.GetLocationCodes(model.AccountInfoId)).ThrowsAsync(ex);

            var result = await controller.GetPlacesOfService(model);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("inner", bad.Value);
        }

        [Fact]
        public async Task GetPlacesOfService_ReturnsBadRequest_OnException_NoInnerMessage()
        {
            var controller = CreateController();
            var model = new UserInfo { AccountInfoId = 1 };
            var ex = new Exception("outer");

            _commonServiceMock.Setup(s => s.GetLocationCodes(model.AccountInfoId)).ThrowsAsync(ex);

            var result = await controller.GetPlacesOfService(model);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("outer", bad.Value);
        }
    }
}