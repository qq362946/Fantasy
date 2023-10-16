using OfficeOpenXml;

namespace Exporter.Excel;

/// <summary>
/// 提供操作 Excel 文件的辅助方法。
/// </summary>
public static class ExcelHelper
{
    /// <summary>
    /// 加载 Excel 文件并返回 ExcelPackage 实例。
    /// </summary>
    /// <param name="name">Excel 文件的路径。</param>
    /// <returns>ExcelPackage 实例。</returns>
    public static ExcelPackage LoadExcel(string name)
    {
        return new ExcelPackage(name);
    }

    /// <summary>
    /// 获取指定工作表中指定行列位置的单元格值。
    /// </summary>
    /// <param name="sheet">Excel 工作表。</param>
    /// <param name="row">行索引。</param>
    /// <param name="column">列索引。</param>
    /// <returns>单元格值。</returns>
    public static string GetCellValue(this ExcelWorksheet sheet, int row, int column)
    {
        ExcelRange cell = sheet.Cells[row, column];
            
        try
        {
            if (cell.Value == null)
            {
                return "";
            }

            string s = cell.GetValue<string>();
                
            return s.Trim();
        }
        catch (Exception e)
        {
            throw new Exception($"Rows {row} Columns {column} Content {cell.Text} {e}");
        }
    }
}