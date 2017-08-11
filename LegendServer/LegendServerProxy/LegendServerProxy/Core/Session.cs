using System;
using LegendProtocol;

namespace LegendServerProxy.Core
{
    //客户端入境网络会话
    public class InboundClientSession : ObjectBase
    {
        public string ip;
        public string userId;
        public int acServerID;
        public int logicServerID;
        public InboundClientPeer peer;
        public SessionStatus status;
        public DateTime connectedTime;
        public DateTime lastSendChatMsgTime;
        public DateTime lastSendFeedbackMsgTime;
        public int procMsgCountByUnitInterval;
        public int lockedTimeByfrequentAttack;
        public int frequentAttackWarningCount;
        public DateTime lastProcMsgTimeForUnitInterval;
        public DateTime lastRecommendFriendTime;
        public DateTime lastRefreshFriendStatusTime;
        public DateTime lastSendHornContentMsgTime;
        public UserAuthority auth;
        public ulong summonerId;

        public InboundClientSession()
        { }

        public override void Init(params object[] paramList)
        {
            if (paramList.Length < 3) return;

            this.peer = (InboundClientPeer)paramList[0];
            this.ip = (string)paramList[1];

            this.acServerID = 1;
            string[] remoteParam = ((string)paramList[2]).Split('.');
            if (remoteParam.Length > 1)
            {
                int.TryParse(remoteParam[1], out this.acServerID);
            }

            this.userId = "";
            this.logicServerID = 0;
            connectedTime = DateTime.Now;
            status = SessionStatus.Connected;
            lastRecommendFriendTime = new DateTime(DateTime.MinValue.Ticks);
            lastRefreshFriendStatusTime = new DateTime(DateTime.MinValue.Ticks);
            lastSendChatMsgTime = new DateTime(DateTime.MinValue.Ticks);
            lastSendFeedbackMsgTime = new DateTime(DateTime.MinValue.Ticks);
            lastSendHornContentMsgTime = new DateTime(DateTime.MinValue.Ticks);

            procMsgCountByUnitInterval = 0;
            lockedTimeByfrequentAttack = 0;
            frequentAttackWarningCount = 0;
            lastProcMsgTimeForUnitInterval = new DateTime(DateTime.MinValue.Ticks);

            auth = UserAuthority.Guest;
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
