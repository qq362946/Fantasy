#if FANTASY_NET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fantasy.Helper;
using Fantasy.Platform.Net;

namespace Fantasy;

/// <summary>
/// 当前 OS 进程的服务注册和批量心跳执行器。
/// 支持 Root Scene 和 SubScene 在运行期间动态注册与下线。
/// </summary>
internal sealed class ServiceDiscoveryWorker
{
    /// <summary>
    /// 两次批量心跳之间的等待时间。
    /// </summary>
    private readonly TimeSpan _heartbeatInterval;

    /// <summary>
    /// Control Center HTTP 客户端。
    /// </summary>
    private readonly ControlCenterClient _client;

    /// <summary>
    /// 注册和续约使用的租约时长。
    /// </summary>
    private readonly int _leaseSeconds;

    /// <summary>
    /// 当前服务器启动标识。
    /// 同一个启动周期内保持不变。
    /// </summary>
    private readonly string _startupId = Guid.NewGuid().ToString("N");

    /// <summary>
    /// SceneType 整数常量到名称的映射。
    /// 用于将 SubScene.SceneType 转换成协议字符串。
    /// </summary>
    private readonly Dictionary<int, string> _sceneTypeNames;

    /// <summary>
    /// 注册信息变更锁。
    /// 只保护低频的创建、销毁和心跳快照重建。
    /// </summary>
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    /// <summary>
    /// 按运行时 Address 保存当前有效注册。
    /// 只允许在 mutationLock 中读写。
    /// </summary>
    private readonly Dictionary<long, RegistrationEntry> _registrationsByAddress = new();

    /// <summary>
    /// 按实例 ID 保存当前有效注册。
    /// 心跳拒绝后通过该索引判断实例是否仍然有效。
    /// </summary>
    private readonly ConcurrentDictionary<string, RegistrationEntry> _registrationsById = new(StringComparer.Ordinal);

    /// <summary>
    /// 心跳线程使用的不可变快照。
    /// 只有注册集合发生变化时才重新构建。
    /// </summary>
    private RegistrationSnapshot _snapshot = RegistrationSnapshot.Empty;

    /// <summary>
    /// 注册集合发生变化时为 1。
    /// 多次变化会合并成一次快照重建。
    /// </summary>
    private int _snapshotDirty;

    /// <summary>
    /// 控制后台循环停止的取消源。
    /// </summary>
    private readonly CancellationTokenSource _stopTokenSource = new();

    /// <summary>
    /// 后台注册和心跳任务。
    /// </summary>
    private Task? _runTask;

    private int _started;
    private int _stopped;

    /// <summary>
    /// 创建服务发现 Worker，并收集当前进程已经启动的 Root Scene。
    /// </summary>
    internal ServiceDiscoveryWorker(
        ControlCenterClient client,
        IReadOnlyList<Process> processes,
        int heartbeatIntervalSeconds,
        int leaseSeconds)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(processes);

        _client = client;
        _leaseSeconds = leaseSeconds;
        _heartbeatInterval =
            TimeSpan.FromSeconds(heartbeatIntervalSeconds);

        var sceneTypes = Scene.SceneTypeDictionary;
        _sceneTypeNames =
            new Dictionary<int, string>(sceneTypes.Count);

        foreach (var sceneType in sceneTypes)
        {
            _sceneTypeNames.Add(
                sceneType.Value,
                sceneType.Key);
        }

        var initialEntries =
            BuildInitialRootEntries(
                processes,
                leaseSeconds,
                _startupId);

        for (var i = 0; i < initialEntries.Length; i++)
        {
            var entry = initialEntries[i];

            _registrationsByAddress.Add(
                entry.Address,
                entry);

            if (!_registrationsById.TryAdd(
                    entry.InstanceId,
                    entry))
            {
                throw new InvalidOperationException(
                    $"Duplicate service instance ID: {entry.InstanceId}");
            }
        }

        _snapshot = BuildSnapshot();
    }

    /// <summary>
    /// 当前 Worker 管理的 Root Scene 和 SubScene 数量。
    /// </summary>
    internal int InstanceCount => _registrationsById.Count;

    /// <summary>
    /// 启动后台注册和心跳循环。
    /// </summary>
    internal void Start()
    {
        if (Volatile.Read(ref _stopped) != 0)
        {
            throw new ObjectDisposedException(nameof(ServiceDiscoveryWorker));
        }

        if (Interlocked.CompareExchange(
                ref _started,
                1,
                0) != 0)
        {
            throw new InvalidOperationException("Service discovery worker has already started.");
        }

        _runTask = RunAsync(_stopTokenSource.Token);
    }

    /// <summary>
    /// 动态注册一个 Root Scene 或 SubScene。
    /// 注册信息会先加入本地集合，即使 HTTP 请求失败，
    /// 后续心跳也会自动触发重新注册。
    /// </summary>
    internal async Task RegisterSceneAsync(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentOutOfRangeException.ThrowIfZero(scene.Address);

        RegistrationEntry entry;

        await _mutationLock.WaitAsync();

        try
        {
            if (Volatile.Read(ref _stopped) != 0)
            {
                return;
            }

            // 已经存在时直接返回，使该方法保持幂等。
            if (_registrationsByAddress.ContainsKey(scene.Address))
            {
                return;
            }

            entry = scene.SceneRuntimeType switch
            {
                SceneRuntimeType.Root => CreateRootEntry(scene),
                SceneRuntimeType.SubScene when scene is SubScene subScene => CreateSubSceneEntry(subScene),
                _ => throw new InvalidOperationException(
                    $"Scene runtime type {scene.SceneRuntimeType} " +
                    "cannot be registered with Control Center.")
            };

            _registrationsByAddress.Add(
                entry.Address,
                entry);

            if (!_registrationsById.TryAdd(
                    entry.InstanceId,
                    entry))
            {
                _registrationsByAddress.Remove(entry.Address);

                throw new InvalidOperationException(
                    $"Duplicate service instance ID: {entry.InstanceId}");
            }

            // 不立即构建新数组和 JSON。
            // 多次动态变化会在下一次心跳前合并成一次重建。
            Volatile.Write(ref _snapshotDirty, 1);
        }
        finally
        {
            _mutationLock.Release();
        }

        // Worker 尚未启动时只记录本地状态，
        // Start 后首次注册会统一处理。
        if (Volatile.Read(ref _started) == 0)
        {
            return;
        }

        await TryRegisterEntryAsync(entry);
    }

    /// <summary>
    /// 将 Scene 从心跳集合移除，并通知 Control Center 下线。
    /// Root Scene 下线时会同时移除其所有 SubScene。
    /// </summary>
    internal async Task UnregisterSceneAsync(Scene scene, bool throwOnFailure = false)
    {
        ArgumentNullException.ThrowIfNull(scene);

        var address = scene.Address;
        if (address == 0)
        {
            return;
        }

        List<RegistrationEntry>? removedEntries = null;

        await _mutationLock.WaitAsync();

        try
        {
            if (Volatile.Read(ref _stopped) != 0 ||
                !_registrationsByAddress.TryGetValue(
                    address,
                    out var entry))
            {
                return;
            }

            removedEntries = new List<RegistrationEntry>();
            removedEntries.Add(entry);

            if (!entry.IsSubScene)
            {
                // ponytail: Root Scene 下线是低频操作，这里 O(n) 扫描子节点；
                // 只有实测 Root Scene 高频销毁时才增加父子索引。
                foreach (var pair in _registrationsByAddress)
                {
                    var child = pair.Value;

                    if (child.IsSubScene &&
                        child.ParentAddress == address)
                    {
                        removedEntries.Add(child);
                    }
                }

                for (var i = 1; i < removedEntries.Count; i++)
                {
                    RemoveEntry(removedEntries[i]);
                }
            }

            RemoveEntry(entry);

            // 必须先从下一次心跳中移除，再发送下线请求。
            Volatile.Write(ref _snapshotDirty, 1);
        }
        finally
        {
            _mutationLock.Release();
        }

        if (Volatile.Read(ref _started) == 0 ||
            removedEntries == null)
        {
            return;
        }

        for (var i = 0; i < removedEntries.Count; i++)
        {
            if (throwOnFailure)
            {
                await _client.SetInstanceOfflineAsync(removedEntries[i].InstanceId);
            }
            else
            {
                await TrySetOfflineAsync(removedEntries[i].InstanceId);
            }
        }
    }

    /// <summary>
    /// 停止心跳并将当前全部实例标记为下线。
    /// </summary>
    internal async Task StopAsync()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        var started = Volatile.Read(ref _started) != 0;
        var runTask = _runTask;

        if (runTask != null)
        {
            await _stopTokenSource.CancelAsync();
            await runTask;
        }

        RegistrationEntry[] entries;

        await _mutationLock.WaitAsync();

        try
        {
            entries =
                new RegistrationEntry[
                    _registrationsByAddress.Count];

            var index = 0;

            foreach (var pair in _registrationsByAddress)
            {
                entries[index++] = pair.Value;
            }

            _registrationsByAddress.Clear();
            _registrationsById.Clear();

            Volatile.Write(
                ref _snapshot,
                RegistrationSnapshot.Empty);

            Volatile.Write(ref _snapshotDirty, 0);
        }
        finally
        {
            _mutationLock.Release();
        }

        if (started)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                await TrySetOfflineAsync(entries[i].InstanceId);
            }
        }

        _stopTokenSource.Dispose();
    }

    /// <summary>
    /// 首次注册并持续执行周期批量心跳。
    /// </summary>
    private async Task RunAsync(
        CancellationToken cancellationToken)
    {
        var initialRegistrationCompleted = false;
        var failureLogged = false;

        using var timer =
            new PeriodicTimer(_heartbeatInterval);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!initialRegistrationCompleted)
                    {
                        var initialSnapshot =
                            await GetHeartbeatSnapshotAsync();

                        await RegisterAllAsync(initialSnapshot);

                        initialRegistrationCompleted = true;

                        Log.Info(
                            $"Registered {initialSnapshot.Entries.Length} " +
                            "service instances with Control Center.");
                    }

                    var snapshot =
                        await GetHeartbeatSnapshotAsync();

                    await SendHeartbeatsAsync(snapshot);

                    if (failureLogged)
                    {
                        Log.Info(
                            "Service discovery connection recovered.");
                    }

                    failureLogged = false;
                }
                catch (Exception exception)
                {
                    if (!failureLogged)
                    {
                        Log.Warning(
                            "Service discovery request failed. " +
                            "It will retry automatically. " +
                            $"Error: {exception}");
                    }

                    failureLogged = true;
                }

                if (!await timer.WaitForNextTickAsync(
                        cancellationToken))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // 正常关闭。
        }
    }

    /// <summary>
    /// 获取心跳不可变快照。
    /// 注册集合没有变化时不加锁、不分配。
    /// </summary>
    private async Task<RegistrationSnapshot>
        GetHeartbeatSnapshotAsync()
    {
        if (Volatile.Read(ref _snapshotDirty) == 0)
        {
            return Volatile.Read(ref _snapshot);
        }

        await _mutationLock.WaitAsync();

        try
        {
            if (Volatile.Read(ref _snapshotDirty) == 0)
            {
                return Volatile.Read(ref _snapshot);
            }

            var snapshot = BuildSnapshot();

            Volatile.Write(ref _snapshot, snapshot);
            Volatile.Write(ref _snapshotDirty, 0);

            return snapshot;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    /// <summary>
    /// 注册快照中的全部实例。
    /// 快照始终保证 Root Scene 排在 SubScene 前面。
    /// </summary>
    private async Task RegisterAllAsync( RegistrationSnapshot snapshot)
    {
        for (var i = 0; i < snapshot.Entries.Length; i++)
        {
            await RegisterCurrentEntryAsync(snapshot.Entries[i]);
        }
    }

    /// <summary>
    /// 发送预序列化心跳，并重新注册被拒绝且当前仍有效的实例。
    /// </summary>
    private async Task SendHeartbeatsAsync( RegistrationSnapshot snapshot)
    {
        for (var batchIndex = 0; batchIndex < snapshot.HeartbeatPayloads.Length; batchIndex++)
        {
            var response = await _client.BatchHeartbeatAsync(snapshot.HeartbeatPayloads[batchIndex]);

            var rejectedInstanceIds =
                response.RejectedInstanceIds ??
                Array.Empty<string>();

            for (var rejectedIndex = 0; rejectedIndex < rejectedInstanceIds.Length; rejectedIndex++)
            {
                var instanceId = rejectedInstanceIds[rejectedIndex];

                if (string.IsNullOrWhiteSpace(instanceId) ||
                    !_registrationsById.TryGetValue(instanceId, out var entry))
                {
                    continue;
                }

                await RegisterCurrentEntryAsync(entry);
            }
        }
    }

    /// <summary>
    /// 根据注册类型调用对应的 Control Center 接口。
    /// </summary>
    private async Task RegisterEntryAsync(
        RegistrationEntry entry)
    {
        if (entry.RootRequest != null)
        {
            await _client.RegisterInstanceAsync(
                entry.RootRequest);

            return;
        }

        await _client.RegisterSubSceneAsync(
            entry.SubSceneRequest!);
    }
    
    /// <summary>
    /// 仅注册当前仍然有效的注册项。
    /// 注册期间实例被移除时，注册完成后立即补发下线。
    /// </summary>
    private async Task RegisterCurrentEntryAsync( RegistrationEntry entry)
    {
        if (!_registrationsById.TryGetValue( entry.InstanceId, out var current) ||
            !ReferenceEquals(current, entry))
        {
            return;
        }

        await RegisterEntryAsync(entry);

        // 注册请求执行期间 Scene 可能已经销毁。
        // 此时必须补发下线，避免出现“先下线、后注册”。
        if (!_registrationsById.ContainsKey(entry.InstanceId))
        {
            await TrySetOfflineAsync(entry.InstanceId);
        }
    }

    /// <summary>
    /// 动态注册失败时保留本地状态，由后续心跳自动重试。
    /// </summary>
    private async Task TryRegisterEntryAsync(RegistrationEntry entry)
    {
        try
        {
            await RegisterCurrentEntryAsync(entry);
        }
        catch (Exception exception)
        {
            Log.Warning(
                $"Unable to register service instance " +
                $"{entry.InstanceId}. It will retry automatically. " +
                $"Error: {exception}");

            // 请求可能已经到达 Control Center，
            // 但客户端在读取响应时发生异常。
            if (!_registrationsById.ContainsKey(entry.InstanceId))
            {
                await TrySetOfflineAsync(entry.InstanceId);
            }
        }
    }

    /// <summary>
    /// 下线请求失败时由 Control Center 租约兜底。
    /// </summary>
    private async Task TrySetOfflineAsync(string instanceId)
    {
        try
        {
            await _client.SetInstanceOfflineAsync(instanceId);
        }
        catch (Exception exception)
        {
            Log.Warning(
                $"Unable to mark service instance {instanceId} offline. " +
                "Its lease will expire automatically. " +
                $"Error: {exception}");
        }
    }

    /// <summary>
    /// 从两个当前索引中移除注册项。
    /// 必须在 mutationLock 中调用。
    /// </summary>
    private void RemoveEntry(RegistrationEntry entry)
    {
        _registrationsByAddress.Remove(entry.Address);
        _registrationsById.TryRemove(entry.InstanceId, out _);
    }

    /// <summary>
    /// 为运行期间新创建的 Root Scene 构建注册项。
    /// </summary>
    private RegistrationEntry CreateRootEntry(Scene scene)
    {
        var process = scene.Process ??
                      throw new InvalidOperationException("Root Scene has no Process.");

        var sceneConfig = scene.SceneConfig;
        var machine = MachineConfigData.Instance.Get(process.MachineId);

        return new RegistrationEntry(
            new RegisterInstanceRequest
            {
                InstanceId =
                    $"{process.MachineId}:" +
                    $"{process.Id}:" +
                    $"{scene.SceneConfigId}:" +
                    $"{_startupId}:" +
                    Guid.NewGuid().ToString("N"),

                SceneId = scene.SceneConfigId,
                Address = scene.Address,
                SceneType = sceneConfig.SceneTypeString,
                WorldId = sceneConfig.WorldConfigId,
                ProcessId = process.Id,
                Host = machine.InnerBindIP,
                InnerPort = sceneConfig.InnerPort,
                OuterPort = sceneConfig.OuterPort,
                Version = ProgramDefine.VERSION,
                LeaseSeconds = _leaseSeconds
            });
    }

    /// <summary>
    /// 为动态 SubScene 构建注册项。
    /// </summary>
    private RegistrationEntry CreateSubSceneEntry( SubScene scene)
    {
        var parentAddress = scene.RootScene.Address;

        if (!_registrationsByAddress.TryGetValue(parentAddress, out var parent) || parent.IsSubScene)
        {
            throw new InvalidOperationException(
                $"The parent Root Scene {parentAddress} " +
                "is not registered with service discovery.");
        }

        if (!_sceneTypeNames.TryGetValue(scene.SceneType, out var sceneTypeName))
        {
            throw new InvalidOperationException($"SceneType id '{scene.SceneType}' is not declared.");
        }

        var request = new RegisterSubSceneRequest
        {
            InstanceId = $"{parent.InstanceId}:sub:{scene.Address}",

            ParentInstanceId = parent.InstanceId,
            Address = scene.Address,
            SceneType = sceneTypeName,
            LeaseSeconds = _leaseSeconds
        };

        return new RegistrationEntry(request, parentAddress);
    }

    /// <summary>
    /// 收集服务器启动阶段已经创建的 Root Scene 注册项。
    /// </summary>
    private static RegistrationEntry[] BuildInitialRootEntries(
        IReadOnlyList<Process> processes,
        int leaseSeconds,
        string startupId)
    {
        var result = new List<RegistrationEntry>();

        for (var processIndex = 0;
             processIndex < processes.Count;
             processIndex++)
        {
            var process = processes[processIndex];

            if (process == null)
            {
                throw new InvalidOperationException(
                    $"Process at index {processIndex} cannot be null.");
            }

            var machine =
                MachineConfigData.Instance.Get(process.MachineId);

            var scenes =
                SceneConfigData.Instance.GetByProcess(process.Id);

            for (var sceneIndex = 0;
                 sceneIndex < scenes.Count;
                 sceneIndex++)
            {
                var scene = scenes[sceneIndex];

                result.Add(
                    new RegistrationEntry(
                        new RegisterInstanceRequest
                        {
                            InstanceId =
                                $"{process.MachineId}:" +
                                $"{process.Id}:" +
                                $"{scene.Id}:" +
                                startupId,

                            SceneId = scene.Id,
                            Address = scene.Address,
                            SceneType = scene.SceneTypeString,
                            WorldId = scene.WorldConfigId,
                            ProcessId = process.Id,
                            Host = machine.InnerBindIP,
                            InnerPort = scene.InnerPort,
                            OuterPort = scene.OuterPort,
                            Version = ProgramDefine.VERSION,
                            LeaseSeconds = leaseSeconds
                        }));
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 从当前注册集合构建一次不可变心跳快照。
    /// 仅在集合变化后执行。
    /// </summary>
    private RegistrationSnapshot BuildSnapshot()
    {
        var entries =
            new RegistrationEntry[
                _registrationsByAddress.Count];

        var index = 0;

        // 父 Root Scene 必须排在 SubScene 前面，
        // 保证首次注册和心跳拒绝后的重新注册顺序正确。
        foreach (var pair in _registrationsByAddress)
        {
            if (!pair.Value.IsSubScene)
            {
                entries[index++] = pair.Value;
            }
        }

        foreach (var pair in _registrationsByAddress)
        {
            if (pair.Value.IsSubScene)
            {
                entries[index++] = pair.Value;
            }
        }

        return new RegistrationSnapshot(
            entries,
            BuildHeartbeatPayloads(entries, _leaseSeconds));
    }

    /// <summary>
    /// 按协议上限切分实例 ID，并预序列化批量心跳 JSON。
    /// </summary>
    private static byte[][] BuildHeartbeatPayloads(
        RegistrationEntry[] entries,
        int leaseSeconds)
    {
        if (entries.Length == 0)
        {
            return Array.Empty<byte[]>();
        }

        var maximumBatchSize =
            ServiceDiscoveryProtocol.MaxBatchHeartbeatCount;

        var batchCount =
            (entries.Length - 1) / maximumBatchSize + 1;

        var result = new byte[batchCount][];
        var entryIndex = 0;

        for (var batchIndex = 0;
             batchIndex < batchCount;
             batchIndex++)
        {
            var remaining =
                entries.Length - entryIndex;

            var currentBatchSize =
                Math.Min(maximumBatchSize, remaining);

            var instanceIds =
                new string[currentBatchSize];

            for (var index = 0;
                 index < currentBatchSize;
                 index++)
            {
                instanceIds[index] =
                    entries[entryIndex++].InstanceId;
            }

            var request = new BatchHeartbeatRequest
            {
                InstanceIds = instanceIds,
                LeaseSeconds = leaseSeconds
            };

            result[batchIndex] =
                Encoding.UTF8.GetBytes(request.ToJson());
        }

        return result;
    }

    /// <summary>
    /// 一个 Root Scene 或 SubScene 的稳定注册信息。
    /// </summary>
    private sealed class RegistrationEntry
    {
        internal RegistrationEntry(
            RegisterInstanceRequest request)
        {
            RootRequest = request;
            Address = request.Address;
        }

        internal RegistrationEntry(
            RegisterSubSceneRequest request,
            long parentAddress)
        {
            SubSceneRequest = request;
            Address = request.Address;
            ParentAddress = parentAddress;
        }

        internal RegisterInstanceRequest? RootRequest { get; }

        internal RegisterSubSceneRequest? SubSceneRequest { get; }

        internal string InstanceId =>
            RootRequest?.InstanceId ??
            SubSceneRequest!.InstanceId;

        internal long Address { get; }

        internal long ParentAddress { get; }

        internal bool IsSubScene =>
            SubSceneRequest != null;
    }

    /// <summary>
    /// 心跳线程读取的不可变快照。
    /// </summary>
    private sealed class RegistrationSnapshot
    {
        internal static readonly RegistrationSnapshot Empty =
            new(
                Array.Empty<RegistrationEntry>(),
                Array.Empty<byte[]>());

        internal RegistrationSnapshot(
            RegistrationEntry[] entries,
            byte[][] heartbeatPayloads)
        {
            Entries = entries;
            HeartbeatPayloads = heartbeatPayloads;
        }

        internal RegistrationEntry[] Entries { get; }

        internal byte[][] HeartbeatPayloads { get; }
    }
}
#endif
