using LegendProtocol;

namespace LegendServerCenter.ServiceBox
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
            MsgFactory.Regist(MsgID.P2C_RequestLoadblanceStatus, new MsgComponent(msg_proxy.OnRequestLoadblanceStatus, typeof(RequestLoadblanceStatus_P2C)));
            MsgFactory.Regist(MsgID.A2C_RequestShowRunningDBCache, new MsgComponent(msg_proxy.OnRequestShowRunningDBCaches, typeof(RequestShowRunningDBCache_A2C)));
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