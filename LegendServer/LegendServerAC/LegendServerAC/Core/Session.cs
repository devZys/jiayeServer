using System;
using LegendProtocol;
using System.Net.Http;
using System.Net;

namespace LegendServerAC.Core
{
    //客户端入境网络会话
    public class InboundClientSession : ObjectBase
    {
        public InboundClientPeer peer;
        public string ip;
        public TerminalOS os;
        public TerminalLoginSDK loginSDK;
        public string userId;
        public int logicId;
        public int procMsgCountByUnitInterval;
        public int lockedTimeByfrequentAttack;
        public int frequentAttackWarningCount;
        public DateTime lastProcMsgTimeForUnitInterval;
        public DateTime connectedTime;
        public UserAuthority auth;
        public SessionStatus status;
        public DateTime lastReqPayMsgTime;

        public InboundClientSession()
        { }
        public override void Init(params object[] paramList)
        {
            if (paramList.Length < 2) return;

            this.peer = (InboundClientPeer)paramList[0];
            this.ip = (string)paramList[1];

            userId = "";
            logicId = 0;
            procMsgCountByUnitInterval = 0;
            lockedTimeByfrequentAttack = 0;
            frequentAttackWarningCount = 0;
            lastProcMsgTimeForUnitInterval = new DateTime(DateTime.MinValue.Ticks);
            connectedTime = DateTime.Now;
            os = TerminalOS.Android;
            loginSDK = TerminalLoginSDK.SDK_Visitor;
            auth = UserAuthority.Guest;
            status = SessionStatus.Connected;
            lastReqPayMsgTime = new DateTime(DateTime.MinValue.Ticks);
        }
    }

    //服务器入境网络会话
    public class InboundServerSession
    {
        public string serverName;
        public int serverID;
        public InboundServerPeer peer;
        public DateTime connectedTime;

        public InboundServerSession(InboundServerPeer peer, string serverName, int serverID)
        {
            this.serverName = serverName;
            this.serverID = serverID;
            this.peer = peer;
            this.connectedTime = DateTime.Now;
        }
    }

    //出境网络会话
    public class OutboundSession
    {
        public string serverName;
        public int serverID;
        public OutboundPeer peer;
        public DateTime connectedTime;
        public OutboundSession(OutboundPeer outboundPeer, string serverName, int serverID)
        {
            this.serverName = serverName;
            this.serverID = serverID;
            this.peer = outboundPeer;
            this.connectedTime = DateTime.Now;
        }
    }
}
