using System.Collections.Generic;

namespace KVNC1EPTestApp
{
    /// <summary>
    /// 報警配置類，定義R位地址與描述的映射關係
    /// </summary>
    public static class AlarmConfig
    {
        /// <summary>
        /// R位地址與描述的映射字典
        /// 用戶可以在這裡配置每個R位對應的報警描述
        /// </summary>
        public static Dictionary<int, string> RAddressDescriptions = new Dictionary<int, string>()
        {
            // 示例配置 - 用戶可以根據實際需求修改
            { 0, "緊急停止按鈕被按下" },
            { 1, "安全門打開" },
            { 2, "溫度過高報警" },
            { 3, "壓力異常" },
            { 4, "馬達過載" },
            { 5, "感測器故障" },
            { 10, "原料不足" },
            { 11, "產品滿載" },
            { 12, "傳送帶停止" },
            { 13, "氣壓不足" },
            { 14, "液壓油位低" },
            { 15, "冷卻水溫度異常" },
            { 20, "伺服馬達報警" },
            { 21, "變頻器故障" },
            { 22, "編碼器錯誤" },
            { 23, "定位超時" },
            { 24, "速度超限" },
            { 25, "扭矩過大" },
            { 100, "系統通訊異常" },
            { 101, "PLC內部錯誤" },
            { 102, "程序運行異常" },
            { 103, "記憶體錯誤" },
            { 200, "工件定位錯誤" },
            { 201, "夾具未夾緊" },
            { 202, "刀具磨損" },
            { 203, "加工尺寸超差" },
            { 9915, "系統最後一個監控位" }

            // 用戶可以繼續添加更多的映射關係
            // 格式: { R地址編號, "報警描述" }
        };

        /// <summary>
        /// 獲取指定R地址的描述
        /// </summary>
        /// <param name="address">R地址編號</param>
        /// <returns>描述文字，如果未配置則返回默認描述</returns>
        public static string GetDescription(int address)
        {
            if (RAddressDescriptions.ContainsKey(address))
            {
                return RAddressDescriptions[address];
            }
            return $"R{address:D4} 未定義描述";
        }
    }
}
