// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 表示消息处理器的接口，处理特定类型的消息。
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// 获取处理的消息类型。
        /// </summary>
        /// <returns>消息类型。</returns>
        public Type Type();
        /// <summary>
        /// 处理消息的方法。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="messageTypeCode">消息类型代码。</param>
        /// <param name="message">要处理的消息。</param>
        /// <returns>异步任务。</returns>
        FTask Handle(Session session, uint rpcId, uint messageTypeCode, object message);
    }

    /// <summary>
    /// 泛型消息基类，实现了 <see cref="IMessageHandler"/> 接口。
    /// </summary>
    public abstract class Message<T> : IMessageHandler
    {
        /// <summary>
        /// 获取处理的消息类型。
        /// </summary>
        /// <returns>消息类型。</returns>
        public Type Type()
        {
            return typeof(T);
        }

        /// <summary>
        /// 处理消息的方法。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="messageTypeCode">消息类型代码。</param>
        /// <param name="message">要处理的消息。</param>
        /// <returns>异步任务。</returns>
        public async FTask Handle(Session session, uint rpcId, uint messageTypeCode, object message)
        {
            try
            {
                await Run(session, (T) message);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        /// <summary>
        /// 运行消息处理逻辑。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="message">要处理的消息。</param>
        /// <returns>异步任务。</returns>
        protected abstract FTask Run(Session session, T message);
    }

    /// <summary>
    /// 泛型消息RPC基类，实现了 <see cref="IMessageHandler"/> 接口，用于处理请求和响应类型的消息。
    /// </summary>
    public abstract class MessageRPC<TRequest, TResponse> : IMessageHandler where TRequest : IRequest where TResponse : IResponse, new()
    {
        /// <summary>
        /// 获取处理的消息类型。
        /// </summary>
        /// <returns>消息类型。</returns>
        public Type Type()
        {
            return typeof(TRequest);
        }

        /// <summary>
        /// 处理消息的方法。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="rpcId">RPC标识。</param>
        /// <param name="messageTypeCode">消息类型代码。</param>
        /// <param name="message">要处理的消息。</param>
        /// <returns>异步任务。</returns>
        public async FTask Handle(Session session, uint rpcId, uint messageTypeCode, object message)
        {
            if (message is not TRequest request)
            {
                Log.Error($"消息类型转换错误: {message.GetType().Name} to {typeof(TRequest).Name}");
                return;
            }
            
            var response = new TResponse();
            var isReply = false;

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
                await Run(session, request, response, Reply);
            }
            catch (Exception e)
            {
                Log.Error(e);
                response.ErrorCode = InnerErrorCode.ErrRpcFail;
            }
            finally
            {
                Reply();
            }
        }

        /// <summary>
        /// 运行消息处理逻辑。
        /// </summary>
        /// <param name="session">会话对象。</param>
        /// <param name="request">请求消息。</param>
        /// <param name="response">响应消息。</param>
        /// <param name="reply">发送响应的方法。</param>
        /// <returns>异步任务。</returns>
        protected abstract FTask Run(Session session, TRequest request, TResponse response, Action reply);
    }
#if FANTASY_UNITY
    public interface IMessageDelegateHandler
    {
        /// <summary>
        /// 注册消息处理器。
        /// </summary>
        /// <param name="delegate"></param>
        public void Register(object @delegate);
        /// <summary>
        /// 取消注册消息处理器。
        /// </summary>
        /// <param name="delegate"></param>
        public int UnRegister(object @delegate);
        /// <summary>
        /// 处理消息的方法。
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        public void Handle(Session session, object message);
    }
    public delegate FTask MessageDelegate<in T>(Session session, T msg) where T : IMessage;
    public sealed class MessageDelegateHandler<T> : IMessageDelegateHandler, IDisposable where T : IMessage
    {
        private readonly List<MessageDelegate<T>> _delegates = new List<MessageDelegate<T>>();
        
        public Type Type()
        {
            return typeof(T);
        }

        public void Register(object @delegate)
        {
            var a = (MessageDelegate<T>)@delegate;
            
            if (_delegates.Contains(a))
            {
                Log.Error($"{typeof(T).Name} already register action delegateName:{a.Method.Name}");
                return;
            }

            _delegates.Add(a);
        }

        public int UnRegister(object @delegate)
        {
            _delegates.Remove((MessageDelegate<T>)@delegate);
            return _delegates.Count;
        }

        public void Handle(Session session, object message)
        {
            foreach (var registerDelegate in _delegates)
            {
                try
                {
                    registerDelegate(session, (T)message).Coroutine();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public void Dispose()
        {
            _delegates.Clear();
        }
    }
#endif
}