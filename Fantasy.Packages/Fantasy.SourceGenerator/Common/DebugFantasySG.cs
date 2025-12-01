using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Fantasy.SourceGenerator.Common
{
    /// <summary>
    /// 调试报告信息输出工具类
    /// </summary>
    internal class DebugFantasySG
    {
        const string Id = "DebugFantasySG";
        const DiagnosticSeverity reportType = DiagnosticSeverity.Warning; // 设置为 Warning 以便于查看, 不能是Info(因为看不到) ,也不能是 Error(因为会中断编译)

        /// <summary>
        /// 携带标题+单条信息的输出
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:启用分析器发布跟踪", Justification = "<挂起>")]
        public static DiagnosticDescriptor OneInfoDescriptor = new(
                id: Id,
                title: "Referenced Assembly",
                messageFormat: $"【{{0}}】\"{{1}}\"",
                category: "GeneratorDebug",
                defaultSeverity: reportType,
                isEnabledByDefault: true);

        /// <summary>
        /// 携带标题+两条信息的输出
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:启用分析器发布跟踪", Justification = "<挂起>")]
        public static DiagnosticDescriptor TwoInfoDescriptor= new(
                id: Id,
                title: "Referenced Assembly",
                messageFormat: $"【{{0}}】1: \"{{1}}\" 2: \"{{2}}\"",
                category: "GeneratorDebug",
                defaultSeverity: reportType,
                isEnabledByDefault: true);

        /// <summary>
        /// 携带标题+三条信息的输出
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:启用分析器发布跟踪", Justification = "<挂起>")]
        public static DiagnosticDescriptor ThreeInfoDescriptor = new(
                id: Id,
                title: "Referenced Assembly",
                messageFormat: $"【{{0}}】1: \"{{1}}\" 2: \"{{2}}\" 3: \"{{3}}\"",
                category: "GeneratorDebug",
                defaultSeverity: reportType,
                isEnabledByDefault: true);    
    }
}
