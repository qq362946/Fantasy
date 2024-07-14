using UnityEditor;
using UnityEngine;

namespace Fantasy
{
    public class CreateSceneWorktable
    {
        public static Object CreateUIWorktableScene(string savePath)
        {
            const string templatePath = "Packages/com.fantasy.ui/Editor/SceneUIWorker/UIWorktableTemplate.unity";
            if (AssetDatabase.CopyAsset(templatePath, savePath))
                return AssetDatabase.LoadAssetAtPath<Object>(savePath);
            EditorUtility.DisplayDialog("创建UI编辑场景", $"找不到场景模板：{templatePath}", "ok");
            return null;
        }

        [MenuItem("Assets/Create/UI Worktable Scene", false, priority = 20)]
        private static void CreateUIWorktableScene(MenuCommand menuCommand)
        {
            var icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateSceneWorktable>(), "NewModule.unity", icon, "NewModule.unity");
        }

        [MenuItem("Assets/Create/UI Worktable Scene", true)]
        private static bool CreateUIWorktableSceneValidateFunction()
        {
            var obj = Selection.activeObject;
            if (!obj)
                return false;
            var path = AssetDatabase.GetAssetPath(obj);
            return path.StartsWith("Assets/Scenes/UI/");
        }

        private class DoCreateSceneWorktable : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var sceneAsset = CreateUIWorktableScene(pathName);
                ProjectWindowUtil.ShowCreatedAsset(sceneAsset);
            }
        }
    }
}