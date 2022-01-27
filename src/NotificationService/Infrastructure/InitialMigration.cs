using FluentMigrator;

namespace NotificationService.Infrastructure;

[Migration(1)]
public class InitialMigration : Migration
{
    public override void Up()
    {
        Create
            .Table("Notifications")
            .WithColumn("Id")
            .AsGuid()
            .PrimaryKey()
            .WithColumn("From")
            .AsString(100)
            .NotNullable()
            .WithColumn("To")
            .AsString(100)
            .NotNullable()
            .WithColumn("Text")
            .AsString(int.MaxValue)
            .NotNullable();
    }

    public override void Down() => Delete.Table("Notifications");
}
