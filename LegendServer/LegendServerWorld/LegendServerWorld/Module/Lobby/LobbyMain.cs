using System;
using LegendProtocol;
using LegendServer.Util;
using LegendServerWorldDefine;

namespace LegendServerWorld.Lobby
{
    public class LobbyMain : Module
    {
        public LobbyMsgProxy msg_proxy;

        public LobbyMain(object root) 
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new LobbyMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.L2W_EnterWorld, new MsgComponent(msg_proxy.OnEnterWorld, typeof(EnterWorld_L2W)));
            MsgFactory.Regist(MsgID.L2W_LeaveWorld, new MsgComponent(msg_proxy.OnLeaveWorld, typeof(LeaveWorld_L2W)));
            MsgFactory.Regist(MsgID.X2X_NotifyGameServerSendGoods, new MsgComponent(msg_proxy.OnNotifyGameServerSendGoods, typeof(NotifyGameServerSendGoods_X2X)));
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