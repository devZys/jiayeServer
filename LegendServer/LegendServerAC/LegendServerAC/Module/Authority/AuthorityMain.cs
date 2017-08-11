using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendProtocol;
using LegendServer.Util;

namespace LegendServerAC.Authority
{
    public class AuthorityMain : Module
    {
        public AuthorityMsgProxy msg_proxy;        

        public AuthorityMain(object root) 
            : base(root)
        {
        }

        public override void OnCreate()
        {
            msg_proxy = new AuthorityMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.B2A_RequestSetUserAuthority, new MsgComponent(msg_proxy.OnReqSetUserAuthority, typeof(RequestSetUserAuthority_B2A)));
            MsgFactory.Regist(MsgID.D2A_ReplySetUserAuthority, new MsgComponent(msg_proxy.OnReplySetUserAuthority, typeof(ReplySetUserAuthority_D2A)));
            MsgFactory.Regist(MsgID.B2A_RequestGetAllSpecificUser, new MsgComponent(msg_proxy.OnReqGetAllSpecificUser, typeof(RequestGetAllSpecificUser_B2A)));
            MsgFactory.Regist(MsgID.D2A_ReplyGetAllSpecificUser, new MsgComponent(msg_proxy.OnReplyGetAllSpecificUser, typeof(ReplyGetAllSpecificUser_D2A)));
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