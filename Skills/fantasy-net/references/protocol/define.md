# Protocol Define — 定义网络协议

## Workflow

### 第 1 步：确定协议目录位置

搜索项目中的 `Fantasy.ProtocolExportTool.dll`，检查其所在目录是否**同时存在** `Avalonia.dll`：

- **同目录同时有 `Avalonia.dll`** → 可视化编辑器（Fantasy.ProtocolEditor），命令行工具未安装，询问用户协议文件放在哪个目录，以用户提供的路径作为协议根目录，进入第 2 步
- **同目录没有 `Avalonia.dll`** → 命令行工具，读取同目录下的 `ExporterSettings.json`，取 `NetworkProtocolDirectory` 字段的 `Value` 作为协议根目录，进入第 2 步
- **完全找不到 `Fantasy.ProtocolExportTool.dll`** → 说明未安装任何导出工具，引导阅读 `references/protocol/export-install.md`，**不要继续往下读**

### 第 2 步：确认/创建 Outer 和 Inner 子目录

协议根目录下必须按通信方向分开存放：

```
{协议根目录}/
├── Outer/    # 客户端 ↔ 服务器之间的所有消息（含服务器主动推送给客户端）
└── Inner/    # 服务器 ↔ 服务器消息（按需创建）
```

不存在时创建：

```bash
mkdir -p "{协议根目录}/Outer"
mkdir -p "{协议根目录}/Inner"
```

### 第 3 步：确认需要定义的协议类型

```
用户需要定义哪类协议？
│
├─► 客户端 ↔ 服务器（外网）──► 阅读 references/protocol/define-outer.md
└─► 服务器 ↔ 服务器（内网）──► 阅读 references/protocol/define-inner.md
```

两类都需要时，分别读取对应文件。

### 第 4 步：定义完成后

协议写入 `.proto` 文件后，运行导出工具生成 C# 类，见 `references/protocol/export.md`。
