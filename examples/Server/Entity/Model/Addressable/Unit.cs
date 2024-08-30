namespace Fantasy;

public sealed class Unit : Entity
{
    public Dictionary<long,GameEntity> GameEntities { get; set; }=new Dictionary<long,GameEntity>();

    public long ClientID;

    public long RoomID=-1;

    public int EntitesCount=>GameEntities.Count;

    public void AddGameEntity(GameEntity ge)
    {
        GameEntities.Add(ge.Id, ge);
        //return ge.Id;
    }
    public void RemoveGameEntity(GameEntity ge)
    {
        GameEntities.Remove(ge.Id);

    }
    public GameEntity[] GetAllGameEntites()
    {
        return GameEntities.Values.ToArray();
    }
    public GameEntity? GetGameEntity(long id)
    {
        if(GameEntities.TryGetValue(id, out GameEntity ge)) return ge;
        return null;
    }
    public void Reset()
    {
        GameEntities.Clear();
    }
}