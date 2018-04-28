using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace WindowsGoodbye
{
    public class DeviceInfo: INotifyPropertyChanged
    {
        [Key]
        public Guid DeviceId { get; set; }
        private string _deviceFriendlyName = "";
        public string DeviceFriendlyName
        {
            get => _deviceFriendlyName;
            set
            {
                _deviceFriendlyName = value;
                OnPropertyChanged(nameof(DeviceFriendlyName));
            }
        }

        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }

        public string DeviceModelName { get; set; }

        private string _deviceMacAddress;
        public string DeviceMacAddress
        {
            get => _deviceMacAddress;
            set
            {
                _deviceMacAddress = value;
                OnPropertyChanged(nameof(DeviceMacAddress));
            }
        }
        public string LastConnectedHost { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DeviceAuthRecord
    {
        [Key]
        public int Id { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime Time { get; set; }
    }
}
