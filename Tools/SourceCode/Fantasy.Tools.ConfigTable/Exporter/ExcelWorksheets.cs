using Fantasy.Exporter;
using Fantasy.Helper;
using OfficeOpenXml;
#pragma warning disable CS8601 // Possible null reference assignment.

namespace Fantasy.Tools.ConfigTable;

public sealed class ExcelWorksheets(ExcelExporter excelExporter)
{
    public bool TryGetValue(string worksheetName, out ExcelWorksheet excelWorksheet)
    {
        if (excelExporter.Worksheets.TryGetValue(worksheetName, out excelWorksheet))
        {
            return true;
        }

        var computeHash64 = HashCodeHelper.ComputeHash64(worksheetName);
        if (!excelExporter.VersionInfo.WorksheetNames.Contains(computeHash64))
        {
            Log.Info($"{worksheetName} is not exist!");
        }
        
        return false;
    }
}