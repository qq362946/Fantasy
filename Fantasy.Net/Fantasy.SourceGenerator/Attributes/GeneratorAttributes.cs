namespace Fantasy.SourceGenerator.Attributes
{
    /// <summary>
    /// 标记程序集启用 Source Generator 生成注册代码
    /// 添加到 AssemblyInfo.cs 或任何文件：
    /// [assembly: Fantasy.SourceGenerator.Attributes.EnableSourceGenerator]
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class EnableSourceGeneratorAttribute : System.Attribute
    {
        /// <summary>
        /// 是否生成 Entity System 注册器
        /// </summary>
        public bool GenerateEntitySystem { get; set; } = true;

        /// <summary>
        /// 是否生成 Event Handler 注册器
        /// </summary>
        public bool GenerateEventHandler { get; set; } = true;

        /// <summary>
        /// 是否生成 OpCode Mapper 注册器
        /// </summary>
        public bool GenerateOpCodeMapper { get; set; } = true;

        /// <summary>
        /// 是否生成 Message Handler 注册器
        /// </summary>
        public bool GenerateMessageHandler { get; set; } = true;
    }
}
