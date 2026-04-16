# Protocol Define Common — 通用特性参考

适用于 Outer 和 Inner 协议，**仅在需要时读此文件**。

---

## 基础类型

| proto 类型 | C# 类型 | 说明 |
|---|---|---|
| `int32` | `int` | 32位有符号整数 |
| `int64` | `long` | 64位有符号整数 |
| `uint32` | `uint` | 32位无符号整数 |
| `uint64` | `ulong` | 64位无符号整数 |
| `float` | `float` | 单精度浮点 |
| `double` | `double` | 双精度浮点 |
| `bool` | `bool` | 布尔值 |
| `string` | `string` | UTF-8 字符串 |
| `bytes` | `byte[]` | 字节数组 |
| 其他 message 名 | 对应 C# 类 | 嵌套消息 |

---

## 自定义类型（原样输出）

框架未识别的类型会**原样**生成到 C#，配合 `// using` 引入命名空间：

```protobuf
// using Unity.Mathematics
message C2G_MoveRequest // IRequest,G2C_MoveResponse
{
    float3 Position = 1;   // 生成: public float3 Position { get; set; }
}
```

---

## 集合类型

| 关键字 | 生成类型 | 初始化 | Dispose 行为 | 适用场景 |
|---|---|---|---|---|
| `repeated` | `List<T>` | 自动初始化 | `Clear()` | 默认选择 |
| `repeatedList` | `List<T>` | 不初始化（null） | `= null` | 需区分空列表与 null |
| `repeatedArray` | `T[]` | 不初始化（null） | `= null` | 需要固定大小数组 |

```protobuf
message C2G_ExampleRequest // IRequest,G2C_ExampleResponse
{
    repeated int32 Ids = 1;           // List<int>，自动初始化
    repeatedList string Names = 2;    // List<string>，可为 null
    repeatedArray float Scores = 3;   // float[]，可为 null
}
```

---

## Map / 字典类型

```protobuf
map<{KeyType}, {ValueType}> {字段名} = {编号};
```

**Key** 支持：整数类型、`string`、`bool`、枚举。  
**Value** 支持：所有基础类型、枚举、其他 message。

```protobuf
message C2G_DataRequest // IRequest,G2C_DataResponse
{
    map<int32, string> Labels = 1;    // Dictionary<int, string>
    map<int64, ItemInfo> Items = 2;   // Dictionary<long, ItemInfo>
}
```

---

## 枚举定义

枚举不属于消息接口体系，无需接口注释，可在消息字段中直接引用：

```protobuf
enum ErrorCode
{
    Success = 0;
    InvalidToken = 1;
    AccountNotFound = 2;
}

message G2C_LoginResponse // IResponse
{
    ErrorCode Code = 1;
    int64 PlayerId = 2;
}
```

---

## 序列化选择

默认 ProtoBuf，如需 MemoryPack 在每条消息前单独声明：

```protobuf
// Protocol MemoryPack
message C2G_FastRequest // IRequest,G2C_FastResponse
{
    int32 Data = 1;
}
// Protocol MemoryPack
message G2C_FastResponse // IResponse
{
    int32 Result = 1;
}
```

---

## 自定义命名空间

```protobuf
// using System.Runtime
// using MyProject.Types
message C2G_CustomRequest // IRequest,G2C_CustomResponse
{
    MyType Data = 1;
}
```

---

## 原生代码注入（四个斜线）

`////` 开头的行会原样输出到生成的 C# 代码（去除 `////` 前缀），适合条件编译、平台特性标注：

```protobuf
////#if FANTASY_UNITY
////[Serializable]
////#endif
message G2C_PlayerData // IMessage
{
    string Name = 1;
}
```
