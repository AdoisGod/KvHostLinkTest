using System;

namespace KVNC1EPTestApp
{
    /// <summary>
    /// PC回應配置信息類
    /// </summary>
    public class PCResponsePair
    {
        public int TriggerAddress { get; set; }      // Trigger MR地址 (PLC→PC)
        public int ResponseAddress { get; set; }     // Response MR地址 (PC→PLC)
        public string Name { get; set; }             // 功能名稱
        public string Sequence { get; set; }         // 序列號

        public override string ToString()
        {
            return $"{Name} ({Sequence})";
        }
    }

    /// <summary>
    /// PC自動回應配置類
    /// </summary>
    public static class PCResponseConfig
    {
        /// <summary>
        /// 所有Trigger/Response對應配置
        /// </summary>
        public static readonly PCResponsePair[] ResponsePairs = new PCResponsePair[]
        {
            new PCResponsePair
            {
                TriggerAddress = 1100,
                ResponseAddress = 1101,
                Name = "左手把拍照",
                Sequence = "SEQ-101"
            },
            new PCResponsePair
            {
                TriggerAddress = 1104,
                ResponseAddress = 1105,
                Name = "右手把拍照",
                Sequence = "SEQ-102"
            },
            new PCResponsePair
            {
                TriggerAddress = 1108,
                ResponseAddress = 1109,
                Name = "門板鑰匙孔拍照",
                Sequence = "SEQ-103"
            },
            new PCResponsePair
            {
                TriggerAddress = 1112,
                ResponseAddress = 1113,
                Name = "氣閥拍照",
                Sequence = "SEQ-104"
            },
            new PCResponsePair
            {
                TriggerAddress = 1204,
                ResponseAddress = 1205,
                Name = "Slot拍照請求",
                Sequence = "SEQ-201"
            },
            new PCResponsePair
            {
                TriggerAddress = 1206,
                ResponseAddress = 1207,
                Name = "Slot掃描完成",
                Sequence = "SEQ-201"
            },
            new PCResponsePair
            {
                TriggerAddress = 1212,
                ResponseAddress = 1213,
                Name = "Foup內部拍照",
                Sequence = "SEQ-202"
            },
            new PCResponsePair
            {
                TriggerAddress = 1300,
                ResponseAddress = 1301,
                Name = "膠條拍照",
                Sequence = "SEQ-303"
            },
            new PCResponsePair
            {
                TriggerAddress = 1304,
                ResponseAddress = 1305,
                Name = "Latch拍照",
                Sequence = "SEQ-304"
            }
        };
    }
}
