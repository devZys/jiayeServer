using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace LegendServerAC.Login
{
    public class LoginMain : Module
    {
        public LoginMsgProxy msg_proxy;

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
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.T2A_RequestLogin, new MsgComponent(msg_proxy.OnReqLogin, typeof(RequestLogin_T2A)));
            MsgFactory.Regist(MsgID.P2A_NotifyLoginData, new MsgComponent(msg_proxy.OnNotifyLoginData, typeof(NotifyLoginData_P2A)));
            MsgFactory.Regist(MsgID.D2A_ReplyLogin, new MsgComponent(msg_proxy.OnDBReplyLogin, typeof(ReplyLogin_D2A)));
            MsgFactory.Regist(MsgID.C2A_NotifyServerClosed, new MsgComponent(msg_proxy.OnServerClosed, typeof(NotifyServerClosed_C2A)));
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
        public bool CheckOpenRootLogin()
        {
            //是否开启root帐号登陆
            bool bOpenRootLogin = false;
            SystemConfigDB systemConfig = NHibernateHelper.DirectGetSingleRecordByCondition<SystemConfigDB>(element => element.key == "openRootLogin");
            if (systemConfig != null && !string.IsNullOrEmpty(systemConfig.value))
            {
                bool.TryParse(systemConfig.value, out bOpenRootLogin);
            }
            return bOpenRootLogin;
        }
        public bool CheckOpenBeginLogin()
        {
            //是否开启帐号登陆
            bool bOpenBeginLogin = false;
            SystemConfigDB systemConfig = NHibernateHelper.DirectGetSingleRecordByCondition<SystemConfigDB>(element => element.key == "openBeginLogin");
            if (systemConfig != null && !string.IsNullOrEmpty(systemConfig.value))
            {
                bool.TryParse(systemConfig.value, out bOpenBeginLogin);
            }
            return bOpenBeginLogin;
        }
    }
}