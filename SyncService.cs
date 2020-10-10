using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Syncthing
{
    public class SyncService : BackgroundService
    {
        private IConfiguration configuration;

        private ILogger<SyncService> logger;

        private EndPointFactory endpointFactory;

        public SyncService(IConfiguration configuration, ILogger<SyncService> logger, EndPointFactory endpointFactory)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.endpointFactory = endpointFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var masterAddress = configuration["Syncthing:Master"];

            IPEndPoint masterEndpoint;
            try
            {
                masterEndpoint = IPEndPoint.Parse(masterAddress);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Configuration:Syncthing:Master parse error");
                throw e;
            }

            var node = this.endpointFactory.Create(masterEndpoint);

            return node.Run();
        }
    }
}