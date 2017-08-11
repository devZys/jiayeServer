using LegendProtocol;
using LegendServer.Database;
using System.Collections.Generic;
using System;
using LegendServer.Database.Config;
using LegendServer.Util;
using System.Linq;
using LegendServerMarketManager;

namespace LegendServerWorld.SpecialActivities
{
    public class SpecialActivitiesMain : Module
    {
        public SpecialActivitiesMsgProxy msg_proxy;
        public int KeyUsedLimit = 1;
        public SpecialActivitiesMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new SpecialActivitiesMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
            ActivitiesSystemConfigDB cfg = DBManager<ActivitiesSystemConfigDB>.Instance.GetSingleRecordInCache(e => e.key == "KeyUsedLimit");
            if (cfg != null)
            {
                int.TryParse(cfg.value, out KeyUsedLimit);
            }
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.P2W_RequestMarketKey, new MsgComponent(msg_proxy.OnRequestMarketKey, typeof(RequestMarketKey_P2W)));
            MsgFactory.Regist(MsgID.P2W_RequestJoinMarket, new MsgComponent(msg_proxy.OnRequestJoinMarket, typeof(RequestJoinMarket_P2W)));
            MsgFactory.Regist(MsgID.L2W_RequestInitCompetitionKey, new MsgComponent(msg_proxy.OnRequestInitCompetitionKey, typeof(RequestInitCompetitionKey_L2W)));
            MsgFactory.Regist(MsgID.P2W_RequestCreateMarketCompetition, new MsgComponent(msg_proxy.OnRequestCreateMarketCompetition, typeof(RequestCreateMarketCompetition_P2W)));
            MsgFactory.Regist(MsgID.L2W_ReplyCreateMarketCompetition, new MsgComponent(msg_proxy.OnReplyCreateMarketCompetition, typeof(ReplyCreateMarketCompetition_L2W)));
            MsgFactory.Regist(MsgID.L2W_RequestMarketCompetitionBelong, new MsgComponent(msg_proxy.OnRequestMarketCompetitionBelong, typeof(RequestMarketCompetitionBelong_L2W)));
        }
        public override void OnRegistTimer()
        {
            TimerManager.Instance.Regist(TimerId.MarketsKeyIndateCheck, 0, 60000, int.MaxValue, OnMarketsKeyIndateCheckTimer, null, null, null);
        }

        private void OnMarketsKeyIndateCheckTimer(object obj)
        {
            foreach (KeyValuePair<int, Market> element in MarketManager.Instance.Markets)
            {
                SpecialActivitiesConfigDB cfg = DBManager<SpecialActivitiesConfigDB>.Instance.GetSingleRecordInCache(e => e.ID == element.Key);
                if (cfg == null) continue;

                var list = element.Value.keys.ToList();
                list.ForEach(e =>
                {
                    if (e.Value.keyType == MarketKeyType.EMK_Ordinary && DateTime.Now > e.Value.destroyTime)
                    {
                        element.Value.DestroyKey(e.Key);
                        //此口令销毁则归还口令池
                        MarketManager.Instance.KeyRandomBox.AddElement(e.Key, 1);
                    }
                });
                if (list.Count <= 0)
                {
                    //此商场所有口令生存期结束则商场打烊
                    MarketManager.Instance.TryRemoveMarket(element.Key);
                }
            }
        }

        public override void OnStart()
        {
           
        }
        public override void OnDestroy()
        {
        }
    }
}