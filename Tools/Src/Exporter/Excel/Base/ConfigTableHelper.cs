using Fantasy;
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy;

public class ConfigTableHelper
{
    private static ConfigTableHelper _instance;
    public static ConfigTableHelper Instance => _instance ??= new ConfigTableHelper();
    
    public T Load<T>() where T : AProto
    {
        return default;
    }
}