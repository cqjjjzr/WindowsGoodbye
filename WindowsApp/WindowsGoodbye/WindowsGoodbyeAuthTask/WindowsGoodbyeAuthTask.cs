using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Security.Authentication.Identity.Provider;
using WindowsGoodbye;

namespace WindowsGoodbyeAuthTask
{
    public sealed class WindowsGoodbyeAuthTask : IBackgroundTask
    {
        internal const string AuthRequestPrefix = "wingb://auth_req";

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
            _deferral.Complete();
        }

        private async void OnStageChanged(object sender, SecondaryAuthenticationFactorAuthenticationStageChangedEventArgs e)
        {
            
            switch (e.StageInfo.Stage)
            {
                case SecondaryAuthenticationFactorAuthenticationStage.WaitingForUserConfirmation:
                    // Show welcome message
                    await SecondaryAuthenticationFactorAuthentication.ShowNotificationMessageAsync(
                        null,
                        SecondaryAuthenticationFactorAuthenticationMessage.LookingForDevice);
                    break;

                case SecondaryAuthenticationFactorAuthenticationStage.CollectingCredential:
                    // TODO Authenticate device
                    await Authenticate();
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

        internal volatile bool auth = false;
        internal static volatile List<DeviceAuthSession> deviceSessions = new List<DeviceAuthSession>();
        private async Task Authenticate()
        {
            var authStageInfo = await SecondaryAuthenticationFactorAuthentication.GetAuthenticationStageInfoAsync();
            if (authStageInfo.Stage != SecondaryAuthenticationFactorAuthenticationStage.CollectingCredential) return; // Bad status!

            auth = true;
            var deviceList = await SecondaryAuthenticationFactorRegistration.FindAllRegisteredDeviceInfoAsync(SecondaryAuthenticationFactorDeviceFindScope.User);
            var db = new DatabaseContext();
            var devicesInDb = db.Devices.ToList();
            IPDiscover.DiscoverIP();
            
            foreach (var device in deviceList)
            {
                var deviceInDb = devicesInDb.Find(d => d.DeviceId.ToString() == device.DeviceId);
                if (deviceInDb == null) continue;
                var session = new DeviceAuthSession
                {
                    LastIP = deviceInDb.LastConnectedHost,
                    MACAddress = deviceInDb.DeviceMacAddress,
                    DeviceID = device.DeviceId
                };
                deviceSessions.Add(session);
            }
            while (auth)
            {
                foreach (var session in deviceSessions)
                {
                    switch (session.Status)
                    {
                        case DeviceStatus.NotConnected:
                            var ips = session.FindIPs()?.ToList();
                            if ((ips == null || !ips.Any()) && string.IsNullOrWhiteSpace(session.MACAddress) && IPDiscover.IsDiscoveryCompleted)
                            {
                                session.Status = DeviceStatus.Unreachable;
                                continue;
                            }

                            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(session.DeviceID));
                            var data = Encoding.UTF8.GetBytes(AuthRequestPrefix + payload);
                            if (!string.IsNullOrWhiteSpace(session.LastIP))
                                try
                                {
                                    UDPListener.Send(session.LastIP, data);
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }
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
                            UDPListener.Send(UDPListener.DeviceMulticastGroupAddress, data);
                            break;
                        case DeviceStatus.Unreachable:
                            deviceSessions.Remove(session);
                            break;
                    }
                }
            }
        }
    }
}
