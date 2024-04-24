// using System;
// using System.Text;
// using Exporter.Excel;
// using Fantasy.Exporter;
// using OfficeOpenXml;
//
// namespace Exporter;
//
// public class ConstValueToConst : ACustomExport
// {
//     public override void Run()
//     {
//         if (!ExcelExporter.LoadIgnoreExcel("#ConstValue", out var excelPackage))
//         {
//             Log.Error("ConstValueToConst: Load Excel failed.");
//             return;
//         }
//         
//         var worksheet = excelPackage.Workbook.Worksheets[0];
//         
//         var serverHotfixStrBuilder = new StringBuilder();
//         serverHotfixStrBuilder.AppendLine("namespace Fantasy\n{");
//         serverHotfixStrBuilder.AppendLine("\t// 生成器自动生成，请不要手动编辑,修改请在#ConstValue.xsl里。");
//         serverHotfixStrBuilder.AppendLine("\tpublic partial class ConstValueHotfix\n\t{");
//         
//         var serverModelStrBuilder = new StringBuilder();
//         serverModelStrBuilder.AppendLine("namespace Fantasy\n{");
//         serverModelStrBuilder.AppendLine("\t// 生成器自动生成，请不要手动编辑。");
//         serverModelStrBuilder.AppendLine("\tpublic partial class ConstValue\n\t{");
//
//         var clientStrBuilder = new StringBuilder();
//         clientStrBuilder.AppendLine("namespace Fantasy\n{");
//         clientStrBuilder.AppendLine("\t// 生成器自动生成，请不要手动编辑。");
//         clientStrBuilder.AppendLine("\tpublic class ConstValue\n\t{");
//         
//         for (var row = 2; row <= worksheet.Dimension.Rows; row++)
//         {
//             var first = worksheet.GetCellValue(row, 1);
//             var second = worksheet.GetCellValue(row, 2);
//             var lower = first?.ToLower() ?? "";
//             var isClient = lower.Contains("c");
//             var isServerModel = lower.Contains("sh");
//             var isServerHotfix = lower.Contains("sm");
//             
//             if (string.IsNullOrEmpty(second))
//             {
//                 continue;
//             }
//             
//             string str;
//             
//             if (second.StartsWith("#"))
//             {
//                 str = $"\t\t// {second}";
//                 clientStrBuilder.AppendLine(str);
//                 serverModelStrBuilder.AppendLine(str);
//                 serverHotfixStrBuilder.AppendLine(str);
//                 continue;
//             }
//             
//             str = GetCodeStr(worksheet, row);
//             
//             if (isServerModel)
//             {
//                 serverModelStrBuilder.AppendLine(str);
//             }
//
//             if (isServerHotfix)
//             {
//                 serverHotfixStrBuilder.AppendLine(str);
//             }
//
//             if (isClient)
//             {
//                 clientStrBuilder.AppendLine(str);
//             }
//         }
//         
//         clientStrBuilder.AppendLine("\t}\n}");
//         serverModelStrBuilder.AppendLine("\t}\n}");
//         serverHotfixStrBuilder.AppendLine("\t}\n}");
//         
//         Write("ConstValue.cs", clientStrBuilder.ToString(), CustomExportType.Client);
//         Write("ConstValue.cs", serverModelStrBuilder.ToString(), CustomExportType.Server);
//         Write("ConstValueHotfix.cs", serverHotfixStrBuilder.ToString(),"../../Server/Hotfix/Generate/CustomExport123" ,CustomExportType.Server);
//     }
//     
//     private static string GetCodeStr(ExcelWorksheet sheet, int row)
//     {
//         var typeStr = sheet.GetCellValue(row, 3);
//         var name = sheet.GetCellValue(row, 2);
//         var value = sheet.GetCellValue(row, 4);
//         var desc = sheet.GetCellValue(row, 5);
//
//         try
//         {
//             if (typeStr.Contains("[]") || typeStr.Contains("[,]"))
//             {
//                 return $"\t\tpublic static readonly {typeStr} {name} = {DefaultValue(typeStr, value)}; // {desc}";
//             }
//
//             if (typeStr.Contains("Vector"))
//             {
//                 return $"\t\tpublic static readonly {typeStr} {name} = {DefaultValue(typeStr, value)}; // {desc}";
//             }
//
//             return $"\t\tpublic const {typeStr} {name} = {DefaultValue(typeStr, value)}; // {desc}";
//         }
//         catch (Exception e)
//         {
//             Log.Error($"{name} 常量导出异常 : {e}");
//             return "";
//         }
//     }
//     
//     private static string DefaultValue(string type, string value)
//     {
//         switch (type)
//         {
//             case "byte[]":
//             case "int[]":
//             case "long[]":
//             case "string[]":
//             case "double[]":
//             case "float[]":
//                 return $"new {type} {{{value}}}";
//             case "byte[,]":
//             case "int[,]":
//             case "long[,]":
//             case "string[,]":
//             case "float[,]":
//             case "double[,]":
//                 return $"new {type} {{{value}}}";
//             case "int":
//             case "bool":
//             case "uint":
//             case "long":
//             case "double":
//                 return $"{value}";
//             case "float":
//                 return value[^1] == 'f' ? value : $"{value}f";
//             case "string":
//                 return $"\"{value}\"";
//             case "Vector2":
//             {
//                 var strings = value.Split(',', StringSplitOptions.TrimEntries);
//                 return $"new Vector2({strings[0]},{strings[1]})";
//             }
//             case "Vector3":
//             {
//                 var strings = value.Split(',', StringSplitOptions.TrimEntries);
//                 return $"new Vector3({strings[0]},{strings[1]},{strings[2]})";
//             }
//             default:
//                 throw new Exception($"不支持此类型: {type}");
//         }
//     }
// }