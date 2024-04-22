#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy;

public sealed partial class SceneConfigData
{
    private readonly OneToManyList<uint, SceneConfig> _sceneConfigByServerConfigId = new OneToManyList<uint, SceneConfig>();
    
    protected override void EndInit()
    {
        _sceneConfigByServerConfigId.Clear();
        
        foreach (var sceneConfig in List)
        {
            _sceneConfigByServerConfigId.Add(sceneConfig.ServerConfigId, sceneConfig);
        }
    }

    public List<SceneConfig> GetByServerConfigId(uint serverConfigId)
    {
        return _sceneConfigByServerConfigId.GetValueOrDefault(serverConfigId);
    }
}