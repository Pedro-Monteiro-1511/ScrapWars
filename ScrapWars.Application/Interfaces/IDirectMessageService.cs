namespace ScrapWars.Application.Interfaces;

public interface IDirectMessageService
{
    Task SendHelpMessageAsync(ulong userId, CancellationToken cancellationToken = default);
}
