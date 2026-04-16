# ECS 入口

Fantasy 的 ECS 是层级组件化架构：`Component` 本身也是 `Entity`，通过父子关系组织。所有 `Entity` / `Component` 都归属于某个 `Scene`，`Scene` 是它们的生命周期边界。

## Workflow

```text
理解 Scene、生命周期边界、OnCreateScene -> scene.md
创建 / 使用 SubScene -> subscene.md
定义 Entity / Component，并判断要不要配套 System -> entity-definition.md
创建 Entity 实例、增删组件、多实例组件 -> entity-operations.md
响应 Awake / Update / Destroy / Deserialize / Transfer -> lifecycle.md
对象池、减少 GC、层级销毁 -> object-pool.md
```

## 必记规则

1. `Entity` / `Component` 都用 `sealed class`，并继承 `Entity`
2. 每个 `Entity` / `Component` 都属于某个 `Scene`
3. `Component` 本质也是 `Entity`，通过父子关系挂到其他 Entity 上
4. 使用对象池时，自定义字段必须在 `DestroySystem` 里重置
5. 如果用户没说明目录或程序集归属，先询问，不要自行假设

## 子文档

- `scene.md` - Scene 概念、生命周期边界、系统组件访问
- `subscene.md` - 动态子场景
- `entity-definition.md` - Entity / Component 定义与配套 System 选择
- `entity-operations.md` - 创建 Entity、组件操作、多实例组件
- `object-pool.md` - 对象池、层级销毁、GC 优化
- `lifecycle.md` - Awake / Update / Destroy / Deserialize / Transfer 生命周期
