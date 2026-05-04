using EraParserService.Domain.Exceptions;

public class EraEdiExceptionTests
{
    [Fact]
    public void Constructor_Should_Set_Message_Correctly()
    {
        // Arrange
        var expectedMessage = "Test error message";

        // Act
        var exception = new EraEdiException(expectedMessage);

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Should_Be_Instance_Of_Exception()
    {
        // Arrange & Act
        var exception = new EraEdiException("error");

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void Constructor_Should_Set_Inner_Exception()
    {
        // Arrange
        var inner = new Exception("inner");

        // Act
        var exception = new EraEdiException("outer", inner);

        // Assert
        Assert.Equal(inner, exception.InnerException);
    }

}