# Fantasy Protocol Export MCP 服务

## 简介

Fantasy Protocol Export Tool 支持作为 MCP (Model Context Protocol) 服务运行，允许 OpenCode（或其它支持 MCP 的编程客户端如 Cursor、Claude Code、Codex、Github Copilot 等）直接调用协议导出功能。

## 启动方式

确保配置好 MCP 客户端后（以 OpenCode 为例，Cursor、Claude Code、Codex 等类似），**请不要手动启动**：
- 给模型发消息, 如"调用一次Fnatasy协议导出工具并返回", 
- 模型应该会触发一次导出, 并返回导出消息。

## MCP 客户端配置

（以 OpenCode 为例，Cursor、Claude Code、Codex 等类似）

在您使用的 MCP 客户端配置文件中添加（各客户端配置格式可能略有差异,如有特定客户端特殊字段请自行搜索）：

```json
{
  "mcp": {
    "fantasy-protocol-to-code": {
      "type": "local",
      "command": ["dotnet", "<dll路径>","mcp", "-c", "<协议配置路径>"],
      "enabled": true
    }
  }
}
```
**替换步骤**：
- 将 `<dll路径>`替换为实际dll绝对路径，如 `F:MyPath/Fantasy.ProtocolExportTool/bin/Debug/net8.0/Fantasy.ProtocolExportTool.dll`;
- 通过将 `<协议配置路径>` 替换为自定义路径, 指定 ExporterSettings.json 文件所在;
- 以上两个步骤都不能省!! (否则模型无法正确启用 MCP)


## 工具调用 (模型会自行识别, 如果没识别到, 检查上述的配置有没有做对)

### export_protocols

导出 Fantasy 网络协议文件为 C# 类。

**参数：**
- `exportType` (string, 可选): 导出类型，`"Client"`、`"Server"` 或 `"All"`(默认)

**使用示例：**
```json
{
  "tool": "export_protocols",
  "arguments": {}
}

// 仅客户端
{
  "tool": "export_protocols",
  "arguments": { "exportType": "Client" }
}
```

**模型接收的返回消息：**
```
✓ 协议导出成功

导出配置:
  导出类型: 双端
  协议目录: F:/.../NetworkProtocol
  服务器目录: F:/.../NetworkProtocol
  客户端目录: F:/.../NetworkProtocol
```

## 运行流程

1. 启动时通过 `-c` 参数加载 `ExporterSettings.json` 配置文件
2. 执行首次协议导出（失败会警告）
3. 通过 stdio 与 MCP 客户端通信
4. 等待并处理工具调用请求

**注意**：MCP 服务必须保持运行，进程生命周期建议由Agent客户端管理。

## 故障排除

### MCP 连接失败

1. 检查dll路径文件路径是否正确
2. 检查配置文件完整路径
3. 检查服务是否正在运行

### 导出失败

检查：
1. 协议目录是否存在且包含 `.proto` 文件
2. 服务器和客户端目录是否有写入权限
3. 路径配置是否正确