# Source Generator 调试指南

## 当前状态

生成器已编译成功，但没有生成任何代码文件。

## 可能的原因

1. **类型匹配问题**：生成器的 `IsSystemClass` 或 `GetSystemType` 方法无法正确识别 System 类
2. **Incremental Generator Pipeline 问题**：Pipeline 可能过滤掉了所有候选类
3. **命名空间不匹配**：生成器可能在寻找错误的命名空间

## 调试步骤

### 方法 1：简化生成器逻辑

将 `IsSystemClass` 改为返回 `true` 以查看是否有任何类被处理：

```csharp
private static bool IsSystemClass(SyntaxNode node)
{
    return node is ClassDeclarationSyntax;  // 处理所有类
}
```

### 方法 2：查看生成器输出

```bash
# 启用详细日志
dotnet build /p:EmitCompilerGeneratedFiles=true /p:CompilerGeneratedFilesOutputPath=Generated

# 查看生成的文件
ls -la Generated/
```

### 方法 3：使用 LinqPad/RoslynPad 测试

创建独立的测试项目来验证 Roslyn API 调用。

## 下一步

建议先尝试方法 2，查看 MSBuild 是否能输出生成的文件。
