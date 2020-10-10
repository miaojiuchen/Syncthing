using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Syncthing
{
    public static class Commands
    {
        public static byte[] LIST = Encoding.UTF8.GetBytes("LIST");

        // public static byte[] LIST = Encoding.UTF8.GetBytes("LIST");

        public static byte[][] AllCommands = typeof(Commands).GetFields(BindingFlags.Public | BindingFlags.Static).Select(x => (byte[])x.GetValue(null)).ToArray();

        public static Dictionary<string, Action<Stream>> CommandHandler = new Dictionary<string, Action<Stream>>()
        {
            {
                "LIST",
                (stream) =>
                {
                    
                }
            }
        };
    }
}