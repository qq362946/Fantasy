using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Fantasy.ProtocolExportTool.Services;

/// <summary>
/// 一次协议导出期间共享的 OpCode 缓存会话。
/// 会话持有跨进程文件锁，并且只在显式提交时原子更新缓存文件。
/// </summary>
public sealed class OpCodeCacheSession : IDisposable
{
    private const string ReservedPrefix = "@reserved:";
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    private readonly string _cachePath;
    private readonly FileStream _lockStream;
    private readonly Dictionary<string, uint> _codes = new(StringComparer.Ordinal);
    private readonly Dictionary<uint, string> _codeOwners = [];
    private readonly Dictionary<uint, string> _reservedCodeOwners = [];
    private bool _changed;
    private bool _disposed;

    private OpCodeCacheSession(string cachePath, FileStream lockStream)
    {
        _cachePath = cachePath;
        _lockStream = lockStream;
        Load();
    }

    public IReadOnlyDictionary<string, uint> Codes => _codes;
    public IReadOnlyDictionary<uint, string> CodeOwners => _codeOwners;

    public static OpCodeCacheSession Open(string cachePath)
    {
        if (string.IsNullOrWhiteSpace(cachePath))
        {
            throw new ArgumentException("OpCode 缓存路径不能为空。", nameof(cachePath));
        }

        var fullPath = Path.GetFullPath(cachePath.Trim());
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lockPath = $"{fullPath}.lock";
        FileStream lockStream;
        try
        {
            lockStream = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                1,
                FileOptions.DeleteOnClose);
        }
        catch (IOException ex)
        {
            throw new IOException($"OpCode 缓存正在被另一个导出进程使用，请稍后重试：{fullPath}", ex);
        }

        try
        {
            return new OpCodeCacheSession(fullPath, lockStream);
        }
        catch
        {
            lockStream.Dispose();
            throw;
        }
    }

    public void Assign(string messageName, uint code)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_codeOwners.TryGetValue(code, out var codeOwner) &&
            (!_codes.TryGetValue(messageName, out var currentCode) || currentCode != code))
        {
            throw new InvalidOperationException($"无法为协议 '{messageName}' 分配 OpCode {code}，该编号已由 '{codeOwner}' 使用。");
        }

        if (_codes.TryGetValue(messageName, out var previousCode))
        {
            if (previousCode == code)
            {
                return;
            }

            _reservedCodeOwners[previousCode] = messageName;
            _codeOwners[previousCode] = CreateReservedKey(previousCode, messageName);
        }

        _codes[messageName] = code;
        _codeOwners[code] = messageName;
        _changed = true;
    }

    public void Commit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_changed && File.Exists(_cachePath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(_cachePath) ?? Directory.GetCurrentDirectory();
        var tempFilePath = Path.Combine(
            directory,
            $".{Path.GetFileName(_cachePath)}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, Utf8WithoutBom, 1024, true))
                {
                    writer.WriteLine("// Fantasy OpCode cache. Generated automatically; do not edit manually.");

                    foreach (var (name, value) in _codes.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                    {
                        writer.WriteLine($"{name} = {value}");
                    }

                    foreach (var (value, owner) in _reservedCodeOwners.OrderBy(pair => pair.Key))
                    {
                        writer.WriteLine($"{CreateReservedKey(value, owner)} = {value}");
                    }

                    writer.Flush();
                }

                stream.Flush(true);
            }

            File.Move(tempFilePath, _cachePath, true);
            _changed = false;
        }
        finally
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch
            {
                // 临时文件清理失败不能掩盖原始保存结果。
            }
        }
    }

    private void Load()
    {
        if (!File.Exists(_cachePath))
        {
            return;
        }

        var lines = File.ReadAllLines(_cachePath);
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var rawLine = lines[lineIndex];
            var line = rawLine.Trim();
            var lineNumber = lineIndex + 1;

            if (string.IsNullOrWhiteSpace(line) ||
                line.StartsWith("//", StringComparison.Ordinal) ||
                line is "{" or "}" or "{}")
            {
                continue;
            }

            var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                line = line[..commentIndex].Trim();
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0 || line.IndexOf('=', separatorIndex + 1) >= 0)
            {
                ThrowInvalidLine(lineNumber, rawLine, "应使用 '协议名 = 数字' 格式");
            }

            var name = line[..separatorIndex].Trim();
            var valueText = line[(separatorIndex + 1)..].Trim();
            if (valueText.EndsWith(",", StringComparison.Ordinal))
            {
                valueText = valueText[..^1].TrimEnd();
            }

            if (!uint.TryParse(valueText, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                ThrowInvalidLine(lineNumber, rawLine, "OpCode 必须是 uint 数字");
            }

            if (name.StartsWith(ReservedPrefix, StringComparison.Ordinal))
            {
                LoadReservedCode(name, value, lineNumber, rawLine);
                continue;
            }

            if (!IsValidMessageName(name))
            {
                ThrowInvalidLine(lineNumber, rawLine, "协议名不是有效标识符");
            }

            if (_codes.ContainsKey(name))
            {
                ThrowInvalidLine(lineNumber, rawLine, $"协议名 '{name}' 重复");
            }

            AddCodeOwner(value, name, lineNumber, rawLine);
            _codes.Add(name, value);
        }
    }

    private void LoadReservedCode(string key, uint value, int lineNumber, string rawLine)
    {
        var parts = key.Split(':', 3, StringSplitOptions.None);
        if (parts.Length != 3 ||
            !uint.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var encodedValue) ||
            encodedValue != value ||
            !IsValidMessageName(parts[2]))
        {
            ThrowInvalidLine(lineNumber, rawLine, "历史保留项格式应为 '@reserved:编号:原协议名 = 编号'");
        }

        AddCodeOwner(value, key, lineNumber, rawLine);
        _reservedCodeOwners.Add(value, parts[2]);
    }

    private void AddCodeOwner(uint value, string owner, int lineNumber, string rawLine)
    {
        if (_codeOwners.TryGetValue(value, out var existingOwner))
        {
            ThrowInvalidLine(lineNumber, rawLine, $"OpCode {value} 已由 '{existingOwner}' 使用");
        }

        _codeOwners.Add(value, owner);
    }

    private void ThrowInvalidLine(int lineNumber, string rawLine, string reason)
    {
        throw new InvalidDataException($"OpCode 缓存格式错误：{_cachePath} 第 {lineNumber} 行，{reason}。内容：{rawLine.Trim()}");
    }

    private static string CreateReservedKey(uint value, string owner)
    {
        return $"{ReservedPrefix}{value}:{owner}";
    }

    private static bool IsValidMessageName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !(char.IsLetter(name[0]) || name[0] == '_'))
        {
            return false;
        }

        return name.Skip(1).All(character => char.IsLetterOrDigit(character) || character == '_');
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lockStream.Dispose();
    }
}
