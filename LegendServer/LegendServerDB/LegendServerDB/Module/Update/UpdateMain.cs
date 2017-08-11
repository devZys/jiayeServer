using LegendProtocol;
using LegendServer.Database;
using System.Collections.Generic;
using System.Linq;
using LegendServerDB.Core;
using LegendServer.Database.Config;
using LegendServer.Database.Summoner;
using LegendServer.Util;
using System;
using LegendServer.Database.ServiceBox;
using LegendServerDB.Distributed;

namespace LegendServerDB.Update
{
    public class UpdateMain : Module
    {
        public UpdateMsgProxy msg_proxy;
        
        private Dictionary<int, Func<SummonerDB, byte[], bool>> summonerHandles = new Dictionary<int, Func<SummonerDB, byte[], bool>>();

        public UpdateMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new UpdateMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
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