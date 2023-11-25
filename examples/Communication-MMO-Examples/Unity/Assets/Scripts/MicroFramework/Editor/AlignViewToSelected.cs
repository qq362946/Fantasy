using UnityEditor;
using UnityEngine;


public static class CustomMenu
{
    // % 代表 Ctrl, # 代表 Shift, & 代表 Alt, _g 代表G键
    [MenuItem("Micro/Align View to Selected #G")]
    public static void AlignViewToSelected()
    {
        if (SceneView.lastActiveSceneView == null || Camera.main == null) return;
        
        // Selection.activeTransform 层级面板中选中的物体Transform
        SceneView.lastActiveSceneView.AlignViewToObject(Selection.activeTransform);
    }
}
