// using System.Reflection;
// using Fantasy.Helper;
// using UnityEngine;
// // ReSharper disable HeuristicUnreachableCode
// #pragma warning disable CS0162
//
// namespace Fantasy
// {
//     public static class AssemblyName
//     {
//         // 这个对应的是Scripts下面的Model工程
//         public const int Model = 1;
//         // 你可以添加多个工程、如果有新添加的可以在这里添加、
//         // 并在AssemblyLoadHelper里添加对应的加载逻辑
//         // 参考LoadModelDll这个方法
//     }
//     
//     public static class AssemblyLoadHelper
//     {
//         private static readonly string HotfixModelDllBundle = "FantasyModelDll".ToLower();
//
//         public static void LoadModelDll()
//         {
// #if UNITY_EDITOR || UNITY_EDITOR_64
//             AssemblyManager.Load(AssemblyName.Model, typeof(Fantasy.Model.Entry).Assembly);
//             return;
// #endif
//             AssetBundleHelper.LoadBundle(HotfixModelDllBundle);
//             var dllBytes = AssetBundleHelper.GetAsset<TextAsset>(HotfixModelDllBundle, "Fantasy.Model.dll").bytes;
//             var pdbBytes = AssetBundleHelper.GetAsset<TextAsset>(HotfixModelDllBundle, "Fantasy.Model.pdb").bytes;
//             var assembly = Assembly.Load(dllBytes, pdbBytes);
//             AssemblyManager.Load(AssemblyName.Model, assembly);
//             AssetBundleHelper.UnloadBundle(HotfixModelDllBundle);
//         }
//     }
// }