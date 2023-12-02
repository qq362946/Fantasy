using UnityEngine;
// 单例基类,不需要手动挂载到GameObject上的脚本
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance = null;
    public static T Ins
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject(typeof(T).Name,typeof(T));
                _instance = go.GetComponent<T>();
            }

            return _instance;
        }
    } 


    protected void Awake()
    {
        // 注意&与&&区别
        if (_instance != null & _instance == this)
            return;

        _instance = this as T;
    }

    void OnDestory()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    } 
}
