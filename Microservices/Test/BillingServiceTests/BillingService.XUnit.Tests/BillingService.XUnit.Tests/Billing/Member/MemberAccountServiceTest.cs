using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using Moq;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Member
{
    public class MemberAccountServiceTest : BaseTest
    {
        private readonly Mock<IRethinkMasterDataMicroServices> _mockRethinkServices;
        private readonly Mock<IMapper> _mockMapper;

        public MemberAccountServiceTest()
        {
            _mockRethinkServices = new Mock<IRethinkMasterDataMicroServices>();
            _mockMapper = new Mock<IMapper>();
        }
        private MemberAccountService CreateSut() => new MemberAccountService(
           _mockRethinkServices.Object,
           _mockMapper.Object
       );

        [Fact]
        public async Task GetMembersByAccountInfoId_ReturnsListOfMemberItems_WhenMembersExist()
        {
            // Arrange
            var accountInfoId = 123;

            var mockMemberData = new List<RethinkAccountMember>
            {
                new RethinkAccountMember
                {
                    id = 1,
                    userName = "john.doe",
                    email = "john.doe@example.com",
                    accountId = accountInfoId,
                    firstName = "John",
                    lastName = "Doe"
                },
                new RethinkAccountMember
                {
                    id = 2,
                    userName = "jane.smith",
                    email = "jane.smith@example.com",
                    accountId = accountInfoId,
                    firstName = "Jane",
                    lastName = "Smith"
                },
                new RethinkAccountMember
                {
                    id = 3,
                    userName = "bob.johnson",
                    email = "bob.johnson@example.com",
                    accountId = accountInfoId,
                    firstName = "Bob",
                    lastName = "Johnson"
                }
            };

            var mockResponse = new RethinkAccountMembersListModel
            {
                total = 3,
                data = mockMemberData
            };

            _mockRethinkServices
                .Setup(x => x.GetMemberListAsync(accountInfoId))
                .ReturnsAsync(mockResponse);

            var sut = CreateSut();

            // Act
            var result = await sut.GetMembersByAccountInfoId(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            // Verify first member
            var firstMember = result.First(x => x.Id == 1);
            Assert.Equal("john.doe", firstMember.UserName);
            Assert.Equal("john.doe@example.com", firstMember.Email);
            Assert.Equal(accountInfoId, firstMember.AccountInfoId);
            Assert.Equal("john.doe", firstMember.Title);
            Assert.Equal("John", firstMember.FirstName);
            Assert.Equal("Doe", firstMember.LastName);

            // Verify second member
            var secondMember = result.First(x => x.Id == 2);
            Assert.Equal("jane.smith", secondMember.UserName);
            Assert.Equal("jane.smith@example.com", secondMember.Email);
            Assert.Equal("Jane", secondMember.FirstName);
            Assert.Equal("Smith", secondMember.LastName);

            // Verify third member
            var thirdMember = result.First(x => x.Id == 3);
            Assert.Equal("bob.johnson", thirdMember.UserName);
            Assert.Equal("Bob", thirdMember.FirstName);
            Assert.Equal("Johnson", thirdMember.LastName);

            _mockRethinkServices.Verify(x => x.GetMemberListAsync(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetMembersByAccountInfoId_ReturnsEmptyList_WhenNoMembersExist()
        {
            // Arrange
            var accountInfoId = 456;

            var mockResponse = new RethinkAccountMembersListModel
            {
                total = 0,
                data = new List<RethinkAccountMember>()
            };

            _mockRethinkServices
                .Setup(x => x.GetMemberListAsync(accountInfoId))
                .ReturnsAsync(mockResponse);

            var sut = CreateSut();

            // Act
            var result = await sut.GetMembersByAccountInfoId(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockRethinkServices.Verify(x => x.GetMemberListAsync(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetMembersByAccountInfoId_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var accountInfoId = 789;

            var mockMemberData = new List<RethinkAccountMember>
            {
                new RethinkAccountMember
                {
                    id = 10,
                    userName = "test.user",
                    email = "test@example.com",
                    accountId = accountInfoId,
                    firstName = "Test",
                    lastName = "User"
                }
            };

            var mockResponse = new RethinkAccountMembersListModel
            {
                total = 1,
                data = mockMemberData
            };

            _mockRethinkServices
                .Setup(x => x.GetMemberListAsync(accountInfoId))
                .ReturnsAsync(mockResponse);

            var sut = CreateSut();

            // Act
            var result = await sut.GetMembersByAccountInfoId(accountInfoId);

            // Assert
            Assert.Single(result);
            var member = result.First();

            Assert.Equal(10, member.Id);
            Assert.Equal("test.user", member.UserName);
            Assert.Equal("test@example.com", member.Email);
            Assert.Equal(accountInfoId, member.AccountInfoId);
            Assert.Equal("test.user", member.Title);
            Assert.Equal("Test", member.FirstName);
            Assert.Equal("User", member.LastName);

            _mockRethinkServices.Verify(x => x.GetMemberListAsync(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetMembersByAccountInfoId_HandlesNullProperties()
        {
            // Arrange
            var accountInfoId = 999;

            var mockMemberData = new List<RethinkAccountMember>
            {
                new RethinkAccountMember
                {
                    id = 5,
                    userName = "minimal.user",
                    email = null,
                    accountId = accountInfoId,
                    firstName = null,
                    lastName = null
                }
            };

            var mockResponse = new RethinkAccountMembersListModel
            {
                total = 1,
                data = mockMemberData
            };

            _mockRethinkServices
                .Setup(x => x.GetMemberListAsync(accountInfoId))
                .ReturnsAsync(mockResponse);

            var sut = CreateSut();

            // Act
            var result = await sut.GetMembersByAccountInfoId(accountInfoId);

            // Assert
            Assert.Single(result);
            var member = result.First();

            Assert.Equal(5, member.Id);
            Assert.Equal("minimal.user", member.UserName);
            Assert.Null(member.Email);
            Assert.Equal(accountInfoId, member.AccountInfoId);
            Assert.Equal("minimal.user", member.Title);
            Assert.Null(member.FirstName);
            Assert.Null(member.LastName);

            _mockRethinkServices.Verify(x => x.GetMemberListAsync(accountInfoId), Times.Once);
        }
    }
}
