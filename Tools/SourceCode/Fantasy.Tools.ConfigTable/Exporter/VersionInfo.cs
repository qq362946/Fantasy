namespace Fantasy.Tools.ConfigTable;

public class VersionInfo
{
    public SortedSet<long> WorksheetNames = [];
    public SortedDictionary<long, long> Tables = new();
}