using AutoMapper;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Payment;
using MockQueryable;
using Moq;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Payment
{
    public class PaymentMethodServiceTest
    {
        private readonly Mock<IRepository<BillingDbContext, PaymentMethodEntity>> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly PaymentMethodService _service;

        public PaymentMethodServiceTest()
        {
            _mockRepository = new Mock<IRepository<BillingDbContext, PaymentMethodEntity>>();
            _mockMapper = new Mock<IMapper>();
            _service = new PaymentMethodService(_mockRepository.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WhenMethodExists_ReturnsPaymentMethodModel()
        {
            // Arrange
            var methodName = "Credit Card";
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = 1,
                Name = methodName,
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var expectedModel = new PaymentMethodModel
            {
                Id = 1,
                Name = methodName
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodByName(methodName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
            _mockRepository.Verify(x => x.Query(), Times.Once);
            _mockMapper.Verify(x => x.Map<PaymentMethodModel>(paymentMethodEntity), Times.Once);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WhenMethodDoesNotExist_ReturnsEmptyModel()
        {
            // Arrange
            var methodName = "NonExistentMethod";
            var entities = new List<PaymentMethodEntity>();
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);

            // Act
            var result = await _service.GetPaymentMethodByName(methodName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
            Assert.Null(result.Name);
            _mockRepository.Verify(x => x.Query(), Times.Once);
            _mockMapper.Verify(x => x.Map<PaymentMethodModel>(It.IsAny<PaymentMethodEntity>()), Times.Never);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WhenMultipleMethodsExist_ReturnsFirstMatch()
        {
            // Arrange
            var methodName = "Cash";
            var paymentMethodEntity1 = new PaymentMethodEntity
            {
                Id = 1,
                Name = methodName,
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var paymentMethodEntity2 = new PaymentMethodEntity
            {
                Id = 2,
                Name = "Check",
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var expectedModel = new PaymentMethodModel
            {
                Id = 1,
                Name = methodName
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity1, paymentMethodEntity2 };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity1))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodByName(methodName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WhenMapperReturnsNull_ReturnsNull()
        {
            // Arrange
            var methodName = "Credit Card";
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = 1,
                Name = methodName,
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity))
                       .Returns((PaymentMethodModel)null);

            // Act
            var result = await _service.GetPaymentMethodByName(methodName);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(x => x.Query(), Times.Once);
            _mockMapper.Verify(x => x.Map<PaymentMethodModel>(paymentMethodEntity), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetPaymentMethodByName_WhenMethodNameIsNullOrWhitespace_ReturnsEmptyModel(string methodName)
        {
            // Arrange
            var entities = new List<PaymentMethodEntity>
            {
                new PaymentMethodEntity { Id = 1, Name = "Cash", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 2, Name = "Credit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow }
            };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);

            // Act
            var result = await _service.GetPaymentMethodByName(methodName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
            _mockRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WhenNameIsCaseSensitive_DoesNotReturnDifferentCase()
        {
            // Arrange
            var methodName = "credit card";
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = 1,
                Name = "Credit Card",
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);

            // Act
            var result = await _service.GetPaymentMethodByName(methodName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
            _mockRepository.Verify(x => x.Query(), Times.Once);
            _mockMapper.Verify(x => x.Map<PaymentMethodModel>(It.IsAny<PaymentMethodEntity>()), Times.Never);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WithSpecialCharacters_ReturnsCorrectMethod()
        {
            // Arrange
            var methodName = "Wire Transfer - ACH";
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = 1,
                Name = methodName,
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var expectedModel = new PaymentMethodModel
            {
                Id = 1,
                Name = methodName
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodByName(methodName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WithLongMethodName_ReturnsCorrectMethod()
        {
            // Arrange
            var methodName = "Electronic Payment Gateway Transaction with Extended Processing";
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = 1,
                Name = methodName,
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var expectedModel = new PaymentMethodModel
            {
                Id = 1,
                Name = methodName
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodByName(methodName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WhenRepositoryReturnsNull_ThrowsException()
        {
            // Arrange
            _mockRepository.Setup(x => x.Query()).Returns((IQueryable<PaymentMethodEntity>)null);

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _service.GetPaymentMethodByName("Cash"));

            _mockRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            _mockRepository.Setup(x => x.Query()).Throws(new Exception("Database failure"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.GetPaymentMethodByName("Cash"));

            Assert.Equal("Database failure", ex.Message);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WhenMapperThrowsException_PropagatesException()
        {
            // Arrange
            var methodName = "Cash";

            var entity = new PaymentMethodEntity
            {
                Id = 1,
                Name = methodName
            };

            var entities = new List<PaymentMethodEntity> { entity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(entity))
                       .Throws(new Exception("Mapping failed"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.GetPaymentMethodByName(methodName));

            Assert.Equal("Mapping failed", ex.Message);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WhenEntityNameIsNull_ReturnsEmptyModel()
        {
            // Arrange
            var entities = new List<PaymentMethodEntity>
    {
        new PaymentMethodEntity { Id = 1, Name = null }
    };

            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);

            // Act
            var result = await _service.GetPaymentMethodByName("Cash");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
        }

        [Fact]
        public async Task GetPaymentMethodByName_UsesCorrectNameFilter()
        {
            var methodName = "Cash";

            var entity = new PaymentMethodEntity
            {
                Id = 1,
                Name = methodName
            };

            var entities = new List<PaymentMethodEntity> { entity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);

            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(
                It.Is<PaymentMethodEntity>(e => e.Name == methodName)))
                .Returns(new PaymentMethodModel { Id = 1, Name = methodName });

            var result = await _service.GetPaymentMethodByName(methodName);

            Assert.Equal(methodName, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WithLeadingTrailingSpaces_DoesNotMatch()
        {
            // Arrange
            var methodName = "Cash";
            var entity = new PaymentMethodEntity
            {
                Id = 1,
                Name = methodName
            };

            var entities = new List<PaymentMethodEntity> { entity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);

            // Act
            var result = await _service.GetPaymentMethodByName("  Cash  ");

            // Assert
            Assert.Equal(0, result.Id);
        }

        [Fact]
        public async Task GetPaymentMethodByName_WithLargeDataset_ReturnsCorrectMatch()
        {
            var methodName = "TargetMethod";

            var entities = Enumerable.Range(1, 1000)
                .Select(i => new PaymentMethodEntity
                {
                    Id = i,
                    Name = "Method" + i
                })
                .ToList();

            entities.Add(new PaymentMethodEntity
            {
                Id = 2000,
                Name = methodName
            });

            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(It.IsAny<PaymentMethodEntity>()))
                .Returns((PaymentMethodEntity e) => new PaymentMethodModel
                {
                    Id = e.Id,
                    Name = e.Name
                });

            var result = await _service.GetPaymentMethodByName(methodName);

            Assert.Equal(2000, result.Id);
        }

        [Fact]
        public async Task GetPaymentMethodById_WhenMapperReturnsNull_ReturnsNull()
        {
            // Arrange
            var methodId = 1;
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = methodId,
                Name = "Credit Card",
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity))
                       .Returns((PaymentMethodModel)null);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(x => x.Query(), Times.Once);
            _mockMapper.Verify(x => x.Map<PaymentMethodModel>(paymentMethodEntity), Times.Once);
        }

        [Fact]
        public async Task GetPaymentMethodById_WhenMethodExists_ReturnsPaymentMethodModel()
        {
            // Arrange
            var methodId = 1;
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = methodId,
                Name = "Credit Card",
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var expectedModel = new PaymentMethodModel
            {
                Id = methodId,
                Name = "Credit Card"
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
            _mockRepository.Verify(x => x.Query(), Times.Once);
            _mockMapper.Verify(x => x.Map<PaymentMethodModel>(paymentMethodEntity), Times.Once);
        }

        [Fact]
        public async Task GetPaymentMethodById_WhenMethodDoesNotExist_ReturnsEmptyModel()
        {
            // Arrange
            var methodId = 999;
            var entities = new List<PaymentMethodEntity>();
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
            Assert.Null(result.Name);
            _mockRepository.Verify(x => x.Query(), Times.Once);
            _mockMapper.Verify(x => x.Map<PaymentMethodModel>(It.IsAny<PaymentMethodEntity>()), Times.Never);
        }

        [Fact]
        public async Task GetPaymentMethodById_WhenMultipleMethodsExist_ReturnsCorrectMatch()
        {
            // Arrange
            var methodId = 3;
            var paymentMethodEntity1 = new PaymentMethodEntity
            {
                Id = 1,
                Name = "Cash",
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var paymentMethodEntity2 = new PaymentMethodEntity
            {
                Id = 2,
                Name = "Check",
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var paymentMethodEntity3 = new PaymentMethodEntity
            {
                Id = 3,
                Name = "Credit Card",
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var expectedModel = new PaymentMethodModel
            {
                Id = 3,
                Name = "Credit Card"
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity1, paymentMethodEntity2, paymentMethodEntity3 };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity3))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
            _mockRepository.Verify(x => x.Query(), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        [InlineData(-100)]
        [InlineData(-999)]
        public async Task GetPaymentMethodById_WhenIdIsZeroOrNegative_ReturnsEmptyModel(int methodId)
        {
            // Arrange
            var entities = new List<PaymentMethodEntity>
            {
                new PaymentMethodEntity { Id = 1, Name = "Cash", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 2, Name = "Credit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 3, Name = "Debit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow }
            };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
            Assert.Null(result.Name);
            _mockRepository.Verify(x => x.Query(), Times.Once);
            _mockMapper.Verify(x => x.Map<PaymentMethodModel>(It.IsAny<PaymentMethodEntity>()), Times.Never);
        }

        [Fact]
        public async Task GetPaymentMethodById_WithLargeValidId_ReturnsCorrectMethod()
        {
            // Arrange
            var methodId = 999999;
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = methodId,
                Name = "High Volume Payment Method",
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var expectedModel = new PaymentMethodModel
            {
                Id = methodId,
                Name = "High Volume Payment Method"
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodById_WithMaxIntValue_ReturnsCorrectMethodIfExists()
        {
            // Arrange
            var methodId = int.MaxValue;
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = methodId,
                Name = "Boundary Test Payment",
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var expectedModel = new PaymentMethodModel
            {
                Id = methodId,
                Name = "Boundary Test Payment"
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodById_WithEmptyRepository_ReturnsEmptyModel()
        {
            // Arrange
            var methodId = 1;
            var entities = new List<PaymentMethodEntity>();
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
            Assert.Null(result.Name);
            _mockRepository.Verify(x => x.Query(), Times.Once);
        }

        [Fact]
        public async Task GetPaymentMethodById_WhenRepositoryHasSingleRecord_ReturnsCorrectRecord()
        {
            // Arrange
            var methodId = 5;
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = methodId,
                Name = "Wire Transfer",
                CreatedBy = 2,
                DateCreated = DateTime.UtcNow
            };

            var expectedModel = new PaymentMethodModel
            {
                Id = methodId,
                Name = "Wire Transfer"
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodById_WhenIdExistsInMiddleOfCollection_ReturnsCorrectMethod()
        {
            // Arrange
            var methodId = 5;
            var entities = new List<PaymentMethodEntity>
            {
                new PaymentMethodEntity { Id = 1, Name = "Cash", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 3, Name = "Check", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 5, Name = "Credit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 7, Name = "Debit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 9, Name = "Wire Transfer", CreatedBy = 1, DateCreated = DateTime.UtcNow }
            };

            var expectedEntity = entities.First(e => e.Id == methodId);
            var expectedModel = new PaymentMethodModel
            {
                Id = methodId,
                Name = "Credit Card"
            };

            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(expectedEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodById_WithNonSequentialIds_ReturnsCorrectMethod()
        {
            // Arrange
            var methodId = 100;
            var entities = new List<PaymentMethodEntity>
            {
                new PaymentMethodEntity { Id = 10, Name = "Cash", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 50, Name = "Check", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 100, Name = "Credit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 250, Name = "Debit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow }
            };

            var expectedEntity = entities.First(e => e.Id == methodId);
            var expectedModel = new PaymentMethodModel
            {
                Id = methodId,
                Name = "Credit Card"
            };

            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(expectedEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodById_WhenCalledMultipleTimes_QueriesRepositoryEachTime()
        {
            // Arrange
            var methodId = 1;
            var paymentMethodEntity = new PaymentMethodEntity
            {
                Id = methodId,
                Name = "Credit Card",
                CreatedBy = 1,
                DateCreated = DateTime.UtcNow
            };

            var expectedModel = new PaymentMethodModel
            {
                Id = methodId,
                Name = "Credit Card"
            };

            var entities = new List<PaymentMethodEntity> { paymentMethodEntity };
            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(paymentMethodEntity))
                       .Returns(expectedModel);

            // Act
            var result1 = await _service.GetPaymentMethodById(methodId);
            var result2 = await _service.GetPaymentMethodById(methodId);
            var result3 = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
            _mockRepository.Verify(x => x.Query(), Times.Exactly(3));
            _mockMapper.Verify(x => x.Map<PaymentMethodModel>(paymentMethodEntity), Times.Exactly(3));
        }

        [Fact]
        public async Task GetPaymentMethodById_WhenSearchingForFirstId_ReturnsFirstRecord()
        {
            // Arrange
            var methodId = 1;
            var entities = new List<PaymentMethodEntity>
            {
                new PaymentMethodEntity { Id = 1, Name = "Cash", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 2, Name = "Check", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 3, Name = "Credit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow }
            };

            var expectedEntity = entities.First(e => e.Id == methodId);
            var expectedModel = new PaymentMethodModel
            {
                Id = methodId,
                Name = "Cash"
            };

            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(expectedEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodById_WhenSearchingForLastId_ReturnsLastRecord()
        {
            // Arrange
            var methodId = 5;
            var entities = new List<PaymentMethodEntity>
            {
                new PaymentMethodEntity { Id = 1, Name = "Cash", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 2, Name = "Check", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 3, Name = "Credit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 4, Name = "Debit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 5, Name = "Wire Transfer", CreatedBy = 1, DateCreated = DateTime.UtcNow }
            };

            var expectedEntity = entities.First(e => e.Id == methodId);
            var expectedModel = new PaymentMethodModel
            {
                Id = methodId,
                Name = "Wire Transfer"
            };

            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(expectedEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodById_WithLargeDataset_ReturnsCorrectMethod()
        {
            // Arrange
            var methodId = 50;
            var entities = new List<PaymentMethodEntity>();
            for (int i = 1; i <= 100; i++)
            {
                entities.Add(new PaymentMethodEntity
                {
                    Id = i,
                    Name = $"Payment Method {i}",
                    CreatedBy = 1,
                    DateCreated = DateTime.UtcNow
                });
            }

            var expectedEntity = entities.First(e => e.Id == methodId);
            var expectedModel = new PaymentMethodModel
            {
                Id = methodId,
                Name = $"Payment Method {methodId}"
            };

            var mockQueryable = entities.AsQueryable().BuildMock();

            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(expectedEntity))
                       .Returns(expectedModel);

            // Act
            var result = await _service.GetPaymentMethodById(methodId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedModel.Id, result.Id);
            Assert.Equal(expectedModel.Name, result.Name);
        }

        [Fact]
        public async Task GetPaymentMethodById_WhenDifferentIdsSearched_ReturnsCorrectMethodsIndependently()
        {
            // Arrange
            var entities = new List<PaymentMethodEntity>
            {
                new PaymentMethodEntity { Id = 1, Name = "Cash", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 2, Name = "Check", CreatedBy = 1, DateCreated = DateTime.UtcNow },
                new PaymentMethodEntity { Id = 3, Name = "Credit Card", CreatedBy = 1, DateCreated = DateTime.UtcNow }
            };

            var mockQueryable = entities.AsQueryable().BuildMock();
            _mockRepository.Setup(x => x.Query()).Returns(mockQueryable);

            var model1 = new PaymentMethodModel { Id = 1, Name = "Cash" };
            var model2 = new PaymentMethodModel { Id = 2, Name = "Check" };
            var model3 = new PaymentMethodModel { Id = 3, Name = "Credit Card" };

            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(entities[0])).Returns(model1);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(entities[1])).Returns(model2);
            _mockMapper.Setup(x => x.Map<PaymentMethodModel>(entities[2])).Returns(model3);

            // Act
            var result1 = await _service.GetPaymentMethodById(1);
            var result2 = await _service.GetPaymentMethodById(2);
            var result3 = await _service.GetPaymentMethodById(3);

            // Assert
            Assert.NotNull(result1);
            Assert.Equal(1, result1.Id);
            Assert.Equal("Cash", result1.Name);

            Assert.NotNull(result2);
            Assert.Equal(2, result2.Id);
            Assert.Equal("Check", result2.Name);

            Assert.NotNull(result3);
            Assert.Equal(3, result3.Id);
            Assert.Equal("Credit Card", result3.Name);
        }
    }
}
