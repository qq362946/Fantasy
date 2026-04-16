# Protocol Export Install — 导出工具安装与配置

> **⚠️ 必须先安装导出工具，没有导出工具 AI 无法帮你生成协议 C# 类。**

选择以下任意一种方式安装：

---

## 方式 1：Fantasy CLI（推荐）

```bash
# 安装 Fantasy CLI（已装过可跳过）
dotnet tool install -g Fantasy.Cli

# macOS/Linux 若 fantasy 命令不可用，需将 dotnet tools 加入 PATH：
# export PATH="$PATH:$HOME/.dotnet/tools"
# 永久生效：写入 ~/.zshrc 或 ~/.bashrc

# 验证安装
fantasy --version

# 在项目根目录安装协议导出工具
fantasy add -t protocolexporttool
```

工具会安装到 `Tools/Exporter/NetworkProtocol/`，并自动生成 `ExporterSettings.json`。

---

## 方式 2：从 GitHub 仓库下载

前往 https://github.com/qq362946/Fantasy 下载或 Clone 整个仓库，工具已预编译好，位于仓库的 `Tools/ProtocolExportTool/` 目录，直接使用，无需编译。

---

## 方式 3：下载可视化编辑器（含导出功能）

- 百度网盘：https://pan.baidu.com/s/1eGk-e8dkkU7QamsSRZqojQ?pwd=niyx（提取码：niyx）
- QQ 群文件（群号 569888673）→「框架工具」目录中下载

---

## 安装后：初始配置

### 命令行工具（方式 1 / 方式 2）

找到工具目录下的 `ExporterSettings.json`，将三个路径字段填写为实际的绝对路径：

```json
{
    "Export": {
        "NetworkProtocolDirectory": {
            "Value": "/绝对路径/到/proto文件目录",
            "Comment": "ProtoBuf文件所在的文件夹位置"
        },
        "NetworkProtocolServerDirectory": {
            "Value": "/绝对路径/到/服务端生成目录",
            "Comment": "ProtoBuf生成到服务端的文件夹位置"
        },
        "NetworkProtocolClientDirectory": {
            "Value": "/绝对路径/到/客户端生成目录",
            "Comment": "ProtoBuf生成到客户端的文件夹位置"
        }
    }
}
```

**确定各路径的方法：**
- `NetworkProtocolDirectory`：存放 `.proto` 文件的目录（见 `references/protocol/define.md`）
- `NetworkProtocolServerDirectory`：搜索项目中已有的 `Generate/NetworkProtocol` 目录；如无则询问用户
- `NetworkProtocolClientDirectory`：无 Unity 客户端时，填与服务端相同的路径即可

### 可视化编辑器（方式 3）

打开编辑器后：
1. `文件 → 打开工作区` — 选择存放 `.proto` 文件的目录
2. `文件 → 导出设置` — 配置服务端和客户端的输出路径

---

配置完成后，回到 `references/protocol/export.md` 继续执行导出流程。
