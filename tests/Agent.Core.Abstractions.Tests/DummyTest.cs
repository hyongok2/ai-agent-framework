namespace Agent.Core.Abstractions.Tests;

public class DummyTest
{
    [Fact]
    public void Should_Pass_Basic_Test()
    {
        // Arrange
        var expected = true;
        
        // Act
        var actual = true;
        
        // Assert
        actual.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 5, 10)]
    public void Should_Add_Numbers_Correctly(int a, int b, int expected)
    {
        // Act
        var result = a + b;
        
        // Assert
        result.Should().Be(expected);
    }
}