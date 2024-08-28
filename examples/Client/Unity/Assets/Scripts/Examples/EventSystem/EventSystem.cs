using System.Collections;
using System.Collections.Generic;
using Fantasy;
using UnityEngine;
using UnityEngine.UI;

public struct TestEvent
{
    public int Age;
}

public class TestEventEntity : Entity
{
    public int Age;
}

// 这个是订阅了一个同步的事件，监听的事件参数是TestEvent
public class OnTestEvent : EventSystem<TestEvent>
{
    public override void Handler(TestEvent self)
    {
        Log.Debug($"接收到TestEvent事件{self.Age}");
    }
}

// 这个是订阅了一个异步步的事件，监听的事件参数是TestEvent
public class OnTestEventAsync : AsyncEventSystem<TestEvent>
{
    public override async FTask Handler(TestEvent self)
    {
        Log.Debug($"接收到TestEvent 异步事件{self.Age}");
        await FTask.CompletedTask;
    }
}

// 这个是订阅了一个同步的事件，监听的事件参数是TestEvent
public class OnTestEventEntity : EventSystem<TestEventEntity>
{
    public override void Handler(TestEventEntity self)
    {
        Log.Debug($"接收到TestEventEntity事件{self.Age}");
    }
}

// 这个是订阅了一个异步步的事件，监听的事件参数是TestEvent
public class OnTestEventEntityAsync : AsyncEventSystem<TestEventEntity>
{
    public override async FTask Handler(TestEventEntity self)
    {
        Log.Debug($"接收到TestEventEntity 异步事件{self.Age}");
        await FTask.CompletedTask;
    }
}

public class EventSystem : MonoBehaviour
{
    public Button Button1;
    public Button Button2;
    public Button Button3;
    private Scene _scene;
    private void Start()
    {
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
            StructEvent().Coroutine();
        });
        Button3.onClick.RemoveAllListeners();
        Button3.onClick.AddListener(() =>
        {
            EntityEvent().Coroutine();
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

    private async FTask StructEvent()
    {
        // 发送一个同步的事件
        _scene.EventComponent.Publish(new TestEvent()
        {
            Age = 1
        });
        // 发送一个异步的事件
        await _scene.EventComponent.PublishAsync(new TestEvent()
        {
            Age = 1
        });
    }
    
    private async FTask EntityEvent()
    {
        // 发送一个同步的事件
        _scene.EventComponent.Publish(new TestEventEntity()
        {
            Age = 1
        });
        // 发送一个异步的事件
        await _scene.EventComponent.PublishAsync(new TestEventEntity()
        {
            Age = 1
        });
    }
}
