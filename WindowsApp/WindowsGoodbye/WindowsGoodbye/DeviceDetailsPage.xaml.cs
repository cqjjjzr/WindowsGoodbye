using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using GalaSoft.MvvmLight.Messaging;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace WindowsGoodbye
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class DeviceDetailsPage : Page
    {
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
        private DeviceInfo Info = new DeviceInfo
        {
            DeviceFriendlyName = "CHarlie's Phone",
            DeviceModelName = "Samsung Note 7",
            DeviceMacAddress = "23:33:33:33:33:33",
            DeviceId = Guid.NewGuid()
        };

        private IList<DeviceAuthRecord> Records;

        public DeviceDetailsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Info = (DeviceInfo) e.Parameter;
            Debug.Assert(Info != null, nameof(Info) + " != null");
            Records = App.DbContext.AuthRecords.Where(record => record.DeviceId == Info.DeviceId).ToList();
            for (int i = 0;i < 50;i++)
            Records.Add(new DeviceAuthRecord
            {
                DeviceId = Info.DeviceId,
                Id = 10,
                Time = DateTime.Now
            });
            EnabledToggle.IsOn = Info.Enabled;
        }

        private void EditFriendlyNameButton_Click(object sender, RoutedEventArgs e)
        {
            if (FriendlyNameEditor.Visibility == Visibility.Visible)
            {
                EditingErrorText.Visibility = Visibility.Collapsed;
                if (string.IsNullOrWhiteSpace(FriendlyNameEditor.Text))
                {
                    RefreshEditor();
                    EditingErrorText.Text = resourceLoader.GetString("_FriendlyNameCantBeEmpty");
                    EditingErrorText.Visibility = Visibility.Visible;
                    return;
                }

                Info.DeviceFriendlyName = FriendlyNameEditor.Text;
                App.DbContext.SaveChanges();
            }

            FriendlyNameText.Visibility = FriendlyNameText.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            FriendlyNameEditor.Visibility = FriendlyNameEditor.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            DiscardEditingButton.Visibility = DiscardEditingButton.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            EditFriendlyNameButton.Content = (string) EditFriendlyNameButton.Content == "\uE70F" ? "\uEC61" : "\uE70F";
            RefreshEditor();
        }

        private void RefreshEditor()
        {
            FriendlyNameEditor.Text = Info.DeviceFriendlyName;
            FriendlyNameEditor.Focus(FocusState.Programmatic);
            FriendlyNameEditor.SelectAll();
        }

        private void DiscardEditingButton_Click(object sender, RoutedEventArgs e)
        {
            EditingErrorText.Visibility = Visibility.Collapsed;
            FriendlyNameText.Visibility = Visibility.Visible;
            FriendlyNameEditor.Visibility = Visibility.Collapsed;
            DiscardEditingButton.Visibility = Visibility.Collapsed;
            EditFriendlyNameButton.Content = "\uE70F";
        }

        private void FriendlyNameEditor_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter) 
                EditFriendlyNameButton_Click(sender, null);
            else if (e.Key == VirtualKey.Escape)
                DiscardEditingButton_Click(sender, null);
            else return;
            EditFriendlyNameButton.Focus(FocusState.Programmatic);
        }

        private void EnabledToggle_Toggled(object sender, RoutedEventArgs e)
        {
            Info.Enabled = EnabledToggle.IsOn;
            App.DbContext.SaveChanges();
        }

        private async void DeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            var confirmDialog = new ContentDialog
            {
                Title = resourceLoader.GetString("_DeleteConfirmTitle"),
                Content = string.Format(resourceLoader.GetString("_DeleteConfirmText"), Info.DeviceFriendlyName, Info.DeviceId.ToString()),
                PrimaryButtonText = resourceLoader.GetString("_Delete"),
                CloseButtonText = resourceLoader.GetString("_Cancel")
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            App.DbContext.Devices.Remove(Info);
            App.DbContext.SaveChanges();
            // TODO unregister in os

            Messenger.Default.Send(new OpenDeviceDetailsMessage());
        }
    }
}
