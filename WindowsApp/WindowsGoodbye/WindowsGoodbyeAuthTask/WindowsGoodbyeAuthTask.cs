using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Security.Authentication.Identity.Provider;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using WindowsGoodbye;
using Buffer = Windows.Storage.Streams.Buffer;

namespace WindowsGoodbyeAuthTask
{
    public sealed class WindowsGoodbyeAuthTask : IBackgroundTask
    {
        internal const string DeviceDiscoverPrefix = "wingb://auth_discover?";
        internal const string AuthRequestPrefix = "wingb://auth_req?";
        internal const string DeviceAlivePrefix = "wingb://auth_alive?";
        internal const string AuthResponsePrefix = "wingb://auth_resp?";

        private BackgroundTaskDeferral _deferral;
        private ManualResetEvent _exitEvent;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background task activated!");
            _deferral = taskInstance.GetDeferral();

            _exitEvent = new ManualResetEvent(false);

            UDPListener.StartListening();
            SecondaryAuthenticationFactorAuthentication.AuthenticationStageChanged += OnStageChanged;

            _exitEvent.WaitOne();
            UDPListener.StopListening();
            findAuth = false;
            _deferral.Complete();
        }

        private async void OnStageChanged(object sender, SecondaryAuthenticationFactorAuthenticationStageChangedEventArgs e)
        {
            switch (e.StageInfo.Stage)
            {
                case SecondaryAuthenticationFactorAuthenticationStage.WaitingForUserConfirmation:
                    // Show welcome message
                    await SecondaryAuthenticationFactorAuthentication.ShowNotificationMessageAsync(
                        "",
                        SecondaryAuthenticationFactorAuthenticationMessage.LookingForDevice);
                    break;

                case SecondaryAuthenticationFactorAuthenticationStage.CollectingCredential:
                    // TODO Authenticate device
                    await DiscoverDevice();
                    break;

                case SecondaryAuthenticationFactorAuthenticationStage.CredentialAuthenticated:
                    /*if (e.StageInfo.DeviceId == _deviceId)
                    {
                        // TODO Show notification on device about PC unlock
                    }*/
                    break;

                case SecondaryAuthenticationFactorAuthenticationStage.StoppingAuthentication:
                    // TODO Quit from background task.
                    SecondaryAuthenticationFactorAuthentication.AuthenticationStageChanged -= OnStageChanged;
                    _exitEvent.Set();
                    break;
                case SecondaryAuthenticationFactorAuthenticationStage.SuspendingAuthentication:
                    break;
            }
        }

        internal static volatile bool findAuth = false;
        internal static volatile List<DeviceAuthSession> deviceSessions = new List<DeviceAuthSession>();
        private async Task DiscoverDevice()
        {
            var authStageInfo = await SecondaryAuthenticationFactorAuthentication.GetAuthenticationStageInfoAsync();
            if (authStageInfo.Stage != SecondaryAuthenticationFactorAuthenticationStage.CollectingCredential) return; // Bad status!

            findAuth = true;
            var deviceList = await SecondaryAuthenticationFactorRegistration.FindAllRegisteredDeviceInfoAsync(SecondaryAuthenticationFactorDeviceFindScope.User);
            var db = new DatabaseContext();
            var devicesInDb = db.Devices.ToList();
            db.Dispose();
            IPDiscover.DiscoverIP();
            
            foreach (var device in deviceList)
            {
                var deviceInDb = devicesInDb.Find(d => d.DeviceId.ToString() == device.DeviceId);
                if (deviceInDb == null) continue;
                var session = new DeviceAuthSession
                {
                    LastIP = deviceInDb.LastConnectedHost,
                    MACAddress = deviceInDb.DeviceMacAddress,
                    DeviceID = device.DeviceId,
                    DeviceInDb = deviceInDb
                };
                deviceSessions.Add(session);
            }
            while (findAuth)
            {
                foreach (var session in deviceSessions)
                {
                    switch (session.Status)
                    {
                        case DeviceStatus.NotConnected:
                            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(session.DeviceID));
                            var data = Encoding.UTF8.GetBytes(DeviceDiscoverPrefix + payload);
                            if (!string.IsNullOrWhiteSpace(session.LastIP))
                                try
                                {
                                    UDPListener.Send(session.LastIP, data);
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }

                            var ips = session.FindIPs()?.ToList();
                            if ((ips == null || !ips.Any()) && string.IsNullOrWhiteSpace(session.MACAddress) && IPDiscover.IsDiscoveryCompleted)
                            {
                                session.Status = DeviceStatus.Unreachable;
                                continue;
                            }

                            if (ips != null)
                            {
                                foreach (var ip in ips)
                                {
                                    try
                                    {
                                        UDPListener.Send(ip, data);
                                    }
                                    catch (Exception)
                                    {
                                        // ignored
                                    }
                                }
                            }
                            await UDPListener.Send(UDPListener.DeviceMulticastGroupAddress, data);
                            break;
                        case DeviceStatus.Unreachable:
                            deviceSessions.Remove(session);
                            break;
                        case DeviceStatus.Established:
                            findAuth = false;
                            break;
                    }
                }
                Thread.Sleep(1000);
            }

            Auth(deviceSessions.FirstOrDefault(s => s.Status == DeviceStatus.Established));
            CurrentSession = null;
        }

        internal static volatile DeviceAuthSession CurrentSession;
        internal static ManualResetEvent AuthResultReceivedEvent = new ManualResetEvent(false);
        private async void Auth(DeviceAuthSession session)
        {
            if (session == null)
            {
                await SecondaryAuthenticationFactorAuthentication.ShowNotificationMessageAsync("",
                    SecondaryAuthenticationFactorAuthenticationMessage.Invalid);
                return;
            }

            var deviceInDb = session.DeviceInDb;
            await SecondaryAuthenticationFactorAuthentication.ShowNotificationMessageAsync(deviceInDb.DeviceFriendlyName,
                SecondaryAuthenticationFactorAuthenticationMessage.ReadyToSignIn);

            IBuffer svcAuthNonce = CryptographicBuffer.GenerateRandom(256 / 8);

            SecondaryAuthenticationFactorAuthenticationResult authResult = await
                SecondaryAuthenticationFactorAuthentication.StartAuthenticationAsync(
                    session.DeviceID,
                    svcAuthNonce);
            if (authResult.Status != SecondaryAuthenticationFactorAuthenticationStatus.Started)
            {
                var message = SecondaryAuthenticationFactorAuthenticationMessage.Invalid;
                switch (authResult.Status)
                {
                    case SecondaryAuthenticationFactorAuthenticationStatus.DisabledByPolicy:
                        message = SecondaryAuthenticationFactorAuthenticationMessage.DisabledByPolicy;
                        break;
                    case SecondaryAuthenticationFactorAuthenticationStatus.InvalidAuthenticationStage:
                        break;
                    default:
                        return;
                }
                await SecondaryAuthenticationFactorAuthentication.ShowNotificationMessageAsync(null, message);
                return;
            }

            var auth = authResult.Authentication;
            CurrentSession = session;
            for (int retries = 0; retries < 3; retries++)
            {
                var svcAuthHmac = auth.ServiceAuthenticationHmac;
                var deviceNonce = auth.DeviceNonce;
                var sessionNonce = auth.SessionNonce;
                var arr = new byte[3 + svcAuthHmac.Length + deviceNonce.Length + sessionNonce.Length];
                arr[0] = (byte)svcAuthHmac.Length;
                arr[1] = (byte)deviceNonce.Length;
                arr[2] = (byte)sessionNonce.Length;
                Array.Copy(svcAuthHmac.ToArray(), 0, arr, 3, svcAuthHmac.Length);
                Array.Copy(deviceNonce.ToArray(), 0, arr, 3 + svcAuthHmac.Length, deviceNonce.Length);
                Array.Copy(sessionNonce.ToArray(), 0, arr, 3 + svcAuthHmac.Length + deviceNonce.Length, sessionNonce.Length);
                var payload = Convert.ToBase64String(arr);
                UDPListener.Send(session.LastIP, Encoding.UTF8.GetBytes(DeviceDiscoverPrefix + payload));

                AuthResultReceivedEvent = new ManualResetEvent(false);
                try
                {
                    AuthResultReceivedEvent.WaitOne(20000);
                }
                catch (Exception)
                {
                    // ignored
                }

                var result = session.ResultBytes;
                if (result == null || result.Length <= 2 || result.Length != 2 + result[0] + result[1])
                {
                    return;
                    /*await SecondaryAuthenticationFactorAuthentication.ShowNotificationMessageAsync("",
                        SecondaryAuthenticationFactorAuthenticationMessage.TryAgain);
                    await auth.AbortAuthenticationAsync("No data got.");
                    continue;*/
                }

                var deviceHmac = new Buffer(result[0]);
                var sessionHmac = new Buffer(result[1]);
                result.CopyTo(2, deviceHmac, 0, result[0]);
                result.CopyTo(2 + result[0], sessionHmac, 0, result[1]);
                var status = await auth.FinishAuthenticationAsync(deviceHmac, sessionHmac);
                switch (status)
                {
                    case SecondaryAuthenticationFactorFinishAuthenticationStatus.Completed:
                        // The credential data is collected and ready for unlock
                        CurrentSession = null;
                        return;
                    default:
                        await SecondaryAuthenticationFactorAuthentication.ShowNotificationMessageAsync("",
                            SecondaryAuthenticationFactorAuthenticationMessage.TryAgain);
                        break;
                }
            }
            await SecondaryAuthenticationFactorAuthentication.ShowNotificationMessageAsync(deviceInDb.DeviceFriendlyName,
                SecondaryAuthenticationFactorAuthenticationMessage.UnauthorizedUser);
            CurrentSession = null;
        }
    }
}
