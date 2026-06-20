namespace KaraokeList.Api.IntegrationTests;

public sealed class IntegrationTestInfrastructureTests
{
    [Fact]
    public void Database_is_available_when_integration_tests_are_required()
    {
        if (!IntegrationTestConnection.IntegrationTestsRequired)
        {
            return;
        }

        var connectionString = IntegrationTestConnection.Resolve();
        Assert.True(
            IntegrationTestConnection.CanConnect(connectionString),
            IntegrationTestConnection.SkipReason);
    }
}
