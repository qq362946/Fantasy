using System;
using UnityEditor;

namespace Fantasy
{
    internal static class CheckUnityVersion
    {
        [InitializeOnLoadMethod]
        private static void OnInitializeOnLoad()
        {
#if !UNITY_2021_3_OR_NEWER
            Debug.LogError("Fantasy支持的最低版本为Unity2021.3.14f1c1");
#endif
        }
    }
}