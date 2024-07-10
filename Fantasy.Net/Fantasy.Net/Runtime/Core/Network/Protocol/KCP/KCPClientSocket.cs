using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using KCP;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
    public delegate void OnReceiveCompletedDelegate(byte[] buffer, ref int length);
    public class KCPClientSocket : IDisposable
    {
        private Kcp _kcp;
        private Scene _scene;
        private Socket _socket;
        private Thread _thread;
        private int _maxSndWnd;
        private bool _isDisconnect;
        private long _connectTimeoutId;
        private long _updateMinTime;
        private long _startTime;
        private uint _channelId;
        private KCPSettings _kcpSettings;
        private ThreadSynchronizationContext _threadSynchronizationContext;
        private readonly byte[] _rawReceiveBuffer = new byte[2048];
        private readonly byte[] _receiveBuffer = new byte[2048];
        private readonly byte[] _channelIdBytes = new byte[sizeof(uint)];
        private EndPoint _ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private readonly SortedSet<uint> _updateTimer = new SortedSet<uint>();
        private readonly Queue<uint> _updateTimeOutTime = new Queue<uint>();
        public uint ChannelId 
        {
            get => _channelId;
            private set
            {
                _channelId = value;
                _channelId.GetBytes(_channelIdBytes);
            }
        }
        public bool IsDisposed { get; private set; }
        public event Action OnConnectTimeout;
        public event Action OnConnectComplete;
        public event Action OnConnectDisconnect;
        public OnReceiveCompletedDelegate OnReceiveCompleted;
        private uint TimeNow => (uint) (TimeHelper.Now - _startTime);
        public void Connect(Scene scene, NetworkTarget networkTarget, IPEndPoint remoteEndPoint, int connectTimeout = 5000)
        {
            _scene = scene;
            _startTime = TimeHelper.Now;
            ChannelId = CreateChannelId();
            _kcpSettings = KCPSettings.Create(networkTarget);
            _maxSndWnd = _kcpSettings.MaxSendWindowSize;
            _connectTimeoutId = _scene.TimerComponent.Core.OnceTimer(connectTimeout, () => { OnConnectTimeout?.Invoke(); });
            _thread = new Thread(() =>
            {
                _threadSynchronizationContext = new ThreadSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
                SynchronizationContext.SetSynchronizationContext(_threadSynchronizationContext);
                _socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                _socket.SetSocketBufferToOsLimit();
                _socket.SetSioUdpConnReset();
                _socket.Connect(remoteEndPoint);
                SendHeader(KcpHeader.RequestConnection);
                Loop();
            });
            _thread.Start();
        }
        
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            IsDisposed = true;
            
            if (!_isDisconnect)
            {
                SendHeader(KcpHeader.Disconnect);
            }
            
            _thread.Join();
            
            if (_socket.Connected)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (OnConnectDisconnect != null)
                {
                    _scene.ThreadSynchronizationContext.Post(() => OnConnectDisconnect?.Invoke());
                }
                
                _socket.Disconnect(false);
                _socket.Close();
            }
            
            ClearConnectTimeout();
            _scene = null;
        }
        
        public void Send(MemoryStream memoryStream)
        {
            _threadSynchronizationContext.Post(() =>
            {
                var waitSendSize = _kcp.WaitSendCount;

                if (waitSendSize > _maxSndWnd)
                {
                    Log.Warning($"ERR_KcpWaitSendSizeTooLarge {waitSendSize} > {_maxSndWnd}");
                    Dispose();
                    return;
                }

                _kcp.Send(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                
                AddToUpdate(0);
            });
        }

        private void Loop()
        {
            while (!IsDisposed)
            {
                while (_socket != null && _socket.Available > 0)
                {
                    try
                    {
                        var receiveLength = _socket.ReceiveFrom_NonAlloc(_rawReceiveBuffer, ref _ipEndPoint);

                        if (receiveLength < 5)
                        {
                            continue;
                        }

                        var channelId = 0U;
                        var header = (KcpHeader)_rawReceiveBuffer[0];

                        switch (header)
                        {
                            case KcpHeader.RepeatChannelId:
                            {
                                // 到这里是客户端的channelId再服务器上已经存在、需要重新生成一个再次尝试连接
                                ChannelId = CreateChannelId();
                                SendHeader(KcpHeader.RequestConnection);
                                break;
                            }
                            case KcpHeader.WaitConfirmConnection:
                            {
                                channelId = BitConverter.ToUInt32(_rawReceiveBuffer, 1);
                                
                                if (channelId != ChannelId)
                                {
                                    break;
                                }

                                ClearConnectTimeout();
                                SendHeader(KcpHeader.ConfirmConnection);
                                _kcp = new Kcp(ChannelId, KcpSpanCallback);
                                _kcp.SetNoDelay(1, 5, 2, 1);
                                _kcp.SetWindowSize(_kcpSettings.SendWindowSize, _kcpSettings.ReceiveWindowSize);
                                _kcp.SetMtu(_kcpSettings.Mtu);
                                _kcp.SetMinrto(30);
                                _scene.ThreadSynchronizationContext.Post(() =>
                                {
                                    OnConnectComplete?.Invoke();
                                });
                                break;
                            }
                            case KcpHeader.ReceiveData:
                            {
                                var messageLength = receiveLength - 5;

                                if (messageLength <= 0)
                                {
                                    Log.Warning($"KCP Server KcpHeader.Data  messageLength <= 0");
                                    break;
                                }

                                channelId = BitConverter.ToUInt32(_rawReceiveBuffer, 1);

                                if (channelId != ChannelId)
                                {
                                    break;
                                }

                                Input(_rawReceiveBuffer, ref messageLength);
                                break;
                            }
                            case KcpHeader.Disconnect:
                            {
                                if (channelId != ChannelId)
                                {
                                    break;
                                }

                                _isDisconnect = true;
                                Dispose();
                                break;
                            }
                        }
                    }
                    // this is fine, the socket might have been closed in the other end
                    catch (SocketException)
                    {
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }

                try
                {
                    CheckUpdate();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                _threadSynchronizationContext.Update();
                Thread.Yield();
            }
        }

        #region Update

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckUpdate()
        {
            var nowTime = TimeNow;

            if (nowTime >= _updateMinTime && _updateTimer.Count > 0)
            {
                foreach (var timeId in _updateTimer)
                {
                    if (timeId > nowTime)
                    {
                        _updateMinTime = timeId;
                        break;
                    }

                    _updateTimeOutTime.Enqueue(timeId);
                }

                while (_updateTimeOutTime.TryDequeue(out var timeId))
                {
                    KcpUpdate();
                    _updateTimer.Remove(timeId);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToUpdate(uint tillTime)
        {
            if (tillTime == 0)
            {
                KcpUpdate();
                return;
            }
        
            if (tillTime < _updateMinTime || _updateMinTime == 0)
            {
                _updateMinTime = tillTime;
            }

            _updateTimer.Add(tillTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void KcpUpdate()
        {
            var nowTime = TimeNow;
            
            try
            {
                _kcp.Update(nowTime);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
                
            AddToUpdate(_kcp.Check(nowTime));
        }

        #endregion
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Input(byte[] bytes, ref int size)
        {
            _kcp.Input(bytes, 5, size);
            AddToUpdate(0);

            while (!IsDisposed)
            {
                try
                {
                    var peekSize = _kcp.PeekSize();

                    switch (peekSize)
                    {
                        case < 0:
                        {
                            return;
                        }
                        case 0:
                        {
                            Log.Error($"SocketError.NetworkReset peekSize:{peekSize}");
                            return;
                        }
                    }

                    var receiveCount = _kcp.Receive(_receiveBuffer, peekSize);

                    if (receiveCount != peekSize)
                    {
                        Log.Error($"receiveCount != peekSize receiveCount:{receiveCount} peekSize:{peekSize}");
                        return;
                    }
                    
                    OnReceiveCompleted.Invoke(_receiveBuffer, ref receiveCount);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint CreateChannelId()
        {
            return 0xC0000000 | (uint)new Random().Next();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendHeader(KcpHeader kcpHeader)
        {
            var buff = new byte[5];
            buff.WriteTo(0, (byte)kcpHeader);
            buff.WriteTo(1, ChannelId);
            _socket.Send(buff, 5, SocketFlags.None);
        }

        private void ClearConnectTimeout()
        {
            if (_connectTimeoutId == 0)
            {
                return;
            }

            _scene.ThreadSynchronizationContext.Post(() =>
            {
                _scene.TimerComponent.Core.Remove(ref _connectTimeoutId);
            });
        }

        private void KcpSpanCallback(byte[] buffer, ref int count)
        {
            if (IsDisposed)
            {
                return;
            }

            if (count == 0)
            {
                throw new Exception("KcpOutput count 0");
            }

            buffer[0] = (byte)KcpHeader.ReceiveData;
            buffer[1] = _channelIdBytes[0];
            buffer[2] = _channelIdBytes[1];
            buffer[3] = _channelIdBytes[2];
            buffer[4] = _channelIdBytes[3];
            _socket.Send(buffer, 0, count + 5, SocketFlags.None);
        }
    }
}