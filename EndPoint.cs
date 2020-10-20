namespace Syncthing
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public sealed partial class EndPoint
    {
        private IPEndPoint master;
        private bool isMaster;
        private List<IPEndPoint> connections;
        private ILogger<EndPoint> logger;
        private static byte[] DoubleCRLF = (FrameHeaderContants.NewLine + FrameHeaderContants.NewLine).Select(x => (byte)x).ToArray();
        public EndPoint(IPEndPoint master, ILogger<EndPoint> logger)
        {
            this.logger = logger;
            this.master = master;
            this.isMaster = IPEndPoint.Equals(NetHelper.GetLocalEndPoint(), master);
            this.connections = new List<IPEndPoint>();
        }

        public Task Run()
        {
            var listenTask = Listen();
            var syncMasterTask = SyncMaster();
            return Task.WhenAll(listenTask, syncMasterTask);
        }

        private async Task SyncMaster()
        {
            Console.WriteLine("Connecting to Master...");
            await ConnectTo(this.master);
        }

        public async Task ConnectTo(IPEndPoint target)
        {
            var client = new TcpClient(AddressFamily.InterNetwork);
            await client.ConnectAsync(target.Address, target.Port);
            Console.WriteLine($"Connected to {target.Address}:{target.Port}");
            var stream = client.GetStream();

            var pipe = PipeStream(stream);
            Console.WriteLine($"Stream Piped");

            await SendFrame(stream, new Frame("LIST", null));
            Console.WriteLine($"Sent LIST command");
            await pipe;
        }

        public async Task Listen()
        {
            var server = new TcpListener(IPAddress.Loopback, 5666);
            server.Start();
            Console.WriteLine("Listener running...");
            while (true)
            {
                var client = await server.AcceptTcpClientAsync();
                Console.WriteLine("New Client Accepted");
                var stream = client.GetStream();
                await PipeStream(stream);
            }
        }
    }
}