using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using WindowsGoodbye.Annotations;

namespace WindowsGoodbye
{
    public class DeviceInfo: INotifyPropertyChanged
    {
        [Key]
        public Guid DeviceId { get; set; }
        public byte[] DeviceKey { get; set; }
        public byte[] AuthKey { get; set; }
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

        public string DeviceModelName { get; set; }
        public string DeviceMacAddress { get; set; }
        public string LastConnectedHost { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
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
