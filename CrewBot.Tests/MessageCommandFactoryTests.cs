using CrewBot.Classes.FactoryClasses;

namespace CrewBot.Tests;

public class MessageCommandFactoryTests
{
    [Theory]
    [InlineData(1ul, 1ul, true)]
    [InlineData(1ul, 2ul, false)]
    [InlineData(0ul, 0ul, true)]
    public void IsServerOwner_ReturnsExpectedResult(ulong authorId, ulong ownerId, bool expected)
    {
        bool actual = MessageCommandFactory.IsServerOwner(authorId, ownerId);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsServerOwner_WhenAdditionalOwnerMatches_ReturnsTrue()
    {
        bool actual = MessageCommandFactory.IsServerOwner(5ul, 1ul, 5ul);

        Assert.True(actual);
    }
}
