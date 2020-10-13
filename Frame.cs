namespace Syncthing
{
    public class Frame
    {
        public string Type;
        public long ContentLength;
        public byte[] Body;
    }

    public static class FrameHeaderContants
    {
        public const string ContentLength = "Content-Length";
        public const string Type = "Type";
        public const string Delimeter = ": ";
        public const string NewLine = "\r\n";
    }
}