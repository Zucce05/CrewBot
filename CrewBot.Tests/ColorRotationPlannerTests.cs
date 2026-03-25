using CrewBot.Classes;

namespace CrewBot.Tests;

public class ColorRotationPlannerTests
{
    [Fact]
    public void GetAssignedColorRoleIds_WhenMemberHasExistingColorRoles_ReturnsMatches()
    {
        IReadOnlyList<ulong> actual = ColorRotationPlanner.GetAssignedColorRoleIds(
            new[] { 1ul, 2ul, 3ul, 4ul },
            new[] { 2ul, 4ul, 6ul });

        Assert.Equal(new[] { 2ul, 4ul }, actual);
    }

    [Fact]
    public void GetAssignedColorRoleIds_WhenMemberHasNoExistingColorRoles_ReturnsEmpty()
    {
        IReadOnlyList<ulong> actual = ColorRotationPlanner.GetAssignedColorRoleIds(
            new[] { 10ul, 11ul },
            new[] { 2ul, 4ul, 6ul });

        Assert.Empty(actual);
    }
}
