// using System.Buffers;
// using System.Diagnostics;
// using Amazon.Runtime.Internal;
// using Fantasy;
// using ProtoBuf;

// namespace Hotfix;

// [ProtoContract]
// public class TestModel
// {
    
// }

// public class OnServerStartComplete_Test : AsyncEventSystem<OnServerStartComplete>
// {
//     public override async FTask Handler(OnServerStartComplete self)
//     {
//         if (self.Server.Id == 2049)
//         {
//             // var startNew = Stopwatch.StartNew();
//             // for (int j = 0; j < 1000000; ++j)
//             // {
//             //     var a = new TestModel();
//             //
//             //     var memoryStream = MemoryPool<byte>.Shared.Rent(1024 * 1024).Memory;
//             //     ProtoBuffHelper.ToMemory(a, memoryStream);
//             // }
//             // Log.Debug($"{startNew.ElapsedMilliseconds}");
//             // Log.Debug("11111111");
//             // var sceneNetworkMessagingComponent = self.Server.Scene.NetworkMessagingComponent;
//             // await sceneNetworkMessagingComponent.CallInnerServer(1025, new TestServerRequest());
//             // Log.Debug("22222222");
//             List<FTask> list = new List<FTask>(100000);
//             var sceneNetworkMessagingComponent = self.Server.Scene.NetworkMessagingComponent;
            
//             async FTask Call()
//             {
//                 await sceneNetworkMessagingComponent.CallInnerServer(1025, new TestServerRequest());
//             }
            
//             for (int j = 0; j < 1000000000; ++j)
//             {
//                 list.Clear();
//                 for (int i = 0; i < list.Capacity; ++i)
//                 {
//                     list.Add(Call());
//                 }
                
//                 await FTask.WhenAll(list);
//             }
//         }
//     }
// }