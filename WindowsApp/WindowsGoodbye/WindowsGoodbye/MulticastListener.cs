using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;

namespace WindowsGoodbye
{
    public static class MulticastListener
    {
        public const string PairingRequestPrefix = "wingb://pair_req?";
        public const int DeviceMulticastPort = 26817;
        public static readonly IPAddress DeviceMulticastGroupAddress = IPAddress.Parse("225.67.76.67");

        private static readonly UdpClient _udpClient = new UdpClient(DeviceMulticastPort) { EnableBroadcast = false, MulticastLoopback = false };

        private static Task _listeningTask;
        private static CancellationTokenSource _cancellationTokenSource;

        private static void Run()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            _udpClient.JoinMulticastGroup(DeviceMulticastGroupAddress);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var receiveTask = _udpClient.ReceiveAsync();
                receiveTask.Wait(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (receiveTask.IsFaulted) continue;

                var result = receiveTask.Result;

                var info = Encoding.UTF8.GetString(result.Buffer);
                if (!info.StartsWith(PairingRequestPrefix) || info.Length <= PairingRequestPrefix.Length)
                    continue;
                var payload = info.Substring(PairingRequestPrefix.Length + 1);
                if (DevicePairingContext.ActivePairingContext != null)
                    DevicePairingContext.ActivePairingContext.ProcessPairingRequest(payload, result.RemoteEndPoint);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public static void StartListening()
        {
            StopListening();
            _cancellationTokenSource = new CancellationTokenSource();
            _listeningTask = Task.Run((Action) Run, _cancellationTokenSource.Token);
        }

        public static void StopListening()
        {
            if (_listeningTask != null && !_listeningTask.IsCanceled)
                _cancellationTokenSource.Cancel();

            _listeningTask = null;
            _cancellationTokenSource = null;
            _udpClient.DropMulticastGroup(DeviceMulticastGroupAddress);
        }
    }
}
