using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using LegendProtocol;
using LegendServerAC.Core;
using LegendServerAC.Distributed;

namespace LegendServerAC.Authority
{
    public class AuthorityMsgProxy : ServerMsgProxy
    {
        private AuthorityMain main;

        public AuthorityMsgProxy(AuthorityMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnReqSetUserAuthority(int peerId, bool inbound, object msg)
        {
            RequestSetUserAuthority_B2A reqMsg_B2A = msg as RequestSetUserAuthority_B2A;

            InboundClientSession requester = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestSetUserAuthority_A2D reqMsg_A2D = new RequestSetUserAuthority_A2D();
            reqMsg_A2D.mySelfUserId = requester.userId;
            reqMsg_A2D.acPeerId = peerId;
            reqMsg_A2D.guid = reqMsg_B2A.guid;
            reqMsg_A2D.auth = reqMsg_B2A.auth;
            reqMsg_A2D.lockTime = reqMsg_B2A.lockTime;
            SendDBMsg(reqMsg_A2D);
        }
        
        public void OnReplySetUserAuthority(int peerId, bool inbound, object msg)
        {
            ReplySetUserAuthority_D2A replyMsg_D2A = msg as ReplySetUserAuthority_D2A;

            InboundClientSession requester = SessionManager.Instance.GetInboundClientSessionByPeerId(replyMsg_D2A.acPeerId);
            if (requester != null)
            {
                if (replyMsg_D2A.result == ResultCode.OK)
                {
                    requester.auth = replyMsg_D2A.userInfo.auth;
                }
                ReplySetUserAuthority_A2B replyMsg_A2B = new ReplySetUserAuthority_A2B();
                replyMsg_A2B.userInfo = replyMsg_D2A.userInfo;
                replyMsg_A2B.result = replyMsg_D2A.result;
                SendClientMsg(replyMsg_D2A.acPeerId, replyMsg_A2B);
            }
        }
        public void OnReqGetAllSpecificUser(int peerId, bool inbound, object msg)
        {
            RequestGetAllSpecificUser_B2A reqMsg_B2A = msg as RequestGetAllSpecificUser_B2A;

            InboundClientSession requester = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestGetAllSpecificUser_A2D reqMsg_A2D = new RequestGetAllSpecificUser_A2D();
            reqMsg_A2D.acPeerId = peerId;
            reqMsg_A2D.userId = requester.userId;
            SendDBMsg(reqMsg_A2D);
        }
        public void OnReplyGetAllSpecificUser(int peerId, bool inbound, object msg)
        {
            ReplyGetAllSpecificUser_D2A replyMsg_D2A = msg as ReplyGetAllSpecificUser_D2A;

            InboundClientSession requester = SessionManager.Instance.GetInboundClientSessionByPeerId(replyMsg_D2A.acPeerId);
            if (requester != null)
            {
                ReplyGetAllSpecificUser_A2B reqMsg_A2B = new ReplyGetAllSpecificUser_A2B();
                reqMsg_A2B.allSpecificUser.AddRange(replyMsg_D2A.allSpecificUser);
                SendClientMsg(replyMsg_D2A.acPeerId, reqMsg_A2B);
            }
        }
    }
}

