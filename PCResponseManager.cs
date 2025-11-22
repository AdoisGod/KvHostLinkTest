using System;
using System.Threading;
using System.Threading.Tasks;

namespace KVNC1EPTestApp
{
    /// <summary>
    /// PC自動回應管理器
    /// </summary>
    public class PCResponseManager
    {
        private KvHostlinkLib.KvHostlinkLib kvSockets;
        private bool[] previousTriggerStates;  // 存儲前一次的Trigger狀態
        private int responseDelayMs;           // 回應延遲時間（毫秒）
        private int[] responseCounts;          // 每組的回應計數

        public int[] ResponseCounts => responseCounts;
        public int TotalResponseCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < responseCounts.Length; i++)
                    total += responseCounts[i];
                return total;
            }
        }

        public PCResponseManager(KvHostlinkLib.KvHostlinkLib kvSockets)
        {
            this.kvSockets = kvSockets;
            this.previousTriggerStates = new bool[PCResponseConfig.ResponsePairs.Length];
            this.responseCounts = new int[PCResponseConfig.ResponsePairs.Length];
            this.responseDelayMs = 1;
        }

        /// <summary>
        /// 設置回應延遲時間
        /// </summary>
        public void SetResponseDelay(int delayMs)
        {
            this.responseDelayMs = Math.Max(1, delayMs);
        }

        /// <summary>
        /// 重置回應計數
        /// </summary>
        public void ResetCount()
        {
            for (int i = 0; i < responseCounts.Length; i++)
                responseCounts[i] = 0;
        }

        /// <summary>
        /// 監控並處理Trigger信號（在定時器中調用）
        /// </summary>
        public void MonitorTriggers(Action<string> logCallback)
        {
            for (int i = 0; i < PCResponseConfig.ResponsePairs.Length; i++)
            {
                var pair = PCResponseConfig.ResponsePairs[i];

                // 讀取Trigger狀態
                bool currentState = ReadTriggerState(pair.TriggerAddress);

                // 檢測上升沿（0→1）
                if (currentState && !previousTriggerStates[i])
                {
                    // 觸發回應（異步延遲），傳遞索引
                    TriggerResponse(i, pair, logCallback);
                }

                // 更新狀態
                previousTriggerStates[i] = currentState;
            }
        }

        /// <summary>
        /// 讀取Trigger狀態
        /// </summary>
        private bool ReadTriggerState(int mrAddress)
        {
            try
            {
                byte[] readData = new byte[2];
                int errCode = kvSockets.ReadDevices("MR", mrAddress, 1, ref readData);

                if (errCode == 0)
                {
                    return (readData[0] & 0x01) != 0;
                }
            }
            catch
            {
                // 讀取失敗，返回false
            }

            return false;
        }

        /// <summary>
        /// 觸發自動回應（異步延遲後寫入Response）
        /// </summary>
        private void TriggerResponse(int index, PCResponsePair pair, Action<string> logCallback)
        {
            logCallback?.Invoke($"[PC模擬] 檢測到 {pair.Name} Trigger (MR{pair.TriggerAddress})");

            // 異步延遲後發送Response
            Task.Run(async () =>
            {
                try
                {
                    // 延遲
                    if (responseDelayMs > 0)
                    {
                        await Task.Delay(responseDelayMs);
                    }

                    // 寫入Response = ON
                    byte[] writeData = new byte[2];
                    writeData[0] = 0x01;
                    int errCode = kvSockets.WriteDevices("MR", pair.ResponseAddress, 1, ref writeData);

                    if (errCode == 0)
                    {
                        Interlocked.Increment(ref responseCounts[index]);
                        int count = responseCounts[index];
                        logCallback?.Invoke($"[PC模擬] 已回應 {pair.Name} Response (MR{pair.ResponseAddress}) - 本組計數:{count}");
                    }
                    else
                    {
                        logCallback?.Invoke($"[PC模擬] 回應失敗: {kvSockets.ErrMsg(errCode)}");
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"[PC模擬] 回應異常: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 重置所有Trigger狀態（用於啟動監控時）
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < previousTriggerStates.Length; i++)
            {
                previousTriggerStates[i] = false;
            }
        }
    }
}
