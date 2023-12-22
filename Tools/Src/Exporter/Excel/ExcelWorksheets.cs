using Fantasy.Exporter;
using OfficeOpenXml;

namespace Exporter.Excel;

public sealed class ExcelWorksheets(ExcelExporter excelExporter)
{
    public bool TryGetValue(string worksheetName, out ExcelWorksheet excelWorksheet)
    {
        if (excelExporter.Worksheets.TryGetValue(worksheetName, out excelWorksheet))
        {
            return true;
        }

        if (!excelExporter.VersionInfo.WorksheetNames.Contains(worksheetName))
        {
            Log.Info($"{worksheetName} is not exist!");
        }
        
        return false;
    }
}