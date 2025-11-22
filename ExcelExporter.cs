using System;
using System.IO;
using System.Text;

namespace KVNC1EPTestApp
{
    /// <summary>
    /// Excel導出類 - 導出Zone數據到Excel文件
    /// 使用CSV格式（可用Excel打開），不需要額外的庫
    /// </summary>
    public static class ExcelExporter
    {
        /// <summary>
        /// 導出Zone數據到CSV/Excel文件
        /// </summary>
        /// <param name="zoneInfo">Zone配置信息</param>
        /// <param name="data">32位數據數組</param>
        /// <param name="savePath">保存路徑</param>
        /// <param name="timestamp">時間戳（用於文件名）</param>
        /// <returns>成功返回文件完整路徑，失敗返回null</returns>
        public static string ExportToExcel(ZoneInfo zoneInfo, int[] data, string savePath, DateTime timestamp)
        {
            try
            {
                // 生成文件名: Zone1_20250122_143052.csv
                string fileName = $"{zoneInfo.Name}_{timestamp:yyyyMMdd_HHmmss}.csv";
                string fullPath = Path.Combine(savePath, fileName);

                // 確保目錄存在
                Directory.CreateDirectory(savePath);

                // 使用UTF-8編碼（帶BOM，Excel才能正確識別中文）
                using (StreamWriter writer = new StreamWriter(fullPath, false, new UTF8Encoding(true)))
                {
                    // 寫入標題行
                    writer.WriteLine("索引,數值");

                    // 寫入數據
                    for (int i = 0; i < data.Length; i++)
                    {
                        writer.WriteLine($"{i},{data[i]}");
                    }
                }

                return fullPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"導出Excel失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 導出Zone數據到真正的Excel文件（需要EPPlus庫）
        /// 這個方法在EPPlus可用時提供更好的格式
        /// </summary>
        public static string ExportToExcelWithEPPlus(ZoneInfo zoneInfo, int[] data, string savePath, DateTime timestamp)
        {
            try
            {
                // 嘗試使用EPPlus（如果可用）
                Type epPlusType = Type.GetType("OfficeOpenXml.ExcelPackage, EPPlus");
                if (epPlusType == null)
                {
                    // EPPlus不可用，回退到CSV格式
                    return ExportToExcel(zoneInfo, data, savePath, timestamp);
                }

                // 使用反射調用EPPlus（避免直接引用）
                return ExportWithEPPlusReflection(zoneInfo, data, savePath, timestamp);
            }
            catch
            {
                // 如果EPPlus方法失敗，回退到CSV
                return ExportToExcel(zoneInfo, data, savePath, timestamp);
            }
        }

        /// <summary>
        /// 使用反射調用EPPlus導出Excel
        /// </summary>
        private static string ExportWithEPPlusReflection(ZoneInfo zoneInfo, int[] data, string savePath, DateTime timestamp)
        {
            string fileName = $"{zoneInfo.Name}_{timestamp:yyyyMMdd_HHmmss}.xlsx";
            string fullPath = Path.Combine(savePath, fileName);

            Directory.CreateDirectory(savePath);

            // 使用反射創建ExcelPackage
            var epPlusAssembly = System.Reflection.Assembly.Load("EPPlus");
            var packageType = epPlusAssembly.GetType("OfficeOpenXml.ExcelPackage");
            var package = Activator.CreateInstance(packageType);

            try
            {
                // 獲取Workbook屬性
                var workbookProperty = packageType.GetProperty("Workbook");
                var workbook = workbookProperty.GetValue(package);

                // 獲取Worksheets屬性
                var worksheetsProperty = workbook.GetType().GetProperty("Worksheets");
                var worksheets = worksheetsProperty.GetValue(workbook);

                // 添加工作表
                var addMethod = worksheets.GetType().GetMethod("Add", new Type[] { typeof(string) });
                var worksheet = addMethod.Invoke(worksheets, new object[] { zoneInfo.Name });

                // 獲取Cells屬性
                var cellsProperty = worksheet.GetType().GetProperty("Cells");
                var cells = cellsProperty.GetValue(worksheet);

                // 寫入標題
                SetCellValue(cells, 1, 1, "索引");
                SetCellValue(cells, 1, 2, "數值");

                // 寫入數據
                for (int i = 0; i < data.Length; i++)
                {
                    SetCellValue(cells, i + 2, 1, i);
                    SetCellValue(cells, i + 2, 2, data[i]);
                }

                // 保存文件
                var fileInfo = new FileInfo(fullPath);
                var saveMethod = packageType.GetMethod("SaveAs", new Type[] { typeof(FileInfo) });
                saveMethod.Invoke(package, new object[] { fileInfo });

                return fullPath;
            }
            finally
            {
                // 釋放資源
                if (package is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// 使用反射設置單元格值
        /// </summary>
        private static void SetCellValue(object cells, int row, int col, object value)
        {
            var indexer = cells.GetType().GetProperty("Item", new Type[] { typeof(int), typeof(int) });
            var cell = indexer.GetValue(cells, new object[] { row, col });
            var valueProperty = cell.GetType().GetProperty("Value");
            valueProperty.SetValue(cell, value);
        }
    }
}
