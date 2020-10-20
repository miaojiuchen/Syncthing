namespace Syncthing
{
    using System;
    using System.Text;

    public class Frame
    {
        public long ContentLength;
        public string Command;
        public byte[] Body;

        public Frame()
        {
            // Do nothing
        }

        public Frame(string command, byte[] body)
        {
            this.ContentLength = body?.Length ?? 0;
            this.Body = body;
            this.Command = command;
        }

        public long CalculateSize()
        {
            return (FrameHeaderContants.ContentLength.Length + FrameHeaderContants.Delimeter.Length + this.ContentLength.ToString().Length +
                    FrameHeaderContants.Command.Length + FrameHeaderContants.Delimeter.Length + this.Command.Length +
                    3 * FrameHeaderContants.NewLine.Length +
                    this.ContentLength);
        }

        public void Fill(Memory<byte> buffer)
        {
            var contentLengthHeaderLine = $"{FrameHeaderContants.ContentLength}{FrameHeaderContants.Delimeter}{this.ContentLength}{FrameHeaderContants.NewLine}";
            var commandHeaderLine = $"{FrameHeaderContants.Command}{FrameHeaderContants.Delimeter}{this.Command}{FrameHeaderContants.NewLine}";
            var fullHeader = $"{contentLengthHeaderLine}{commandHeaderLine}{FrameHeaderContants.NewLine}";
            Console.WriteLine($"Full Header: \n{fullHeader}");
            var headerSpan = Encoding.UTF8.GetBytes(fullHeader).AsSpan();
            headerSpan.CopyTo(buffer.Span);
            buffer = buffer.Slice(headerSpan.Length);
            Body?.CopyTo(buffer);
        }
    }

    public static class FrameHeaderContants
    {
        public const string ContentLength = "Content-Length";
        public const string Command = "Command";
        public const string Delimeter = ": ";
        public const string NewLine = "\r\n";
    }
}