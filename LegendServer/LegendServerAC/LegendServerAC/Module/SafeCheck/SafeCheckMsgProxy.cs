namespace LegendServerAC.SafeCheck
{
    public class SafeCheckMsgProxy : ServerMsgProxy
    {
        private SafeCheckMain main;

        public SafeCheckMsgProxy(SafeCheckMain main)
            : base(main.root)
        {
            this.main = main;
        }
    }
}

