using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace WindowsGoodbyeAuthTask
{
    internal class DeviceAuthSession
    {
        public DeviceStatus Status { get; set; }
        //private Task _requestTask;
        public string MACAddress = "";
        public string DeviceID { get; set; }
        public volatile string SucceedIPAddress = null;
        public string LastIP = null;
        public volatile byte[] ResultBytes = null;

        public IEnumerable<string> FindIPs()
        {
            if (string.IsNullOrWhiteSpace(MACAddress)) return null;
            MACAddress = MACAddress.Replace('-', ':').ToUpper();
            return IPDiscover.IPAddresses.Where(p => p.Key == MACAddress).Select(p => p.Value);
        }
    }

    internal enum DeviceStatus
    {
        NotConnected,
        Established,
        Authenticated,
        Unreachable
    }
}
