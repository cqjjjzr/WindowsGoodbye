using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UDPTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var udpClient = new UdpClient(17500);
            udpClient.JoinMulticastGroup(IPAddress.Parse("225.67.76.67"));
            udpClient.MulticastLoopback = false;
            IPEndPoint p = new IPEndPoint(IPAddress.Parse("225.67.76.67"), 17500);
            while (true)
            {
                udpClient.Send(new byte[1], 1, p);
                var x = udpClient.ReceiveAsync();
                x.Wait();
                Console.WriteLine(x.Result.RemoteEndPoint);
                Console.ReadLine();
            }
        }
    }
}
