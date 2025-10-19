// ReSharper disable CheckNamespace
//------------------------------------------------------------------------------
// 这个 IsExternalInit 类是一个 polyfill（兼容性填充）
// 用于在 .NET Standard 2.0 或较低版本的框架中启用 C# 9.0 的 init 访问器和 record 类型功能。
// 为什么需要它？
// C# 9.0 引入了 init 访问器（只在初始化时可设置的属性）
// 编译器在编译 init 属性时，会查找 IsExternalInit 类型
// 示例：
// public class Person
// {
//     public string Name { get; init; }  // 需要 IsExternalInit
//     public int Age { get; init; }
// }
// 使用
// var person = new Person { Name = "Alice", Age = 30 };
// person.Name = "Bob";  // ❌ 编译错误：init 属性只能在对象初始化时设置
// 不定义会怎样？
//  如果目标框架是 netstandard2.0 但没定义 IsExternalInit，编译器会报错：
//  error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' 
//                is not defined or imported
// 实际应用场景
// 在 IncrementalGenerator 中，你可能会生成或使用带 init 的代码
//------------------------------------------------------------------------------
#if NETSTANDARD2_0 || NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for C# 9.0 record types in netstandard2.0
    /// </summary>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif
