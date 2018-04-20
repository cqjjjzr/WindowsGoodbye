using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace WindowsGoodbye
{
    class IPHelperUtils
    {
        private object processLock;

        public void GetMACFromIP(string ip)
        {
            lock (processLock)
            {

            }
            FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync("IPToMac").Sync();

        }
    }

    class IPHelperReadyMessage
    {

    }
}
