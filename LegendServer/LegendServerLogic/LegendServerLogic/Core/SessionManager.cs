using System.Linq;
using System.Collections.Concurrent;
using LegendProtocol;
using LegendServerLogic.Distributed;
using System.Diagnostics;
using System.Collections.Generic;

namespace LegendServerLogic.Core
{
    //网络会话管理器
    public class SessionManager
    {
        private static object singletonLocker = new object();//单例双检锁
        private ConcurrentDictionary<int, OutboundSession> outboundSessionCollection = new ConcurrentDictionary<int, OutboundSession>();

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
        public SessionManager() { }
        public void Init() { }
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
        //获取主动去连接过的服务器会话
        public List<OutboundSession> GetAllOutboundSession(string server)
        {
            return outboundSessionCollection.Values.Where(element => element.serverName == server).ToList();
        }
        //移除出境会话
        public void RemoveOutboundSession(string serverName, int serverId, string reasonDetail)
        {
            foreach(var session in outboundSessionCollection)
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
        //主动与某个服务器断开连接
        public void Disconnect(string serverName, int serverID)
        {
            OutboundSession session = GetOutboundSession(serverName, serverID);
            if (session != null && session.peer != null)
            {
                session.peer.Disconnect();
            }
        }
        //断开中心服务器
        public void DisconnectCenter()
        {
            Disconnect("center", 1);
        }
        //断开日志服务器
        public void DisconnectRecord()
        {
            Disconnect("record", 1);
        }
        //断开世界服务器
        public void DisconnectWorld()
        {
            Disconnect("world", 1);
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
        //与所有认证服断开
        public void DisconnectAllAC()
        {
            List<OutboundSession> sessions = GetAllOutboundSession("ac");
            sessions.ForEach(ac =>
            {
                if (ac != null && ac.peer != null)
                {
                    ac.peer.Disconnect();
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
    }
}
