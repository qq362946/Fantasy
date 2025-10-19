using System;
using UnityEditor;

namespace Fantasy
{
    internal static class CheckUnityVersion
    {
        [InitializeOnLoadMethod]
        private static void OnInitializeOnLoad()
        {
#if !UNITY_2022_3_OR_NEWER
            Debug.LogError("Fantasy支持的最低版本为Unity2022.3.622f2");
#endif
        }
    }
}