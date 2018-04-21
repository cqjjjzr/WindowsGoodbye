using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                try
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
                    UdpEventPublisher.FirePairingRequestReceived(payload, result.RemoteEndPoint);

                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Debug.Write(e);
                }
            }
            // ReSharper disable once FunctionNeverReturns
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

    public static class UnicastListener
    {
        public const int DeviceUnicastPort = 26818;

        public static readonly UdpClient DeviceUnicastClient = new UdpClient(DeviceUnicastPort);

        private static Task _listeningTask;
        private static CancellationTokenSource _cancellationTokenSource;

        private static void Run()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var receiveTask = DeviceUnicastClient.ReceiveAsync();
                    receiveTask.Wait(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (receiveTask.IsFaulted) continue;

                    var result = receiveTask.Result;

                    var info = Encoding.UTF8.GetString(result.Buffer);
                    // TODO unicast packet resolve
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Debug.Write(e);
                }
            }
        }

        public static void StartListening()
        {
            StopListening();
            _cancellationTokenSource = new CancellationTokenSource();
            _listeningTask = Task.Run((Action)Run, _cancellationTokenSource.Token);
        }

        public static void StopListening()
        {
            if (_listeningTask != null && !_listeningTask.IsCanceled)
                _cancellationTokenSource.Cancel();

            _listeningTask = null;
            _cancellationTokenSource = null;
        }


    }

    public static class UdpEventPublisher
    {
        public delegate void UdpEventHandler(string payload, IPEndPoint fromEndPoint);

        public static event UdpEventHandler PairingRequestReceived = delegate { };

        public static void FirePairingRequestReceived(string payload, IPEndPoint fromEndPoint)
        {
            Volatile.Read(ref PairingRequestReceived)(payload, fromEndPoint);
        }
    }
}
