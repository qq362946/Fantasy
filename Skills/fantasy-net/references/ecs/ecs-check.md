# ECS 审查清单

**本文件用于检查已有的 ECS 代码。**

## 检查顺序

1. 类定义是否符合 Fantasy 约定
2. Entity / Component 归属和层级是否正确
3. 是否遗漏关键生命周期 System
4. 是否存在对象池复用风险
5. 是否把 Scene / SubScene / Component 的职责混乱使用

## 常见问题

### 错误 1：Entity / Component 不是 `sealed class`

- `Entity` / `Component` 应为 `sealed class`
- 不要把普通业务类误写成继承 `Entity`

### 错误 2：Entity / Component 不属于正确的 `Scene`

- 每个 Entity 都必须归属于某个 Scene
- 不要跨 Scene 持有并长期使用另一个 Scene 的运行时对象引用

### 错误 3：该有 `AwakeSystem` / `DestroySystem` 却没写

重点检查：

- 需要初始化默认值、注册逻辑、添加子组件 -> 应考虑 `AwakeSystem`
- 有自定义字段、对象池复用、清理逻辑 -> 应考虑 `DestroySystem`
- 需要逐帧逻辑 -> 才考虑 `UpdateSystem`

### 错误 4：对象池复用但没重置自定义字段

如果 Entity 走对象池：

- 自定义字段必须在 `DestroySystem` 里重置
- 否则下一次复用可能带脏数据

### 错误 5：把 Component 当普通字段容器，而不是 Entity 层级节点

在 Fantasy 里，Component 本质也是 Entity。

重点检查：

- 是否通过 `AddComponent<T>()` 挂载
- 是否误手动 new Component
- 是否正确使用父子关系和生命周期

### 错误 6：SubScene 和普通 Scene 的职责混淆

如果代码涉及副本、房间、私有地图、动态战场：

- 检查是不是应该用 `SubScene`
- 不要把所有动态隔离空间都硬塞到一个 Root Scene 里

## 审查时重点问自己

1. 这个类真的该继承 `Entity` 吗
2. 它应该挂在哪个 Scene / 父 Entity 下
3. 是否缺生命周期 System
4. 是否有对象池复用风险
5. 是否应该拆成 Component 而不是堆在主 Entity 上

## 相关文档

- `index.md`
- `entity-definition.md`
- `entity-operations.md`
- `object-pool.md`
- `lifecycle.md`
- `scene.md`
