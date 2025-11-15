using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KVNC1EPTestApp
{
    /// <summary>
    /// 報警詳細信息類
    /// </summary>
    public class AlarmDetail
    {
        public string Name { get; set; }        // 異常名稱 (CSV C列)
        public string Details { get; set; }     // 詳細內容 (CSV D-J列合併)
    }

    /// <summary>
    /// 報警配置類，從 ERRORCODE.csv 文件讀取R位地址與描述的映射關係
    /// </summary>
    public static class AlarmConfig
    {
        private static Dictionary<int, AlarmDetail> alarmDictionary = new Dictionary<int, AlarmDetail>();
        private static bool isLoaded = false;
        private static string lastError = "";

        /// <summary>
        /// 從 ERRORCODE.csv 加載報警配置
        /// CSV 格式：A列=點位(R000-R9915), C列=異常名稱, D-J列=內容說明
        /// </summary>
        public static bool LoadFromCSV(string csvFilePath = "ERRORCODE.csv")
        {
            alarmDictionary.Clear();
            isLoaded = false;
            lastError = "";

            try
            {
                if (!File.Exists(csvFilePath))
                {
                    lastError = $"找不到檔案: {csvFilePath}";
                    return false;
                }

                // 使用 UTF-8 編碼讀取 CSV
                string[] lines = File.ReadAllLines(csvFilePath, Encoding.UTF8);

                if (lines.Length <= 1)
                {
                    lastError = "CSV 檔案內容為空";
                    return false;
                }

                // 從第二行開始讀取（跳過標題行）
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    // 分割 CSV 行
                    string[] columns = ParseCSVLine(line);

                    if (columns.Length < 3)
                        continue;

                    // A 列：點位 (例如 "R000")
                    string addressStr = columns[0].Trim().ToUpper();
                    if (!addressStr.StartsWith("R"))
                        continue;

                    // 解析地址編號
                    if (!int.TryParse(addressStr.Substring(1), out int address))
                        continue;

                    // C 列（索引2）：異常名稱
                    string name = columns.Length > 2 ? columns[2].Trim() : "";

                    // D-J 列（索引3-9）：內容說明
                    List<string> detailParts = new List<string>();
                    for (int col = 3; col < Math.Min(columns.Length, 10); col++)
                    {
                        string part = columns[col].Trim();
                        if (!string.IsNullOrEmpty(part))
                        {
                            detailParts.Add(part);
                        }
                    }

                    string details = string.Join(" ", detailParts);

                    // 如果名稱和內容都為空，跳過
                    if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(details))
                        continue;

                    // 添加到字典
                    alarmDictionary[address] = new AlarmDetail
                    {
                        Name = name,
                        Details = details
                    };
                }

                isLoaded = true;
                return true;
            }
            catch (Exception ex)
            {
                lastError = $"讀取CSV失敗: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 解析 CSV 行（處理逗號分隔和引號包圍的字段）
        /// </summary>
        private static string[] ParseCSVLine(string line)
        {
            List<string> fields = new List<string>();
            bool inQuotes = false;
            StringBuilder currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            fields.Add(currentField.ToString());
            return fields.ToArray();
        }

        /// <summary>
        /// 獲取指定R地址的報警詳細信息
        /// </summary>
        public static AlarmDetail GetAlarmDetail(int address)
        {
            if (alarmDictionary.ContainsKey(address))
            {
                return alarmDictionary[address];
            }

            // 返回默認值
            return new AlarmDetail
            {
                Name = $"R{address:D4}",
                Details = "未定義描述"
            };
        }

        /// <summary>
        /// 檢查配置是否已加載
        /// </summary>
        public static bool IsLoaded => isLoaded;

        /// <summary>
        /// 獲取最後的錯誤信息
        /// </summary>
        public static string LastError => lastError;

        /// <summary>
        /// 獲取已加載的報警配置數量
        /// </summary>
        public static int Count => alarmDictionary.Count;
    }
}
