using System.Linq;
using System.Collections.Concurrent;
using LegendProtocol;
using LegendServerProxy.Distributed;
using LegendServerProxy.MainCity;
using System.Diagnostics;
using System;
using System.Collections.Generic;

namespace LegendServerProxy.Core
{
    //网络会话管理器
    public class SessionManager
    {
        private static object singletonLocker = new object();//单例双检锁

        private bool servicePause = false;
        private ConcurrentDictionary<int, OutboundSession> outboundSessionCollection = new ConcurrentDictionary<int, OutboundSession>();
        private ConcurrentDictionary<int, InboundClientSession> inboundClientSessionCollection = new ConcurrentDictionary<int, InboundClientSession>();
        private ConcurrentDictionary<int, InboundServerSession> inboundServerSessionCollection = new ConcurrentDictionary<int, InboundServerSession>();

        private static SessionManager instance = null;
        public static SessionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new SessionManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }
        public SessionManager() {}
        public void Init()
        {
        }
        public bool ServicePause
        {
            get
            {
                return servicePause;
            }
            set
            {
                servicePause = value;
            }
        }
        public ConcurrentDictionary<int, InboundClientSession> GetInboundClientSessionCollection()
        {
            return inboundClientSessionCollection;
        }
        //获取真实CCU（供后台工具获取的精确CCU）
        public int GetActualCCU()
        {
            return inboundClientSessionCollection.Values.Count(e => e.peer != null && e.peer.ConnectionState == Photon.SocketServer.ConnectionState.Connected);
        }
        //新增客户端玩家入境会话
        public void AddInboundClientSession(int peerId, InboundClientSession session)
        {
            if (!inboundClientSessionCollection.TryAdd(peerId, session))
            {
                ObjectPoolManager<InboundClientSession>.Instance.FreeObject(session);
            }
        }
        //新增服务器入境会话
        public void AddInboundServerSession(int peerId, InboundServerSession session)
        {
            inboundServerSessionCollection.TryAdd(peerId, session);
        }
        //新增出境会话
        public void AddOutboundSession(int peerId, OutboundSession session)
        {
            outboundSessionCollection.TryAdd(peerId, session);

            ServerUtil.RecordLog(LogType.Info, ModuleManager.Get<DistributedMain>().GetMyselfServerId() + " 号 " + ModuleManager.Get<DistributedMain>().GetMyselfServerName() + " 服务器成功连接到" + session.serverID + " 号 " + session.serverName + " !");
        }
        //通过peerId获取出境会话
        public OutboundSession GetOutboundSessionByPeerId(int peerId)
        {
            OutboundSession session;
            outboundSessionCollection.TryGetValue(peerId, out session);
            return session;
        }
        //获取指定主动连接的服务器的会话(服务器=服务器名+服务器ID号)
        public OutboundSession GetOutboundSession(string serverName, int serverID)
        {
            OutboundSession session = outboundSessionCollection.Values.FirstOrDefault(element => (element.serverName == serverName && element.serverID == serverID));            
            return session;
        }
        //获取指定被动连接的服务器的会话(服务器=服务器名+服务器ID号)
        public InboundServerSession GetInboundServerSession(string serverName, int serverID)
        {
            InboundServerSession session = inboundServerSessionCollection.Values.FirstOrDefault(element => (element.serverName == serverName && element.serverID == serverID));
            return session;
        }
        //通过peerId获取客户端玩家入境会话
        public InboundClientSession GetInboundClientSessionByPeerId(int peerId)
        {
            InboundClientSession session;
            inboundClientSessionCollection.TryGetValue(peerId, out session);
            return session;
        }
        //通过peerId获取服务器入境会话
        public InboundServerSession GetInboundServerSessionByPeerId(int peerId)
        {
            InboundServerSession session;
            inboundServerSessionCollection.TryGetValue(peerId, out session);
            return session;
        }
        //通过用户ID获取客户端玩家入境会话
        public InboundClientSession GetInboundClientSessionByUserId(string userId)
        {
            if (inboundClientSessionCollection.Values.Count <= 0) return null;

            InboundClientSession session = inboundClientSessionCollection.Values.FirstOrDefault(element => element.userId == userId);
            return session;
        }
        //通过用户ID获取客户端玩家入境会话（结果必须是排除指定peerId的）
        public InboundClientSession GetInboundClientSessionByUserId(string userId, int excludePeerId)
        {
            if (inboundClientSessionCollection.Values.Count <= 0) return null;

            InboundClientSession session = inboundClientSessionCollection.Values.FirstOrDefault(element => element.userId == userId && element.peer != null && element.peer.ConnectionId != excludePeerId);
            return session;
        }

        //通过用户ID获取客户端玩家入境会话
        public InboundClientSession GetInboundClientSessionBySummonerId(ulong summonerId)
        {
            if (inboundClientSessionCollection.Values.Count <= 0) return null;

            InboundClientSession session = inboundClientSessionCollection.Values.FirstOrDefault(element => element.summonerId == summonerId);
            return session;
        }
        //踢调指定用户ID的客户端
        public void KickOutClient(string userId)
        {
            foreach (InboundClientSession session in inboundClientSessionCollection.Values)
            {
                if (session != null && session.peer != null && session.userId == userId)
                {
                    session.peer.Disconnect();
                    SessionManager.Instance.RemoveInboundClientSession(session.peer.ConnectionId, true, false);
                }
            }
        }
        //踢调所有用户（除root系统用户外）
        public void KickOutAllClient()
        {
            foreach (InboundClientSession session in inboundClientSessionCollection.Values)
            {
                if (session != null && session.peer != null && session.auth != UserAuthority.Root)
                {
                    session.peer.Disconnect();
                    SessionManager.Instance.RemoveInboundClientSession(session.peer.ConnectionId, true, false);
                }
            }
        }
        //移除出境会话
        public void RemoveOutboundSession(string serverName, int serverId, string reasonDetail)
        {
            foreach (var session in outboundSessionCollection)
            {
                if (session.Value != null && session.Value.serverName == serverName && session.Value.serverID == serverId)
                {
                    OutboundSession removeSession = null;
                    outboundSessionCollection.TryRemove(session.Key, out removeSession);
                    if (removeSession != null)
                    {
                        ServerUtil.RecordLog(LogType.Fatal, "第 " + removeSession.serverID + " 号 " + removeSession.serverName + " 服务器断开连接! 断开原因：" + reasonDetail, new StackTrace(new StackFrame(true)).GetFrame(0));
                    }
                }
            }
        }
        //移除客户端玩家入境会话
        public void RemoveInboundClientSession(int peerId, bool notifyLogic, bool byPlaceOtherLogin)
        {
            InboundClientSession session = null;

            if (inboundClientSessionCollection.TryRemove(peerId, out session))
            {
                if (session != null)
                {
                    if (session.logicServerID > 0)
                    {
                        //只有已被center分配登陆的才会通知center减CCU，因为强制关掉客户端的瞬间会自动重连成功然后再断线掉，但是未走正常登陆流程未在center累加CCU，所以不能在此通知减CCU
                        ModuleManager.Get<DistributedMain>().UpdateLoadBlanceStatus(false);
                    }

                    if (notifyLogic)
                    {
                        ModuleManager.Get<MainCityMain>().LogicServerPlayerLogout(session.logicServerID, session.userId, byPlaceOtherLogin);
                    }

                    session.status = SessionStatus.Disconnect;
                    ObjectPoolManager<InboundClientSession>.Instance.FreeObject(session);

                    ServerUtil.RecordLog(LogType.Debug, "玩家：" + session.userId + " 退出网关! " + (!byPlaceOtherLogin ? "[主动或者断线]" : "[异地登陆]"));
                }
            }
        }
        //移除服务器端入境会话
        public void RemoveInboundServerSession(int peerId, string reasonDetail)
        {
            InboundServerSession session = null;
            inboundServerSessionCollection.TryRemove(peerId, out session);

            if (session != null)
            {
                ServerUtil.RecordLog(LogType.Fatal, "第 " + session.serverID + " 号 " + session.serverName + " 服务器断开连接! 断开原因：" + reasonDetail, new StackTrace(new StackFrame(true)).GetFrame(0));
            }
        }
        //获取所有被动连接进来的服务器会话
        public List<InboundServerSession> GetAllInboundServerSession(string server)
        {
            return inboundServerSessionCollection.Values.Where(element => element.serverName == server).ToList();
        }
        //断开中心服务器
        public void DisconnectCenter()
        {
            Disconnect("center", 1, false);
        }
        //断开日志记录服
        public void DisconnectRecord()
        {
            Disconnect("record", 1, false);
        }
        //断开世界服
        public void DisconnectWorld()
        {
            Disconnect("world", 1, true);
        }

        //断开所有玩家
        public void DisconnectAllPlayer()
        {
            foreach (InboundClientSession session in inboundClientSessionCollection.Values)
            {
                if (session != null && session.peer != null)
                {
                    session.peer.Disconnect();
                }
            }
        }
        //断开所有认证服
        public void DisconnectAllAC()
        {
            List<InboundServerSession> sessions = GetAllInboundServerSession("ac");
            sessions.ForEach(ac =>
            {
                if (ac != null && ac.peer != null)
                {
                    ac.peer.Disconnect();
                }
            });
        }
        //断开所有逻辑服
        public void DisconnectAllLogic()
        {
            List<InboundServerSession> sessions = GetAllInboundServerSession("logic");
            sessions.ForEach(logic =>
            {
                if (logic != null && logic.peer != null)
                {
                    logic.peer.Disconnect();
                }
            });
        }
        //主动与某个服务器断开连接
        public void Disconnect(string serverName, int serverID, bool inbound)
        {
            if (inbound)
            {
                InboundServerSession session = GetInboundServerSession(serverName, serverID);
                if (session != null && session.peer != null)
                {
                    session.peer.Disconnect();
                }
            }
            else
            {
                OutboundSession session = GetOutboundSession(serverName, serverID);
                if (session != null && session.peer != null)
                {
                    session.peer.Disconnect();
                }
            }
        }
    }
}
