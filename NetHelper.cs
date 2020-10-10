using System.Net;
using System.Net.Sockets;

namespace Syncthing
{
    public class NetHelper
    {
        public static IPEndPoint GetLocalEndPoint()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            return socket.LocalEndPoint as IPEndPoint;
        }
    }
}