using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace IPHelper
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Program
    {
        private static readonly string[] BannedNICs = {"vmware", "hamachi"};
        private static readonly string[] PreferredNICs = { "intel", "realtek" };
        private static string processingIPAddr, mode;

        private static readonly AppServiceConnection appServiceConnection = new AppServiceConnection
        {
            AppServiceName = "IPHelperService",
            PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName
        };

        public static void Main(string[] args)
        {
            /*if (args.Length < 2)
            {
                Console.WriteLine("usage: arphelper -i <ipaddress> or arphelper -m <macaddress>");
                return;
            }*/

            OpenConnection();

            mode = args[2];
            switch (mode)
            {
                case "-i":
                    new Ping().Send(args[0]);
                    SendResult(ArpUtils.GetMacFromIp(args[1]));
                    break;
                case "-m":
                    var fromArp = ArpUtils.GetIpFromMac(args[1]);
                    if (fromArp != null)
                        SendResult(fromArp);

                    var nics = NetworkInterface.GetAllNetworkInterfaces().ToList();
                    nics.RemoveAll(adapter =>
                    {
                        var name = adapter.Name.ToLower();
                        return BannedNICs.Any(ban => name.Contains(ban));
                    });
                    var rnic = nics.FindAll(adapter =>
                    {
                        var name = adapter.Name.ToLower();
                        return PreferredNICs.Any(pref => name.Contains(pref));
                    });
                    rnic.AddRange(nics.Except(rnic));
                    foreach (var adapter in rnic)
                    {
                        foreach (var ipInfo in adapter.GetIPProperties().UnicastAddresses)
                            if (ipInfo.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ipInfo.Address))
                            {
                                int totalBits = 0;
                                foreach (byte octetByteorig in ipInfo.IPv4Mask.GetAddressBytes())
                                {
                                    byte octetByte = octetByteorig;
                                    while (octetByte != 0)
                                    {
                                        totalBits += octetByte & 1;     // logical AND on the LSB
                                        octetByte >>= 1;            // do a bitwise shift to the right to create a new LSB
                                    }
                                }

                                var rst = NmapUtils.Scan($"{ipInfo.Address}/{totalBits}", args[1]);
                                if (rst != null)
                                {
                                    SendResult(rst);
                                }
                            }
                    }
                    break;
                default:
                    Environment.Exit(-1);
                    break;
            }
        }

        private static void OpenConnection()
        {
            var connectResult = SyncIAsyncOperation(appServiceConnection.OpenAsync());
            if (connectResult != AppServiceConnectionStatus.Success)
                Environment.Exit(-1);

            appServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;
            var addressResult = SyncIAsyncOperation(appServiceConnection.SendMessageAsync(new ValueSet { ["op"] = "addr" }));
            if (addressResult.Status != AppServiceResponseStatus.Success)
                Environment.Exit(-1);

            processingIPAddr = (string)addressResult.Message["addr"];
        }

        private static void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var req = args.Request;
            var msg = args.Request.Message;
            switch ((string) msg["op"])
            {
                case "exit":
                    SyncIAsyncOperation(req.SendResponseAsync(new ValueSet { ["op"] = "exit", ["status"] = "ok" }));
                    Environment.Exit(0);
                    break;
            }
        }

        private static void SendResult(string addr)
        {
            SyncIAsyncOperation(appServiceConnection.SendMessageAsync(new ValueSet
            {
                ["op"] = "addr",
                ["mode"] = mode,
                ["orig"] = processingIPAddr,
                ["addr"] = addr
            }));
        }

        private static T SyncIAsyncOperation<T>(IAsyncOperation<T> oper)
        {
            var task = oper.AsTask();
            if (task.Status == TaskStatus.Created) task.Start();
            task.Wait();
            return task.Result;
        }
    }
}
