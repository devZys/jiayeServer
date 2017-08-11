using System.Linq;
using System.Collections.Concurrent;
using LegendProtocol;
using LegendServerAC.Distributed;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;

namespace LegendServerAC.Core
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
        public void Init() {}
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
        public ConcurrentDictionary<int, OutboundSession> GetOutboundSessionCollection()
        {
            return outboundSessionCollection;
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
        //新增客户端入境会话
        public void AddInboundClientSession(int peerId, InboundClientSession session)
        {
            if (!inboundClientSessionCollection.TryAdd(peerId, session))
            {
                ObjectPoolManager<InboundClientSession>.Instance.FreeObject(session);
            }
        }
        //移除客户端入境会话
        public void RemoveInboundClientSession(int peerId)
        {
            InboundClientSession session = null; 
            if (inboundClientSessionCollection.TryRemove(peerId, out session))
            {
                if (session != null)
                {
                    if(session.logicId > 0)
                    {
                        //只有已被center分配登陆的才会通知center减CCU，因为强制关掉客户端的瞬间会自动重连成功然后再断线掉，但是未走正常登陆流程未在center累加CCU，所以不能在此通知减CCU
                        ModuleManager.Get<DistributedMain>().UpdateLoadBlanceStatus(false);
                    }

                    session.status = SessionStatus.Disconnect;
                    ObjectPoolManager<InboundClientSession>.Instance.FreeObject(session);
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
        //通过peerId获取客户端入境会话
        public InboundClientSession GetInboundClientSessionByPeerId(int peerId)
        {
            InboundClientSession session;
            inboundClientSessionCollection.TryGetValue(peerId, out session);
            return session;
        }

        //通过用户ID获取客户端玩家入境会话
        public InboundClientSession GetInboundClientSessionByUserId(string userId)
        {
            if (inboundClientSessionCollection.Values.Count <= 0) return null;

            InboundClientSession session = inboundClientSessionCollection.Values.FirstOrDefault(element => element.userId == userId);
            return session;
        }
        //通过用户ID获取客户端玩家入境会话（按连接时间降序）
        public List<InboundClientSession> GetAllInboundClientSessionByUserId(string userId)
        {
            List<InboundClientSession> result = new List<InboundClientSession>();
            if (inboundClientSessionCollection.Values.Count <= 0) return result;

            result = inboundClientSessionCollection.Values.Where(e => e.userId == userId).OrderByDescending(v => v.connectedTime).ToList();
            return result;
        }

        //通过peerId获取服务器入境会话
        public InboundServerSession GetInboundServerSessionByPeerId(int peerId)
        {
            InboundServerSession session;
            inboundServerSessionCollection.TryGetValue(peerId, out session);
            return session;
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
        //获取主动去连接过的服务器会话
        public List<OutboundSession> GetAllOutboundSession(string server)
        {
            return outboundSessionCollection.Values.Where(element => element.serverName == server).ToList();
        }


        //踢调所有用户（除root系统用户外）
        public void KickOutAllClient()
        {
            foreach (InboundClientSession session in inboundClientSessionCollection.Values)
            {
                if (session != null && session.peer != null && session.auth != UserAuthority.Root)
                {
                    session.peer.Disconnect();
                    RemoveInboundClientSession(session.peer.ConnectionId);
                }
            }
        }
        //主动与某个主动连接过的服务器断开连接
        public void DisconnectOutboundServer(string serverName, int serverID)
        {
            OutboundSession session = GetOutboundSession(serverName, serverID);
            if (session != null && session.peer != null)
            {
                session.peer.Disconnect();
            }
        }
        //与所有网关断开
        public void DisconnectAllProxy()
        {
            List<OutboundSession> sessions = GetAllOutboundSession("proxy");
            sessions.ForEach(proxy =>
            {
                if (proxy != null && proxy.peer != null)
                {
                    proxy.peer.Disconnect();
                }
            });
        }
        //与所有DB断开
        public void DisconnectAllDB()
        {
            List<OutboundSession> sessions = GetAllOutboundSession("db");
            sessions.ForEach(db =>
            {
                if (db != null && db.peer != null)
                {
                    db.peer.Disconnect();
                }
            });
        }
        //断开中心服务器
        public void DisconnectCenter()
        {
            DisconnectOutboundServer("center", 1);
        }
        //断开记录服务器
        public void DisconnectRecord()
        {
            DisconnectOutboundServer("record", 1);
        }
        //断开所有玩家
        public void DisconnectAllPlayer()
        {
            foreach(InboundClientSession session in inboundClientSessionCollection.Values)
            {
                if (session != null && session.peer != null)
                {
                    session.peer.Disconnect();
                }
            }
        }

        //断开所有连接进来的逻辑服务器
        public void DisconnectAllLogic()
        {
            foreach (InboundServerSession session in inboundServerSessionCollection.Values)
            {
                if (session != null && session.peer != null && session.serverName == "logic")
                {
                    session.peer.Disconnect();
                }
            }
        }
    }
}
