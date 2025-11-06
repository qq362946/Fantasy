# 服务器启动命令行参数配置

本指南将介绍如何配置 Fantasy 服务器的命令行参数,包括:
- 命令行参数说明
- 开发环境配置 (launchSettings.json)
- 生产环境配置
- 常用启动场景示例

> **📌 提示:** 本文档是 [编写启动代码](03-WritingStartupCode.md) 的延续,建议先阅读启动代码文档。

---

## 目录

- [命令行参数说明](#命令行参数说明)
  - [RuntimeMode (运行模式)](#runtimemode-运行模式)
  - [ProcessId (进程ID)](#processid-进程id)
  - [ProcessType (进程类型)](#processtype-进程类型)
  - [StartupGroup (启动组)](#startupgroup-启动组)
- [开发环境配置](#开发环境配置)
  - [Visual Studio / Rider 配置](#visual-studio--rider-配置)
  - [launchSettings.json 配置](#launchsettingsjson-配置)
  - [开发模式最佳实践](#开发模式最佳实践)
- [生产环境配置](#生产环境配置)
  - [命令行启动](#命令行启动)
  - [多进程部署](#多进程部署)
  - [Docker 部署](#docker-部署)
- [常用启动场景](#常用启动场景)
- [常见问题](#常见问题)

---

## 命令行参数说明

Fantasy 服务器通过 `CommandLineOptions` 类定义命令行参数,位于 `/Fantasy.Net/Fantasy.Net/Runtime/Core/Platform/Net/ProcessDefine.cs:25-51`。

### RuntimeMode (运行模式)

**参数:** `-m` 或 `--RuntimeMode`
**必填:** 是
**默认值:** `Release`
**可选值:** `Develop`, `Release`

控制服务器的运行模式:

| 模式 | 行为 | 适用场景 |
|------|------|---------|
| **Develop** | 启动 Fantasy.config 配置表中的**所有 Process** | 本地开发调试,所有进程在同一个进程内运行,方便调试 |
| **Release** | 根据 `ProcessId` 参数启动**单个 Process** | 生产环境,每个 Process 独立运行在不同的进程中 |

**示例:**
```bash
# 开发模式 - 启动所有进程
dotnet YourServer.dll --m Develop

# 发布模式 - 需要配合 ProcessId 使用
dotnet YourServer.dll --m Release --pid 1
```

---

### ProcessId (进程ID)

**参数:** `--pid`
**必填:** Release 模式下必填
**默认值:** 0
**类型:** uint (无符号整数)

指定要启动的 Process ID,该 ID 必须与 `Fantasy.config` 中的 `ProcessConfig` ID 对应。

**注意事项:**
- 只能传递单个 ID,不支持同时传递多个 ID
- ID 必须在 `Fantasy.config` 的 `<Process>` 配置中存在
- Develop 模式下会忽略此参数

**示例:**
```bash
# 启动 ProcessId = 1 的进程 (例如 Gate 服务器)
dotnet YourServer.dll --m Release --pid 1

# 启动 ProcessId = 2 的进程 (例如 Game 服务器)
dotnet YourServer.dll --m Release --pid 2
```

**对应 Fantasy.config 配置:**
```xml
<Process Id="1" MachineId="1" ProcessType="Game" InnerPort="20000">
    <Scene Id="1" SceneType="Gate" SceneSubType="Gate" />
</Process>
<Process Id="2" MachineId="1" ProcessType="Game" InnerPort="20001">
    <Scene Id="2" SceneType="Map" SceneSubType="None" />
</Process>
```

---

### ProcessType (进程类型)

**参数:** `-a` 或 `--ProcessType`
**必填:** 否
**默认值:** `Game`
**可选值:** `Game`, `Robot`

设置应用程序的类型:

| 类型 | 说明 | 状态 |
|------|------|------|
| **Game** | 游戏服务器进程 | ✅ 已实现 |
| **Robot** | 机器人客户端(压测工具) | ⚠️ 暂未支持 |

**示例:**
```bash
# 启动游戏服务器 (默认)
dotnet YourServer.dll --m Release --pid 1 -a Game

# 启动机器人客户端 (功能开发中)
dotnet YourServer.dll --m Release --pid 100 -a Robot
```

---

### StartupGroup (启动组)

**参数:** `-g` 或 `--StartupGroup`
**必填:** 否
**默认值:** 0
**类型:** int

用于批量启动一组 Process。可以在 `Fantasy.config` 中为 Process 分配组别,然后通过此参数启动整组进程。

**适用场景:**
- 分区分服部署(例如:区服1、区服2)
- 功能模块分组(例如:战斗服务器组、社交服务器组)
- 灰度发布(例如:测试组、稳定组)

**示例:**
```bash
# 启动组 1 的所有进程
dotnet YourServer.dll --m Release -g 1

# 启动组 2 的所有进程
dotnet YourServer.dll --m Release -g 2
```

**对应 Fantasy.config 配置示例:**
```xml
<!-- 组 1: 战斗服务器组 -->
<Process Id="10" MachineId="1" ProcessType="Game" StartupGroup="1" InnerPort="30000">
    <Scene Id="10" SceneType="Battle" SceneSubType="None" />
</Process>
<Process Id="11" MachineId="1" ProcessType="Game" StartupGroup="1" InnerPort="30001">
    <Scene Id="11" SceneType="Battle" SceneSubType="None" />
</Process>

<!-- 组 2: 社交服务器组 -->
<Process Id="20" MachineId="1" ProcessType="Game" StartupGroup="2" InnerPort="31000">
    <Scene Id="20" SceneType="Social" SceneSubType="None" />
</Process>
```

---

## 开发环境配置

### Visual Studio / Rider 配置

在 IDE 中调试时,推荐使用 `launchSettings.json` 配置命令行参数。

**Visual Studio:**
1. 右键项目 → 属性 → 调试 → 启动配置文件
2. 在 "命令行参数" 中填入 `--m Develop`

**Rider:**
1. Run → Edit Configurations
2. 在 "Program arguments" 中填入 `--m Develop`

---

### launchSettings.json 配置

`launchSettings.json` 是 .NET 项目的调试配置文件,位于项目的 `Properties` 目录下。

**文件位置:**
```
YourProject/
├── Program.cs
└── Properties/
    └── launchSettings.json
```

---

#### 创建 launchSettings.json 文件

**在 Visual Studio 中:**
1. 在项目的 `Properties` 文件夹上右键
2. 选择 "添加" → "新建项"
3. 搜索 "launchSettings" 或选择 "JSON 文件"
4. 命名为 `launchSettings.json`

**在 Rider 中:**
1. 在项目的 `Properties` 文件夹上右键
2. 选择 "New" → "File"
3. 命名为 `launchSettings.json`

**在 VS Code 中:**
1. 在项目的 `Properties` 文件夹上右键
2. 选择 "New File"
3. 命名为 `launchSettings.json`

> **💡 提示:** 如果项目中没有 `Properties` 文件夹，需要先创建这个文件夹。

---

**基础配置示例:**

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "Develop": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "commandLineArgs": "--m Develop"
    },
    "Release-Gate": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      },
      "commandLineArgs": "--m Release --pid 1"
    },
    "Release-Game": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      },
      "commandLineArgs": "--m Release --pid 2"
    }
  }
}
```

**多配置示例:**

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "开发模式 - 所有进程": {
      "commandName": "Project",
      "commandLineArgs": "--m Develop"
    },
    "发布模式 - Gate服务器": {
      "commandName": "Project",
      "commandLineArgs": "--m Release --pid 1"
    },
    "发布模式 - Map服务器": {
      "commandName": "Project",
      "commandLineArgs": "--m Release --pid 2"
    },
    "发布模式 - 战斗服务器组": {
      "commandName": "Project",
      "commandLineArgs": "--m Release -g 1"
    },
    "自定义配置": {
      "commandName": "Project",
      "commandLineArgs": "--m Release --pid 10 -a Game",
      "environmentVariables": {
        "LOG_LEVEL": "Debug"
      }
    }
  }
}
```

**配置说明:**

| 字段 | 说明 | 必填 |
|------|------|------|
| `commandName` | 启动方式,通常为 `Project` | 是 |
| `commandLineArgs` | 命令行参数 | 否 |
| `environmentVariables` | 环境变量 | 否 |
| `workingDirectory` | 工作目录 | 否 |

---

### 开发模式最佳实践

**推荐配置:**
```json
{
  "profiles": {
    "Develop": {
      "commandName": "Project",
      "commandLineArgs": "--m Develop"
    }
  }
}
```

**开发模式的优势:**

1. **单进程调试**
   - 所有 Process 和 Scene 运行在同一个进程内
   - 可以使用断点调试所有逻辑
   - 避免多进程调试的复杂性

2. **快速启动**
   - 一次启动即可运行完整服务器
   - 无需手动启动多个进程
   - 节省开发时间

3. **日志集中**
   - 所有日志输出在同一个控制台
   - 方便跟踪调用链路
   - 易于定位问题

**注意事项:**

- ⚠️ 开发模式会忽略 `--pid` 参数
- ⚠️ 性能表现与生产环境不同,不适合性能测试
- ⚠️ 进程间通信仍然通过网络,确保端口未被占用

---

## 生产环境配置

### 命令行启动

在生产环境中,通常使用命令行启动服务器,每个 Process 独立运行在不同的进程中。

**基础启动命令:**

```bash
# 1. 构建项目
dotnet build --configuration Release

# 2. 进入输出目录
cd bin/Release/net8.0/

# 3. 启动服务器
dotnet YourServer.dll --m Release --pid 1
```

**或者先发布项目:**

```bash
# 1. 发布项目 (用于生成自包含部署包)
dotnet publish --configuration Release --output ./publish

# 2. 进入发布目录
cd ./publish

# 3. 启动服务器
dotnet YourServer.dll --m Release --pid 1
```

**多个服务器启动示例:**

```bash
# 启动 Gate 服务器 (ProcessId = 1)
dotnet YourServer.dll --m Release --pid 1

# 启动 Map 服务器 (ProcessId = 2, 在另一个终端或后台运行)
dotnet YourServer.dll --m Release --pid 2
```

---

### 多进程部署

在生产环境中,通常需要启动多个进程,每个进程负责不同的功能模块。

**方案 1: 使用 Shell 脚本**

`start-servers.sh`:
```bash
#!/bin/bash

# 服务器可执行文件路径
SERVER_DLL="./YourServer.dll"

# 日志目录
LOG_DIR="./logs"
mkdir -p $LOG_DIR

# 启动 Gate 服务器 (ProcessId = 1)
echo "启动 Gate 服务器..."
nohup dotnet $SERVER_DLL --m Release --pid 1 > $LOG_DIR/gate.log 2>&1 &

# 启动 Map 服务器 (ProcessId = 2)
echo "启动 Map 服务器..."
nohup dotnet $SERVER_DLL --m Release --pid 2 > $LOG_DIR/map.log 2>&1 &

# 启动 Battle 服务器 (ProcessId = 3)
echo "启动 Battle 服务器..."
nohup dotnet $SERVER_DLL --m Release --pid 3 > $LOG_DIR/battle.log 2>&1 &

echo "所有服务器已启动"
ps aux | grep "dotnet.*YourServer.dll"
```

**使用方法:**
```bash
chmod +x start-servers.sh
./start-servers.sh
```

**停止服务器:**
```bash
pkill -f "dotnet.*YourServer.dll"
```

---

**方案 2: 使用 systemd (Linux)**

`/etc/systemd/system/fantasy-gate.service`:
```ini
[Unit]
Description=Fantasy Gate Server
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=/opt/fantasy-server
ExecStart=/usr/bin/dotnet /opt/fantasy-server/YourServer.dll --m Release --pid 1
Restart=on-failure
RestartSec=10
Environment="DOTNET_ENVIRONMENT=Production"

[Install]
WantedBy=multi-user.target
```

**使用方法:**
```bash
# 启动服务
sudo systemctl start fantasy-gate

# 停止服务
sudo systemctl stop fantasy-gate

# 查看状态
sudo systemctl status fantasy-gate

# 开机自启
sudo systemctl enable fantasy-gate

# 查看日志
sudo journalctl -u fantasy-gate -f
```

---

**方案 3: 使用进程管理器 (PM2)**

虽然 PM2 主要用于 Node.js,但也可以管理 .NET 应用:

`ecosystem.config.js`:
```javascript
module.exports = {
  apps: [
    {
      name: 'fantasy-gate',
      script: 'dotnet',
      args: 'YourServer.dll --m Release --pid 1',
      cwd: '/opt/fantasy-server',
      instances: 1,
      autorestart: true,
      watch: false,
      max_memory_restart: '1G',
      env: {
        DOTNET_ENVIRONMENT: 'Production'
      }
    },
    {
      name: 'fantasy-map',
      script: 'dotnet',
      args: 'YourServer.dll --m Release --pid 2',
      cwd: '/opt/fantasy-server',
      instances: 1,
      autorestart: true,
      watch: false,
      max_memory_restart: '1G',
      env: {
        DOTNET_ENVIRONMENT: 'Production'
      }
    }
  ]
};
```

**使用方法:**
```bash
# 启动所有服务器
pm2 start ecosystem.config.js

# 停止所有服务器
pm2 stop all

# 重启所有服务器
pm2 restart all

# 查看状态
pm2 status

# 查看日志
pm2 logs
```

---

### Docker 部署

**Dockerfile 示例:**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["YourServer/YourServer.csproj", "YourServer/"]
RUN dotnet restore "YourServer/YourServer.csproj"
COPY . .
WORKDIR "/src/YourServer"
RUN dotnet build "YourServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "YourServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 默认启动参数 (可通过 docker run 覆盖)
ENV RUNTIME_MODE=Release
ENV PROCESS_ID=1

ENTRYPOINT ["sh", "-c", "dotnet YourServer.dll --m $RUNTIME_MODE --pid $PROCESS_ID"]
```

**docker-compose.yml 示例:**

```yaml
version: '3.8'

services:
  fantasy-gate:
    build: .
    container_name: fantasy-gate
    environment:
      - RUNTIME_MODE=Release
      - PROCESS_ID=1
      - DOTNET_ENVIRONMENT=Production
    ports:
      - "20000:20000"
    restart: unless-stopped
    volumes:
      - ./logs:/app/logs

  fantasy-map:
    build: .
    container_name: fantasy-map
    environment:
      - RUNTIME_MODE=Release
      - PROCESS_ID=2
      - DOTNET_ENVIRONMENT=Production
    ports:
      - "20001:20001"
    restart: unless-stopped
    volumes:
      - ./logs:/app/logs

  fantasy-battle:
    build: .
    container_name: fantasy-battle
    environment:
      - RUNTIME_MODE=Release
      - PROCESS_ID=3
      - DOTNET_ENVIRONMENT=Production
    ports:
      - "20002:20002"
    restart: unless-stopped
    volumes:
      - ./logs:/app/logs
```

**使用方法:**
```bash
# 构建镜像
docker-compose build

# 启动所有服务器
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止所有服务器
docker-compose down

# 重启特定服务器
docker-compose restart fantasy-gate
```

**直接使用 Docker 命令:**
```bash
# 构建镜像
docker build -t fantasy-server .

# 启动 Gate 服务器
docker run -d \
  --name fantasy-gate \
  -e RUNTIME_MODE=Release \
  -e PROCESS_ID=1 \
  -p 20000:20000 \
  fantasy-server

# 启动 Map 服务器
docker run -d \
  --name fantasy-map \
  -e RUNTIME_MODE=Release \
  -e PROCESS_ID=2 \
  -p 20001:20001 \
  fantasy-server
```

---

## 常用启动场景

### 场景 1: 本地全功能开发

**需求:** 在本地调试完整的服务器功能

**配置:**
```json
{
  "profiles": {
    "Develop": {
      "commandName": "Project",
      "commandLineArgs": "--m Develop"
    }
  }
}
```

**启动:**
```bash
# 进入输出目录
cd bin/Debug/net8.0/

# 启动服务器
dotnet YourServer.dll --m Develop
```

**特点:**
- ✅ 所有 Process 在同一个进程中
- ✅ 可以使用断点调试
- ✅ 适合功能开发和调试

---

### 场景 2: 模拟生产环境测试

**需求:** 在本地测试多进程部署

**配置:**
```json
{
  "profiles": {
    "Gate": {
      "commandName": "Project",
      "commandLineArgs": "--m Release --pid 1"
    },
    "Map": {
      "commandName": "Project",
      "commandLineArgs": "--m Release --pid 2"
    }
  }
}
```

**启动 (需要多个终端):**
```bash
# 终端 1: 启动 Gate 服务器
cd bin/Release/net8.0/
dotnet YourServer.dll --m Release --pid 1

# 终端 2: 启动 Map 服务器
cd bin/Release/net8.0/
dotnet YourServer.dll --m Release --pid 2
```

**特点:**
- ✅ 每个 Process 独立运行
- ✅ 模拟真实的生产环境
- ✅ 适合集成测试和性能测试

---

### 场景 3: 单个服务器调试

**需求:** 只调试某个特定的服务器

**配置:**
```json
{
  "profiles": {
    "Debug-Gate": {
      "commandName": "Project",
      "commandLineArgs": "--m Release --pid 1"
    }
  }
}
```

**启动:**
```bash
# 进入输出目录
cd bin/Release/net8.0/

# 启动特定服务器
dotnet YourServer.dll --m Release --pid 1
```

**特点:**
- ✅ 只启动需要调试的服务器
- ✅ 节省资源
- ✅ 适合单模块开发

---

### 场景 4: 启动服务器组

**需求:** 批量启动一组相关的服务器

**配置:**
```json
{
  "profiles": {
    "BattleGroup": {
      "commandName": "Project",
      "commandLineArgs": "--m Release -g 1"
    }
  }
}
```

**启动:**
```bash
# 进入输出目录
cd bin/Release/net8.0/

# 启动服务器组
dotnet YourServer.dll --m Release -g 1
```

**特点:**
- ✅ 批量启动相关服务器
- ✅ 方便功能模块测试
- ✅ 适合灰度发布和分区分服

---

### 场景 5: CI/CD 自动化部署

**需求:** 在 CI/CD 流程中自动启动服务器

**GitLab CI 示例 (.gitlab-ci.yml):**
```yaml
deploy:
  stage: deploy
  script:
    - dotnet publish -c Release -o ./publish
    - ssh user@server "systemctl stop fantasy-*"
    - scp -r ./publish/* user@server:/opt/fantasy-server/
    - ssh user@server "systemctl start fantasy-gate fantasy-map"
  only:
    - main
```

**GitHub Actions 示例 (.github/workflows/deploy.yml):**
```yaml
name: Deploy

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 8.0.x
      - name: Publish
        run: dotnet publish -c Release -o ./publish
      - name: Deploy
        run: |
          ssh user@server "systemctl stop fantasy-*"
          scp -r ./publish/* user@server:/opt/fantasy-server/
          ssh user@server "systemctl start fantasy-gate fantasy-map"
```

---

## 常见问题

### Q1: 开发模式下为什么需要 --m Develop 参数?

**原因:**

Fantasy 框架的设计理念是:
- **Develop 模式**: 所有 Process 运行在同一个进程内,方便本地开发调试
- **Release 模式**: 每个 Process 独立运行,模拟真实的生产环境

不传递 `-m` 参数会导致参数解析失败,因为该参数被标记为 `Required = true`。

**解决方案:**

始终传递 `--m Develop` 或 `--m Release` 参数。

---

### Q2: 如何在不同环境使用不同的配置文件?

**方案 1: 使用环境变量**

```json
{
  "profiles": {
    "Development": {
      "commandName": "Project",
      "commandLineArgs": "--m Develop",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "CONFIG_FILE": "Fantasy.Development.config"
      }
    },
    "Production": {
      "commandName": "Project",
      "commandLineArgs": "--m Release --pid 1",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production",
        "CONFIG_FILE": "Fantasy.Production.config"
      }
    }
  }
}
```

**在代码中读取:**
```csharp
var configFile = Environment.GetEnvironmentVariable("CONFIG_FILE") ?? "Fantasy.config";
// 使用 configFile 加载配置
```

**方案 2: 使用不同的工作目录**

```json
{
  "profiles": {
    "Development": {
      "commandName": "Project",
      "commandLineArgs": "--m Develop",
      "workingDirectory": "./configs/dev"
    },
    "Production": {
      "commandName": "Project",
      "commandLineArgs": "--m Release --pid 1",
      "workingDirectory": "./configs/prod"
    }
  }
}
```

---

### Q3: Release 模式下如何快速启动多个服务器?

**方案 1: 使用 Shell 脚本 (推荐)**

参考 [多进程部署](#多进程部署) 章节。

**方案 2: 使用 Visual Studio 多启动项目**

1. 右键解决方案 → 属性 → 启动项目
2. 选择 "多个启动项目"
3. 为每个项目配置不同的 `launchSettings.json` profile

**方案 3: 使用 Docker Compose (推荐)**

参考 [Docker 部署](#docker-部署) 章节。

---

### Q4: 如何在 Rider 中切换不同的启动配置?

**步骤:**

1. 打开 Run/Debug Configurations (Run → Edit Configurations)
2. 点击 `+` 添加新的 .NET Project 配置
3. 为每个配置设置不同的 "Program arguments"
4. 在 Run 菜单中选择对应的配置

**配置示例:**

| 配置名称 | Program arguments |
|---------|-------------------|
| Develop | `--m Develop` |
| Release - Gate | `--m Release --pid 1` |
| Release - Map | `--m Release --pid 2` |

---

### Q5: 如何验证服务器启动成功?

**方法 1: 查看日志输出**

```
[INFO] 加载程序集:Entity
[INFO] 加载程序集:Hotfix
[INFO] Fantasy.Net 初始化完成
[INFO] 场景创建:SceneId=1001, SceneType=Gate
[INFO] Gate 场景监听:0.0.0.0:20000 (KCP)
[INFO] 服务器启动完成
```

**方法 2: 检查端口监听**

```bash
# Linux/Mac
netstat -tuln | grep 20000

# Windows
netstat -ano | findstr 20000

# 使用 lsof (Mac/Linux)
lsof -i :20000
```

**方法 3: 使用进程管理器**

```bash
# 查看进程
ps aux | grep "dotnet.*YourServer.dll"

# 使用 systemd
sudo systemctl status fantasy-gate
```

---

### Q6: ProcessId 在配置文件中找不到会怎样?

**错误信息:**
```
Error: Process with ID 999 not found in Fantasy.config
```

**原因:**

传递的 `--pid` 参数在 `Fantasy.config` 中没有对应的 `<Process Id="999" ...>` 配置。

**解决:**

1. 检查 `Fantasy.config` 确认 ProcessId
2. 使用正确的 ProcessId 启动服务器
3. 或在 `Fantasy.config` 中添加缺失的 Process 配置

---

### Q7: 开发模式下端口被占用怎么办?

**错误信息:**
```
System.Net.Sockets.SocketException: Address already in use
```

**原因:**

Develop 模式会启动所有 Process,可能导致端口冲突。

**解决方案:**

1. **检查端口占用:**
   ```bash
   # Linux/Mac
   lsof -i :20000

   # Windows
   netstat -ano | findstr 20000
   ```

2. **终止占用端口的进程:**
   ```bash
   # Linux/Mac
   kill -9 <PID>

   # Windows
   taskkill /PID <PID> /F
   ```

3. **修改 Fantasy.config 中的端口配置:**
   ```xml
   <Process Id="1" MachineId="1" ProcessType="Game" InnerPort="20000">
   ```

---

## 下一步

现在你已经掌握了命令行参数配置,接下来可以:

1. 🎯 阅读 [OnCreateScene 事件使用指南](05-OnCreateScene.md) 学习如何在场景启动时初始化逻辑
2. 📖 阅读 [配置系统使用指南](02-ConfigUsage.md) 学习如何在代码中使用配置
3. 🚀 尝试在生产环境部署服务器
4. 🐳 尝试使用 Docker 容器化部署
5. 📚 查看 `Examples/Server` 目录下的完整示例

## 获取帮助

- **GitHub**: https://github.com/qq362946/Fantasy
- **文档**: https://www.code-fantasy.com/
- **Issues**: https://github.com/qq362946/Fantasy/issues

---
