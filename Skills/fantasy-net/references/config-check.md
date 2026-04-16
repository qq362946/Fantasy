# Fantasy.config 审查清单

**本文件用于检查 Fantasy.config 是否正确。**

## 检查顺序

1. 是否仍在使用 `<configTable>`
2. machine / process / world / scene / database 引用关系是否正确
3. 端口和协议是否匹配
4. world 模式 ID 范围是否越界
5. 配置和运行时语义是否一致

## 常见问题

### 错误 1：仍在使用 `<configTable>`

当前统一使用 `<server>` 方式。

### 错误 2：ID 为 0 或重复

检查：

- `machine.id`
- `process.id`
- `world.id`
- `scene.id`

### 错误 3：引用关系断裂

重点检查：

- `process.machineId`
- `scene.processConfigId`
- `scene.worldConfigId`

### 错误 4：端口和协议不匹配

- `innerPort` 不能为 0
- `outerPort > 0` 时必须配置 `networkProtocol`
- 同一机器上端口不能冲突

### 错误 5：`<database>` 放错位置

数据库配置挂在 `<world>` 下，不是 `<scene>` 下。

### 错误 6：`idFactory type="World"` 时 ID 越界

此时 `worldId` 和 `sceneId` 范围要满足约束。

### 错误 7：存在 world / database / scene 的悬空配置

重点检查：

- 配了 `world` 但没有任何 `scene` 绑定到它
- 配了 `<database>` 但当前没有任何 Scene 会通过该 `world` 使用它
- 这类配置如果只是预留，应明确注释；否则容易误导后续维护者

## 审查时重点问自己

1. 配置是否能正确推导出运行时 Scene 和 World 结构
2. 端口和网络协议是否可实际启动
3. world 和 database 的关系是否清晰
4. 是否存在“看起来已启用、其实没有任何运行时场景使用”的悬空配置

## 相关文档

- `config.md`
- `config-scenarios.md`
- `templates/Fantasy.config`
