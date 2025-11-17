using System;

namespace KVNC1EPTestApp
{
    /// <summary>
    /// R地址转换工具类
    /// R地址规则：后两位是16进制(00-15)，前面是10进制(0-99)
    /// 例如：R000-R015, R100-R115, R200-R215, ..., R9900-R9915
    /// 总共1600个地址 (100 × 16)
    /// </summary>
    public static class RAddressHelper
    {
        /// <summary>
        /// 将R地址字符串转换为线性索引 (0-1599)
        /// </summary>
        /// <param name="rAddress">R地址字符串，例如 "R000", "R115", "R9915"</param>
        /// <returns>线性索引，失败返回-1</returns>
        public static int RAddressToIndex(string rAddress)
        {
            if (string.IsNullOrEmpty(rAddress) || !rAddress.StartsWith("R"))
                return -1;

            if (!int.TryParse(rAddress.Substring(1), out int addressNum))
                return -1;

            return RAddressNumToIndex(addressNum);
        }

        /// <summary>
        /// 将R地址数字转换为线性索引
        /// </summary>
        /// <param name="addressNum">R地址数字部分，例如 0, 115, 9915</param>
        /// <returns>线性索引 (0-1599)，失败返回-1</returns>
        public static int RAddressNumToIndex(int addressNum)
        {
            // 分解地址：前面是百位数(10进制)，后两位是个位数(16进制)
            int hundreds = addressNum / 100;  // 前面的部分 (0-99)
            int lastTwo = addressNum % 100;   // 后两位 (00-99)

            // 后两位必须在 00-15 范围内（16进制的0-F）
            if (lastTwo > 15)
                return -1;

            // 前面部分必须在 0-99 范围内
            if (hundreds < 0 || hundreds > 99)
                return -1;

            // 计算线性索引：每组16个地址
            return hundreds * 16 + lastTwo;
        }

        /// <summary>
        /// 将线性索引转换为R地址数字
        /// </summary>
        /// <param name="index">线性索引 (0-1599)</param>
        /// <returns>R地址数字部分</returns>
        public static int IndexToRAddressNum(int index)
        {
            if (index < 0 || index >= 1600)
                return -1;

            int hundreds = index / 16;  // 百位数部分
            int lastTwo = index % 16;   // 后两位（16进制）

            return hundreds * 100 + lastTwo;
        }

        /// <summary>
        /// 将线性索引转换为R地址字符串
        /// </summary>
        /// <param name="index">线性索引 (0-1599)</param>
        /// <returns>R地址字符串，例如 "R0000", "R0115"</returns>
        public static string IndexToRAddress(int index)
        {
            int addressNum = IndexToRAddressNum(index);
            if (addressNum < 0)
                return null;

            return $"R{addressNum:D4}";
        }

        /// <summary>
        /// 验证R地址是否有效
        /// </summary>
        public static bool IsValidRAddress(int addressNum)
        {
            return RAddressNumToIndex(addressNum) >= 0;
        }

        /// <summary>
        /// 获取总的R地址数量
        /// </summary>
        public const int TotalRAddresses = 1600;  // 100 × 16
    }
}
