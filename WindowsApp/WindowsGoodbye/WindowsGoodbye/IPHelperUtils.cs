using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Microsoft.EntityFrameworkCore.ChangeTracking;

// ReSharper disable InconsistentNaming

namespace WindowsGoodbye
{
    public partial class App
    {
        public static volatile IList<IPHelperContext> Contexts = new List<IPHelperContext>();

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            if (!(args.TaskInstance.TriggerDetails is AppServiceTriggerDetails)) return;
            var appServiceDeferral = args.TaskInstance.GetDeferral();
            AppServiceConnection connection = ((AppServiceTriggerDetails)args.TaskInstance.TriggerDetails).AppServiceConnection;
            if (Contexts.Count <= 0)
            {
                await IPHelperUtils.TerminateIPHelper(connection);
                return;
            }
            var context = Contexts.First();
            context.Connection = connection;
            connection.RequestReceived += IPHelperUtils.ConnectionOnRequestReceivedAsync;
            connection.ServiceClosed += (sender, eventArgs) =>
            {
                context.After(context.Result);
                Contexts.Remove(context);
            };
        }
    }

    public static class IPHelperUtils
    {
        public delegate void ProcessResult(ISet<string> result);

        public static async void GetMACFromIP(string ip, ProcessResult after)
        {
            await RunHelper(ip, after, "IPToMac");
        }

        public static async void GetIPFromMAC(string mac, ProcessResult after)
        {
            await RunHelper(mac, after, "MacToIP");
        }

        private static async Task RunHelper(string addr, ProcessResult after, string parameter)
        {
            var ctxt = new IPHelperContext
            {
                Connection = null,
                OriginalAddress = addr,
                Result = new HashSet<string>(),
                After = after
            };

            App.Contexts.Add(ctxt);
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync(parameter);
        }

        public static async void ConnectionOnRequestReceivedAsync(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var req = args.Request;
            var msg = req.Message;
            var ctxts = App.Contexts.Where(ctx => ctx.Connection == sender);
            if (!ctxts.Any())
            {
                await args.Request.SendResponseAsync(new ValueSet());
                await TerminateIPHelper(sender);
                return;
            }

            var ctxt = ctxts.First();
            switch ((string) msg["op"])
            {
                case "addr":
                    await req.SendResponseAsync(new ValueSet {["addr"] = ctxt.OriginalAddress });
                    break;
                case "result":
                    ctxt.Result.Add((string) msg["addr"]);
                    await req.SendResponseAsync(new ValueSet { ["op"] = "ok" });
                    break;
                case "exit":
                    ctxt.After(ctxt.Result);
                    App.Contexts.Remove(ctxt);
                    break;
            }
        }

        public static async Task TerminateIPHelper(AppServiceConnection conn)
        {
            await conn.SendMessageAsync(new ValueSet { ["op"] = "exit" }).AsTask();
        }
    }

    public class IPHelperContext
    {
        public string OriginalAddress { get; set; }
        public ISet<string> Result { get; set; }
        public AppServiceConnection Connection { get; set; }
        public IPHelperUtils.ProcessResult After { get; set; }

        public async void Terminate()
        {
            await IPHelperUtils.TerminateIPHelper(Connection);
        }
    }
}
