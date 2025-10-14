namespace Fantasy.Tools.ProtocalExporter;

public static class NetworkProtocolTemplate
{
    private static string? _cachedTemplate;

    public static string Template
    {
        get
        {
            if (_cachedTemplate != null)
            {
                return _cachedTemplate;
            }

            // 尝试从多个位置读取模板文件
            var possiblePaths = new[]
            {
                Path.Combine(ExporterAges.Instance.Folder ?? Directory.GetCurrentDirectory(), "NetworkProtocolTemplate.txt"),
                Path.Combine(Directory.GetCurrentDirectory(), "NetworkProtocolTemplate.txt"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NetworkProtocolTemplate.txt")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _cachedTemplate = File.ReadAllText(path);
                    return _cachedTemplate;
                }
            }

            throw new FileNotFoundException("找不到 NetworkProtocolTemplate.txt 模板文件！\n" +
                                           "请确保模板文件位于以下位置之一：\n" +
                                           string.Join("\n", possiblePaths));
        }
    }
}