using System.Collections.Generic;
using LegendProtocol;
using System;
using System.Linq;
using LegendServer.Database.Config;
using LegendServer.Database;
using LegendServer.Database.Summoner;
using System.Text;

namespace LegendServerDB.Login
{
    public class LoginMain : Module
    {
        public LoginMsgProxy msg_proxy;
        public int initRoomCard;
        public string defaultHeadIcon = "";
        public LoginMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new LoginMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
            //初始值房卡
            initRoomCard = 10;
            ServerConfigDB serverConfig = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(element => element.key == "InitRoomCard");
            if (serverConfig != null && !string.IsNullOrEmpty(serverConfig.value))
            {
                int.TryParse(serverConfig.value, out initRoomCard);
            }
            //初始头像
            SystemConfigDB headIconCfg = DBManager<SystemConfigDB>.Instance.GetSingleRecordInCache(e => e.key == "defaultHeadIconURL");
            if (headIconCfg != null)
            {
                defaultHeadIcon = headIconCfg.value;
            }
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.A2D_RequestLogin, new MsgComponent(msg_proxy.OnReqLogin, typeof(RequestLogin_A2D)));
            MsgFactory.Regist(MsgID.X2X_ReplyUID, new MsgComponent(msg_proxy.OnReplyUID, typeof(ReplyUID_X2X)));
            MsgFactory.Regist(MsgID.W2D_NotifyCreateCompetition, new MsgComponent(msg_proxy.OnNotifyCreateCompetition, typeof(NotifyCreateCompetition_W2D)));
            MsgFactory.Regist(MsgID.W2D_NotifyDelCompetition, new MsgComponent(msg_proxy.OnNotifyDelCompetition, typeof(NotifyDelCompetition_W2D)));
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
        public string GetDefaultEncodingString(string srcStr)
        {
            return Encoding.Default.GetString(Encoding.Convert(Encoding.UTF8, Encoding.Default, Encoding.UTF8.GetBytes(srcStr)));
        }
    }

}