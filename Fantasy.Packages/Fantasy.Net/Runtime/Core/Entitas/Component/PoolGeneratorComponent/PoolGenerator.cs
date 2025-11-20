using System;
using System.Collections.Generic;
using Fantasy.Assembly;
using Fantasy.DataStructure.Dictionary;
using Fantasy.Pool;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy.Entitas
{
    internal sealed class PoolGenerator
    {
        private int _generatorCount;
        private readonly Dictionary<long, GeneratorInfo> _generatorInfos = new();
        private RuntimeTypeHandleFrozenDictionary<Func<IPool>> _cachedDictionary;
        public RuntimeTypeHandleFrozenDictionary<Func<IPool>> GetFrozenDictionary() => _cachedDictionary;

        public void Add(AssemblyManifest assemblyManifest)
        {
            var generator = assemblyManifest.PoolCreatorGenerator;
            Add(assemblyManifest.AssemblyManifestId, generator.RuntimeTypeHandles(), generator.Generators());
        }
        
        private void Add(long assemblyManifestId, RuntimeTypeHandle[] runtimeTypeHandles, Func<IPool>[] generator)
        {
            if (runtimeTypeHandles.Length != generator.Length)
            {
                throw new ArgumentException("runtimeTypeHandles and generator must have the same length");
            }
            
            if (runtimeTypeHandles.Length == 0)
            {
                return;
            }

            Remove(assemblyManifestId, false);

            var length = runtimeTypeHandles.Length;
            _generatorInfos.Add(assemblyManifestId, new GeneratorInfo(length, runtimeTypeHandles, generator));
            _generatorCount += length;
            
            RebuildDictionary();
        }

        public void Remove(long assemblyManifestId, bool rebuildDictionary = true)
        {
            if (!_generatorInfos.Remove(assemblyManifestId, out var generatorInfo))
            {
                return;
            }

            _generatorCount = Math.Max(0, _generatorCount - generatorInfo.Count);

            if (rebuildDictionary)
            {
                RebuildDictionary();
            }
        }

        private void RebuildDictionary()
        {
            if (_generatorCount == 0)
            {
                _cachedDictionary = new RuntimeTypeHandleFrozenDictionary<Func<IPool>>(Array.Empty<RuntimeTypeHandle>(), Array.Empty<Func<IPool>>());
                return;
            }

            var index = 0;
            var runtimeTypeHandles = new RuntimeTypeHandle[_generatorCount];
            var generators = new Func<IPool>[_generatorCount];

            foreach (var generatorInfo in _generatorInfos.Values)
            {
                Array.Copy(generatorInfo.RuntimeTypeHandles, 0, runtimeTypeHandles, index, generatorInfo.Count);
                Array.Copy(generatorInfo.Generators, 0, generators, index, generatorInfo.Count);
                index += generatorInfo.Count;
            }
            
            _cachedDictionary = new RuntimeTypeHandleFrozenDictionary<Func<IPool>>(runtimeTypeHandles, generators);
        }

        private sealed class GeneratorInfo
        {
            public readonly int Count;
            public readonly RuntimeTypeHandle[] RuntimeTypeHandles;
            public readonly Func<IPool>[] Generators;

            public GeneratorInfo(int count, RuntimeTypeHandle[] runtimeTypeHandles, Func<IPool>[] generators)
            {
                Count = count;
                RuntimeTypeHandles = runtimeTypeHandles;
                Generators = generators;
            }
        }
    }
}