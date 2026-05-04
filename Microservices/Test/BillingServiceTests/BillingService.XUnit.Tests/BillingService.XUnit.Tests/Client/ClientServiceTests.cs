using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Interfaces.Provider;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Clients;
using BillingService.Domain.Services.Client;
using Microsoft.Extensions.Configuration;
using Moq;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Client
{
    public class ClientServiceTests
    {
        private readonly Mock<IProviderService> _providerServiceMock = new();
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkMicroservicesRepositoryMock = new();
        private readonly Mock<IConfiguration> _configurationMock = new();
        private readonly Mock<ICacheService> _cacheService = new();

        private ClientService CreateService() =>
            new ClientService(_providerServiceMock.Object, _configurationMock.Object, _rethinkMicroservicesRepositoryMock.Object, _cacheService.Object);

        [Fact]
        public async Task GetClientFundersAsync_ReturnsEmptyList_WhenFunderMappingsNull()
        {
            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFunderMappings(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((ChildProfileFunderResponseModel)null);

            var service = CreateService();
            var result = await service.GetClientFundersAsync(1, 1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetClientFundersAsync_SkipsPrivatePayAndSelfPayFunders()
        {
            var funderMappings = new ChildProfileFunderResponseModel
            {
                data = new List<FunderDetails>
            {
                new FunderDetails
                {
                    funderId = 10,
                    metaData = new MetaData { deletedOn = null },
                    insuranceType = ResponsibilitySequenceType.Primary,
                    childProfileInsuranceContactId = 100,
                    id = 1
                },
                new FunderDetails
                {
                    funderId = 20,
                    metaData = new MetaData { deletedOn = null },
                    insuranceType = ResponsibilitySequenceType.Primary,
                    childProfileInsuranceContactId = 200,
                    id = 2
                }
            }
            };

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFunderMappings(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(funderMappings);

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetFunder(It.IsAny<int>(), 10))
                .ReturnsAsync(new FunderDataModel
                {
                    funderTypeId = (int)FunderType.PrivatePay
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetFunder(It.IsAny<int>(), 20))
                .ReturnsAsync(new FunderDataModel
                {
                    funderTypeId = (int)FunderType.SelfPay
                });

            var service = CreateService();
            var result = await service.GetClientFundersAsync(1, 1);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetClientFundersAsync_ReturnsFundersWithCorrectProperties()
        {
            var funderMappings = new ChildProfileFunderResponseModel
            {
                data = new List<FunderDetails>
            {
                new FunderDetails
                {
                    funderId = 30,
                    metaData = new MetaData { deletedOn = null },
                    insuranceType = ResponsibilitySequenceType.Primary,
                    childProfileInsuranceContactId = 300,
                    id = 3
                }
            }
            };

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFunderMappings(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(funderMappings);

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetFunder(It.IsAny<int>(), 30))
                .ReturnsAsync(new FunderDataModel
                {
                    funderTypeId = 1,
                    funderName = "TestFunder",
                    referringProviderRequiredOnClaim = true,
                    billingProviderOptionId = 99
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetInsuranceContactEntity(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new InsuranceContacts
                {
                    InsuranceContactsType = new InsuranceContactsTypeModel
                    {
                        insurancePolicyNumber = "PN123"
                    }
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetInsuranceContactsType(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new InsuranceContactsTypeModel
                {
                    insurancePolicyNumber = "PN123"
                });

            var service = CreateService();
            var result = await service.GetClientFundersAsync(1, 1);

            Assert.Single(result);
            var funder = result[0];
            Assert.Equal(3, funder.Id);
            Assert.Equal(30, funder.FunderId);
            Assert.Equal("TestFunder - PN123 - 3", funder.FunderName);
            Assert.True(funder.ReferringProviderRequiredOnClaim);
            Assert.Equal(99, funder.BillingProviderOptionId);
        }

        [Fact]
        public async Task GetClientFundersSmallAsync_HandlesNullInsuranceContactAndType()
        {
            var funderMappings = new ChildProfileFunderResponseModel
            {
                data = new List<FunderDetails>
                {
                    new FunderDetails
                    {
                        funderId = 40,
                        metaData = new MetaData { deletedOn = null },
                        insuranceType = ResponsibilitySequenceType.Primary,
                        childProfileInsuranceContactId = 400,
                        id = 4
                    }
                }
            };

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFunderMappings(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(funderMappings);

            // Ensure funderTypeId is not PrivatePay or SelfPay
            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetFunder(It.IsAny<int>(), 40))
                .ReturnsAsync(new FunderDataModel
                {
                    funderTypeId = 1,
                    funderName = "FunderNoContact",
                    referringProviderRequiredOnClaim = false,
                    billingProviderOptionId = 88
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetInsuranceContactEntity(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((InsuranceContacts)null);

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetInsuranceContactsType(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((InsuranceContactsTypeModel)null);

            var service = CreateService();
            var result = await service.GetClientFundersAsync(1, 1);

            // The funder should not be skipped, so the result should contain one item
            Assert.Single(result);
            var funder = result[0];
            Assert.Equal("FunderNoContact - 4", funder.FunderName);
            Assert.False(funder.ReferringProviderRequiredOnClaim);
            Assert.Equal(88, funder.BillingProviderOptionId);
        }

        [Fact]
        public async Task GetClientFundersSmallAsync_ReturnsExpectedFunders_WhenInsuranceContactAndTypeArePresent()
        {
            // Arrange
            var funderMappings = new ChildProfileFunderResponseModel
            {
                data = new List<FunderDetails>
                {
                    new FunderDetails
                    {
                        funderId = 101,
                        metaData = new MetaData { deletedOn = null },
                        insuranceType = ResponsibilitySequenceType.Primary,
                        childProfileInsuranceContactId = 501,
                        id = 11
                    }
                }
            };

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFunderMappings(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(funderMappings);

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetFunder(It.IsAny<int>(), 101))
                .ReturnsAsync(new FunderDataModel
                {
                    funderTypeId = 3, // Not PrivatePay or SelfPay
                    funderName = "ExpectedFunder",
                    referringProviderRequiredOnClaim = true,
                    billingProviderOptionId = 77
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetInsuranceContactEntity(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new InsuranceContacts
                {
                    InsuranceContactsType = new InsuranceContactsTypeModel
                    {
                        insurancePolicyNumber = "POLICY-XYZ"
                    }
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetInsuranceContactsType(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new InsuranceContactsTypeModel
                {
                    insurancePolicyNumber = "POLICY-XYZ"
                });

            var service = CreateService();

            // Act
            var result = await service.GetClientFundersAsync(1, 1);

            // Assert
            Assert.Single(result);
            var funder = result[0];
            Assert.Equal(11, funder.Id);
            Assert.Equal(101, funder.FunderId);
            Assert.Equal("ExpectedFunder - POLICY-XYZ - 11", funder.FunderName);
            Assert.True(funder.ReferringProviderRequiredOnClaim);
            Assert.Equal(77, funder.BillingProviderOptionId);
        }

        [Fact]
        public async Task GetClientsListForClaimAsync_ReturnsEmpty_WhenRepositoryReturnsNull()
        {
            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfile(It.IsAny<int>()))
                .ReturnsAsync((List<ChildProfileRethinkModel>)null);

            var service = CreateService();

            var result = await service.GetClientsListForClaimAsync(1, 1);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetClientsListForClaimAsync_ReturnsMappedClients_WhenRepositoryReturnsData()
        {
            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfile(It.IsAny<int>()))
                .ReturnsAsync(new List<ChildProfileRethinkModel>
                {
                    new ChildProfileRethinkModel { Id = 101, FirstName = "John", LastName = "Doe" },
                    new ChildProfileRethinkModel { Id = 102, FirstName = "Jane", LastName = "Smith" }
                });

            var service = CreateService();

            var result = await service.GetClientsListForClaimAsync(1, 1);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == 101 && x.Name.Contains("Doe"));
            Assert.Contains(result, x => x.Id == 102 && x.Name.Contains("Smith"));
        }

        [Fact]
        public async Task GetClientFacilityIdAsync_ReturnsZero_WhenFacilityIsNull()
        {
            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFacility(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((ProviderLocationModel)null);

            var service = CreateService();

            var result = await service.GetClientFacilityIdAsync(10, 20);

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetClientFacilityIdAsync_ReturnsProviderLocationId_WhenFacilityExists()
        {
            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFacility(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new ProviderLocationModel { providerLocationId = 555 });

            var service = CreateService();

            var result = await service.GetClientFacilityIdAsync(10, 20);

            Assert.Equal(555, result);
        }

        [Fact]
        public async Task SearchDiagnosis_ReturnsResults_WhenSearchIsNameBased()
        {
            var clientDiagnosis = new List<Diagnosis>
                {
                    new Diagnosis { id = 1, name = "Autism", diagnosisCode = "A11", accountInfoId = 0 },
                    new Diagnosis { id = 2, name = "Anxiety", diagnosisCode = "B22", accountInfoId = 0 },
                    new Diagnosis { id = 3, name = "Speech Delay", diagnosisCode = "C33", accountInfoId = 0 }
                };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Diagnosis>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(clientDiagnosis);

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetAllDiagnosisAsync(It.IsAny<int>()))
                .ReturnsAsync(clientDiagnosis);

            var service = CreateService();

            var result = await service.SearchDiagnosis("Au", 1, 1);

            Assert.NotNull(result);
            Assert.True(result.Count > 0);
            Assert.Contains(result, x => x.DiagnosisLUDescription.StartsWith("Autism", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SearchDiagnosis_ReturnsResults_WhenSearchIsCodeBased()
        {
            var clientDiagnosis = new List<Diagnosis>
                {
                    new Diagnosis { id = 1, name = "Autism", diagnosisCode = "A11", accountInfoId = 0 },
                    new Diagnosis { id = 2, name = "Anxiety", diagnosisCode = "B22", accountInfoId = 0 },
                    new Diagnosis { id = 3, name = "Speech Delay", diagnosisCode = "C33", accountInfoId = 0 }
                };

            _cacheService.Setup(c => c.GetOrSetCacheAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<Diagnosis>>>>(),
                It.IsAny<TimeSpan>()))
                .ReturnsAsync(clientDiagnosis);

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetAllDiagnosisAsync(It.IsAny<int>()))
                .ReturnsAsync(clientDiagnosis);

            var service = CreateService();

            var result = await service.SearchDiagnosis("A11", 1, 1);

            Assert.NotNull(result);
            Assert.True(result.Count > 0);
            Assert.Contains(result, x => x.DiagnosisLUCode.StartsWith("A11"));
        }

        [Fact]
        public async Task GetFunderServiceLinesAsync_ReturnsEmpty_WhenMappingsNull()
        {
            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetServiceLineMappingsByFunderId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((List<ServiceLines>)null);

            var service = CreateService();

            var result = await service.GetFunderServiceLinesAsync(1, 1, 1, 1);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDiagnosisForClaimWithoutAuthAsync_ReturnsOnlyActiveDiagnosis()
        {
            int accountInfoId = 1;
            int clientId = 2;
            int serviceLineId = 3;

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetClientDiagnosisByServiceId(accountInfoId, clientId, serviceLineId))
                .ReturnsAsync(new ClientDiagnosisCodeForClaimWithoutAut
                {
                    data = new List<ClientDiagnosisCodes>
                    {
                        new ClientDiagnosisCodes
                        {
                            diagnosisId = 10,
                            order = 1,
                            endDate = null,
                            diagnosis = new Diagnosis
                            {
                                diagnosisCode = "A11",
                                description = "Desc A11"
                            }
                        },
                        new ClientDiagnosisCodes
                        {
                            diagnosisId = 20,
                            order = 2,
                            endDate = DateTime.Now.AddDays(5),
                            diagnosis = new Diagnosis
                            {
                                diagnosisCode = "B22",
                                description = "Desc B22"
                            }
                        },
                        new ClientDiagnosisCodes
                        {
                            diagnosisId = 30,
                            order = 3,
                            endDate = DateTime.Now.AddDays(-5), // expired -> should be removed
                            diagnosis = new Diagnosis
                            {
                                diagnosisCode = "C33",
                                description = "Desc C33"
                            }
                        }
                    }
                });

            var service = CreateService();

            var result = await service.GetDiagnosisForClaimWithoutAuthAsync(clientId, serviceLineId, accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            Assert.Contains(result, x => x.DiagnosisCode == "A11");
            Assert.Contains(result, x => x.DiagnosisCode == "B22");
            Assert.DoesNotContain(result, x => x.DiagnosisCode == "C33");
        }

        [Fact]
        public async Task GetClientAuthorizationsForClaimAsync_ReturnsEmpty_WhenResponseNull()
        {
            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetClientAuthorizationsByClientId(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((ClientAuthorizationsModel)null);

            var service = CreateService();

            var result = await service.GetClientAuthorizationsForClaimAsync(1, 10, 20, 1);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetClientAuthorizationsForClaimAsync_FiltersByFunderAndService()
        {
            int accountInfoId = 1;
            int childProfileId = 2;
            int funderId = 50;
            int serviceLineId = 77;

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetClientAuthorizationsByClientId(accountInfoId, childProfileId))
                .ReturnsAsync(new ClientAuthorizationsModel
                {
                    data = new List<ClientAuthorization>
                    {
                        new ClientAuthorization
                        {
                            id = 1,
                            funderId = funderId,
                            providerServiceId = serviceLineId,
                            authorizationNumber = "AUTH-1",
                            startDate = new DateTime(2025, 1, 1),
                            endDate = new DateTime(2025, 12, 31)
                        },
                        new ClientAuthorization
                        {
                            id = 2,
                            funderId = 999,
                            providerServiceId = serviceLineId,
                            authorizationNumber = "AUTH-2",
                            startDate = new DateTime(2025, 1, 1),
                            endDate = new DateTime(2025, 12, 31)
                        },
                        new ClientAuthorization
                        {
                            id = 3,
                            funderId = funderId,
                            providerServiceId = 999,
                            authorizationNumber = "AUTH-3",
                            startDate = new DateTime(2025, 1, 1),
                            endDate = new DateTime(2025, 12, 31)
                        }
                    }
                });

            var service = CreateService();

            var result = await service.GetClientAuthorizationsForClaimAsync(childProfileId, funderId, serviceLineId, accountInfoId);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Contains("AUTH-1", result[0].Name);
        }

        [Fact]
        public async Task GetClientAuthorization_MapsDiagnosisAndFiltersInactiveBillingCodes()
        {
            // Arrange
            int accountInfoId = 1;
            int childProfileId = 2;
            int authorizationId = 100;
            int memberId = 500;

            var authorizationResponse = new ClientAuthorization
            {
                id = authorizationId,
                authorizationNumber = "AUTH-100",
                authorizationSubmissionTypeId = 1,
                funderId = 10,
                providerServiceId = 200,
                renderingProviderStaffId = memberId,
                authorizationRenderingProviderTypeId = 0,
                startDate = DateTime.Now.Date.AddDays(-2),
                endDate = DateTime.Now.Date.AddDays(2),
                childProfileReferringProviderId = 55,
                authorizationDistributionTypeId = 1,
                totalNumberOfUnits = 20,
                childProfileDiagnosisId = 999,
                childProfileFunderServiceLineMappingId = new List<int> { 88 }
            };

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileAuthorizationByClientId(accountInfoId, childProfileId, authorizationId))
                .ReturnsAsync(authorizationResponse);

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFunderMappings(accountInfoId, childProfileId))
                .ReturnsAsync(new ChildProfileFunderResponseModel
                {
                    data = new List<FunderDetails>
                    {
                new FunderDetails
                {
                    id = 123,
                    funderId = 10,
                    startDate = DateTime.Now.AddDays(-5),
                    endDate = DateTime.Now.AddDays(5),
                    metaData = new MetaData()
                }
                    }
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetFunder(accountInfoId, 10))
                .ReturnsAsync(new FunderDataModel
                {
                    id = 10,
                    funderName = "TestFunder",
                    funderTypeId = 1
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetMainLocation(accountInfoId))
                .ReturnsAsync(new ProviderLocations
                {
                    id = 1,
                    accountId = accountInfoId
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFacility(accountInfoId, childProfileId))
                .ReturnsAsync(new ProviderLocationModel
                {
                    providerLocationId = 999
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetAllRenderingProvidersAsync(accountInfoId))
                .ReturnsAsync(new ClientListUserModel
                {
                    data = new List<ClientUserModel>
                    {
                new ClientUserModel
                {
                    memberId = memberId,
                    id = 101
                }
                    }
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileAuthorizationDiagnosisCodesAsync(
                    accountInfoId,
                    childProfileId,
                    999,
                    authorizationId))
                .ReturnsAsync(new List<ChildProfileAuthorizationDiagnosisCode>
                {
            new ChildProfileAuthorizationDiagnosisCode
            {
                id = 1,
                diagnosisId = 999,
                includeOnClaims = true,
                order = 1,
                Diagnosis = new Diagnosis
                {
                    diagnosisCode = "D1",
                    description = "Diagnosis One"
                }
            }
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetClientAuthBillingCodesByAuthId(accountInfoId, childProfileId, authorizationId))
                .ReturnsAsync(new List<ClientAuthorizationBillingCodeModel>
                {
            new ClientAuthorizationBillingCodeModel
            {
                BillingCodeId = 11,
                NoOfUnits = 10
            },
            new ClientAuthorizationBillingCodeModel
            {
                BillingCodeId = 22,
                NoOfUnits = 20
            }
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetBillingCodeList(accountInfoId))
                .ReturnsAsync(new ClientBillingCodesModel
                {
                    data = new List<BillingCodes>
                    {
                new BillingCodes { id = 11, inactive = false },
                new BillingCodes { id = 22, inactive = true }
                    }
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFunderMappingByMappingId(accountInfoId, childProfileId, 123))
                .ReturnsAsync(new FunderDetails
                {
                    id = 123,
                    startDate = DateTime.Now.AddDays(-5),
                    endDate = DateTime.Now.AddDays(5)
                });

            var service = CreateService();

            // Act
            var result = await service.GetClientAuthorization(
                authorizationId,
                childProfileId,
                memberId,
                accountInfoId,
                "en-US");

            // Assert basic mapping
            Assert.NotNull(result);
            Assert.Equal(authorizationId, result.Id);
            Assert.Equal("AUTH-100", result.AuthorizationNumber);
            Assert.Equal(10, result.FunderId);
            Assert.Equal(memberId, result.RenderingProviderId);

            // Assert diagnosis mapping
            Assert.NotNull(result.DiagnosisCodes);
            Assert.Single(result.DiagnosisCodes);
            Assert.Equal("D1", result.DiagnosisCodes[0].DiagnosisCode);
            Assert.Equal("Diagnosis One", result.DiagnosisCodes[0].Description);

            // Assert billing codes filtering (only active billing code)
            Assert.NotNull(result.BillingCodes);
            Assert.Single(result.BillingCodes);
            Assert.Equal(11, result.BillingCodes[0].BillingCodeId);

            // Assert active flags            
            Assert.True(result.IsFunderValid);
            Assert.True(result.IsInactiveDateValid);
            Assert.True(result.IsStartDateValid);
            Assert.True(result.IsEndDateValid);
        }

        [Fact]
        public async Task GetClientFunderResponsiblePartiesAsync_ReturnsEmptyInsuranceContact_WhenMappingIsNull()
        {
            int memberId = 1;
            int accountId = 10;
            int clientId = 20;
            int clientFunderId = 999;

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfileFunderMappings(accountId, clientId))
                .ReturnsAsync((ChildProfileFunderResponseModel)null);

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetChildProfile(accountId, clientId))
                .ReturnsAsync(new ClientUserModel
                {
                    id = clientId,
                    name = null // SAFE: we never dereference name directly
                });

            var service = CreateService();

            var result = await service.GetClientFunderResponsiblePartiesAsync(
                memberId, accountId, clientId, clientFunderId);

            Assert.NotNull(result);
            Assert.NotNull(result.InsuranceContact);
            Assert.NotNull(result.ClientDemographics);
            Assert.Equal(clientId, result.ClientDemographics.Id);
        }
        [Fact]
        public async Task GetClaimCreateInfoAsync_ReturnsEmptyBillingCodes_WhenBillingCodeResultIsNull()
        {
            int accountInfoId = 1;

            var model = new ClaimCreateInfoGetModel
            {
                ClientId = 10,
                FunderId = 20,
                ServiceId = 30
            };

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetRenderingProvidersAsync(accountInfoId, true))
                .ReturnsAsync(new List<AuthRenderingProviderType>());

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetBillingCodeList(accountInfoId))
                .ReturnsAsync((ClientBillingCodesModel)null);

            _providerServiceMock
                .Setup(x => x.GetProviderLocationList(accountInfoId, It.IsAny<Newtonsoft.Json.JsonSerializerSettings>()))
                .ReturnsAsync(new List<ProviderLocationData>());

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetReferringProvidersByClientId(model.ClientId, accountInfoId))
                .ReturnsAsync(new List<ReferringProviderDropdownModel>());

            var service = CreateService();

            var result = await service.GetClaimCreateInfoAsync(model, accountInfoId);

            Assert.NotNull(result);
            Assert.Empty(result.BillingCodes);
            Assert.Empty(result.ReferringProviders);
            Assert.Empty(result.Locations);
        }
        [Fact]
        public async Task GetClaimCreateInfoAsync_FiltersOutBillingCodes_WhenServiceAndFunderDoNotMatch()
        {
            int accountInfoId = 1;

            var model = new ClaimCreateInfoGetModel
            {
                ClientId = 10,
                FunderId = 99,
                ServiceId = 88
            };

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetRenderingProvidersAsync(accountInfoId, true))
                .ReturnsAsync(new List<AuthRenderingProviderType>());

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetBillingCodeList(accountInfoId))
                .ReturnsAsync(new ClientBillingCodesModel
                {
                    data = new List<BillingCodes>
                    {
                new BillingCodes
                {
                    id = 1,
                    providerServiceId = 1, // mismatch
                    funderId = 1,          // mismatch
                    metaData = new MetaData { deletedOn = null }
                }
                    }
                });

            _providerServiceMock
                .Setup(x => x.GetProviderLocationList(accountInfoId, It.IsAny<Newtonsoft.Json.JsonSerializerSettings>()))
                .ReturnsAsync(new List<ProviderLocationData>());

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetReferringProvidersByClientId(model.ClientId, accountInfoId))
                .ReturnsAsync(new List<ReferringProviderDropdownModel>());

            var service = CreateService();

            var result = await service.GetClaimCreateInfoAsync(model, accountInfoId);

            Assert.NotNull(result);
            Assert.Empty(result.BillingCodes);
        }
        [Fact]
        public async Task GetClaimCreateInfoAsync_CoversBillingCodeForeachBlock_Completely()
        {
            int accountInfoId = 1;

            var model = new ClaimCreateInfoGetModel
            {
                ClientId = 10,
                FunderId = 20,
                ServiceId = 30
            };

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetRenderingProvidersAsync(accountInfoId, true))
                .ReturnsAsync(new List<AuthRenderingProviderType>());

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetBillingCodeList(accountInfoId))
                .ReturnsAsync(new ClientBillingCodesModel
                {
                    data = new List<BillingCodes>
                    {
                new BillingCodes
                {
                    id = 1,
                    funderId = model.FunderId,
                    providerServiceId = model.ServiceId,
                    metaData = new MetaData { deletedOn = null },

                    inactive = false,
                    billingCode = "BC-FOREACH",
                    billingCode2 = "BC-FOREACH-2",
                    description = "Foreach Coverage Code",
                    frequencyTypeId = 5,
                    noAuthRequired = true,
                    serviceId = 77,
                    unitTypeId = 1,
                    unitTypeId2 = 2,
                    rate = 10,
                    rate2 = 20
                }
                    }
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetProviderService(accountInfoId, 77))
                .ReturnsAsync(new ClientProviderServiceModel
                {
                    name = "ABA Therapy"
                });

            _providerServiceMock
                .Setup(x => x.GetProviderLocationList(accountInfoId, It.IsAny<Newtonsoft.Json.JsonSerializerSettings>()))
                .ReturnsAsync(new List<ProviderLocationData>());

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetReferringProvidersByClientId(model.ClientId, accountInfoId))
                .ReturnsAsync(new List<ReferringProviderDropdownModel>());

            var service = CreateService();

            // ACT
            var result = await service.GetClaimCreateInfoAsync(model, accountInfoId);

            // ASSERT
            Assert.NotNull(result);
            Assert.NotNull(result.BillingCodes);
            Assert.Single(result.BillingCodes);

            var billingCode = result.BillingCodes.First();

            Assert.Equal("BC-FOREACH", billingCode.BillingCodeName);
            Assert.Equal("BC-FOREACH-2", billingCode.BillingCodeName2);
            Assert.Equal("Foreach Coverage Code", billingCode.BillingCodeDescription);
            Assert.Equal(1, billingCode.BillingCodeId);
            Assert.Equal(model.FunderId, billingCode.FunderId);
            Assert.Equal(model.ServiceId, billingCode.ProviderServiceId);
            Assert.Equal(77, billingCode.ServiceLineId);            
        }

        [Fact]
        public async Task GetFunderServiceLinesAsync_ReturnsServiceLines_WhenValidMappingsExist()
        {
            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetServiceLineMappingsByFunderId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ServiceLines>
                {
                    new ServiceLines
                    {
                        id = 1,
                        serviceId = 100,
                        responsibilitySequence = ResponsibilitySequenceType.Primary,
                        metaData = new MetaData { deletedOn = null }
                    },
                    new ServiceLines
                    {
                        id = 2,
                        serviceId = 200,
                        responsibilitySequence = ResponsibilitySequenceType.Primary,
                        metaData = new MetaData { deletedOn = DateTime.Now } 
                    }
                });

            _rethinkMicroservicesRepositoryMock
                .Setup(x => x.GetServiceLine(It.IsAny<int>(), 100))
                .ReturnsAsync(new ChildProfileServiceLines { name = "ABA Therapy" });

            var service = CreateService();

            var result = await service.GetFunderServiceLinesAsync(1, 1, 1, 1);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(100, result[0].ServiceId);
            Assert.Equal("ABA Therapy", result[0].Name);
        }
    }
}
