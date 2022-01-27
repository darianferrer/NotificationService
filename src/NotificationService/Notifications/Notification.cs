namespace NotificationService.Notifications;

internal record Notification
{
    public Guid Id { get; init; }
    public string From { get; init; }
    public string To { get; init; }
    public string Text { get; init; }
}
