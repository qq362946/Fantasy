// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fantasy
{
    public enum FantasyAutoRefRule
    {
        prefix = 1,
        suffix = 2,
    }

    public static class FantasyAutoRefRuleHelper
    {
        /// <summary>
        /// 根据gameObject的name拆分出引用关键字和名字
        /// </summary>
        public static string[] GetRefInfoByName(string name)
        {
            var autoRefRule = FantasyUISettingsScriptableObject.Instance.autoRefRule;
            char splitChar = FantasyUISettingsScriptableObject.Instance.splitChar;
            string[] list = { "", "" };
            switch (autoRefRule)
            {
                case FantasyAutoRefRule.prefix:
                {
                    var idx = name.IndexOf(splitChar);
                    if (idx <= 0)
                    {
                        list[1] = name;
                        return list;
                    }

                    list[0] = name[..idx];
                    list[1] = name[(idx + 1)..];
                    return list;
                }
                case FantasyAutoRefRule.suffix:
                {
                    var idx = name.LastIndexOf(splitChar);
                    if (idx <= 0)
                    {
                        list[1] = name;
                        return list;
                    }

                    list[1] = name[..idx];
                    list[0] = name[(idx + 1)..];
                    return list;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Object GetComponentByKey(GameObject go, string key)
        {
            Object o = go;
            var componentNames = FantasyUISettingsScriptableObject.Instance.FantasyUIAutoRefSettings[key];
            if (string.IsNullOrEmpty(componentNames))
                return o;
            var list = componentNames.Replace(" ", "").Split(',');
            if (list.Length == 0)
                return o;
            foreach (var componentName in list)
            {
                if (componentName == "GameObject")
                    return o;
                o = go.GetComponent(componentName);
                if(o != null)
                    return o;
            }
            return o;
        }

        public static string GetRefName(string name)
        {
            var splitChar = FantasyUISettingsScriptableObject.Instance.splitChar;
            return name.Replace(splitChar, '_');
        }
    }
}