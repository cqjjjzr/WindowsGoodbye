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
    internal static class IPDiscover
    {
        public static bool IsDiscoveryCompleted => _discoverConnection == null;
        private static AppServiceConnection _discoverConnection;
        public static ConcurrentDictionary<string, string> IPAddresses = new ConcurrentDictionary<string, string>();
        
        internal static async void DiscoverIP()
        {
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
                    case "result":
                        var res = msg["addr"];
                        if (res is Dictionary<string, string> dict)
                        {
                            foreach (var pair in dict)
                            {
                                IPAddresses.AddOrUpdate(pair.Key, s => pair.Value, (s1, s2) => pair.Value);
                            }
                        }
                        req.SendResponseAsync(new ValueSet()).GetAwaiter().GetResult();
                        break;
                }
            };
            _discoverConnection.ServiceClosed += (sender, args) => StopDiscovery();
        }

        internal static void StopDiscovery()
        {
            _discoverConnection?.Dispose();
            _discoverConnection = null;
        }
    }
}
