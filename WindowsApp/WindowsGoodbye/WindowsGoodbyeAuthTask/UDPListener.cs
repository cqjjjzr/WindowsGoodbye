using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace WindowsGoodbyeAuthTask
{
    internal static class UDPListener
    {
        public const int ReadBufferSize = 1024;
        public const int DeviceAuthPort = 26819;
        public static DatagramSocket DatagramSocket;
        public static readonly HostName DeviceMulticastGroupAddress = new HostName("225.67.76.67");

        public static async void StartListening()
        {
            if (DatagramSocket != null)
                await DatagramSocket.CancelIOAsync();
            DatagramSocket = new DatagramSocket();
            DatagramSocket.MessageReceived += async (sender, args) =>
            {
                await OnReceived(args.GetDataStream(), args.RemoteAddress);
            };
            await DatagramSocket.BindServiceNameAsync(DeviceAuthPort.ToString());
            DatagramSocket.JoinMulticastGroup(DeviceMulticastGroupAddress);
        }

        public static async void Send(string hostname, byte[] data)
        {
            Send(new HostName(hostname), data);
        }

        public static async void Send(HostName hostname, byte[] data)
        {
            using (var stream = (await DatagramSocket.GetOutputStreamAsync(
                hostname,
                DeviceAuthPort.ToString())).AsStreamForWrite())
            {
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
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
            if (info.StartsWith(WindowsGoodbyeAuthTask.DeviceAlivePrefix) &&
                info.Length > WindowsGoodbyeAuthTask.DeviceAlivePrefix.Length)
            {
                var payload = Convert.FromBase64String(info.Substring(WindowsGoodbyeAuthTask.DeviceAlivePrefix.Length));
                if (payload.Length != 16) return;
                var guid = new Guid(payload);
                var session = WindowsGoodbyeAuthTask.deviceSessions.FirstOrDefault(s => s.DeviceInDb.DeviceId == guid);
                if (session != null) session.Status = DeviceStatus.Established;
            } else if (info.StartsWith(WindowsGoodbyeAuthTask.AuthResponsePrefix) &&
                       info.Length > WindowsGoodbyeAuthTask.AuthResponsePrefix.Length)
            {
                var payload = Convert.FromBase64String(info.Substring(WindowsGoodbyeAuthTask.DeviceAlivePrefix.Length));
                if (payload.Length <= 18) return;
            }
        }
    }
}
