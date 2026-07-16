using Fantasy.ControlCenter.Models;
using SqlSugar;

namespace Fantasy.ControlCenter.Infrastructure;

/// <summary>
/// 配置仓储。只有启动和后台修改配置时会访问这里。
/// </summary>
public sealed class ControlCenterRepository(ControlCenterDatabase database)
{
    private readonly SqlSugarScope _db = database.Db;

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        database.InitializeAsync(cancellationToken);

    public async Task<TopologySnapshot> LoadTopologyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // SqlSugarScope 是单例；顺序读取避免同一个连接上下文并发执行异步查询。
        var namespaces = await _db.Queryable<NamespaceDefinition>().OrderBy(x => x.Id).ToListAsync();
        var machines = await _db.Queryable<MachineDefinition>().OrderBy(x => x.Id).ToListAsync();
        var processes = await _db.Queryable<ProcessDefinition>().OrderBy(x => x.Id).ToListAsync();
        var worldGroups = await _db.Queryable<WorldGroupDefinition>().OrderBy(x => x.Id).ToListAsync();
        var worlds = await _db.Queryable<WorldDefinition>().OrderBy(x => x.Id).ToListAsync();
        var databases = await _db.Queryable<DatabaseDefinition>()
            .OrderBy(x => x.WorldId)
            .OrderBy(x => x.Id)
            .ToListAsync();
        var scenes = await _db.Queryable<SceneDefinition>().OrderBy(x => x.Id).ToListAsync();
        var revision = await GetRevisionAsync(cancellationToken);

        return new TopologySnapshot(namespaces, machines, processes, worldGroups, worlds, databases, scenes, revision);
    }

    public Task<long> SaveNamespaceAsync(
        NamespaceDefinition definition,
        CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Storageable(definition).ExecuteCommandAsync(), cancellationToken);

    public Task<long> DeleteNamespaceAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Deleteable<NamespaceDefinition>().In(id).ExecuteCommandAsync(), cancellationToken);

    public Task<long> SaveMachineAsync(MachineDefinition machine, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Storageable(machine).ExecuteCommandAsync(), cancellationToken);

    public Task<long> DeleteMachineAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Deleteable<MachineDefinition>().In(id).ExecuteCommandAsync(), cancellationToken);

    public Task<long> SaveProcessAsync(ProcessDefinition process, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Storageable(process).ExecuteCommandAsync(), cancellationToken);

    public Task<long> DeleteProcessAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Deleteable<ProcessDefinition>().In(id).ExecuteCommandAsync(), cancellationToken);

    public Task<long> SaveWorldGroupAsync(
        WorldGroupDefinition group,
        CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Storageable(group).ExecuteCommandAsync(), cancellationToken);

    public Task<long> DeleteWorldGroupAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Deleteable<WorldGroupDefinition>().In(id).ExecuteCommandAsync(), cancellationToken);

    public Task<long> SaveWorldAsync(WorldDefinition world, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Storageable(world).ExecuteCommandAsync(), cancellationToken);

    public Task<long> DeleteWorldAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Deleteable<WorldDefinition>().In(id).ExecuteCommandAsync(), cancellationToken);

    public async Task<(long Id, long Revision)> SaveDatabaseAsync(
        DatabaseDefinition definition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.Ado.BeginTranAsync();
        try
        {
            if (definition.IsDefault)
            {
                await _db.Updateable<DatabaseDefinition>()
                    .SetColumns(x => x.IsDefault == false)
                    .Where(x => x.WorldId == definition.WorldId && x.Id != definition.Id && x.IsDefault)
                    .ExecuteCommandAsync();
            }

            if (definition.Id == 0)
            {
                definition.Id = await _db.Insertable(definition).ExecuteReturnBigIdentityAsync();
            }
            else if (await _db.Updateable(definition).ExecuteCommandAsync() == 0)
            {
                throw new InvalidOperationException($"数据库配置 {definition.Id} 不存在。");
            }

            var revision = await IncrementRevisionAsync();
            await _db.Ado.CommitTranAsync();
            return (definition.Id, revision);
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    public Task<long> DeleteDatabaseAsync(
        long id,
        uint? worldId = null,
        CancellationToken cancellationToken = default)
    {
        var delete = _db.Deleteable<DatabaseDefinition>().Where(x => x.Id == id);
        if (worldId.HasValue)
        {
            var value = worldId.Value;
            delete = delete.Where(x => x.WorldId == value);
        }

        return MutateAsync(delete.ExecuteCommandAsync, cancellationToken);
    }

    public Task<long> SaveSceneAsync(SceneDefinition scene, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Storageable(scene).ExecuteCommandAsync(), cancellationToken);

    public Task<long> DeleteSceneAsync(uint id, CancellationToken cancellationToken = default) =>
        MutateAsync(() => _db.Deleteable<SceneDefinition>().In(id).ExecuteCommandAsync(), cancellationToken);

    private async Task<long> GetRevisionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _db.Queryable<ControlCenterStateEntity>()
            .Where(x => x.Id == 1)
            .Select(x => x.Revision)
            .FirstAsync();
    }

    private async Task<long> MutateAsync(Func<Task<int>> mutation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.Ado.BeginTranAsync();
        try
        {
            await mutation();
            var revision = await IncrementRevisionAsync();
            await _db.Ado.CommitTranAsync();
            return revision;
        }
        catch
        {
            await _db.Ado.RollbackTranAsync();
            throw;
        }
    }

    private Task<long> IncrementRevisionAsync() =>
        _db.Ado.GetLongAsync("""
            UPDATE ControlCenterState
            SET Revision = Revision + 1
            WHERE Id = 1
            RETURNING Revision;
            """);

    [SugarTable("ControlCenterState", IsDisabledDelete = true)]
    private sealed class ControlCenterStateEntity
    {
        [SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }

        public long Revision { get; set; }
    }
}
