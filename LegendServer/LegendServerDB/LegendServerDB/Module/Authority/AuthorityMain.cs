using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendProtocol;
using LegendServer.Util;
using LegendServer.Database.Summoner;
using LegendServerDB.Core;
using LegendServer.Database;

namespace LegendServerDB.Authority
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
            MsgFactory.Regist(MsgID.A2D_RequestSetUserAuthority, new MsgComponent(msg_proxy.OnReqSetUserAuthority, typeof(RequestSetUserAuthority_A2D)));
            MsgFactory.Regist(MsgID.A2D_RequestGetAllSpecificUser, new MsgComponent(msg_proxy.OnReqGetAllSpecificUser, typeof(RequestGetAllSpecificUser_A2D)));
        }
        public override void OnRegistTimer()
        {
            TimerManager.Instance.Regist(TimerId.UnfreezeCheck, 0, 30000, int.MaxValue, OnUnfreezeCheckTimer, null, null, null);
        }

        public override void OnStart()
        {
        }
        public override void OnDestroy()
        {
        }
        public void OnUnfreezeCheckTimer(object obj)
        {
            DateTime nowTime = DateTime.Now;
            DateTime initTime = DateTime.Parse("1970-01-01 00:00:00");
            int illegalPlayerCnt = 0;
            foreach (SummonerDB player in DBManager<SummonerDB>.Instance.GetRecordsInCache())
            {
                if (DateTime.Equals(player.unLockTime, initTime) || player.auth != UserAuthority.Illegal)
                {
                    //过滤掉不需要处理解封的
                    continue;
                }
                if (nowTime > player.unLockTime)
                {
                    //过时解封
                    player.auth = UserAuthority.Guest;
                    player.unLockTime = initTime;

                    //持久化
                    DBManager<SummonerDB>.Instance.UpdateRecordInCache(player, e => e.id == player.id);
                }
                else
                {
                    //累计仍被封号的玩家数
                    illegalPlayerCnt++;
                }
            }
            if (illegalPlayerCnt <= 0)
            {
                //没有任何被封号的玩家则可以删除计时器了
                TimerManager.Instance.Remove(TimerId.UnfreezeCheck);
            }
        }
    }

}