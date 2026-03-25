using CrewBot.Classes.FactoryClasses;

namespace CrewBot.Tests;

public class SlashCommandFactoryTests
{
    [Theory]
    [InlineData("+color list")]
    [InlineData("+trigger add ~hello ~world")]
    [InlineData("+settings status")]
    [InlineData("+setlogchannel")]
    public void IsMigratedPrefixCommand_WhenKnownConfigCommand_ReturnsTrue(string content)
    {
        Assert.True(SlashCommandFactory.IsMigratedPrefixCommand(content, "+"));
    }

    [Theory]
    [InlineData("hello there")]
    [InlineData("+unknown command")]
    [InlineData("evetime")]
    public void IsMigratedPrefixCommand_WhenNotMigratedConfigCommand_ReturnsFalse(string content)
    {
        Assert.False(SlashCommandFactory.IsMigratedPrefixCommand(content, "+"));
    }

    [Fact]
    public void BuildGuildCommands_ReturnsExpectedTopLevelCommands()
    {
        IReadOnlyCollection<Discord.ApplicationCommandProperties> commands = SlashCommandFactory.BuildGuildCommands();

        Assert.Equal(4, commands.Count);
    }
}
