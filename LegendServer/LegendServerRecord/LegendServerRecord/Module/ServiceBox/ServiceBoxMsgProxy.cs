using System.Diagnostics;
using LegendProtocol;
using LegendServer.Database;
using LegendServerRecord.Core;
using System.Collections.Generic;
using LegendServerRecord.Distributed;
using System;

namespace LegendServerRecord.ServiceBox
{
    public class ServiceBoxMsgProxy : ServerMsgProxy
    {
        private ServiceBoxMain main;

        public ServiceBoxMsgProxy(ServiceBoxMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnNotifyShowRunningDBCache(int peerId, bool inbound, object msg)
        {
            NotifyShowRunningDBCache_C2X notifyMsg = msg as NotifyShowRunningDBCache_C2X;

            NHibernateHelper.RunningCacheSender.show = notifyMsg.show;
            NHibernateHelper.RunningCacheSender.acServerId = notifyMsg.senderACServerId;
            NHibernateHelper.RunningCacheSender.boxPeerId = notifyMsg.senderBoxPeerId;
        }

        public void SendShowRunningDBCache(string dbCacheInstance)
        {
            ReplyShowRunningDBCache_X2A replyMsg = new ReplyShowRunningDBCache_X2A();

            if (string.IsNullOrEmpty(dbCacheInstance))
            {
                replyMsg.dbCacheInstance = "服务器内部错误!【未识别的数据库操作】";
                replyMsg.senderBoxPeerId = NHibernateHelper.RunningCacheSender.boxPeerId;
                SendACMsg(replyMsg, NHibernateHelper.RunningCacheSender.acServerId);
                return;
            }
           
            replyMsg.senderBoxPeerId = NHibernateHelper.RunningCacheSender.boxPeerId;
            replyMsg.dbCacheInstance = dbCacheInstance;
            replyMsg.show = true;
            replyMsg.fromServerName = root.ServerID + "号 记录服务器";
            SendACMsg(replyMsg, NHibernateHelper.RunningCacheSender.acServerId);
        }
    }
}

