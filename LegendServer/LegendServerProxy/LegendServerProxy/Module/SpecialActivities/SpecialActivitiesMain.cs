using LegendProtocol;
using LegendServer.Database;
using System.Collections.Generic;
using System;
using LegendServer.Database.Config;

namespace LegendServerProxy.SpecialActivities
{
    public class SpecialActivitiesMain : Module
    {
        public SpecialActivitiesMsgProxy msg_proxy;
        public int Version = 1;
        public int maxPlayerNum = 0;
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
            ActivitiesSystemConfigDB cfg = DBManager<ActivitiesSystemConfigDB>.Instance.GetSingleRecordInCache(e => e.key == "Version");
            if (cfg != null)
            {
                int.TryParse(cfg.value, out Version);
            }
#if RUNFAST
            //跑得快商家模式最大人数
            maxPlayerNum = 3;
            RunFastConfigDB runFastConfigDB = DBManager<RunFastConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "RunFastMaxPlayerNum");
            if (runFastConfigDB != null)
            {
                int.TryParse(runFastConfigDB.value, out this.maxPlayerNum);
            }
#elif MAHJONG
            //麻将商家模式最大人数
            maxPlayerNum = 4;
            MahjongConfigDB mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongMaxPlayerNum");
            if (mahjongConfigDB != null)
            {
                int.TryParse(mahjongConfigDB.value, out this.maxPlayerNum);
            }
#elif WORDPLATE
            this.maxPlayerNum = WordPlateConstValue.WordPlateMaxPlayer;
#endif
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.X2P_RequestMarketKey, new MsgComponent(msg_proxy.OnRequestMarketKey, typeof(RequestMarketKey_X2P)));
            MsgFactory.Regist(MsgID.W2P_ReplyMarketKey, new MsgComponent(msg_proxy.OnReplyMarketKey, typeof(ReplyMarketKey_W2P)));
            MsgFactory.Regist(MsgID.X2P_RequestJoinMarket, new MsgComponent(msg_proxy.OnRequestJoinMarket, typeof(RequestJoinMarket_X2P)));
            MsgFactory.Regist(MsgID.W2P_ReplyJoinMarket, new MsgComponent(msg_proxy.OnReplyJoinMarket, typeof(ReplyJoinMarket_W2P)));
            MsgFactory.Regist(MsgID.X2P_RequestMarketVersion, new MsgComponent(msg_proxy.OnRequestMarketVersion, typeof(RequestMarketVersion_X2P)));
            MsgFactory.Regist(MsgID.T2P_RequestUseTickets, new MsgComponent(msg_proxy.OnRequestUseTickets, typeof(RequestUseTickets_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyUseTickets, new MsgComponent(msg_proxy.OnReplyUseTickets, typeof(ReplyUseTickets_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestTicketsInfo, new MsgComponent(msg_proxy.OnRequestTicketsInfo, typeof(RequestTicketsInfo_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyTicketsInfo, new MsgComponent(msg_proxy.OnReplyTicketsInfo, typeof(ReplyTicketsInfo_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestQuitMarketCompetition, new MsgComponent(msg_proxy.OnRequestQuitMarketCompetition, typeof(RequestQuitMarketCompetition_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyQuitMarketCompetition, new MsgComponent(msg_proxy.OnReplyQuitMarketCompetition, typeof(ReplyQuitMarketCompetition_L2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyJoinMarketCompetition, new MsgComponent(msg_proxy.OnReplyJoinMarketCompetition, typeof(ReplyJoinMarketCompetition_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestCreateMarketCompetition, new MsgComponent(msg_proxy.OnRequestCreateMarketCompetition, typeof(RequestCreateMarketCompetition_T2P)));
            MsgFactory.Regist(MsgID.W2P_ReplyCreateMarketCompetition, new MsgComponent(msg_proxy.OnReplyCreateMarketCompetition, typeof(ReplyCreateMarketCompetition_W2P)));
            MsgFactory.Regist(MsgID.T2P_RequestMarketCompetitionInfo, new MsgComponent(msg_proxy.OnRequestMarketCompetitionInfo, typeof(RequestMarketCompetitionInfo_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyMarketCompetitionInfo, new MsgComponent(msg_proxy.OnReplyMarketCompetitionInfo, typeof(ReplyMarketCompetitionInfo_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestCompetitionPlayerInfo, new MsgComponent(msg_proxy.OnRequestCompetitionPlayerInfo, typeof(RequestCompetitionPlayerInfo_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyCompetitionPlayerInfo, new MsgComponent(msg_proxy.OnReplyCompetitionPlayerInfo, typeof(ReplyCompetitionPlayerInfo_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvCompetitionPlayerRank, new MsgComponent(msg_proxy.OnRecvCompetitionPlayerRank, typeof(RecvCompetitionPlayerRank_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvCompetitionPlayerOverRank, new MsgComponent(msg_proxy.OnRecvCompetitionPlayerOverRank, typeof(RecvCompetitionPlayerOverRank_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestCompetitionPlayerOnline, new MsgComponent(msg_proxy.OnRequestCompetitionPlayerOnline, typeof(RequestCompetitionPlayerOnline_T2P)));
            MsgFactory.Regist(MsgID.L2P_RecvCompetitionPlayerApplyNum, new MsgComponent(msg_proxy.OnRecvCompetitionPlayerApplyNum, typeof(RecvCompetitionPlayerApplyNum_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestDelMarketCompetition, new MsgComponent(msg_proxy.OnRequestDelMarketCompetition, typeof(RequestDelMarketCompetition_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyDelMarketCompetition, new MsgComponent(msg_proxy.OnReplyDelMarketCompetition, typeof(ReplyDelMarketCompetition_L2P)));
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
        public bool IsPow(int powNum, int baseNum)
        {
            if (powNum <= 0 || baseNum <= 0 || powNum % baseNum != 0)
            {
                return false;
            }
            int tempNum = powNum;
            for (int i = 0; i < powNum; ++i)
            {
                tempNum = tempNum / baseNum;
                if (tempNum == 1)
                {
                    return true;
                }
                if (tempNum % baseNum != 0)
                {
                    return false;
                }
            }
            return false;
        }
    }
}