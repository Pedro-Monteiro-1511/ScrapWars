using System.Threading;
using System.Threading.Tasks;

namespace ScrapWars.Application.Interfaces;

public interface IBotService
{
    Task StartAsync(string token, CancellationToken cancellationToken);
    Task StopAsync();
}
