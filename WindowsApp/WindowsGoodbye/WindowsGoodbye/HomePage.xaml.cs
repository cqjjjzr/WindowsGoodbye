using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace WindowsGoodbye
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private readonly static DateTime TimeStampEpoch = new DateTime(1970, 1, 1);
        public List<DeviceInfo> DevicesSet = new List<DeviceInfo>();
        public HomePage()
        {
            InitializeComponent();
            App.DbContext.Devices.Load();
            var enabled = App.DbContext.Devices.Where(info => info.Enabled).OrderBy(info =>
                App.DbContext.AuthRecords.Where(record => info.DeviceId == record.DeviceId)
                    .Select(record => record.Time).DefaultIfEmpty(DateTime.MinValue).Max());
            var disabled = App.DbContext.Devices.Where(info => !info.Enabled).OrderBy(info =>
                App.DbContext.AuthRecords.Where(record => info.DeviceId == record.DeviceId)
                    .Select(record => record.Time).DefaultIfEmpty(DateTime.MinValue).Max());
            DevicesSet.AddRange(enabled);
            DevicesSet.AddRange(disabled);
            DevicesGrid.ItemClick += (sender, args) =>
            {
                Messenger.Default.Send(new OpenDeviceDetailsMessage {DeviceInfo = (DeviceInfo) args.ClickedItem});
            };
        }
    }

    class OpenDeviceDetailsMessage
    {
        public DeviceInfo DeviceInfo;
    }
}
