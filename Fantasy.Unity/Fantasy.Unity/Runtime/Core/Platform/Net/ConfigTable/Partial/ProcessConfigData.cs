#if FANTASY_NET
namespace Fantasy;

public sealed partial class ProcessConfigData
{
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