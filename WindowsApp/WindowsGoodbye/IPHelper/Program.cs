using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
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
#if DEBUG
            Console.WriteLine(string.Join(", ", args));
#endif

            var t = OpenConnection();
            t.Wait();
            processingIPAddr = t.Result;

            //Console.ReadKey();
#if DEBUG
            Console.WriteLine("Mode: " + mode);
#endif
            switch (mode)
            {
                case "-i":
                    new Ping().Send(processingIPAddr);
                    SendResult(ArpUtils.GetMacFromIp(processingIPAddr));
                    break;
                case "-m":
                    if (processingIPAddr != null)
                    {
                        var fromArp = ArpUtils.GetIpFromMac(processingIPAddr);
                        if (fromArp != null)
                            SendResult(fromArp);
                    }

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

                                NmapUtils.Scan($"{ipInfo.Address}/{totalBits}", processingIPAddr);
                            }
                    }
                    break;
                default:
                    Thread.Sleep(2000);
                    //appServiceConnection.Dispose();
                    Environment.Exit(-1);
                    break;
            }
            Thread.Sleep(1000);
            Exit();
        }

        private static async Task<string> OpenConnection()
        {
#if DEBUG
            Console.WriteLine("Connect to the app..." + appServiceConnection.PackageFamilyName + " " + appServiceConnection.AppServiceName);
#endif
            var connectResult = await appServiceConnection.OpenAsync();
            if (connectResult != AppServiceConnectionStatus.Success)
                Environment.Exit(-1);

#if DEBUG
            Console.WriteLine("Established.");
#endif
            appServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;
            appServiceConnection.ServiceClosed += (sender, args) =>
            {
#if DEBUG
                Console.WriteLine("What's the fucking shit?");
                //Console.ReadKey();
#endif
                //Environment.Exit(0);
            };
            var addressResult = await appServiceConnection.SendMessageAsync(new ValueSet { ["op"] = "addr" });
#if DEBUG
            Console.WriteLine("Address result: " + addressResult.Status);
            //Console.ReadKey();
#endif
            if (addressResult.Status != AppServiceResponseStatus.Success)
            {
#if DEBUG
                Console.WriteLine("Error getting address! " + addressResult.Status);
                //Console.ReadKey();
#endif
                Thread.Sleep(2000);
                //appServiceConnection.Dispose();
                Environment.Exit(-1);
            }
            processingIPAddr = (string)addressResult.Message["addr"];
            mode = (string) addressResult.Message["mode"];
#if DEBUG
            Console.WriteLine("Got address! " + processingIPAddr);
            //Console.ReadKey();
#endif
            return processingIPAddr;
        }

        private static async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var msg = args.Request.Message;
            switch ((string) msg["op"])
            {
                case "exit":
                    await args.Request.SendResponseAsync(new ValueSet());
                    Exit();
                    Environment.Exit(0);
                    break;
            }
        }

        public static async void SendResult(string result)
        {
#if DEBUG
            Console.WriteLine("Send result!" + result);
            //Console.ReadKey();
#endif
            await appServiceConnection.SendMessageAsync(new ValueSet
            {
                ["op"] = "result",
                ["mode"] = mode,
                ["orig"] = processingIPAddr,
                ["content"] = result
            });
#if DEBUG
            //Console.ReadKey();
#endif
        }

        private static async void Exit()
        {
#if DEBUG
            Console.WriteLine("Exiting!");
            //Console.ReadKey();
#endif
            await appServiceConnection.SendMessageAsync(new ValueSet
            {
                ["op"] = "exit",
                ["status"] = "ok"
            });
            //appServiceConnection.Dispose();
        }
    }
}
