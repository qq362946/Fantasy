#if FANTASY_NET
namespace Fantasy.Platform.Net;

/// <summary>
/// ProcessConfigD扩展方法
/// </summary>
public sealed partial class ProcessConfigData
{
    /// <summary>
    /// 按照startupGroup寻找属于startupGroup组的ProcessConfig
    /// </summary>
    /// <param name="startupGroup">startupGroup</param>
    /// <returns></returns>
    public IEnumerable<ProcessConfig> ForEachByStartupGroup(uint startupGroup)
    {
        foreach (var processConfig in List)
        {
            if (processConfig.StartupGroup == startupGroup)
            {
                yield return processConfig;
            }
        }
    }
}
#endif