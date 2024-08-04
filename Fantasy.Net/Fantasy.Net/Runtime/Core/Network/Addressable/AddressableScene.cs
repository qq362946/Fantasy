#if FANTASY_NET
namespace Fantasy;

public sealed class AddressableScene
{
    public readonly long Id;
    public readonly long RunTimeId;

    public AddressableScene(SceneConfig sceneConfig)
    {
        Id = new EntityIdStruct(0, sceneConfig.Id, (byte)sceneConfig.WorldConfigId, 0);
        RunTimeId = new RuntimeIdStruct(0, sceneConfig.Id, (byte)sceneConfig.WorldConfigId, 0);
    }
}
#endif