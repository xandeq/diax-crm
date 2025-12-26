namespace Diax.IntegrationTests;

public class SampleIntegrationTest
{
    [Fact]
    public void Sample_IntegrationTest_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var result = true;

        // Assert
        Assert.Equal(expected, result);
    }
}
