namespace NotificationService.Notifications;

public record NewNotification(string From, string To, string Text, TranslationType TranslationType);

public enum TranslationType : byte
{
    None,
    Shakespeare,
    Yoda,
}
