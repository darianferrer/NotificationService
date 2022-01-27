using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NotificationService.IntegrationTests.Mocks;
using NotificationService.IntegrationTests.Server;
using NotificationService.Notifications;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace NotificationService.IntegrationTests.Features.Notifications;

[Collection(nameof(CustomTestApplicationFactory))]
public class NotificationApiTests : IAsyncLifetime
{
    private readonly CustomTestApplicationFactory _applicationFactory;
    private readonly DatabaseWrapper _databaseWrapper;
    private readonly Fixture _fixture = new();
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private readonly string _scenario = Guid.NewGuid().ToString();

    public NotificationApiTests(CustomTestApplicationFactory applicationFactory)
    {
        _applicationFactory = applicationFactory;
        _databaseWrapper = new DatabaseWrapper(_applicationFactory);
    }

    [Fact]
    public async Task GivenNewNotification_WhenItIsSubmitted_ThenItIsStored()
    {
        // Arrange
        var client = _applicationFactory.CreateClient();
        var contract = new
        {
            From = "Me",
            To = "You",
            Text = "Testing, 1, 2, 3 probando",
            TranslationType = "None",
        };
        var content = GetContent(contract);

        // Act
        var result = await client.PostAsync("/notifications", content);

        // Assert  
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseStream = await result.Content.ReadAsStreamAsync();
        var response = await JsonSerializer.DeserializeAsync<Notification>(
            responseStream,
            _options);
        response.Should().BeEquivalentTo(new
        {
            contract.From,
            contract.To,
            contract.Text,
        });
    }

    [Fact]
    public async Task GivenNewNotificationWithYodaTranslation_WhenItIsSubmitted_ThenItIsTranslatedAndStored()
    {
        // Arrange
        var client = _applicationFactory.CreateClient();
        var contract = new
        {
            From = "Me",
            To = "You",
            Text = "Master Obiwan has lost a planet.",
            TranslationType = "Yoda",
        };
        var content = GetContent(contract);

        var funTranslationContent = await File
                .ReadAllTextAsync(MockDataPaths.FunTranslations.MasterObiwanYoda);
        _applicationFactory.FunTranslationsServer
            .Given(
                Request.Create()
                    .UsingPost()
                    .WithPath("/translate/yoda"))
            .InScenario(_scenario)
            .WillSetStateTo(_scenario)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(StatusCodes.Status200OK)
                    .WithBody(funTranslationContent, "json"));

        // Act
        var result = await client.PostAsync("/notifications", content);

        // Assert  
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseStream = await result.Content.ReadAsStreamAsync();
        var response = await JsonSerializer.DeserializeAsync<Notification>(
            responseStream,
            _options);
        response.Should().BeEquivalentTo(new
        {
            contract.From,
            contract.To,
            Text = "Lost a planet, master obiwan has.",
        });
    }

    [Fact]
    public async Task GivenNewNotificationWithShakespeareTranslation_WhenItIsSubmitted_ThenItIsTranslatedAndStored()
    {
        // Arrange
        var client = _applicationFactory.CreateClient();
        var contract = new
        {
            From = "Me",
            To = "You",
            Text = "Master Obiwan has lost a planet.",
            TranslationType = "Shakespeare",
        };
        var content = GetContent(contract);

        var funTranslationContent = await File.ReadAllTextAsync(MockDataPaths.FunTranslations.MasterObiwanShakespeare);
        _applicationFactory.FunTranslationsServer
            .Given(
                Request.Create()
                    .UsingPost()
                    .WithPath("/translate/shakespeare"))
            .InScenario(_scenario)
            .WillSetStateTo(_scenario)
            .RespondWith(
                Response.Create()
                    .WithStatusCode(StatusCodes.Status200OK)
                    .WithBody(funTranslationContent, "json"));

        // Act
        var result = await client.PostAsync("/notifications", content);

        // Assert  
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseStream = await result.Content.ReadAsStreamAsync();
        var response = await JsonSerializer.DeserializeAsync<Notification>(
            responseStream,
            _options);
        response.Should().BeEquivalentTo(new
        {
            contract.From,
            contract.To,
            Text = "Master obiwan hath did lose a planet.",
        });
    }

    [Theory]
    [InlineData(null, "To", "Testing, 1, 2, 3 probando", "None")]
    [InlineData("", "To", "Testing, 1, 2, 3 probando", "None")]
    [InlineData("Me", null, "Testing, 1, 2, 3 probando", "None")]
    [InlineData("Me", "", "Testing, 1, 2, 3 probando", "None")]
    [InlineData("Me", "To", null, "None")]
    [InlineData("Me", "To", "", "None")]
    public async Task GivenInvalidNewNotification_WhenItIsSubmitted_ThenBadRequestIsReturned(
        string from,
        string to,
        string text,
        string translationType)
    {
        // Arrange
        var client = _applicationFactory.CreateClient();
        var contract = new
        {
            From = from,
            To = to,
            Text = text,
            TranslationType = translationType,
        };
        var content = GetContent(contract);

        // Act
        var result = await client.PostAsync("/notifications", content);

        // Assert  
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenNotificationsInDatabase_WhenAllNotificationsAreRequested_ThenAllOfThemAreReturned()
    {
        // Arrange
        var client = _applicationFactory.CreateClient();
        var expectedCount = 8;
        var notifications = _fixture.CreateMany<Notification>(expectedCount).ToList();
        foreach (var notification in notifications)
        {
            await _databaseWrapper.AddNotificationAsync(notification);
        }

        // Act
        var result = await client.GetAsync("/notifications");

        // Assert 
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseStream = await result.Content.ReadAsStreamAsync();
        var response = await JsonSerializer.DeserializeAsync<IEnumerable<Notification>>(responseStream, _options);
        response.Should().BeEquivalentTo(notifications);
    }

    [Fact]
    public async Task GivenExistingNotification_WhenItIsDeleted_ThenItIsGoneFromDatabase()
    {
        // Arrange
        var client = _applicationFactory.CreateClient();
        var notification = _fixture.Create<Notification>();
        await _databaseWrapper.AddNotificationAsync(notification);

        // Act
        var result = await client.DeleteAsync($"/notifications/{notification.Id}");

        // Assert  
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var expected = await _databaseWrapper.GetByIdAsync(notification.Id);
        expected.Should().BeNull();
    }

    [Fact]
    public async Task GivenNoMatchingNotificationId_WhenItIsDeleted_ThenNotFoundIsReturned()
    {
        // Arrange
        var client = _applicationFactory.CreateClient();

        // Act
        var result = await client.DeleteAsync($"/notifications/{Guid.NewGuid()}");

        // Assert  
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenExistingNotification_WhenItIsRequestedById_ThenItIsReturned()
    {
        // Arrange
        var client = _applicationFactory.CreateClient();
        var notification = _fixture.Create<Notification>();
        await _databaseWrapper.AddNotificationAsync(notification);

        // Act
        var result = await client.GetAsync($"/notifications/{notification.Id}");

        // Assert  
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseStream = await result.Content.ReadAsStreamAsync();
        var response = await JsonSerializer.DeserializeAsync<Notification>(
            responseStream,
            _options);
        response.Should().BeEquivalentTo(new
        {
            notification.From,
            notification.To,
            notification.Text,
        });
    }

    [Fact]
    public async Task GivenNoMatchingNotificationId_WhenItIsRequestedById_ThenNotFoundIsReturned()
    {
        // Arrange
        var client = _applicationFactory.CreateClient();

        // Act
        var result = await client.GetAsync($"/notifications/{Guid.NewGuid()}");

        // Assert  
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static StringContent GetContent<T>(T contract) =>
        new(JsonSerializer.Serialize(contract, _options), Encoding.UTF8, MediaTypeNames.Application.Json);

    public async Task InitializeAsync() =>
            await _databaseWrapper.ClearDatabaseAsync();

    public async Task DisposeAsync() =>
            await _databaseWrapper.DisposeAsync();
}
