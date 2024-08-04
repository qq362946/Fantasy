#if FANTASY_NET
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy;

public sealed partial class SceneConfigData
{
    private readonly OneToManyList<uint, SceneConfig> _sceneConfigByProcess = new OneToManyList<uint, SceneConfig>();
    
    protected override void EndInit()
    {
        _sceneConfigByProcess.Clear();
        
        foreach (var sceneConfig in List)
        {
            _sceneConfigByProcess.Add(sceneConfig.ProcessConfigId, sceneConfig);
        }
    }

    public List<SceneConfig> GetByProcess(uint serverConfigId)
    {
        return _sceneConfigByProcess.TryGetValue(serverConfigId, out var list) ? list : new List<SceneConfig>();
    }
}
#endif
