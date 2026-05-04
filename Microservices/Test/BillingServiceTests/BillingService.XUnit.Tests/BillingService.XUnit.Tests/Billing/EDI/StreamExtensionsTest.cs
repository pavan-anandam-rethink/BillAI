using BillingService.Domain.Services.Billing.EDI;
using System.IO;
using System.Text;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.EDI
{
    public class StreamExtensionsTest
    {
        [Fact]
        public void LoadToString_Should_Return_Stream_Content()
        {
            // Arrange
            var expected = "Hello World";
            var bytes = Encoding.Default.GetBytes(expected);

            using var stream = new MemoryStream(bytes);

            // Act
            var result = stream.LoadToString();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void LoadToString_Should_Reset_Position_And_Read_From_Start()
        {
            // Arrange
            var expected = "Position Reset Test";
            var bytes = Encoding.Default.GetBytes(expected);

            using var stream = new MemoryStream(bytes);

            // Move position to end
            stream.Position = stream.Length;

            // Act
            var result = stream.LoadToString();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void LoadToString_Should_Return_Empty_String_For_Empty_Stream()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            var result = stream.LoadToString();

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}