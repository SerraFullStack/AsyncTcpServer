using Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static AsyncTcpServer server;
        static void Main(string[] args)
        {
            int port = 8181;

            if (args.Length > 0)
                port = Convert.ToInt32(args[0]);

            server = new AsyncTcpServer(IPAddress.Any, port);

            server.OnConnect((TcpClient client) =>
            {
                Console.WriteLine("New client connected");

            }).OnMessage((byte[] data) =>
            {
                return data;

            }).Start();


            Console.WriteLine("Server started at port {0}. Awaiting for connections.", port);

            String line = "";
            do
            {
                
                Console.WriteLine("Options Menu: ");
                Console.WriteLine("   0 -> Stop server.");
                Console.Write("Option: ");
                line = Console.ReadLine();
                if (line.Contains("0"))
                    server.Stop();
            } while (!line.Contains("0"));
        }
    }
}
