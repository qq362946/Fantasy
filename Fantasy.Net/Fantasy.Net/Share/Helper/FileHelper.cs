using System.IO;

namespace Fantasy.Helper
{
    public static class FileHelper
    {
        public static void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite)
        {
            // 创建目标文件夹

            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // 获取当前文件夹中的所有文件

            var files = Directory.GetFiles(sourceDirectory);

            // 拷贝文件到目标文件夹

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destinationPath = Path.Combine(destinationDirectory, fileName);
                File.Copy(file, destinationPath, overwrite);
            }

            // 获取源文件夹中的所有子文件夹

            var directories = Directory.GetDirectories(sourceDirectory);

            // 递归方式拷贝文件夹

            foreach (var directory in directories)
            {
                var directoryName = Path.GetFileName(directory);
                var destinationPath = Path.Combine(destinationDirectory, directoryName);
                CopyDirectory(directory, destinationPath, overwrite);
            }
        }
    }
}