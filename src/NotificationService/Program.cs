using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http.Json;
using NotificationService;
using NotificationService.FunTranslations;
using NotificationService.Infrastructure;
using NotificationService.Notifications;
using NotificationService.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<AppSettings>()
    .BindConfiguration(AppSettings.Position)
    .ValidateDataAnnotations();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SchemaFilter<EnumSchemaFilter>());
builder.Services.AddFluentValidation(fv
    => fv.RegisterValidatorsFromAssemblyContaining<NewNotificationValidator>());

builder.Services.AddSingleton<NotificationStore>();
builder.Services.AddSingleton<SqlServerConnectionFactory>();

builder.Services.AddHttpClient<FunTranslationServiceClient>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/notifications", async (
    NotificationStore store,
    CancellationToken token) =>
{
    var notifications = await store.GetAsync(token);
    return Results.Ok(notifications);
})
.Produces<IEnumerable<Notification>>();

app.MapGet("/notifications/{id}", async (
    Guid id,
    NotificationStore store,
    CancellationToken token) =>
{
    var notification = await store.GetByIdAsync(id, token);
    return notification switch
    {
        null => Results.NotFound(),
        _ => Results.Ok(notification),
    };
})
.WithName("notificationById")
.ProducesProblem(StatusCodes.Status404NotFound)
.Produces<Notification>();

app.MapPost("/notifications", async (
    NewNotification newNotification,
    NotificationStore store,
    FunTranslationServiceClient client,
    IValidator<NewNotification> validator,
    CancellationToken token) =>
{
    var validation = validator.Validate(newNotification);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.GetValidationProblems());
    }
    var (from, to, text, translationType) = newNotification;

    var translatedText = translationType switch
    {
        TranslationType.None => text,
        TranslationType.Yoda => await client.YodaTranslateAsync(text, token),
        TranslationType.Shakespeare => await client.ShakespeareTranslateAsync(text, token),
        _ => null,
    };
    if (translatedText is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            {  nameof(NewNotification.TranslationType), new[]{ "Unsuported translation type" } }
        });
    }

    var notification = new Notification
    {
        Id = Guid.NewGuid(),
        From = from,
        To = to,
        Text = translatedText,
    };

    await store.CreateAsync(notification, token);

    return Results.CreatedAtRoute("notificationById",
        new { id = notification.Id },
        notification);
})
.ProducesValidationProblem()
.Produces<Notification>();

app.MapDelete("/notifications/{id}", async (
    Guid id,
    NotificationStore store,
    CancellationToken token) =>
{
    var notification = await store.GetByIdAsync(id, token);
    if (notification is null)
    {
        return Results.NotFound();
    }

    await store.DeleteAsync(notification, token);
    return Results.NoContent();
})
.ProducesProblem(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status204NoContent);

app.Run();

public partial class Program { }
