using Chronify.Teams;

namespace Chronify.Tests;

public class TeamRuleTests
{
    [Theory]
    [InlineData(TeamRole.NoRole, TeamRole.Executor, false)]
    [InlineData(TeamRole.Executor, TeamRole.Executor, false)]
    [InlineData(TeamRole.TeamLead, TeamRole.Executor, true)]
    [InlineData(TeamRole.TeamLead, TeamRole.TeamLead, false)]
    [InlineData(TeamRole.TeamLead, TeamRole.Administrator, false)]
    [InlineData(TeamRole.Administrator, TeamRole.Executor, true)]
    [InlineData(TeamRole.Administrator, TeamRole.TeamLead, true)]
    [InlineData(TeamRole.Administrator, TeamRole.NoRole, false)]
    [InlineData(TeamRole.Administrator, TeamRole.Administrator, false)]
    public void AssignmentsRespectRoleHierarchy(
        TeamRole sender,
        TeamRole receiver,
        bool allowed
    )
    {
        Assert.Equal(allowed, TeamRules.CanAssign(sender, receiver));
    }
}
