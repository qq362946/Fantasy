using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using Fantasy;
using Fantasy.Exporter;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using OfficeOpenXml;
using static System.String;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace Exporter.Excel;
using TableDictionary = SortedDictionary<string, List<int>>;
/// <summary>
/// Excel 数据导出器，用于从 Excel 文件导出数据到二进制格式和生成 C# 类文件。
/// </summary>
public sealed class ExcelExporter
{
    private readonly string _excelProgramPath;
    private readonly string _versionFilePath;
    private readonly string _excelClientFileDirectory;
    private readonly string _excelServerFileDirectory;
    public readonly string ClientCustomExportDirectory;
    public readonly string ServerCustomExportDirectory;
    private readonly string _excelServerBinaryDirectory;
    private readonly string _excelClientBinaryDirectory;
    private readonly string _excelServerJsonDirectory;
    private readonly string _excelClientJsonDirectory;
    private Dictionary<string, long> _versionDic; // 存储 Excel 文件的版本信息。
    private readonly Regex _regexName = new Regex("^[a-zA-Z][a-zA-Z0-9_]*$"); // 用于验证 Excel 表名的正则表达式。
    private readonly HashSet<string> _loadFiles = new HashSet<string> {".xlsx", ".xlsm", ".csv"}; // 加载的支持文件扩展名。
    private readonly OneToManyList<string, ExportInfo> _tables = new OneToManyList<string, ExportInfo>(); // 存储 Excel 表及其导出信息。
    private readonly ConcurrentDictionary<string, ExcelTable> _excelTables = new ConcurrentDictionary<string, ExcelTable>(); // 存储解析后的 Excel 表。
    public readonly ConcurrentDictionary<string, ExcelWorksheet> Worksheets = new ConcurrentDictionary<string, ExcelWorksheet>(); // 存储已加载的 Excel 工作表。
    public readonly Dictionary<string, string> IgnoreTable = new Dictionary<string, string>(); // 存储以#开头的的表和路径
    private static string _template;
    /// <summary>
    /// 获取或设置 Excel 代码模板的内容。
    /// </summary>
    private static string ExcelTemplate
    {
        get
        {
            return _template ??= File.ReadAllText(ExporterSettingsHelper.ExcelTemplatePath);
        }
    }
    /// <summary>
    /// 导表支持的数据类型集合。
    /// </summary>
    public static readonly HashSet<string> ColTypeSet = new HashSet<string>()
    {
        "", "0", "bool", "byte", "short", "ushort", "int", "uint", "long", "ulong", "float", "string",
        "IntDictionaryConfig", "StringDictionaryConfig",
        "short[]", "int[]", "long[]", "float[]", "string[]","uint[]"
    };
    static ExcelExporter()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// 根据指定的 exportType 初始化 ExcelExporter 类的新实例。
    /// </summary>
    /// <param name="exportType">要执行的导出类型（AllExcel 或 AllExcelIncrement）。</param>
    public ExcelExporter(ExportType exportType)
    {
        if (ExporterSettingsHelper.ExcelTemplatePath?.Trim() == "")
        {
            Log.Info($"ExcelTemplatePath Can not be empty!");
            return;
        }
        
        if (ExporterSettingsHelper.ExcelVersionFile == null || ExporterSettingsHelper.ExcelVersionFile.Trim() == "")
        {
            Log.Info($"ExcelVersionFile Can not be empty!");
            return;
        }
        
        if (ExporterSettingsHelper.ExcelProgramPath == null || ExporterSettingsHelper.ExcelProgramPath.Trim() == "")
        {
            Log.Info($"ExcelProgramPath Can not be empty!");
            return;
        }
        
        _excelProgramPath = FileHelper.GetFullPath(ExporterSettingsHelper.ExcelProgramPath);
        _versionFilePath = FileHelper.GetFullPath(ExporterSettingsHelper.ExcelVersionFile);
        
        switch (exportType)
        {
            case ExportType.AllExcelIncrement:
            {
                break;
            }
            case ExportType.AllExcel:
            {
                if (File.Exists(_versionFilePath))
                {
                    File.Delete(_versionFilePath);
                }

                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
                {
                    if (ExporterSettingsHelper.ExcelClientBinaryDirectory == null ||
                        ExporterSettingsHelper.ExcelClientBinaryDirectory.Trim() == "")
                    {
                        Log.Info($"ExcelClientBinaryDirectory Can not be empty!");
                        return;
                    }
                    
                    if (ExporterSettingsHelper.ExcelClientJsonDirectory == null ||
                        ExporterSettingsHelper.ExcelClientJsonDirectory.Trim() == "")
                    {
                        Log.Info($"ExcelClientJsonDirectory Can not be empty!");
                        return;
                    }
                    
                    if (ExporterSettingsHelper.ExcelClientFileDirectory == null ||
                        ExporterSettingsHelper.ExcelClientFileDirectory.Trim() == "")
                    {
                        Log.Info($"ExcelServerFileDirectory Can not be empty!");
                        return;
                    }
                    
                    if (ExporterSettingsHelper.ClientCustomExportDirectory == null ||
                        ExporterSettingsHelper.ClientCustomExportDirectory.Trim() == "")
                    {
                        Log.Info($"ClientCustomExportDirectory Can not be empty!");
                        return;
                    }

                    _excelClientJsonDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.ExcelClientJsonDirectory);
                    _excelClientBinaryDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.ExcelClientBinaryDirectory);
                    ClientCustomExportDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.ClientCustomExportDirectory);
                    _excelClientFileDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.ExcelClientFileDirectory);
                    FileHelper.ClearDirectoryFile(_excelClientFileDirectory);
                }

                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
                {
                    if (ExporterSettingsHelper.ExcelServerFileDirectory == null ||
                        ExporterSettingsHelper.ExcelServerFileDirectory.Trim() == "")
                    {
                        Log.Info($"ExcelServerFileDirectory Can not be empty!");
                        return;
                    }
                    
                    if (ExporterSettingsHelper.ExcelServerJsonDirectory == null ||
                        ExporterSettingsHelper.ExcelServerJsonDirectory.Trim() == "")
                    {
                        Log.Info($"ExcelServerJsonDirectory Can not be empty!");
                        return;
                    }
                    
                    if (ExporterSettingsHelper.ExcelServerBinaryDirectory == null ||
                        ExporterSettingsHelper.ExcelServerBinaryDirectory.Trim() == "")
                    {
                        Log.Info($"ExcelServerBinaryDirectory Can not be empty!");
                        return;
                    }

                    if (ExporterSettingsHelper.ServerCustomExportDirectory == null ||
                        ExporterSettingsHelper.ServerCustomExportDirectory.Trim() == "")
                    {
                        Log.Info($"ServerCustomExportDirectory Can not be empty!");
                        return;
                    }

                    _excelServerJsonDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.ExcelServerJsonDirectory);
                    _excelServerBinaryDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.ExcelServerBinaryDirectory);
                    ServerCustomExportDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.ServerCustomExportDirectory);
                    _excelServerFileDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.ExcelServerFileDirectory);
                    FileHelper.ClearDirectoryFile(_excelServerFileDirectory);
                }
                
                break;
            }
        }

        Find();
        Parsing();
        ExportToBinary();
        File.WriteAllText(_versionFilePath, JsonConvert.SerializeObject(_versionDic));
        CustomExport();
    }
    
    private void CustomExport()
    {
        var task = new List<Task>();
        
        // 加载自定义导出程序集
        
        Assembly serverCustomAssembly = null;
        Assembly clientCustomAssembly = null;
        
        if (!Directory.Exists($"{_excelProgramPath}CSharp"))
        {
            Directory.CreateDirectory($"{_excelProgramPath}CSharp");
        }
        
        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
        {
            if (!Directory.Exists($"{_excelProgramPath}CSharp/Server"))
            {
                Directory.CreateDirectory($"{_excelProgramPath}CSharp/Server");
            }
            serverCustomAssembly = DynamicAssembly.Load($"{_excelProgramPath}CSharp/Server");
            FileHelper.ClearDirectoryFile(ServerCustomExportDirectory);
            // 生成一个系统内置的自定义导出类
            var sceneTypeConfigToEnum = new SceneTypeConfigToEnum();
            sceneTypeConfigToEnum.Init(this, Worksheets);
            task.Add(Task.Run(sceneTypeConfigToEnum.Run));
        }
    
        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
        {
            if (!Directory.Exists($"{_excelProgramPath}CSharp/Client"))
            {
                Directory.CreateDirectory($"{_excelProgramPath}CSharp/Client");
            }
            clientCustomAssembly = DynamicAssembly.Load($"{_excelProgramPath}CSharp/Client");
            FileHelper.ClearDirectoryFile(ClientCustomExportDirectory);
        }

        void AddCustomExportTask(Assembly assembly)
        {
            var assemblyInfo = new AssemblyInfo(assembly);
            
            if (assemblyInfo.AssemblyTypeGroupList.TryGetValue(typeof(ICustomExport), out var customExportList))
            {
                foreach (var type in customExportList)
                {
                    var customExport = (ICustomExport)Activator.CreateInstance(type);
                    
                    if (customExport != null)
                    {
                        customExport.Init(this, Worksheets);
                        task.Add(Task.Run(customExport.Run));
                    }
                }
            }
        }
        
        if (serverCustomAssembly != null)
        {
            AddCustomExportTask(serverCustomAssembly);
        }
        
        if (clientCustomAssembly != null)
        {
            AddCustomExportTask(clientCustomAssembly);
        }
        
        Task.WaitAll(task.ToArray());
    }
    
    /// <summary>
    /// 查找配置文件
    /// </summary>
    private void Find()
    {
        if(File.Exists(_versionFilePath))
        {
            var versionJson = File.ReadAllText(_versionFilePath);
            _versionDic = JsonConvert.DeserializeObject<Dictionary<string, long>>(versionJson);
        }
        else
        {
            _versionDic = new Dictionary<string, long>();
        }
        
        var dir = new DirectoryInfo(_excelProgramPath);
        var excelFiles = dir.GetFiles("*", SearchOption.AllDirectories);

        if (excelFiles.Length <= 0)
        {
            return;
        }

        foreach (var excelFile in excelFiles)
        {
            // 过滤掉非指定后缀的文件

            if (!_loadFiles.Contains(excelFile.Extension))
            {
                continue;
            }

            var lastIndexOf = excelFile.Name.LastIndexOf(".", StringComparison.Ordinal);

            if (lastIndexOf < 0)
            {
                continue;
            }

            var fullName = excelFile.FullName;
            var excelName = excelFile.Name.Substring(0, lastIndexOf);
            var path = fullName.Substring(0, fullName.Length - excelFile.Name.Length);

            // 过滤~开头文件
            
            if (excelName.StartsWith("~", StringComparison.Ordinal))
            {
                continue;
            }

            // 如果文件名以#开头，那么这个文件夹下的所有文件都不导出
            
            if (excelName.StartsWith("#", StringComparison.Ordinal))
            {
                IgnoreTable.Add(excelName, fullName);
                continue;
            }
            
            // 如果文件夹名包含#，那么这个文件夹下的所有文件都不导出

            if (path.Contains("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (!_regexName.IsMatch(excelName))
            {
                Log.Info($"{excelName} 配置文件名非法");
                continue;
            }

            _tables.Add(excelName.Split('_')[0], new ExportInfo()
            {
                Name = excelName, FileInfo = excelFile
            });
        }
        
        var removeTables = new List<string>();

        foreach (var (tableName, tableList) in _tables)
        {
            var isNeedExport = false;

            foreach (var exportInfo in tableList)
            {
                var timer = TimeHelper.Transition(exportInfo.FileInfo.LastWriteTime);

                if (!isNeedExport)
                {
                    if (_versionDic.TryGetValue(exportInfo.Name, out var lastWriteTime))
                    {
                        isNeedExport = lastWriteTime != timer;
                    }
                    else
                    {
                        isNeedExport = true;
                    }
                }

                _versionDic[exportInfo.Name] = timer;
            }

            if (!isNeedExport)
            {
                removeTables.Add(tableName);
            }
        }
        
        foreach (var removeTable in removeTables)
        {
            _tables.Remove(removeTable);
        }
        
        foreach (var (_, exportInfo) in _tables)
        {
            exportInfo.Sort((x, y) => Compare(x.Name, y.Name, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// 生成配置文件
    /// </summary>
    private void Parsing()
    {
        var generateTasks = new List<Task>();

        foreach (var (tableName, tableList) in _tables)
        {
            var task = Task.Run(() =>
            {
                var writeToClassTask = new List<Task>();
                var excelTable = new ExcelTable(tableName);

                // 筛选需要导出的列

                foreach (var exportInfo in tableList)
                {
                    try
                    {
                        var serverColInfoList = new List<int>();
                        var clientColInfoList = new List<int>();
                        var worksheet = LoadExcel(exportInfo.FileInfo.FullName, true);

                        for (var col = 3; col <= worksheet.Columns.EndColumn; col++)
                        {
                            // 列名字第一个字符是#不参与导出

                            var colName = worksheet.GetCellValue(5, col);
                            if (colName.StartsWith("#", StringComparison.Ordinal))
                            {
                                continue;
                            }

                            // 数值列不参与导出

                            var numericalCol = worksheet.GetCellValue(3, col);
                            if (numericalCol != "" && numericalCol != "0")
                            {
                                continue;
                            }

                            var serverType = worksheet.GetCellValue(1, col);
                            var clientType = worksheet.GetCellValue(2, col);
                            var isExportServer = !IsNullOrEmpty(serverType) && serverType != "0";
                            var isExportClient = !IsNullOrEmpty(clientType) && clientType != "0";

                            if (!isExportServer && !isExportClient)
                            {
                                continue;
                            }

                            if (isExportServer && isExportClient & serverType != clientType)
                            {
                                Log.Info($"配置表 {exportInfo.Name} {col} 列 [{colName}] 客户端类型 {clientType} 和 服务端类型 {serverType} 不一致");
                                continue;
                            }

                            if (!ColTypeSet.Contains(serverType) || !ColTypeSet.Contains(clientType))
                            {
                                Log.Info($"配置表 {exportInfo.Name} {col} 列 [{colName}] 客户端类型 {clientType}, 服务端类型 {serverType} 不合法");
                                continue;
                            }

                            if (!_regexName.IsMatch(colName))
                            {
                                Log.Info($"配置表 {exportInfo.Name} {col} 列 [{colName}] 列名非法");
                                continue;
                            }

                            serverColInfoList.Add(col);

                            if (isExportClient)
                            {
                                clientColInfoList.Add(col);
                            }
                        }

                        if (clientColInfoList.Count > 0)
                        {
                            excelTable.ClientColInfos.Add(exportInfo.FileInfo.FullName, clientColInfoList);
                        }

                        if (serverColInfoList.Count > 0)
                        {
                            excelTable.ServerColInfos.Add(exportInfo.FileInfo.FullName, serverColInfoList);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Config : {tableName}, Name : {exportInfo.Name}, Error : {e}");
                    }
                }

                // 生成cs文件

                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
                {
                    writeToClassTask.Add(Task.Run(() =>
                    {
                        WriteToClass(excelTable.ServerColInfos, _excelServerFileDirectory, true);
                    }));
                }

                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
                {
                    writeToClassTask.Add(Task.Run(() =>
                    {
                        WriteToClass(excelTable.ClientColInfos, _excelClientFileDirectory, false);
                    }));
                }

                Task.WaitAll(writeToClassTask.ToArray());
                _excelTables.TryAdd(tableName, excelTable);
            });

            generateTasks.Add(task);
        }

        Task.WaitAll(generateTasks.ToArray());
    }

    /// <summary>
    /// 写入到cs
    /// </summary>
    /// <param name="colInfos"></param>
    /// <param name="exportPath"></param>
    /// <param name="isServer"></param>
    private void WriteToClass(TableDictionary colInfos, string exportPath, bool isServer)
    {
        if (colInfos.Count <= 0)
        {
            return;
        }

        var index = 0;
        var fileBuilder = new StringBuilder();
        var colNameSet = new HashSet<string>();

        if (colInfos.Count == 0)
        {
            return;
        }

        var csName = Path.GetFileNameWithoutExtension(colInfos.First().Key)?.Split('_')[0];

        foreach (var (tableName, cols) in colInfos)
        {
            if (cols == null || cols.Count == 0)
            {
                continue;
            }

            var excelWorksheet = LoadExcel(tableName, false);

            foreach (var colIndex in cols)
            {
                var colName = excelWorksheet.GetCellValue(5, colIndex);

                if (colNameSet.Contains(colName))
                {
                    continue;
                }

                colNameSet.Add(colName);

                string colType;

                if (isServer)
                {
                    colType = excelWorksheet.GetCellValue(1, colIndex);

                    if (IsNullOrEmpty(colType) || colType == "0")
                    {
                        colType = excelWorksheet.GetCellValue(2, colIndex);
                    }
                }
                else
                {
                    colType = excelWorksheet.GetCellValue(2, colIndex);
                }

                var remarks = excelWorksheet.GetCellValue(4, colIndex);

                // 解决不同平台换行符不一致的问题
                
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                    {
                        fileBuilder.Append($"\r\n\t\t[ProtoMember({++index}, IsRequired  = true)]\r\n");
                        break;
                    }
                    default:
                    {
                        fileBuilder.Append($"\n\t\t[ProtoMember({++index}, IsRequired  = true)]\n");
                        break;
                    }
                }
                
                fileBuilder.Append(
                    IsArray(colType,out var t)
                        ? $"\t\tpublic {colType} {colName} {{ get; set; }} = Array.Empty<{t}>(); // {remarks}"
                        : $"\t\tpublic {colType} {colName} {{ get; set; }} // {remarks}");
            }
        }

        var template = ExcelTemplate;
        
        if (fileBuilder.Length > 0)
        {
            if (!Directory.Exists(exportPath))
            {
                FileHelper.CreateDirectory(exportPath);
            }

            var content = template.Replace("(namespace)", "Fantasy")
                .Replace("(ConfigName)", csName)
                .Replace("(Fields)", fileBuilder.ToString());
            File.WriteAllText(Path.Combine(exportPath, $"{csName}.cs"), content);
        }
    }
    /// <summary>
    /// 把数据和实体类转换二进制导出到文件中
    /// </summary>
    private void ExportToBinary()
    {
        Assembly dynamicServerAssembly = null;
        Assembly dynamicClientAssembly = null;
        var exportToBinaryTasks = new List<Task>();
        
        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server) && Directory.Exists(_excelServerFileDirectory))
        {
            dynamicServerAssembly = DynamicAssembly.Load(_excelServerFileDirectory);
        }

        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client) && Directory.Exists(_excelClientFileDirectory))
        {
            dynamicClientAssembly = DynamicAssembly.Load(_excelClientFileDirectory);
        }
        
        foreach (var (tableName, tableList) in _tables)
        {
            var task = Task.Run(() =>
            {
                DynamicConfigDataType serverDynamicInfo = null;
                DynamicConfigDataType clientDynamicInfo = null;
                
                var idCheck = new HashSet<string>();
                var excelTable = _excelTables[tableName];
                var csName = Path.GetFileNameWithoutExtension(tableName);
                
                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
                {
                    var serverColInfoCount = excelTable.ServerColInfos.Sum(d=>d.Value.Count);
                    serverDynamicInfo = serverColInfoCount == 0 ? null : DynamicAssembly.GetDynamicInfo(dynamicServerAssembly, csName);
                }

                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
                {
                    var clientColInfoCount = excelTable.ClientColInfos.Sum(d=>d.Value.Count);
                    clientDynamicInfo = clientColInfoCount == 0 ? null : DynamicAssembly.GetDynamicInfo(dynamicClientAssembly, csName);
                }

                for (var i = 0; i < tableList.Count; i++)
                {
                    var tableListName = tableList[i];

                    try
                    {
                        var fileInfoFullName = tableListName.FileInfo.FullName;
                        var excelWorksheet = LoadExcel(fileInfoFullName, false);
                        var rows = excelWorksheet.Dimension.Rows;
                        excelTable.ServerColInfos.TryGetValue(fileInfoFullName, out var serverCols);
                        excelTable.ClientColInfos.TryGetValue(fileInfoFullName, out var clientCols);

                        for (var row = 7; row <= rows; row++)
                        {
                            if (excelWorksheet.GetCellValue(row, 1).StartsWith("#", StringComparison.Ordinal))
                            {
                                continue;
                            }
                            
                            var id = excelWorksheet.GetCellValue(row, 3);

                            if (idCheck.Contains(id))
                            {
                                Log.Info($"{tableListName.Name} 存在重复Id {id} 行号 {row}");
                                continue;
                            }

                            idCheck.Add(id);
                            var isLast = row == rows && (i == tableList.Count - 1);
                            
                            if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
                            {
                                GenerateBinary(fileInfoFullName, excelWorksheet, serverDynamicInfo, serverCols, id, row, isLast, true);
                            }

                            if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
                            {
                                GenerateBinary(fileInfoFullName, excelWorksheet, clientDynamicInfo, clientCols, id, row, isLast, false);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Table:{tableListName} error! \n{e}");
                        throw;
                    }
                }

                if (serverDynamicInfo?.ConfigData != null)
                {
                    var bytes = ProtoBufHelper.ToBytes(serverDynamicInfo.ConfigData);
                    
                    if (!Directory.Exists(_excelServerBinaryDirectory))
                    {
                        Directory.CreateDirectory(_excelServerBinaryDirectory);
                    }
                    
                    File.WriteAllBytes(Path.Combine(_excelServerBinaryDirectory, $"{csName}Data.bytes"), bytes);

                    if (serverDynamicInfo.Json.Length > 0)
                    {
                        using var sw = new StreamWriter(Path.Combine(_excelServerJsonDirectory, $"{csName}Data.Json"));
                        sw.WriteLine("{\"List\":[");
                        sw.Write(serverDynamicInfo.Json.ToString());
                        sw.WriteLine("]}");
                    }
                }
                
                if (clientDynamicInfo?.ConfigData != null)
                {
                    var bytes = ProtoBufHelper.ToBytes(clientDynamicInfo.ConfigData);
                    
                    if (!Directory.Exists(_excelClientBinaryDirectory))
                    {
                        Directory.CreateDirectory(_excelClientBinaryDirectory);
                    }
                    
                    File.WriteAllBytes(Path.Combine(_excelClientBinaryDirectory, $"{csName}Data.bytes"), bytes);
                
                    if (clientDynamicInfo.Json.Length > 0)
                    {
                        using var sw = new StreamWriter(Path.Combine(_excelClientJsonDirectory, $"{csName}Data.Json"));
                        sw.WriteLine("{\"List\":[");
                        sw.Write(clientDynamicInfo.Json.ToString());
                        sw.WriteLine("]}");
                    }
                }
            });
            exportToBinaryTasks.Add(task);
        }

        Task.WaitAll(exportToBinaryTasks.ToArray());
    }

    private void GenerateBinary(string fileInfoFullName, ExcelWorksheet excelWorksheet, DynamicConfigDataType dynamicInfo, List<int> cols, string id, int row, bool isLast, bool isServer)
    {
        if (cols == null || IsNullOrEmpty(id) || cols.Count <= 0 || dynamicInfo?.ConfigType == null)
        {
            return;
        }

        var config = DynamicAssembly.CreateInstance(dynamicInfo.ConfigType);

        for (var i = 0; i < cols.Count; i++)
        {
            string colType;
            var colIndex = cols[i];
            var colName = excelWorksheet.GetCellValue(5, colIndex);
            var value = excelWorksheet.GetCellValue(row, colIndex);
            
            if (isServer)
            {
                colType = excelWorksheet.GetCellValue(1, colIndex);
                    
                if (IsNullOrEmpty(colType) || colType == "0")
                {
                    colType = excelWorksheet.GetCellValue(2, colIndex);
                }
            }
            else
            {
                colType = excelWorksheet.GetCellValue(2, colIndex);
            }

            try
            {
                SetNewValue(dynamicInfo.ConfigType.GetProperty(colName), config, colType, value);
            }
            catch (Exception e)
            {
                Log.Error($"Error Table {fileInfoFullName} Col:{colName} colType:{colType} Row:{row} value:{value} {e}");
                throw;
            }
        }
        
        dynamicInfo.Method.Invoke(dynamicInfo.Obj, new object[] {config});
                
        var json = JsonConvert.SerializeObject(config);

        if (isLast)
        {
            dynamicInfo.Json.AppendLine(json);
        }
        else
        {
            dynamicInfo.Json.AppendLine($"{json},");
        }
    }

    /// <summary>
    /// 加载忽略表
    /// </summary>
    /// <param name="name"></param>
    /// <param name="excelPackage"></param>
    /// <returns></returns>
    public bool LoadIgnoreExcel(string name, out ExcelPackage excelPackage)
    {
        excelPackage = null;

        if (!IgnoreTable.TryGetValue(name, out var path))
        {
            return false;
        }

        excelPackage = ExcelHelper.LoadExcel(path);
        return true;
    }

    /// <summary>
    /// 从 Excel 文件加载工作表并返回 ExcelWorksheet 对象。
    /// </summary>
    /// <param name="name">工作表的名称或文件路径。</param>
    /// <param name="isAddToDic">是否将加载的工作表添加到缓存字典中。</param>
    /// <returns>表示 Excel 工作表的 ExcelWorksheet 对象。</returns>
    public ExcelWorksheet LoadExcel(string name, bool isAddToDic)
    {
        if (Worksheets.TryGetValue(name, out var worksheet))
        {
            return worksheet;
        }

        var workbookWorksheets = ExcelHelper.LoadExcel(name).Workbook.Worksheets;
        worksheet = workbookWorksheets[0];

        if (isAddToDic)
        {
            Worksheets.TryAdd(name, worksheet);
            
            foreach (var workbookWorksheet in workbookWorksheets)
            {
                Worksheets.TryAdd(workbookWorksheet.Name, workbookWorksheet);
            }
        }
        
        Log.Info(name);
        return workbookWorksheets[0];
    }

    private void SetNewValue(PropertyInfo propertyInfo, AProto config, string type, string value)
    {
        if (IsNullOrWhiteSpace(value))
        {
            return;
        }

        switch (type)
        {
            case "short":
            {
                propertyInfo.SetValue(config, Convert.ToInt16(value));
                return;
            }
            case "ushort":
            {
                propertyInfo.SetValue(config, Convert.ToUInt16(value));
                return;
            }
            case "uint":
            {
                propertyInfo.SetValue(config, Convert.ToUInt32(value));
                return;
            }
            case "int":
            {
                propertyInfo.SetValue(config, Convert.ToInt32(value));
                return;
            }
            case "decimal":
            {
                propertyInfo.SetValue(config, Convert.ToDecimal(value));
                return;
            }
            case "string":
            {
                try
                {
                    propertyInfo.SetValue(config, value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                
                return;
            }
            case "bool":
            {
                // 空字符串

                value = value.ToLower();

                if (IsNullOrEmpty(value))
                {
                    propertyInfo.SetValue(config, false);
                }
                else if (bool.TryParse(value, out bool b))
                {
                    propertyInfo.SetValue(config, b);
                }
                else if (int.TryParse(value, out int v))
                {
                    propertyInfo.SetValue(config, v != 0);
                }
                else
                {
                    propertyInfo.SetValue(config, false);
                }

                return;
            }
            case "ulong":
            {
                propertyInfo.SetValue(config, Convert.ToUInt64(value));
                return;
            }
            case "long":
            {
                propertyInfo.SetValue(config, Convert.ToInt64(value));
                return;
            }
            case "double":
            {
                propertyInfo.SetValue(config, Convert.ToDouble(value));
                return;
            }
            case "float":
            {
                propertyInfo.SetValue(config, Convert.ToSingle(value));
                return;
            }
            case "int32[]":
            case "int[]":
            {
                if (value != "0")
                {
                    propertyInfo.SetValue(config, value.Split(",").Select(d => Convert.ToInt32(d)).ToArray());
                }

                return;
            }
            case "uint[]":
            {
                if (value != "0")
                {
                    propertyInfo.SetValue(config, value.Split(",").Select(d => Convert.ToUInt32(d)).ToArray());
                }
                return;
            }
            case "long[]":
            {
                if (value != "0")
                {
                    propertyInfo.SetValue(config, value.Split(",").Select(d => Convert.ToInt64(d)).ToArray());
                }

                return;
            }
            case "double[]":
            {
                if (value != "0")
                {
                    propertyInfo.SetValue(config, value.Split(",").Select(d => Convert.ToDouble(d)).ToArray());
                }

                return;
            }
            case "string[]":
            {
                if (value == "0")
                {
                    return;
                }

                var list = value.Split(",").ToArray();

                for (var i = 0; i < list.Length; i++)
                {
                    list[i] = list[i].Replace("\"", "");
                }

                propertyInfo.SetValue(config, value.Split(",").ToArray());

                return;
            }
            case "float[]":
            {
                if (value != "0")
                {
                    propertyInfo.SetValue(config, value.Split(",").Select(d => Convert.ToSingle(d)).ToArray());
                }

                return;
            }
            case "IntDictionaryConfig":
            {
                if (value.Trim() == "" || value.Trim() == "{}")
                {
                    propertyInfo.SetValue(config, null);
                    return;
                }

                var attr = new IntDictionaryConfig {Dic = JsonConvert.DeserializeObject<Dictionary<int, int>>(value)};

                propertyInfo.SetValue(config, attr);

                return;
            }
            case "StringDictionaryConfig":
            {
                if (value.Trim() == "" || value.Trim() == "{}")
                {
                    propertyInfo.SetValue(config, null);
                    return;
                }

                var attr = new StringDictionaryConfig {Dic = JsonConvert.DeserializeObject<Dictionary<int, string>>(value)};

                propertyInfo.SetValue(config, attr);

                return;
            }
            default:
                throw new NotSupportedException($"不支持此类型: {type}");
        }
    }

    private bool IsArray(string type, out string t)
    {
        t = null;
        var index = type.IndexOf("[]", StringComparison.Ordinal);

        if (index >= 0)
        {
            t = type.Remove(index, 2);
        }

        return index >= 0;
    }
}