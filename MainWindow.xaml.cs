using System;
using System.Windows;
using System.Windows.Controls;
using KvHostlinkLib;

namespace KVNC1EPTestApp
{
    public partial class MainWindow : Window
    {
        private KvHostlinkLib.KvHostlinkLib kvSockets;

        public MainWindow()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            // 初始化設備類型下拉選單
            DeviceTypeComboBox.Items.Add("DM");
            DeviceTypeComboBox.Items.Add("MR");
            DeviceTypeComboBox.Items.Add("R");
            DeviceTypeComboBox.SelectedIndex = 0;

            // 為設備類型選擇添加事件處理
            DeviceTypeComboBox.SelectionChanged += DeviceTypeComboBox_SelectionChanged;
        }

          private void DeviceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedDevice = DeviceTypeComboBox.SelectedItem as string;
            switch (selectedDevice)
            {
                case "R":
                    // 隱藏寫入相關的控件
                    WriteValueTextBox.Visibility = Visibility.Collapsed;
                    WriteButton.Visibility = Visibility.Collapsed;
                    LogMessage("R 設備僅支持讀取操作");
                    break;
                case "MR":
                    WriteValueTextBox.Visibility = Visibility.Visible;
                    WriteButton.Visibility = Visibility.Visible;
                    WriteValueTextBox.MaxLength = 1; // 限制輸入為單一字符（0或1）
                    WriteValueTextBox.Text = "0"; // 默認值設為0
                    break;
                default: // DM
                    WriteValueTextBox.Visibility = Visibility.Visible;
                    WriteButton.Visibility = Visibility.Visible;
                    WriteValueTextBox.MaxLength = 5; // 允許輸入更大的數值
                    WriteValueTextBox.Text = "0"; // 重置為默認值
                    break;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ipAddress = IpAddressTextBox.Text;
                int port = int.Parse(PortTextBox.Text);

                kvSockets = new KvHostlinkLib.KvHostlinkLib(
                    socketType: (int)KvHostlinkLib.KvHostlinkLib.socketType.tcp,
                    socketNum: 1,
                    timeOutMs: 5000
                );

                int errCode = kvSockets.ConnectAll(ipAddress, port);

                if (errCode != 0)
                {
                    LogMessage($"連接錯誤: {kvSockets.ErrMsg(errCode)}");
                }
                else
                {
                    LogMessage("成功連接到 PLC");
                    ConnectButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"連接異常: {ex.Message}");
            }
        }

        private void WriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (kvSockets == null)
            {
                LogMessage("請先連接到 PLC");
                return;
            }

            try
            {
                string deviceType = DeviceTypeComboBox.SelectedItem as string;
                if (deviceType == "R")
                {
                    LogMessage("R 設備不支持寫入操作");
                    return;
                }

                int startAddress = int.Parse(StartAddressTextBox.Text);
                int count = int.Parse(CountTextBox.Text);
                int writeValue = int.Parse(WriteValueTextBox.Text);
                WriteWord(deviceType, startAddress, count, writeValue);
            }
            catch (Exception ex)
            {
                LogMessage($"寫入異常: {ex.Message}");
            }
        }

        private void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (kvSockets == null)
            {
                LogMessage("請先連接到 PLC");
                return;
            }

            try
            {
                string deviceType = DeviceTypeComboBox.SelectedItem as string;
                int startAddress = int.Parse(StartAddressTextBox.Text);
                int count = int.Parse(CountTextBox.Text);

                if (deviceType == "R")
                {
                    ReadBit(deviceType, startAddress, count);
                }
                else
                {
                    ReadWord(deviceType, startAddress, count);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"讀取異常: {ex.Message}");
            }
        }

        private void WriteWord(string deviceType, int startAddress, int count, int value)
        {
            byte[] writeData = new byte[count * 2];
            for (int i = 0; i < count; i++)
            {
                BitConverter.GetBytes((short)value).CopyTo(writeData, i * 2);
            }

            int errCode = kvSockets.WriteDevices(deviceType, startAddress, count * 2, ref writeData);

            if (errCode != 0)
            {
                LogMessage($"寫入錯誤: {kvSockets.ErrMsg(errCode)}");
            }
            else
            {
                LogMessage($"成功寫入 {count} 個 {deviceType} 設備，起始地址: {startAddress}，值: {value}");
            }
        }

        private void WriteBit(string deviceType, int startAddress, int count, bool value)
        {
            byte[] writeData = new byte[(count + 7) / 8]; // 計算需要多少字節來存儲位元
            for (int i = 0; i < count; i++)
            {
                if (value)
                {
                    writeData[i / 8] |= (byte)(1 << (i % 8));
                }
            }

            int errCode = kvSockets.WriteDevices(deviceType, startAddress, writeData.Length, ref writeData);

            if (errCode != 0)
            {
                LogMessage($"寫入錯誤: {kvSockets.ErrMsg(errCode)}");
            }
            else
            {
                LogMessage($"成功寫入 {count} 個 {deviceType} 位元，起始地址: {startAddress}，值: {(value ? 1 : 0)}");
            }
        }

        private void ReadWord(string deviceType, int startAddress, int count)
        {
            byte[] readData = new byte[count * 2];
            int errCode = kvSockets.ReadDevices(deviceType, startAddress, count * 2, ref readData);

            if (errCode != 0)
            {
                LogMessage($"讀取錯誤: {kvSockets.ErrMsg(errCode)}");
            }
            else
            {
                LogMessage($"成功讀取 {count} 個 {deviceType} 設備，起始地址: {startAddress}");
                for (int i = 0; i < count; i++)
                {
                    int value = BitConverter.ToInt16(readData, i * 2);
                    LogMessage($"{deviceType}{startAddress + i}: {value}");
                }
            }
        }

        private void ReadBit(string deviceType, int startAddress, int count)
        {
            byte[] readData = new byte[(count + 7) / 8]; // 計算需要多少字節來存儲位元
            int errCode = kvSockets.ReadDevices(deviceType, startAddress, readData.Length, ref readData);

            if (errCode != 0)
            {
                LogMessage($"讀取錯誤: {kvSockets.ErrMsg(errCode)}");
            }
            else
            {
                LogMessage($"成功讀取 {count} 個 {deviceType} 位元，起始地址: {startAddress}");
                for (int i = 0; i < count; i++)
                {
                    bool bitValue = (readData[i / 8] & (1 << (i % 8))) != 0;
                    LogMessage($"{deviceType}{startAddress + i}: {(bitValue ? 1 : 0)}");
                }
            }
        }

        private void LogMessage(string message)
        {
            LogTextBox.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
            LogTextBox.ScrollToEnd();
        }
    }
}