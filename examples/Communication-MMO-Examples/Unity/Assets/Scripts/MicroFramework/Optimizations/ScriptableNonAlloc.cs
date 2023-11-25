using UnityEngine;

public abstract class ScriptableNonAlloc : ScriptableObject
{
    // .name是经常访问的，缓存它以避免GC!
    // 避免反复调用任何字符串访问器或数组访问器，造成垃圾资源分配（特别是在update中），这里是只获取一次并缓存它。
    // unity许多API有NonAlloc版本，这就是牺牲部分精确性，换来性能提升，减少资源分配
    string cachedName;
    public new string name
    {
        get
        {
            if (string.IsNullOrWhiteSpace(cachedName))
                cachedName = base.name;
            return cachedName;
        }
    }
}
