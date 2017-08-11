using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Summoner;
using System.Collections.Generic;
using System;
using LegendServerDB.Distributed;
using System.Text;
using LegendServer.Database.Config;

namespace LegendServerDB.Update
{
    public class UpdateMsgProxy : ServerMsgProxy
    {
        private UpdateMain main;

        public UpdateMsgProxy(UpdateMain main)
            : base(main.root)
        {
            this.main = main;
        }
    }
}

