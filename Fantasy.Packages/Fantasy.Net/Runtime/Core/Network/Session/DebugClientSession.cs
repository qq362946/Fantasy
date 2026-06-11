#if FANTASY_UNITY || FANTASY_CONSOLE
using System;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network.Interface;

namespace Fantasy.Network
{
    internal static class IRequestSupportedChecker<T> where T : IMessage
    {
        public static bool IsRequest { get; }
        static IRequestSupportedChecker()
        {
            IsRequest= typeof(IRequest).IsAssignableFrom(typeof(T));
        }
    }
    
    public class DebugClientSession : Session
    {
        public override void Send<T>(T message, uint rpcId = 0, long address = 0)
        {
            if(!IRequestSupportedChecker<T>.IsRequest)
            {
                Log.Debug($"Send Message: {typeof(T).Name} Json:{message.ToJson()}");
            }
            
            base.Send(message, rpcId, address);
        }

        public override void Send(IMessage message, Type messageType, uint rpcId = 0, long address = 0)
        {
            if(message is not IRequest)
            {
                Log.Debug($"Send Message: {messageType.Name} Json:{message.ToJson()}");
            }
            
            base.Send(message, messageType, rpcId, address);
        }

        public override FTask<IResponse> Call<T>(T request, long address = 0)
        {
            if (request.OpCode() != 4026531841)
            {
                Log.Debug($"Send Message: {typeof(T).Name} Json:{request.ToJson()}");
            }
        
            return base.Call(request, address);
        }
    }
}
#endif