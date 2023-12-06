using UnityEngine;
using System.Collections.Generic;
using Fantasy;

public partial class Sender
{
    // 使用字典存储不同消息类型对应的处理方法
    private Dictionary<ReciveType, MessageHandler> messageHandlers = new Dictionary<ReciveType, MessageHandler>();
    private Dictionary<ReciveType, AddressableHandler> addressableHandlers = new Dictionary<ReciveType, AddressableHandler>();
    
    public void Recive(ReciveType reciveType,IMessage message){

        // 检查是否有注册过该消息类型的处理方法
        if (messageHandlers.TryGetValue(reciveType, out var handler))
        {
            // 调用处理方法
            handler.Invoke(message);
        }
    }

    public void Recive(ReciveType reciveType,IAddressableRouteMessage message){
        // 检查是否有注册过该消息类型的处理方法
        if (addressableHandlers.TryGetValue(reciveType, out var handler))
        {
            // 调用处理方法
            handler.Invoke(message);
        }
    }

    // 注册消息处理方法
    public void RegisterMessageHandler(ReciveType reciveType, MessageHandler handler)
    {
        if (messageHandlers.ContainsKey(reciveType))
            messageHandlers[reciveType] = handler;
        else
            messageHandlers.Add(reciveType, handler);
    }

    public void RegisterMessageHandler(ReciveType reciveType, AddressableHandler handler)
    {
        if (addressableHandlers.ContainsKey(reciveType))
            addressableHandlers[reciveType] = handler;
        else
            addressableHandlers.Add(reciveType, handler);
    }
}