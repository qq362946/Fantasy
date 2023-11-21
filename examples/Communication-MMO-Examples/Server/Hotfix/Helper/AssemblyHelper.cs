#pragma warning disable CS8603

namespace Fantasy;

/// <summary>
/// 整个框架使用的程序集、有几个程序集就定义集。这里定义是为了后面方面使用
/// </summary>
public static class AssemblyName
{
    public const int Hotfix = 1;
}

public static class AssemblyHelper
{
    public static void Initialize()
    {
        LoadHotfix();
    }

    public static void LoadHotfix()
    {
        AssemblyManager.Load(AssemblyName.Hotfix, typeof(AssemblyHelper).Assembly);
    }
}