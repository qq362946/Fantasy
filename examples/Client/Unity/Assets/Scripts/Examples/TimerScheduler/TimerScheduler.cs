using System.Collections;
using System.Collections.Generic;
using Fantasy;
using UnityEngine;
using UnityEngine.UI;

public struct OnceTimerEvent
{
    public string Tag;
}

public sealed class OnOnceTimerEvent : EventSystem<OnceTimerEvent>
{
    public override void Handler(OnceTimerEvent self)
    {
        Log.Debug("使用OnceTimer在2秒后执行了一个Event");
    }
}

public class TimerScheduler : MonoBehaviour
{
    public Button Button1;
    public Button Button2;
    public Button Button3;
    public Button Button4;
    public Button Button5;
    private Queue<long> _timersQueue = new Queue<long>();
    
    private Scene _scene;
    private void Start()
    {
        //需要使用当前场景（Scene）中的 TimerComponent 组件:
        // Unity:支持Unity的时间缩放的时间任务调度。（只有Unity平台下有）
        // Net:根据系统事件的任务调度。
        // 这里用Net来做功能演示。Unity用法跟Net是一样的。
        Button1.interactable = true;
        Button2.interactable = false;
        Button3.interactable = false;
        Button4.interactable = false;
        Button5.interactable = false;
        Button1.onClick.RemoveAllListeners();
        Button1.onClick.AddListener(() =>
        {
            StartAsync().Coroutine();
        });
        Button2.onClick.RemoveAllListeners();
        Button2.onClick.AddListener(() =>
        {
            Wait().Coroutine();
        });
        Button3.onClick.RemoveAllListeners();
        Button3.onClick.AddListener(OnceTimer);
        Button4.onClick.RemoveAllListeners();
        Button4.onClick.AddListener(RepeatedTimer);
        Button5.onClick.RemoveAllListeners();
        Button5.onClick.AddListener(Remove);
    }

    private async FTask StartAsync()
    {
        _scene = await Fantasy.Entry.Initialize(GetType().Assembly);
        Button1.interactable = false;
        Button2.interactable = true;
        Button3.interactable = true;
        Button4.interactable = true;
        Button5.interactable = true;
        Log.Debug("框架初始化完成");
    }

    private async FTask Wait()
    {
        // WaitAsync
        // 在当前代码中，可以通过设置延时来等待一段时间后再继续执行后续逻辑，从而确保依赖的资源准备就绪或前置条件满足。
        // time参数:毫秒单位
        // 等待2秒后执行下面的逻辑。
        Button2.interactable = false;
        await _scene.TimerComponent.Net.WaitAsync(2000);
        Log.Debug("使用了WaitAsync方法等待了2秒后执行到了这里。");
        // WaitTillAsync
        // 在当前代码中，可以通过设置延时来等待一段时间后再继续执行后续逻辑，从而确保依赖的资源准备就绪或前置条件满足。
        // time参数:绝对时间,毫秒单位
        await _scene.TimerComponent.Net.WaitTillAsync(TimeHelper.Now + 2000);
        Log.Debug("使用了WaitTillAsync方法等待了2秒后执行到了这里。");
        Button2.interactable = true;
    }

    private void OnceTimer()
    {
        Button3.interactable = false;
        // OnceTimer
        // 设置一个定时任务，指定在未来的某个特定时间自动执行
        // Action
        // 传递一个Action在未来的某个时间执行。
        // time参数:毫秒单位
        // action:时间到执行的回调。
        // 返回参数:返回一个时间任务的Id,可以通过这个Id取消这个任务。
        var timerId = _scene.TimerComponent.Net.OnceTimer(2000, () =>
        {
            Log.Debug("使用OnceTimer在2秒后执行了一个Action");
        });
        _timersQueue.Enqueue(timerId);
        // Event
        // Action 已经能够满足大部分需求，但在某些情况下，可能会遇到热重载的问题。
        // 由于 Action 在热重载时无法替换为新的 Action，因此需要采用 Event 的方式。
        // 在热重载过程中，新的 Event 会覆盖旧的，从而确保系统正常更新。
        // time参数:毫秒单位
        // Event参数:传入一个事件参数，如果不知道怎么定义可以参考事件系统。
        // 返回参数:返回一个时间任务的Id,可以通过这个Id取消这个任务。
        timerId = _scene.TimerComponent.Net.OnceTimer(2000, new OnceTimerEvent()
        {
            Tag = "Hello OnceTimer"
        });
        _timersQueue.Enqueue(timerId);
        // OnceTillTimer
        // 这个方法同OnceTimer一样，唯一不同的是time参数的时间是绝对时间。
        // 这个传入的是未来的时间，所以这个参数是当前的时间+1000 * 60也就是未来1分钟后执行。
        // 执行Action
        // var timerId = _scene.TimerComponent.Net.OnceTillTimer(TimeHelper.Now + 1000 * 60, () =>
        // {
        //     Log.Debug("Timer started");
        // });
        // 执行Event
        // var timerId = _scene.TimerComponent.Net.OnceTillTimer(TimeHelper.Now + 1000 * 60, new OnceTimerEvent());
        Button3.interactable = true;
    }

    private void RepeatedTimer()
    {
        Button4.interactable = false;
        // RepeatedTimer
        // 执行一个任务，该任务会按照指定的时间间隔反复运行，直到明确取消为止。
        // 该方法同样支持Action和Event，由于OnceTimer方法已经演示过这两个使用的不同。
        // 这里只用Action方式进行演示。
        var timerId = _scene.TimerComponent.Net.RepeatedTimer(1000, () =>
        {
            Log.Debug("Timer started");
        });
        _timersQueue.Enqueue(timerId);
        // 后面可以用Remove方法，把这个timerId传入取消任务。
        Button4.interactable = true;
    }

    private void Remove()
    {
        Button2.interactable = false;
        // Remove
        // 根据任务Id，取消一个任务。
        // timerId:要取消的任务Id。
        // 因为上面运行的任务可能有很多，所以我这里用queue来存储Id,用于这里取消全部任务。
        var taskCount = _timersQueue.Count;
        while (_timersQueue.TryDequeue(out var timerId))
        {
            _scene.TimerComponent.Net.Remove(timerId);
        }
        Button2.interactable = true;
        Log.Debug($"已经取消了当前运行的{taskCount}个任务。");
    }
}
