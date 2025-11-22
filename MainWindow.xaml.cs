using System;
using System.Collections.ObjectModel;
using System.IO;
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

        // Zone監控相關成員變量
        private ZoneDataManager zoneDataManager;
        private bool[] previousZoneReadyStates;  // 存儲Zone就緒旗標的前一次狀態
        private string zoneSavePath = "";

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
            string csvPath = System.IO.Path.GetFullPath("ERRORCODE.csv");
            LogMessage($"正在讀取配置文件：{csvPath}");

            if (AlarmConfig.LoadFromCSV("ERRORCODE.csv"))
            {
                CSVStatusTextBlock.Text = $"已加載 {AlarmConfig.Count} 筆配置";
                CSVStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                LogMessage($"✓ 成功加載 CSV 配置：{AlarmConfig.Count} 筆資料");
            }
            else
            {
                CSVStatusTextBlock.Text = $"未加載 ({AlarmConfig.LastError})";
                CSVStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                LogMessage($"✗ CSV 配置加載失敗：{AlarmConfig.LastError}");
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

                // 驗證R地址是否有效
                if (!RAddressHelper.IsValidRAddress(monitorStartAddress))
                {
                    LogMessage($"起始地址 R{monitorStartAddress:D4} 無效（後兩位必須在00-15之間）");
                    return;
                }

                if (!RAddressHelper.IsValidRAddress(monitorEndAddress))
                {
                    LogMessage($"結束地址 R{monitorEndAddress:D4} 無效（後兩位必須在00-15之間）");
                    return;
                }

                // 轉換為線性索引
                int startIndex = RAddressHelper.RAddressNumToIndex(monitorStartAddress);
                int endIndex = RAddressHelper.RAddressNumToIndex(monitorEndAddress);

                if (startIndex < 0 || endIndex < startIndex)
                {
                    LogMessage("監控地址範圍無效");
                    return;
                }

                if (interval < 100)
                {
                    LogMessage("掃描間隔不能小於100ms");
                    return;
                }

                // 初始化狀態緩存（基於線性索引範圍）
                int totalBits = endIndex - startIndex + 1;
                previousStates = new bool[totalBits];

                // 啟動定時器
                monitorTimer.Interval = TimeSpan.FromMilliseconds(interval);
                monitorTimer.Start();

                // 啟動Zone監控（如果路徑已設置）
                InitializeZoneMonitoring();

                // 更新UI狀態
                StartMonitorButton.IsEnabled = false;
                StopMonitorButton.IsEnabled = true;
                MonitorStartAddressTextBox.IsEnabled = false;
                MonitorEndAddressTextBox.IsEnabled = false;
                MonitorIntervalTextBox.IsEnabled = false;

                LogMessage($"開始監控 R{monitorStartAddress:D4} 到 R{monitorEndAddress:D4}，共 {totalBits} 個位址，間隔 {interval}ms");
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

            // 停止Zone監控
            StopZoneMonitoring();

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
                MonitorZoneFlags();  // 同時監控Zone旗標
            }
            catch (Exception ex)
            {
                LogMessage($"監控掃描異常: {ex.Message}");
            }
        }

        /// <summary>
        /// 執行監控掃描
        /// R地址規則：後兩位是16進制(00-15)，需要分組讀取
        /// 例如：R000-R015是一組，R100-R115是一組
        /// </summary>
        private void PerformMonitorScan()
        {
            // 轉換為線性索引
            int startIndex = RAddressHelper.RAddressNumToIndex(monitorStartAddress);
            int endIndex = RAddressHelper.RAddressNumToIndex(monitorEndAddress);

            int currentStateIndex = 0;  // 當前狀態數組的索引

            // 按組掃描（每組16個R位）
            int startGroup = startIndex / 16;  // 起始組號
            int endGroup = endIndex / 16;      // 結束組號

            for (int group = startGroup; group <= endGroup; group++)
            {
                // 計算這一組的起始和結束R地址
                int groupStartIndex = group * 16;
                int groupEndIndex = Math.Min(groupStartIndex + 15, endIndex);

                // 如果起始組，可能不是從組的開頭開始
                if (group == startGroup)
                    groupStartIndex = startIndex;

                // 計算這一組要讀取的R位數量
                int bitsInGroup = groupEndIndex - groupStartIndex + 1;

                // 轉換索引為R地址
                int groupStartAddr = RAddressHelper.IndexToRAddressNum(groupStartIndex);
                int groupEndAddr = RAddressHelper.IndexToRAddressNum(groupEndIndex);

                // 讀取這一組的數據
                byte[] readData = new byte[(bitsInGroup + 7) / 8];
                int errCode = kvSockets.ReadDevices("R", groupStartAddr, readData.Length, ref readData);

                if (errCode != 0)
                {
                    LogMessage($"監控讀取錯誤 R{groupStartAddr:D4}-R{groupEndAddr:D4}: {kvSockets.ErrMsg(errCode)}");
                    continue;
                }

                // 檢測這一組的狀態變化
                DetectStateChangesInGroup(readData, bitsInGroup, groupStartIndex, ref currentStateIndex);
            }
        }

        /// <summary>
        /// 檢測一組內的狀態變化（上升沿/下降沿檢測）
        /// </summary>
        /// <param name="currentData">當前讀取的數據</param>
        /// <param name="bitsInGroup">這一組的位數</param>
        /// <param name="groupStartIndex">這一組的起始線性索引</param>
        /// <param name="stateArrayIndex">狀態數組的當前索引（引用傳遞，會自動遞增）</param>
        private void DetectStateChangesInGroup(byte[] currentData, int bitsInGroup, int groupStartIndex, ref int stateArrayIndex)
        {
            for (int i = 0; i < bitsInGroup; i++)
            {
                // 讀取當前位狀態
                bool currentState = (currentData[i / 8] & (1 << (i % 8))) != 0;
                bool previousState = previousStates[stateArrayIndex];

                // 計算實際的R地址
                int currentIndex = groupStartIndex + i;
                int rAddress = RAddressHelper.IndexToRAddressNum(currentIndex);

                // 檢測上升沿（0->1）
                if (currentState && !previousState)
                {
                    // 狀態從0變為1，觸發報警
                    OnAlarmTriggered(rAddress);
                }
                // 檢測下降沿（1->0）
                else if (!currentState && previousState)
                {
                    // 狀態從1變為0，移除報警
                    OnAlarmCleared(rAddress);
                }

                // 更新狀態緩存
                previousStates[stateArrayIndex] = currentState;
                stateArrayIndex++;
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

        #region Zone數據監控功能

        /// <summary>
        /// 選擇路徑按鈕點擊事件
        /// </summary>
        private void SelectPathButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "選擇Excel文件保存路徑";
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(zoneSavePath))
                {
                    dialog.SelectedPath = zoneSavePath;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    zoneSavePath = dialog.SelectedPath;
                    ZoneSavePathTextBox.Text = zoneSavePath;
                    LogMessage($"已設置Excel保存路徑: {zoneSavePath}");
                }
            }
        }

        /// <summary>
        /// 初始化Zone監控（在監控啟動時調用）
        /// </summary>
        private void InitializeZoneMonitoring()
        {
            if (string.IsNullOrEmpty(zoneSavePath))
            {
                return; // 路徑未設置，不啟動Zone監控
            }

            // 初始化Zone數據管理器
            EndianMode mode = EndianModeCheckBox.IsChecked == true ? EndianMode.BigEndian : EndianMode.LittleEndian;
            zoneDataManager = new ZoneDataManager(kvSockets, mode);

            // 初始化Zone就緒旗標狀態緩存（4個Zone）
            previousZoneReadyStates = new bool[4];

            ZoneStatusTextBlock.Text = "監控中";
            ZoneStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            LogMessage("Zone數據監控已啟動");
        }

        /// <summary>
        /// 監控Zone就緒旗標（在定時器Tick中調用）
        /// </summary>
        private void MonitorZoneFlags()
        {
            if (zoneDataManager == null || string.IsNullOrEmpty(zoneSavePath))
            {
                return;
            }

            // 檢查每個Zone的就緒旗標
            for (int i = 0; i < ZoneConfig.Zones.Length; i++)
            {
                var zone = ZoneConfig.Zones[i];

                // 檢查就緒旗標
                bool currentState = zoneDataManager.CheckReadyFlag(zone.ReadyFlagAddress, out string error);

                if (!string.IsNullOrEmpty(error))
                {
                    // 讀取錯誤，記錄日誌
                    continue;
                }

                // 檢測上升沿（0->1）
                if (currentState && !previousZoneReadyStates[i])
                {
                    // Zone就緒旗標變為ON，開始讀取數據
                    OnZoneDataReady(zone);
                }

                // 更新狀態
                previousZoneReadyStates[i] = currentState;
            }
        }

        /// <summary>
        /// Zone數據就緒事件處理
        /// </summary>
        private void OnZoneDataReady(ZoneInfo zone)
        {
            LogMessage($"檢測到 {zone.Name} 就緒旗標 (MR{zone.ReadyFlagAddress})");

            try
            {
                // 讀取Zone數據
                int[] data = zoneDataManager.ReadZoneData(zone, out string readError);

                if (data == null)
                {
                    LogMessage($"讀取 {zone.Name} 數據失敗: {readError}");
                    return;
                }

                LogMessage($"成功讀取 {zone.Name} 數據，共 {data.Length} 個值");

                // 導出到Excel
                DateTime timestamp = DateTime.Now;
                string filePath = ExcelExporter.ExportToExcel(zone, data, zoneSavePath, timestamp);

                LogMessage($"Excel文件已保存: {filePath}");

                // 設置完成旗標
                bool setResult = zoneDataManager.SetCompleteFlag(zone.CompleteFlagAddress, true, out string setError);

                if (!setResult)
                {
                    LogMessage($"設置 {zone.Name} 完成旗標失敗: {setError}");
                }
                else
                {
                    LogMessage($"已設置 {zone.Name} 完成旗標 (MR{zone.CompleteFlagAddress})");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"處理 {zone.Name} 數據時發生異常: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止Zone監控
        /// </summary>
        private void StopZoneMonitoring()
        {
            if (zoneDataManager != null)
            {
                zoneDataManager = null;
                previousZoneReadyStates = null;
                ZoneStatusTextBlock.Text = "未啟動";
                ZoneStatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                LogMessage("Zone數據監控已停止");
            }
        }

        #endregion
    }
}