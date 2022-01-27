using System.ComponentModel.DataAnnotations;

namespace NotificationService.Settings;

public class AppSettings
{
    public const string Position = nameof(AppSettings);

    [Required]
    public string ConnectionString { get; set; }

    [Required]
    public Uri FunTranslations { get; set; }
}
