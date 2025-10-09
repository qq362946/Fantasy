
using Fantasy.Settings;

namespace Fantasy
{
    /// <summary>
    /// 项目设置
    /// </summary>
    public class ProjectSettings
    {
        /// <summary>
        /// 实体设置
        /// </summary>
        public static EntitySettings EntitySettings = new();
    }

    /// <summary>
    /// 类型码模式
    /// </summary>
    public enum TypeCodeMode
    {
        /// <summary>
        /// 从全名计算
        /// </summary>
        FromFullName,
        /// <summary>
        /// 从TypeHandle直接获取
        /// </summary>
        FromTypeHandle
    }
}

namespace Fantasy.Settings
{
    /// <summary>
    /// 实体相关设置
    /// </summary>
    public class EntitySettings
    {
        /// <summary>
        /// TypeCodeMode
        /// </summary>
        public TypeCodeMode TypeCodeMode = TypeCodeMode.FromFullName;
    }
}
