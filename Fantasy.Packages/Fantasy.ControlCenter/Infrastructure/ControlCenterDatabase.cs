using Fantasy.ControlCenter.Options;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Fantasy.ControlCenter.Infrastructure;

/// <summary>
/// SqlSugar 只负责控制中心配置的低频持久化。
/// 服务注册、心跳和发现不经过数据库。
/// </summary>
public sealed class ControlCenterDatabase
{
    private const int SchemaVersion = 1;

    public ControlCenterDatabase(IOptions<ControlCenterOptions> options)
    {
        var dataDirectory = options.Value.DataDirectory;
        if (!Path.IsPathRooted(dataDirectory))
        {
            dataDirectory = Path.Combine(AppContext.BaseDirectory, dataDirectory);
        }

        Directory.CreateDirectory(dataDirectory);
        DatabasePath = Path.Combine(dataDirectory, "fantasy-control.db");
        Db = new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = $"Data Source={DatabasePath};Cache=Shared;Foreign Keys=True;Pooling=True;Default Timeout=5",
            DbType = DbType.Sqlite,
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        });
    }

    public string DatabasePath { get; }

    public SqlSugarScope Db { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Db.Ado.ExecuteCommandAsync("""
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;
            """);

        // ponytail: 当前数据库可丢弃；正式需要跨版本保留数据时再增加迁移。
        if (await Db.Ado.GetIntAsync("PRAGMA user_version;") == SchemaVersion)
        {
            return;
        }

        await Db.Ado.BeginTranAsync();
        try
        {
            await Db.Ado.ExecuteCommandAsync("""
            DROP TABLE IF EXISTS ServiceInstances;
            DROP TABLE IF EXISTS Scenes;
            DROP TABLE IF EXISTS Databases;
            DROP TABLE IF EXISTS Worlds;
            DROP TABLE IF EXISTS Processes;
            DROP TABLE IF EXISTS WorldGroups;
            DROP TABLE IF EXISTS Namespaces;
            DROP TABLE IF EXISTS Machines;
            DROP TABLE IF EXISTS Metadata;
            DROP TABLE IF EXISTS ControlCenterState;

            CREATE TABLE Machines (
                Id              INTEGER PRIMARY KEY,
                Name            TEXT NOT NULL,
                OuterIp         TEXT NOT NULL,
                OuterBindIp     TEXT NOT NULL,
                InnerBindIp     TEXT NOT NULL,
                Enabled         INTEGER NOT NULL
            );

            CREATE TABLE Namespaces (
                Id              INTEGER PRIMARY KEY,
                Name            TEXT NOT NULL,
                Enabled         INTEGER NOT NULL
            );

            CREATE TABLE Processes (
                Id              INTEGER PRIMARY KEY,
                NamespaceId     INTEGER NOT NULL,
                MachineId       INTEGER NOT NULL,
                Name            TEXT NOT NULL,
                StartupGroup    INTEGER NOT NULL,
                Enabled         INTEGER NOT NULL,
                FOREIGN KEY (NamespaceId) REFERENCES Namespaces(Id) ON DELETE RESTRICT,
                FOREIGN KEY (MachineId) REFERENCES Machines(Id) ON DELETE RESTRICT
            );

            CREATE INDEX IX_Processes_NamespaceId ON Processes(NamespaceId);
            CREATE INDEX IX_Processes_MachineId ON Processes(MachineId);

            CREATE TABLE WorldGroups (
                Id              INTEGER PRIMARY KEY,
                NamespaceId     INTEGER NOT NULL,
                Name            TEXT NOT NULL,
                Enabled         INTEGER NOT NULL,
                FOREIGN KEY (NamespaceId) REFERENCES Namespaces(Id) ON DELETE RESTRICT
            );

            CREATE INDEX IX_WorldGroups_NamespaceId ON WorldGroups(NamespaceId);

            CREATE TABLE Worlds (
                Id              INTEGER PRIMARY KEY,
                GroupId         INTEGER NOT NULL,
                Name            TEXT NOT NULL,
                Enabled         INTEGER NOT NULL,
                FOREIGN KEY (GroupId) REFERENCES WorldGroups(Id) ON DELETE RESTRICT
            );

            CREATE INDEX IX_Worlds_GroupId ON Worlds(GroupId);

            CREATE TABLE Databases (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                WorldId         INTEGER NOT NULL,
                DbType          TEXT NOT NULL,
                DbName          TEXT NOT NULL,
                DbConnection    TEXT NOT NULL,
                IsDefault       INTEGER NOT NULL,
                FOREIGN KEY (WorldId) REFERENCES Worlds(Id) ON DELETE CASCADE,
                UNIQUE (WorldId, DbName)
            );

            CREATE INDEX IX_Databases_WorldId ON Databases(WorldId);
            CREATE UNIQUE INDEX UX_Databases_DefaultPerWorld
                ON Databases(WorldId) WHERE IsDefault = 1;

            CREATE TABLE Scenes (
                Id              INTEGER PRIMARY KEY,
                ProcessId       INTEGER NOT NULL,
                WorldId         INTEGER NOT NULL,
                SceneType       TEXT NOT NULL,
                RuntimeMode     TEXT NOT NULL,
                NetworkProtocol TEXT NOT NULL,
                OuterPort       INTEGER NOT NULL,
                InnerPort       INTEGER NOT NULL,
                Enabled         INTEGER NOT NULL,
                FOREIGN KEY (ProcessId) REFERENCES Processes(Id) ON DELETE RESTRICT,
                FOREIGN KEY (WorldId) REFERENCES Worlds(Id) ON DELETE RESTRICT
            );

            CREATE INDEX IX_Scenes_ProcessId ON Scenes(ProcessId);
            CREATE INDEX IX_Scenes_WorldType ON Scenes(WorldId, SceneType);

            CREATE TABLE ControlCenterState (
                Id       INTEGER PRIMARY KEY CHECK (Id = 1),
                Revision INTEGER NOT NULL
            );

            INSERT INTO ControlCenterState(Id, Revision) VALUES (1, 0);
            PRAGMA user_version = 1;
            """);
            await Db.Ado.CommitTranAsync();
        }
        catch
        {
            await Db.Ado.RollbackTranAsync();
            throw;
        }
    }
}
