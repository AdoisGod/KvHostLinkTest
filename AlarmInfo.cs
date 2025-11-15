using System;
using System.ComponentModel;

namespace KVNC1EPTestApp
{
    /// <summary>
    /// 報警信息類，用於DataGrid顯示
    /// </summary>
    public class AlarmInfo : INotifyPropertyChanged
    {
        private string timestamp;
        private string address;
        private string description;

        public string Timestamp
        {
            get => timestamp;
            set
            {
                timestamp = value;
                OnPropertyChanged(nameof(Timestamp));
            }
        }

        public string Address
        {
            get => address;
            set
            {
                address = value;
                OnPropertyChanged(nameof(Address));
            }
        }

        public string Description
        {
            get => description;
            set
            {
                description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
