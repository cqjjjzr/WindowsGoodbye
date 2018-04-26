using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Security.Cryptography;
using GalaSoft.MvvmLight.Messaging;

namespace WindowsGoodbye
{
    public class DevicePairingContext
    {
        public const int DeviceUnicastPort = 26818;
        public readonly IPAddress DevicePairingMulticastGroupAddress = IPAddress.Parse("225.67.76.67");
        public const string PairingPrefix = "wingb://pair?";
        public const string PairingFinishPrefix = "wingb://pair_finish?";
        public const string PairingTerminatePrefix = "wingb://pair_terminate";

        public static DevicePairingContext ActiveDevicePairingContext;

        public readonly Guid DeviceId;
        public readonly byte[] DeviceKey;
        public readonly byte[] AuthKey;
        public readonly byte[] PairEncryptKey;
        public HostName LastConnectedHost;

        public DevicePairingContext() : this(CryptographicBuffer.GenerateRandom(32).ToArray(), CryptographicBuffer.GenerateRandom(32).ToArray()) {  }

        public DevicePairingContext(byte[] deviceKey, byte[] authKey)
        {
            DeviceKey = deviceKey;
            AuthKey = authKey;

            PairEncryptKey = CryptoTools.GenerateAESKey();
            DeviceId = Guid.NewGuid();
        }

        public string GeneratePairData()
        {
            var stream = new MemoryStream();
            stream.Write(DeviceId.ToByteArray());
            stream.Write(DeviceKey);
            stream.Write(AuthKey);
            stream.Write(PairEncryptKey);

            var payload = Convert.ToBase64String(stream.ToArray());
            return PairingPrefix + payload;
        }

        public void StartListening()
        {
            UdpEventPublisher.PairingRequestReceived += ProcessPairingRequest;
        }

        public void StopListening()
        {
            UdpEventPublisher.PairingRequestReceived -= ProcessPairingRequest;
        }

        public void ProcessPairingRequest(string pairPayload, IPAddress remoteIP)
        {
            LastConnectedHost = new HostName(remoteIP.ToString());
            var rawBytes = Convert.FromBase64String(pairPayload);
            if (rawBytes.Length <= Utils.GuidLength) return;
            var deviceIdBytes = new byte[Utils.GuidLength];
            rawBytes.CopyTo(deviceIdBytes, Utils.GuidLength);
            var deviceId = new Guid(deviceIdBytes);
            if (deviceId != DeviceId) return;

            // 注意由于已经检测到了设备，下方导致的任何错误都必须直接退出Pair过程并发送通知
            var encryptedLen = rawBytes.Length - Utils.GuidLength;
            var encryptedData = new byte[encryptedLen];

            rawBytes.CopyTo(encryptedData, encryptedLen);
            var decryptedData = CryptoTools.DecryptAES(encryptedData, PairEncryptKey);
            

            if (decryptedData.Length < 2) throw new PairingException(Utils.GetBackgroundI18n("Pairing.Exception.BadResponse"));
            int friendlyNameLen = encryptedData[0];
            int modelNameLen = encryptedData[1];
            if (decryptedData.Length != 2 + friendlyNameLen + modelNameLen) throw new PairingException(Utils.GetBackgroundI18n("Pairing.Exception.BadResponse"));

            var friendlyName = Encoding.UTF8.GetString(decryptedData, 2, friendlyNameLen);
            var modelName = Encoding.UTF8.GetString(decryptedData, 2 + friendlyNameLen, modelNameLen);

            Messenger.Default.Send(new PairDeviceDetectedMessage(friendlyName, modelName));

            var info = new DeviceInfo
            {
                AuthKey = AuthKey,
                DeviceFriendlyName = friendlyName,
                DeviceId = DeviceId,
                DeviceKey = DeviceKey,
                DeviceMacAddress = null,
                DeviceModelName = modelName,
                LastConnectedHost = LastConnectedHost.CanonicalName
            };

            // TODO: 调用我自己的代码开始交互操作系统
        }

        public async void FinishPairing(DeviceInfo deviceInfo, string computerInfo)
        {
            // 配对完成，通知设备
            // TODO: 这里是否需要设备返回，以保证没有发生配对之后手机断开造成电脑注册了手机却没有登记电脑信息的情况？
            var payload = Convert.ToBase64String(CryptoTools.EncryptAES(Encoding.UTF8.GetBytes(computerInfo), PairEncryptKey));
            var bytes = Encoding.UTF8.GetBytes(PairingFinishPrefix + payload);
            var stream = await UnicastListener.DatagramSocket.GetOutputStreamAsync(new HostName(deviceInfo.LastConnectedHost),
                UnicastListener.DeviceUnicastPort.ToString());
            Windows.Storage.Streams.Buffer buf = new Windows.Storage.Streams.Buffer((uint) bytes.Length);
            bytes.CopyTo(buf);
            await stream.WriteAsync(buf);
            stream.Dispose();
            
            var macs = new HashSet<string>();
            IPHelperUtils.GetMACFromIP(deviceInfo.LastConnectedHost.ToString(), macs);
            deviceInfo.DeviceMacAddress = macs.FirstOrDefault();
        }

        public async void TerminatePairing()
        {
            if (LastConnectedHost == null) return;
            var bytes = Encoding.UTF8.GetBytes(PairingTerminatePrefix);
            var stream = await UnicastListener.DatagramSocket.GetOutputStreamAsync(LastConnectedHost,
                UnicastListener.DeviceUnicastPort.ToString());
            Windows.Storage.Streams.Buffer buf = new Windows.Storage.Streams.Buffer((uint)bytes.Length);
            bytes.CopyTo(buf);
            await stream.WriteAsync(buf);
            stream.Dispose();
        }
    }

    public class PairDeviceDetectedMessage
    {

        public string DeviceFriendlyName, DeviceModelName;

        public PairDeviceDetectedMessage(string deviceFriendlyName, string deviceModelName)
        {
            DeviceFriendlyName = deviceFriendlyName;
            DeviceModelName = deviceModelName;
        }
    }

    public class DeviceAuthContext
    {
        public const int DeviceAuthMulticastPort = 26817;
        public readonly IPAddress DeviceAuthMulticastGroupAddress = IPAddress.Parse("225.67.76.67");
        public readonly IPEndPoint DeviceAuthMulticastEndpoint = new IPEndPoint(DeviceAuthMulticastPort, 26817);
        public const string CheckExistsPrefix = "wingb://auth_exists?";
        public const string CheckExistsResponsePrefix = "wingb://auth_exists_resp?";
        public const string AuthPrefix = "wingb://auth_req?";

        public readonly Guid DeviceId;
        public readonly byte[] DeviceKey;
        public readonly byte[] AuthKey;

        public DeviceAuthContext(Guid deviceId, byte[] deviceKey, byte[] authKey)
        {
            DeviceId = deviceId;
            DeviceKey = deviceKey;
            AuthKey = authKey;
        }

        public async Task WaitForDeviceAsync()
        {
            var udpClient = new UdpClient(DeviceAuthMulticastPort) { EnableBroadcast = false };
            udpClient.JoinMulticastGroup(DeviceAuthMulticastGroupAddress);

            var payload = Convert.ToBase64String(DeviceId.ToByteArray());
            var data = Encoding.UTF8.GetBytes(CheckExistsPrefix + payload);
            await udpClient.SendAsync(data, data.Length, DeviceAuthMulticastEndpoint);
            
            while (true)
            {
                var result = await udpClient.ReceiveAsync();
                var str = Encoding.UTF8.GetString(result.Buffer);
                if (str.Length <= CheckExistsResponsePrefix.Length || !str.StartsWith(CheckExistsResponsePrefix)) continue;

                payload = str.Substring(CheckExistsResponsePrefix.Length + 1);
                var deviceIdBytes = Convert.FromBase64String(payload);
                if (deviceIdBytes.Length != Utils.GuidLength) continue;

                Guid deviceId = new Guid(deviceIdBytes);
                if (deviceId == DeviceId) return;
            }
        }
    }

    class PairingException: Exception
    {
        public PairingException()
        {

        }

        public PairingException(string message): base(message)
        {
            
        }
    }
}
