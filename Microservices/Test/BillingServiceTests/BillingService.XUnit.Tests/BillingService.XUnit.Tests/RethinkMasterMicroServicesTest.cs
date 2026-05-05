using BillingService.Domain.Models.ClientMicroServicesModels;
using BillingService.Domain.Services.RethinkMasterDataMicroservices;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests
{
    public class RethinkMasterMicroServicesTest
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

        public RethinkMasterMicroServicesTest()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        }

        private RethinkMasterDataMicroServices CreateService(HttpClient sharedClient)
        {
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(sharedClient);

            _mockHttpClientFactory.Setup(x => x.CreateClient("appointmentClient"))
                .Returns(sharedClient);

            return new RethinkMasterDataMicroServices(_mockConfiguration.Object, _mockHttpClientFactory.Object);
        }

        [Fact]
        public async Task GetProviderLocationName_ReturnsEmpty_WhenFirstCallReturnsNull()
        {
            int accountInfoId = 10;
            int childProfileId = 20;

            var handler = new TestHttpMessageHandler((request) =>
            {
                string path = request.RequestUri!.AbsolutePath;

                if (path.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/facility"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetProviderLocationName(accountInfoId, childProfileId);

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetProviderLocationName_ReturnsEmpty_WhenSecondCallReturnsNull()
        {
            int accountInfoId = 10;
            int childProfileId = 20;

            var facilityResponse = new ProviderLocationModel
            {
                providerLocationId = 101
            };

            var handler = new TestHttpMessageHandler((request) =>
            {
                string path = request.RequestUri!.AbsolutePath;

                if (path.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/facility"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(facilityResponse), Encoding.UTF8, "application/json")
                    };
                }

                if (path.Contains($"/accounts/{accountInfoId}/providerLocations/{facilityResponse.providerLocationId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetProviderLocationName(accountInfoId, childProfileId);

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetClientDiagnosisAsync_ReturnsNull_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            int clientId = 20;

            var handler = new TestHttpMessageHandler(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetClientDiagnosisAsync(accountInfoId, clientId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetClientDiagnosisListByDiagnosisIdAsync_ReturnsNull_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            int clientId = 20;
            int clientDiagnosisId = 777;

            var handler = new TestHttpMessageHandler(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetClientDiagnosisListByDiagnosisIdAsync(accountInfoId, clientId, clientDiagnosisId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountReturningEntityAsync_ReturnsMappedEntity_WhenAccountsApiReturnsData()
        {
            int accountInfoId = 10;

            string accountJson = @"
            {
                ""id"": 10,
                ""name"": ""Test Account"",
                ""isTestAccount"": true,
                ""clearingHouseId"": 0,
                ""accountAddress"": {
                    ""address1"": ""A1"",
                    ""address2"": ""A2"",
                    ""address3"": ""A3"",
                    ""city"": ""City1"",
                    ""stateId"": 100,
                    ""town"": ""Town1"",
                    ""zipCode"": ""12345""
                },
                ""billingAddress"": {
                    ""address1"": ""B1"",
                    ""address2"": ""B2"",
                    ""address3"": ""B3"",
                    ""city"": ""City2"",
                    ""stateId"": 200,
                    ""town"": ""Town2"",
                    ""zipCode"": ""54321"",
                    ""countryId"": 1
                },
                ""billingName"": {
                    ""firstName"": ""John"",
                    ""lastName"": ""Doe""
                },
                ""hcTimezoneId"": 5,
                ""metaData"": {
                    ""createdOn"": ""2026-01-01T00:00:00Z"",
                    ""createdBy"": 1,
                    ""deletedOn"": null,
                    ""deletedBy"": null,
                    ""modifiedOn"": null,
                    ""modifiedBy"": null
                },
                ""accountOptions"": [
                    { ""type"": ""ParentVerificationRequired"", ""value"": true },
                    { ""type"": ""StaffVerificationRequired"", ""value"": true },
                    { ""type"": ""SessionNoteRequired"", ""value"": true }
                ],
                ""subscription"": {
                        ""id"": 100,
                        ""subscriptionOptions"": [
                            { ""type"": ""Option1"", ""value"": ""Value1"" },
                            { ""type"": ""Option2"", ""value"": ""Value2"" }
                        ]
                    }
            }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(accountJson, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAccountReturningEntityAsync(accountInfoId, false);

            Assert.NotNull(result);
            Assert.Equal(10, result.Id);
            Assert.Equal("Test Account", result.Name);
            Assert.Equal(1, result.AccountType);
        }

        [Fact]
        public async Task GetAccountReturningEntityAsync_ReturnsDefaultEntity_WhenAccountsApiReturnsNull()
        {
            int accountInfoId = 10;

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAccountReturningEntityAsync(accountInfoId, false);

            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
            Assert.Null(result.Name);
        }


        [Fact]
        public async Task GetMemberAsync_ReturnsMember_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int memberId = 20;

            string json = @"{ ""id"": 20 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/members/{memberId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetMemberAsync(accountInfoId, memberId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetMemberListAsync_ReturnsListModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            string json = @"{ ""data"": [] }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/members") && url.Contains("take=1000"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetMemberListAsync(accountInfoId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetMembersAsync_ReturnsListModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            string memberIds = "memberIds=1&memberIds=2";
            string json = @"{ ""data"": [] }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/members?") && url.Contains("memberIds=1"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetMembersAsync(accountInfoId, memberIds);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetChildProfile_ReturnsClientUserModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int clientId = 99;

            string json = @"{ ""id"": 99 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{clientId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfile(accountInfoId, clientId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetInsuranceContactsIds_ReturnsInsuranceContactsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int clientId = 99;

            string json = @"{ }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{clientId}/contacts"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetInsuranceContactsIds(accountInfoId, clientId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetInsuranceContactsType_ReturnsInsuranceContactsTypeModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int contactId = 30;

            string json = @"{ }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/contacts/{contactId}/insurance"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetInsuranceContactsType(accountInfoId, childProfileId, contactId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFunderList_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            string json = @"{ ""data"": [] }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/funders") && url.Contains("take=1000"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetFunderList(accountInfoId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFunderListByName_ReturnsModel_WhenApiReturnsData()
        {
            string funderName = "ABC";

            string json = @"
            {
                ""funders"": [
                    { ""id"": 1, ""funderName"": ""ABC"", ""vendorId"": ""V1"", ""accountId"": 10 }
                ]
            }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/funders?name={funderName}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""funders"": [] }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetFunderListByName(funderName);

            Assert.NotNull(result);
            Assert.NotNull(result.data);
            Assert.True(result.data.Count > 0);
        }

        [Fact]
        public async Task GetFunderListByTaxId_ReturnsModel_WhenApiReturnsData()
        {
            string taxId = "TX123";

            string json = @"
            {
                ""funder"": { ""id"": 5, ""funderName"": ""TAX FUNDER"", ""vendorId"": ""TX123"", ""accountId"": 10 }
            }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/funders/externalid?vendorid={taxId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""funder"": null }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetFunderListByTaxId(taxId);

            Assert.NotNull(result);
            Assert.NotNull(result.data);
            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetInsuranceContactByPolicy_ReturnsList_WhenApiReturnsData()
        {
            string policyNo = "P123";
            string json = @"[]";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/users/types/client/insurancecontacts?policyNumber={policyNo}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetInsuranceContactByPolicy(policyNo);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetChildProfileFunderMappings_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;

            string json = @"{ ""data"": [] }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/funderMappings"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""data"": [] }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileFunderMappings(accountInfoId, childProfileId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetClientAuthorizationsByClientId_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int clientId = 20;

            string json = @"{ ""data"": [] }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{clientId}/authorizations") && url.Contains("take=500"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""data"": [] }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetClientAuthorizationsByClientId(accountInfoId, clientId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetClientDiagnosisByServiceId_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int clientId = 20;
            int serviceLineId = 99;

            string json = @"{ ""data"": [] }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{clientId}/diagnoses")
                    && url.Contains($"serviceLineId={serviceLineId}")
                    && url.Contains("take=1000"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""data"": [] }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetClientDiagnosisByServiceId(accountInfoId, clientId, serviceLineId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetProviderBillingCode_ById_ReturnsModel_WhenApiReturnsData()
        {
            int accountId = 10;
            int billingCodeId = 50;

            string json = @"{ ""id"": 50 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountId}/billingcodes/{billingCodeId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{}", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetProviderBillingCode(accountId, billingCodeId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetBillingCodeList_ReturnsModel_WhenApiReturnsData()
        {
            int accountId = 10;
            string json = @"{ ""data"": [] }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountId}/billingcodes") && url.Contains("take=1000"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""data"": [] }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetBillingCodeList(accountId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetProviderService_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int serviceId = 77;

            string json = @"{ ""id"": 77 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/services/{serviceId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{}", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetProviderService(accountInfoId, serviceId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetUnitTypesAsync_ReturnsList_WhenApiReturnsData()
        {
            string json = @"[]";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains("/definitions/unittypes"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetUnitTypesAsync();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetReasonCodes_ReturnsList_WhenApiReturnsData()
        {
            string json = @"[]";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains("/definitions/reasonCodes"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetReasonCodes();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetClearingHouseId_ReturnsId_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            string json = @"{ ""clearingHouseId"": 25 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""clearingHouseId"": 0 }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetClearingHouseId(accountInfoId);

            Assert.Equal(25, result);
        }

        [Fact]
        public async Task GetStateList_ReturnsList_WhenApiReturnsData()
        {
            string json = @"[]";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains("/definitions/states"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetStateList();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetCountryList_ReturnsList_WhenApiReturnsData()
        {
            string json = @"[]";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains("/definitions/countries"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetCountryList();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetTimezones_ReturnsList_WhenApiReturnsData()
        {
            string json = @"[]";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains("/definitions/timezones"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetTimezones();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CallGenericRequest_ReturnsAllPages_WhenPagedApiReturnsMultiplePages()
        {
            int callCount = 0;

            var handler = new TestHttpMessageHandler((req) =>
            {
                callCount++;

                var json = @"{
                    ""Total"": 10,
                    ""Data"": [
                        { ""id"": 1 },
                        { ""id"": 2 },
                        { ""id"": 3 },
                        { ""id"": 4 },
                        { ""id"": 5 }
                    ]
                }";

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-api.com/")
            };

            var service = CreateService(httpClient);

            var filterQuery = new Dictionary<string, string>
            {
                { "name", "abc" }
            };

            var method = typeof(RethinkMasterDataMicroServices)
                .GetMethod("CallGenericRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.NotNull(method);

            // ✅ GENERIC INVOCATION FIX
            var genericMethod = method!.MakeGenericMethod(typeof(ClientUserModel));

            var task = (Task<List<ClientUserModel>>)genericMethod.Invoke(service, new object[]
            {
                httpClient,
                "/test?",
                filterQuery,
                0,
                5
            });

            var result = await task;

            Assert.NotNull(result);
            Assert.True(callCount >= 2); 
        }

        [Fact]
        public async Task ReturnGenericData_ReturnsObject_WhenResponseIsSuccess()
        {
            var client = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
            {
                BaseAddress = new Uri("https://fake-api.com/")
            };

            var service = CreateService(client);

            var method = typeof(RethinkMasterDataMicroServices)
                .GetMethod("ReturnGenericData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.NotNull(method);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{ ""id"": 10 }", Encoding.UTF8, "application/json")
            };

            var task = (Task<ProviderLocationModel>)method!.MakeGenericMethod(typeof(ProviderLocationModel))
                .Invoke(service, new object[] { response });

            var result = await task;

            Assert.NotNull(result);
        }

        [Fact]
        public async Task ReturnGenericData_ReturnsNull_WhenResponseIsNotSuccess()
        {
            var client = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
            {
                BaseAddress = new Uri("https://fake-api.com/")
            };

            var service = CreateService(client);

            var method = typeof(RethinkMasterDataMicroServices)
                .GetMethod("ReturnGenericData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.NotNull(method);

            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            var task = (Task<ProviderLocationModel>)method!.MakeGenericMethod(typeof(ProviderLocationModel))
                .Invoke(service, new object[] { response });

            var result = await task;

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnGenericPagedData_ReturnsPagedResponse_WhenResponseIsSuccess()
        {
            var client = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
            {
                BaseAddress = new Uri("https://fake-api.com/")
            };

            var service = CreateService(client);

            var method = typeof(RethinkMasterDataMicroServices)
                .GetMethod("ReturnGenericPagedData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.NotNull(method);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{
                    ""Total"": 1,
                    ""Data"": [ { ""id"": 1 } ]
                }", Encoding.UTF8, "application/json")
            };

            var task = (Task<PagedResponse<ProviderLocationModel>>)method!.MakeGenericMethod(typeof(ProviderLocationModel))
                .Invoke(service, new object[] { response });

            var result = await task;

            Assert.NotNull(result);
            Assert.Equal(1, result.Total);
            Assert.NotNull(result.Data);
        }



        [Fact]
        public async Task GetAccountInfoByTaxIDNPI_ReturnsList_WhenTaxIdEmpty_UsesNpiUrl()
        {
            string taxId = "";
            string npi = "12345";

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/providerlocations/externalid?npinumber={npi}"))
                {
                    string json = @"[
                { ""id"": 1, ""accountId"": 10 },
                { ""id"": 2, ""accountId"": 20 }
            ]";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAccountInfoByTaxIDNPI(taxId, npi);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].id);
            Assert.Equal(10, result[0].accountId);
        }

        [Fact]
        public async Task GetAccountInfoByTaxIDNPI_ReturnsList_WhenNpiEmpty_UsesTaxIdUrl()
        {
            string taxId = "TX999";
            string npi = "";

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/providerlocations/externalid?federalTaxId={taxId}"))
                {
                    string json = @"[
                { ""id"": 5, ""accountId"": 55 }
            ]";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAccountInfoByTaxIDNPI(taxId, npi);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(5, result[0].id);
            Assert.Equal(55, result[0].accountId);
        }

        [Fact]
        public async Task GetAccountInfoByTaxIDNPI_ReturnsList_WhenTaxIdAndNpiProvided_UsesCombinedUrl()
        {
            string taxId = "TX111";
            string npi = "NPI222";

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/providerlocations/externalid?npinumber={npi}&federalTaxId={taxId}"))
                {
                    string json = @"[
                { ""id"": 9, ""accountId"": 99 }
            ]";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAccountInfoByTaxIDNPI(taxId, npi);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(9, result[0].id);
            Assert.Equal(99, result[0].accountId);
        }

        [Fact]
        public async Task GetAccountInfoByTaxIDNPI_ReturnsNull_WhenApiReturnsNull()
        {
            string taxId = "TX1";
            string npi = "NPI1";

            var handler = new TestHttpMessageHandler(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAccountInfoByTaxIDNPI(taxId, npi);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountByName_ReturnsMappedList_WhenApiReturnsData()
        {
            string name = "TestAccount";

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts?name={name}"))
                {
                    string json = @"{
                ""data"": [
                    { ""id"": 101 },
                    { ""id"": 202 }
                ]
            }";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAccountByName(name);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(0, result[0].id);
            Assert.Equal(101, result[0].accountId);
        }

        [Fact]
        public async Task GetAccountByName_ReturnsNull_WhenApiReturnsNull()
        {
            string name = "NoAccount";

            var handler = new TestHttpMessageHandler(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAccountByName(name);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetFunderInfoByTaxID_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            string taxId = "TX123";

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/funders?vendorId={taxId}"))
                {
                    string json = @"[
                { ""id"": 1 },
                { ""id"": 2 }
            ]";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetFunderInfoByTaxID(accountInfoId, taxId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetChildProfileFunderMappingByMappingId_ReturnsEntity_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int mappingId = 99;

            int insuranceContactId = 555;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                // 1) main mapping entity call
                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/funderMappings/{mappingId}"))
                {
                    string json = $@"{{
                ""childProfileInsuranceContactId"": {insuranceContactId}
            }}";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                // 2) insurance contact entity call
                if (url.Contains($"/accounts/{accountInfoId}/users/types/10/{childProfileId}/contacts/{insuranceContactId}"))
                {
                    string json = @"{ ""id"": 555 }";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                // 3) insurance contact type call
                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/contacts/{insuranceContactId}/insurance"))
                {
                    string json = @"{ }";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, mappingId);

            Assert.NotNull(result);
            Assert.NotNull(result.InsuranceContact);
            Assert.NotNull(result.InsuranceContact.InsuranceContactsType);
        }

        [Fact]
        public async Task GetChildProfileFunderMappingByMappingId_ReturnsNull_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int mappingId = 99;

            var handler = new TestHttpMessageHandler(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, mappingId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetFunder_ReturnsFunder_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int funderId = 50;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/funders/{funderId}"))
                {
                    string json = @"{
                ""funder"": { ""id"": 50, ""funderName"": ""ABC"" }
            }";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetFunder(accountInfoId, funderId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFunder_ReturnsNull_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            int funderId = 50;

            var handler = new TestHttpMessageHandler(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetFunder(accountInfoId, funderId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllFundersForAccount_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/funders?take=99999"))
                {
                    string json = @"{
                ""Data"": [
                    { ""id"": 1 },
                    { ""id"": 2 }
                ]
            }";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAllFundersForAccount(accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }


        [Fact]
        public async Task GetChildProfileFunderServiceLineMappingEntity_ReturnsEntity_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int mappingId = 30;
            int servicelineMappingId = 40;

            int insuranceContactId = 555;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                // 1) serviceline mapping entity call
                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/funderMappings/{mappingId}/serviceLineMappings/{servicelineMappingId}"))
                {
                    // must include ChildProfileFunderMappingId for nested mapping call
                    string json = $@"{{
                ""ChildProfileFunderMappingId"": {mappingId}
            }}";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                // 2) mapping call used inside it
                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/funderMappings/{mappingId}"))
                {
                    string json = $@"{{
                ""childProfileInsuranceContactId"": {insuranceContactId}
            }}";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                // 3) insurance contact call
                if (url.Contains($"/accounts/{accountInfoId}/users/types/10/{childProfileId}/contacts/{insuranceContactId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""id"": 555 }", Encoding.UTF8, "application/json")
                    };
                }

                // 4) insurance type call
                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/contacts/{insuranceContactId}/insurance"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ }", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileFunderServiceLineMappingEntity(accountInfoId, childProfileId, mappingId, servicelineMappingId);

            Assert.NotNull(result);
            Assert.NotNull(result.ChildProfileFunderMapping);
            Assert.NotNull(result.ChildProfileFunderMapping.InsuranceContact);
            Assert.NotNull(result.ChildProfileFunderMapping.InsuranceContact.InsuranceContactsType);
        }


        [Fact]
        public async Task GetInsuranceContactEntity_ReturnsEntity_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int insuranceContactId = 30;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/10/{childProfileId}/contacts/{insuranceContactId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""id"": 30 }", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetInsuranceContactEntity(accountInfoId, childProfileId, insuranceContactId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetServiceFundersEntityById_ReturnsEntity_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int serviceFunderId = 99;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/serviceFunders/{serviceFunderId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""id"": 99 }", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetServiceFundersEntityById(accountInfoId, serviceFunderId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetServiceFundersEntityListByFunderId_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int funderId = 30;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/serviceFunders?funderId={funderId}"))
                {
                    string json = @"{ ""data"": [ { ""id"": 1 }, { ""id"": 2 } ] }";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetServiceFundersEntityListByFunderId(accountInfoId, childProfileId, funderId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetChildProfileReferringProviderEntity_ReturnsEntity_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int referringProviderId = 300;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                // 1) childProfile referring provider entity
                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/referringProviders/{referringProviderId}"))
                {
                    string json = $@"{{ ""referringProviderId"": {referringProviderId} }}";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                // 2) provider info call (demographics)
                if (url.Contains($"/accounts/{accountInfoId}/users/types/referringProvider/{referringProviderId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""id"": 300 }", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileReferringProviderEntity(accountInfoId, childProfileId, referringProviderId);

            Assert.NotNull(result);
            Assert.NotNull(result.ReferringProvider);
        }

        [Fact]
        public async Task GetChildProfileReferringProviderEntity_ReturnsDefault_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int referringProviderId = 300;

            var handler = new TestHttpMessageHandler(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileReferringProviderEntity(accountInfoId, childProfileId, referringProviderId);

            Assert.NotNull(result);
        }


        [Fact]
        public async Task GetReferringProviderInfo_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int referringProviderId = 300;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/referringProvider/{referringProviderId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""id"": 300 }", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetReferringProviderInfo(accountInfoId, childProfileId, referringProviderId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFunderInsurancePlansForFunder_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int clientFunderId = 30;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/funders/{clientFunderId}/insurancePlans"))
                {
                    string json = @"{ ""data"": [ { ""id"": 1 }, { ""id"": 2 } ] }";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetFunderInsurancePlansForFunder(accountInfoId, childProfileId, clientFunderId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetFunderInsurancePlansForFunder_ReturnsEmptyList_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int clientFunderId = 30;

            var handler = new TestHttpMessageHandler(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetFunderInsurancePlansForFunder(accountInfoId, childProfileId, clientFunderId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllRenderingProvidersAsync_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/20?take=1000"))
                {
                    string json = @"{ ""data"": [] }";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAllRenderingProvidersAsync(accountInfoId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRenderingProvidersAsync_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;

            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/20?take=1000"))
                {
                    string json = @"{
                ""data"": [
                    { ""id"": 2, ""memberId"": 200, ""name"": { ""firstName"": ""B"", ""lastName"": ""Z"" }, ""identifiers"": [] },
                    { ""id"": 1, ""memberId"": 100, ""name"": { ""firstName"": ""A"", ""lastName"": ""Y"" }, ""identifiers"": [] }
                ]
            }";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetRenderingProvidersAsync(accountInfoId, false);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.True(result[0].Name.CompareTo(result[1].Name) <= 0); // sorted by Name
        }

        [Fact]
        public async Task GetRenderingProvidersAsync_ReturnsEmptyList_WhenApiReturnsNull()
        {
            int accountInfoId = 10;

            var handler = new TestHttpMessageHandler(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetRenderingProvidersAsync(accountInfoId, false);

            Assert.NotNull(result);
            Assert.Empty(result);
        }
        [Fact]
        public async Task GetAppointmentAsync_ReturnsAppointment_WhenApiReturnsData()
        {
            int appointmentId = 10;

            var handler = new TestHttpMessageHandler((req) =>
            {
                if (req.RequestUri!.ToString().EndsWith("/" + appointmentId))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""id"": 10 }", Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, "application/json") };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetAppointmentAsync(appointmentId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetCompletedAppointmentListAsync_ReturnsEmptyList_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            int clientId = 20;
            DateTime startDate = DateTime.UtcNow.Date;

            var handler = new TestHttpMessageHandler((req) =>
            {
                if (req.RequestUri!.ToString().Contains($"/client/{clientId}/completed?startDate="))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, "application/json") };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetCompletedAppointmentListAsync(accountInfoId, clientId, startDate);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPropagatingStaffMemberById_ReturnsModel_WhenApiReturnsData()
        {
            int propStaffId = 99;

            var handler = new TestHttpMessageHandler((req) =>
            {
                if (req.RequestUri!.ToString().Contains($"/users/types/staff/propagating/{propStaffId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""id"": 99 }", Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, "application/json") };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetPropagatingStaffMemberById(propStaffId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetWorkFlowHistoyDetailsById_ReturnsModel_WhenApiReturnsData()
        {
            int workflowId = 55;

            var handler = new TestHttpMessageHandler((req) =>
            {
                if (req.RequestUri!.ToString().Contains($"/status/{workflowId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""id"": 55 }", Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, "application/json") };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetWorkFlowHistoyDetailsById(workflowId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetChildProfileFacility_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;

            var handler = new TestHttpMessageHandler((req) =>
            {
                if (req.RequestUri!.ToString().Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/facility"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""providerLocationId"": 101 }", Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, "application/json") };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileFacility(accountInfoId, childProfileId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetBillingAccountsAsync_ReturnsAllAccounts_WhenPagedApiReturnsData()
        {
            var handler = new TestHttpMessageHandler((req) =>
            {
                if (req.RequestUri!.ToString().Contains("/accounts/?") && req.RequestUri.ToString().Contains("take=5"))
                {
                    string json = @"{
                ""Total"": 2,
                ""Data"": [
                    { ""id"": 1 },
                    { ""id"": 2 }
                ]
            }";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(@"{ ""Total"": 0, ""Data"": [] }", Encoding.UTF8, "application/json") };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetBillingAccountsAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task CallGenericRequest_ReturnsAllPages_WhenApiHasMultiplePages()
        {
            // Arrange
            var handler = new TestHttpMessageHandler((req) =>
            {
                var url = req.RequestUri!.ToString();

                // 1st call => skip=0 take=5
                if (url.Contains("skip=0") && url.Contains("take=5"))
                {
                    string json = @"{
                ""Total"": 6,
                ""Data"": [
                    { ""id"": 1 }, { ""id"": 2 }, { ""id"": 3 }, { ""id"": 4 }, { ""id"": 5 }
                ]
            }";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                // 2nd call => skip=5 take=5
                if (url.Contains("skip=5") && url.Contains("take=5"))
                {
                    string json2 = @"{
                ""Total"": 6,
                ""Data"": [
                    { ""id"": 6 }
                ]
            }";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json2, Encoding.UTF8, "application/json")
                    };
                }

                // fallback
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-api.com/")
            };

            var service = CreateService(httpClient);

            var filterQuery = new Dictionary<string, string>
    {
        { "name", "abc" }
    };

            var method = typeof(RethinkMasterDataMicroServices)
                .GetMethod("CallGenericRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.NotNull(method);

            // Act
            var task = (Task<List<ClientUserModel>>)method.MakeGenericMethod(typeof(ClientUserModel))
                .Invoke(service, new object[]
                {
            httpClient,
            "/test?",
            filterQuery,
            0,
            5
                })!;

            var result = await task;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(6, result.Count);
        }

        [Fact]
        public async Task GetCountryById_ReturnsMatchingCountry_WhenCountryExists()
        {
            // Arrange
            int countryId = 2;

            string json = @"[
        { ""id"": 1, ""name"": ""India"" },
        { ""id"": 2, ""name"": ""USA"" }
    ]";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains("/definitions/countries"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            // Act
            var result = await service.GetCountryById(countryId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(countryId, result.id);
        }

        [Fact]
        public async Task GetCountryById_ReturnsNull_WhenCountryDoesNotExist()
        {
            // Arrange
            int countryId = 999;

            string json = @"[
        { ""id"": 1, ""name"": ""India"" },
        { ""id"": 2, ""name"": ""USA"" }
    ]";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains("/definitions/countries"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            // Act
            var result = await service.GetCountryById(countryId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCountryById_ReturnsNull_WhenApiReturnsNull()
        {
            // Arrange
            int countryId = 2;

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains("/definitions/countries"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            // Act
            var result = await service.GetCountryById(countryId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetChildProfileAuthBillingCodeForAppointment_ReturnsModel_WhenApiReturnsData()
        {
            // Arrange
            int accountInfoId = 10;
            int childProfileId = 20;
            int billingCodeId = 30;

            string json = @"{ ""id"": 30 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/billingCodes/{billingCodeId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            // Act
            var result = await service.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, childProfileId, billingCodeId);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetChildProfileAuthBillingCodeForAppointment_ReturnsNull_WhenApiReturnsNull()
        {
            // Arrange
            int accountInfoId = 10;
            int childProfileId = 20;
            int billingCodeId = 30;

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/billingCodes/{billingCodeId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            // Act
            var result = await service.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, childProfileId, billingCodeId);

            // Assert
            Assert.Null(result);
        }


        [Fact]
        public async Task GetClientAuthBillingCodesByAuthId_ReturnsList_WhenApiReturnsData()
        {
            // Arrange
            int accountInfoId = 10;
            int childProfileId = 20;
            int authId = 30;
            string authBillingCodesJson = @"
    {
        ""data"": [
            {
                ""id"": 1,
                ""providerBillingCodeId"": 999,
                ""noOfUnits"": 5,
                ""unitTypeId"": 2,
                ""schedulingGoalFrequencyTypeId"": 1,
                ""frequencyTypeId"": 1,
                ""schedulingGoalNoOfUnits"": 10,
                ""childProfileAuthorizationId"": 777
            }
        ]
    }";

            string providerBillingCodeJson = @"
    {
        ""id"": 999,
        ""billingCode2"": ""BC2"",
        ""billingCode"": ""BC"",
        ""rate"": 100,
        ""serviceId"": 55,
        ""unitTypeId"": 2
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/authorizations/{authId}/billingCodes"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(authBillingCodesJson, Encoding.UTF8, "application/json")
                    };
                }

                if (url.Contains($"/accounts/{accountInfoId}/billingcodes/999"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(providerBillingCodeJson, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            // Act
            var result = await service.GetClientAuthBillingCodesByAuthId(accountInfoId, childProfileId, authId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            Assert.Equal(1, result[0].Id);
            Assert.Equal(999, result[0].BillingCodeId);
            Assert.NotNull(result[0].ProviderBillingCode);
            Assert.Equal(999, result[0].ProviderBillingCode.id);
        }

        [Fact]
        public async Task GetClientAuthBillingCodesByAuthId_ReturnsEmptyList_WhenApiReturnsNull()
        {
            // Arrange
            int accountInfoId = 10;
            int childProfileId = 20;
            int authId = 30;

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/authorizations/{authId}/billingCodes"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            // Act
            var result = await service.GetClientAuthBillingCodesByAuthId(accountInfoId, childProfileId, authId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }


        [Fact]
        public async Task GetPayerDetails_ReturnsFirstItem_WhenApiReturnsList()
        {
            // Arrange
            int funderId = 10;

            string json = @"
    [
        { ""id"": 1 },
        { ""id"": 2 }
    ]";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/definitions/payers?funderId={funderId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            // Act
            var result = await service.GetPayerDetails(funderId);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetPayerDetails_ReturnsNewObject_WhenApiReturnsNull()
        {
            // Arrange
            int funderId = 10;

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/definitions/payers?funderId={funderId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            // Act
            var result = await service.GetPayerDetails(funderId);

            // Assert
            Assert.NotNull(result); // should return new PayerDetailsModel()
        }
            [Fact]
            public async Task GetChildProfilesForAccount_ReturnsMappedList_WhenApiReturnsData()
            {
                int accountInfoId = 10;

                string json = @"
                {
                    ""Data"": [
                        {
                            ""id"": 101,
                            ""memberId"": 5001,
                            ""accountId"": 10,
                            ""genderId"": 1,
                            ""dateOfBirth"": ""2020-01-10T00:00:00Z"",
                            ""name"": {
                                ""firstName"": ""John"",
                                ""middleName"": ""M"",
                                ""lastName"": ""Doe""
                            },
                            ""address"": {
                                ""city"": ""New York"",
                                ""countryId"": 1,
                                ""stateId"": 33,
                                ""town"": ""TownX"",
                                ""zipCode"": ""10001"",
                                ""street1"": ""Street 1"",
                                ""street2"": ""Street 2""
                            },
                            ""metaData"": {
                                ""createdBy"": 1,
                                ""createdOn"": ""2026-01-01T00:00:00Z"",
                                ""deletedOn"": null,
                                ""deletedBy"": null,
                                ""modifiedOn"": null,
                                ""modifiedBy"": null
                            },
                            ""identifiers"": [
                                { ""identifierType"": ""Uci"", ""value"": ""UCI-123"" }
                            ],
                            ""contacts"": [
                                {
                                    ""id"": 9001,
                                    ""timezoneId"": 5,
                                    ""maritalStatusId"": 1,
                                    ""dateOfBirth"": ""1990-01-01T00:00:00Z"",
                                    ""genderId"": 2,
                                    ""metaData"": {
                                        ""createdBy"": 1,
                                        ""createdOn"": ""2026-01-01T00:00:00Z"",
                                        ""deletedOn"": null,
                                        ""deletedBy"": null,
                                        ""modifiedOn"": null,
                                        ""modifiedBy"": null
                                    }
                                }
                            ],
                            ""attributes"": [
                                { ""type"": ""facilityId"", ""value"": ""555"" }
                            ]
                        }
                    ]
                }";

                var handler = new TestHttpMessageHandler((request) =>
                {
                    var url = request.RequestUri!.ToString();

                    if (url.Contains($"/accounts/{accountInfoId}/users/types/client?take=99999"))
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(json, Encoding.UTF8, "application/json")
                        };
                    }

                    return new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("{}", Encoding.UTF8, "application/json")
                    };
                });

                var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
                var service = CreateService(client);

                var result = await service.GetChildProfilesForAccount(accountInfoId);

                Assert.NotNull(result);
                Assert.Single(result);

                var child = result[0];

                Assert.Equal(101, child.Id);
                Assert.Equal("John", child.FirstName);
                Assert.Equal("M", child.MiddleName);
                Assert.Equal("Doe", child.LastName);

                Assert.Equal(1, child.GenderId);
                Assert.Equal(5001, child.MemberId);
                Assert.Equal(10, child.AccountInfoId);

                Assert.Equal("New York", child.City);
                Assert.Equal(1, child.CountryId);
                Assert.Equal(33, child.StateId);
                Assert.Equal("TownX", child.Town);
                Assert.Equal("10001", child.ZipCode);

                Assert.Equal("Street 1", child.Address);
                Assert.Equal("Street 2", child.Address2);

                Assert.Equal("UCI-123", child.UCI);
                Assert.Equal(555, child.FacilityId);

                Assert.NotNull(child.ChildProfileContacts);
                Assert.Single(child.ChildProfileContacts);
                Assert.Equal(9001, child.ChildProfileContacts[0].Id);
            }


            [Fact]
            public async Task GetChildProfileReturningEntity_ReturnsMappedEntity_WhenApiReturnsData()
            {
                int accountInfoId = 10;
                int clientId = 101;

                string json = @"
                {
                    ""id"": 101,
                    ""memberId"": 5001,
                    ""accountId"": 10,
                    ""genderId"": 1,
                    ""dateOfBirth"": ""2020-01-10T00:00:00Z"",
                    ""name"": {
                        ""firstName"": ""John"",
                        ""middleName"": ""M"",
                        ""lastName"": ""Doe""
                    },
                    ""address"": {
                        ""city"": ""New York"",
                        ""countryId"": 1,
                        ""stateId"": 33,
                        ""town"": ""TownX"",
                        ""zipCode"": ""10001"",
                        ""street1"": ""Street 1"",
                        ""street2"": ""Street 2""
                    },
                    ""metaData"": {
                        ""createdBy"": 1,
                        ""createdOn"": ""2026-01-01T00:00:00Z"",
                        ""deletedOn"": null,
                        ""deletedBy"": null,
                        ""modifiedOn"": null,
                        ""modifiedBy"": null
                    },
                    ""identifiers"": [
                        { ""identifierType"": ""Uci"", ""value"": ""UCI-123"" }
                    ],
                    ""contacts"": [
                        {
                            ""id"": 9001,
                            ""timezoneId"": 5,
                            ""maritalStatusId"": 1,
                            ""dateOfBirth"": ""1990-01-01T00:00:00Z"",
                            ""genderId"": 2,
                            ""metaData"": {
                                ""createdBy"": 1,
                                ""createdOn"": ""2026-01-01T00:00:00Z"",
                                ""deletedOn"": null,
                                ""deletedBy"": null,
                                ""modifiedOn"": null,
                                ""modifiedBy"": null
                            }
                        }
                    ],
                    ""attributes"": [
                        { ""type"": ""facilityId"", ""value"": ""555"" }
                    ]
                }";

                var handler = new TestHttpMessageHandler((request) =>
                {
                    var url = request.RequestUri!.ToString();

                    if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{clientId}"))
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(json, Encoding.UTF8, "application/json")
                        };
                    }

                    return new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("{}", Encoding.UTF8, "application/json")
                    };
                });

                var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
                var service = CreateService(client);

                var result = await service.GetChildProfileReturningEntity(accountInfoId, clientId);

                Assert.NotNull(result);

                Assert.Equal(101, result.Id);
                Assert.Equal("John", result.FirstName);
                Assert.Equal("M", result.MiddleName);
                Assert.Equal("Doe", result.LastName);

                Assert.Equal("New York", result.City);
                Assert.Equal("Street 1", result.Address);
                Assert.Equal("Street 2", result.Address2);

                Assert.Equal("UCI-123", result.UCI);
                Assert.Equal(555, result.FacilityId);

                Assert.NotNull(result.ChildProfileContacts);
                Assert.Single(result.ChildProfileContacts);
                Assert.Equal(9001, result.ChildProfileContacts[0].Id);
            }

        [Fact]
        public async Task GetStaffMember_ReturnsStaffMember_WithTimezone_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int staffMemberId = 99;

            string staffJson = @"{
        ""id"": 99,
        ""timezoneId"": 5
    }";

            string timezonesJson = @"[
        { ""id"": 5, ""name"": ""EST"" },
        { ""id"": 6, ""name"": ""CST"" }
    ]";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/staff/{staffMemberId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(staffJson, Encoding.UTF8, "application/json")
                    };
                }

                if (url.Contains("/definitions/timezones"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(timezonesJson, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetStaffMember(accountInfoId, staffMemberId);

            Assert.NotNull(result);
            Assert.Equal(99, result.id);
            Assert.NotNull(result.Timezone);
            Assert.Equal(5, result.Timezone.id);
        }

        [Fact]
        public async Task GetStaffMember_ReturnsNull_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            int staffMemberId = 99;

            var handler = new TestHttpMessageHandler((request) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetStaffMember(accountInfoId, staffMemberId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetStaffMemberList_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;

            string json = @"{
        ""data"": [
            { ""id"": 1 },
            { ""id"": 2 }
        ]
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/staff?take=1000"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetStaffMemberList(accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetStaffMemberList_ReturnsNull_WhenApiReturnsNull()
        {
            int accountInfoId = 10;

            var handler = new TestHttpMessageHandler((request) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetStaffMemberList(accountInfoId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetStaffMemberListByPermission_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            var permissions = new List<string> { "perm1", "perm2" };
            string logicalOperator = "AND";

            string json = @"{
        ""data"": [
            { ""id"": 1 },
            { ""id"": 2 }
        ]
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/staff/by-permissions?")
                    && url.Contains("permissions=perm1")
                    && url.Contains("permissions=perm2")
                    && url.Contains($"logicOperator={logicalOperator}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetStaffMemberListByPermission(accountInfoId, permissions, logicalOperator);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetStaffMemberListByPermission_ReturnsNull_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            var permissions = new List<string> { "perm1" };
            string logicalOperator = "OR";

            var handler = new TestHttpMessageHandler((request) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetStaffMemberListByPermission(accountInfoId, permissions, logicalOperator);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetProviderLocation_ReturnsProviderLocation_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int providerLocationId = 55;

            string json = @"{ ""id"": 55, ""accountId"": 10 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/providerLocations/{providerLocationId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetProviderLocation(accountInfoId, providerLocationId);

            Assert.NotNull(result);
            Assert.Equal(55, result.id);
        }

        [Fact]
        public async Task GetMainLocation_ReturnsProviderLocation_WhenApiReturnsData()
        {
            int accountInfoId = 10;

            string json = @"{ ""id"": 100, ""accountId"": 10 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/providerLocations/main"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetMainLocation(accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(100, result.id);
        }

        [Fact]
        public async Task GetProviderLocationList_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;

            string json = @"{ ""data"": [ { ""id"": 1 }, { ""id"": 2 } ] }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/providerLocations"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetProviderLocationList(accountInfoId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetProviderBillingCodeCredential_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int providerBillingCode = 22;
            int credentialId = 33;

            string json = @"{ ""id"": 33 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/providerBillingCodes/{providerBillingCode}/credentials/{credentialId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetProviderBillingCodeCredential(accountInfoId, providerBillingCode, credentialId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetReferringProvidersByClientId_ReturnsList_WhenApiReturnsData()
        {
            int clientId = 20;
            int accountInfoId = 10;

            string referringProvidersListJson = @"
    {
      ""childProfileReferringProviders"": {
        ""data"": [
          { ""id"": 1, ""isDefault"": true, ""referringProviderId"": 100 },
          { ""id"": 2, ""isDefault"": false, ""referringProviderId"": 200 }
        ]
      }
    }";

            string provider100Json = @"{
      ""name"": { ""firstName"": ""John"", ""middleName"": ""M"", ""lastName"": ""Doe"" }
    }";

            string provider200Json = @"{
      ""name"": { ""firstName"": ""Jane"", ""middleName"": """", ""lastName"": ""Smith"" }
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{clientId}/referringproviders"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(referringProvidersListJson, Encoding.UTF8, "application/json")
                    };
                }

                if (url.Contains($"/accounts/{accountInfoId}/users/types/referringProvider/100"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(provider100Json, Encoding.UTF8, "application/json")
                    };
                }

                if (url.Contains($"/accounts/{accountInfoId}/users/types/referringProvider/200"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(provider200Json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetReferringProvidersByClientId(clientId, accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.True(result[0].IsDefault);
        }

        [Fact]
        public async Task GetReferringProvidersByClientId_ReturnsEmptyList_WhenApiReturnsNull()
        {
            int clientId = 20;
            int accountInfoId = 10;

            var handler = new TestHttpMessageHandler((request) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetReferringProvidersByClientId(clientId, accountInfoId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChildProfileByName_ReturnsCombinedList_WhenApisReturnData()
        {
            int accountInfoId = 10;
            string search = "abc";

            string firstJson = @"{
        ""data"": [
            { ""id"": 1, ""name"": { ""firstName"": ""abc"", ""middleName"": ""m"", ""lastName"": ""x"" } }
        ]
    }";

            string middleJson = @"{
        ""data"": [
            { ""id"": 2, ""name"": { ""firstName"": ""y"", ""middleName"": ""abc"", ""lastName"": ""z"" } }
        ]
    }";

            string lastJson = @"{
        ""data"": [
            { ""id"": 3, ""name"": { ""firstName"": ""p"", ""middleName"": ""q"", ""lastName"": ""abc"" } }
        ]
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client?firstName={search}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(firstJson, Encoding.UTF8, "application/json")
                    };
                }

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client?middleName={search}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(middleJson, Encoding.UTF8, "application/json")
                    };
                }

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client?lastname={search}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(lastJson, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""data"": [] }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileByName(accountInfoId, search);

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetChildProfileByFirstName_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            string firstName = "abc";

            string json = @"{
        ""data"": [
            { ""id"": 1, ""name"": { ""firstName"": ""abc"", ""middleName"": ""m"", ""lastName"": ""x"" } }
        ]
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client?firstName={firstName}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""data"": [] }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileByFirstName(accountInfoId, firstName);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public async Task GetChildProfileByFirstName_ReturnsEmptyList_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            string firstName = "abc";

            var handler = new TestHttpMessageHandler((request) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileByFirstName(accountInfoId, firstName);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChildProfileByMiddleName_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            string middleName = "abc";

            string json = @"{
        ""data"": [
            { ""id"": 2, ""name"": { ""firstName"": ""y"", ""middleName"": ""abc"", ""lastName"": ""z"" } }
        ]
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client?middleName={middleName}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""data"": [] }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileByMiddleName(accountInfoId, middleName);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(2, result[0].Id);
        }

        [Fact]
        public async Task GetChildProfileByMiddleName_ReturnsEmptyList_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            string middleName = "abc";

            var handler = new TestHttpMessageHandler((request) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileByMiddleName(accountInfoId, middleName);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChildProfileByLastName_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            string lastName = "abc";

            string json = @"{
        ""data"": [
            { ""id"": 3, ""name"": { ""firstName"": ""p"", ""middleName"": ""q"", ""lastName"": ""abc"" } }
        ]
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client?lastname={lastName}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""data"": [] }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileByLastName(accountInfoId, lastName);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(3, result[0].Id);
        }

        [Fact]
        public async Task GetChildProfileByLastName_ReturnsEmptyList_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            string lastName = "abc";

            var handler = new TestHttpMessageHandler((request) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileByLastName(accountInfoId, lastName);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetClientDiagnosisById_ReturnsData_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int clientId = 20;
            int clientDiagnosisId = 30;

            string json = @"{
        ""id"": 30,
        ""diagnosisId"": 999,
        ""metaData"": { ""createdOn"": ""2026-01-01T00:00:00Z"" }
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{clientId}/diagnoses/{clientDiagnosisId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetClientDiagnosisById(accountInfoId, clientId, clientDiagnosisId);

            Assert.NotNull(result);
            Assert.Equal(30, result.id);
            Assert.Equal(999, result.diagnosisId);
        }

        [Fact]
        public async Task GetDiagnosisById_ReturnsDiagnosis_WhenApiReturnsData()
        {
            int diagnosisId = 999;

            string json = @"{
        ""id"": 999,
        ""diagnosisCode"": ""DX123"",
        ""pos"": ""11"",
        ""name"": ""Sample Diagnosis"",
        ""description"": ""Test Desc"",
        ""diagnosisTypeId"": 2,
        ""metaData"": { ""createdOn"": ""2026-01-01T00:00:00Z"" }
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/definitions/diagnoses/{diagnosisId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetDiagnosisById(diagnosisId);

            Assert.NotNull(result);
            Assert.Equal(999, result.id);
            Assert.Equal("DX123", result.diagnosisCode);
        }

        [Fact]
        public async Task GetChildProfileAuthorizationDiagnosisCodesAsync_ReturnsEmpty_WhenClientDiagnosisIsNull()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int diagnosisId = 30;
            int authId = 40;

            var handler = new TestHttpMessageHandler((request) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileAuthorizationDiagnosisCodesAsync(accountInfoId, childProfileId, diagnosisId, authId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChildProfileAuthorizationDiagnosisCodesAsync_ReturnsList_WhenClientDiagnosisExists()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int clientDiagnosisId = 30;
            int authId = 40;

            string clientDiagnosisJson = @"{
        ""id"": 30,
        ""diagnosisId"": 999,
        ""metaData"": { ""createdOn"": ""2026-01-01T00:00:00Z"" }
    }";

            string diagnosisJson = @"{
        ""id"": 999,
        ""diagnosisCode"": ""DX999"",
        ""pos"": ""11"",
        ""name"": ""DiagnosisName"",
        ""description"": ""DiagnosisDesc"",
        ""diagnosisTypeId"": 2,
        ""metaData"": { ""createdOn"": ""2026-01-01T00:00:00Z"" }
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/diagnoses/{clientDiagnosisId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(clientDiagnosisJson, Encoding.UTF8, "application/json")
                    };
                }

                if (url.Contains("/definitions/diagnoses/999"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(diagnosisJson, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileAuthorizationDiagnosisCodesAsync(
                accountInfoId, childProfileId, clientDiagnosisId, authId);

            Assert.NotNull(result);
            Assert.Single(result);

            var first = result[0];
            Assert.Equal(30, first.id);
            Assert.Equal(999, first.diagnosisId);
            Assert.Equal(authId, first.childProfileAuthorizationId);
            Assert.Equal(clientDiagnosisId, first.childProfileDiagnosisId);
            Assert.True(first.includeOnClaims);
            Assert.NotNull(first.Diagnosis);
            Assert.Equal(999, first.Diagnosis.id);
        }
        [Fact]
        public async Task GetProviderBillingCode_ByBillingCode_ReturnsEmptyList_WhenApiReturnsNull()
        {
            int accountId = 10;
            string billingCode = "H001";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountId}/billingcodes?billingCode={billingCode}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetProviderBillingCode(accountId, billingCode);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetProviderBillingCodeList_ReturnsEmptyList_WhenApiReturnsNull()
        {
            int accountId = 10;

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountId}/billingcodes") && !url.Contains("billingCode="))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetProviderBillingCodeList(accountId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }
        [Fact]
        public async Task GetChildProfileFunderServiceLineMapping_ReturnsEmpty_WhenApiReturnsNull()
        {
            int accountInfoId = 10;
            int childProfileId = 20;

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                // funderMappings call
                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/funderMappings"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetChildProfileFunderServiceLineMapping(accountInfoId, childProfileId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetServiceLineMappingsByFunderId_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int childProfileId = 20;
            int mappingId = 999;

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();


                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/funderMappings/{mappingId}/serviceLineMappings"))
                {
                    string json = @$"{{
                ""data"": [
                    {{
                        ""id"": 1,
                        ""serviceId"": 101,
                        ""childProfileFunderMappingId"": {mappingId}
                    }}
                ]
            }}";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/funderMappings/{mappingId}"))
                {
                    string json = @$"{{
                ""id"": {mappingId},
                ""insuranceType"": 1,
                ""funderId"": 555,
                ""childProfileInsuranceContactId"": 123
            }}";

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                if (url.Contains($"/accounts/{accountInfoId}/users/types/10/{childProfileId}/contacts/123"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{}", Encoding.UTF8, "application/json")
                    };
                }
                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{childProfileId}/contacts/123/insurance"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{}", Encoding.UTF8, "application/json")
                    };
                }

                // 5) GetFunder(accountInfoId, funderId)
                if (url.Contains($"/accounts/{accountInfoId}/funders/555"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""funder"": { ""id"": 555 } }", Encoding.UTF8, "application/json")
                    };
                }

                // 6) GetFunderInsurancePlansForFunder(accountInfoId, childProfileId, clientFunderId)
                if (url.Contains($"/accounts/{accountInfoId}/funders/{mappingId}/insurancePlans"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""data"": [] }", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetServiceLineMappingsByFunderId(accountInfoId, childProfileId, mappingId);

            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }

        

        [Fact]
        public async Task GetServiceLine_ReturnsModel_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int serviceLineMappingId = 777;

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/serviceLines/{serviceLineMappingId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{ ""id"": 777 }", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetServiceLine(accountInfoId, serviceLineMappingId);

            Assert.NotNull(result);
        }
        [Fact]
        public async Task GetChildProfile_ReturnsList_WhenApiReturnsData()
        {
            int accountInfoId = 10;

            string json = @"
    {
        ""data"": [
            { 
                ""id"": 1, 
                ""name"": { ""firstName"": ""A"", ""middleName"": ""M"", ""lastName"": ""B"" } 
            },
            { 
                ""id"": 2, 
                ""name"": { ""firstName"": ""X"", ""middleName"": null, ""lastName"": ""Y"" } 
            }
        ]
    }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client") && url.Contains("take=1000"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-api.com")
            };

            var service = CreateService(client);

            var result = await service.GetChildProfile(accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            Assert.Equal("A M B", result[0].Name);
            Assert.Equal("X Y", result[1].Name);
        }
        [Fact]
        public async Task GetChildProfileAuthorizationById_ReturnsAuthorization_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            int authorizationId = 111;

            string json = @"{ ""id"": 111 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/all/authorizations/{authorizationId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-api.com")
            };

            var service = CreateService(client);

            var result = await service.GetChildProfileAuthorizationById(accountInfoId, authorizationId);

            Assert.NotNull(result);
            Assert.Equal(111, result.id);
        }
        [Fact]
        public async Task GetChildProfileAuthorizationByClientId_ReturnsAuthorization_WhenClientIdNotZero()
        {
            int accountInfoId = 10;
            int clientId = 20;
            int authorizationId = 111;

            string json = @"{ ""id"": 111 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/{clientId}/authorizations/{authorizationId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-api.com")
            };

            var service = CreateService(client);

            var result = await service.GetChildProfileAuthorizationByClientId(accountInfoId, clientId, authorizationId);

            Assert.NotNull(result);
            Assert.Equal(111, result.id);
        }
        [Fact]
        public async Task GetChildProfileAuthorizationByClientId_ReturnsAuthorization_WhenClientIdZero()
        {
            int accountInfoId = 10;
            int clientId = 0;
            int authorizationId = 111;

            string json = @"{ ""id"": 111 }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/{accountInfoId}/users/types/client/all/authorizations/{authorizationId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-api.com")
            };

            var service = CreateService(client);

            var result = await service.GetChildProfileAuthorizationByClientId(accountInfoId, clientId, authorizationId);

            Assert.NotNull(result);
            Assert.Equal(111, result.id);
        }

        [Fact]
        public async Task GetPropagatingAccountInfo_ReturnsName_WhenApiReturnsData()
        {
            int accountInfoId = 10;
            string json = @"{ ""name"": ""MyAccount"" }";

            var handler = new TestHttpMessageHandler((request) =>
            {
                var url = request.RequestUri!.ToString();

                if (url.Contains($"/accounts/propagating/{accountInfoId}"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""name"": """" }", Encoding.UTF8, "application/json")
                };
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("https://fake-api.com") };
            var service = CreateService(client);

            var result = await service.GetPropagatingAccountInfo(accountInfoId);

            Assert.Equal("MyAccount", result);
        }
    }

    public class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
