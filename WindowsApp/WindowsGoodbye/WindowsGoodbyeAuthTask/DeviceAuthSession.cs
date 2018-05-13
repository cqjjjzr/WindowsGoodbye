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
        public bool IsDiscoveryCompleted { get; set; }
        //private Task _requestTask;
        public string MACAddress = "";
        public readonly ConcurrentBag<string> IPAddresses = new ConcurrentBag<string>();
        public string DeviceID { get; set; }
        public volatile string SucceedIPAddress = null;
        private volatile AppServiceConnection _discoverConnection;

        internal async void DiscoverIP()
        {
            IsDiscoveryCompleted = false;
            _discoverConnection = new AppServiceConnection
            {
                AppServiceName = "IPDiscoverService",
                PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName
            };
            await _discoverConnection.OpenAsync();
            _discoverConnection.RequestReceived += (sender, args) =>
            {
                var req = args.Request;
                var msg = args.Request.Message;
                switch (msg["op"])
                {
                    case "addr":
                        req.SendResponseAsync(new ValueSet{["addr"] = MACAddress}).GetAwaiter().GetResult();
                        break;
                    case "result":
                        IPAddresses.Add((string) msg["addr"]);
                        req.SendResponseAsync(new ValueSet()).GetAwaiter().GetResult();
                        break;
                }
            };
        }

        internal void StopDiscovering()
        {
            _discoverConnection.Dispose();
        }
    }

    internal enum DeviceStatus
    {
        NotConnected,
        Established,
        AuthRequested,
        Unreachable
    }
}
