using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using MemoryPack;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

//跨程序集泛型实体测试
namespace Fantasy.GenericTest
{
    //单一泛型参数实体
    [MemoryPackable]
    public sealed partial class TestEntity<T> : Entity where T : Entity
    {
        public T Entity;
        public Type type = typeof(TestEntity<Unit>);
    }

    public sealed class TestGenericEntityAwakeSystem<T> : AwakeSystem<TestEntity<T>> where T : Entity
    {
        protected override void Awake(TestEntity<T> self)
        {
            Log.Debug($"TestGenericEntityAwakeSystem : {typeof(T).FullName}");
        }
    }

    //双泛型参数实体
    [MemoryPackable]
    public sealed partial class TestEntity2<T,S> : Entity where T : Entity
    {
        public T Entity;
        public Type type = typeof(TestEntity<Unit>);
    }

    public sealed class TestGenericEntity2AwakeSystem<T, S> : AwakeSystem<TestEntity2<T, S>> where T : Entity
    {
        protected override void Awake(TestEntity2<T, S> self)
        {
            Log.Debug($"TestGenericEntity2AwakeSystem : {typeof(T).FullName}、{typeof(S).FullName}");
        }
    }
    
    // 测试用例：使用闭合泛型类型
    public class GenericTestComponent : Entity
    {
        // 这些字段会被我们的 closedGenericTypesProvider 收集
        public TestEntity<Scene> SceneEntity;
        public TestEntity2<Scene, Scene> DoubleSceneEntity;
    }
}

