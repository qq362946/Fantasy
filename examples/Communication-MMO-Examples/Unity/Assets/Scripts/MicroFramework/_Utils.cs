using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Linq.Expressions;
[Serializable] public class UnityEventString : UnityEvent<String> {}

public static class Utils
{
    public static object GetPropertyValue(string propertyName,object ob)
    {
        //Debug.Log(ob.GetType());
        return ob.GetType().GetProperty(propertyName).GetValue(ob, null);
    }

    public static string GetPropertyName<T>(Expression<Func<T,object>> expr)  
    {  
        var rtn = "";  
        if (expr.Body is UnaryExpression)  
        {  
            rtn = ((MemberExpression)((UnaryExpression)expr.Body).Operand).Member.Name;  
        }  
        else if (expr.Body is MemberExpression)  
        {  
            rtn = ((MemberExpression)expr.Body).Member.Name;  
        }  
        else if (expr.Body is ParameterExpression)  
        {  
            rtn = ((ParameterExpression)expr.Body).Type.Name;  
        }  
        return rtn;
    }  

    // 仅适用于float和int，这个方法支持更多
    public static long Clamp(long value, long min, long max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static bool AnyKeyUp(KeyCode[] keys)
    {
        // 避免Linq.Any，因为它影响GC性能
        foreach (KeyCode key in keys)
            if (Input.GetKeyUp(key))
                return true;
        return false;
    }

    public static bool AnyKeyDown(KeyCode[] keys)
    {
        // 避免Linq.Any，因为它影响GC性能
        foreach (KeyCode key in keys)
            if (Input.GetKeyDown(key))
                return true;
        return false;
    }

    public static bool AnyKeyPressed(KeyCode[] keys)
    {
        // 避免Linq.Any，因为它影响GC性能
        foreach (KeyCode key in keys)
            if (Input.GetKey(key))
                return true;
        return false;
    }

    //
    public static T[] CopyBehind<T>(IList<T> a,int index)where T:struct
    {
        List<T> list = new List<T>();
        for(int i=index;i<a.Count;i++){
            T t = new T();
            t = a[i];
            list.Add(t);
        }
        return list.ToArray();
    }

    // 2D point in screen
    public static bool IsPointInScreen(Vector2 point)
    {
        return 0 <= point.x && point.x <= Screen.width &&
               0 <= point.y && point.y <= Screen.height;
    }

    // 计算世界空间中边界半径的辅助函数
    // -> collider.radius  local scale
    // -> collider.bounds  world scale
    // -> 使用x+y扩展平均值来确定 (for capsules, x==y extends)
    // -> 使用“extends”而不是“size”，因为extends是半径。
    //    换句话说，如果我们从右边来，我们只想停在的半径是半径的一半，不是半径的两倍。
    public static float BoundsRadius(Bounds bounds) =>
        (bounds.extents.x + bounds.extents.z) / 2;

    // 两个闭合点之间的距离
    // Vector3.Distance(a.transform.position, b.transform.position):
    //    _____        _____
    //   |     |      |     |
    //   |  x==|======|==x  |
    //   |_____|      |_____|
    //
    //
    // Utils.ClosestDistance(a.collider, b.collider):
    //    _____        _____
    //   |     |      |     |
    //   |     |x====x|     |
    //   |_____|      |_____|
    // public static float ClosestDistance(Entity a, Entity b)
    // {
    //     float distance = Vector3.Distance(a.transform.position, b.transform.position);

    //     // 两个碰撞体的半径
    //     float radiusA = BoundsRadius(a.collider.bounds);
    //     float radiusB = BoundsRadius(b.collider.bounds);

    //     // 减去两个半径
    //     float distanceInside = distance - radiusA - radiusB;

    //     // 返回距离。如果它小于0，它们在彼此内部，那么返回0
    //     return Mathf.Max(distanceInside, 0);
    // }

    // 从实体的碰撞器到另一个点的最近点
    // public static Vector3 ClosestPoint(Entity entity, Vector3 point)
    // {
    //     float radius = BoundsRadius(entity.collider.bounds);

    //     Vector3 direction = entity.transform.position - point;
    //     //Debug.DrawLine(point, point + direction, Color.red, 1, false);

    //     // 从direction length减去半径
    //     Vector3 directionSubtracted = Vector3.ClampMagnitude(direction, direction.magnitude - radius);

    //     // return the point
    //     //Debug.DrawLine(point, point + directionSubtracted, Color.green, 1, false);
    //     return point + directionSubtracted;
    // }


    static Dictionary<Transform, int> castBackups = new Dictionary<Transform, int>();

    // 忽略自身时进行光线投射（首先将层设置为“忽略光线投射”）
    public static bool RaycastWithout(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance, GameObject ignore, int layerMask=Physics.DefaultRaycastLayers)
    {
        // remember layers
        castBackups.Clear();

        // 全部设置为忽略光线投射
        foreach (Transform tf in ignore.GetComponentsInChildren<Transform>(true))
        {
            castBackups[tf] = tf.gameObject.layer;
            tf.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        // raycast
        bool result = Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);

        // restore layers
        foreach (KeyValuePair<Transform, int> kvp in castBackups)
            kvp.Key.gameObject.layer = kvp.Value;

        return result;
    }

    // 计算所有子渲染器的封装边界
    public static Bounds CalculateBoundsForAllRenderers(GameObject go)
    {
        Bounds bounds = new Bounds();
        bool initialized = false;
        foreach (Renderer rend in go.GetComponentsInChildren<Renderer>())
        {
            // initialize or encapsulate
            if (!initialized)
            {
                bounds = rend.bounds;
                initialized = true;
            }
            else bounds.Encapsulate(rend.bounds);
        }
        return bounds;
    }

    // 从 "from" 查找最近的变换
    public static Transform GetNearestTransform(List<Transform> transforms, Vector3 from)
    {
        Transform nearest = null;
        foreach (Transform tf in transforms)
        {
            if (nearest == null ||
                Vector3.Distance(tf.position, from) < Vector3.Distance(nearest.position, from))
                nearest = tf;
        }
        return nearest;
    }

    // 漂亮的打印秒数小时：分钟：秒（.毫秒/100）秒
    public static string PrettySeconds(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        string res = "";
        if (t.Days > 0) res += t.Days + "d";
        if (t.Hours > 0) res += " " + t.Hours + "h";
        if (t.Minutes > 0) res += " " + t.Minutes + "m";
        // 0.5s, 1.5s etc. if any milliseconds. 1s, 2s etc. if any seconds
        if (t.Milliseconds > 0) res += " " + t.Seconds + "." + (t.Milliseconds / 100) + "s";
        else if (t.Seconds > 0) res += " " + t.Seconds + "s";
        // 如果字符串仍然是空的，因为值是“0”，那么至少
        // 返回秒数，而不是返回空字符串
        return res != "" ? res : "0s";
    }

    // 所有平台之间一致的硬鼠标滚动
    // 输入.GetAxis（“鼠标滚轮”）和
    // 输入.GetAxisRaw（“鼠标滚轮”）
    // 这两个返回值都是独立的0.01和WebGL上的0.5，这
    // 导致在WebGL上缩放过快等。
    // 通常GetAxisRaw应该返回-1,0,1，但对于滚动则不返回
    public static float GetAxisRawScrollUniversal()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll < 0) return -1;
        if (scroll > 0) return  1;
        return 0;
    }

    // two finger pinch detection
    // source: https://docs.unity3d.com/Manual/PlatformDependentCompilation.html
    public static float GetPinch()
    {
        if (Input.touchCount == 2)
        {
            // Store both touches.
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Find the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Find the difference in the distances between each frame.
            return touchDeltaMag - prevTouchDeltaMag;
        }
        return 0;
    }

    // 通用变焦：鼠标滚动,鼠标两个手指不按
    public static float GetZoomUniversal()
    {
        if (Input.mousePresent)
            return GetAxisRawScrollUniversal();
        else if (Input.touchSupported)
            return GetPinch();
        return 0;
    }

    // 解析字符串中最后一个大写名词
    //   EquipmentWeaponBow => Bow
    //   EquipmentShield => Shield
    static Regex lastNountRegEx = new Regex(@"([A-Z][a-z]*)"); // cache to avoid allocations. this is used a lot.
    public static string ParseLastNoun(string text)
    {
        MatchCollection matches = lastNountRegEx.Matches(text);
        return matches.Count > 0 ? matches[matches.Count-1].Value : "";
    }

    // 检查光标是否位于UI或OnGUI元素上
    public static bool IsCursorOverUserInterface()
    {
        // IsPointerOverGameObject 检查r left mouse (default)
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // IsPointerOverGameObject 检查 touches
        for (int i = 0; i < Input.touchCount; ++i)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;

        // OnGUI check
        return GUIUtility.hotControl != 0;
    }

    // NIST推荐的PBKDF2哈希
    // http://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-132.pdf
    // salt should be at least 128 bits = 16 bytes
    public static string PBKDF2Hash(string text, string salt)
    {
        byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
        Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(text, saltBytes, 10000);
        byte[] hash = pbkdf2.GetBytes(20);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    // 利用反射，通过前缀调用多个函数
    // -> 缓存它，以便足够快地进行更新调用
    static Dictionary<KeyValuePair<Type,string>, MethodInfo[]> lookup = new Dictionary<KeyValuePair<Type,string>, MethodInfo[]>();
    public static MethodInfo[] GetMethodsByPrefix(Type type, string methodPrefix)
    {
        KeyValuePair<Type, string> key = new KeyValuePair<Type, string>(type, methodPrefix);
        if (!lookup.ContainsKey(key))
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                       .Where(m => m.Name.StartsWith(methodPrefix))
                                       .ToArray();
            lookup[key] = methods;
        }
        return lookup[key];
    }

    public static void InvokeMany(Type type, object onObject, string methodPrefix, params object[] args)
    {
        foreach (MethodInfo method in GetMethodsByPrefix(type, methodPrefix))
            method.Invoke(onObject, args);
    }

    // clamp a rotation around x axis
    public static Quaternion ClampRotationAroundXAxis(Quaternion q, float min, float max)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan (q.x);
        angleX = Mathf.Clamp (angleX, min, max);
        q.x = Mathf.Tan (0.5f * Mathf.Deg2Rad * angleX);

        return q;
    }
}