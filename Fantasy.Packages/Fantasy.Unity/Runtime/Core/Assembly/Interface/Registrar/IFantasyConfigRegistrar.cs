#if FANTASY_NET
namespace Fantasy.Assembly;

/// <summary>
/// 
/// </summary>
public interface IFantasyConfigRegistrar
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Dictionary<string, int> GetDatabaseNameDictionary();
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Dictionary<string, int> GetSceneTypeDictionary();
}
#endif