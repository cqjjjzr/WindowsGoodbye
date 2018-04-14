using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace WindowsGoodbye
{
    class DeviceCommunication
    {
        

        
    }

    class DevicePairingContext
    {
        public const int DevicePairingMulticastPort = 26817;
        public const int DevicePairingResultPort = 26818;
        public readonly IPAddress DevicePairingMulticastGroupAddress = IPAddress.Parse("225.67.76.67");
        public const string PairingPrefix = "wingb://pair?$";
        public const string PairingRequestPrefix = "wingb://pair_req?";
        public const string PairingFinishPrefix = "wingb://pair_finish?";

        public readonly Guid DeviceId;
        public readonly byte[] DeviceKey;
        public readonly byte[] AuthKey;

        private readonly UdpClient _udpClient;
        private Task<DevicePairingResult> _task;
        private CancellationTokenSource _cancellation;

        public DevicePairingContext(byte[] deviceKey, byte[] authKey)
        {
            DeviceKey = deviceKey;
            AuthKey = authKey;
            
            DeviceId = Guid.NewGuid();

            _udpClient = new UdpClient(DevicePairingMulticastPort)
            {
                EnableBroadcast = false
            };
        }

        public string GeneratePairData()
        {
            var stream = new MemoryStream();
            stream.Write(DeviceId.ToByteArray());
            stream.Write(DeviceKey);
            stream.Write(AuthKey);

            var payload = Convert.ToBase64String(stream.ToArray());
            return PairingPrefix + payload;
        }

        public Task<DevicePairingResult> StartPairingListeningAsync()
        {
            _udpClient.JoinMulticastGroup(DevicePairingMulticastGroupAddress);
            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;
            _task = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var receiveTask = _udpClient.ReceiveAsync();
                        receiveTask.Wait(_cancellation.Token);
                        token.ThrowIfCancellationRequested();
                        var result = receiveTask.Result;
                        
                        var info = Encoding.UTF8.GetString(result.Buffer);
                        if (!info.StartsWith(PairingRequestPrefix) || info.Length <= PairingRequestPrefix.Length)
                            continue;
                        var payload = info.Substring(PairingRequestPrefix.Length + 1);
                        var stream = new MemoryStream(Convert.FromBase64String(payload));
                            
                        if (stream.Length <= Utils.GuidLength) continue;
                            
                        var reader = new BinaryReader(stream, Encoding.UTF8);
                        var deviceIdBytes = reader.ReadBytes(Utils.GuidLength);
                        var deviceId = new Guid(deviceIdBytes);
                        if (deviceId != DeviceId) continue;

#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                        MainPage.Instance.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () => MainPage.Instance.PairDeviceDetected());
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法

                        var friendlyName = reader.ReadString();
                        var modelName =  reader.ReadString();

                        return new DevicePairingResult
                        {
                            DeviceFriendName = friendlyName,
                            DeviceModelName = modelName,
                            DeviceEndPoint = result.RemoteEndPoint
                        };
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e);
                    }
                }
            }, _cancellation.Token);
            return _task;
        }

        public void StopPairingListening()
        {
            _cancellation.Cancel();
        }

        public void FinishPairing(DevicePairingResult previousResult, string computerInfo)
        {
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(computerInfo));
            var bytes = Encoding.UTF8.GetBytes(PairingFinishPrefix + payload);
            new UdpClient(DevicePairingResultPort).SendAsync(bytes, bytes.Length, previousResult.DeviceEndPoint);
        }
    }

    class DevicePairingResult
    {
        public string DeviceFriendName, DeviceModelName;
        public IPEndPoint DeviceEndPoint;
    }
}
