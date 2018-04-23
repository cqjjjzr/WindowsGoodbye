using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace WindowsGoodbye
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public IList<DeviceInfo> Devices = new ObservableCollection<DeviceInfo>();
        public HomePage()
        {
            this.InitializeComponent();
            Devices.Add(new DeviceInfo { DeviceFriendlyName = "INNNPRIU0", DeviceModelName = "PJMISOPDNMF" });
            DevicesGrid.ItemClick += (sender, args) =>
                {
                    Devices.Add(new DeviceInfo {DeviceFriendlyName = "INNNPRIU0", DeviceModelName = "PJMISOPDNMF"});
                };
            
        }
    }
}
