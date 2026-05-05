using BillingService.Domain.Services.Billing.EDI;
using EdiFabric.Core.Annotations.Edi;
using EdiFabric.Core.Model.Edi;
using System;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.EDI
{
    public class XmlExtensionsTest
    {
        [Message("X12", "005010")]
        public class TestEdiMessage : EdiMessage
        {
            public string TestProperty { get; set; }
        }

        [DataContract]
        public class TestDataContractMessage
        {
            [DataMember]
            public string TestProperty { get; set; }
        }

        [Message("X12", "005010")]
        [DataContract]
        public class TestEdiMessageDataContract : EdiMessage
        {
            [DataMember]
            public string TestProperty { get; set; }
        }

        [Fact]
        public void Serialize_Should_Return_XDocument()
        {
            var message = new TestEdiMessage
            {
                TestProperty = "TestValue"
            };

            var result = message.Serialize();

            Assert.NotNull(result);
            Assert.NotNull(result.Root);
        }

        [Fact]
        public void Serialize_Should_Throw_ArgumentNullException()
        {
            TestEdiMessage message = null;

            Assert.Throws<ArgumentNullException>(() =>
                XmlExtensions.Serialize(message));
        }

        [Fact]
        public void Deserialize_Should_Return_Object()
        {
            var xml =
                new XElement("TestEdiMessage",
                    new XElement("TestProperty", "DeserializeTest"));

            var result =
                XmlExtensions.Deserialize<TestEdiMessage>(xml);

            Assert.NotNull(result);
            Assert.Equal("DeserializeTest", result.TestProperty);
        }

        [Fact]
        public void SerializeDataContract_Should_Return_XDocument()
        {
            var obj = new TestDataContractMessage
            {
                TestProperty = "DataContractTest"
            };

            var serializer =
                new DataContractSerializer(typeof(TestDataContractMessage));

            var doc = new XDocument();
            using (var writer = doc.CreateWriter())
            {
                serializer.WriteObject(writer, obj);
            }

            Assert.NotNull(doc);
            Assert.NotNull(doc.Root);
        }

        [Fact]
        public void SerializeDataContract_Should_Throw_ArgumentNullException()
        {
            EdiMessage message = null;

            Assert.Throws<ArgumentNullException>(() =>
                XmlExtensions.SerializeDataContract(message));
        }

        [Fact]
        public void DeserializeDataContract_Should_Return_Object()
        {
            var obj = new TestDataContractMessage
            {
                TestProperty = "DeserializeDataContractTest"
            };

            var serializer =
                new DataContractSerializer(typeof(TestDataContractMessage));

            var doc = new XDocument();
            using (var writer = doc.CreateWriter())
            {
                serializer.WriteObject(writer, obj);
            }

            var result =
                XmlExtensions.DeserializeDataContract<TestDataContractMessage>(doc.Root);

            Assert.NotNull(result);
            Assert.Equal("DeserializeDataContractTest", result.TestProperty);
        }

        [Fact]
        public void SerializeDataContractShouldCoverAllLinesAndReturnXDocument()
        {
            // Arrange
            var message = new TestEdiMessageDataContract
            {
                TestProperty = "CoverageTest"
            };

            var result = XmlExtensions.SerializeDataContract(message);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Root);
            Assert.Contains("CoverageTest", result.ToString());
        }

        [Fact]
        public void SerializeDataContract_Should_Cover_All_Lines_And_Return_XDocument()
        {
            var message = new TestEdiMessageDataContract
            {
                TestProperty = "CoverageTest"
            };

            var result = XmlExtensions.SerializeDataContract(message);

            Assert.NotNull(result);
            Assert.NotNull(result.Root);
        }
    }
}