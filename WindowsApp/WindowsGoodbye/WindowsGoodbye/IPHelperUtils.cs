using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

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
            AppServiceConnection connection = ((AppServiceTriggerDetails)args.TaskInstance.TriggerDetails).AppServiceConnection;
            var appServiceDeferral = args.TaskInstance.GetDeferral();
            switch (connection.AppServiceName)
            {
                case "IPHelperService":
                    args.TaskInstance.Canceled += (sender, reason) =>
                    {
                        try
                        {
                            appServiceDeferral.Complete();
                        }
                        catch (Exception)
                        { // ignored
                        }
                    };
                    if (Contexts.Count <= 0)
                    {
                        await IPHelperUtils.TerminateIPHelper(connection);
                        appServiceDeferral.Complete();
                        return;
                    }
                    var context = Contexts.First();
                    context.Connection = connection;
                    context.Deferral = appServiceDeferral;
                    args.TaskInstance.Canceled += (sender, reason) => context.DoFinal();
                    connection.RequestReceived += IPHelperUtils.ConnectionOnRequestReceivedAsync;
                    connection.ServiceClosed += IPHelperUtils.OnServiceClosed;
                    break;
                case "IPDiscoverService":
                    args.TaskInstance.Canceled += (sender, reason) =>
                    {
                        try
                        {
                            appServiceDeferral.Complete();
                        }
                        catch (Exception)
                        { // ignored
                        }
                    };
                    var mac = await connection.SendMessageAsync(new ValueSet { ["op"] = "addr" });
                    IPHelperUtils.GetIPFromMAC((string) mac.Message["addr"], (result, last) =>
                    {
                        if (last)
                            appServiceDeferral.Complete();
                        if (result == null) return true;
                        connection.SendMessageAsync(new ValueSet {["op"] = "result", ["addr"] = result}).GetAwaiter()
                            .GetResult();
                        return true;
                    });
                    connection.ServiceClosed += (sender, a) =>
                    {
                        try
                        {
                            appServiceDeferral.Complete();
                        }
                        catch (Exception)
                        {
                            //ignored
                        }
                    };
                    break;
                default:
                    appServiceDeferral.Complete();
                    break;
            }
        }
    }

    public static class IPHelperUtils
    {
        public delegate bool ProcessResult(string result, bool last);

        public static async void GetMACFromIP(string ip, ProcessResult after)
        {
            await RunHelper(ip, after, "=i");
        }

        public static async void GetIPFromMAC(string mac, ProcessResult after)
        {
            await RunHelper(mac, after, "-m");
        }

        private static async Task RunHelper(string addr, ProcessResult after, string mode)
        {
            var ctxt = new IPHelperContext
            {
                Connection = null,
                OriginalAddress = addr,
                Mode = mode,
                After = after
            };

            App.Contexts.Add(ctxt);
            try
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
            catch (Exception)
            {
                App.Contexts.Remove(ctxt);
                after(null, true);
            }
        }

        public static async void ConnectionOnRequestReceivedAsync(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            //var deferral = args.GetDeferral();
            var req = args.Request;
            var msg = req.Message;
            var ctxts = App.Contexts.Where(ctx => ctx.Connection == sender);
            var ctxt = ctxts.FirstOrDefault();
            if (ctxt == null)
            {
                await args.Request.SendResponseAsync(new ValueSet());
                await TerminateIPHelper(sender);
                //deferral.Complete();
                return;
            }

            switch ((string) msg["op"])
            {
                case "addr":
                    await req.SendResponseAsync(new ValueSet {["addr"] = ctxt.OriginalAddress, ["mode"] = ctxt.Mode });
                    break;
                case "result":
                    //Debug.WriteLine("received!!!!!!!!");
                    bool cont = ctxt.After((string) msg["addr"], false);
                    Thread.Sleep(1000);
                    await req.SendResponseAsync(new ValueSet { ["op"] = "ok" });
                    if (!cont) await TerminateIPHelper(ctxt.Connection);
                    break;
                case "exit":
                    await args.Request.SendResponseAsync(new ValueSet());
                    ctxt.Deferral.Complete();
                    ctxt.DoFinal();
                    ctxt.Connection.ServiceClosed -= OnServiceClosed;
                    break;
                default:
                    await req.SendResponseAsync(new ValueSet { ["wtf"] = "What the fuck u've said?" });
                    break;
            }
        }

        public static async Task TerminateIPHelper(AppServiceConnection conn)
        {
            await conn.SendMessageAsync(new ValueSet { ["op"] = "exit" }).AsTask();
        }

        public static void OnServiceClosed(object sender, AppServiceClosedEventArgs args)
        {
            var ctxts = App.Contexts.Where(ctx => ctx.Connection == sender);
            var ctxt = ctxts.FirstOrDefault();
            if (ctxt == null) return;
            try
            {
                ctxt.Deferral.Complete();
            }
            catch (Exception)
            {
                //ignored
            }
            ctxt.DoFinal();
        }
    }

    public class IPHelperContext
    {
        public string OriginalAddress { get; set; }
        public AppServiceConnection Connection { get; set; }
        public IPHelperUtils.ProcessResult After { get; set; }
        public BackgroundTaskDeferral Deferral { get; set; }
        public string Mode { get; set; }
        public volatile bool Finished;

        public async void Terminate()
        {
            await IPHelperUtils.TerminateIPHelper(Connection);
        }

        public void DoFinal()
        {
            if (Finished) return;
            Finished = true;
            App.Contexts.Remove(this);
            After(null, true);
        }
    }
}
