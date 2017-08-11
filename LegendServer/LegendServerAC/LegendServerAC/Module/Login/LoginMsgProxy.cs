using LegendProtocol;
using LegendServerAC.Core;
using LegendServerAC.Distributed;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LegendServerAC.Login
{
    public class LoginMsgProxy : ServerMsgProxy
    {
        private LoginMain main;

        public LoginMsgProxy(LoginMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnReqLogin(int peerId, bool inbound, object msg)
        {
            RequestLogin_T2A reqMsg_T2A = msg as RequestLogin_T2A;

            if (string.IsNullOrEmpty(reqMsg_T2A.userId) || reqMsg_T2A.nickName == null || reqMsg_T2A.headImgUrl == null)
            {
                ReplyLogin_A2T playerReplyMsg = new ReplyLogin_A2T();
                playerReplyMsg.result = ResultCode.InvalidPlayer;
                playerReplyMsg.unlockTime = "";
                SendClientMsg(peerId, playerReplyMsg);
                return;
            }

            InboundClientSession requester = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);
            requester.os = reqMsg_T2A.os;
            requester.userId = reqMsg_T2A.userId;

            if (!main.CheckOpenBeginLogin())
            {
                //禁止帐号登陆
                ReplyLogin_A2T playerReplyMsg = new ReplyLogin_A2T();
                playerReplyMsg.result = ResultCode.NotLoginedStatus;
                playerReplyMsg.unlockTime = "";
                SendClientMsg(peerId, playerReplyMsg);
                return;
            }

            if (!main.CheckOpenRootLogin() && reqMsg_T2A.userId == "root")
            {
                //禁止root帐号登陆
                ReplyLogin_A2T playerReplyMsg = new ReplyLogin_A2T();
                playerReplyMsg.result = ResultCode.NotLoginedStatus;
                playerReplyMsg.unlockTime = "";
                SendClientMsg(peerId, playerReplyMsg);
                return;
            }

            RequestLogin_A2D reqMsg_A2D = new RequestLogin_A2D();
            reqMsg_A2D.acPeerId = peerId;
            reqMsg_A2D.acId = root.ServerID;
            reqMsg_A2D.requesterIp = requester.ip;
            reqMsg_A2D.userId = requester.userId;
            reqMsg_A2D.nickName = reqMsg_T2A.nickName;
            reqMsg_A2D.headImgUrl = reqMsg_T2A.headImgUrl;
            reqMsg_A2D.sex = reqMsg_T2A.sex;
            SendDBMsg(reqMsg_A2D);            
        }
        public void OnServerClosed(int peerId, bool inbound, object msg)
        {
            NotifyServerClosed_C2A notifyMsg = msg as NotifyServerClosed_C2A;

            ReplyLogin_A2T playerReplyMsg = new ReplyLogin_A2T();
            playerReplyMsg.result = ResultCode.ServerIsClosed;
            SendClientMsg(notifyMsg.acPeerId, playerReplyMsg);
        }
        public void OnDBReplyLogin(int peerId, bool inbound, object msg)
        {
            ReplyLogin_D2A dbReplyMsg = msg as ReplyLogin_D2A;
            InboundClientSession requester = SessionManager.Instance.GetInboundClientSessionByPeerId(dbReplyMsg.acPeerId);
            if (requester != null)
            {
                if (dbReplyMsg.result != ResultCode.OK)
                {
                    ReplyLogin_A2T playerReplyMsg = new ReplyLogin_A2T();
                    playerReplyMsg.result = dbReplyMsg.result;
                    playerReplyMsg.unlockTime = dbReplyMsg.unlockTime;

                    SendClientMsg(dbReplyMsg.acPeerId, playerReplyMsg);
                }
            }
        }
        public void OnNotifyLoginData(int peerId, bool inbound, object msg)
        {
            NotifyLoginData_P2A notifyMsg = msg as NotifyLoginData_P2A;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(notifyMsg.acPeerId);
            if (session != null && session.peer != null && session.peer.Connected)
            {
                session.logicId = notifyMsg.logicId;
                session.auth = notifyMsg.auth;

                ReplyLogin_A2T replyLoginMsg = new ReplyLogin_A2T();
                replyLoginMsg.result = ResultCode.OK;
                replyLoginMsg.accessToken = notifyMsg.accessToken;
                replyLoginMsg.proxyId = notifyMsg.proxyId;
                replyLoginMsg.closedACServerList.AddRange(notifyMsg.closedACServerList);
                replyLoginMsg.auth = notifyMsg.auth;

                SendClientMsg(session.peer.ConnectionId, replyLoginMsg);
            }
        }
    }
}

