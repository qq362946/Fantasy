using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Controls;

namespace Fantasy.ProtocolEditor.Services;

/// <summary>
/// 代码补全服务 - 提供智能提示功能
/// </summary>
public class CompletionService
{
    /// <summary>
    /// 有效的接口列表
    /// </summary>
    private static readonly HashSet<string> ValidInterfaces = new()
    {
        // 基础接口
        "IMessage",
        "IRequest",
        "IResponse",

        // Address 系列
        "IAddressMessage",
        "IAddressRequest",
        "IAddressResponse",

        // Addressable 系列
        "IAddressableMessage",
        "IAddressableRequest",
        "IAddressableResponse",

        // CustomRoute 系列
        "ICustomRouteMessage",
        "ICustomRouteRequest",
        "ICustomRouteResponse",

        // Roaming 系列
        "IRoamingMessage",
        "IRoamingRequest",
        "IRoamingResponse"
    };

    /// <summary>
    /// 接口使用说明
    /// </summary>
    private static readonly Dictionary<string, string> InterfaceDescriptions = new()
    {
        // ===== 基础接口 =====
        { "IMessage", "单向消息 - 无回应的简单消息 \n所在网络: Outer \n作用对象: Session \n定义规范: // IMessage" },
        { "IRequest", "请求消息 - 注意: 该消息须有一个IResponse作为回应\n所在网络: Outer \n作用对象: Session \n定义规范: // IRequest,ResponseTypeName" },
        { "IResponse", "回应消息 - 该消息作为IRequest的回应 \n定义规范: // IResponse" },

        // ===== Address 系列 =====
        { "IAddressMessage", "内网地址消息 - 带实体地址路由的单向消息\n所在网络: Inner \n作用对象: 特定实体 \n定义规范: // IAddressMessage" },
        { "IAddressRequest", "内网地址请求 - 带实体地址路由的请求\n所在网络: Inner \n作用对象: 特定实体\n定义规范: // IAddressRequest,ResponseTypeName" },
        { "IAddressResponse", "内网地址响应 - IAddressRequest的回应\n所在网络: Inner \n作用对象: 特定实体\n定义规范: // IAddressResponse" },

        // ===== Addressable 系列 =====
        { "IAddressableMessage", "可寻址消息 - 由Gate服务器寻址转发的单向消息\n（旧版兼容, 新版推荐更自动化的IRoamingMessage）\n所在网络: Outer & Inner \n作用对象: 特定实体\n定义规范: // IAddressableMessage" },
        { "IAddressableRequest", "可寻址请求 - 由Gate服务器寻址转发的请求\n（旧版兼容, 新版推荐更自动化的IRoamingRequest）\n所在网络: Outer & Inner \n作用对象: 特定实体\n定义规范: // IAddressableRequest,ResponseTypeName" },
        { "IAddressableResponse", "可寻址响应 - IAddressableRequest的回应\n所在网络: Outer & Inner \n作用对象: 特定实体\n定义规范: // IAddressableResponse" },

        // ===== CustomRoute 系列 =====
        { "ICustomRouteMessage", "自定义路由转发消息 - 最基础的转发消息的实现，需要自己管理注册\n所在网络: Inner \n作用对象: 特定实体\n定义规范: // ICustomRouteMessage,RouteType" },
        { "ICustomRouteRequest", "自定义路由转发请求 - 最基础的转发消息的实现，需要自己管理注册\n所在网络: Inner \n作用对象: 特定实体\n定义规范: // ICustomRouteRequest,ResponseTypeName,RouteType" },
        { "ICustomRouteResponse", "自定义路由转发响应 - ICustomRouteRequest的回应\n所在网络: Inner \n作用对象: 特定实体\n定义规范: // ICustomRouteResponse" },

        // ===== Roaming 系列 =====
        { "IRoamingMessage", "自动漫游消息 - 支持Gate服务器自动寻址的单向消息\n（新版推荐，IAddressableMessage的自动化升级版本）\n所在网络: Outer & Inner \n作用对象: 特定实体\n定义规范: // IRoamingMessage,RoamingType" },
        { "IRoamingRequest", "自动漫游请求 - 支持Gate服务器自动寻址的请求\n（新版推荐，IAddressableRequest的自动化升级版本）\n所在网络: Outer & Inner \n作用对象: 特定实体 \n定义规范: // IRoamingRequest,ResponseTypeName,RoamingType" },
        { "IRoamingResponse", "自动漫游响应 - IRoamingRequest的回应\n所在网络: Outer & Inner \n作用对象: 特定实体\n定义规范: // IRoamingResponse" }
    };

    /// <summary>
    /// 检查是否应该触发接口补全
    /// </summary>
    /// <param name="document">文档</param>
    /// <param name="offset">光标位置</param>
    /// <returns>是否触发补全</returns>
    public bool ShouldTriggerCompletion(TextDocument document, int offset)
    {
        if (offset < 2) return false;

        // 获取当前行
        var line = document.GetLineByOffset(offset);
        var lineText = document.GetText(line.Offset, offset - line.Offset);

        // 检查行中是否包含 "message" 关键字
        // 使用正则表达式匹配: message XXX
        if (!System.Text.RegularExpressions.Regex.IsMatch(lineText, @"\bmessage\s+\w+"))
        {
            return false;  // 不是 message 定义行，不触发补全
        }

        // 查找注释符号 "//" 的位置
        var commentIndex = lineText.LastIndexOf("//");
        if (commentIndex >= 0)
        {
            // 获取 "//" 之后的内容
            var afterComment = lineText.Substring(commentIndex + 2).TrimStart();

            // 如果输入了 "I" 开头的内容,触发补全
            // 支持: message Foo // I, message Bar //IRequest, etc.
            if (afterComment.StartsWith("I", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取当前光标位置的单词前缀
    /// </summary>
    private string GetWordBeforeCaret(TextDocument document, int offset)
    {
        var line = document.GetLineByOffset(offset);
        var lineText = document.GetText(line.Offset, offset - line.Offset);

        // 提取注释后的内容
        var commentIndex = lineText.LastIndexOf("//");
        if (commentIndex >= 0)
        {
            var afterComment = lineText.Substring(commentIndex + 2).TrimStart();

            // 提取逗号前的部分(如果有逗号的话)
            var commaIndex = afterComment.IndexOf(',');
            if (commaIndex >= 0)
            {
                afterComment = afterComment.Substring(0, commaIndex);
            }

            return afterComment.Trim();
        }

        return string.Empty;
    }

    /// <summary>
    /// 获取补全项列表
    /// </summary>
    public List<ICompletionData> GetCompletions(TextDocument document, int offset)
    {
        var completions = new List<ICompletionData>();
        var prefix = GetWordBeforeCaret(document, offset);

        // 筛选匹配的接口
        var matchingInterfaces = ValidInterfaces
            .Where(i => i.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i);

        foreach (var interfaceName in matchingInterfaces)
        {
            var description = InterfaceDescriptions.TryGetValue(interfaceName, out var desc)
                ? desc
                : "网络协议接口";

            completions.Add(new InterfaceCompletionData(interfaceName, description, prefix.Length));
        }

        return completions;
    }
}

/// <summary>
/// 接口补全项
/// </summary>
public class InterfaceCompletionData : ICompletionData
{
    public InterfaceCompletionData(string text, string description, int replacementLength)
    {
        Text = text;
        Description = description;
        ReplacementLength = replacementLength;
    }

    public string Text { get; }

    public object Content => Text;

    public object Description { get; }
    public IImage? Image => null;
    public double Priority => 0;

    /// <summary>
    /// 需要替换的字符长度(已输入的前缀长度)
    /// </summary>
    public int ReplacementLength { get; }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        System.Diagnostics.Debug.WriteLine($"[Complete] Text={Text}, completionSegment.Offset={completionSegment.Offset}, completionSegment.Length={completionSegment.Length}");

        // 手动计算需要删除的前缀
        var offset = textArea.Caret.Offset;

        // 向前查找到 "//" 后的位置
        var line = textArea.Document.GetLineByOffset(offset);
        var lineText = textArea.Document.GetText(line.Offset, offset - line.Offset);
        var commentIndex = lineText.LastIndexOf("//");

        int deleteStart = offset;
        int deleteLength = 0;

        if (commentIndex >= 0)
        {
            var afterComment = lineText.Substring(commentIndex + 2).TrimStart();
            deleteLength = afterComment.Length;
            deleteStart = offset - deleteLength;
        }

        // 删除已输入的前缀
        if (deleteLength > 0)
        {
            textArea.Document.Remove(deleteStart, deleteLength);
        }

        // 构建完整的插入文本
        var insertionText = Text;
        var template = GetTemplate(Text);
        if (!string.IsNullOrEmpty(template))
        {
            insertionText += template;
        }

        // 插入文本
        textArea.Document.Insert(deleteStart, insertionText);

        // 设置光标位置
        if (!string.IsNullOrEmpty(template))
        {
            var commaIndex = template.IndexOf(',');
            if (commaIndex > 0)
            {
                textArea.Caret.Offset = deleteStart + Text.Length + commaIndex + 1;
            }
            else
            {
                textArea.Caret.Offset = deleteStart + insertionText.Length;
            }
        }
        else
        {
            textArea.Caret.Offset = deleteStart + insertionText.Length;
        }
    }

    /// <summary>
    /// 获取接口模板(参数占位符)
    /// </summary>
    private string GetTemplate(string interfaceName)
    {
        return interfaceName switch
        {
            // 基础接口
            "IMessage" => "",
            "IRequest" => ",ResponseTypeName",
            "IResponse" => "",

            // Address 系列
            "IAddressMessage" => "",
            "IAddressRequest" => ",ResponseTypeName",
            "IAddressResponse" => "",

            // Addressable 系列
            "IAddressableMessage" => "",
            "IAddressableRequest" => ",ResponseTypeName",
            "IAddressableResponse" => "",

            // CustomRoute 系列
            "ICustomRouteMessage" => ",RouteType",
            "ICustomRouteRequest" => ",ResponseTypeName,RouteType",
            "ICustomRouteResponse" => "",

            // Roaming 系列
            "IRoamingMessage" => ",RoamingType",
            "IRoamingRequest" => ",ResponseTypeName,RoamingType",
            "IRoamingResponse" => "",

            _ => ""
        };
    }
}
