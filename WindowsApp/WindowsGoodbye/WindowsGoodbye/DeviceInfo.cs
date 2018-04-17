using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGoodbye
{
    class DeviceInfo
    {
        public Guid DeviceId;
        public byte[] DeviceKey, AuthKey;
        public string DeviceFriendName, DeviceModelName;
        public string DeviceMacAddress;
    }
}
