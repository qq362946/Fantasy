using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Network.Route;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy;

public sealed class C2G_SendAddressableToMapHandler : Message<C2G_SendAddressableToMap>
{
    protected override async FTask Run(Session session, C2G_SendAddressableToMap message)
    {
        var addressableRouteComponent = session.GetComponent<AddressableRouteComponent>();
        
        if (addressableRouteComponent == null)
        {
            return;
        }
        
        // Gate发送一个Addressable消息给MAP
        
        await session.Scene.NetworkMessagingComponent.SendAddressable(addressableRouteComponent.AddressableId,
            new G2M_SendAddressableMessage()
            {
                Tag = message.Tag
            });
    }
}