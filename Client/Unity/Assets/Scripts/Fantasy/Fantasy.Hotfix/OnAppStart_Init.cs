using System;
using System.Threading.Tasks;
using FairyGUI;
using Fantasy.Core;
using Fantasy.Core.Network;
using Fantasy.Helper;
using Fantasy.Hotfix.System;
using Fantasy.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Fantasy.Hotfix
{
    public sealed class OnAppStart_Init : AsyncEventSystem<OnAppStart>
    {
        public CoroutineLockQueueType _mian = new CoroutineLockQueueType("main");
        
        public override async FTask Handler(OnAppStart self)
        {  
            UIComponent.Initialize();

            var unitA = Entity.Create<UnitA>(self.Scene);
            unitA.AddComponent<MoveComponent>();
            // var scene = self.Scene;
            //
            // scene.CreateSession("192.168.31.37:20000", NetworkProtocolType.KCP, () =>
            // {
            //     Log.Error("not found server 192.168.31.37:20000");
            // });
            //
            // scene.Session.Send(new H_C2L_TestMessage()
            // {
            //     Key = 123
            // });
            //
            // await UIComponent.CreateAsync<LoginComponent>(self.Scene);


        }
    }
}