using UnityEditor;

namespace Fantasy
{
    public class FantasyUISettings
    {
        [MenuItem("Fantasy/FantasyUI Settings")]
        public static void OpenFantasySettings()
        {
            SettingsService.OpenProjectSettings("Project/FantasyUI Settings");
        }
    }
}