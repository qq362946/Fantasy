using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    public static class SendUtil
    {

        /// <summary>
        /// 只能Map服务器调
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene"></param>
        /// <param name="ids"></param>
        public static void M_Broadcast<T>(this Room room,Action<T,long> action,params long[] exId)where T: AMessage, IRouteMessage,new()
        {
            var scene=room.Scene;
            var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
            var logMgr = scene.GetComponent<LogicMgr>();
            var ids = room.AllClientID();
            foreach(var v in ids)
            {
                if (exId != null && exId.Contains(v)) continue;

                var t = new T();
                action(t,v);
                scene.NetworkMessagingComponent.SendInnerRoute(sceneConfig.RouteId, t);
            }
           
        }
        public static void SendClient<T>(Scene scene,long clientId,T message) where T : AMessage,IMessage
        {
            var session = scene.GetEntity<Session>(clientId);
            if (session != null)
            {
                session.Send(message);
            }
            else
            {
                var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
                scene.NetworkMessagingComponent.SendInnerRoute(sceneConfig.RouteId, new G2M_RemoveClient() { ClientID = clientId });
            }
        }
    }
}
