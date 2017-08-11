#if WORDPLATE
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Util;
using LegendServerLogic.Actor.Summoner;
using LegendServerLogic.Entity.Base;
using LegendServerLogic.Entity.Houses;
using LegendServerLogic.Entity.Players;
using LegendServerLogic.MainCity;
using LegendServerLogic.SpecialActivities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LegendServerLogic.WordPlate
{
    public class WordPlateMain : Module
    {
        public WordPlateMsgProxy msg_proxy;
        public List<BureauByHouseCard> bureauByHouseCardList = new List<BureauByHouseCard>();
        public List<int> whzBasaScoreList = new List<int>();
        public List<int> whzMaxScoreList = new List<int>();
        public List<ulong> houseVateList = new List<ulong>();
        public List<HouseActionNode> houseActionList = new List<HouseActionNode>();
        public int houseDissolveVoteTime;
        public int businessDissolveVoteTime;
        public bool alreadyRequestHouseInfo = false;
        public WordPlateMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new WordPlateMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
            //字牌开局数对应的房卡消耗
            WordPlateConfigDB wordPlateConfig = DBManager<WordPlateConfigDB>.Instance.GetSingleRecordInCache(element => element.key == "WordPlateBureauByHouseCard");
            if (wordPlateConfig != null && !string.IsNullOrEmpty(wordPlateConfig.value))
            {
                InitWordPlateBureauByHouseCard(wordPlateConfig.value);
            }
            //歪胡子对应的胡牌基础积分
            wordPlateConfig = DBManager<WordPlateConfigDB>.Instance.GetSingleRecordInCache(element => element.key == "WHZWordPlateBasaWinScore");
            if (wordPlateConfig != null && !string.IsNullOrEmpty(wordPlateConfig.value))
            {
                InitWordPlateBasaWinScore(wordPlateConfig.value);
            }
            //歪胡子对应的胡牌最大积分
            wordPlateConfig = DBManager<WordPlateConfigDB>.Instance.GetSingleRecordInCache(element => element.key == "WHZWordPlateMaxWinScore");
            if (wordPlateConfig != null && !string.IsNullOrEmpty(wordPlateConfig.value))
            {
                InitWordPlateMaxWinScore(wordPlateConfig.value);
            }
            //房间解散计时时间
            houseDissolveVoteTime = 300;
            ServerConfigDB serverConfig = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "HouseDissolveVoteTime");
            if (serverConfig != null && !string.IsNullOrEmpty(serverConfig.value))
            {
                int.TryParse(serverConfig.value, out houseDissolveVoteTime);
            }
            //活动房间解散计时时间
            businessDissolveVoteTime = 60;
            serverConfig = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "BusinessDissolveVoteTime");
            if (serverConfig != null && !string.IsNullOrEmpty(serverConfig.value))
            {
                int.TryParse(serverConfig.value, out businessDissolveVoteTime);
            }
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.D2L_ReplyWordPlateHouseInfo, new MsgComponent(msg_proxy.OnReplyWordPlateHouseInfo, typeof(ReplyWordPlateHouseInfo_D2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyWordPlatePlayerAndBureau, new MsgComponent(msg_proxy.OnReplyWordPlatePlayerAndBureau, typeof(ReplyWordPlatePlayerAndBureau_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestCreateWordPlateHouse, new MsgComponent(msg_proxy.OnReqCreateWordPlateHouse, typeof(RequestCreateWordPlateHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestJoinWordPlateHouse, new MsgComponent(msg_proxy.OnReqJoinWordPlateHouse, typeof(RequestJoinWordPlateHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestQuitWordPlateHouse, new MsgComponent(msg_proxy.OnReqQuitWordPlateHouse, typeof(RequestQuitWordPlateHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestReadyWordPlateHouse, new MsgComponent(msg_proxy.OnReqReadyWordPlateHouse, typeof(RequestReadyWordPlateHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestWordPlateHouseVote, new MsgComponent(msg_proxy.OnReqDissolveHouseVote, typeof(RequestWordPlateHouseVote_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestWordPlateHouseInfo, new MsgComponent(msg_proxy.OnReqWordPlateHouseInfo, typeof(RequestWordPlateHouseInfo_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestShowWordPlate, new MsgComponent(msg_proxy.OnReqShowWordPlate, typeof(RequestShowWordPlate_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestOperatWordPlate, new MsgComponent(msg_proxy.OnReqOperatWordPlate, typeof(RequestOperatWordPlate_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestWordPlateOverallRecord, new MsgComponent(msg_proxy.OnReqWordPlateOverallRecord, typeof(RequestWordPlateOverallRecord_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyWordPlateOverallRecord, new MsgComponent(msg_proxy.OnReplyWordPlateOverallRecord, typeof(ReplyWordPlateOverallRecord_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestWordPlateBureauRecord, new MsgComponent(msg_proxy.OnReqWordPlateBureauRecord, typeof(RequestWordPlateBureauRecord_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyWordPlateBureauRecord, new MsgComponent(msg_proxy.OnReplyWordPlateBureauRecord, typeof(ReplyWordPlateBureauRecord_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestWordPlateBureauPlayback, new MsgComponent(msg_proxy.OnReqWordPlateBureauPlayback, typeof(RequestWordPlateBureauPlayback_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyWordPlateBureauPlayback, new MsgComponent(msg_proxy.OnReplyWordPlateBureauPlayback, typeof(ReplyWordPlateBureauPlayback_D2L)));
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
        private void OnHouseDissolveVoteTimer(object obj)
        {
            List<ulong> delHouseIdList = new List<ulong>();
            foreach (ulong houseId in houseVateList)
            {
                House house = HouseManager.Instance.GetHouseById(houseId);
                if (house == null)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnHouseDissolveVoteTimer, 房间不存在! houseId = " + houseId);
                    delHouseIdList.Add(houseId);
                    continue;
                }
                if (house.houseType == HouseType.WordPlateHouse)
                {
                    WordPlateHouse wordPlateHouse = house as WordPlateHouse;
                    if (wordPlateHouse == null || wordPlateHouse.houseStatus >= WordPlateHouseStatus.EWPS_Dissolved)
                    {
                        //房间不存在
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseDissolveVoteTimer, 房间不存在!  houseId = " + houseId);
                        delHouseIdList.Add(houseId);
                        continue;
                    }
                    if (wordPlateHouse.voteBeginTime == DateTime.Parse("1970-01-01 00:00:00"))
                    {
                        //房间没有发起投票
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseDissolveVoteTimer, 房间没有发起投票! houseId = " + houseId);
                        delHouseIdList.Add(houseId);
                        continue;
                    }
                    int dissolveVoteTime = this.houseDissolveVoteTime;
                    if (wordPlateHouse.businessId > 0)
                    {
                        dissolveVoteTime = this.businessDissolveVoteTime;
                    }
                    TimeSpan span = DateTime.Now.Subtract(wordPlateHouse.voteBeginTime);
                    if (span.TotalSeconds < dissolveVoteTime)
                    {
                        //时间未到
                        continue;
                    }
                    OnHouseEndSettlement(wordPlateHouse, WordPlateHouseStatus.EWPS_Dissolved);
                    delHouseIdList.Add(houseId);
                }
                else
                {
                    delHouseIdList.Add(houseId);
                }
            }
            if (delHouseIdList.Count > 0)
            {
                DelDissolveVoteHouse(delHouseIdList);
            }
        }
        public void AddDissolveVoteHouse(ulong houseId)
        {
            if (!houseVateList.Contains(houseId))
            {
                houseVateList.Add(houseId);
                if (houseVateList.Count > 0 && !TimerManager.Instance.Exist(TimerId.HouseVote))
                {
                    TimerManager.Instance.Regist(TimerId.HouseVote, 0, 3000, int.MaxValue, OnHouseDissolveVoteTimer, null, null, null);
                }
            }
        }
        public void DelDissolveVoteHouse(ulong delHouseId)
        {
            if (houseVateList.Exists(element => element == delHouseId))
            {
                List<ulong> delHouseIdList = new List<ulong>();
                delHouseIdList.Add(delHouseId);
                DelDissolveVoteHouse(delHouseIdList);
            }
        }
        public void DelDissolveVoteHouse(List<ulong> delHouseIdList)
        {
            foreach (ulong delHouseId in delHouseIdList)
            {
                houseVateList.RemoveAll(element => element == delHouseId);
            }
            if (houseVateList.Count == 0 && TimerManager.Instance.Exist(TimerId.HouseVote))
            {
                TimerManager.Instance.Remove(TimerId.HouseVote);
            }
        }
        public void OnHouseEndSettlement(WordPlateHouse wordPlateHouse, WordPlateHouseStatus houseStatus)
        {
            msg_proxy.OnHouseEndSettlement(wordPlateHouse, houseStatus);
        }
        private void OnHouseActionTimer(object obj)
        {
            List<ulong> delHouseIdList = new List<ulong>();
            foreach (HouseActionNode houseAction in houseActionList)
            {
                House house = HouseManager.Instance.GetHouseById(houseAction.houseId);
                if (house == null)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnHouseActionTimer, 房间不存在! houseId = " + houseAction.houseId);
                    delHouseIdList.Add(houseAction.houseId);
                    continue;
                }
                if (house.houseType == HouseType.WordPlateHouse)
                {
                    WordPlateHouse wordPlateHouse = house as WordPlateHouse;
                    if (wordPlateHouse == null || wordPlateHouse.houseStatus >= WordPlateHouseStatus.EWPS_Dissolved)
                    {
                        //房间不存在
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseActionTimer, 房间不存在!  houseId = " + houseAction.houseId);
                        delHouseIdList.Add(houseAction.houseId);
                        continue;
                    }
                    if (wordPlateHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
                    {
                        //房间正在投票，等会再操作
                        continue;
                    }
                    bool bDel = true;
                    if (houseAction.actionId == 1)
                    {
                        msg_proxy.BeginWordPlates(wordPlateHouse);
                    }
                    else if (houseAction.actionId == 2)
                    {
                        if(wordPlateHouse.waitTime == 0)
                        {
                            bDel = msg_proxy.GiveOffWordPlatePlayer(wordPlateHouse, houseAction.parameter);
                        }
                        else
                        {
                            wordPlateHouse.waitTime--;
                            bDel = false;
                        }
                    }
                    //else if (houseAction.actionId == 3)
                    //{
                    //    if (wordPlateHouse.voteBeginTime == DateTime.Parse("1970-01-01 00:00:00"))
                    //    {
                    //        //房间没有发起投票
                    //        ServerUtil.RecordLog(LogType.Debug, "OnHouseDissolveVoteTimer, 房间没有发起投票! houseId = " + houseAction.houseId);
                    //        delHouseIdList.Add(houseAction.houseId);
                    //        continue;
                    //    }
                    //    int dissolveVoteTime = this.houseDissolveVoteTime;
                    //    if (wordPlateHouse.businessId > 0)
                    //    {
                    //        dissolveVoteTime = this.businessDissolveVoteTime;
                    //    }
                    //    TimeSpan span = DateTime.Now.Subtract(wordPlateHouse.voteBeginTime);
                    //    if (span.TotalSeconds < dissolveVoteTime)
                    //    {
                    //        //时间未到
                    //        continue;
                    //    }
                    //    OnHouseEndSettlement(wordPlateHouse, WordPlateHouseStatus.EWPS_Dissolved);
                    //}
                    //else if (houseAction.actionId == 4)
                    //{
                    //    msg_proxy.DissolveHouseVote(wordPlateHouse, (VoteStatus)houseAction.parameter);
                    //}
                    else if (houseAction.actionId == 5)
                    {
                        msg_proxy.SettlementWordPlateHouse(wordPlateHouse);
                    }
                    if(bDel)
                    {
                        delHouseIdList.Add(houseAction.houseId);
                        wordPlateHouse.waitTime = 0;
                    }
                }
                else
                {
                    delHouseIdList.Add(houseAction.houseId);
                }
            }
            if (delHouseIdList.Count > 0)
            {
                DelActionHouse(delHouseIdList);
            }
        }
        public void AddActionHouse(ulong houseId, byte actionId, int param = 0)
        {
            HouseActionNode houseAction = houseActionList.Find(element => element.houseId == houseId);
            if (houseAction != null)
            {
                houseAction.actionId = actionId;
                houseAction.parameter = param;
            }
            else
            {
                houseAction = new HouseActionNode(houseId, actionId, param);
                houseActionList.Add(houseAction);
                CheckHouseActionTimer();
            }
        }
        public void CheckHouseActionTimer()
        {
            if (!TimerManager.Instance.Exist(TimerId.HouseAction))
            {
                TimerManager.Instance.Regist(TimerId.HouseAction, 0, 1000, int.MaxValue, OnHouseActionTimer, null, null, null);
            }
        }
        public void DelActionHouse(List<ulong> delHouseIdList)
        {
            foreach (ulong delHouseId in delHouseIdList)
            {
                houseActionList.RemoveAll(element => element.houseId == delHouseId);
            }
            if (houseActionList.Count == 0 && TimerManager.Instance.Exist(TimerId.HouseAction))
            {
                TimerManager.Instance.Remove(TimerId.HouseAction);
            }
        }
        private void OnHouseAutomaticOperateTimer(object obj)
        {
            //玩家行为
            List<House> wordPlateHouseList = HouseManager.Instance.GetHouseListByCondition(element => element.businessId > 0 && element.houseType == HouseType.WordPlateHouse &&
                element.operateBeginTime != DateTime.Parse("1970-01-01 00:00:00") && element.GetWordPlateHouseStatus() != WordPlateHouseStatus.EWPS_FreeBureau);
            if (wordPlateHouseList != null && wordPlateHouseList.Count > 0)
            {
                foreach (House house in wordPlateHouseList)
                {
                    WordPlateHouse wordPlateHouse = house as WordPlateHouse;
                    if (wordPlateHouse == null || wordPlateHouse.houseStatus >= WordPlateHouseStatus.EWPS_Dissolved)
                    {
                        //房间不存在
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间不存在!");
                        continue;
                    }
                    if (wordPlateHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
                    {
                        //房间正在发起投票
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间正在发起投票!");
                        wordPlateHouse.operateBeginTime.AddSeconds(1);
                        continue;
                    }
                    TimeSpan span = DateTime.Now.Subtract(wordPlateHouse.operateBeginTime);
                    if (wordPlateHouse.houseStatus == WordPlateHouseStatus.EWPS_BeginBureau)
                    {
                        if (wordPlateHouse.currentShowCard == -1)
                        {
                            WordPlateOperatNode wordPlateOperatNode = wordPlateHouse.GetPlayerWordPlateOperatNode();
                            if (wordPlateOperatNode != null)
                            {
                                WordPlatePlayer wordPlatePlayer = wordPlateHouse.GetWordPlatePlayer(wordPlateOperatNode.playerIndex);
                                if (wordPlatePlayer == null)
                                {
                                    //房间等待操作玩家不存在
                                    ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间等待操作玩家不存在! playerIndex = " + wordPlateOperatNode.playerIndex);
                                    continue;
                                }
                                if ((span.TotalSeconds < 15 && !wordPlatePlayer.bHosted) || (wordPlatePlayer.bHosted && span.TotalSeconds < 2))
                                {
                                    //时间未到
                                    continue;
                                }
                                SetWordPlatePlayerHosted(wordPlatePlayer);
                                List<int> wordPlateList = new List<int>();
                                WordPlateOperatType operatType = WordPlateOperatType.EWPO_None;
                                if (wordPlateOperatNode.operatType == WordPlateOperatType.EWPO_Hu && wordPlateHouse.currentWordPlate != null)
                                {
                                    wordPlateList.Add(wordPlateHouse.currentWordPlate.GetWordPlateNode());
                                    operatType = WordPlateOperatType.EWPO_Hu;
                                }
                                msg_proxy.OnReqOperatWordPlate(wordPlateHouse, wordPlatePlayer, operatType, wordPlateList);
                            }
                        }
                        else
                        {
                            WordPlatePlayer wordPlatePlayer = wordPlateHouse.GetWordPlatePlayer(wordPlateHouse.currentShowCard);
                            if (wordPlatePlayer == null || wordPlatePlayer.housePlayerStatus != WordPlatePlayerStatus.WordPlateWaitCard)
                            {
                                //房间玩家不存在
                                ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间玩家不存在! currentShowCard = " + wordPlateHouse.currentShowCard);
                                continue;
                            }
                            if ((span.TotalSeconds < 15 && !wordPlatePlayer.bHosted) || (wordPlatePlayer.bHosted && span.TotalSeconds < 2))
                            {
                                //时间未到
                                continue;
                            }
                            SetWordPlatePlayerHosted(wordPlatePlayer);
                            if (!wordPlatePlayer.m_bGiveUpWin && wordPlatePlayer.WinHandTileCheck())
                            {
                                List<int> wordPlateList = new List<int>();
                                msg_proxy.OnReqOperatWordPlateByMyself(wordPlateHouse, wordPlatePlayer, WordPlateOperatType.EWPO_Hu, wordPlateList);
                                continue;
                            }
                            int showWordPlateTile = wordPlatePlayer.GetPlayerHandWordPlateEndNode();
                            if (showWordPlateTile != 0)
                            {
                                msg_proxy.OnReqShowWordPlate(wordPlateHouse, wordPlatePlayer, new WordPlateTile(showWordPlateTile));
                            }
                            else
                            {
                                //要出的牌有误
                                ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 要出的牌有误! showWordPlateTile = " + showWordPlateTile);
                                continue;
                            }
                        }
                    }
                    else if (wordPlateHouse.houseStatus == WordPlateHouseStatus.EWPS_Settlement)
                    {
                        if (span.TotalSeconds < 5)
                        {
                            //时间未到
                            continue;
                        }
                        WordPlatePlayer wordPlatePlayer = wordPlateHouse.GetWordPlatePlayerByCondition(element => element.housePlayerStatus != WordPlatePlayerStatus.WordPlateReady);
                        if (wordPlatePlayer != null)
                        {
                            msg_proxy.OnReqReadyWordPlateHouse(wordPlateHouse, wordPlatePlayer);
                        }
                    }
                }
            }
            else if (TimerManager.Instance.Exist(TimerId.HouseAutomaticOperate))
            {
                TimerManager.Instance.Remove(TimerId.HouseAutomaticOperate);
            }
        }
        public void CheckHouseOperateTimer()
        {
            if (!TimerManager.Instance.Exist(TimerId.HouseAutomaticOperate))
            {
                TimerManager.Instance.Regist(TimerId.HouseAutomaticOperate, 0, 2000, int.MaxValue, OnHouseAutomaticOperateTimer, null, null, null);
            }
        }
        public void OnRecvDissolveWordPlateHouse(WordPlateHouse wordPlateHouse)
        {
            foreach (WordPlatePlayer housePlayer in wordPlateHouse.GetWordPlatePlayer())
            {
                Summoner sender = SummonerManager.Instance.GetSummonerByUserId(housePlayer.userId);
                if (sender != null)
                {
                    sender.houseId = 0;
                    msg_proxy.OnRecvQuitWordPlateHouse(housePlayer.summonerId, housePlayer.proxyServerId, 0);
                }
                msg_proxy.OnRequestSaveHouseId(housePlayer.userId, 0);
            }
            OnRecvGMDissolveWordPlateHouse(wordPlateHouse);
        }
        public void OnRecvGMDissolveWordPlateHouse(WordPlateHouse wordPlateHouse)
        {
            wordPlateHouse.houseStatus = WordPlateHouseStatus.EWPS_GMDissolved;
            //保存房间状态
            msg_proxy.OnRequestSaveWordPlateHouseStatus(wordPlateHouse.houseId, wordPlateHouse.houseStatus);
            HouseManager.Instance.RemoveHouse(wordPlateHouse.houseId);
        }
        private void InitWordPlateBureauByHouseCard(string wordPlateBureauByHouseCard)
        {
            bureauByHouseCardList.Clear();
            string[] valueArr = wordPlateBureauByHouseCard.Split('|');
            for (int i = 0; i < valueArr.Length; ++i)
            {
                string[] valueList = valueArr[i].Split(',');
                if (2 == valueList.Length)
                {
                    BureauByHouseCard node = new BureauByHouseCard();
                    int.TryParse(valueList[0], out node.bureau);
                    int.TryParse(valueList[1], out node.houseCard);
                    bureauByHouseCardList.Add(node);
                }
            }
        }
        private void InitWordPlateBasaWinScore(string whzWinBasaScore)
        {
            whzBasaScoreList.Clear();
            string[] valueArr = whzWinBasaScore.Split(',');
            for (int i = 0; i < valueArr.Length; ++i)
            {
                int basaScore = 0;
                int.TryParse(valueArr[i], out basaScore);
                whzBasaScoreList.Add(basaScore);
            }
        }
        private void InitWordPlateMaxWinScore(string whzWinMaxScore)
        {
            whzMaxScoreList.Clear();
            string[] valueArr = whzWinMaxScore.Split(',');
            for (int i = 0; i < valueArr.Length; ++i)
            {
                int maxScore = 0;
                int.TryParse(valueArr[i], out maxScore);
                whzMaxScoreList.Add(maxScore);
            }
        }
        public int GetHouseCard(int maxBureau)
        {
            BureauByHouseCard node = bureauByHouseCardList.Find(element => element.bureau == maxBureau);
            if (node != null)
            {
                return node.houseCard;
            }
            return int.MaxValue;
        }
        public void OnRequestHouseInfo(int dbServerID)
        {
            if (!alreadyRequestHouseInfo)
            {
                msg_proxy.OnRequestHouseInfo(dbServerID);
                alreadyRequestHouseInfo = true;
            }
            else
            {
                ModuleManager.Get<MainCityMain>().OnNotifyDBClearHouse(msg_proxy.root.ServerID, dbServerID);
            }
        }
        public bool CheckOpenDelHouseCard()
        {
            return ModuleManager.Get<MainCityMain>().CheckOpenDelHouseCard();
        }
        public bool CheckOpenCreateHouse()
        {
            return ModuleManager.Get<MainCityMain>().CheckOpenCreateHouse();
        }
        public TicketsNode GetTickets(int businessId, int rank)
        {
            return ModuleManager.Get<SpecialActivitiesMain>().GetTickets(businessId, rank);
        }
        public WordPlatePlayerShowNode GetPlayerShowNode(WordPlatePlayer housePlayer)
        {
            WordPlatePlayerShowNode playerShowNode = new WordPlatePlayerShowNode();
            playerShowNode.index = housePlayer.index;
            playerShowNode.nickName = housePlayer.nickName;
            playerShowNode.summonerId = housePlayer.summonerId;
            playerShowNode.sex = housePlayer.sex;
            playerShowNode.ip = housePlayer.ip;
            playerShowNode.housePlayerStatus = housePlayer.housePlayerStatus;
            playerShowNode.allIntegral = housePlayer.allIntegral;
            playerShowNode.lineType = housePlayer.lineType;
            return playerShowNode;
        }
        public WordPlateMyPlayerOnlineNode GetMyPlayerOnlineNode(WordPlatePlayer housePlayer)
        {
            WordPlateMyPlayerOnlineNode myPlayerOnline = new WordPlateMyPlayerOnlineNode();
            myPlayerOnline.index = housePlayer.index;
            myPlayerOnline.housePlayerStatus = housePlayer.housePlayerStatus;
            myPlayerOnline.allIntegral = housePlayer.allIntegral;
            myPlayerOnline.voteStatus = housePlayer.voteStatus;
            myPlayerOnline.bDeadHand = housePlayer.m_bDeadHand;
            myPlayerOnline.bGiveUpWin = housePlayer.m_bGiveUpWin;
            housePlayer.GetPlayerHandTileList(myPlayerOnline.playerWordPlateList);
            housePlayer.GetPlayerWordPlateMeldList(myPlayerOnline.displayWordPlateList);
            housePlayer.GetPlayerShowTileList(myPlayerOnline.showWordPlateList);
            housePlayer.GetPlayerPassChowTileList(myPlayerOnline.passChowTileList);
            myPlayerOnline.bHosted = housePlayer.bHosted;

            return myPlayerOnline;
        }
        public WordPlatePlayerOnlineNode GetPlayerOnlineNode(WordPlatePlayer housePlayer, bool bOnLineFlag = false)
        {
            WordPlatePlayerOnlineNode playerOnlineNode = new WordPlatePlayerOnlineNode();
            playerOnlineNode.index = housePlayer.index;
            playerOnlineNode.nickName = housePlayer.nickName;
            playerOnlineNode.summonerId = housePlayer.summonerId;
            playerOnlineNode.sex = housePlayer.sex;
            playerOnlineNode.ip = housePlayer.ip;
            playerOnlineNode.housePlayerStatus = housePlayer.housePlayerStatus;
            playerOnlineNode.allIntegral = housePlayer.allIntegral;
            playerOnlineNode.voteStatus = housePlayer.voteStatus;
            playerOnlineNode.bDeadHand = housePlayer.m_bDeadHand;
            housePlayer.GetPlayerWordPlateMeldList(playerOnlineNode.displayWordPlateList);
            housePlayer.GetPlayerShowTileList(playerOnlineNode.showWordPlateList);
            if (bOnLineFlag)
            {
                playerOnlineNode.lineType = LineType.OnLine;
            }
            else
            {
                playerOnlineNode.lineType = housePlayer.lineType;
            }
            return playerOnlineNode;
        }
        public List<int> GetWordPlateNode(List<WordPlateTile> wordPlateTileList)
        {
            List<int> wordPlateList = new List<int>();
            if (wordPlateTileList == null || wordPlateTileList.Count <= 0)
            {
                return wordPlateList;
            }
            foreach (WordPlateTile tile in wordPlateTileList)
            {
                wordPlateList.Add(tile.GetWordPlateNode());
            }
            return wordPlateList;
        }
        public WordPlateOperatNode GetWordPlateOperatNode(WordPlateHouse wordPlateHouse, List<WordPlateOperatNode> wordPlateOperatList)
        {
            int playerIndex = wordPlateHouse.currentWhoPlay;
            for (int i = 0; i < wordPlateHouse.GetHousePlayerCount(); ++i)
            {
                WordPlateOperatNode operatNode = wordPlateOperatList.Find(element => element.playerIndex == playerIndex);
                if (operatNode != null)
                {
                    return operatNode;
                }
                playerIndex = wordPlateHouse.GetNextHousePlayerIndex(playerIndex);
            }
            return wordPlateOperatList.FirstOrDefault();
        }
        private void SetWordPlatePlayerHosted(WordPlatePlayer player)
        {
            if (!player.bHosted)
            {
                player.bHosted = true;
                ModuleManager.Get<MainCityMain>().OnRecvPlayerHostedStatus(player.summonerId, player.proxyServerId, player.bHosted);
            }
        }
        public void SendPassChowWordPlate(ulong summonerId, int proxyServerId, WordPlateTile wordPlateTile)
        {
            msg_proxy.SendPassChowWordPlate(summonerId, proxyServerId, wordPlateTile);
        }
        public void TryJoinWordPlateHouse(Summoner sender, WordPlateHouse wordPlateHouse)
        {
            if (sender == null || wordPlateHouse == null) return;

            msg_proxy.JoinWordPlateHouse(sender, wordPlateHouse);
        }
        public void CheckFunction()
        {
            List<WordPlateTile> tileList = new List<WordPlateTile>();
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Two));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Three));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Four));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Five));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Six));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Eight));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Eight));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Three));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Three));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Three));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Two));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Seven));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Ten));
            tileList.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_One));

            List<WordPlateMeld> meldList = new List<WordPlateMeld>();

            List<WordPlateTile> tileList1 = new List<WordPlateTile>();
            tileList1.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Three));
            tileList1.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_Two));
            tileList1.Add(new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_One));
            WordPlateMeld meld1 = new WordPlateMeld(tileList1, PlateMeldType.EPM_Sequence);
            meldList.Add(meld1);

            List<WordPlateTile> tileList2 = new List<WordPlateTile>();
            tileList2.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Four));
            tileList2.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Four));
            tileList2.Add(new WordPlateTile(PlateDescType.EPD_Big, PlateNumType.EPN_Four));
            WordPlateMeld meld2 = new WordPlateMeld(tileList2, PlateMeldType.EPM_Pong);
            meldList.Add(meld2);

            WhzWordPlateStrategy strategy = new WhzWordPlateStrategy();
            tileList.Sort(strategy.CompareTile);
            bool bresult = strategy.AnalyseHandTile(tileList, meldList, 6, true, new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_One), false);
            if (bresult)
            {
                List<WordPlateMeld> _meldList = new List<WordPlateMeld>();
                _meldList.Add(meld1);
                _meldList.Add(meld2);
                List<WordPlateHuMeld> huList = strategy.GetHuMeld(tileList, _meldList, 6, true, false, new WordPlateTile(PlateDescType.EPD_Small, PlateNumType.EPN_One), false);
                huList.Clear();
            }
            return;
        }
    }
}
#endif