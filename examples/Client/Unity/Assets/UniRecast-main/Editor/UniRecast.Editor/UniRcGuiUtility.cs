using UnityEditor;
using UnityEngine;

namespace UniRecast.Editor
{
    public static class UniRcGuiUtility
    {
        public static GameObject CreateAndSelectGameObject(string suggestedName, GameObject parent)
        {
            var parentTransform = parent != null ? parent.transform : null;
            var uniqueName = GameObjectUtility.GetUniqueNameForSibling(parentTransform, suggestedName);
            var child = new GameObject(uniqueName);

            Undo.RegisterCreatedObjectUndo(child, "Create " + uniqueName);
            if (parentTransform != null)
                Undo.SetTransformParent(child.transform, parentTransform, "Parent " + uniqueName);

            Selection.activeGameObject = child;

            return child;
        }
        
        public static SerializedProperty FindPropertySafe(this SerializedObject serializedObject, string name)
        {
            string safeName = name;
            if (name[0] == '_')
            {
                safeName = name.Substring(1);
            }

            return serializedObject.FindProperty(safeName);
        }
    }
}