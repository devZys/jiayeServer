using LegendProtocol;

namespace LegendServerAC.ServiceBox
{
    public class ServiceBoxMain : Module
    {
        public ServiceBoxMsgProxy msg_proxy;
        public ServiceBoxMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new ServiceBoxMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.B2A_RequestShowRunningDBCache, new MsgComponent(msg_proxy.OnRequestShowRunningDBCache, typeof(RequestShowRunningDBCache_B2A)));
            MsgFactory.Regist(MsgID.X2A_ReplyShowRunningDBCache, new MsgComponent(msg_proxy.OnReplyShowRunningDBCache, typeof(ReplyShowRunningDBCache_X2A)));
        }
        public override void OnRegistTimer()
        {
        }
        public override void OnStart()
        {
        }
        public override void OnDestroy()
        {
        }
    }

}