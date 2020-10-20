namespace Syncthing
{
    using System;
    using System.Buffers;
    using System.Buffers.Binary;
    using System.IO;
    using System.Threading.Tasks;

    public sealed partial class EndPoint
    {
        private async Task SendFrame(Stream stream, Frame frame)
        {
            Console.WriteLine("Send Frame");

            /*
                Content-Length: 12345678
                Command: LIST

                XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
            */
            int totalSize = (int)frame.CalculateSize();
            Console.WriteLine($"Total Size: {totalSize}");

            var memoryOwner = MemoryPool<byte>.Shared.Rent(totalSize);

            // need slice here, since Rent memory will give us extra bigger than totalSize
            var buffer = memoryOwner.Memory.Slice(0, totalSize);

            frame.Fill(buffer);

            await stream.WriteAsync(buffer);
        }
    }
}