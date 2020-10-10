using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Syncthing
{
    public class EndPoint
    {
        private IPEndPoint master;

        private bool isMaster;

        private List<IPEndPoint> connections;

        private ILogger<EndPoint> logger;

        public EndPoint(IPEndPoint master, ILogger<EndPoint> logger)
        {
            this.logger = logger;
            this.master = master;
            this.isMaster = IPEndPoint.Equals(NetHelper.GetLocalEndPoint(), master);
            this.connections = new List<IPEndPoint>();
        }

        public Task Run()
        {
            return Task.WhenAll(
                Listen(),
                ConnectTo(this.master)
            );
        }

        public async Task ConnectTo(IPEndPoint target)
        {
            var client = new TcpClient(AddressFamily.InterNetwork);

            await client.ConnectAsync(target.Address, target.Port);

            var stream = client.GetStream();

#pragma warning disable CS4014
            PipeStream(stream);
#pragma warning restore CS4014

            // connections.Add(target, (pipe, stream));

            await stream.WriteAsync(Commands.LIST);
        }

        private Task PipeStream(NetworkStream stream)
        {
            var pipe = new Pipe();

            Task writing = WritePipeAsync(stream, pipe.Writer);
            Task reading = ReadPipeAsync(stream, pipe.Reader);

            return Task.WhenAll(writing, reading);
        }

        private async Task WritePipeAsync(Stream stream, PipeWriter writer)
        {
            const int miniBufferSize = 512;

            while (true)
            {
                var memory = writer.GetMemory(miniBufferSize);

                try
                {
                    int bytesRead = await stream.ReadAsync(memory);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    writer.Advance(bytesRead);
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, "Write Pipe error");
                    break;
                }

                FlushResult flushResult = await writer.FlushAsync();

                if (flushResult.IsCompleted)
                {
                    break;
                }
            }

            writer.Complete();
        }

        private async Task ReadPipeAsync(Stream stream, PipeReader reader)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync();

                var buffer = result.Buffer;

                SequencePosition? pos = null;

                do
                {
                    pos = buffer.PositionOf((byte)'\r');

                    if (pos != null)
                    {
                        ProcessCommand(buffer.Slice(0, pos.Value), stream);

                        buffer = buffer.Slice(buffer.GetPosition(2 /* skip \r\n */, pos.Value));
                    }
                }
                while (pos != null);

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            reader.Complete();
        }

        private void ProcessCommand(in ReadOnlySequence<byte> buffer, Stream stream)
        {
            buffer.
            var command = Encoding.UTF8.GetString(buffer.ToArray());

            switch (command)
            {
                case "LIST":
                    {
                        stream.WriteAsync();
                    }
                default:

            }
        }

        public async Task Listen()
        {
            var server = new TcpListener(IPAddress.Loopback, 5666);
            server.Start();

            while (true)
            {
                var client = server.AcceptTcpClient();
                var stream = client.GetStream();
                var pipe = PipeStream(stream);


            }
        }
    }
}