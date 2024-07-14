using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AssetBundleBrowser;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Fantasy
{
    [InitializeOnLoad]
    public class SceneUIWorktable
    {
        static SceneUIWorktable()
        {
            WorktableProcess();
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.EnteredPlayMode)
                {
                    var activeScene = SceneManager.GetActiveScene();
                    if (!activeScene.path.StartsWith("Assets/Scenes/UI/"))
                        return;
                    SceneManager.LoadScene(0);
                }
            };
        }

        private static void WorktableProcess()
        {
            SceneView.duringSceneGui += _ =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                var activeScene = SceneManager.GetActiveScene();
                if (!activeScene.path.StartsWith("Assets/Scenes/UI/"))
                    return;

                var path = activeScene.path.Replace("Assets/Scenes/UI/", "");
                if (path.IndexOf('/') < 0)
                    return;
                var moduleName = path[..path.IndexOf('/')];
                var nameSpaceName = path.IndexOf('/') == path.LastIndexOf('/') ? $"{moduleName}" : $"{moduleName}/{path[(path.IndexOf('/') + 1)..path.LastIndexOf('/')]}";
                var assetName = activeScene.name;
                var bundleName = $"UI/{moduleName}/prefab";

                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(50, 5, 300, 100));
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("生成UI预设&脚本"))
                {
                    if (GetPrefabPoint(out var fantasyUIRef))
                    {
                        if (!CheckRepeatedName(fantasyUIRef.gameObject))
                            return;
                        AutoAddRef(fantasyUIRef);
                        if (!SetParamsForRef(fantasyUIRef, bundleName, assetName, nameSpaceName, assetName))
                            return;
                        CreateUIPrefab(fantasyUIRef);
                    }
                }

                if (GUILayout.Button("自动获取引用"))
                {
                    if (GetPrefabPoint(out var fantasyUIRef))
                    {
                        if (!CheckRepeatedName(fantasyUIRef.gameObject))
                            return;
                        AutoAddRef(fantasyUIRef);
                        if (!SetParamsForRef(fantasyUIRef, bundleName, assetName, nameSpaceName, assetName))
                            return;
                        EditorUtility.DisplayDialog("自动获取引用", "已经更新引用", "ok");
                    }
                }

                if (GUILayout.Button("检查重复命名"))
                {
                    if (GetPrefabPoint(out var fantasyUIRef))
                    {
                        if (CheckRepeatedName(fantasyUIRef.gameObject))
                            EditorUtility.DisplayDialog("检查节点名重复", "检查通过", "ok");
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                Handles.EndGUI();
            };
        }

        private static bool GetPrefabPoint(out FantasyUIRef fantasyUIRef)
        {
            fantasyUIRef = null;
            var go = GameObject.Find("Canvas/Prefab");
            if (go == null)
            {
                EditorUtility.DisplayDialog("生成ui预设", "找不到Canvas下Prefab节点", "确定");
                return false;
            }

            fantasyUIRef = go.GetComponent<FantasyUIRef>();
            if (fantasyUIRef == null)
            {
                EditorUtility.DisplayDialog("生成ui预设", "Prefab节点下未挂载FantasyUIRef组件", "确定");
                return false;
            }

            return true;
        }

        private static void CreateUIPrefab(FantasyUIRef fantasyUIRef)
        {
            var bundleName = fantasyUIRef.bundleName;
            var assetName = fantasyUIRef.assetName;
            var prefabName = fantasyUIRef.assetName;
            var nameSpaceName = fantasyUIRef.nameSpaceName;

            if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(prefabName) || string.IsNullOrEmpty(nameSpaceName))
            {
                EditorUtility.DisplayDialog("生成UI预设", "FantasyUIRef参数未填写完全", "ok");
                return;
            }

            var bundlePath = $"Assets/Bundles/{bundleName}";
            if (!AssetDatabase.IsValidFolder(bundlePath))
            {
                var moduleName = nameSpaceName.IndexOf('/') < 0 ? nameSpaceName : nameSpaceName[..nameSpaceName.IndexOf('/')];
                CreateModuleFolderHelper.CreateModuleFolder(moduleName);
            }

            // 生成脚本
            if (!GenerateScript(fantasyUIRef))
                return;

            // 保存预制体
            var prefab = fantasyUIRef.gameObject;
            var prefabPath = $"{bundlePath}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);

            // 剔除掉不需要的部分
            CutoutPrefab(prefabPath);

            // 保存当前场景
            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            EditorUtility.DisplayDialog("生成UI预设", "生成成功", "ok");
            AssetBundleEditorHelper.SetAllName();
            AssetDatabase.Refresh();
        }

        public static void CutoutPrefab(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Queue<Transform> transList = new();
            Queue<GameObject> destroyList = new();
            transList.Enqueue(prefab.transform);
            while (transList.Count > 0)
            {
                var transform = transList.Dequeue();
                var childCount = transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = transform.GetChild(i);
                    var fantasyUIRef = child.GetComponent<FantasyUIRef>();
                    if (fantasyUIRef is not null)
                    {
                        destroyList.Enqueue(child.gameObject);
                        continue;
                    }

                    transList.Enqueue(child);
                }

                while (destroyList.Count > 0)
                {
                    var go = destroyList.Dequeue();
                    Object.DestroyImmediate(go, true);
                }
            }

            PrefabUtility.SavePrefabAsset(prefab);
        }

        private static bool GenerateScript(FantasyUIRef fantasyUIRef)
        {
            var generatePath = FantasyUISettingsScriptableObject.Instance.uiGenerateSavePath;
            var isWidget = fantasyUIRef.isWidget;

            if (string.IsNullOrEmpty(fantasyUIRef.assetName))
            {
                EditorUtility.DisplayDialog("Generate Code", $"assetName is null", "OK");
                return false;
            }

            if (string.IsNullOrEmpty(fantasyUIRef.bundleName))
            {
                EditorUtility.DisplayDialog("Generate Code", $"bundleName is null", "OK");
                return false;
            }

            if (!isWidget && fantasyUIRef.uiLayer == UILayer.None)
            {
                EditorUtility.DisplayDialog("Generate Code", $"UILayer is None", "OK");
                return false;
            }

            if (string.IsNullOrEmpty(generatePath))
            {
                EditorUtility.DisplayDialog("Generate Code", $"UI Generate Save Path is null, please set it in FantasyUISettings", "OK");
                return false;
            }

            bool diffNameSpace = !string.IsNullOrEmpty(fantasyUIRef.nameSpaceName);
            generatePath = diffNameSpace ? $"{generatePath}/Module/{fantasyUIRef.nameSpaceName}" : $"{generatePath}/Entity";

            if (!Directory.Exists(generatePath))
            {
                Directory.CreateDirectory(generatePath);
            }

            var createStr = new List<string>();
            var propertyStr = new List<string>();

            for (var i = fantasyUIRef.list.Count - 1; i >= 0; i--)
            {
                var key = fantasyUIRef.list[i].key;
                var gameObject = fantasyUIRef.list[i].gameObject;
                var refName = gameObject.GetType().FullName;
                var gameObjectName = Regex.Replace(key, @"\s+", "");
                propertyStr.Add($"\t\tpublic {refName} {gameObjectName};");
                createStr.Add($"\t\t\t{gameObjectName} = referenceComponent.GetReference<{refName}>(\"{key}\");");
            }

            var sb = new StringBuilder();

            sb.AppendLine("using Fantasy;");
            sb.AppendLine(diffNameSpace ? $"\nnamespace Fantasy.{fantasyUIRef.nameSpaceName.Replace('/', '.')}\n{{" : "\nnamespace Fantasy\n{");
            sb.AppendLine(isWidget ? $"\tpublic partial class {fantasyUIRef.componentName} : SubUI\n\t{{" : $"\tpublic partial class {fantasyUIRef.componentName} : UI\n\t{{");
            sb.AppendLine($"\t\tpublic override string AssetName {{ get; protected set; }} = \"{fantasyUIRef.assetName}\";");
            sb.AppendLine($"\t\tpublic override string BundleName {{ get; protected set; }} = \"{fantasyUIRef.bundleName.ToLower()}\";");
            if (!isWidget)
                sb.AppendLine($"\t\tpublic override UILayer Layer {{ get; protected set; }} = UILayer.{fantasyUIRef.uiLayer.ToString()};\n");

            foreach (var property in propertyStr)
            {
                sb.AppendLine(property);
            }

            sb.AppendLine("\n\t\tpublic override void Initialize()\n\t\t{");
            sb.AppendLine("\t\t\tvar referenceComponent = GameObject.GetComponent<FantasyUIRef>();");

            foreach (var str in createStr)
            {
                sb.AppendLine(str);
            }

            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine("}");

            var combinePath = Path.Combine(generatePath, $"{fantasyUIRef.componentName}_gen.cs").Replace('\\', '/');
            using var entityStreamWriter = new StreamWriter(combinePath);
            entityStreamWriter.Write(sb.ToString());

            var propList = fantasyUIRef.list.Select(data => (data.gameObject.GetType(), Regex.Replace(data.key, @"\s+", ""))).ToList();
            var nameSpace = diffNameSpace ? $"Fantasy.{fantasyUIRef.nameSpaceName.Replace('/', '.')}" : "Fantasy";
            CodeGenerator.GenCode(nameSpace, fantasyUIRef.componentName, propList, Path.Combine(generatePath, $"{fantasyUIRef.componentName}.cs").Replace('\\', '/'));

            Debug.Log($"代码生成位置:{combinePath}");

            return true;
        }

        private static bool SetParamsForRef(FantasyUIRef fantasyUIRef, string bundleName, string assetName, string nameSpaceName, string componentName)
        {
            char firstChar = componentName[0];
            if (!char.IsUpper(firstChar))
            {
                EditorUtility.DisplayDialog("生成ui预设", $"当前Scene的名称首字母必须为大写！\n因为Scene的名字代表生成的预制体名字和脚本名字，脚本名字首字母按照规范必须为大写。\n请修改后再尝试", "ok");
                return false;
            }

            fantasyUIRef.bundleName = bundleName;
            fantasyUIRef.nameSpaceName = nameSpaceName;
            fantasyUIRef.componentName = componentName;
            fantasyUIRef.assetName = assetName;

            Queue<Transform> transList = new();
            transList.Enqueue(fantasyUIRef.transform);
            while (transList.Count > 0)
            {
                var transform = transList.Dequeue();
                var childCount = transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = transform.GetChild(i);
                    var fantasyRef = child.GetComponent<FantasyRef>();
                    if (fantasyRef is FantasyUIRef)
                        continue;
                    if (fantasyRef is not null)
                    {
                        fantasyRef.nameSpaceName = nameSpaceName;
                        continue;
                    }

                    transList.Enqueue(child);
                }
            }

            return true;
        }

        /// <summary>
        /// 根据名字自动设置好引用
        /// </summary>
        /// <param name="fr"></param>
        private static void AutoAddRef(FantasyRef fr)
        {
            fr.list.Clear();
            Queue<Transform> transList = new();
            List<FantasyRef> childRefList = new();
            transList.Enqueue(fr.transform);

            while (transList.Count > 0)
            {
                var tf = transList.Dequeue();
                var childCount = tf.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = tf.GetChild(i);
                    var childFantasyRef = child.GetComponent<FantasyRef>();
                    if (childFantasyRef is FantasyUIRef)
                        continue;
                    if (childFantasyRef)
                        childRefList.Add(childFantasyRef);
                    else
                        transList.Enqueue(child);

                    var name = child.name;
                    var list = FantasyAutoRefRuleHelper.GetRefInfoByName(name);
                    if (string.IsNullOrEmpty(list[0]))
                        continue;
                    var obj = FantasyAutoRefRuleHelper.GetComponentByKey(child.gameObject, list[0]);
                    var refName = FantasyAutoRefRuleHelper.GetRefName(name);
                    fr.list.Add(new FantasyUIData
                    {
                        key = refName,
                        gameObject = obj
                    });
                }
            }

            foreach (var childRef in childRefList)
            {
                AutoAddRef(childRef);
            }
        }

        /// <summary>
        /// 检查预制体中重复的节点名
        /// </summary>
        private static bool CheckRepeatedName(GameObject go)
        {
            Dictionary<string, GameObject> names = new();
            Dictionary<string, GameObject> refNames = new();
            List<GameObject> fantasyRefList = new();
            Queue<Transform> transList = new();
            transList.Enqueue(go.transform);
            while (transList.Count > 0)
            {
                var tf = transList.Dequeue();
                var childCount = tf.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = tf.GetChild(i);
                    var fantasyUIRef = child.GetComponent<FantasyUIRef>();
                    if (fantasyUIRef)
                        continue;
                    if (!names.TryAdd(child.name, child.gameObject))
                    {
                        var gameObject = child.gameObject;
                        Selection.objects = new Object[] { gameObject, names[child.name] };
                        EditorGUIUtility.PingObject(gameObject);
                        EditorUtility.DisplayDialog("检查重复节点名", "同节点下，同层级，不得出现重复的名字", "ok");
                        return false;
                    }

                    var list = FantasyAutoRefRuleHelper.GetRefInfoByName(child.name);
                    if (!string.IsNullOrEmpty(list[0]) && !refNames.TryAdd(child.name, child.gameObject))
                    {
                        var gameObject = child.gameObject;
                        Selection.objects = new Object[] { gameObject, refNames[child.name] };
                        EditorGUIUtility.PingObject(gameObject);
                        EditorUtility.DisplayDialog("检查重复节点名", "不得出现重复的引用名", "ok");
                        return false;
                    }

                    var childFantasyRef = child.GetComponent<FantasyRef>();
                    if (childFantasyRef != null)
                    {
                        fantasyRefList.Add(child.gameObject);
                        continue;
                    }

                    transList.Enqueue(child);
                }

                names.Clear();
            }

            foreach (var gameObject in fantasyRefList)
            {
                if (!CheckRepeatedName(gameObject))
                    return false;
            }

            return true;
        }
    }
}