using System.Collections.Concurrent;
using LegendProtocol;
using LegendServerAC.Core;
using LegendServer.Util;
using LegendServer.Database;
using LegendServer.Database.Config;

namespace LegendServerAC.SafeCheck
{
    public class SafeCheckMain : Module
    {
        public SafeCheckMsgProxy msg_proxy;
        public int procMsgAttackCheckTimePeriod;
        public int procMsgCountLimitAttackCheck;
        public int procMsgAttackCheckLockTime;
        public int procMsgAttackWarningCntLimit;
        public SafeCheckMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new SafeCheckMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
            procMsgAttackCheckTimePeriod = 3000;
            SystemConfigDB cfg1 = DBManager<SystemConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "procMsgAttackCheckTimePeriod");
            if (cfg1 != null)
            {
                int.TryParse(cfg1.value, out procMsgAttackCheckTimePeriod);
            }
            procMsgCountLimitAttackCheck = 50;
            SystemConfigDB cfg2 = DBManager<SystemConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "procMsgCountLimitAttackCheck");
            if (cfg2 != null)
            {
                int.TryParse(cfg2.value, out procMsgCountLimitAttackCheck);
            }
            procMsgAttackCheckLockTime = 3;
            SystemConfigDB cfg3 = DBManager<SystemConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "procMsgAttackCheckLockTime");
            if (cfg3 != null)
            {
                int.TryParse(cfg3.value, out procMsgAttackCheckLockTime);
            }
            procMsgAttackWarningCntLimit = 3;
            SystemConfigDB cfg4 = DBManager<SystemConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "procMsgAttackWarningCntLimit");
            if (cfg4 != null)
            {
                int.TryParse(cfg4.value, out procMsgAttackWarningCntLimit);
            }
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
        public void OnFrequentAttackCheckTimer(object obj)
        {
            ConcurrentDictionary<int, InboundClientSession> sessions = obj as ConcurrentDictionary<int, InboundClientSession>;

            bool existHacker = false;
            foreach (InboundClientSession session in sessions.Values)
            {
                if (session.lockedTimeByfrequentAttack > 0)
                {
                    //恶意攻击被锁定的时间倒计时
                    session.lockedTimeByfrequentAttack--;
                    existHacker = true;
                }
            }
            if (!existHacker)
            {
                //本轮没有需要给黑客锁定时间倒计时了则删除计时器达到优化目的
                TimerManager.Instance.Remove(TimerId.FrequentAttackCheck);
            }
        }
    }

}