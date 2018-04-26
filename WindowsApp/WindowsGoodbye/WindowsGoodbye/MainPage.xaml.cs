using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using GalaSoft.MvvmLight.Messaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WindowsGoodbye
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
        public MainPage()
        {
            InitializeComponent();
            foreach (NavigationViewItemBase item in NaviView.MenuItems)
            {
                if (item is NavigationViewItem && item.Tag.ToString() == "home")
                {
                    NaviView.SelectedItem = item;
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                }
            }
            Messenger.Default.Register<OpenDeviceDetailsMessage>(this, true, msg =>
            {
                if (msg.DeviceInfo == null) ContentFrame.Navigate(typeof(HomePage));
                else ContentFrame.Navigate(typeof(DeviceDetailsPage), msg.DeviceInfo);
            });
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NaviView.Header = resourceLoader.GetString("_Title/" + e.SourcePageType.Name);
        }

        private void NavView_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                // TODO Settings
            }
            var item = sender.MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.InvokedItem);
            switch ((string)item.Tag)
            {
                case "home":
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected) return;
            //var item = sender.MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.SelectedItem);
            switch ((string) ((NavigationViewItem) args.SelectedItem).Tag)
            {
                case "pair":
                    ContentFrame.Navigate(typeof(PairingPage));
                    break;
            }
        }
    }
}
