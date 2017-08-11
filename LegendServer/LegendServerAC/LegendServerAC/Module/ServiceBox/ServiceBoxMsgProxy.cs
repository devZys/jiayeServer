using System.Diagnostics;
using LegendProtocol;
using LegendServer.Database;
using System.Collections.Generic;
using LegendServerAC.Core;
using LegendServerAC.Distributed;
using System;

namespace LegendServerAC.ServiceBox
{
    public class ServiceBoxMsgProxy : ServerMsgProxy
    {
        private ServiceBoxMain main;

        public ServiceBoxMsgProxy(ServiceBoxMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestShowRunningDBCache(int peerId, bool inbound, object msg)
        {
            RequestShowRunningDBCache_B2A reqMsg_B2A = msg as RequestShowRunningDBCache_B2A;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);
            if (session.auth != UserAuthority.Root)
            {
                return;
            }

            RequestShowRunningDBCache_A2C reqMsg_A2C = new RequestShowRunningDBCache_A2C();
            reqMsg_A2C.senderACServerId = root.ServerID;
            reqMsg_A2C.senderBoxPeerId = peerId;
            reqMsg_A2C.show = reqMsg_B2A.show;
            SendCenterMsg(reqMsg_A2C);
        }

        public void OnReplyShowRunningDBCache(int peerId, bool inbound, object msg)
        {
            ReplyShowRunningDBCache_X2A replyMsg_X2A = msg as ReplyShowRunningDBCache_X2A;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(replyMsg_X2A.senderBoxPeerId);
            if (session == null || session.peer == null || session.status == SessionStatus.Disconnect) return;

            ReplyShowRunningDBCache_A2B replyMsg_A2B = new ReplyShowRunningDBCache_A2B();
            replyMsg_A2B.dbCacheInstance = replyMsg_X2A.dbCacheInstance;
            replyMsg_A2B.show = replyMsg_X2A.show;
            replyMsg_A2B.fromServerName = replyMsg_X2A.fromServerName;
            SendClientMsg(replyMsg_X2A.senderBoxPeerId, replyMsg_A2B);
        }
    }
}

