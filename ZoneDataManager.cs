using System;
using System.Collections.Generic;

namespace KVNC1EPTestApp
{
    /// <summary>
    /// 字節序枚舉
    /// </summary>
    public enum EndianMode
    {
        LittleEndian,   // 低位在前 (DM5000=低16位, DM5001=高16位)
        BigEndian       // 高位在前 (DM5000=高16位, DM5001=低16位)
    }

    /// <summary>
    /// Zone數據管理類 - 負責讀取Zone數據
    /// </summary>
    public class ZoneDataManager
    {
        private KvHostlinkLib.KvHostlinkLib kvSockets;
        private EndianMode endianMode;

        public ZoneDataManager(KvHostlinkLib.KvHostlinkLib kvSockets, EndianMode endianMode = EndianMode.LittleEndian)
        {
            this.kvSockets = kvSockets;
            this.endianMode = endianMode;
        }

        /// <summary>
        /// 設置字節序模式
        /// </summary>
        public void SetEndianMode(EndianMode mode)
        {
            this.endianMode = mode;
        }

        /// <summary>
        /// 讀取指定Zone的所有數據
        /// </summary>
        /// <param name="zoneInfo">Zone配置信息</param>
        /// <param name="errorMessage">錯誤信息（如果有）</param>
        /// <returns>32位帶符號整數數組，失敗返回null</returns>
        public int[] ReadZoneData(ZoneInfo zoneInfo, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                // 計算需要讀取的DM數量 (每個32位數據需要2個DM)
                int totalDMs = zoneInfo.DataCount * 2;
                byte[] readData = new byte[totalDMs * 2];  // 每個DM是2字節

                // 讀取DM數據
                int errCode = kvSockets.ReadDevices("DM", zoneInfo.DataStartAddress, readData.Length, ref readData);

                if (errCode != 0)
                {
                    errorMessage = $"讀取失敗: {kvSockets.ErrMsg(errCode)}";
                    return null;
                }

                // 將字節數組轉換為32位整數數組
                return ConvertBytesTo32BitIntegers(readData, zoneInfo.DataCount);
            }
            catch (Exception ex)
            {
                errorMessage = $"讀取異常: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// 將字節數組轉換為32位帶符號整數數組
        /// </summary>
        private int[] ConvertBytesTo32BitIntegers(byte[] data, int count)
        {
            int[] result = new int[count];

            for (int i = 0; i < count; i++)
            {
                int byteIndex = i * 4;  // 每個32位數據占4字節

                int value;
                if (endianMode == EndianMode.LittleEndian)
                {
                    // Little Endian: 低字節在前
                    // DM[i*2] = 低16位, DM[i*2+1] = 高16位
                    ushort lowWord = (ushort)(data[byteIndex] | (data[byteIndex + 1] << 8));
                    ushort highWord = (ushort)(data[byteIndex + 2] | (data[byteIndex + 3] << 8));
                    value = (int)((highWord << 16) | lowWord);
                }
                else
                {
                    // Big Endian: 高字節在前
                    // DM[i*2] = 高16位, DM[i*2+1] = 低16位
                    ushort highWord = (ushort)(data[byteIndex] | (data[byteIndex + 1] << 8));
                    ushort lowWord = (ushort)(data[byteIndex + 2] | (data[byteIndex + 3] << 8));
                    value = (int)((highWord << 16) | lowWord);
                }

                result[i] = value;
            }

            return result;
        }

        /// <summary>
        /// 檢查Zone就緒旗標
        /// </summary>
        public bool CheckReadyFlag(int mrAddress, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                byte[] readData = new byte[1];
                int errCode = kvSockets.ReadDevices("MR", mrAddress, 1, ref readData);

                if (errCode != 0)
                {
                    errorMessage = $"讀取MR{mrAddress}失敗: {kvSockets.ErrMsg(errCode)}";
                    return false;
                }

                return (readData[0] & 0x01) != 0;
            }
            catch (Exception ex)
            {
                errorMessage = $"檢查旗標異常: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 設置Zone完成旗標
        /// </summary>
        public bool SetCompleteFlag(int mrAddress, bool value, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                byte[] writeData = new byte[1];
                if (value)
                {
                    writeData[0] = 0x01;
                }

                int errCode = kvSockets.WriteDevices("MR", mrAddress, 1, ref writeData);

                if (errCode != 0)
                {
                    errorMessage = $"寫入MR{mrAddress}失敗: {kvSockets.ErrMsg(errCode)}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"設置旗標異常: {ex.Message}";
                return false;
            }
        }
    }
}
