using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WindowsGoodbye
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string TitleString = String.Empty;
        private readonly ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
        public MainPage()
        {
            this.InitializeComponent();
            foreach (NavigationViewItemBase item in NaviView.MenuItems)
            {
                if (item is NavigationViewItem && item.Tag.ToString() == "home")
                {
                    NaviView.SelectedItem = item;
                    break;
                }
            }
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NaviView.Header = resourceLoader.GetString("_Title/" + e.SourcePageType.Name);
        }

        private void NavView_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var item = sender.MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.InvokedItem);
            if ((string) ((NavigationViewItem) NaviView.SelectedItem).Tag == "home" && (string)item.Tag == "home")
                ContentFrame.Navigate(typeof(HomePage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                //ContentFrame.Navigate(typeof(SettingsPage));
            }
            else
            {
                //var item = sender.MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.SelectedItem);
                switch ((string)((NavigationViewItem)args.SelectedItem).Tag)
                {
                    case "home":
                        ContentFrame.Navigate(typeof(DeviceDetailsPage));
                        break;
                    case "pair":
                        ContentFrame.Navigate(typeof(PairingPage));
                        break;
                }
            }
        }
    }
}
