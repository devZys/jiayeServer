using System;

namespace LegendServerCenter.Core
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
}
