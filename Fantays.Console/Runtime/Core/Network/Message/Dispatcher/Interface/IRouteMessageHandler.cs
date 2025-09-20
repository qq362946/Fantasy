using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Serialize;

#if FANTASY_NET
// ReSharper disable InconsistentNaming

namespace Fantasy.Network.Interface
{
    /// <summary>
    /// 表示路由消息处理器的接口，处理特定类型的路由消息。
    /// </summary>
    public interface IRouteMessageHandler
    {
        /// <summary>
        /// 获取处理的消息类型。
        /// </summary>
        /// <returns>消息类型。</returns>
        public Type Type();

        /// <summary>
        /// 处理路由消息的方法。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="entity">实体对象。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeMessage">要处理的路由消息。</param>
        /// <returns>异步任务。</returns>
        FTask Handle(Session session, Entity entity, uint rpcId, object routeMessage);
    }

    /// <summary>
    /// 泛型路由基类，实现了 <see cref="IRouteMessageHandler"/> 接口，用于处理特定实体和路由消息类型的路由。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TMessage">路由消息类型。</typeparam>
    public abstract class Route<TEntity, TMessage> : IRouteMessageHandler where TEntity : Entity where TMessage : IRouteMessage
    {
        /// <summary>
        /// 获取处理的消息类型。
        /// </summary>
        /// <returns>消息类型。</returns>
        public Type Type()
        {
            return typeof(TMessage);
        }

        /// <summary>
        /// 处理路由消息的方法。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="entity">实体对象。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeMessage">要处理的路由消息。</param>
        /// <returns>异步任务。</returns>
        public async FTask Handle(Session session, Entity entity, uint rpcId, object routeMessage)
        {
            if (routeMessage is not TMessage ruteMessage)
            {
                Log.Error($"Message type conversion error: {routeMessage.GetType().FullName} to {typeof(TMessage).Name}");
                return;
            }

            if (entity is not TEntity tEntity)
            {
                Log.Error($"{this.GetType().Name} Route type conversion error: {entity.GetType().Name} to {typeof(TEntity).Name}");
                return;
            }

            try
            {
                await Run(tEntity, ruteMessage);
            }
            catch (Exception e)
            {
                if (entity is not Scene scene)
                {
                    scene = entity.Scene;
                }
                
                Log.Error($"SceneConfigId:{session.Scene.SceneConfigId} ProcessConfigId:{scene.Process.Id} SceneType:{scene.SceneType} EntityId {tEntity.Id} : Error {e}");
            }
        }

        /// <summary>
        /// 运行路由消息处理逻辑。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <param name="message">要处理的路由消息。</param>
        /// <returns>异步任务。</returns>
        protected abstract FTask Run(TEntity entity, TMessage message);
    }

    /// <summary>
    /// 泛型路由RPC基类，实现了 <see cref="IRouteMessageHandler"/> 接口，用于处理请求和响应类型的路由。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TRouteRequest">路由请求类型。</typeparam>
    /// <typeparam name="TRouteResponse">路由响应类型。</typeparam>
    public abstract class RouteRPC<TEntity, TRouteRequest, TRouteResponse> : IRouteMessageHandler where TEntity : Entity where TRouteRequest : IRouteRequest where TRouteResponse : AMessage, IRouteResponse, new()
    {
        /// <summary>
        /// 获取处理的消息类型。
        /// </summary>
        /// <returns>消息类型。</returns>
        public Type Type()
        {
            return typeof(TRouteRequest);
        }

        /// <summary>
        /// 处理路由消息的方法。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="entity">实体对象。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeMessage">要处理的路由消息。</param>
        /// <returns>异步任务。</returns>
        public async FTask Handle(Session session, Entity entity, uint rpcId, object routeMessage)
        {
            if (routeMessage is not TRouteRequest tRouteRequest)
            {
                Log.Error($"Message type conversion error: {routeMessage.GetType().FullName} to {typeof(TRouteRequest).Name}");
                return;
            }

            if (entity is not TEntity tEntity)
            {
                Log.Error($"{this.GetType().Name} Route type conversion error: {entity.GetType().Name} to {typeof(TEntity).Name}");
                return;
            }
            
            var isReply = false;
            var response = new TRouteResponse();
            
            void Reply()
            {
                if (isReply)
                {
                    return;
                }

                isReply = true;

                if (session.IsDisposed)
                {
                    return;
                }

                session.Send(response, rpcId);
            }
            
            try
            {
                await Run(tEntity, tRouteRequest, response, Reply);
            }
            catch (Exception e)
            {
                if (entity is not Scene scene)
                {
                    scene = entity.Scene;
                }
                
                Log.Error($"SceneConfigId:{session.Scene.SceneConfigId} ProcessConfigId:{scene.Process.Id} SceneType:{scene.SceneType} EntityId {tEntity.Id} : Error {e}");
                response.ErrorCode = InnerErrorCode.ErrRpcFail;
            }
            finally
            {
                Reply();
            }
        }

        /// <summary>
        /// 运行路由消息处理逻辑。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <param name="request">请求路由消息。</param>
        /// <param name="response">响应路由消息。</param>
        /// <param name="reply">发送响应的方法。</param>
        /// <returns>异步任务。</returns>
        protected abstract FTask Run(TEntity entity, TRouteRequest request, TRouteResponse response, Action reply);
    }

    /// <summary>
    /// 泛型可寻址路由基类，实现了 <see cref="IRouteMessageHandler"/> 接口，用于处理特定实体和可寻址路由消息类型的路由。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TMessage">可寻址路由消息类型。</typeparam>
    public abstract class Addressable<TEntity, TMessage> : IRouteMessageHandler where TEntity : Entity where TMessage : IAddressableRouteMessage
    {
        /// <summary>
        /// 获取消息类型。
        /// </summary>
        /// <returns>消息类型。</returns>
        public Type Type()
        {
            return typeof(TMessage);
        }

        /// <summary>
        /// 处理可寻址路由消息。
        /// </summary>
        /// <param name="session">会话。</param>
        /// <param name="entity">实体。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeMessage">可寻址路由消息。</param>
        public async FTask Handle(Session session, Entity entity, uint rpcId, object routeMessage)
        {
            if (routeMessage is not TMessage ruteMessage)
            {
                Log.Error($"Message type conversion error: {routeMessage.GetType().FullName} to {typeof(TMessage).Name}");
                return;
            }

            if (entity is not TEntity tEntity)
            {
                Log.Error($"{this.GetType().Name} Route type conversion error: {entity.GetType().Name} to {typeof(TEntity).Name}");
                return;
            }

            try
            {
                await Run(tEntity, ruteMessage);
            }
            catch (Exception e)
            {
                if (entity is not Scene scene)
                {
                    scene = entity.Scene;
                }
                
                Log.Error($"SceneConfigId:{session.Scene.SceneConfigId} ProcessConfigId:{scene.Process.Id} SceneType:{scene.SceneType} EntityId {tEntity.Id} : Error {e}");
            }
            finally
            {
                session.Send(new RouteResponse(), rpcId);
            }
        }

        /// <summary>
        /// 运行处理可寻址路由消息。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <param name="message">可寻址路由消息。</param>
        protected abstract FTask Run(TEntity entity, TMessage message);
    }

    /// <summary>
    /// 泛型可寻址RPC路由基类，实现了 <see cref="IRouteMessageHandler"/> 接口，用于处理特定实体和可寻址RPC路由请求类型的路由。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TRouteRequest">可寻址RPC路由请求类型。</typeparam>
    /// <typeparam name="TRouteResponse">可寻址RPC路由响应类型。</typeparam>
    public abstract class AddressableRPC<TEntity, TRouteRequest, TRouteResponse> : IRouteMessageHandler where TEntity : Entity where TRouteRequest : IAddressableRouteRequest where TRouteResponse : IAddressableRouteResponse, new()
    {
        /// <summary>
        /// 获取消息类型。
        /// </summary>
        /// <returns>消息类型。</returns>
        public Type Type()
        {
            return typeof(TRouteRequest);
        }

        /// <summary>
        /// 处理可寻址RPC路由请求。
        /// </summary>
        /// <param name="session">会话。</param>
        /// <param name="entity">实体。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeMessage">可寻址RPC路由请求。</param>
        public async FTask Handle(Session session, Entity entity, uint rpcId, object routeMessage)
        {
            if (routeMessage is not TRouteRequest tRouteRequest)
            {
                Log.Error($"Message type conversion error: {routeMessage.GetType().FullName} to {typeof(TRouteRequest).Name}");
                return;
            }

            if (entity is not TEntity tEntity)
            {
                Log.Error($"{this.GetType().Name} Route type conversion error: {entity.GetType().Name} to {typeof(TEntity).Name}");
                return;
            }
            
            var isReply = false;
            var response = new TRouteResponse();
            
            void Reply()
            {
                if (isReply)
                {
                    return;
                }

                isReply = true;

                if (session.IsDisposed)
                {
                    return;
                }

                session.Send(response, rpcId);
            }
            
            try
            {
                await Run(tEntity, tRouteRequest, response, Reply);
            }
            catch (Exception e)
            {
                if (entity is not Scene scene)
                {
                    scene = entity.Scene;
                }
                
                Log.Error($"SceneConfigId:{session.Scene.SceneConfigId} ProcessConfigId:{scene.Process.Id} SceneType:{scene.SceneType} EntityId {tEntity.Id} : Error {e}");
                response.ErrorCode = InnerErrorCode.ErrRpcFail;
            }
            finally
            {
                Reply();
            }
        }

        /// <summary>
        /// 运行处理可寻址RPC路由请求。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <param name="request">可寻址RPC路由请求。</param>
        /// <param name="response">可寻址RPC路由响应。</param>
        /// <param name="reply">回复操作。</param>
        protected abstract FTask Run(TEntity entity, TRouteRequest request, TRouteResponse response, Action reply);
    }
    
    /// <summary>
    /// 泛型漫游路由基类，实现了 <see cref="IRouteMessageHandler"/> 接口，用于处理特定实体和漫游路由消息类型的路由。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TMessage">漫游消息类型。</typeparam>
    public abstract class Roaming<TEntity, TMessage> : IRouteMessageHandler where TEntity : Entity where TMessage : IRoamingMessage
    {
        /// <summary>
        /// 获取消息类型。
        /// </summary>
        /// <returns>消息类型。</returns>
        public Type Type()
        {
            return typeof(TMessage);
        }

        /// <summary>
        /// 处理漫游消息。
        /// </summary>
        /// <param name="session">会话。</param>
        /// <param name="entity">实体。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeMessage">漫游消息。</param>
        public async FTask Handle(Session session, Entity entity, uint rpcId, object routeMessage)
        {
            if (routeMessage is not TMessage ruteMessage)
            {
                Log.Error($"Message type conversion error: {routeMessage.GetType().FullName} to {typeof(TMessage).Name}");
                return;
            }

            if (entity is not TEntity tEntity)
            {
                Log.Error($"{this.GetType().Name} Route type conversion error: {entity.GetType().Name} to {typeof(TEntity).Name}");
                return;
            }

            try
            {
                await Run(tEntity, ruteMessage);
            }
            catch (Exception e)
            {
                if (entity is not Scene scene)
                {
                    scene = entity.Scene;
                }
                
                Log.Error($"SceneConfigId:{session.Scene.SceneConfigId} ProcessConfigId:{scene.Process.Id} SceneType:{scene.SceneType} EntityId {tEntity.Id} : Error {e}");
            }
            finally
            {
                session.Send(new RouteResponse(), rpcId);
            }
        }

        /// <summary>
        /// 运行处理漫游消息。
        /// </summary>
        /// <param name="terminus">终点实体。</param>
        /// <param name="message">漫游消息。</param>
        protected abstract FTask Run(TEntity terminus, TMessage message);
    }

    /// <summary>
    /// 漫游RPC路由基类，实现了 <see cref="IRouteMessageHandler"/> 接口，用于处理特定实体和漫游RPC路由请求类型的路由。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TRouteRequest">漫游RPC路由请求类型。</typeparam>
    /// <typeparam name="TRouteResponse">漫游RPC路由响应类型。</typeparam>
    public abstract class RoamingRPC<TEntity, TRouteRequest, TRouteResponse> : IRouteMessageHandler where TEntity : Entity where TRouteRequest : IRoamingRequest where TRouteResponse : IRoamingResponse, new()
    {
        /// <summary>
        /// 获取消息类型。
        /// </summary>
        /// <returns>消息类型。</returns>
        public Type Type()
        {
            return typeof(TRouteRequest);
        }

        /// <summary>
        /// 处理漫游RPC路由请求。
        /// </summary>
        /// <param name="session">会话。</param>
        /// <param name="entity">实体。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="routeMessage">漫游RPC路由请求。</param>
        public async FTask Handle(Session session, Entity entity, uint rpcId, object routeMessage)
        {
            if (routeMessage is not TRouteRequest tRouteRequest)
            {
                Log.Error($"Message type conversion error: {routeMessage.GetType().FullName} to {typeof(TRouteRequest).Name}");
                return;
            }

            if (entity is not TEntity tEntity)
            {
                Log.Error($"{this.GetType().Name} Route type conversion error: {entity.GetType().Name} to {typeof(TEntity).Name}");
                return;
            }
            
            var isReply = false;
            var response = new TRouteResponse();
            
            void Reply()
            {
                if (isReply)
                {
                    return;
                }

                isReply = true;

                if (session.IsDisposed)
                {
                    return;
                }

                session.Send(response, rpcId);
            }
            
            try
            {
                await Run(tEntity, tRouteRequest, response, Reply);
            }
            catch (Exception e)
            {
                if (entity is not Scene scene)
                {
                    scene = entity.Scene;
                }
                
                Log.Error($"SceneConfigId:{session.Scene.SceneConfigId} ProcessConfigId:{scene.Process.Id} SceneType:{scene.SceneType} EntityId {tEntity.Id} : Error {e}");
                response.ErrorCode = InnerErrorCode.ErrRpcFail;
            }
            finally
            {
                Reply();
            }
        }

        /// <summary>
        /// 运行处理漫游RPC路由请求。
        /// </summary>
        /// <param name="terminus">终点实体。</param>
        /// <param name="request">漫游RPC路由请求。</param>
        /// <param name="response">漫游RPC路由响应。</param>
        /// <param name="reply">回复操作。</param>
        protected abstract FTask Run(TEntity terminus, TRouteRequest request, TRouteResponse response, Action reply);
    }
}
#endif