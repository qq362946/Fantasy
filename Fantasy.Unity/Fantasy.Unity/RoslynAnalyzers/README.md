# Fantasy.SourceGenerator for Unity

This directory contains the Fantasy.SourceGenerator Roslyn Analyzer DLL that enables automatic code generation for Fantasy.Unity.

## What does it generate?

The Source Generator automatically generates the following code:

1. **EntitySystemRegistrar** - Registers entity systems (Awake, Update, Destroy, etc.)
2. **EventSystemRegistrar** - Registers event handlers
3. **MessageDispatcherRegistrar** - Registers network message handlers
4. **EntityTypeCollectionRegistrar** - Registers entity type collections
5. **ProtoBufRegistrar** - Registers ProtoBuf serialization types

## How to use

### Method 1: csc.rsp (Unity 2020.2+)

The `csc.rsp` file in the package root already references this analyzer:
```
-analyzer:"SourceGenerators/Fantasy.SourceGenerator.dll"
```

### Method 2: asmdef RoslynAnalyzerDllPaths (Unity 2021.2+)

The `Fantasy.Unity.asmdef` file includes:
```json
"RoslynAnalyzerDllPaths": [
    "SourceGenerators/Fantasy.SourceGenerator.dll"
]
```

## Troubleshooting

### Generator not working?

1. **Check Unity version**: Make sure you're using Unity 2020.2 or later
2. **Reimport the package**: Right-click on the package folder → Reimport
3. **Restart Unity**: Close and reopen Unity to force recompilation
4. **Check generated files**: Look in `Temp/GeneratedCode/` for generated files

### View generated code

Generated code is placed in Unity's temporary directory:
```
<ProjectRoot>/Temp/GeneratedCode/Fantasy.SourceGenerator/
```

You can also enable viewing generated files in your IDE:
- Visual Studio: Tools → Options → Text Editor → C# → Advanced → Enable "Show Roslyn analyzer output"
- Rider: Settings → Build, Execution, Deployment → Roslyn → Enable "Show compiler output"

## Updating the Generator

To update the generator DLL:

1. Build the latest Fantasy.SourceGenerator:
   ```bash
   dotnet build Fantasy.Net/Fantasy.SourceGenerator/Fantasy.SourceGenerator.csproj --configuration Release
   ```

2. Copy the DLL to this directory:
   ```bash
   cp Fantasy.Net/Fantasy.SourceGenerator/bin/Release/netstandard2.0/Fantasy.SourceGenerator.dll \
      Fantasy.Unity/Fantasy.Unity/SourceGenerators/
   ```

3. Reimport the package in Unity

## Notes

- The DLL is only used during compilation and is not included in builds
- The `.meta` file marks this as a RoslynAnalyzer plugin
- The generator is automatically invoked during C# compilation
