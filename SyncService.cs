using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Syncthing
{
    public class SyncService : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new System.NotImplementedException();
        }
    }
}