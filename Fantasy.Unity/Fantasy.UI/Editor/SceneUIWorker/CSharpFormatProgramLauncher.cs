using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Fantasy
{
    public class CSharpFormatProgramLauncher
    {
        private readonly string _appPath;
        public string Result { get; private set; }
        public string ErrorMsg { get; private set; }
        public bool Success { get; private set; }

        public CSharpFormatProgramLauncher(string appPath)
        {
            _appPath = appPath;
        }

        public bool LaunchFormatFile(string scriptPath)
        {
            Process process = new Process();
            process.StartInfo.FileName = _appPath;
            process.StartInfo.Arguments = $"script {scriptPath}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            try
            {
                process.Start();
                ErrorMsg = process.StandardError.ReadToEnd();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                Success = exitCode == 0;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                Success = false;
            }

            return Success;
        }

        public bool LaunchFormatCode(string codeStr)
        {
            Process process = new Process();
            process.StartInfo.FileName = _appPath;
            process.StartInfo.Arguments = $"code {codeStr}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            try
            {
                process.Start();
                Result = process.StandardOutput.ReadToEnd();
                ErrorMsg = process.StandardError.ReadToEnd();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                Success = exitCode == 0;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                Success = false;
            }

            return Success;
        }

        public bool LaunchFormatCode(string codeStr, [NotNullWhen(true)]out string formattedCode)
        {
            formattedCode = default;
            if (LaunchFormatCode(codeStr))
                formattedCode = Result;
            return Success;
        }
    }
}