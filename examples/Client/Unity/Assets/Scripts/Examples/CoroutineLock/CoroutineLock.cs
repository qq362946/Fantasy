using System.Collections;
using System.Collections.Generic;
using Fantasy;
using NativeCollections;
using UnityEngine;
using UnityEngine.UI;

public class CoroutineLock : MonoBehaviour
{
    public Button Button1;
    public Button Button2;
    public Button Button3;
    private Scene _scene;
    private bool _haveDate;
    private int _counter;
    void Start()
    {
        // 使用异步操作已成为现代开发中不可或缺的手段，但它也带来了某些潜在问题。
        // 由于异步操作可能导致逻辑执行缺乏原子性，如果处理不当，可能会引发一些意想不到的错误。
        // 为了应对这一挑战，框架提供了 CoroutineLock，用于确保异步操作的原子性。
        // CoroutineLock 通过将异步逻辑按队列顺序执行，避免了因并发操作导致的竞争条件，从而有效解决了异步环境下的原子性问题。
        Button1.interactable = true;
        Button2.interactable = false;
        Button3.interactable = false;
        Button1.onClick.RemoveAllListeners();
        Button1.onClick.AddListener(() =>
        {
            StartAsync().Coroutine();
        });
        Button2.onClick.RemoveAllListeners();
        Button2.onClick.AddListener(() =>
        {
            RunAsync().Coroutine();
        });
        Button3.onClick.RemoveAllListeners();
        Button3.onClick.AddListener(() =>
        {
            RunCoroutineLockAsync().Coroutine();
        });
    }

    private async FTask StartAsync()
    {
        _scene = await Fantasy.Entry.Initialize(GetType().Assembly);
        Button1.interactable = false;
        Button2.interactable = true;
        Button3.interactable = true;
        Log.Debug("框架初始化完成");
    }

    private async FTask RunAsync()
    {
        _haveDate = false;
        Button2.interactable = false;
        // 这里模拟了一个大家正常开发都会使用的一个场景。
        // 模拟了同时有10个请求
        var tasks = new List<FTask>();
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(WaitAsync());
        }
        await FTask.WaitAll(tasks);
        Button2.interactable = true;
    }

    private async FTask RunCoroutineLockAsync()
    {
        _haveDate = false;
        Button3.interactable = false;
        // 这里模拟了一个大家正常开发都会使用的一个场景。
        // 模拟了同时有10个请求
        var tasks = new List<FTask>();
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(WaitCoroutineLockAsync());
        }
        await FTask.WaitAll(tasks);
        Button3.interactable = true;
    }

    private async FTask WaitAsync()
    {
        // 用TimerComponent来模拟，比如数据库读取数据等操作的耗时。
        // 比如这个方法是数据库里查询某一条数据。
        var haveData = await LoadDataAsync();
        // 比如到这里拿到数据。
        // 到这里只有两种情况:
        // 1、数据库有这个数据，返回了这个数据。
        // 2、数据库没有查询到这个数据，我要创建一个新的数据再插入到数据库中
        // 例如现在数据库没有查询这个数据，我用haveData = false来表示
        if (!haveData)
        {
            // 用这个来表示数据已经插入到数据库中了。
            // 这样后面的请求得到这里标记就知道数据库中已经存在这个数据了。
            _haveDate = true;
            // 如果不用WaitAsync来处理，如果同时有多个请求，都会执行到查询数据那里去数据库查询数据。
            // 如果数据库没有这个数据，那这些请求返回的数据都是没有查询到。
            // 但根据现在的逻辑，如果没有就会创建一个新的到数据库里。
            // 看现在的逻辑，如果有10个请求，都会创建一个新的到数据库了。
            // 问题已经很明显了。
            Log.Debug($"没有查询到数据，创建一个新的数据到数据库 重复插入次数:{++_counter}");
            return;
        }
        Log.Debug("查询到数据");
    }
    
    private async FTask WaitCoroutineLockAsync()
    {
        using (await _scene.CoroutineLockComponent.Wait(1,1))
        {
            // 使用CoroutineLock后，会保证逻辑是按照先入先出的方式进行执行。
            // 这样就不会出现上面例子出现的问题。
            // 这里只是拿读取数据库来做例子，但实际开发中会有比这个还要复杂很多的场景。
            // 如果不保证逻辑的顺序，会出现意想不到的问题。并且还很难排查出来。
            await WaitAsync();
        }
    }

    private FTask<bool> LoadDataAsync()
    {
        var tcs = FTask<bool>.Create();
        var haveDate = _haveDate;
        _scene.TimerComponent.Net.OnceTimer(2000, () => { tcs.SetResult(haveDate); });
        return tcs;
    }

    private async FTask Demo()
    {
        // 建议看官方的文档，里面介绍的更加详细。
        // 需要使用当前场景（Scene）中的 CoroutineLockComponent 组件:
        // Wait 
        // 在当前的异步操作中，按照队列的方式依次执行逻辑，直至成功释放锁为止。
        // 在此期间，每个任务将按照先后顺序等待锁的解除，以确保资源的有序访问和线程安全
        // Wait方法共有4个参数:
        // coroutineLockType: 指定等待的锁类型。该参数用于确定当前协程应等待哪种类型的锁，确保并发操作的有序性。
        // coroutineLockQueueKey: 指定等待的锁 ID。每个锁类型可能对应多个锁实例，此参数用于明确当前协程应等待的具体锁。
        // time: 设置等待锁的超时时间，单位为毫秒，默认值为 30000 毫秒（即 30 秒）。在超时未获取到锁的情况下，系统会记录一条 Error 级别的日志。
        // tag: 超时后用于标识具体是哪一个锁发生了超时的标记。此参数可以帮助在日志中快速定位超时问题，默认为 null。
        // Wait方法会返回WaitCoroutineLock类型的实例，可以通过WaitCoroutineLock.dispose进行解锁
        var waitCoroutineLock = await _scene.CoroutineLockComponent.Wait(1, 1);
        // 比如读取数据库加锁，那coroutineLockType肯定是固定的，但查询的逻辑是不固定的，所以可以用coroutineLockQueueKey来指定要锁定的逻辑的Id。
        // 比如查询某一个用户名是否存在，可以用这个用户名.GetHashCode()做为coroutineLockQueueKey
        // 解锁方式
        // 可以使用返回的waitCoroutineLock的dispose方法进行解锁
        // 因为waitCoroutineLock实现了IDisposable接口，也可以用using来进行自动调用dispose方法，这个也是推荐的方式。
        using (await _scene.CoroutineLockComponent.Wait(1, 1))
        {
            // 这里写逻辑，当执行到}后会自动调用dispose方法
        }
        // 也可以使用Release方法解锁，这里接受两个参数
        // coroutineLockType: 指定解锁的锁类型。该参数用于确定当前协程应等待哪种类型的锁，确保并发操作的有序性。
        // coroutineLockQueueKey: 指定解锁的锁 ID。每个锁类型可能对应多个锁实例，此参数用于明确当前协程应等待的具体锁。
        _scene.CoroutineLockComponent.Release(1,1);
        var coroutineLock = _scene.CoroutineLockComponent.Create(1);
        await coroutineLock.Wait(1);
    }
}
