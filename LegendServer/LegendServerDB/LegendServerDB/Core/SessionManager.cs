using System.Linq;
using System.Collections.Concurrent;
using LegendProtocol;
using LegendServerDB.Distributed;
using System.Diagnostics;
using System;
using System.Collections.Generic;

namespace LegendServerDB.Core
{
    //网络会话管理器
    public class SessionManager
    {
        private static object singletonLocker = new object();//单例双检锁
        private ConcurrentDictionary<int, OutboundSession> outboundSessionCollection = new ConcurrentDictionary<int, OutboundSession>();
        private ConcurrentDictionary<int, InboundSession> inboundSessionCollection = new ConcurrentDictionary<int, InboundSession>();

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
        public ConcurrentDictionary<int, OutboundSession> GetOutboundSessionCollection()
        {
            return outboundSessionCollection;
        }
        public ConcurrentDictionary<int, InboundSession> GetInboundSessionCollection()
        {
            return inboundSessionCollection;
        }
        //新增出境会话
        public void AddOutboundSession(int peerId, OutboundSession session)
        {
            outboundSessionCollection.TryAdd(peerId, session);

            ServerUtil.RecordLog(LogType.Info, ModuleManager.Get<DistributedMain>().GetMyselfServerId() + " 号 " + ModuleManager.Get<DistributedMain>().GetMyselfServerName() + " 服务器成功连接到" + session.serverID + " 号 " + session.serverName + " !");
        }
        //新增入境会话
        public void AddInboundSession(int peerId, InboundSession session)
        {
            inboundSessionCollection.TryAdd(peerId, session);
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
        public InboundSession GetInboundSession(string serverName, int serverID)
        {
            InboundSession session = inboundSessionCollection.Values.FirstOrDefault(element => (element.serverName == serverName && element.serverID == serverID));
            return session;
        }
        //通过peerId获取入境会话
        public InboundSession GetInboundSessionByPeerId(int peerId)
        {
            InboundSession session;
            inboundSessionCollection.TryGetValue(peerId, out session);
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
        //移除入境会话
        public void RemoveInboundSession(int peerId, string reasonDetail)
        {
            InboundSession session = null;
            inboundSessionCollection.TryRemove(peerId, out session);

            if (session != null)
            {
                ServerUtil.RecordLog(LogType.Fatal, "第 " + session.serverID + " 号 " + session.serverName + " 服务器断开连接! 断开原因：" + reasonDetail, new StackTrace(new StackFrame(true)).GetFrame(0));
            }
        }
        //获取被动连接进来的服务器会话
        public List<InboundSession> GetAllInboundSession(string server)
        {
            return inboundSessionCollection.Values.Where(element => element.serverName == server).ToList();
        }
        //主动与某个服务器断开连接
        public void Disconnect(string serverName, int serverID, bool inbound)
        {
            if (inbound)
            {
                InboundSession session = GetInboundSession(serverName, serverID);
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
        //断开中心服务器
        public void DisconnectCenter()
        {
            Disconnect("center", 1, false);
        }
        //断开所有与自己相连的其他DB服务器（则不再彼此同步）
        public void DisconnectAllDB()
        {
            foreach (InboundSession session in SessionManager.Instance.GetInboundSessionCollection().Values)
            {
                if (session != null && session.peer != null && session.serverName == "db")
                {
                    session.peer.Disconnect();
                }
            }

            foreach (OutboundSession session in SessionManager.Instance.GetOutboundSessionCollection().Values)
            {
                if (session != null && session.peer != null && session.serverName == "db")
                {
                    session.peer.Disconnect();
                }
            }
        }
        //与所有Logic断开
        public void DisconnectAllLogic()
        {
            List<InboundSession> sessions = GetAllInboundSession("logic");
            sessions.ForEach(logic =>
            {
                if (logic != null && logic.peer != null)
                {
                    logic.peer.Disconnect();
                }
            });
        }

        //断开所有AC服务器
        public void DisconnectAllAC()
        {
            List<InboundSession> sessions = GetAllInboundSession("ac");
            sessions.ForEach(ac =>
            {
                if (ac != null && ac.peer != null)
                {
                    ac.peer.Disconnect();
                }
            });
        }
        //断开世界服务器
        public void DisconnectWorld()
        {
            Disconnect("world", 1, true);
        }
        //断开记录服务器
        public void DisconnectRecord()
        {
            Disconnect("record", 1, false);
        }
    }
}
