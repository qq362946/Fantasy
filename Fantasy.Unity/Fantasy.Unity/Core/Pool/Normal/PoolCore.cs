// using System;
// using System.Collections.Generic;
//
// namespace Fantasy
// {
//     /// <summary>
//     /// 泛型对象池核心类，用于创建和管理可重复使用的对象实例。
//     /// </summary>
//     /// <typeparam name="T">要池化的对象类型。</typeparam>
//     public abstract class PoolCore<T>
//     {
//         private readonly Action<T> _reset;
//         private readonly Func<T> _generator;
//         private readonly Stack<T> _objects = new Stack<T>();
//
//         /// <summary>
//         /// 获取池中当前可用的对象数量。
//         /// </summary>
//         public int Count => _objects.Count;
//
//         /// <summary>
//         /// 初始化一个对象池
//         /// </summary>
//         /// <param name="generator">生成对象实例的方法。某些类型的构造函数中可能需要额外的参数，所以使用Func生成器</param>
//         /// <param name="reset">重置对象实例的方法，可用于清理对象状态。某些类型可能需要对返回的对象进行额外清理</param>
//         /// <param name="initialCapacity">初始池容量，可预分配对象实例。</param>
//         protected PoolCore(Func<T> generator, Action<T> reset, int initialCapacity = 0)
//         {
//             _generator = generator;
//             _reset = reset;
//
//             for (var i = 0; i < initialCapacity; ++i)
//             {
//                 _objects.Push(generator());
//             }
//         }
//
//         /// <summary>
//         /// 从对象池中获取一个对象实例。
//         /// </summary>
//         /// <returns>可重复使用的对象实例。</returns>
//         public T Rent()
//         {
//             return _objects.Count > 0 ? _objects.Pop() : _generator();
//         }
//
//         /// <summary>
//         /// 将对象实例返回到对象池中，以便重复使用。
//         /// </summary>
//         /// <param name="item">要返回的对象实例。</param>
//         public void Return(T item)
//         {
//             _reset(item);
//             _objects.Push(item);
//         }
//
//         /// <summary>
//         /// 清空对象池。
//         /// </summary>
//         public void Clear()
//         {
//             _objects.Clear();
//         }
//     }
// }