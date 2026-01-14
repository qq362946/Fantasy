# AGENTS.md

Agent instructions for Fantasy Framework - a high-performance C# distributed game server framework.

## Build Commands

```bash
# Main solution (includes all packages)
dotnet build Fanatsy.sln
dotnet build Fanatsy.sln --configuration Release

# Server application
dotnet build Examples/Server/Server.sln
dotnet run --project Examples/Server/APP/Main/Main.csproj

# Console application
dotnet build Examples/Console/Fantasy.Console.sln
dotnet run --project Examples/Console/Fantasy.Console.Main/Fantasy.Console.Main.csproj

# Core framework only
dotnet build Fantasy.Packages/Fantasy.Net/Fantasy.Net.csproj

# Benchmarks (start server first, then run benchmarks)
dotnet run --project Fantasy.Benchmark/Fantasy.Benchmark.csproj --configuration Release
```

## Testing

This project uses **BenchmarkDotNet** for performance testing. No unit test framework is present.

```bash
# Run benchmarks (requires server running)
# 1. Start server: dotnet run --project Examples/Server/APP/Main/Main.csproj
# 2. Run benchmarks: dotnet run --project Fantasy.Benchmark/Fantasy.Benchmark.csproj -c Release
```

## Code Style Guidelines

### Project Configuration
- **Target Frameworks**: net8.0, net9.0
- **Language Version**: default (C# 12+)
- **Nullable**: enabled
- **Implicit Usings**: enabled
- **Unsafe Code**: allowed (`AllowUnsafeBlocks=true`)
- **Warnings as Errors**: enabled in Debug (`TreatWarningsAsErrors=true`)

### Naming Conventions
- **Classes/Structs**: PascalCase (`Scene`, `AccountHelper`, `C2G_LoginGameRequest`)
- **Interfaces**: `I` prefix (`IMessage`, `IRequest`, `IResponse`)
- **Methods**: PascalCase (`Initialize`, `Online`, `Handler`)
- **Private Fields**: `_camelCase` with underscore prefix
- **Public Fields**: PascalCase (e.g., `public string Name;`)
- **Properties**: PascalCase
- **Constants**: PascalCase
- **Message Handlers**: `{MessageName}Handler` pattern

### Imports Organization
```csharp
// System namespaces first
using System;
using System.Collections.Generic;
// Fantasy namespaces
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Network;
// Conditional compilation
#if FANTASY_NET
using Fantasy.Platform.Net;
#endif
```

### File-Scoped Namespaces
Prefer file-scoped namespaces for application code:
```csharp
namespace Fantasy;

public sealed class MyClass { }
```

### Entity Pattern
```csharp
public sealed class Account : Entity
{
    public string Name;
    public EntityReference<Session> Session;
}
```

### Message Handler Pattern
```csharp
public sealed class C2G_TestRequestHandler : MessageRPC<C2G_TestRequest, G2C_TestResponse>
{
    protected override async FTask Run(Session session, C2G_TestRequest request, 
        G2C_TestResponse response, Action reply)
    {
        // Handler logic
        await FTask.CompletedTask;
    }
}
```

### Async Operations
- Use `FTask` instead of `Task` for framework-optimized async
- Use `FCancellationToken` for cancellation
- Return `await FTask.CompletedTask` for sync paths in async methods

### Error Handling
- Return error codes via response objects (e.g., `response.ErrorCode = 1`)
- Use `Log.Error()` for error logging with context
- Never throw exceptions for expected business logic errors

### Logging
```csharp
Log.Debug($"Message: {value}");
Log.Info($"Info message");
Log.Error($"Error with context: {details}");
```

### Null Handling
Suppress nullable warnings where framework guarantees non-null:
```csharp
#pragma warning disable CS8618 // Non-nullable field
#pragma warning disable CS8602 // Dereference of possibly null
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
```

### Protocol Definitions (.proto)
```protobuf
// Comment format for message types
message C2G_TestRequest // IRequest,G2C_TestResponse
{
    string Tag = 1;
}
message G2C_TestResponse // IResponse
{
    string Tag = 1;
}
```

### Conditional Compilation
Use preprocessor directives for platform-specific code:
- `FANTASY_NET` - Server/.NET platform
- `FANTASY_UNITY` - Unity client
- `FANTASY_CONSOLE` - Console applications

## Project Structure

```
Examples/Server/APP/          # Server application code
  Entity/                     # Entities, components, configs
  Hotfix/                     # Hot-reloadable handlers
  Main/                       # Entry point (Program.cs)
Fantasy.Packages/
  Fantasy.Net/                # Core framework
  Fantasy.SourceGenerator/    # Roslyn source generators
  Fantasy.Unity/              # Unity client package
Examples/Config/
  NetworkProtocol/            # .proto message definitions
    Inner/                    # Server-to-server messages
    Outer/                    # Client-to-server messages
```

## Key Framework Patterns

### ECS Architecture
- Entities inherit from `Entity` base class
- Components attached via `AddComponent<T>()`
- Systems implement `AwakeSystem<T>`, `UpdateSystem<T>`, `DestroySystem<T>`

### Network Messages
- `IMessage` - One-way messages
- `IRequest`/`IResponse` - RPC messages
- `IRoamingMessage`/`IRoamingRequest` - Cross-server routing
- `IAddressableMessage` - Entity-addressed messages

### Source Generators
All registration is compile-time generated. No manual registration needed for:
- Entity systems
- Message handlers
- Event handlers
- Network protocols

## Important Notes

1. **No reflection at runtime** - Framework uses source generators for AOT compatibility
2. **Never modify generated `.g.cs` files** - They are overwritten on build
3. **Protocol changes require exporter** - Run protocol export tool after .proto changes
4. **Hot reload support** - Entity/Hotfix assembly separation enables runtime updates
5. **FTask over Task** - Always use `FTask` for framework async operations
