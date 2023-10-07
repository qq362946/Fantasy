using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fantasy.Editor
{
    [ScriptableObjectPath("ProjectSettings/FantasySettings.asset")]
    public class FantasySettingsScriptableObject : ScriptableObjectSingleton<FantasySettingsScriptableObject>
    {
        [FormerlySerializedAs("AutoCopyAssembly")] 
        [Header("自动拷贝程序集到HotUpdatePath目录中")]
        public bool autoCopyAssembly = false;
        [FormerlySerializedAs("UIGenerateSavePath")] 
        [Header("UI生层代码路径")]
        public string uiGenerateSavePath;
        [FormerlySerializedAs("EditorModel")] 
        [Header("是否是编辑器开发模式(关闭会通过远程服务器下载更新资源)")]
        public bool editorModel;
        [FormerlySerializedAs("RemoteUpdatePath")] 
        [Header("远程资源服务器地址(http://127.0.0.1)")]
        public string remoteUpdatePath;
        [FormerlySerializedAs("HotUpdatePath")] 
        [Header("HotUpdate目录(Unity编译后会把所有HotUpdate程序集Copy一份到这个目录下)")]
        public string hotUpdatePath;
        [FormerlySerializedAs("HotUpdateAssemblyDefinitions")] 
        [Header("HotUpdate程序集")]
        public AssemblyDefinitionAsset[] hotUpdateAssemblyDefinitions;
    }
}