using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fantasy
{
    public static class CreateModuleFolderHelper
    {
        public static string CreateModuleFolder(string moduleName)
        {
            AssetDatabase.CreateFolder("Assets/Bundles/UI", moduleName);
            var folderPath = $"Assets/Bundles/UI/{moduleName}";
            AssetDatabase.CreateFolder(folderPath, "prefab");
            AssetDatabase.CreateFolder(folderPath, "atlas");
            AssetDatabase.CreateFolder(folderPath, "texture");
            return folderPath;
        }
        
        [MenuItem("Assets/Create/Module Folder", false, priority = 20)]
        private static void CreateModuleFolder(MenuCommand menuCommand)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Bundles/UI"))
            {
                Directory.CreateDirectory("Assets/Bundles/UI");
            }

            var icon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateModuleFolder>(), "NewModule", icon, "NewModule");
        }

        [MenuItem("Assets/Create/Module Folder", true)]
        private static bool CreateModuleFolderValidateFunction()
        {
            var obj = Selection.activeObject;
            if (!obj)
                return true;
            var path = AssetDatabase.GetAssetPath(obj);
            return path is "Assets/Bundles/UI";
        }

        private class DoCreateModuleFolder : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                CreateModuleFolder(pathName[(pathName.LastIndexOf('/') + 1)..]);
                var moduleFolder = AssetDatabase.LoadAssetAtPath<Object>(pathName);
                ProjectWindowUtil.ShowCreatedAsset(moduleFolder);
            }
        }
    }
}