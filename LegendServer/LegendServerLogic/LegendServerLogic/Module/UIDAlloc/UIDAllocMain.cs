using System;
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using NLog;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;

namespace LegendServerLogic.UIDAlloc
{
    public class UIDAllocMain : Module
    {
        public UIDAllocMsgProxy msg_proxy;
        public UIDAllocMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new UIDAllocMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.X2X_ReplyUID, new MsgComponent(msg_proxy.OnReplyUID, typeof(ReplyUID_X2X)));
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