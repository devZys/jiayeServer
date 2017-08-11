namespace LegendServerProxy.SafeCheck
{
    public class SafeCheckMsgProxy : ServerMsgProxy
    {
        private SafeCheckMain main;

        public SafeCheckMsgProxy(SafeCheckMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnLongHeartBeat(int peerId, bool inbound, object msg)
        {
            //Don't need to deal with.
            return;
        }
    }
}

