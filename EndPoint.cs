using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        // private static int FrameLengthSize = 4;
        // private static int FrameTypeSize = 4;
        // private static int HeaderSize = FrameLengthSize + FrameTypeSize;
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
            return Task.WhenAll(
                Listen(),
                SyncMaster()
            );
        }

        private async Task SyncMaster()
        {
            await ConnectTo(this.master);
        }

        public async Task ConnectTo(IPEndPoint target)
        {
            var client = new TcpClient(AddressFamily.InterNetwork);

            await client.ConnectAsync(target.Address, target.Port);

            var stream = client.GetStream();

            var _ = PipeStream(stream);

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

                Frame frame;
                while (TryReadFrame(ref buffer, out frame))
                {
                    ProcessFrame(frame);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            reader.Complete();
        }

        private bool TryReadFrame(ref ReadOnlySequence<byte> buffer, out Frame frame)
        {
            if (!TryReadHeader(ref buffer, out NameValueCollection header))
            {
                frame = null;
                return false;
            }

            if (!long.TryParse(header["Content-Length"], out long contentLength))
            {
                throw new Exception("Frame parse error, Invalid Content-Length value");
            }

            if (!TryReadBody(ref buffer, contentLength, out byte[] body))
            {
                frame = null;
                return false;
            }

            frame = new Frame();
            frame.ContentLength = contentLength;
            frame.Body = body;

            return true;
        }

        private bool TryReadHeader(ref ReadOnlySequence<byte> buffer, out NameValueCollection header)
        {
            SequencePosition? pos = buffer.PositionOf(DoubleCRLF);
            if (pos == null)
            {
                header = null;
                return false;
            }

            var rawHeader = Encoding.UTF8.GetString(buffer.Slice(buffer.Start, pos.Value));

            Console.WriteLine(rawHeader);
            header = null;

            buffer = buffer.Slice(buffer.GetPosition(FrameHeaderContants.NewLine.Length, pos.Value));

            return true;
        }

        private bool TryReadBody(ref ReadOnlySequence<byte> buffer, long length, out byte[] body)
        {
            if (buffer.Length < length)
            {
                body = null;
                return false;
            }

            body = buffer.Slice(0, length).ToArray();

            buffer = buffer.Slice(length);

            return true;
        }

        private async Task SendFrame(Stream stream, Frame frame)
        {
            /*
                Command: LIST
                Content-Length: 12345678

                XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
            */
            int totalSize = (int)(FrameHeaderContants.ContentLength.Length + FrameHeaderContants.Delimeter.Length + frame.ContentLength.ToString().Length +
                            FrameHeaderContants.Type.Length + FrameHeaderContants.Delimeter.Length + frame.Type.Length +
                            3 * FrameHeaderContants.NewLine.Length +
                            frame.ContentLength);

            var memory = MemoryPool<byte>.Shared.Rent(totalSize);
            await stream.WriteAsync(memory.Memory.Slice(0, totalSize));
        }

        private void ProcessFrame(Frame frame)
        {
            // buffer.
            // var command = Encoding.UTF8.GetString(buffer.ToArray());

            // switch (command)
            // {
            //     case "LIST":
            //         {
            //             stream.WriteAsync();
            //         }
            //     default:

            // }
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