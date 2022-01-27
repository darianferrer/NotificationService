using Xunit;

namespace NotificationService.IntegrationTests.Server;

[CollectionDefinition(nameof(CustomTestApplicationFactory))]
public class TestApplicationFactoryCollection
    : ICollectionFixture<CustomTestApplicationFactory>
{
}