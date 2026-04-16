# Protocol Export — 导出工具使用

## Workflow

### 第 1 步：检测命令行工具是否已安装

搜索项目中是否存在 `Fantasy.ProtocolExportTool.dll`（通常在 `Tools/ProtocolExportTool/` 或 `Tools/Exporter/NetworkProtocol/` 下）。

- **已安装** → 直接进入第 2 步
- **未安装** → 阅读 `references/protocol/export-install.md` 完成安装和初始配置后再回来，**不要继续往下读**

> **注意**：可视化编辑器（Fantasy.ProtocolEditor）需要人工手动操作，AI 无法代为执行导出。如果用户目前只有可视化编辑器，同样需要先安装命令行工具。

---

### 第 2 步：确认 ExporterSettings.json 已配置

确认 `ExporterSettings.json` 中三个路径已填写且指向实际存在的目录。

- **已配置** → 直接进入第 3 步
- **未配置** → 阅读 `references/protocol/export-install.md` 的"安装后：初始配置"章节，**不要继续往下读**

---

### 第 3 步：运行导出

在工具目录执行：

```bash
./Run.sh                                               # macOS/Linux（如果有 Run.sh）
dotnet Fantasy.ProtocolExportTool.dll export --silent  # 跨平台静默模式
```

---

### 第 4 步：验证生成结果

导出成功后，项目中会出现（或更新）对应的 `.cs` 文件。

> **不要手动修改这些生成文件**，每次导出都会覆盖。

编译确认无报错：

```bash
dotnet build {解决方案}.sln
```
