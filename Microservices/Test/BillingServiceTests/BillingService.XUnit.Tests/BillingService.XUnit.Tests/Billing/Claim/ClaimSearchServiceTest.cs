using AutoMapper;
using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using Moq;
using Moq.Protected;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Linq.Expressions;
using System.Collections;
using System.Threading;
using MockQueryable;

namespace BillingService.XUnit.Tests.Billing.Claim
{

    public class ClaimSearchServiceTest : BaseTest
    {
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepo;
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchClientEntity>> _clientRepo;
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchFunderEntity>> _funderRepo;
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity>> _staffRepo;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethink;
        private readonly Mock<IMapper> _mapper;


        public ClaimSearchServiceTest()
        {
            _claimRepo = new(MockBehavior.Strict);
            _clientRepo = new(MockBehavior.Strict);
            _funderRepo = new(MockBehavior.Strict);
            _staffRepo  = new(MockBehavior.Strict);
            _rethink    = new(MockBehavior.Strict);
            _mapper     = new(MockBehavior.Loose);
        }


        [Fact]
        public async Task GetAllClientsForAccount_ReturnsMappedList_WhenSourceHasItems()
        {
            // Arrange
            int accountId = 42;

            var rethinkResult = new List<ChildProfileEntityModel>
        {
            new ChildProfileEntityModel { Id = 1, FirstName = "Alice", LastName = "A" },
            new ChildProfileEntityModel { Id = 2, FirstName = "Bob",   LastName = "B" }
        };

            var mapped = new List<BaseNameOption>
        {
            new BaseNameOption { Id = 1, Name = "Alice A" },
            new BaseNameOption { Id = 2, Name = "Bob B" }
        };

            
            _rethink
                .Setup(s => s.GetChildProfilesForAccount(accountId))
                .ReturnsAsync(rethinkResult);

            _mapper
                .Setup(m => m.Map<List<BaseNameOption>>(rethinkResult))
                .Returns(mapped);

            // Repos not used here—just dummies
          

            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetAllClientsForAccount(accountId);

            // Assert
            Assert.Same(mapped, result);
            _rethink.Verify(s => s.GetChildProfilesForAccount(accountId), Times.Once);
            _mapper.Verify(m => m.Map<List<BaseNameOption>>(rethinkResult), Times.Once);
            _mapper.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task GetAllClientsForAccount_HandlesEmptyList()
        {
            // Arrange
            int accountId = 7;
            var rethinkResult = new List<ChildProfileEntityModel>();
            var mapped = new List<BaseNameOption>();

           
            _rethink
                .Setup(s => s.GetChildProfilesForAccount(accountId))
                .ReturnsAsync(rethinkResult);

           
            _mapper
                .Setup(m => m.Map<List<BaseNameOption>>(rethinkResult))
                .Returns(mapped);

            var sut = new ClaimSearchService(
                _claimRepo.Object, _clientRepo.Object, _funderRepo.Object, _staffRepo.Object,
                _rethink.Object, _mapper.Object);

            // Act
            var result = await sut.GetAllClientsForAccount(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _rethink.Verify(s => s.GetChildProfilesForAccount(accountId), Times.Once);
            _mapper.Verify(m => m.Map<List<BaseNameOption>>(rethinkResult), Times.Once);
        }

        [Fact]
        public async Task GetAllClientsForAccount_HandlesNullFromRethink()
        {
            // Arrange
            int accountId = 9;
            List<ChildProfileEntityModel> rethinkResult = null;
            var mapped = new List<BaseNameOption>(); // whatever your mapping returns for null

           
            // If your Moq lacks ReturnsAsync, use .Returns(Task.FromResult(rethinkResult))
            _rethink
                .Setup(s => s.GetChildProfilesForAccount(accountId))
                .ReturnsAsync((List<ChildProfileEntityModel>)null);

            _mapper
                .Setup(m => m.Map<List<BaseNameOption>>(rethinkResult))
                .Returns(mapped);


            var sut = new ClaimSearchService(
                _claimRepo.Object, _clientRepo.Object, _funderRepo.Object, _staffRepo.Object,
                _rethink.Object, _mapper.Object);

            // Act
            var result = await sut.GetAllClientsForAccount(accountId);

            // Assert
            Assert.Same(mapped, result);
            _rethink.Verify(s => s.GetChildProfilesForAccount(accountId), Times.Once);
            _mapper.Verify(m => m.Map<List<BaseNameOption>>(rethinkResult), Times.Once);
        }


        [Fact]
        public async Task GetFunderInfoByIds_ReturnsMatchingFunders()
        {
            // Arrange
            int accountId = 1001;

            // Mock ClaimEntity data (this will be filtered by GetUnbilledClaims)
            var claimList = new List<ClaimEntity>
        {
            new ClaimEntity
            {
                AccountInfoId = accountId,
                ClaimStatus = ClaimStatus.PendingReview,
                PrimaryFunderId = 10,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
            new ClaimEntity
            {
                AccountInfoId = accountId,
                ClaimStatus = ClaimStatus.ReadyToBill,
                PrimaryFunderId = 20,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
            // Should be excluded (wrong account)
            new ClaimEntity
            {
                AccountInfoId = 9999,
                ClaimStatus = ClaimStatus.ReadyToBill,
                PrimaryFunderId = 30,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            }
        };

            // Mock Funder Repository Data
            var funderList = new List<ClaimSearchFunderEntity>
        {
            new ClaimSearchFunderEntity { Id = 10, Name = "Funder A" },
            new ClaimSearchFunderEntity { Id = 20, Name = "Funder B" },
            new ClaimSearchFunderEntity { Id = 30, Name = "Funder C" } // should be excluded
        };

            // Setup the mocks
            _claimRepo.Setup(r => r.Query()).Returns(claimList.AsQueryable().BuildMock());

            _funderRepo.Setup(r => r.Query()).Returns(funderList.AsQueryable().BuildMock());
          
            // Create service
            var sut = new ClaimSearchService(
                 _claimRepo.Object, _clientRepo.Object, _funderRepo.Object, _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetFunderInfoByIds(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Name == "Funder A");
            Assert.Contains(result, x => x.Name == "Funder B");
            Assert.DoesNotContain(result, x => x.Name == "Funder C");

            _claimRepo.Verify(r => r.Query(), Times.AtLeastOnce);
            _funderRepo.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task GetFunderInfoByIds_ReturnsEmpty_WhenNoUnbilledClaims()
        {
            // Arrange
            int accountId = 2222;

            // No matching claim records
            var claimList = new List<ClaimEntity>();
            var funderList = new List<ClaimSearchFunderEntity>
        {
            new ClaimSearchFunderEntity { Id = 10, Name = "Funder A" }
        };

            _claimRepo.Setup(r => r.Query()).Returns(claimList.AsQueryable().BuildMock());
            _funderRepo.Setup(r => r.Query()).Returns(funderList.AsQueryable().BuildMock());
           
            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetFunderInfoByIds(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _funderRepo.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task GetFunderInfoByIds_HandlesDuplicateFunderIds()
        {
            // Arrange
            int accountId = 42;

            // Claims with duplicate funder IDs
            var claimList = new List<ClaimEntity>
            {
                new ClaimEntity
                {
                    AccountInfoId = accountId,
                    ClaimStatus = ClaimStatus.PendingReview,
                    PrimaryFunderId = 10,
                    ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
                },
                new ClaimEntity
                {
                    AccountInfoId = accountId,
                    ClaimStatus = ClaimStatus.ReadyToBill,
                    PrimaryFunderId = 10, // Duplicate funder ID
                    ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
                }
            };

            var funderList = new List<ClaimSearchFunderEntity>
            {
                new ClaimSearchFunderEntity { Id = 10, Name = "Funder A" }
            };

            _claimRepo.Setup(r => r.Query()).Returns(claimList.AsQueryable().BuildMock());

            _funderRepo.Setup(r => r.Query()).Returns(funderList.AsQueryable().BuildMock());


            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetFunderInfoByIds(accountId);

            // Assert
            Assert.Single(result);
            Assert.Equal("Funder A", result.First().Name);
            Assert.Equal(10, result.First().Id);
            _funderRepo.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task GetFunderInfoByIds_ReturnsEmpty_WhenFunderNotFound()
        {
            // Arrange
            int accountId = 42;

            var claimList = new List<ClaimEntity>
            {
                new ClaimEntity
                {
                    AccountInfoId = accountId,
                    ClaimStatus = ClaimStatus.PendingReview,
                    PrimaryFunderId = 999, // Non-existent funder ID
                    ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
                }
            };

            var funderList = new List<ClaimSearchFunderEntity>();

            _claimRepo.Setup(r => r.Query()).Returns(claimList.AsQueryable().BuildMock());

            _funderRepo.Setup(r => r.Query()).Returns(funderList.AsQueryable().BuildMock());


            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );
            // Act
            var result = await sut.GetFunderInfoByIds(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _funderRepo.Verify(r => r.Query(), Times.Once);
        }


        [Fact]
        public async Task GetClientHistoryFunderInfoByIds_ReturnsMatchingFunders_ForGivenClient()
        {
            // Arrange
            int accountId = 1001;
            int clientId = 501;

            var claimList = new List<ClaimEntity>
        {
            // match: same account & client, has links
            new ClaimEntity {
                AccountInfoId = accountId, ChildProfileId = clientId, PrimaryFunderId = 10,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
            // match: same account & client, has links
            new ClaimEntity {
                AccountInfoId = accountId, ChildProfileId = clientId, PrimaryFunderId = 20,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
            // exclude: different client
            new ClaimEntity {
                AccountInfoId = accountId, ChildProfileId = 999, PrimaryFunderId = 30,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
            // exclude: no links
            new ClaimEntity {
                AccountInfoId = accountId, ChildProfileId = clientId, PrimaryFunderId = 40,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>()
            }
        }.AsQueryable().BuildMock();

            var funderList = new List<ClaimSearchFunderEntity>
        {
            new ClaimSearchFunderEntity { Id = 10, Name = "Funder A" },
            new ClaimSearchFunderEntity { Id = 20, Name = "Funder B" },
            new ClaimSearchFunderEntity { Id = 30, Name = "Funder C" }, // not selected
            new ClaimSearchFunderEntity { Id = 40, Name = "Funder D" }  // not selected
        }.AsQueryable().BuildMock();

            _claimRepo.Setup(r => r.Query()).Returns(claimList);
            _funderRepo.Setup(r => r.Query()).Returns(funderList);

            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetClientHistoryFunderInfoByIds(accountId, clientId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == 10 && x.Name == "Funder A");
            Assert.Contains(result, x => x.Id == 20 && x.Name == "Funder B");
            Assert.DoesNotContain(result, x => x.Id == 30);
            Assert.DoesNotContain(result, x => x.Id == 40);

            _claimRepo.Verify(r => r.Query(), Times.Once);
            _funderRepo.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task GetClientHistoryFunderInfoByIds_DeduplicatesFunderIds()
        {
            // Arrange
            int accountId = 42;
            int clientId = 123;

            var claimsWithDupFunders = new List<ClaimEntity>
        {
            new ClaimEntity {
                AccountInfoId = accountId, ChildProfileId = clientId, PrimaryFunderId = 10,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
            new ClaimEntity {
                AccountInfoId = accountId, ChildProfileId = clientId, PrimaryFunderId = 10,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            }
        }.AsQueryable().BuildMock();

            var funders = new List<ClaimSearchFunderEntity>
        {
            new ClaimSearchFunderEntity { Id = 10, Name = "Funder A" },
            new ClaimSearchFunderEntity { Id = 20, Name = "Funder B" }
        }.AsQueryable().BuildMock();

           
            _claimRepo.Setup(r => r.Query()).Returns(claimsWithDupFunders);
            _funderRepo.Setup(r => r.Query()).Returns(funders);

            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetClientHistoryFunderInfoByIds(accountId, clientId);

            // Assert
            Assert.Single(result);
            Assert.Equal(10, result[0].Id);
            Assert.Equal("Funder A", result[0].Name);
        }

        [Fact]
        public async Task GetClientHistoryFunderInfoByIds_ReturnsEmpty_WhenNoClaimsForClient()
        {
            // Arrange
            int accountId = 77;
            int clientId = 888;

            var emptyClaims = new List<ClaimEntity>().AsQueryable().BuildMock();
            var funders = new List<ClaimSearchFunderEntity>
        {
            new ClaimSearchFunderEntity { Id = 10, Name = "Funder A" }
        }.AsQueryable().BuildMock();

            _claimRepo.Setup(r => r.Query()).Returns(emptyClaims);
            _funderRepo.Setup(r => r.Query()).Returns(funders);

            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetClientHistoryFunderInfoByIds(accountId, clientId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetStaffInfoByIds_ReturnsDistinctStaff_WithNames_AndTypeIds()
        {
            // Arrange
            int accountId = 1001;

            // Claims that will be included by GetUnbilledClaims (PendingReview/ReadyToBill + Any links)
            var claims = new List<ClaimEntity>
        {
            new ClaimEntity {
                AccountInfoId = accountId,
                ClaimStatus = ClaimStatus.PendingReview,
                RenderingProviderTypeId = 1,
                RenderingStaffMemberId = 11,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
            // duplicate (same staffId+typeId) should be de-duplicated
            new ClaimEntity {
                AccountInfoId = accountId,
                ClaimStatus = ClaimStatus.ReadyToBill,
                RenderingProviderTypeId = 1,
                RenderingStaffMemberId = 11,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
            // second unique staff
            new ClaimEntity {
                AccountInfoId = accountId,
                ClaimStatus = ClaimStatus.ReadyToBill,
                RenderingProviderTypeId = 2,
                RenderingStaffMemberId = 22,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
            // excluded: different account
            new ClaimEntity {
                AccountInfoId = 9999,
                ClaimStatus = ClaimStatus.ReadyToBill,
                RenderingProviderTypeId = 3,
                RenderingStaffMemberId = 33,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
            // excluded: no links
            new ClaimEntity {
                AccountInfoId = accountId,
                ClaimStatus = ClaimStatus.ReadyToBill,
                RenderingProviderTypeId = 4,
                RenderingStaffMemberId = 44,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity>()
            },
            // excluded: null staff id
            new ClaimEntity {
                AccountInfoId = accountId,
                ClaimStatus = ClaimStatus.PendingReview,
                RenderingProviderTypeId = 5,
                RenderingStaffMemberId = null,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            },
        }.AsQueryable().BuildMock(); // async-capable for ToListAsync inside GetUnbilledClaims

            var staffDirectory = new List<ClaimSearchRenderingProviderEntity>
        {
            new ClaimSearchRenderingProviderEntity { Id = 11, Name = "Therapist Alpha" },
            new ClaimSearchRenderingProviderEntity { Id = 22, Name = "Therapist Beta" },
            new ClaimSearchRenderingProviderEntity { Id = 99, Name = "Someone Else" }
        }.AsQueryable().BuildMock(); // async-capable for ToListAsync on _staffSearchRepository.Query()

            
            _claimRepo.Setup(r => r.Query()).Returns(claims);
            _staffRepo.Setup(r => r.Query()).Returns(staffDirectory);

            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetStaffInfoByIds(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // distinct (11, type 1) and (22, type 2)

            var staff11 = result.Single(x => x.Id == 11);
            Assert.Equal("Therapist Alpha", staff11.Name);
            Assert.Equal(1, staff11.TypeId);

            var staff22 = result.Single(x => x.Id == 22);
            Assert.Equal("Therapist Beta", staff22.Name);
            Assert.Equal(2, staff22.TypeId);

            _claimRepo.Verify(r => r.Query(), Times.Once);
            _staffRepo.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task GetStaffInfoByIds_AssignsNullName_WhenStaffMissingInDirectory()
        {
            // Arrange
            int accountId = 2002;

            var claims = new List<ClaimEntity>
        {
            new ClaimEntity {
                AccountInfoId = accountId,
                ClaimStatus = ClaimStatus.PendingReview,
                RenderingProviderTypeId = 7,
                RenderingStaffMemberId = 77,
                ClaimAppointmentLinks = new List<ClaimAppointmentLinkEntity> { new ClaimAppointmentLinkEntity() }
            }
        }.AsQueryable().BuildMock();

            // Staff directory does NOT contain Id 77
            var staffDirectory = new List<ClaimSearchRenderingProviderEntity>
        {
            new ClaimSearchRenderingProviderEntity { Id = 99, Name = "Other" }
        }.AsQueryable().BuildMock();


            _claimRepo.Setup(r => r.Query()).Returns(claims);
            _staffRepo.Setup(r => r.Query()).Returns(staffDirectory);

            var sut = new ClaimSearchService(
                _claimRepo.Object, _clientRepo.Object, _funderRepo.Object, _staffRepo.Object, _rethink.Object, _mapper.Object);

            // Act
            var result = await sut.GetStaffInfoByIds(accountId);

            // Assert
            Assert.Single(result);
            var item = result[0];
            Assert.Equal(77, item.Id);
            Assert.Null(item.Name);          // not found in directory -> null
            Assert.Equal(7, item.TypeId);
        }

        [Fact]
        public async Task GetStaffInfoByIds_ReturnsEmpty_WhenNoUnbilledClaims()
        {
            // Arrange
            int accountId = 3003;

            var emptyClaims = new List<ClaimEntity>().AsQueryable().BuildMock();
            var staffDirectory = new List<ClaimSearchRenderingProviderEntity>
        {
            new ClaimSearchRenderingProviderEntity { Id = 1, Name = "Anyone" }
        }.AsQueryable().BuildMock();

           

            _claimRepo.Setup(r => r.Query()).Returns(emptyClaims);
            _staffRepo.Setup(r => r.Query()).Returns(staffDirectory);

            var sut = new ClaimSearchService(
                _claimRepo.Object, _clientRepo.Object, _funderRepo.Object, _staffRepo.Object, _rethink.Object, _mapper.Object);

            // Act
            var result = await sut.GetStaffInfoByIds(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPlaceOfServiceInfoByIds_ReturnsMappedBaseNameOptions()
        {
            // Arrange
            int accountId = 101;

            var locationCodes = new List<LocationCodesModel> // 👈 Replace with your actual type if different
        {
            new LocationCodesModel { id = 1, code = "11", description = "Office" },
            new LocationCodesModel { id = 2, code = "12", description = "Home" }
        };

          
            _rethink
                .Setup(r => r.GetLocationCodes())
                .ReturnsAsync(locationCodes);

            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetPlaceOfServiceInfoByIds(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            Assert.Equal(1, result[0].Id);
            Assert.Equal("11 - Office", result[0].Name);

            Assert.Equal(2, result[1].Id);
            Assert.Equal("12 - Home", result[1].Name);

            _rethink.Verify(r => r.GetLocationCodes(), Times.Once);
        }

        [Fact]
        public async Task GetPlaceOfServiceInfoByIds_ReturnsEmptyList_WhenNoLocationCodes()
        {
            // Arrange
            int accountId = 202;
          
            _rethink.Setup(r => r.GetLocationCodes())
                       .ReturnsAsync(new List<LocationCodesModel>());


            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetPlaceOfServiceInfoByIds(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _rethink.Verify(r => r.GetLocationCodes(), Times.Once);
        }

        [Fact]
        public async Task GetPlaceOfServiceInfoByIds_HandlesNullResponse()
        {
            // Arrange
            int accountId = 303;
            List<LocationCodesModel> nullLocationCodes = null;

            _rethink.Setup(r => r.GetLocationCodes())
                       .ReturnsAsync(nullLocationCodes);


            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetPlaceOfServiceInfoByIds(accountId);

            // Assert
            // If your code doesn't handle null -> expect NullReferenceException
            // But ideally, it should return empty list instead
            Assert.NotNull(result);
            Assert.Empty(result);

            _rethink.Verify(r => r.GetLocationCodes(), Times.Once);
        }

        [Fact]
        public async Task GetLocationInfoByIds_ShouldMapLocations_ToBaseNameOptions()
        {
            // Arrange
            int accountId = 101;

            var providerLocations = new List<ProviderLocations>
        {
            new ProviderLocations { id = 1, name = "Main Center" },
            new ProviderLocations { id = 2, name = "Therapy Hub" }
        };

            var rethinkResponse = new ClientProviderLocationsModel
            {
                total = providerLocations.Count,
                data = providerLocations
            };
            _rethink.Setup(r => r.GetProviderLocationList(accountId))
                    .ReturnsAsync(rethinkResponse);

            // 👇 Directly instantiate the SUT (no helper)
            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetLocationInfoByIds(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Main Center", result[0].Name);
            Assert.Equal("Therapy Hub", result[1].Name);
            _rethink.Verify(r => r.GetProviderLocationList(accountId), Times.Once);
        }

        [Fact]
        public async Task GetLocationInfoByIds_ShouldReturnEmptyList_WhenNoData()
        {
            // Arrange
            int accountId = 102;
            var emptyResponse = new { data = new List<dynamic>() };

            var rethinkResponse = new ClientProviderLocationsModel
            {
            };

            _rethink.Setup(r => r.GetProviderLocationList(accountId))
                    .ReturnsAsync(rethinkResponse);

            var sut = new ClaimSearchService(
                _claimRepo.Object,
                _clientRepo.Object,
                _funderRepo.Object,
                _staffRepo.Object,
                _rethink.Object,
                _mapper.Object
            );

            // Act
            var result = await sut.GetLocationInfoByIds(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _rethink.Verify(r => r.GetProviderLocationList(accountId), Times.Once);
        }
    }
}
