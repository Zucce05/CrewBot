using CrewBot.Classes.FactoryClasses;

namespace CrewBot.Tests;

public class MessageCommandParserTests
{
    [Fact]
    public void NormalizePrefix_WhenNullOrWhitespace_ReturnsPlus()
    {
        Assert.Equal("+", MessageCommandParser.NormalizePrefix(null));
        Assert.Equal("+", MessageCommandParser.NormalizePrefix(""));
        Assert.Equal("+", MessageCommandParser.NormalizePrefix("   "));
    }

    [Fact]
    public void GetBlocks_SplitsAndNormalizesToLowercase()
    {
        string[] blocks = MessageCommandParser.GetBlocks("  +TrIgGeR   Add   ~Test ~Response  ");

        Assert.Equal(new[] { "+trigger", "add", "~test", "~response" }, blocks);
    }

    [Fact]
    public void IsPrefixed_IsCaseInsensitive()
    {
        Assert.True(MessageCommandParser.IsPrefixed("+Color list", "+"));
        Assert.True(MessageCommandParser.IsPrefixed("!HELP", "!"));
        Assert.False(MessageCommandParser.IsPrefixed("color list", "+"));
    }

    [Theory]
    [InlineData("+trigger add ~a ~b", "+", "trigger")]
    [InlineData("!settings help", "!", "settings")]
    [InlineData("color list", "+", "color")]
    [InlineData("", "+", "")]
    public void ExtractCommand_HandlesPrefixAndEmptyCases(string content, string prefix, string expected)
    {
        string command = MessageCommandParser.ExtractCommand(content, prefix);
        Assert.Equal(expected, command);
    }
}