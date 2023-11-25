using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Extensions
{
    public static void SetActiveX(this GameObject go, bool state)
    {
        if (go && go.activeSelf != state)
        {
            go.SetActive(state);
        }
    }

    public static void SetActive(this Component go, bool state)
    {
        if (go && go.gameObject && go.gameObject.activeSelf != state)
        {
            go.gameObject.SetActive(state);
        }
    }

    // string to int 
    public static int ToInt(this string value, int errVal=0)
    {
        Int32.TryParse(value, out errVal);
        return errVal;
    }

    // string to long
    public static long ToLong(this string value, long errVal=0)
    {
        Int64.TryParse(value, out errVal);
        return errVal;
    }

    // UI SetListener扩展，删除以前的侦听器，然后添加新的侦听器
    //（此版本用于onClick等）
    public static void SetListener(this UnityEvent uEvent, UnityAction call)
    {
        uEvent.RemoveAllListeners();
        uEvent.AddListener(call);
    }

    // UI SetListener扩展，删除以前的侦听器，然后添加新的侦听器
    //（此版本适用于onededit、onValueChanged等）
    public static void SetListener<T>(this UnityEvent<T> uEvent, UnityAction<T> call)
    {
        uEvent.RemoveAllListeners();
        uEvent.AddListener(call);
    }

    // 检查列表是否有重复项
    public static bool HasDuplicates<T>(this List<T> list)
    {
        return list.Count != list.Distinct().Count();
    }

    // 查找列表中的所有重复项
    // 注意：这只在开始时调用一次，所以Linq在这里很好！
    public static List<U> FindDuplicates<T, U>(this List<T> list, Func<T, U> keySelector)
    {
        return list.GroupBy(keySelector)
                   .Where(group => group.Count() > 1)
                   .Select(group => group.Key).ToList();
    }

    // string.GetHashCode 不能保证在所有机器上都是相同的，但是
    // 我们需要一个在所有机器上都一样的。这是一个简单的方法：
    public static int GetStableHashCode(this string text)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in text)
                hash = hash * 31 + c;
            return hash;
        }
    }
}