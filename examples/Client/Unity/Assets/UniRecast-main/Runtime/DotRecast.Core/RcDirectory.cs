using System.IO;
using System.Linq;

namespace DotRecast.Core
{
    public static class RcDirectory
    {
        public static string SearchPath(string searchPath, int depth, out bool isDir)
        {
            isDir = false;

            for (int i = 0; i < depth; ++i)
            {
                var relativePath = string.Join("", Enumerable.Range(0, i).Select(x => "../"));
                var searchingPath = Path.Combine(relativePath, searchPath);
                var fullSearchingPath = Path.GetFullPath(searchingPath);

                if (File.Exists(fullSearchingPath))
                {
                    return fullSearchingPath;
                }
                
                if (Directory.Exists(fullSearchingPath))
                {
                    isDir = true;
                    return fullSearchingPath;
                }
            }

            return string.Empty;
        }

        // only directory
        public static string SearchDirectory(string dirname, int depth = 10)
        {
            var searchingPath = SearchPath(dirname, depth, out var isDir);
            if (isDir)
            {
                return searchingPath;
            }

            var path = Path.GetDirectoryName(searchingPath) ?? string.Empty;
            return path;
        }

        public static string SearchFile(string filename, int depth = 10)
        {
            var searchingPath = SearchPath(filename, depth, out var isDir);
            if (!isDir)
            {
                return searchingPath;
            }

            return string.Empty;
        }
    }
}