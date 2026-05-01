namespace ScrapWars.Notifications.Worker.Data.ReadModels;

public class CategoryNotificationChannelReadModel
{
    public Guid Id { get; set; }
    public ulong GuildId { get; set; }
    public Guid CategoryId { get; set; }
    public ulong ChannelId { get; set; }
}
