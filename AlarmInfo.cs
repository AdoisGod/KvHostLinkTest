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
        private string name;        // 異常名稱 (對應CSV的C列)
        private string details;     // 詳細內容 (對應CSV的D-J列)

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

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string Details
        {
            get => details;
            set
            {
                details = value;
                OnPropertyChanged(nameof(Details));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
