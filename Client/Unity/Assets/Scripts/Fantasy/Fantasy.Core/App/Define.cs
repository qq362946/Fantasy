using System.Diagnostics;
using Fantasy.Helper;

namespace Fantasy
{
    public class Define
    {
#if FANTASY_SERVER
        public static CommandLineOptions Options;
        public static uint AppId => Options.AppId;
#endif
#if FANTASY_UNITY
        public static string RemoteUpdatePath;
        public static bool EditorModel = true;
        public const string VersionName = "version.bytes";
        public const string VersionMD5Name = "version.md5";
        public const string AssetBundleManifestName = "Fantasy";
        public static bool IsEditor => UnityEngine.Application.isEditor && EditorModel;
        public static string AppHotfixResPath => $"{UnityEngine.Application.persistentDataPath}";
#endif
    }
}