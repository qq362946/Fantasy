using Fantasy.DataStructure;
#pragma warning disable CS8618
#pragma warning disable CS8603

namespace Fantasy;

public sealed partial class SceneConfigData
{
    public SceneConfig ChatScene;
    public readonly List<SceneConfig> AddressableScenes = new List<SceneConfig>();
    public readonly List<SceneConfig> MapScenes = new List<SceneConfig>();
    private readonly OneToManyList<uint, SceneConfig> _scenesByRoute = new();
    private readonly OneToManyList<int, SceneConfig> _configBySceneType = new ();
    
    protected override void EndInit()
    {
        _scenesByRoute.Clear();
        _configBySceneType.Clear();
        
        foreach (var sceneConfig in List)
        {
            if (!SceneType.SceneDic.TryGetValue(sceneConfig.SceneType, out var sceneType))
            {
                continue;
            }

            _configBySceneType.Add(sceneType, sceneConfig);
            _scenesByRoute.Add(sceneConfig.RouteId, sceneConfig);
            
            switch (sceneType)
            {
                case SceneType.Addressable:
                {
                    AddressableScenes.Add(sceneConfig);
                    continue;
                }
                case SceneType.Map:
                {
                    MapScenes.Add(sceneConfig);
                    continue;
                }
                case SceneType.Chat:
                {
                    ChatScene = sceneConfig;
                    continue;
                }
            }
        }
    }
    
    public SceneConfigInfo FirstSceneInfo(int sceneType)
    {
        if (!_configBySceneType.TryGetValue(sceneType, out var sceneConfigs))
        {
            return null;
        }
    
        var sceneConfig = sceneConfigs[0];
            
        return new SceneConfigInfo()
        {
            Id = sceneConfig.Id,
            Name = sceneConfig.Name,
            EntityId = sceneConfig.EntityId,
            SceneType = sceneConfig.SceneType,
            OuterPort = sceneConfig.OuterPort,
            NetworkProtocol = sceneConfig.NetworkProtocol
        };
    }
    
    public List<SceneConfig> GetByRouteId(uint routeId)
    {
        return _scenesByRoute.GetValues(routeId);
    }

    public List<SceneConfigInfo> GetSceneInfoByRouteId(uint routeId)
    {
        var sceneConfigs = _scenesByRoute.GetValues(routeId);

        if (sceneConfigs == null)
        {
            return null;
        }

        var sceneInfos = new List<SceneConfigInfo>();

        foreach (var sceneConfig in sceneConfigs)
        {
            sceneInfos.Add(new SceneConfigInfo()
            {
                Id = sceneConfig.Id,
                Name = sceneConfig.Name,
                EntityId = sceneConfig.EntityId,
                SceneType = sceneConfig.SceneType,
                OuterPort = sceneConfig.OuterPort,
                NetworkProtocol = sceneConfig.NetworkProtocol
            });
        }

        return sceneInfos;
    }
}