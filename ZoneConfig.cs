using System;

namespace KVNC1EPTestApp
{
    /// <summary>
    /// Zone配置信息類
    /// </summary>
    public class ZoneInfo
    {
        public int ZoneNumber { get; set; }             // Zone編號 (1-4)
        public int DataStartAddress { get; set; }       // 數據區起始地址 (DM)
        public int DataEndAddress { get; set; }         // 數據區結束地址 (DM)
        public int ReadyFlagAddress { get; set; }       // 就緒旗標地址 (MR100X)
        public int CompleteFlagAddress { get; set; }    // 完成旗標地址 (MR100X+1)
        public int DataCount { get; set; }              // 數據個數 (32位數據個數)

        public string Name => $"Zone{ZoneNumber}";
    }

    /// <summary>
    /// Zone配置類 - 定義4個Zone的記憶體配置
    /// </summary>
    public static class ZoneConfig
    {
        /// <summary>
        /// 所有Zone的配置信息
        /// </summary>
        public static readonly ZoneInfo[] Zones = new ZoneInfo[]
        {
            new ZoneInfo
            {
                ZoneNumber = 1,
                DataStartAddress = 5000,
                DataEndAddress = 5299,
                ReadyFlagAddress = 1000,
                CompleteFlagAddress = 1001,
                DataCount = 150  // 300個DM = 150個32位數據
            },
            new ZoneInfo
            {
                ZoneNumber = 2,
                DataStartAddress = 5300,
                DataEndAddress = 5599,
                ReadyFlagAddress = 1002,
                CompleteFlagAddress = 1003,
                DataCount = 150
            },
            new ZoneInfo
            {
                ZoneNumber = 3,
                DataStartAddress = 5600,
                DataEndAddress = 5899,
                ReadyFlagAddress = 1004,
                CompleteFlagAddress = 1005,
                DataCount = 150
            },
            new ZoneInfo
            {
                ZoneNumber = 4,
                DataStartAddress = 5900,
                DataEndAddress = 6199,
                ReadyFlagAddress = 1006,
                CompleteFlagAddress = 1007,
                DataCount = 150
            }
        };

        /// <summary>
        /// 根據Zone編號獲取配置
        /// </summary>
        public static ZoneInfo GetZoneInfo(int zoneNumber)
        {
            if (zoneNumber >= 1 && zoneNumber <= 4)
            {
                return Zones[zoneNumber - 1];
            }
            return null;
        }

        /// <summary>
        /// 根據就緒旗標地址獲取Zone信息
        /// </summary>
        public static ZoneInfo GetZoneByReadyFlag(int mrAddress)
        {
            foreach (var zone in Zones)
            {
                if (zone.ReadyFlagAddress == mrAddress)
                {
                    return zone;
                }
            }
            return null;
        }
    }
}
