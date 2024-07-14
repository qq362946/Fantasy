using System;
using System.Collections.Generic;

namespace Fantasy
{
    public interface IBindingCodeGenerator
    {
        bool ValidType(Type type);
        List<(Type, string)> FunctionParams { get; }
        int Priority { get; }

        /// <summary>
        /// 生成环绕生成代码的xml格式字符串
        /// </summary>
        string CodeXmlSurround { get; }

        /// <summary>
        /// 生成的方法的名称
        /// </summary>
        /// <param name="fieldName">当前字段</param>
        /// <returns>方法名</returns>
        string GetFunctionName(string fieldName);

        /// <summary>
        /// 生成的方法体内容
        /// </summary>
        /// <param name="fieldName">当前字段</param>
        /// <returns>方法体内容</returns>
        List<string> GetFunctionCode(string fieldName);

        /// <summary>
        /// 生成绑定的代码
        /// </summary>
        /// <param name="targetField">当前字段</param>
        /// <param name="functionName">绑定的方法名</param>
        /// <returns>绑定代码</returns>
        List<string> GetBindingCode(string targetField, string functionName);

        /// <summary>
        /// 生成解除绑定的代码
        /// </summary>
        /// <param name="targetField">当前字段</param>
        /// <param name="functionName">解除绑定的方法名</param>
        /// <returns>绑定代码</returns>
        List<string> GetUnBindingCode(string targetField, string functionName);
    }
}