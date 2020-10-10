using System.Net;
using Microsoft.Extensions.Logging;

namespace Syncthing
{
    public class EndPointFactory
    {
        private ILogger<EndPoint> itemLogger;

        public EndPointFactory(ILogger<EndPoint> itemLogger)
        {
            this.itemLogger = itemLogger;
        }

        public EndPoint Create(IPEndPoint target)
        {
            return new EndPoint(target, this.itemLogger);
        }
    }
}