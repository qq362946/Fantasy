using System;
using System.Reflection.Emit;
using Fantasy.Serialize;

#pragma warning disable CS8604 // Possible null reference argument.

namespace Fantasy.Pool
{
    internal static class CreateInstance<T> where T : IPool
    {
        public static Func<T> Create { get; }
        
        static CreateInstance()
        {
            var type = typeof(T);
            var dynamicMethod = new DynamicMethod($"CreateInstance_{type.Name}", type, Type.EmptyTypes, true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ret);
            Create = (Func<T>) dynamicMethod.CreateDelegate(typeof(Func<T>));
        }
    }

    internal static class CreateInstance
    {
        public static Func<IPool> CreateIPool(Type type)
        {
            var dynamicMethod = new DynamicMethod($"CreateInstance_{type.Name}", type, Type.EmptyTypes, true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ret);
            return (Func<IPool>)dynamicMethod.CreateDelegate(typeof(Func<IPool>));
        }
        
        public static Func<object> CreateObject(Type type)
        {
            var dynamicMethod = new DynamicMethod($"CreateInstance_{type.Name}", type, Type.EmptyTypes, true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ret);
            return (Func<object>)dynamicMethod.CreateDelegate(typeof(Func<object>));
        }
        
        public static Func<AMessage> CreateMessage(Type type)
        {
            var dynamicMethod = new DynamicMethod($"CreateInstance_{type.Name}", type, Type.EmptyTypes, true);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ret);
            return (Func<AMessage>)dynamicMethod.CreateDelegate(typeof(Func<AMessage>));
        }
    }
    
    // public static class CreateInstance
    // {
    //     public static Func<IPool> Create(Type type)
    //     {
    //         var dynamicMethod = new DynamicMethod($"CreateInstance_{type.Name}", type, Type.EmptyTypes, true);
    //         var il = dynamicMethod.GetILGenerator();
    //         il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
    //         il.Emit(OpCodes.Ret);
    //         return (Func<IPool>)dynamicMethod.CreateDelegate(typeof(Func<IPool>));
    //     }
    // }
    
    // /// <summary>
    // /// 利用泛型的特性来减少反射的使用。
    // /// </summary>
    // /// <typeparam name="T"></typeparam>
    // public static class PoolChecker<T> where T : new()
    // {
    //     public static bool IsPool { get; }
    //
    //     static PoolChecker()
    //     {
    //         IsPool = typeof(IPool).IsAssignableFrom(typeof(T));
    //     }
    // }
}