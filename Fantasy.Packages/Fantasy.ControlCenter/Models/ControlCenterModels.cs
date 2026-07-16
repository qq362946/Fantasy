using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SqlSugar;

namespace Fantasy.ControlCenter.Models;

[SugarTable("Namespaces", IsDisabledDelete = true)]
public sealed class NamespaceDefinition
{
    [SugarColumn(IsPrimaryKey = true)]
    [ValidateNever]
    [Range(1, uint.MaxValue)]
    public uint Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}

[SugarTable("Machines", IsDisabledDelete = true)]
public sealed class MachineDefinition
{
    [SugarColumn(IsPrimaryKey = true)]
    [ValidateNever]
    [Range(1, uint.MaxValue)]
    public uint Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string OuterIp { get; set; } = "127.0.0.1";

    [Required, StringLength(255)]
    public string OuterBindIp { get; set; } = "127.0.0.1";

    [Required, StringLength(255)]
    public string InnerBindIp { get; set; } = "127.0.0.1";

    public bool Enabled { get; set; } = true;
}

[SugarTable("Processes", IsDisabledDelete = true)]
public sealed class ProcessDefinition
{
    [SugarColumn(IsPrimaryKey = true)]
    [ValidateNever]
    [Range(1, uint.MaxValue)]
    public uint Id { get; set; }

    [Range(1, uint.MaxValue)]
    public uint NamespaceId { get; set; }

    [Range(1, uint.MaxValue)]
    public uint MachineId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public uint StartupGroup { get; set; }
    public bool Enabled { get; set; } = true;
}

[SugarTable("WorldGroups", IsDisabledDelete = true)]
public sealed class WorldGroupDefinition
{
    [SugarColumn(IsPrimaryKey = true)]
    [ValidateNever]
    [Range(1, uint.MaxValue)]
    public uint Id { get; set; }

    [Range(1, uint.MaxValue)]
    public uint NamespaceId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}

[SugarTable("Worlds", IsDisabledDelete = true)]
public sealed class WorldDefinition
{
    [SugarColumn(IsPrimaryKey = true)]
    [ValidateNever]
    [Range(1, uint.MaxValue)]
    public uint Id { get; set; }

    [Range(1, uint.MaxValue)]
    public uint GroupId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}

[SugarTable("Databases", IsDisabledDelete = true)]
public sealed class DatabaseDefinition
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [ValidateNever]
    [Range(1, uint.MaxValue)]
    public uint WorldId { get; set; }

    [Required, StringLength(32)]
    public string DbType { get; set; } = "MongoDB";

    [Required, StringLength(128)]
    public string DbName { get; set; } = string.Empty;

    [StringLength(4096)]
    public string DbConnection { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

[SugarTable("Scenes", IsDisabledDelete = true)]
public sealed class SceneDefinition
{
    [SugarColumn(IsPrimaryKey = true)]
    [ValidateNever]
    [Range(1, uint.MaxValue)]
    public uint Id { get; set; }

    [Range(1, uint.MaxValue)]
    public uint ProcessId { get; set; }

    [Range(1, uint.MaxValue)]
    public uint WorldId { get; set; }

    [Required, StringLength(80)]
    public string SceneType { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string RuntimeMode { get; set; } = "MultiThread";

    [StringLength(20)]
    public string NetworkProtocol { get; set; } = string.Empty;

    [Range(0, 65535)]
    public int OuterPort { get; set; }

    [Range(1, 65535)]
    public int InnerPort { get; set; }

    public bool Enabled { get; set; } = true;
}

public sealed class ServiceInstanceView
{
    public string InstanceId { get; set; } = string.Empty;
    public uint SceneId { get; set; }
    public string SceneType { get; set; } = string.Empty;
    public long Address { get; set; }
    public bool IsSubScene { get; set; }
    public string ParentInstanceId { get; set; } = string.Empty;
    public long ParentAddress { get; set; }
    public uint NamespaceId { get; set; }
    public uint WorldGroupId { get; set; }
    public uint WorldId { get; set; }
    public string Host { get; set; } = string.Empty;
    public int InnerPort { get; set; }
    public int OuterPort { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset LastHeartbeatUtc { get; set; }
    public DateTimeOffset LeaseExpiresAtUtc { get; set; }
}

public sealed record TopologySnapshot(
    IReadOnlyList<NamespaceDefinition> Namespaces,
    IReadOnlyList<MachineDefinition> Machines,
    IReadOnlyList<ProcessDefinition> Processes,
    IReadOnlyList<WorldGroupDefinition> WorldGroups,
    IReadOnlyList<WorldDefinition> Worlds,
    IReadOnlyList<DatabaseDefinition> Databases,
    IReadOnlyList<SceneDefinition> Scenes,
    long Revision);

public sealed record ControlCenterSummary(
    int NamespaceCount,
    int MachineCount,
    int ProcessCount,
    int WorldCount,
    int SceneCount,
    int OnlineInstanceCount,
    int OfflineInstanceCount,
    long Revision);
