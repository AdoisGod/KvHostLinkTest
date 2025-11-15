using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KvHostlinkLib;

namespace KVNC1EPTestApp
{
    public partial class MainWindow : Window
    {
        private KvHostlinkLib.KvHostlinkLib kvSockets;

        // 監控相關成員變量
        private DispatcherTimer monitorTimer;
        private bool[] previousStates;  // 存儲前一次的狀態，用於檢測變化
        private int monitorStartAddress;
        private int monitorEndAddress;
        private ObservableCollection<AlarmInfo> activeAlarms;

        public MainWindow()
        {
            InitializeComponent();
            InitializeUI();
            InitializeMonitoring();
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

        private void InitializeMonitoring()
        {
            // 初始化活動報警集合
            activeAlarms = new ObservableCollection<AlarmInfo>();
            ActiveAlarmsDataGrid.ItemsSource = activeAlarms;

            // 初始化定時器
            monitorTimer = new DispatcherTimer();
            monitorTimer.Tick += MonitorTimer_Tick;

            // 嘗試自動加載 CSV 配置
            TryLoadCSVConfig();
        }

        /// <summary>
        /// 嘗試加載 CSV 配置文件
        /// </summary>
        private void TryLoadCSVConfig()
        {
            if (AlarmConfig.LoadFromCSV("ERRORCODE.csv"))
            {
                CSVStatusTextBlock.Text = $"已加載 {AlarmConfig.Count} 筆配置";
                CSVStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                LogMessage($"成功加載 CSV 配置：{AlarmConfig.Count} 筆資料");
            }
            else
            {
                CSVStatusTextBlock.Text = $"未加載 ({AlarmConfig.LastError})";
                CSVStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                LogMessage($"CSV 配置加載失敗：{AlarmConfig.LastError}");
            }
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

        #region 監控功能

        /// <summary>
        /// 加載 CSV 配置按鈕點擊事件
        /// </summary>
        private void LoadCSVButton_Click(object sender, RoutedEventArgs e)
        {
            TryLoadCSVConfig();
        }

        /// <summary>
        /// 開始監控按鈕點擊事件
        /// </summary>
        private void StartMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            if (kvSockets == null)
            {
                LogMessage("請先連接到 PLC");
                return;
            }

            try
            {
                // 獲取監控參數
                monitorStartAddress = int.Parse(MonitorStartAddressTextBox.Text);
                monitorEndAddress = int.Parse(MonitorEndAddressTextBox.Text);
                int interval = int.Parse(MonitorIntervalTextBox.Text);

                // 驗證參數
                if (monitorStartAddress < 0 || monitorEndAddress < monitorStartAddress)
                {
                    LogMessage("監控地址範圍無效");
                    return;
                }

                if (interval < 100)
                {
                    LogMessage("掃描間隔不能小於100ms");
                    return;
                }

                // 初始化狀態緩存
                int totalBits = monitorEndAddress - monitorStartAddress + 1;
                previousStates = new bool[totalBits];

                // 啟動定時器
                monitorTimer.Interval = TimeSpan.FromMilliseconds(interval);
                monitorTimer.Start();

                // 更新UI狀態
                StartMonitorButton.IsEnabled = false;
                StopMonitorButton.IsEnabled = true;
                MonitorStartAddressTextBox.IsEnabled = false;
                MonitorEndAddressTextBox.IsEnabled = false;
                MonitorIntervalTextBox.IsEnabled = false;

                LogMessage($"開始監控 R{monitorStartAddress} 到 R{monitorEndAddress}，間隔 {interval}ms");
            }
            catch (Exception ex)
            {
                LogMessage($"啟動監控失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止監控按鈕點擊事件
        /// </summary>
        private void StopMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            StopMonitoring();
        }

        /// <summary>
        /// 清除報警按鈕點擊事件
        /// </summary>
        private void ClearAlarmsButton_Click(object sender, RoutedEventArgs e)
        {
            activeAlarms.Clear();
            LogMessage("已清除所有報警記錄");
        }

        /// <summary>
        /// 停止監控
        /// </summary>
        private void StopMonitoring()
        {
            monitorTimer.Stop();

            // 更新UI狀態
            StartMonitorButton.IsEnabled = true;
            StopMonitorButton.IsEnabled = false;
            MonitorStartAddressTextBox.IsEnabled = true;
            MonitorEndAddressTextBox.IsEnabled = true;
            MonitorIntervalTextBox.IsEnabled = true;

            LogMessage("已停止監控");
        }

        /// <summary>
        /// 定時器Tick事件，執行監控掃描
        /// </summary>
        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                PerformMonitorScan();
            }
            catch (Exception ex)
            {
                LogMessage($"監控掃描異常: {ex.Message}");
            }
        }

        /// <summary>
        /// 執行監控掃描
        /// </summary>
        private void PerformMonitorScan()
        {
            int totalBits = monitorEndAddress - monitorStartAddress + 1;
            byte[] readData = new byte[(totalBits + 7) / 8];

            // 讀取R區域數據
            int errCode = kvSockets.ReadDevices("R", monitorStartAddress, readData.Length, ref readData);

            if (errCode != 0)
            {
                LogMessage($"監控讀取錯誤: {kvSockets.ErrMsg(errCode)}");
                return;
            }

            // 檢測狀態變化
            DetectStateChanges(readData, totalBits);
        }

        /// <summary>
        /// 檢測狀態變化（上升沿檢測）
        /// </summary>
        private void DetectStateChanges(byte[] currentData, int totalBits)
        {
            for (int i = 0; i < totalBits; i++)
            {
                // 讀取當前位狀態
                bool currentState = (currentData[i / 8] & (1 << (i % 8))) != 0;
                bool previousState = previousStates[i];

                // 檢測上升沿（0->1）
                if (currentState && !previousState)
                {
                    // 狀態從0變為1，觸發報警
                    int address = monitorStartAddress + i;
                    OnAlarmTriggered(address);
                }
                // 檢測下降沿（1->0）
                else if (!currentState && previousState)
                {
                    // 狀態從1變為0，移除報警
                    int address = monitorStartAddress + i;
                    OnAlarmCleared(address);
                }

                // 更新狀態緩存
                previousStates[i] = currentState;
            }
        }

        /// <summary>
        /// 報警觸發事件
        /// </summary>
        private void OnAlarmTriggered(int address)
        {
            AlarmDetail detail = AlarmConfig.GetAlarmDetail(address);
            string addressString = $"R{address:D4}";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 添加到活動報警列表
            var alarmInfo = new AlarmInfo
            {
                Timestamp = timestamp,
                Address = addressString,
                Name = detail.Name,
                Details = detail.Details
            };

            activeAlarms.Insert(0, alarmInfo); // 插入到最前面

            // 記錄到日誌
            LogMessage($"[報警] {addressString} - {detail.Name}: {detail.Details}");
        }

        /// <summary>
        /// 報警清除事件
        /// </summary>
        private void OnAlarmCleared(int address)
        {
            string addressString = $"R{address:D4}";

            // 從活動報警列表中移除
            for (int i = activeAlarms.Count - 1; i >= 0; i--)
            {
                if (activeAlarms[i].Address == addressString)
                {
                    activeAlarms.RemoveAt(i);
                    break;
                }
            }

            // 記錄到日誌
            LogMessage($"[清除] {addressString}");
        }

        #endregion
    }
}