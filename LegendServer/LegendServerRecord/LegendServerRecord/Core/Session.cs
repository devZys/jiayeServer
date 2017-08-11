using System;

namespace LegendServerRecord.Core
{
    //入境网络会话
    public class InboundSession
    {
        public string serverName;
        public int serverID;
        public InboundPeer peer;
        public DateTime connectedTime;

        public InboundSession(InboundPeer peer, string serverName, int serverID)
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
