using System;
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using NLog;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;

namespace LegendServerCenter.UIDAlloc
{
    public class UIDAllocMain : Module
    {
        public UIDAllocMsgProxy msg_proxy;
        private IList summonerBlackList = null;
        private ulong initBeginId = 10000;
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
            MsgFactory.Regist(MsgID.X2X_RequestUID, new MsgComponent(msg_proxy.OnRequestUID, typeof(RequestUID_X2X)));
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
        public void DisSummonerId()
        {
            ulong summonerCount = Convert.ToUInt64(NHibernateHelper.SQL("select count(*) from summoner"));
            summonerBlackList = NHibernateHelper.SQLSelect("select summonerId from summonerblacklist");
            if (summonerBlackList.Count > 0 && summonerCount > 0)
            {
                string sql = "SELECT MAX(id)FROM summoner WHERE id NOT IN(SELECT summonerId FROM summonerblacklist)";
                ulong max = (ulong)NHibernateHelper.SQL(sql);
                initBeginId = max + 1;
            }
            else if (summonerCount > 0)
            {
                initBeginId += summonerCount;
            }
        }
        public ulong NewSummonerId()
        {
            while (summonerBlackList.Count > 0)
            {
                if (summonerBlackList.Contains(initBeginId))
                {
                    initBeginId++;
                }
                else
                {
                    break;
                }
            }

            ulong summonerId = initBeginId;
            initBeginId++;
            return summonerId;
        }
    }
}