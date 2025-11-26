using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fantasy.ProtocolExportTool.Generators;
using Fantasy.ProtocolExportTool.Generators.Parsers;
using Fantasy.ProtocolExportTool.Generators.Validators;
using Fantasy.ProtocolExportTool.Models;
using Spectre.Console;

namespace Fantasy.ProtocolExportTool.Abstract;

public abstract partial class AProtocolExporter( string protocolDirectory, string clientDirectory, string serverDirectory, ProtocolExportType protocolExportType)
{
    private readonly ConcurrentBag<string> _errors = new();
    private readonly Dictionary<string, int> _routeTypes = new();
    private readonly Dictionary<string, int> _roamingTypes = new();
    private readonly List<MessageDefinition> _outerMessages = [];
    private readonly List<MessageDefinition> _innerMessages = [];
    private readonly List<OpcodeInfo> _outerOpcode = [];
    private readonly List<OpcodeInfo> _innerOpcode = [];
    
    protected readonly string ProtocolDirectory = NormalizePath(protocolDirectory);
    protected readonly string ClientDirectory = NormalizePath(clientDirectory);
    protected readonly string ServerDirectory = NormalizePath(serverDirectory);
    
    private static string NormalizePath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Path cannot be null or empty", nameof(path)) : Path.GetFullPath(path.Trim());
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
        
        var template = GenerateOuterMessages(_outerMessages);
        
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
        
        var template = GenerateInnerMessages(_innerMessages);
        
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

    protected abstract string GenerateRouteTypes(IReadOnlyDictionary<string, int> routeTypes);
    protected abstract string GenerateRoamingTypes(IReadOnlyDictionary<string, int> roamingTypes);
    protected abstract string GenerateOuterOpcode(IReadOnlyList<OpcodeInfo> opcodeInfos);
    protected abstract string GenerateInnerOpcode(IReadOnlyList<OpcodeInfo> opcodeInfos);
    protected abstract string GenerateOuterMessages(IReadOnlyList<MessageDefinition> messageDefinitions);
    protected abstract string GenerateInnerMessages(IReadOnlyList<MessageDefinition> messageDefinitions);
    protected abstract string GenerateOuterMessageHelper(IReadOnlyList<MessageDefinition> messageDefinitions);

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
        ParseAndValidateProtocols("Outer", _outerOpcode, _outerMessages);
    }
    
    public void ParseAndValidateInnerProtocols()
    {
        ParseAndValidateProtocols("Inner", _innerOpcode, _innerMessages);
    }

    private void ParseAndValidateProtocols(string protocol, List<OpcodeInfo> opcodeInfo, List<MessageDefinition> allMessages)
    {
        var validator = new ProtocolValidator();
        var isOuter = protocol.Equals("Outer", StringComparison.OrdinalIgnoreCase);
        var opCodeGenerator = new OpCodeGenerator(isOuter);

        // 1. 解析所有文件
        foreach (var (filePath, fileLines) in ReadProtocolFilesLinesWithPath(protocol))
        {
            var parser = new ProtocolFileParser(filePath);
            var messages = parser.Parse(fileLines);

            // 收集解析错误
            foreach (var error in parser.GetErrors())
            {
                _errors.Add(error);
            }

            allMessages.AddRange(messages);
        }

        // 2. 为所有需要 OpCode 的消息生成 OpCode
        foreach (var message in allMessages.Where(m => m.HasOpCode))
        {
            message.OpCode = opCodeGenerator.Generate(message);
            opcodeInfo.Add(message.OpCode);
        }

        // 3. 验证所有消息
        foreach (var message in allMessages)
        {
            validator.Validate(message);
        }

        // 4. 收集验证错误
        foreach (var error in validator.GetErrors())
        {
            _errors.Add(error);
        }
    }
    
    private IEnumerable<(string FilePath, string[] Lines)> ReadProtocolFilesLinesWithPath(string protocolDir)
    {
        var protocolPath = Path.Combine(ProtocolDirectory, protocolDir);

        if (!Directory.Exists(protocolPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: {protocolDir} directory not found at {protocolPath}[/]");
            yield break;
        }

        var files = Directory.GetFiles(protocolPath, "*.proto", SearchOption.AllDirectories);

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