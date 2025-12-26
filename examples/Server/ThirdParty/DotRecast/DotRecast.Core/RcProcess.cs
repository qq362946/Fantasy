using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DotRecast.Core
{
    public static class RcProcess
    {
        public static void OpenUrl(string url)
        {
            try
            {
                // OS에 따라 다른 명령 실행
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var psi = new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true };
                    Process.Start(psi);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening web browser: {ex.Message}");
            }
        }
    }
}