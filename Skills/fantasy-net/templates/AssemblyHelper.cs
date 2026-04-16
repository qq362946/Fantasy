using System.Runtime.Loader;
using Fantasy.Helper;

namespace Fantasy;

public static class AssemblyHelper
{
    private const string HotfixDll = "Hotfix";
    private static AssemblyLoadContext? _assemblyLoadContext;

    public static void Initialize()
    {
        typeof(AssemblyHelper).Assembly.EnsureLoaded();
        LoadHotfixAssembly();
    }

    public static System.Reflection.Assembly LoadHotfixAssembly()
    {
        _assemblyLoadContext?.Unload();
        System.GC.Collect();
        _assemblyLoadContext = new AssemblyLoadContext(HotfixDll, isCollectible: true);
        var dir = AppContext.BaseDirectory;
        var asm = _assemblyLoadContext.LoadFromStream(
            new MemoryStream(File.ReadAllBytes(Path.Combine(dir, $"{HotfixDll}.dll"))),
            new MemoryStream(File.ReadAllBytes(Path.Combine(dir, $"{HotfixDll}.pdb"))));
        asm.EnsureLoaded();
        return asm;
    }
}
