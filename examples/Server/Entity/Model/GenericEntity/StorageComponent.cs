using Fantasy.Attributes;
using Fantasy.Entitas;

namespace Fantasy;

public interface I可储物
{
}
public interface I可交易
{
    int 市价 { get; }
}

public class 武器 : Entity, I可储物, I可交易 { public int 市价 { get; } }
public class 圣遗物 : Entity, I可储物, I可交易 { public int 市价 { get; } }
public class 角色皮肤 : Entity, I可储物, I可交易 { public int 市价 { get; } }

///关键是下面这里, 打上ClosedGenericAttribute进行注册, 否则会触发Awake 阶段运行时反射!
///服务器肯定要打, 客户端如果是少量实体Awake的话, 其实也可以不打, 可以接受动态反射就行.  
[ClosedGeneric(typeof(StorageComponent<武器>))]
[ClosedGeneric(typeof(StorageComponent<圣遗物>))]
[ClosedGeneric(typeof(StorageComponent<角色皮肤>))]
///泛型储物组件
public class StorageComponent<T> : Entity where T : Entity, I可交易
{
    public int money;

    public EntityReference<T> 当前聚焦物品;
}

public class 交易帮助类 {
    public static void 出售<T>(StorageComponent<T>卖方, T 交易物, StorageComponent<T> 买方) where T : Entity, I可交易
    {
        卖方.money += 交易物.市价;
        买方.money -= 交易物.市价;

        卖方.RemoveComponent(交易物);
        买方.AddComponent(交易物);
    }
}

public class 角色 : Entity
{

    public void 初始化() {

        AddComponent<角色皮肤>();
        初始化角色Storage();
    }

    public void 初始化角色Storage(){

        AddComponent<StorageComponent<圣遗物>>();
        AddComponent<StorageComponent<武器>>();
        AddComponent<StorageComponent<角色皮肤>>().当前聚焦物品 = this.GetComponent<角色皮肤>();
    }

    public void 卖东西给 <T>(角色 买方) where T : Entity, I可交易
    {
        交易帮助类.出售(
            GetComponent<StorageComponent<T>>(),
            GetComponent<StorageComponent<T>>().当前聚焦物品,
            买方.GetComponent<StorageComponent<T>>()
            );
    }
}