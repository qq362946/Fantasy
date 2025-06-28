using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace Fantasy;
// 这个是一个自定义系统类型，用于决定系统类型。
// 也可以用枚举，看个人怎么使用了。
public static class CustomSystemType
{
    public const int RunSystem = 1;
}
// 这个是一个自定义系统，用于处理自定义事件。
public abstract class RunSystem<T> : CustomSystem<T> where T : Entity
{
    /// <summary>
    /// 自定义事件类型，用于决定事件的类型。
    /// </summary>
    public override int CustomEventType => CustomSystemType.RunSystem;
    /// <summary>
    /// 不知道为什么这样定义的，就照搬就可以了。
    /// </summary>
    /// <param name="self"></param>
    protected abstract override void Custom(T self);
    /// <summary>
    /// 不知道为什么这样定义的，就照搬就可以了。
    /// </summary>
    /// <returns></returns>
    public override Type EntitiesType() => typeof(T);
}
// 下面是一个测试自定义系统。
// 首先定义一个组件用来测试自定义系统。
public class TestCustomSystemComponent : Entity
{
    
}
// 现在给TestCustomSystemComponent组件添加一个自定义系统。
// 现在添加的就是上面定义的RunSystem自定义系统。
public class TestCustomSystemComponentRunSystem : RunSystem<TestCustomSystemComponent>
{
    protected override void Custom(TestCustomSystemComponent self)
    {
        Log.Debug($"执行了TestCustomSystemComponentRunSystem");
    }
}
// 执行方法
// 在任何实体下获得Scene下面的EntityComponent.CustomSystem方法。
// 比如下面这个
// 第一个参数是你要执行自定义系统的实体实例
// 第二个参数是自定义系统类型
// scene.EntityComponent.CustomSystem(testCustomSystemComponent, CustomSystemType.RunSystem);
// 你可以在OnCreateSceneEvent.cs的Handler方法里找到执行这个的例子。
