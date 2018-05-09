using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace WindowsGoodbye
{
    public static class MulticastListener
    {
        public const int ReadBufferSize = 1024;
        public const string PairingRequestPrefix = "wingb://pair_req?";
        public const string PairingTerminate = "wingb://pair_terminate";
        public const int DeviceMulticastPort = 26817;
        public static readonly HostName DeviceMulticastGroupAddress = new HostName("225.67.76.67");

        public static DatagramSocket DatagramSocket;

        public static async void StartListening()
        {
            if (DatagramSocket != null)
                await DatagramSocket.CancelIOAsync();
            DatagramSocket = new DatagramSocket();
            DatagramSocket.MessageReceived += async (sender, args) =>
            {
                await OnReceived(args.GetDataStream(), args.RemoteAddress);
            };

            DatagramSocket.Control.MulticastOnly = true;
            
            await DatagramSocket.BindServiceNameAsync(DeviceMulticastPort.ToString());
            DatagramSocket.JoinMulticastGroup(DeviceMulticastGroupAddress);
        }

        public static async void StopListening()
        {
            await DatagramSocket.CancelIOAsync();
            DatagramSocket.Dispose();
            DatagramSocket = null;
        }

        public static async Task OnReceived(IInputStream dataStream, HostName remoteAddress)
        {
            IBuffer buffer = new Windows.Storage.Streams.Buffer(ReadBufferSize);
            await dataStream.ReadAsync(buffer, ReadBufferSize, InputStreamOptions.None);
            dataStream.Dispose();

            var info = Encoding.UTF8.GetString(buffer.ToArray());
            if (info.StartsWith(PairingRequestPrefix) && info.Length > PairingRequestPrefix.Length)
            {
                var payload = info.Substring(PairingRequestPrefix.Length);
                UdpEventPublisher.FirePairingRequestReceived(payload, IPAddress.Parse(remoteAddress.CanonicalName));
            } else if (info.StartsWith(PairingTerminate))
            {
                UdpEventPublisher.FirePairingTerminateReceived(IPAddress.Parse(remoteAddress.CanonicalName));
            }
        }
    }

    public static class UnicastListener
    {
        public const int ReadBufferSize = 1024;
        public const int DeviceUnicastPort = 26818;
        public static DatagramSocket DatagramSocket;

        public static async void StartListening()
        {
            if (DatagramSocket != null)
                await DatagramSocket.CancelIOAsync();
            DatagramSocket = new DatagramSocket();
            DatagramSocket.MessageReceived += async (sender, args) =>
            {
                await OnReceived(args.GetDataStream(), args.RemoteAddress);
            };
            await DatagramSocket.BindServiceNameAsync(DeviceUnicastPort.ToString());
        }

        public static async void StopListening()
        {
            await DatagramSocket.CancelIOAsync();
            DatagramSocket.Dispose();
            DatagramSocket = null;
        }

        public static async Task OnReceived(IInputStream dataStream, HostName remoteAddress)
        {
            IBuffer buffer = new Windows.Storage.Streams.Buffer(ReadBufferSize);
            await dataStream.ReadAsync(buffer, ReadBufferSize, InputStreamOptions.None);
            dataStream.Dispose();

            var info = Encoding.UTF8.GetString(buffer.ToArray());
            // TODO process unicast
        }
    }

    public static class UdpEventPublisher
    {
        public delegate void UdpEventHandler(string payload, IPAddress fromAddress);

        public static event UdpEventHandler PairingRequestReceived = delegate { };
        public static event UdpEventHandler PairingTerminateReceived = delegate { };

        public static void FirePairingTerminateReceived(IPAddress fromAddress)
        {
            try
            {
                Volatile.Read(ref PairingTerminateReceived)(null, fromAddress);
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        public static void FirePairingRequestReceived(string payload, IPAddress fromAddress)
        {
            try
            {
                Volatile.Read(ref PairingRequestReceived)(payload, fromAddress);
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }
    }
}
