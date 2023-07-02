// using System;
// using System.IO;
//
// namespace Fantasy.Core.Network
// {
//     public enum NetworkMessageSchedulerHandlerType
//     {
//         None = 0,
//         ClientMessage = 1,
//         OuterMessageRoute = 2,
//         InnerMessage = 3,
//         ServerInnerMessage = 4
//     }
//
//     public interface INetworkMessageSchedulerHandler
//     {
//         NetworkMessageSchedulerHandlerType HandlerType();
//
//         FTask Handler(Session session, Type messageType, APackInfo packInfo);
//     }
// }