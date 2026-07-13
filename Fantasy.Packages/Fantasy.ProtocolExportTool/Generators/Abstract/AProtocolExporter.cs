using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fantasy.Network;
using Fantasy.ProtocolExportTool.Generators;
using Fantasy.ProtocolExportTool.Generators.Parsers;
using Fantasy.ProtocolExportTool.Generators.Validators;
using Fantasy.ProtocolExportTool.Models;
using Spectre.Console;

namespace Fantasy.ProtocolExportTool.Abstract;

public abstract partial class AProtocolExporter(string protocolDirectory, string clientDirectory, string serverDirectory,
    string opCodeCacheFile, ProtocolExportType protocolExportType)
{
    private readonly ConcurrentBag<string> _errors = new();
    private readonly Dictionary<string, int> _routeTypes = new();
    private readonly Dictionary<string, int> _roamingTypes = new();
    
    private readonly HashSet<string> _outerNamespaces = [];
    private readonly HashSet<string> _innerNamespaces = [];

    private readonly Dictionary<string, MessageDefinition> _outerMessages = [];
    private readonly Dictionary<string, MessageDefinition> _innerMessages = [];
    
    private readonly List<EnumDefinition> _outerEnums = [];
    private readonly List<EnumDefinition> _innerEnums = [];
    
    private readonly List<OpcodeInfo> _outerOpcode = [];
    private readonly List<OpcodeInfo> _innerOpcode = [];
    private readonly Dictionary<string, uint> _opCodeCache = new(StringComparer.Ordinal);
    private readonly Dictionary<uint, string> _opCodeOwners = [];
    private bool _opCodeCacheLoaded;
    
    protected readonly string ProtocolDirectory = NormalizePath(protocolDirectory);
    protected readonly string ClientDirectory = NormalizePath(clientDirectory);
    protected readonly string ServerDirectory = NormalizePath(serverDirectory);
    private readonly string _opCodeCacheFile = NormalizeOptionalPath(opCodeCacheFile);
    
    private static string NormalizePath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Path cannot be null or empty", nameof(path)) : Path.GetFullPath(path.Trim());
    }

    private static string NormalizeOptionalPath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFullPath(path.Trim());
    }
    
    public bool IsErrors()
    {
        if (_errors.IsEmpty)
        {
            return false;
        }

        AnsiConsole.MarkupLine("[red]The following format errors were found:[/]");
        foreach (var error in _errors)
        {
            AnsiConsole.MarkupLine(error);
        }

        return true;
    }

    private string GetOpCodeCachePath()
    {
        return string.IsNullOrWhiteSpace(_opCodeCacheFile)
            ? Path.Combine(ProtocolDirectory, "OpCode.Cache")
            : _opCodeCacheFile;
    }

    private void EnsureOpCodeCacheLoaded()
    {
        if (_opCodeCacheLoaded)
        {
            return;
        }

        _opCodeCacheLoaded = true;
        var cachePath = GetOpCodeCachePath();
        if (!File.Exists(cachePath))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(cachePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal) || line is "{" or "}")
            {
                continue;
            }

            var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                line = line[..commentIndex].Trim();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = line[..separatorIndex].Trim();
            var valueText = line[(separatorIndex + 1)..].Trim().TrimEnd(',');
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(valueText))
            {
                continue;
            }

            if (uint.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                if (_opCodeOwners.TryGetValue(value, out var existingOwner) && !string.Equals(existingOwner, name, StringComparison.Ordinal))
                {
                    continue;
                }

                _opCodeCache[name] = value;
                _opCodeOwners[value] = name;
            }
        }
    }

    private void SaveOpCodeCache()
    {
        var cachePath = GetOpCodeCachePath();
        var directory = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = _opCodeCache
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key} = {pair.Value}");
        File.WriteAllLines(cachePath, lines);
    }

    #region Generate

    public async Task GenerateRouteTypeAsync()
    {
        // AnsiConsole.MarkupLine($"[cyan]Generating RouteType code with {_routeTypes.Count} types...[/]");
                    
        var template = GenerateRouteTypes(_routeTypes);

        if (template == string.Empty)
        {
            return;
        }

        if (protocolExportType.HasFlag(ProtocolExportType.Server))
        {
            if (!Directory.Exists(ServerDirectory))
            {
                Directory.CreateDirectory(ServerDirectory);
            }
            
            var filePath = Path.Combine(ServerDirectory, "RouteType.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated RouteType.cs at {filePath}[/]");
        }

        if (protocolExportType.HasFlag(ProtocolExportType.Client))
        {
            if (!Directory.Exists(ClientDirectory))
            {
                Directory.CreateDirectory(ClientDirectory);
            }
            
            var filePath = Path.Combine(ClientDirectory, "RouteType.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated RouteType.cs at {filePath}[/]");
        }
    }

    public async Task GenerateRoamingTypeAsync()
    {
        // AnsiConsole.MarkupLine($"[cyan]Generating RoamingType code with {_roamingTypes.Count} types...[/]");
        
        var template = GenerateRoamingTypes(_roamingTypes);
        
        if (template == string.Empty)
        {
            return;
        }
        
        if (protocolExportType.HasFlag(ProtocolExportType.Server))
        {
            if (!Directory.Exists(ServerDirectory))
            {
                Directory.CreateDirectory(ServerDirectory);
            }
            
            var filePath = Path.Combine(ServerDirectory, "RoamingType.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated RoamingType.cs at {filePath}[/]");
        }

        if (protocolExportType.HasFlag(ProtocolExportType.Client))
        {
            if (!Directory.Exists(ClientDirectory))
            {
                Directory.CreateDirectory(ClientDirectory);
            }
            
            var filePath = Path.Combine(ClientDirectory, "RoamingType.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated RoamingType.cs at {filePath}[/]");
        }
    }

    public async Task GenerateOuterOpcodeAsync()
    {
        // AnsiConsole.MarkupLine($"[cyan]Generating {_outerOpcode.Count} Outer Opcode...[/]");
        
        var template = GenerateOuterOpcode(_outerOpcode);
        
        if (template == string.Empty)
        {
            return;
        }
        
        if (protocolExportType.HasFlag(ProtocolExportType.Server))
        {
            if (!Directory.Exists(ServerDirectory))
            {
                Directory.CreateDirectory(ServerDirectory);
            }
            
            var filePath = Path.Combine(ServerDirectory, "OuterOpcode.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated OuterOpcode.cs at {filePath}[/]");
        }

        if (protocolExportType.HasFlag(ProtocolExportType.Client))
        {
            if (!Directory.Exists(ClientDirectory))
            {
                Directory.CreateDirectory(ClientDirectory);
            }
            
            var filePath = Path.Combine(ClientDirectory, "OuterOpcode.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated OuterOpcode.cs at {filePath}[/]");
        }
    }
    
    public async Task GenerateInnerOpcodeAsync()
    {
        // AnsiConsole.MarkupLine($"[cyan]Generating {_innerOpcode.Count} Inner Opcode...[/]");
        
        var template = GenerateInnerOpcode(_innerOpcode);
        
        if (template == string.Empty)
        {
            return;
        }
        
        if (protocolExportType.HasFlag(ProtocolExportType.Server))
        {
            if (!Directory.Exists(ServerDirectory))
            {
                Directory.CreateDirectory(ServerDirectory);
            }
            
            var filePath = Path.Combine(ServerDirectory, "InnerOpcode.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated InnerOpcode.cs at {filePath}[/]");
        }
    }

    public async Task GenerateOuterMessageAsync()
    {
        // AnsiConsole.MarkupLine($"[cyan]Generating {_outerMessages.Count} Outer messages...[/]");

        var template = GenerateOuterMessages(_outerNamespaces, _outerMessages);
        
        if (template == string.Empty)
        {
            return;
        }
        
        if (protocolExportType.HasFlag(ProtocolExportType.Server))
        {
            if (!Directory.Exists(ServerDirectory))
            {
                Directory.CreateDirectory(ServerDirectory);
            }
            
            var filePath = Path.Combine(ServerDirectory, "OuterMessage.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated OuterMessage.cs at {filePath}[/]");
        }

        if (protocolExportType.HasFlag(ProtocolExportType.Client))
        {
            if (!Directory.Exists(ClientDirectory))
            {
                Directory.CreateDirectory(ClientDirectory);
            }
            
            var filePath = Path.Combine(ClientDirectory, "OuterMessage.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated OuterMessage.cs at {filePath}[/]");
        }
    }

    public async Task GenerateInnerMessageAsync()
    {
        // AnsiConsole.MarkupLine($"[cyan]Generating {_innerMessages.Count} Inner messages...[/]");

        var template = GenerateInnerMessages(_innerNamespaces, _innerMessages);
        
        if (template == string.Empty)
        {
            return;
        }
        
        if (!Directory.Exists(ServerDirectory))
        {
            Directory.CreateDirectory(ServerDirectory);
        }

        if (protocolExportType.HasFlag(ProtocolExportType.Server))
        {
            var filePath = Path.Combine(ServerDirectory, "InnerMessage.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated InnerMessage.cs at {filePath}[/]");
        }
    }

    public async Task GenerateOuterMessageHelperAsync()
    {
        // AnsiConsole.MarkupLine($"[cyan]Generating {_outerMessages.Count} Outer messageHelper...[/]");

        var template = GenerateOuterMessageHelper(_outerMessages);

        if (template == string.Empty)
        {
            return;
        }

        if (!Directory.Exists(ClientDirectory))
        {
            Directory.CreateDirectory(ClientDirectory);
        }

        if (protocolExportType.HasFlag(ProtocolExportType.Client))
        {
            var filePath = Path.Combine(ClientDirectory, "NetworkProtocolHelper.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated NetworkProtocolHelper.cs at {filePath}[/]");
        }
    }

    public async Task GenerateOuterEnumsAsync()
    {
        // AnsiConsole.MarkupLine($"[cyan]Generating Outer enums...[/]");

        var template = GenerateOuterEnums(_outerEnums);

        if (template == string.Empty)
        {
            return;
        }

        if (protocolExportType.HasFlag(ProtocolExportType.Server))
        {
            if (!Directory.Exists(ServerDirectory))
            {
                Directory.CreateDirectory(ServerDirectory);
            }

            var filePath = Path.Combine(ServerDirectory, "OuterEnum.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated OuterEnum.cs at {filePath}[/]");
        }

        if (protocolExportType.HasFlag(ProtocolExportType.Client))
        {
            if (!Directory.Exists(ClientDirectory))
            {
                Directory.CreateDirectory(ClientDirectory);
            }

            var filePath = Path.Combine(ClientDirectory, "OuterEnum.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated OuterEnum.cs at {filePath}[/]");
        }
    }

    public async Task GenerateInnerEnumsAsync()
    {
        // AnsiConsole.MarkupLine($"[cyan]Generating Inner enums...[/]");

        var template = GenerateInnerEnums(_innerEnums);

        if (template == string.Empty)
        {
            return;
        }

        if (!Directory.Exists(ServerDirectory))
        {
            Directory.CreateDirectory(ServerDirectory);
        }

        if (protocolExportType.HasFlag(ProtocolExportType.Server))
        {
            var filePath = Path.Combine(ServerDirectory, "InnerEnum.cs");
            await File.WriteAllTextAsync(filePath, template);
            // AnsiConsole.MarkupLine($"[green]Generated InnerEnum.cs at {filePath}[/]");
        }
    }

    protected abstract string GenerateRouteTypes(IReadOnlyDictionary<string, int> routeTypes);
    protected abstract string GenerateRoamingTypes(IReadOnlyDictionary<string, int> roamingTypes);
    protected abstract string GenerateOuterOpcode(IReadOnlyList<OpcodeInfo> opcodeInfos);
    protected abstract string GenerateInnerOpcode(IReadOnlyList<OpcodeInfo> opcodeInfos);
    protected abstract string GenerateOuterMessages(IReadOnlySet<string> outerNamespaces, IReadOnlyDictionary<string, MessageDefinition> messageDefinitions);
    protected abstract string GenerateInnerMessages(IReadOnlySet<string> innerNamespaces, IReadOnlyDictionary<string, MessageDefinition> messageDefinitions);
    protected abstract string GenerateOuterMessageHelper(IReadOnlyDictionary<string, MessageDefinition> messageDefinitions);
    protected abstract string GenerateOuterEnums(IReadOnlyList<EnumDefinition> enumDefinitions);
    protected abstract string GenerateInnerEnums(IReadOnlyList<EnumDefinition> enumDefinitions);

    #endregion

    #region ParseRouteType

    public void ParseRouteTypeConfig()
    {
        var configPath = Path.Combine(ProtocolDirectory, "RouteType.Config");

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: RouteType.Config not found at {configPath}[/]");
            return;
        }

        var lines = File.ReadAllLines(configPath);
        
        var regex = MyRegex();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//"))
            {
                continue;
            }
            
            var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            
            if (commentIndex > 0)
            {
                line = line.Substring(0, commentIndex).Trim();
            }

            var match = regex.Match(line);
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var value = int.Parse(match.Groups[2].Value);

                if (!_routeTypes.TryAdd(name, value))
                {
                    _errors.Add($"[red] RouteType.Config line {lineNumber}: Duplicate route type '{name}'[/]");
                }
            }
            else
            {
                _errors.Add($"[red] RouteType.Config line {lineNumber}: {Markup.Escape(line)}[/]");
            }
        }
    }
    
    public void ParseRoamingTypeConfig()
    {
        var configPath = Path.Combine(ProtocolDirectory, "RoamingType.Config");

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: RoamingType.Config not found at {configPath}[/]");
            return;
        }

        var lines = File.ReadAllLines(configPath);
        var regex = MyRegex();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//"))
            {
                continue;
            }
            
            var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            
            if (commentIndex > 0)
            {
                line = line.Substring(0, commentIndex).Trim();
            }

            var match = regex.Match(line);
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var value = int.Parse(match.Groups[2].Value);

                if (!_roamingTypes.TryAdd(name, value))
                {
                    _errors.Add($"[red] RoamingType.Config line {lineNumber}: Duplicate roaming type '{name}'[/]");
                }
            }
            else
            {
                _errors.Add($"[red] RoamingType.Config line {lineNumber}: {Markup.Escape(line)}[/]");
            }
        }
    }

    #endregion
    
    #region ParseMessage

    public void ParseAndValidateOuterProtocols()
    {
        ParseAndValidateProtocols("Outer", _outerOpcode, _outerNamespaces, _outerMessages, _outerEnums);
    }

    public void ParseAndValidateInnerProtocols()
    {
        ParseAndValidateProtocols("Inner", _innerOpcode, _innerNamespaces, _innerMessages, _innerEnums);
    }

    private void ParseAndValidateProtocols(string protocol, List<OpcodeInfo> opcodeInfo, HashSet<string> namespaces, Dictionary<string, MessageDefinition> allMessages, List<EnumDefinition> allEnums)
    {
        var validator = new ProtocolValidator();
        var isOuter = protocol.Equals("Outer", StringComparison.OrdinalIgnoreCase);
        EnsureOpCodeCacheLoaded();
        var opCodeGenerator = new OpCodeGenerator(isOuter, _opCodeCache, _opCodeOwners);

        // 1. 解析所有文件
        foreach (var (filePath, fileLines) in ReadProtocolFilesLinesWithPath(protocol))
        {
            var parser = new ProtocolFileParser(filePath);
            var parseResult = parser.Parse(fileLines);

            // 收集解析错误
            foreach (var error in parser.GetErrors())
            {
                _errors.Add(error);
            }
            
            // 收集Namespaces
            foreach (var @namespace in parseResult.Namespaces)
            {
                namespaces.Add(@namespace);
            }
            
            // 收集Messages
            foreach (var (name, messageDefinition) in parseResult.Messages)
            {
                // 验证消息
                if (!validator.Validate(messageDefinition))
                {
                    continue;
                }
                
                allMessages.Add(name, messageDefinition);
            }

            // 收集Enums
            allEnums.AddRange(parseResult.Enums);
        }

        // 2. 为所有需要 OpCode 的消息生成 OpCode
        foreach (var messageDefinition in allMessages.Values.OrderBy(message => message.Name, StringComparer.Ordinal))
        {
            if (!messageDefinition.HasOpCode)
            {
                continue;
            }
            
            messageDefinition.OpCode = opCodeGenerator.Generate(messageDefinition);
            opcodeInfo.Add(messageDefinition.OpCode);
        }
        
        // 3. 验证所有Enum
        foreach (var @enum in allEnums)
        {
            validator.Validate(@enum);
        }

        // 4. 收集验证错误
        foreach (var error in validator.GetErrors())
        {
            _errors.Add(error);
        }

        foreach (var pair in opCodeGenerator.AssignedCodes)
        {
            _opCodeCache[pair.Key] = pair.Value;
            _opCodeOwners[pair.Value] = pair.Key;
        }

        SaveOpCodeCache();
    }
    
    private IEnumerable<(string FilePath, string[] Lines)> ReadProtocolFilesLinesWithPath(string protocolDir)
    {
        var protocolPath = Path.Combine(ProtocolDirectory, protocolDir);

        if (!Directory.Exists(protocolPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: {protocolDir} directory not found at {protocolPath}[/]");
            yield break;
        }

        var files = Directory.GetFiles(protocolPath, "*.proto", SearchOption.AllDirectories)
            .OrderBy(f => Path.GetRelativePath(protocolPath, f), StringComparer.Ordinal);

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            yield return (file, lines);
        }
    }

    #endregion
    
    [GeneratedRegex(@"^\s*(\w+)\s*=\s*(\d+)\s*$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}