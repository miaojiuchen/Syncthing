namespace Syncthing
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.IO.Pipelines;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public sealed partial class EndPoint
    {
        private async Task ProcessFrame(Frame frame, Stream stream)
        {
            switch (frame.Command)
            {
                case "LIST":
                    {
                        var message = new Frame("ECHO", Encoding.UTF8.GetBytes("Hello World"));
                        await SendFrame(stream, message);
                        break;
                    }
                case "ECHO":
                    {
                        Console.WriteLine(Encoding.UTF8.GetString(frame.Body));
                        break;
                    }
                case "FILE":
                    {
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
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
                    var _ = Task.Run(() => ProcessFrame(frame, stream));
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
            if (!TryReadHeader(ref buffer, out Dictionary<string, string> header))
            {
                frame = null;
                return false;
            }

            if (!long.TryParse(header[FrameHeaderContants.ContentLength], out long contentLength))
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
            frame.Command = header[FrameHeaderContants.Command];
            frame.Body = body;

            return true;
        }

        private bool TryReadHeader(ref ReadOnlySequence<byte> buffer, out Dictionary<string, string> header)
        {
            SequencePosition? pos = buffer.PositionOf(DoubleCRLF);
            if (pos == null)
            {
                header = null;
                return false;
            }

            var rawHeader = Encoding.UTF8.GetString(buffer.Slice(buffer.Start, pos.Value));

            header = ParseHeader(rawHeader);

            buffer = buffer.Slice(buffer.GetPosition(FrameHeaderContants.NewLine.Length, pos.Value));

            return true;
        }

        private Dictionary<string, string> ParseHeader(string rawHeader)
        {
            var result = new Dictionary<string, string>();

            var lines = rawHeader.Split(FrameHeaderContants.NewLine);
            foreach (var line in lines)
            {
                var parts = line.Split(FrameHeaderContants.Delimeter);
                if (parts.Length != 2)
                {
                    throw new Exception(string.Format("Invalid frame format in header: {0}", line));
                }
                var key = parts[0];
                var value = parts[1];

                if (!result.ContainsKey(key))
                {
                    result.Add(key, value);
                }
            }

            return result;
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
    }
}