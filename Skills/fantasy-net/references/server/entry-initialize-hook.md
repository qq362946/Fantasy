# Entry Initialization Hook

## Overview

`IEntryInitializeHook` allows you to inject custom startup logic during `Entry.Initialize()`, running after configuration loading but before serializer initialization.

## When to Use

Use this hook for:
- **Startup validation** - Check database connectivity, validate configuration values
- **Pre-initialization setup** - Register additional services, load external resources
- **Environment-specific logic** - Different initialization based on `ProgramDefine.ProcessType`
- **Integration checks** - Verify third-party service availability before server starts

**Do NOT use for:**
- Scene-specific initialization (use `OnCreateScene` event instead)
- Entity/Component setup (use AwakeSystem instead)
- Network message handling (use Message Handlers instead)

## Timing in Startup Flow

```
Entry.Start()
  └─ Entry.Initialize()
      ├─ 1. Parse command line arguments
      ├─ 2. Log.Initialize()
      ├─ 3. typeof(Entry).Assembly.EnsureLoaded()  // Trigger ModuleInitializer
      ├─ 4. ConfigLoader.InitializeFromXml()       // Load Fantasy.config
      ├─ 5. ★ IEntryInitializeHook.OnInitialize() ★  // Your custom logic here
      ├─ 6. SerializerManager.Initialize()
      └─ 7. WinPeriod.Initialize()
  └─ StartProcess()
      └─ Process.Create() → Scene.Create() → OnCreateScene event
```

**Available at hook execution:**
- ✅ Command line arguments (`ProgramDefine.ProcessType`, `ProcessId`, `RuntimeMode`)
- ✅ Logging system (`Log.Debug/Info/Error`)
- ✅ Configuration data (`ConfigLoader`, `ProcessConfigData`, `SceneConfigData`)
- ✅ All assemblies loaded (`AssemblyManifest`)

**NOT available yet:**
- ❌ Scenes (created later in `StartProcess()`)
- ❌ Serialization system
- ❌ Network connections

## Implementation

### Step 1: Define Hook Implementation

Create your hook class in the **Hotfix** layer (supports hot reload):

```csharp
// File: Examples/Server/APP/Hotfix/GameStartupHook.cs
namespace Fantasy;

public class GameStartupHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // Execute only for Game process type
        if (ProgramDefine.ProcessType == "Game")
        {
            Log.Info("Executing pre-startup checks...");
            
            await ValidateDatabaseConnection();
            await CheckExternalServices();
            await LoadAdditionalConfig();
        }
        
        await Task.CompletedTask;
    }
    
    private async Task ValidateDatabaseConnection()
    {
        // Check database connectivity before server starts
        Log.Info("Validating database connection...");
        // Your validation logic
        await Task.CompletedTask;
    }
    
    private async Task CheckExternalServices()
    {
        // Verify external service availability
        Log.Info("Checking external services...");
        // Your check logic
        await Task.CompletedTask;
    }
    
    private async Task LoadAdditionalConfig()
    {
        // Load additional configuration files
        Log.Info("Loading additional configuration...");
        // Your config loading logic
        await Task.CompletedTask;
    }
}
```

### Step 2: That's It!

The source generator automatically discovers and registers all `IEntryInitializeHook` implementations. No manual registration needed.

## Multiple Hooks

You can define multiple hook implementations - all will execute sequentially:

```csharp
// Hook 1: Database validation
public class DatabaseValidationHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        Log.Info("Validating database schema...");
        // Database checks
        await Task.CompletedTask;
    }
}

// Hook 2: External service check
public class ExternalServiceHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        Log.Info("Checking external APIs...");
        // API checks
        await Task.CompletedTask;
    }
}

// Hook 3: License validation
public class LicenseValidationHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        Log.Info("Validating license...");
        // License checks
        await Task.CompletedTask;
    }
}
```

Execution order is not guaranteed - design hooks to be independent of each other.

## Error Handling

If any hook throws an exception, Entry initialization fails and the server stops:

```csharp
public class ValidationHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        try
        {
            await ValidateCriticalConfig();
        }
        catch (Exception e)
        {
            Log.Error($"Critical validation failed: {e}");
            throw; // Server will NOT start
        }
    }
}
```

The framework logs hook failures:
```
EntryInitializeHook GameStartupHook failed: System.Exception: Database unavailable
```

## Common Patterns

### Pattern 1: Environment-Specific Logic

```csharp
public class EnvironmentSetupHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        switch (ProgramDefine.RuntimeMode)
        {
            case ProcessMode.Develop:
                Log.Info("Development mode - enabling debug features");
                // Enable dev-only features
                break;
                
            case ProcessMode.Release:
                Log.Info("Production mode - strict validation");
                await ValidateProductionRequirements();
                break;
        }
        
        await Task.CompletedTask;
    }
}
```

### Pattern 2: Process Type Filtering

```csharp
public class GateServerHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // Only execute for Gate servers
        if (ProgramDefine.ProcessType != "Gate")
        {
            return;
        }
        
        Log.Info("Gate server initialization...");
        await InitializeGateSpecificServices();
    }
}
```

### Pattern 3: Configuration Validation

```csharp
public class ConfigValidationHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        // Validate loaded configuration
        var processConfig = ProcessConfigData.Instance.Get(ProgramDefine.ProcessId);
        
        if (processConfig == null)
        {
            throw new InvalidOperationException(
                $"Process {ProgramDefine.ProcessId} not found in Fantasy.config");
        }
        
        var sceneConfigs = SceneConfigData.Instance.GetByProcess(ProgramDefine.ProcessId);
        
        if (sceneConfigs.Count == 0)
        {
            Log.Warning($"Process {ProgramDefine.ProcessId} has no scenes configured");
        }
        
        Log.Info($"Configuration validated: {sceneConfigs.Count} scenes configured");
        await Task.CompletedTask;
    }
}
```

## Comparison with Other Extension Points

| Extension Point | Timing | Use Case |
|----------------|--------|----------|
| **IEntryInitializeHook** | Before serializer init, no Scene exists | Config validation, pre-startup checks, global setup |
| **OnCreateScene** | Each Scene creation | Scene-specific initialization, add components |
| **IAwakeSystem** | Entity/Component creation | Entity lifecycle logic |
| **IAssemblyLifecycle** | Assembly load/unload | Hot reload support, dynamic registration |

## Troubleshooting

### Hook Not Executing

**Symptom:** Hook implementation exists but `OnInitialize()` never called

**Causes:**
1. Hook not implementing `IEntryInitializeHook` correctly
2. Source generator not running (rebuild project)
3. Hook in wrong assembly (must be in Hotfix or other loaded assembly)

**Solution:**
```bash
# Rebuild to trigger source generator
dotnet clean
dotnet build

# Verify generated registration code exists
# Check: obj/Debug/net8.0/generated/Fantasy.SourceGenerator/.../CustomInterfaceRegistrar.g.cs
```

### Server Fails to Start

**Symptom:** Server crashes during initialization

**Cause:** Hook threw unhandled exception

**Solution:** Check logs for hook failure message, add proper error handling

### Multiple Hooks Conflict

**Symptom:** Hooks interfere with each other

**Solution:** Design hooks to be independent - avoid shared state or execution order dependencies

## Best Practices

1. **Keep hooks focused** - One responsibility per hook
2. **Fail fast** - Throw exceptions for critical failures
3. **Log clearly** - Use descriptive log messages
4. **Be async-ready** - Use `await` properly, avoid blocking calls
5. **Test in isolation** - Each hook should work independently
6. **Document dependencies** - If hook requires external services, document it

## Related

- `references/ecs/scene.md` - OnCreateScene event for Scene initialization
- `references/ecs/lifecycle.md` - Entity lifecycle systems
- `references/logging.md` - Logging system usage
- `references/config.md` - Fantasy.config structure
