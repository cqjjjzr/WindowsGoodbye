using System;
using System.Collections.Generic;
using System.Threading;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
// ReSharper disable InconsistentNaming

namespace WindowsGoodbye
{
    public static class IPHelperUtils
    {
        private static readonly object processLock = new object();

        public static AutoResetEvent ConnectionOpenEvent = new AutoResetEvent(false);
        public static AutoResetEvent ExitedEvent = new AutoResetEvent(false);
        private static volatile string _pendingAddr;
        private static volatile ISet<string> _resultTo;

        public static void GetMACFromIP(string ip, ISet<string> resultTo)
        {
            RunHelper(ip, resultTo, "IPToMac");
        }

        public static void GetIPFromMAC(string mac, ISet<string> resultTo)
        {
            RunHelper(mac, resultTo, "MacToIP");
        }

        private static void RunHelper(string addr, ISet<string> resultTo, string parameter)
        {
            lock (processLock)
            {
                _pendingAddr = addr;
                _resultTo = resultTo;

                FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync(parameter).Sync();
                ConnectionOpenEvent.WaitOne();
                
                App.IPHelperConnection.RequestReceived += IpHelperConnectionOnRequestReceivedAsync;
                App.IPHelperConnection.ServiceClosed += IpHelperConnectionOnServiceClosed;
                ExitedEvent.WaitOne();
            }
        }

        private static void IpHelperConnectionOnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            App.IPHelperConnection = null;
            ExitedEvent.Set();
        }

        public static void TerminateCurrent()
        {
            if (App.IPHelperConnection == null) return;
            App.IPHelperConnection.SendMessageAsync(new ValueSet {["op"] = "exit" }).AsTask().RunSynchronously();
        }

        private static void IpHelperConnectionOnRequestReceivedAsync(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            if (sender != App.IPHelperConnection) return;
            var req = args.Request;
            var msg = req.Message;
            switch ((string) msg["op"])
            {
                case "addr":
                    req.SendResponseAsync(new ValueSet {["addr"] = _pendingAddr}).AsTask().RunSynchronously();
                    break;
                case "result":
                    if ((string) msg["orig"] != _pendingAddr)
                        return;
                    _resultTo.Add((string) msg["addr"]);
                    req.SendResponseAsync(new ValueSet { ["op"] = "ok" }).AsTask().RunSynchronously();
                    break;
            }
        }
    }
}
