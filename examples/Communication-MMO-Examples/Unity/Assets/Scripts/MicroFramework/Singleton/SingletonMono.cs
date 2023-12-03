using UnityEngine;
// 单例基类,这是要手动挂载到GameObject上的脚本
public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance = null;

    public static T Ins => _instance;

    public virtual bool DontDestroy => false;

    protected void Awake()
    {
        // 注意&与&&区别
        if (_instance != null & _instance == this)
            return;

        _instance = this as T;
        if(DontDestroy) DontDestroyOnLoad(gameObject);
    }

    void OnDestory()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    } 
}
