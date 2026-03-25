using CrewBot.Classes;

namespace CrewBot.Tests;

public class VoidTimingParserTests
{
    [Theory]
    [InlineData("10s", 10)]
    [InlineData("10m", 600)]
    [InlineData("2h", 7200)]
    [InlineData("1d", 86400)]
    public void TryParseDuration_WhenValid_ReturnsSeconds(string raw, int expectedSeconds)
    {
        bool success = VoidTimingParser.TryParseDuration(raw, out int actualSeconds);

        Assert.True(success);
        Assert.Equal(expectedSeconds, actualSeconds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("10")]
    [InlineData("+10m")]
    public void TryParseDuration_WhenInvalid_ReturnsFalse(string raw)
    {
        bool success = VoidTimingParser.TryParseDuration(raw, out _);

        Assert.False(success);
    }

    [Fact]
    public void TryParsePrefixedDurationToken_WhenPrefixedOverridePresent_ReturnsSeconds()
    {
        bool success = VoidTimingParser.TryParsePrefixedDurationToken("+10m hello world", "+", out int actualSeconds);

        Assert.True(success);
        Assert.Equal(600, actualSeconds);
    }

    [Fact]
    public void TryParsePrefixedDurationToken_WhenFirstTokenIsNotDuration_ReturnsFalse()
    {
        bool success = VoidTimingParser.TryParsePrefixedDurationToken("+color list", "+", out _);

        Assert.False(success);
    }
}
