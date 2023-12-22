using OfficeOpenXml;

namespace Exporter.Excel;

public sealed class ExcelWorksheets
{
    private readonly ExcelExporter _excelExporter;
    private readonly ExcelWorksheet _defaultExcelWorksheet;
    public ExcelWorksheets(ExcelExporter excelExporter)
    {
        this._excelExporter = excelExporter;
        _defaultExcelWorksheet = new ExcelPackage().Workbook.Worksheets.Add("VersionInfo");
        _defaultExcelWorksheet.Cells["A1"].Value = "VersionInfo";
    }
    
    public bool TryGetValue(string worksheetName, out ExcelWorksheet excelWorksheet)
    {
        if (_excelExporter.Worksheets.TryGetValue(worksheetName, out excelWorksheet))
        {
            return true;
        }

        if (!_excelExporter.VersionInfo.WorksheetNames.Contains(worksheetName))
        {
            return false;
        }
        
        excelWorksheet = _defaultExcelWorksheet;
        return true;

    }
}