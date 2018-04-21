using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;

namespace WindowsGoodbye
{
    public class DeviceInfo
    {
        public Guid DeviceId;
        public byte[] DeviceKey, AuthKey;
        public string DeviceFriendlyName, DeviceModelName;
        public string DeviceMacAddress;
        public HostName LastConnectedHost;
    }
}
