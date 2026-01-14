namespace SRDebugger.Internal
{
    using System.Collections;
    using UnityEngine;

    public class BugReportScreenshotUtil
    {
        public static byte[] ScreenshotData;

        public static IEnumerator ScreenshotCaptureCo()
        {
            if (ScreenshotData != null)
            {
                Debug.LogWarning("[SRDebugger] Warning, overriding existing screenshot data.");
            }

            yield return new WaitForEndOfFrame();

            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();

            ScreenshotData = tex.EncodeToPNG();

            Object.Destroy(tex);
        }
    }
}
