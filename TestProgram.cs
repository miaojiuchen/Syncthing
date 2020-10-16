using System;
using System.Buffers;
using System.Collections.Specialized;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpSocketTest
{
    public class TestProgram
    {
        static int port = 10086;

        public static void Main2(string[] args)
        {
            var nvc = new NameValueCollection();
            nvc.Add("key", "value,");
            nvc.Add("key", "value2");
            Console.WriteLine(nvc["key"]);
            Console.WriteLine(nvc["key_not_exist"]);
            // var _ = RunServer();

            // Task.Delay(1000).GetAwaiter().GetResult();
            // Task.WaitAll(
            //     Enumerable
            //         .Range(0, 10)
            //         .Select(x => RunClients())
            //         .ToArray()
            // );
            // Task.Delay(-1).GetAwaiter().GetResult();
        }

        public static async Task RunServer()
        {
            var server = new TcpListener(IPAddress.Loopback, port);
            server.Start();
            var i = 0;
            while (true)
            {
                var clientId = i++;
                var client = await server.AcceptTcpClientAsync();

                Console.WriteLine("accept client{0}", clientId);

                var stream = client.GetStream();

                var pipe = new System.IO.Pipelines.Pipe();

                Task writing = FillPipeAsync(stream, pipe.Writer);
                Task reading = ReadPipeAsync(pipe.Reader, clientId);

                var _ = Task.WhenAll(writing, reading)
                        .ContinueWith(x =>
                        {
                            client.Dispose();
                            Console.WriteLine("disconnect client{0}", clientId);
                        });
            }
        }

        static async Task FillPipeAsync(Stream socket, PipeWriter writer)
        {
            const int minimumBufferSize = 512;

            while (true)
            {
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                try
                {
                    int bytesRead = await socket.ReadAsync(memory);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    writer.Advance(bytesRead);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }

                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            writer.Complete();
        }

        static async Task ReadPipeAsync(PipeReader reader, int clientId)
        {
            while (true)
            {
                var result = await reader.ReadAsync();

                var buffer = result.Buffer;

                var pos = buffer.PositionOf((byte)'\n');
                // buffer.TryGet(ref pos.Value, out ReadOnlyMemory<byte> memory);

                Console.WriteLine(clientId.ToString() + ":" + Encoding.UTF8.GetString(buffer.ToArray()));

                reader.AdvanceTo(buffer.End, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            reader.Complete();
        }

        static async Task RunClients()
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);

            var stream = client.GetStream();

            var tasks = new Task[100];
            for (var i = 0; i < 100; ++i)
            {
                var clientId = i;
                tasks[i] = Task.Run(async () =>
                {
                    var buffer = Encoding.UTF8.GetBytes("-" + clientId.ToString() + "-");
                    Console.WriteLine(clientId);
                    await stream.WriteAsync(buffer);
                });
            }

            await Task.WhenAll(tasks);
        }
    }
}
