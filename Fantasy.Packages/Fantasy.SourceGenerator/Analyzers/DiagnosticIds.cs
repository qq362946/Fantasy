namespace Fantasy.SourceGenerator.Analyzers
{
    /// <summary>
    /// 统一管理所有分析器的诊断 ID，避免冲突
    /// </summary>
    internal static class DiagnosticIds
    {
        // FANTASY001 - SphereEventArgsCreationAnalyzer
        /// <summary>
        /// SphereEventArgs 必须使用 SphereEventArgs.Create 创建
        /// </summary>
        public const string SphereEventArgsCreation = "FANTASY001";

        // FANTASY002 - EntityCreationAnalyzer
        /// <summary>
        /// Entity 必须使用 Entity.Create 创建
        /// </summary>
        public const string EntityCreation = "FANTASY002";

        // FANTASY003 - RoamingLinkArgumentAnalyzer
        /// <summary>
        /// SessionRoamingComponent.Link 的 Entity 参数必须是 MemoryPackable 和 partial
        /// </summary>
        public const string RoamingLinkArgument = "FANTASY003";

        // FANTASY004 - SphereEventArgsCreationAnalyzer
        /// <summary>
        /// SphereEventArgs 派生类缺少 [MemoryPackable] 特性
        /// </summary>
        public const string SphereEventArgsMissingMemoryPackable = "FANTASY004";

        // FANTASY005 - SphereEventArgsCreationAnalyzer
        /// <summary>
        /// SphereEventArgs 派生类缺少 partial 关键字
        /// </summary>
        public const string SphereEventArgsMissingPartial = "FANTASY005";

        // FANTASY006 - MessageEntityFieldAnalyzer
        /// <summary>
        /// ProtoContract 消息不能包含 Entity 字段
        /// </summary>
        public const string MessageProtoContractEntityField = "FANTASY006";

        // FANTASY007 - MessageEntityFieldAnalyzer
        /// <summary>
        /// MemoryPackable 消息中的 Entity 派生类字段必须是 MemoryPackable 和 partial
        /// </summary>
        public const string MessageMemoryPackableEntityField = "FANTASY007";

        // 预留 FANTASY008-FANTASY010 供未来使用
    }
}
