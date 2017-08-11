using LegendProtocol;
using LegendServer.Database;
using System.Collections.Generic;
using System;
using LegendServer.Database.Config;
using LegendServerLogic.Distributed;
using LegendServerCompetitionManager;
using LegendServerLogicDefine;
using LegendServerLogic.Entity.Players;
using LegendServerLogic.Entity.Houses;
using LegendServerLogic.Entity.Base;
using LegendServerLogic.Actor.Summoner;
using LegendServer.Util;
#if RUNFAST
using LegendServerLogic.RunFast;
#elif MAHJONG
using LegendServerLogic.Mahjong;
#elif WORDPLATE
using LegendServerLogic.WordPlate;
#endif

namespace LegendServerLogic.SpecialActivities
{
    public class SpecialActivitiesMain : Module
    {
        public SpecialActivitiesMsgProxy msg_proxy;
#if RUNFAST
        public RunFastType runFastType = RunFastType.Sixteen;
        public int runFastMaxPlayerNum = 3;
        public int runFastMaxBureau = 5;
        public bool bSpadeThree = true;
        public bool bStrongOff = true;
        public bool bSurplusCardCount = false;
        public RunFastHousePropertyType housePropertyType = RunFastHousePropertyType.ERFHP_None;
#elif MAHJONG
        public MahjongType mahjongType = MahjongType.ChangShaMahjong;
        public int mahjongMaxPlayerNum = 4;
        public int mahjongMaxBureau = 4;
        public bool bZhuangLeisure = true;
        public bool bEatHu = true;
        public bool bHu7Pairs = true;
        public bool bGrabKongHu = true;
        public bool bJin = false;
        public bool bFakeHu = true;
        public bool bDoubleKong = true;
        public bool bDoubleSeabed = true;
        public bool bHuZhuang = true;
        public bool bIntegralCapped = true;
        public bool bPersonalisePendulum = true;
        public int catchBird = 6;
        public int flutter = 0;
        public int winBirdType = 1;
        public MahjongHousePropertyType housePropertyType = MahjongHousePropertyType.EMHP_None;
#elif WORDPLATE
        public WordPlateType wordPlateType = WordPlateType.WaiHuZiPlate;
        public int maxWinScore = 300;
        public int baseWinScore = 7;
        public int wordPlateMaxBureau = 4;
        public bool bFamous = true;
        public bool bBigSmallHu = true;
        public bool bBaoTing = true;
        public WordPlateHousePropertyType housePropertyType = WordPlateHousePropertyType.EMHP_None;
#endif
        private List<int> competitionKeyList = new List<int>();
        private List<int> competitionEndList = new List<int>();
        public int maxPlayerNum = 0;
        public int competitionPlayerPageNum = 20;
        public int competitionCreateLimitNum = 5;
        public int competitionApplyTime = 3;    //(小时)
        public int competitionEndTime = 3;     //(天)
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
            //活动比赛场请求玩家每页数量
            ActivitiesSystemConfigDB activitiesConfigDB = DBManager<ActivitiesSystemConfigDB>.Instance.GetSingleRecordInCache(e => e.key == "CompetitionPlayerPageNum");
            if (activitiesConfigDB != null)
            {
                int.TryParse(activitiesConfigDB.value, out competitionPlayerPageNum);
            }
            //商家创建比赛场限制
            activitiesConfigDB = DBManager<ActivitiesSystemConfigDB>.Instance.GetSingleRecordInCache(e => e.key == "CompetitionCreateLimitNum");
            if (activitiesConfigDB != null)
            {
                int.TryParse(activitiesConfigDB.value, out competitionCreateLimitNum);
            }
            //比赛场报名存在时间
            activitiesConfigDB = DBManager<ActivitiesSystemConfigDB>.Instance.GetSingleRecordInCache(e => e.key == "CompetitionApplyTime");
            if (activitiesConfigDB != null)
            {
                int.TryParse(activitiesConfigDB.value, out competitionApplyTime);
            }
            //比赛场结束存在时间
            activitiesConfigDB = DBManager<ActivitiesSystemConfigDB>.Instance.GetSingleRecordInCache(e => e.key == "CompetitionEndTime");
            if (activitiesConfigDB != null)
            {
                int.TryParse(activitiesConfigDB.value, out competitionEndTime);
            }
#if RUNFAST
            //跑得快商家模式最大人数
            RunFastConfigDB runFastConfigDB = DBManager<RunFastConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "RunFastMaxPlayerNum");
            if (runFastConfigDB != null)
            {
                int.TryParse(runFastConfigDB.value, out this.runFastMaxPlayerNum);
            }
            //跑得快商家模式最大对局数
            runFastConfigDB = DBManager<RunFastConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "RunFastMaxBureau");
            if (runFastConfigDB != null)
            {
                int.TryParse(runFastConfigDB.value, out this.runFastMaxBureau);
            }
            //跑得快商家模式最大张数
            runFastConfigDB = DBManager<RunFastConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "RunFastCardCount");
            if (runFastConfigDB != null)
            {
                int runFastCardCount = RunFastConstValue.RunFastSixteen;
                int.TryParse(runFastConfigDB.value, out runFastCardCount);
                if (runFastCardCount == RunFastConstValue.RunFastSixteen)
                {
                    this.runFastType = RunFastType.Sixteen;
                }
                else
                {
                    this.runFastType = RunFastType.Fifteen;
                }
            }
            //跑得快商家模式是否第一局必出黑桃三
            runFastConfigDB = DBManager<RunFastConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "RunFastSpadeThree");
            if (runFastConfigDB != null)
            {
                bool.TryParse(runFastConfigDB.value, out this.bSpadeThree);
            }
            //跑得快商家模式是否判断强关
            runFastConfigDB = DBManager<RunFastConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "RunFastStrongOff");
            if (runFastConfigDB != null)
            {
                bool.TryParse(runFastConfigDB.value, out this.bStrongOff);
            }
            //跑得快商家模式是否显示剩余牌数
            runFastConfigDB = DBManager<RunFastConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "RunFastSurplusCardCount");
            if (runFastConfigDB != null)
            {
                bool.TryParse(runFastConfigDB.value, out this.bSurplusCardCount);
            }
            InitHousePropertyType();
            //最大上限人数
            this.maxPlayerNum = runFastMaxPlayerNum;
#elif MAHJONG
            //麻将商家模式最大人数
            MahjongConfigDB mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongMaxPlayerNum");
            if (mahjongConfigDB != null)
            {
                int.TryParse(mahjongConfigDB.value, out this.mahjongMaxPlayerNum);
            }
            //麻将商家模式最大对局数
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongMaxBureau");
            if (mahjongConfigDB != null)
            {
                int.TryParse(mahjongConfigDB.value, out this.mahjongMaxBureau);
            }
            //麻将商家模式麻将类型
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongType");
            if (mahjongConfigDB != null)
            {
                int type = 0;
                int.TryParse(mahjongConfigDB.value, out type);
                this.mahjongType = (MahjongType)type;
            }
            //麻将商家模式抓鸟个数
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongCatchBirdCount");
            if (mahjongConfigDB != null)
            {
                int.TryParse(mahjongConfigDB.value, out this.catchBird);
            }     
            //麻将商家模式飘(0-不飘,1-飘1分,2-飘2分)
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongFlutterType");
            if (mahjongConfigDB != null)
            {
                int.TryParse(mahjongConfigDB.value, out this.flutter);
            }
            //麻将商家模式是否庄闲
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongZhuangLeisure");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bZhuangLeisure);
            }
            //麻将商家模式中鸟类型
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongWinBirdType");
            if (mahjongConfigDB != null)
            {
                int.TryParse(mahjongConfigDB.value, out this.winBirdType);
            }
            //麻将商家模式是否吃胡
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongEatHu");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bEatHu);
            }
            //麻将商家模式是否胡7对
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongHu7Pairs");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bHu7Pairs);
            }
            //麻将商家模式是否抢杠胡
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongGrabKongHu");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bGrabKongHu);
            }
            //麻将商家模式是否算筋
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongJin");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bJin);
            }
            //麻将商家模式是否假将胡
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongFakeHu");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bFakeHu);
            }
            //麻将商家模式是否双倍开杠
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongDoubleKong");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bDoubleKong);
            }
            //麻将商家模式是否双倍海底
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongDoubleSeabed");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bDoubleSeabed);
            }
            //麻将商家模式是否胡牌为庄
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongHuZhuang");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bHuZhuang);
            }
            //麻将商家模式是否积分上限
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongOpenIntegralCapped");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bIntegralCapped);
            }
            //麻将商家模式是否开启个性摆牌
            mahjongConfigDB = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongOpenPersonalisePendulum");
            if (mahjongConfigDB != null)
            {
                bool.TryParse(mahjongConfigDB.value, out this.bPersonalisePendulum);
            }
            InitHousePropertyType();
            //最大上限人数
            this.maxPlayerNum = mahjongMaxPlayerNum;
#elif WORDPLATE
            //字牌商家模式麻将类型
            WordPlateConfigDB wordPlateConfigDB = DBManager<WordPlateConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "WordPlateType");
            if (wordPlateConfigDB != null)
            {
                int type = 0;
                int.TryParse(wordPlateConfigDB.value, out type);
                this.wordPlateType = (WordPlateType)type;
            }
            //字牌商家模式最大对局数
            wordPlateConfigDB = DBManager<WordPlateConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "WordPlateMaxBureau");
            if (wordPlateConfigDB != null)
            {
                int.TryParse(wordPlateConfigDB.value, out this.wordPlateMaxBureau);
            }
            //字牌商家基础胡息
            wordPlateConfigDB = DBManager<WordPlateConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "WordPlateBaseWinScore");
            if (wordPlateConfigDB != null)
            {
                int.TryParse(wordPlateConfigDB.value, out this.baseWinScore);
            }
            //字牌商家最大胡息
            wordPlateConfigDB = DBManager<WordPlateConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "WordPlateMaxWinScore");
            if (wordPlateConfigDB != null)
            {
                int.TryParse(wordPlateConfigDB.value, out this.maxWinScore);
            }
            //字牌商家是否计算名堂
            wordPlateConfigDB = DBManager<WordPlateConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "WordPlateIsOpenFamous");
            if (wordPlateConfigDB != null)
            {
                bool.TryParse(wordPlateConfigDB.value, out this.bFamous);
            }
            //字牌商家是否计算大小胡
            wordPlateConfigDB = DBManager<WordPlateConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "WordPlateIsOpenBigSmallHu");
            if (wordPlateConfigDB != null)
            {
                bool.TryParse(wordPlateConfigDB.value, out this.bBigSmallHu);
            }
            //字牌商家是否计算报听
            wordPlateConfigDB = DBManager<WordPlateConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "WordPlateIsOpenBaoTing");
            if (wordPlateConfigDB != null)
            {
                bool.TryParse(wordPlateConfigDB.value, out this.bBaoTing);
            }
            InitHousePropertyType();
            //最大上限人数
            this.maxPlayerNum = WordPlateConstValue.WordPlateMaxPlayer;
#endif
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.W2L_NotifyJoinMarket, new MsgComponent(msg_proxy.OnNotifyJoinMarket, typeof(NotifyJoinMarket_W2L)));
            MsgFactory.Regist(MsgID.P2L_RequestTicketsInfo, new MsgComponent(msg_proxy.OnRequestTicketsInfo, typeof(RequestTicketsInfo_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestUseTickets, new MsgComponent(msg_proxy.OnRequestUseTickets, typeof(RequestUseTickets_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestQuitMarketCompetition, new MsgComponent(msg_proxy.OnRequestQuitMarketCompetition, typeof(RequestQuitMarketCompetition_P2L)));
            MsgFactory.Regist(MsgID.W2L_RequestCreateMarketCompetition, new MsgComponent(msg_proxy.OnRequestCreateMarketCompetition, typeof(RequestCreateMarketCompetition_W2L)));
            MsgFactory.Regist(MsgID.P2L_RequestMarketCompetitionInfo, new MsgComponent(msg_proxy.OnRequestMarketCompetitionInfo, typeof(RequestMarketCompetitionInfo_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestCompetitionPlayerInfo, new MsgComponent(msg_proxy.OnRequestCompetitionPlayerInfo, typeof(RequestCompetitionPlayerInfo_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestCompetitionPlayerOnline, new MsgComponent(msg_proxy.OnRequestCompetitionPlayerOnline, typeof(RequestCompetitionPlayerOnline_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestDelMarketCompetition, new MsgComponent(msg_proxy.OnRequestDelMarketCompetition, typeof(RequestDelMarketCompetition_P2L)));
            MsgFactory.Regist(MsgID.W2L_ReplyMarketCompetitionBelong, new MsgComponent(msg_proxy.OnReplyMarketCompetitionBelong, typeof(ReplyMarketCompetitionBelong_W2L)));
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
        private void InitHousePropertyType()
        {
#if MAHJONG
            this.housePropertyType = MahjongHousePropertyType.EMHP_None;
            if (this.bZhuangLeisure)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_ZhuangLeisure;
            }
            if (bEatHu)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_EatHu;
            }
            if (1 == this.winBirdType)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_BirdDoubleIntegral;
            }
            else if (2 == this.winBirdType)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_BirdAddIntegral;
            }
            if (bHu7Pairs)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_Hu7Pairs;
            }
            if (bGrabKongHu)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_GrabKongHu;
            }
            if (bJin)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_Jin;
            }
            if (bFakeHu)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_FakeHu;
            }
            if (bDoubleKong)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_DoubleKong;
            }
            if (bDoubleSeabed)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_DoubleSeabed;
            }
            if (bHuZhuang)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_HuZhuang;
            }
            if (bIntegralCapped)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_IntegralCapped;
            }
            if (bPersonalisePendulum)
            {
                this.housePropertyType |= MahjongHousePropertyType.EMHP_PersonalisePendulum;
            }
#elif RUNFAST
            this.housePropertyType = RunFastHousePropertyType.ERFHP_None;
            if (this.bSpadeThree)
            {
                this.housePropertyType |= RunFastHousePropertyType.ERFHP_SpadeThree;
            }
            if (this.bStrongOff)
            {
                this.housePropertyType |= RunFastHousePropertyType.ERFHP_StrongOff;
            }
            if (this.bSurplusCardCount)
            {
                this.housePropertyType |= RunFastHousePropertyType.ERFHP_SurplusCardCount;
            }
#elif WORDPLATE
            this.housePropertyType = WordPlateHousePropertyType.EMHP_None;     
            if (this.bFamous)
            {
                this.housePropertyType |= WordPlateHousePropertyType.EMHP_Famous;
            }
            if (this.bBigSmallHu)
            {
                this.housePropertyType |= WordPlateHousePropertyType.EMHP_BigSmallHu;
            }
            if (this.bBaoTing)
            {
                this.housePropertyType |= WordPlateHousePropertyType.EMHP_BaoTing;
            }
#endif
        }
        private void OnMarketsCompetitionCheckTimer(object obj)
        {
            List<int> delKeyList = new List<int>();
            competitionKeyList.ForEach(competitionKey =>
            {
                bool bDel = true;
                Competition competition = CompetitionManager.Instance.GetCompetitionByKey(competitionKey);
                if (competition != null)
                {
                    if (competition.status == CompetitionStatus.ECS_Begin)
                    {
                        if (competition.waitCount >= 3)
                        {
                            if (competition.currentBureau == 1)
                            {
                                PlayerMatchGame(competition.comPlayerList, competition.marketId, competition.competitionKey, competition.maxGameBureau);
                            }
                            else
                            {
                                List<CompetitionPlayer> comPlayerList = competition.comPlayerList.FindAll(element => element.status == CompPlayerStatus.ECPS_Apply);
                                PlayerMatchGame(comPlayerList, competition.marketId, competition.competitionKey, competition.maxGameBureau);
                            }
                            if (!competition.comPlayerList.Exists(element => element.status == CompPlayerStatus.ECPS_Apply))
                            {
                                competition.ChangeCompetitionStatus(CompetitionStatus.ECS_Game);
                            }
                        }
                        else
                        {
                            competition.waitCount += 1;
                            bDel = false;
                        }
                    }
                    else if (competition.status == CompetitionStatus.ECS_Wait)
                    {
                        if (competition.waitCount >= 3)
                        {
                            if (competition.currentBureau == 1)
                            {
                                PlayerSortAndAdmit(competition.strTicketList, competition.comPlayerList, competition.firstAdmitNum, competition.curAdmitNum);
                            }
                            else
                            {
                                List<CompetitionPlayer> comPlayerList = competition.comPlayerList.FindAll(element => element.status == CompPlayerStatus.ECPS_Wait);
                                PlayerSortAndAdmit(competition.strTicketList, comPlayerList, competition.firstAdmitNum, competition.curAdmitNum);
                            }
                            if (competition.CheckFullBureau())
                            {
                                competition.ChangeCompetitionStatus(CompetitionStatus.ECS_End);
                            }
                            else
                            {
                                bDel = false;
                                competition.ChangeCompetitionStatus(CompetitionStatus.ECS_Begin, bDel);
                            }
                        }
                        else
                        {
                            competition.waitCount += 1;
                            bDel = false;
                        }
                    }
                }
                if (bDel)
                {
                    delKeyList.Add(competitionKey);
                }
            });
            if (delKeyList.Count > 0)
            {
                DelMarketsCompetitionTimer(delKeyList);
            }
        }
        private void DelMarketsCompetitionTimer(List<int> delKeyList)
        {
            delKeyList.ForEach(delKey =>
            {
                competitionKeyList.RemoveAll(element => element == delKey);
            });
            if (competitionKeyList.Count == 0 && TimerManager.Instance.Exist(TimerId.MarketsCompetitionCheck))
            {
                //没有商家开比赛不用计时了
                TimerManager.Instance.Remove(TimerId.MarketsCompetitionCheck);
            }
        }
        public void AddMarketsCompetitionTimer(int competitionKey)
        {
            if (!competitionKeyList.Contains(competitionKey))
            {
                competitionKeyList.Add(competitionKey);
                CheckMarketsCompetitionTimer();
            }
        }
        private void CheckMarketsCompetitionTimer()
        {
            if (!TimerManager.Instance.Exist(TimerId.MarketsCompetitionCheck))
            {
                TimerManager.Instance.Regist(TimerId.MarketsCompetitionCheck, 0, 1000, int.MaxValue, OnMarketsCompetitionCheckTimer, null, null, null);
            }
        }
        private void OnMarketsCompetitionEndCheckTimer(object obj)
        {
            List<int> delKeyList = new List<int>();
            List<int> delCompetitionList = new List<int>();
            competitionEndList.ForEach(competitionKey =>
            {
                Competition competition = CompetitionManager.Instance.GetCompetitionByKey(competitionKey);
                if (competition != null)
                {
                    if (competition.status == CompetitionStatus.ECS_Apply)
                    {
                        TimeSpan span = DateTime.Now.Subtract(competition.createTime);
                        if (span.TotalHours >= competitionApplyTime)
                        {
                            //规定时间内没加满自动清理
                            delCompetitionList.Add(competitionKey);
                        }
                    }
                    else if (competition.status == CompetitionStatus.ECS_End)
                    {
                        TimeSpan span = DateTime.Now.Subtract(competition.endTime);
                        if (span.TotalDays >= competitionEndTime)
                        {
                            //比赛完规定时间内自动清理
                            delCompetitionList.Add(competitionKey);
                        }
                    }
                    else
                    {
                        //比赛场进行比赛在本计时器中清除
                        delKeyList.Add(competitionKey);
                    }
                }
                else
                {
                    //口令不存在规定时间内自动清理
                    delCompetitionList.Add(competitionKey);
                }
            });
            if (delCompetitionList.Count > 0 || delKeyList.Count > 0)
            {
                DelMarketsCompetitionEndTimer(delCompetitionList, delKeyList);
            }
        }
        private void DelMarketsCompetitionEndTimer(List<int> delCompetitionList, List<int> delKeyList = null)
        {
            if (delKeyList != null)
            {
                delKeyList.ForEach(delKey =>
                {
                    competitionEndList.RemoveAll(element => element == delKey);
                });
            }
            if (delCompetitionList.Count > 0)
            {
                delCompetitionList.ForEach(delKey =>
                {
                    competitionEndList.RemoveAll(element => element == delKey);
                    CompetitionManager.Instance.RemoveCompetitionByKey(delKey);
                });
                //通知world服务器清除比赛场口令
                OnRequestInitCompetitionKey(delCompetitionList);
            }
            if (competitionEndList.Count == 0 && TimerManager.Instance.Exist(TimerId.MarketsCompetitionEndCheck))
            {
                //没有商家结束比赛不用计时了
                TimerManager.Instance.Remove(TimerId.MarketsCompetitionEndCheck);
            }
        }
        public void DelMarketsCompetition(int competitionKey)
        {
            List<int> delCompetitionList = new List<int>();
            delCompetitionList.Add(competitionKey);

            DelMarketsCompetitionEndTimer(delCompetitionList);
        }
        public void AddMarketsCompetitionEndTimer(int competitionKey)
        {
            if (!competitionEndList.Contains(competitionKey))
            {
                competitionEndList.Add(competitionKey);
                CheckMarketsCompetitionEndTimer();
            }
        }
        private void CheckMarketsCompetitionEndTimer()
        {
            if (!TimerManager.Instance.Exist(TimerId.MarketsCompetitionEndCheck))
            {
                TimerManager.Instance.Regist(TimerId.MarketsCompetitionEndCheck, 0, 60 * 60 * 1000, int.MaxValue, OnMarketsCompetitionEndCheckTimer, null, null, null);
            }
        }
        private void OnRequestInitCompetitionKey(List<int> delCompetitionList)
        {
            msg_proxy.OnRequestInitCompetitionKey(delCompetitionList);
        }
        private void PlayerMatchGame(List<CompetitionPlayer> playerList, int marketId, int marketKey, int maxGameBureau)
        {
            int playerNum = 0;
            House house = null;
            List<CompetitionPlayer> randomPlayerList = RandomCompetitionPlayer(playerList);
            randomPlayerList.ForEach(player =>
            {
                if (player.status == CompPlayerStatus.ECPS_Apply)
                {
                    PlayerInfo playerInfo = new PlayerInfo(player.summonerId, player.nickName, player.userId, player.ip, player.sex);
                    house = PlayerMatchGame(playerInfo, marketId, marketKey, maxGameBureau, house);
                    if (house != null)
                    {
                        player.rank = 0;
                        player.allIntegral = 0;
                        player.status = CompPlayerStatus.ECPS_Game;
                        playerNum += 1;
                        if (maxPlayerNum == playerNum)
                        {
                            playerNum = 0;
                            house = null;
                        }
                    }
                }
            });
        }
        private House PlayerMatchGame(PlayerInfo playerInfo, int marketId, int marketKey, int maxGameBureau, House house)
        {
#if RUNFAST
            if (house == null)
            {
                RunFastHouse runFastHouse = new RunFastHouse();
                runFastHouse.houseId = MyGuid.NewGuid(ServiceType.Logic, (uint)msg_proxy.root.ServerID);
                runFastHouse.logicId = msg_proxy.root.ServerID;
                runFastHouse.houseCardId = 0;
                runFastHouse.maxBureau = maxGameBureau;
                runFastHouse.runFastType = runFastType;
                runFastHouse.maxPlayerNum = maxPlayerNum;
                runFastHouse.housePropertyType = (int)housePropertyType;
                runFastHouse.businessId = marketId;
                runFastHouse.competitionKey = marketKey;
                runFastHouse.createTime = DateTime.Now;
                runFastHouse.houseStatus = RunFastHouseStatus.RFHS_FreeBureau;
                ModuleManager.Get<RunFastMain>().msg_proxy.OnReqCreateRunFastHouse(playerInfo, runFastHouse);

                return runFastHouse;
            }
            else
            {
                RunFastHouse runFastHouse = house as RunFastHouse;
                ModuleManager.Get<RunFastMain>().msg_proxy.OnReqJoinRunFastHouse(playerInfo, runFastHouse);

                return runFastHouse;
            }
#elif MAHJONG
            if (house == null)
            {
                MahjongHouse mahjongHouse = new MahjongHouse();
                if (!mahjongHouse.SetMahjongSetTile(mahjongType))
                {
                    mahjongHouse.houseId = MyGuid.NewGuid(ServiceType.Logic, (uint)msg_proxy.root.ServerID);
                    mahjongHouse.logicId = msg_proxy.root.ServerID;
                    mahjongHouse.houseCardId = 0;
                    mahjongHouse.maxBureau = maxGameBureau;
                    mahjongHouse.mahjongType = mahjongType;
                    mahjongHouse.maxPlayerNum = maxPlayerNum;
                    mahjongHouse.catchBird = catchBird;
                    mahjongHouse.flutter = flutter;
                    mahjongHouse.housePropertyType = (int)housePropertyType;
                    mahjongHouse.businessId = marketId;
                    mahjongHouse.competitionKey = marketKey;
                    mahjongHouse.createTime = DateTime.Now;
                    mahjongHouse.houseStatus = MahjongHouseStatus.MHS_FreeBureau;
                    ModuleManager.Get<MahjongMain>().msg_proxy.OnReqCreateMahjongHouse(playerInfo, mahjongHouse);

                    return mahjongHouse;
                }
                return null;
            }
            else
            {
                MahjongHouse mahjongHouse = house as MahjongHouse;
                ModuleManager.Get<MahjongMain>().msg_proxy.OnReqJoinMahjongHouse(playerInfo, mahjongHouse);

                return mahjongHouse;
            }
#elif WORDPLATE
            if (house == null)
            {
                WordPlateHouse wordPlateHouse = new WordPlateHouse();
                if (!wordPlateHouse.SetWordPlateStrategy(wordPlateType))
                {
                    wordPlateHouse.houseId = MyGuid.NewGuid(ServiceType.Logic, (uint)msg_proxy.root.ServerID);
                    wordPlateHouse.logicId = msg_proxy.root.ServerID;
                    wordPlateHouse.houseCardId = 0;
                    wordPlateHouse.maxBureau = maxGameBureau;
                    wordPlateHouse.wordPlateType = wordPlateType;
                    wordPlateHouse.maxPlayerNum = maxPlayerNum;
                    wordPlateHouse.baseWinScore = baseWinScore;
                    wordPlateHouse.maxWinScore = maxWinScore;
                    wordPlateHouse.housePropertyType = (int)housePropertyType;
                    wordPlateHouse.businessId = marketId;
                    wordPlateHouse.competitionKey = marketKey;
                    wordPlateHouse.createTime = DateTime.Now;
                    wordPlateHouse.houseStatus = WordPlateHouseStatus.EWPS_FreeBureau;
                    ModuleManager.Get<WordPlateMain>().msg_proxy.OnReqCreateWordPlateHouse(playerInfo, wordPlateHouse);

                    return wordPlateHouse;
                }
                return null;
            }
            else
            {
                WordPlateHouse wordPlateHouse = house as WordPlateHouse;
                ModuleManager.Get<WordPlateMain>().msg_proxy.OnReqJoinWordPlateHouse(playerInfo, wordPlateHouse);

                return wordPlateHouse;
            }
#endif
        }
        private void PlayerSortAndAdmit(string[] strTicketList, List<CompetitionPlayer> waitPlayerList, int firstAdmitNum, int curAdmitNum)
        {
            //先排序(除了最后一局，其他都在等待的时候排过序)
            if (curAdmitNum == 1)
            {
                waitPlayerList.Sort(CompareTileByIntegra);
            }
            int rank = 0;
            waitPlayerList.ForEach(player =>
            {
                if (curAdmitNum == 1)
                {
                    rank += 1;
                    player.rank = rank;
                }
                if (curAdmitNum > 1 && player.rank <= curAdmitNum)
                {
                    player.status = CompPlayerStatus.ECPS_Apply;
                }
                else
                {
                    player.status = CompPlayerStatus.ECPS_Over;
                    PlayerOverGame(player.summonerId, player.rank, strTicketList);
                }
            });
        }
        private List<CompetitionPlayer> RandomCompetitionPlayer(List<CompetitionPlayer> playerList)
        {
            List<CompetitionPlayer> randomPlayerList = new List<CompetitionPlayer>();
            List<int> randomIndexList = new List<int>();
            for (int i = 0; i < playerList.Count; ++i)
            {
                randomIndexList.Add(i);
            }
            for (int i = 0; i < playerList.Count; ++i)
            {
                int random = MyRandom.NextPrecise(0, randomIndexList.Count);
                int index = randomIndexList[random];
                randomPlayerList.Add(playerList[index]);
                randomIndexList.Remove(index);
            }
            return randomPlayerList;
        }
        public int CompareTileByIntegra(CompetitionPlayer a, CompetitionPlayer b)
        {
            if (a.allIntegral < b.allIntegral)
            {
                return 1;
            }
            else if (a.allIntegral == b.allIntegral)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
        public int CompareTileByRank(CompetitionPlayer a, CompetitionPlayer b)
        {
            if (a.rank > b.rank)
            {
                return 1;
            }
            else if (a.rank == b.rank)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
        public void OnReqCompetitionIntegral(int marketKey, List<ComPlayerIntegral> playerIntegralList)
        {
            Competition competition = CompetitionManager.Instance.GetCompetitionByKey(marketKey);
            if (competition != null)
            {
                competition.AddPlayerIntegral(playerIntegralList);
            }
        }
        public TicketsNode GetTickets(int businessId, int rank, bool bCompetition = false)
        {
            return GetTickets(GetStrTickets(businessId, bCompetition), rank);
        }
        public string[] GetStrTickets(int businessId, bool bCompetition = true)
        {
            if (businessId <= 0)
            {
                return null;
            }
            SpecialActivitiesConfigDB activitiesConfig = DBManager<SpecialActivitiesConfigDB>.Instance.GetSingleRecordInCache(config => config.ID == businessId);
            if (activitiesConfig == null)
            {
                return null;
            }
            string strTickets = "";
            if (bCompetition)
            {
                strTickets = activitiesConfig.ComTickets;
            }
            else
            {
                strTickets = activitiesConfig.Tickets;
            }
            if (string.IsNullOrEmpty(strTickets))
            {
                return null;
            }
            return strTickets.Split(';');
        }
        private TicketsNode GetTickets(string[] strTicketList, int rank)
        {
            if (strTicketList == null || rank < 0)
            {
                return null;
            }
            if (strTicketList.Length <= rank)
            {
                return null;
            }
            int ticketsId = 0;
            if (!int.TryParse(strTicketList[rank], out ticketsId))
            {
                return null;
            }
            TicketsConfigDB ticketsConfig = DBManager<TicketsConfigDB>.Instance.GetSingleRecordInCache(config => config.ID == ticketsId);
            if (ticketsConfig == null)
            {
                return null;
            }
            TicketsNode ticketsNode = new TicketsNode();
            ticketsNode.id = MyGuid.NewGuid(ServiceType.Logic, (uint)msg_proxy.root.ServerID);
            ticketsNode.ticketsId = ticketsId;
            ticketsNode.useStatus = UseStatus.Unused;
            ticketsNode.beginTime = DateTime.Now.ToString();

            return ticketsNode;
        }
        public bool CheckTicketsTime(int ticketsId, string beginTime)
        {
            if (string.IsNullOrEmpty(beginTime))
            {
                return true;
            }
            DateTime time = new DateTime();
            if(!DateTime.TryParse(beginTime, out time))
            {
                return true;
            }
            TicketsConfigDB ticketsConfig = DBManager<TicketsConfigDB>.Instance.GetSingleRecordInCache(config => config.ID == ticketsId);
            if (ticketsConfig == null || ticketsConfig.Indate == 0)
            {
                return true;
            }            
            if ((DateTime.Now - time).TotalMinutes < ticketsConfig.Indate)
            {
                return false;
            }
            return true;
        }
        public int GetMaxBureau(int firstAdmitNum)
        {
            if (firstAdmitNum <= 1)
            {
                return 0;
            }
            if (maxPlayerNum > 0 && firstAdmitNum % maxPlayerNum == 0)
            {
                int result = 1;
                int admintNum = firstAdmitNum;
                for(int i = 0; i < firstAdmitNum; ++i)
                {
                    admintNum = admintNum / maxPlayerNum;
                    result += 1;
                    if (admintNum == 1)
                    {
                        break;
                    }
                    if (admintNum % maxPlayerNum != 0)
                    {
                        return 0;
                    }
                }
                return result;
            }
            return 0;
        }
        public void SendPlayerRank(ulong summonerId, int rank, int admitNum, int houseCount)
        {
            Summoner summoner = SummonerManager.Instance.GetSummonerById(summonerId);
            if (summoner != null)
            {
                //在线才发
                msg_proxy.RecvCompetitionPlayerRank(summoner.id, summoner.proxyServerId, rank, admitNum, houseCount);
            }
        }
        public void SendPlayerQuitCompetition(ulong summonerId)
        {
            Summoner sender = SummonerManager.Instance.GetSummonerById(summonerId);
            if (sender != null)
            {
                sender.competitionKey = 0;
                msg_proxy.RecvQuitMarketCompetition(sender.id, sender.proxyServerId);
            }
            msg_proxy.PlayerSaveCompetitionKey(summonerId, 0, true);
        }
        public void PlayerOverGame(ulong summonerId, int rank, string[] strTicketList)
        {
            TicketsNode ticketsNode = GetTickets(strTicketList, rank - 1);
            if (ticketsNode != null)
            {
                Summoner sender = SummonerManager.Instance.GetSummonerById(summonerId);
                if (sender != null)
                {
                    msg_proxy.RecvCompetitionPlayerOverRank(sender.id, sender.proxyServerId, rank, ticketsNode);
                    //清理口令
                    sender.competitionKey = 0;
                    //加入优惠券
                    sender.AddTicketsNode(ticketsNode);
                }
                msg_proxy.PlayerSaveCompetitionKey(summonerId, 0, true);
                //玩家保存优惠券
                msg_proxy.OnRequestSaveTickets(summonerId, ticketsNode);
            }
            else
            {
                Summoner summoner = SummonerManager.Instance.GetSummonerById(summonerId);
                if (summoner != null)
                {
                    msg_proxy.RecvCompetitionPlayerOverRank(summoner.id, summoner.proxyServerId, rank);
                }
                msg_proxy.PlayerSaveCompetitionKey(summonerId);
            }
        }
    }
}