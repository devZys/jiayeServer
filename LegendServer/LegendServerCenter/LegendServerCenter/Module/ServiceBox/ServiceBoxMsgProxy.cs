using System;
using System.Collections.Generic;
using LegendProtocol;
using LegendServerCenter.Distributed;

namespace LegendServerCenter.ServiceBox
{
    public class ServiceBoxMsgProxy : ServerMsgProxy
    {
        private ServiceBoxMain main;

        public ServiceBoxMsgProxy(ServiceBoxMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestLoadblanceStatus(int peerId, bool inbound, object msg)
        {
            RequestLoadblanceStatus_P2C reqMsg_P2C = msg as RequestLoadblanceStatus_P2C;

            ReplyLoadblanceStatus_C2P replyMsg = new ReplyLoadblanceStatus_C2P();
            replyMsg.userId = reqMsg_P2C.userId;
            replyMsg.serverType = reqMsg_P2C.serverType;
            List<ServerInfo> serverList = ModuleManager.Get<DistributedMain>().GetServerInfoList(reqMsg_P2C.serverType);
            replyMsg.result.AddRange(serverList);
            SendMsg(peerId, replyMsg);
        }

        public void OnRequestShowRunningDBCaches(int peerId, bool inbound, object msg)
        {
            RequestShowRunningDBCache_A2C reqMsg_A2C = msg as RequestShowRunningDBCache_A2C;

            NotifyShowRunningDBCache_C2X notifyMsg_C2X = new NotifyShowRunningDBCache_C2X();
            notifyMsg_C2X.senderACServerId = reqMsg_A2C.senderACServerId;
            notifyMsg_C2X.senderBoxPeerId = reqMsg_A2C.senderBoxPeerId;
            notifyMsg_C2X.show = reqMsg_A2C.show;

            //广播给所有存在缓存实例入库的服务器
            BroadCastMsg(notifyMsg_C2X, "db");
            BroadCastMsg(notifyMsg_C2X, "record");
        }
    }
}

