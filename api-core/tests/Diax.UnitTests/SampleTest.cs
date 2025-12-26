namespace Diax.UnitTests;

public class SampleTest
{
    [Fact]
    public void Sample_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var result = true;

        // Assert
        Assert.Equal(expected, result);
    }
}
