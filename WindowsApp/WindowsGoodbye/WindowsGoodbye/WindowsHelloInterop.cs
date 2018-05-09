using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Identity.Provider;
using Windows.Storage.Streams;
using Windows.UI.Popups;

namespace WindowsGoodbye
{
    public class WindowsHelloInterop
    {
        public static async Task RegisterDevice(DeviceInfo info, IBuffer deviceKey, IBuffer authKey)
        {
            var registrationResult =
                await SecondaryAuthenticationFactorRegistration.RequestStartRegisteringDeviceAsync(
                    info.DeviceId.ToString(),  // deviceId: max 40 wide characters. For example, serial number of the device
                    SecondaryAuthenticationFactorDeviceCapabilities.SecureStorage |
                    SecondaryAuthenticationFactorDeviceCapabilities.HMacSha256 |
                    SecondaryAuthenticationFactorDeviceCapabilities.StoreKeys,
                    info.DeviceFriendlyName, // deviceFriendlyName: max 64 wide characters. For example: John's card
                    info.DeviceModelName, // deviceModelNumber: max 32 wide characters. The app should read the model number from device.
                    deviceKey,
                    authKey);

            if (registrationResult.Status != SecondaryAuthenticationFactorRegistrationStatus.Started)
                throw new OperationCanceledException(registrationResult.Status.ToString());
            await registrationResult.Registration.FinishRegisteringDeviceAsync(null); //config data limited to 4096 bytes
            BackgroundHelper.RegisterIfNeeded();
        }

        public static async Task UnregisterDevice(DeviceInfo info)
        {
            try
            {
                await SecondaryAuthenticationFactorRegistration.UnregisterDeviceAsync(info.DeviceId.ToString());
                if ((await SecondaryAuthenticationFactorRegistration.FindAllRegisteredDeviceInfoAsync(SecondaryAuthenticationFactorDeviceFindScope.AllUsers)).Count <= 0)
                    BackgroundHelper.Unregister();
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        public static async void UnregisterAll()
        {
            var infos = await SecondaryAuthenticationFactorRegistration.FindAllRegisteredDeviceInfoAsync(SecondaryAuthenticationFactorDeviceFindScope.User);
            foreach (var info in infos)
            {
                await SecondaryAuthenticationFactorRegistration.UnregisterDeviceAsync(info.DeviceId);
            }
        }
    }
}
