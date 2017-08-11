using System;
using NLog;

namespace LegendServerLogic.Core
{
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
