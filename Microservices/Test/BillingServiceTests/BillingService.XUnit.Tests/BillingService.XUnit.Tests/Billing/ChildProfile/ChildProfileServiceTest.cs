using AutoFixture;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using Moq;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.ChildProfile
{

    public class ChildProfileServiceTest : BaseTest
    {
        private Mock<IRethinkMasterDataMicroServices> _rethinkServices;
        private IChildProfileService _childProfileService;

        public ChildProfileServiceTest()
        {
            _rethinkServices = new Mock<IRethinkMasterDataMicroServices>();
            _childProfileService = new ChildProfileService(_rethinkServices.Object);
        }

        [Fact]
        public async Task GetAccountPatientsByNameAsync_ReturnsFilteredSortedPatients()
        {
            // Arrange
            var personSearch = Fixture.Build<PersonSearchModel>().With(x => x.PersonName, "john").Create();

            var childProfileList = new List<ChildProfileRethinkModel>
            {
             Fixture.Build<ChildProfileRethinkModel>()
                         .With(x => x.Id, 1)
                         .With(x => x.Name, "John Smith")
                         .With(x => x.DateDeleted, (DateTime?)null)
                         .Create(),

             Fixture.Build<ChildProfileRethinkModel>()
                         .With(x=>x.Id,2)
                         .With(x=>x.Name, "Alice Johnson")
                         .With(x => x.DateDeleted, (DateTime?)null)
                        .Create(),

             Fixture.Build<ChildProfileRethinkModel>()
                .With(x => x.Id, 3)
                .With(x => x.Name, "Peter Parker")
                .With(x => x.DateDeleted, (DateTime?)null)
                .Create(),
            };

            _rethinkServices.Setup(x => x.GetChildProfile(personSearch.AccountInfoId))
                            .ReturnsAsync(childProfileList);

            //Act
            var result = await _childProfileService.GetAccountPatinetsByNameAsync(personSearch);
            var resultList = result.ToList();
            Assert.NotNull(resultList);
            Assert.Equal(2, resultList.Count);

            Assert.Collection(resultList,
                   item => Assert.Equal("Alice Johnson", item.PatientName),
                   item => Assert.Equal("John Smith", item.PatientName)
               );

            _rethinkServices.Verify(
                x => x.GetChildProfile(personSearch.AccountInfoId),
                Times.Once
            );


        }

        [Fact]
        public async Task GetAccountPatientsByNameAsync_ReturnsEmptyList_WhenNoMatchesFound()
        {
            // Arrange
            var personSearch = Fixture.Build<PersonSearchModel>()
                .With(x => x.PersonName, "Robert")
                .Create();

            var childProfileList = new List<ChildProfileRethinkModel>
    {
        Fixture.Build<ChildProfileRethinkModel>()
            .With(x => x.Name, "John Smith")
            .With(x => x.DateDeleted, (DateTime?)null) // Ensure not deleted
            .Create(),
        Fixture.Build<ChildProfileRethinkModel>()
            .With(x => x.Name, "Alice Johnson")
            .With(x => x.DateDeleted, (DateTime?)null) // Ensure not deleted
            .Create()
    };

            _rethinkServices.Setup(x => x.GetChildProfile(personSearch.AccountInfoId))
                            .ReturnsAsync(childProfileList);

            // Act
            var result = await _childProfileService.GetAccountPatinetsByNameAsync(personSearch);
            var resultList = result.ToList();

            // Assert
            Assert.NotNull(resultList);
            Assert.Empty(resultList);

            _rethinkServices.Verify(
                x => x.GetChildProfile(personSearch.AccountInfoId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAccountPatientsByNameAsync_MatchesCaseInsensitive()
        {
            // Arrange
            var personSearch = Fixture.Build<PersonSearchModel>()
                .With(x => x.PersonName, "SATYAM")
                .Create();

            var childProfileList = new List<ChildProfileRethinkModel>
            {
                Fixture.Build<ChildProfileRethinkModel>().With(x => x.Name, "satyam singh").Create(),
                Fixture.Build<ChildProfileRethinkModel>().With(x => x.Name, "Peter Parker").Create()
            };

            _rethinkServices.Setup(x => x.GetChildProfile(personSearch.AccountInfoId))
                            .ReturnsAsync(childProfileList);

            // Act
            var result = await _childProfileService.GetAccountPatinetsByNameAsync(personSearch);
            var resultList = result.ToList();

            // Assert
            Assert.Single(resultList);
            Assert.Equal("satyam singh", resultList.First().PatientName);
        }


        [Fact]
        public async Task GetAccountPatientsByNameAsync_ReturnsEmptyList_WhenRethinkReturnsEmpty()
        {
            // Arrange
            var personSearch = Fixture.Build<PersonSearchModel>()
                .With(x => x.PersonName, "john")
                .Create();

            _rethinkServices.Setup(x => x.GetChildProfile(personSearch.AccountInfoId))
                            .ReturnsAsync(new List<ChildProfileRethinkModel>());

            // Act
            var result = await _childProfileService.GetAccountPatinetsByNameAsync(personSearch);
            var resultList = result.ToList();

            // Assert
            Assert.NotNull(resultList);
            Assert.Empty(resultList);

            _rethinkServices.Verify(
                x => x.GetChildProfile(personSearch.AccountInfoId),
                Times.Once
            );
        }


        [Fact]
        public async Task GetAccountPatientsByNameAsync_ReturnsAlphabeticallySortedPatients()
        {
            // Arrange
            var personSearch = Fixture.Build<PersonSearchModel>()
                .With(x => x.PersonName, "ann")
                .Create();

            var childProfileList = new List<ChildProfileRethinkModel>
        {
            Fixture.Build<ChildProfileRethinkModel>().With(x => x.Name, "Anna Brown").Create(),
            Fixture.Build<ChildProfileRethinkModel>().With(x => x.Name, "Ann Parker").Create(),
            Fixture.Build<ChildProfileRethinkModel>().With(x => x.Name, "Annabelle White").Create(),
        };

            _rethinkServices.Setup(x => x.GetChildProfile(personSearch.AccountInfoId))
                            .ReturnsAsync(childProfileList);

            // Act
            var result = await _childProfileService.GetAccountPatinetsByNameAsync(personSearch);
            var resultList = result.ToList();

            // Assert
            Assert.Equal(3, resultList.Count);

            Assert.Collection(resultList,
                item => Assert.Equal("Ann Parker", item.PatientName),
                item => Assert.Equal("Anna Brown", item.PatientName),
                item => Assert.Equal("Annabelle White", item.PatientName)
            );

            _rethinkServices.Verify(
                x => x.GetChildProfile(personSearch.AccountInfoId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAccountPatientsByNameAsync_ReturnsAll_WhenSearchTermEmpty()
        {
            // Arrange
            var personSearch = Fixture.Build<PersonSearchModel>()
                .With(x => x.PersonName, string.Empty)
                .Create();

            var childProfileList = new List<ChildProfileRethinkModel>
            {
                Fixture.Build<ChildProfileRethinkModel>().With(x => x.Name, "Zoe Z").Create(),
                Fixture.Build<ChildProfileRethinkModel>().With(x => x.Name, "Adam A").Create(),
                Fixture.Build<ChildProfileRethinkModel>().With(x => x.Name, "Mary M").Create(),
            };

            _rethinkServices.Setup(x => x.GetChildProfile(personSearch.AccountInfoId))
                            .ReturnsAsync(childProfileList);

            // Act
            var result = await _childProfileService.GetAccountPatinetsByNameAsync(personSearch);
            var list = result.ToList();

            // Assert: all returned and sorted ascending
            Assert.Equal(3, list.Count);
            Assert.Collection(list,
                i => Assert.Equal("Adam A", i.PatientName),
                i => Assert.Equal("Mary M", i.PatientName),
                i => Assert.Equal("Zoe Z", i.PatientName)
            );
        }

        [Fact]
        public async Task GetAccountPatientsByNameAsync_Throws_WhenSearchTermNull()
        {
            // Arrange
            var personSearch = Fixture.Build<PersonSearchModel>()
                .With(x => x.PersonName, (string)null)
                .Create();

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _childProfileService.GetAccountPatinetsByNameAsync(personSearch));
        }
    }
}
