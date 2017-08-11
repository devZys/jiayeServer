using System.Linq;
using System.Collections.Concurrent;
using LegendProtocol;
using LegendServerCenter.Distributed;
using System.Diagnostics;

namespace LegendServerCenter.Core
{
    //网络会话管理器
    public class SessionManager
    {
        private static object singletonLocker = new object();//单例双检锁
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
        public ConcurrentDictionary<int, InboundSession> GetInboundSessionCollection()
        {
            return inboundSessionCollection;
        }
        //新增会话
        public void AddInboundSession(int peerId, InboundSession session)
        {
            inboundSessionCollection.TryAdd(peerId, session);
        }
        //通过peerId获取会话
        public InboundSession GetInboundSessionByPeerId(int peerId)
        {
            InboundSession session;
            inboundSessionCollection.TryGetValue(peerId, out session);
            return session;
        }
        //获取指定服务器的会话(服务器=服务器名+服务器ID号)
        public InboundSession GetInboundSession(string serverName, int serverID)
        {
            InboundSession session = inboundSessionCollection.Values.FirstOrDefault(element => (element.serverName == serverName && element.serverID == serverID));            
            return session;
        }
        //移除会话
        public void RemoveInboundSession(int peerId, string reasonDetail)
        {
            InboundSession session = null;
            inboundSessionCollection.TryRemove(peerId, out session);

            if (session != null)
            {
                string myselfServerName = ModuleManager.Get<DistributedMain>().GetMyselfServerName();
                int myselfServerId = ModuleManager.Get<DistributedMain>().GetMyselfServerId();

                ServerUtil.RecordLog(LogType.Fatal, "第 " + session.serverID + " 号 " + session.serverName + " 服务器断开连接! 断开原因：" + reasonDetail, new StackTrace(new StackFrame(true)).GetFrame(0));

                ModuleManager.Get<DistributedMain>().SetServerStatus(session.serverName, session.serverID, ServerInternalStatus.Closed);
            }
        }        
    }
}
