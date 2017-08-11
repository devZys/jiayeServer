#if RUNFAST
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.RoomCard;
using LegendServer.Util;
using LegendServerLogic.Actor.Summoner;
using LegendServerLogic.Entity.Base;
using LegendServerLogic.Entity.Houses;
using LegendServerLogic.Entity.Players;
using LegendServerLogic.MainCity;
using LegendServerLogic.SpecialActivities;
using System;
using System.Collections.Generic;

namespace LegendServerLogic.RunFast
{
    public class RunFastMain : Module
    {
        public RunFastMsgProxy msg_proxy;
        public List<BureauByHouseCard> bureauByHouseCardList = new List<BureauByHouseCard>();
        public int loseIntegral;
        public int houseDissolveVoteTime;
        public int businessDissolveVoteTime;
        public List<ulong> houseVateList = new List<ulong>();
        private bool alreadyRequestHouseInfo = false;
        public RunFastMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new RunFastMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
            //跑得快开局数对应的房卡消耗
            RunFastConfigDB runFastConfig = DBManager<RunFastConfigDB>.Instance.GetSingleRecordInCache(element => element.key == "RunFastBureauByHouseCard");
            if (runFastConfig != null && !string.IsNullOrEmpty(runFastConfig.value))
            {
                InitRunFastBureauByHouseCard(runFastConfig.value);
            }
            //跑得快炸弹输得人要扣的积分
            loseIntegral = 10;
            runFastConfig = DBManager<RunFastConfigDB>.Instance.GetSingleRecordInCache(element => element.key == "RunFastZhaDanLoseIntegral");
            if (runFastConfig != null && !string.IsNullOrEmpty(runFastConfig.value))
            {
                int.TryParse(runFastConfig.value, out loseIntegral);
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
            MsgFactory.Regist(MsgID.D2L_ReplyHouseInfo, new MsgComponent(msg_proxy.OnReplyHouseInfo, typeof(ReplyHouseInfo_D2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyHousePlayerAndBureau, new MsgComponent(msg_proxy.OnReplyHousePlayerAndBureau, typeof(ReplyHousePlayerAndBureau_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestCreateRunFastHouse, new MsgComponent(msg_proxy.OnReqCreateRunFastHouse, typeof(RequestCreateRunFastHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestJoinRunFastHouse, new MsgComponent(msg_proxy.OnReqJoinRunFastHouse, typeof(RequestJoinRunFastHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestShowRunFastCard, new MsgComponent(msg_proxy.OnReqShowRunFastCard, typeof(RequestShowRunFastCard_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestPassRunFastCard, new MsgComponent(msg_proxy.OnReqPassRunFastCard, typeof(RequestPassRunFastCard_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestQuitRunFastHouse, new MsgComponent(msg_proxy.OnReqQuitRunFastHouse, typeof(RequestQuitRunFastHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestReadyRunFastHouse, new MsgComponent(msg_proxy.OnReqReadyRunFastHouse, typeof(RequestReadyRunFastHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestRunFastHouseInfo, new MsgComponent(msg_proxy.OnReqRunFastHouseInfo, typeof(RequestRunFastHouseInfo_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestDissolveHouseVote, new MsgComponent(msg_proxy.OnReqDissolveHouseVote, typeof(RequestDissolveHouseVote_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestRunFastOverallRecord, new MsgComponent(msg_proxy.OnReqRunFastOverallRecord, typeof(RequestRunFastOverallRecord_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyRunFastOverallRecord, new MsgComponent(msg_proxy.OnReplyRunFastOverallRecord, typeof(ReplyRunFastOverallRecord_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestRunFastBureauRecord, new MsgComponent(msg_proxy.OnReqRunFastBureauRecord, typeof(RequestRunFastBureauRecord_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyRunFastBureauRecord, new MsgComponent(msg_proxy.OnReplyRunFastBureauRecord, typeof(ReplyRunFastBureauRecord_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestRunFastBureauPlayback, new MsgComponent(msg_proxy.OnReqRunFastBureauPlayback, typeof(RequestRunFastBureauPlayback_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyRunFastBureauPlayback, new MsgComponent(msg_proxy.OnReplyRunFastBureauPlayback, typeof(ReplyRunFastBureauPlayback_D2L)));
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
                if (house.houseType == HouseType.RunFastHouse)
                {
                    RunFastHouse runFastHouse = house as RunFastHouse;
                    if (runFastHouse == null || runFastHouse.houseStatus >= RunFastHouseStatus.RFHS_Dissolved)
                    {
                        //房间不存在
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseDissolveVoteTimer, 房间不存在!  houseId = " + houseId);
                        delHouseIdList.Add(houseId);
                        continue;
                    }
                    if (runFastHouse.voteBeginTime == DateTime.Parse("1970-01-01 00:00:00"))
                    {
                        //房间没有发起投票
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseDissolveVoteTimer, 房间没有发起投票! houseId = " + houseId);
                        delHouseIdList.Add(houseId);
                        continue;
                    }
                    TimeSpan span = DateTime.Now.Subtract(runFastHouse.voteBeginTime);
                    int dissolveVoteTime = this.houseDissolveVoteTime;
                    if (runFastHouse.businessId > 0)
                    {
                        dissolveVoteTime = this.businessDissolveVoteTime;
                    }
                    if (span.TotalSeconds < dissolveVoteTime)
                    {
                        //时间未到
                        continue;
                    }
                    OnHouseEndSettlement(runFastHouse, RunFastHouseStatus.RFHS_Dissolved);
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
            foreach(ulong delHouseId in delHouseIdList)
            {
                houseVateList.RemoveAll(element => element == delHouseId);
            }
            if (houseVateList.Count == 0 && TimerManager.Instance.Exist(TimerId.HouseVote))
            {
                TimerManager.Instance.Remove(TimerId.HouseVote);
            }
        }
        private void OnHouseAutomaticOperateTimer(object obj)
        {
            //玩家行为
            List<House> runFastHouseList = HouseManager.Instance.GetHouseListByCondition(element => element.businessId > 0 && element.houseType == HouseType.RunFastHouse &&
                element.operateBeginTime != DateTime.Parse("1970-01-01 00:00:00") && element.GetRunFastHouseStatus() != RunFastHouseStatus.RFHS_FreeBureau);
            if (runFastHouseList != null && runFastHouseList.Count > 0)
            {
                foreach(House house in runFastHouseList)
                {
                    RunFastHouse runFastHouse = house as RunFastHouse;
                    if (runFastHouse == null || runFastHouse.houseStatus >= RunFastHouseStatus.RFHS_Dissolved)
                    {
                        //房间不存在
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间不存在!");
                        continue;
                    }
                    if (runFastHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
                    {
                        //房间正在发起投票
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间正在发起投票!");
                        runFastHouse.operateBeginTime.AddSeconds(1);
                        continue;
                    }
                    TimeSpan span = DateTime.Now.Subtract(runFastHouse.operateBeginTime);
                    if (runFastHouse.houseStatus == RunFastHouseStatus.RFHS_BeginBureau)
                    {
                        RunFastPlayer player = runFastHouse.GetRunFastPlayer(runFastHouse.currentShowCard);
                        if (player == null)
                        {
                            //当前操作玩家不存在
                            ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 当前操作玩家不存在! currentShowCard = " + runFastHouse.currentShowCard);
                            continue;
                        }
                        if (runFastHouse.currentWhoPlay == -1 || runFastHouse.currentWhoPlay == runFastHouse.currentShowCard)
                        {
                            if ((span.TotalSeconds < 15 && !player.bHosted) || (player.bHosted &&span.TotalSeconds < 2))
                            {
                                //时间未到
                                continue;
                            }
                            SetRunFastPlayerHosted(player);
                            //自主出牌
                            List<Card> showCardList = new List<Card>();
                            showCardList.AddRange(player.cardList);
                            if (!IsAllOut(player.cardList, showCardList, runFastHouse.currentCardList, runFastHouse.pokerGroupType, runFastHouse.currentBureau, runFastHouse.CheckHousePropertyType(RunFastHousePropertyType.ERFHP_SpadeThree)))
                            {
                                showCardList.Clear();
                                Card showCard = null;
                                if (runFastHouse.currentBureau == 1 && runFastHouse.currentWhoPlay == -1 && runFastHouse.CheckHousePropertyType(RunFastHousePropertyType.ERFHP_SpadeThree))
                                {
                                    showCard = player.cardList.Find(element => (element.suit == Suit.Spade && element.rank == Rank.Three));
                                }
                                if (showCard == null)
                                {
                                    showCard = player.cardList[0];
                                }
                                showCardList.Add(new Card { suit = showCard.suit, rank = showCard.rank });
                            }
                            msg_proxy.OnReqShowRunFastCard(runFastHouse, player, showCardList);
                        }
                        else
                        {
                            if (player.housePlayerStatus == HousePlayerStatus.WaitShowCard)
                            {
                                if ((span.TotalSeconds < 15 && !player.bHosted) || (player.bHosted && span.TotalSeconds < 2))
                                {
                                    //时间未到
                                    continue;
                                }
                                SetRunFastPlayerHosted(player);
                                List<Card> showCardList = new List<Card>();
                                showCardList.AddRange(player.cardList);
                                if (!IsAllOut(player.cardList, showCardList, runFastHouse.currentCardList, runFastHouse.pokerGroupType, runFastHouse.currentBureau, runFastHouse.CheckHousePropertyType(RunFastHousePropertyType.ERFHP_SpadeThree), false))
                                {
                                    showCardList = GetShowCardList(player.cardList, runFastHouse.currentCardList, runFastHouse.pokerGroupType);
                                }
                                msg_proxy.OnReqShowRunFastCard(runFastHouse, player, showCardList);
                            }
                            else if (player.housePlayerStatus == HousePlayerStatus.Pass)
                            {
                                if (span.TotalSeconds < 15 && !player.bHosted)
                                {
                                    //时间未到
                                    continue;
                                }
                                SetRunFastPlayerHosted(player);
                                msg_proxy.OnReqPassRunFastCard(runFastHouse, player);
                            }
                            else
                            {
                                //当前操作玩家状态有误
                                ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 当前操作玩家状态有误! housePlayerStatus = " + player.housePlayerStatus);
                                continue;
                            }
                        }
                    }
                    else if (runFastHouse.houseStatus == RunFastHouseStatus.RFHS_Settlement)
                    {
                        if (span.TotalSeconds < 5)
                        {
                            //时间未到
                            continue;
                        }
                        RunFastPlayer housePlayer = runFastHouse.GetRunFastPlayerByCondition(element => element.housePlayerStatus != HousePlayerStatus.Ready);
                        if (housePlayer != null)
                        {
                            msg_proxy.OnReqReadyRunFastHouse(runFastHouse, housePlayer);
                            continue;
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
        //是否可以全部出完
        public bool IsAllOut(List<Card> headCardList, List<Card> showCardList, List<Card> currentCardList, PokerGroupType pokerGroupType, int currentBureau, bool bSpadeThree, bool bIsMySelf = true)
        {
            bool isReturn;
            isReturn = MyRunFastDeck.GetRunFastDeck().IsShowPokersRules(headCardList, showCardList, currentCardList, pokerGroupType, currentBureau, bSpadeThree, bIsMySelf);

            if (isReturn)
            {
                isReturn = MyRunFastDeck.GetRunFastDeck().IsShowFinallyPokersRules(headCardList);
            }

            return isReturn;

        }
        public List<Card> GetShowCardList(List<Card> playerCardList, List<Card> lastCardList, PokerGroupType lastGroupType)
        {
            List<Card> showCardList = new List<Card>();
            UnshieldedCard unshieldedCard = MyRunFastDeck.GetRunFastDeck().GetUnshieldedCardByType(playerCardList, lastCardList, lastGroupType);
            if (unshieldedCard != null && unshieldedCard.type != PokerGroupType.Error && (unshieldedCard.rankZhaDanList.Count > 0 || unshieldedCard.rankList.Count > 0))
            {
                List<PromptCard> promptCardList = new List<PromptCard>();
                PokerGroupType pokerGroupType = PokerGroupType.Error;
                if (unshieldedCard.rankZhaDanList.Count > 0)
                {
                    pokerGroupType = PokerGroupType.ZhaDan;
                    MyRunFastDeck.GetRunFastDeck().GetPromptCardByType(playerCardList, lastCardList, pokerGroupType, unshieldedCard.rankZhaDanList, promptCardList);
                }
                if (unshieldedCard.type != PokerGroupType.ZhaDan && unshieldedCard.rankList.Count > 0)
                {
                    pokerGroupType = lastGroupType;
                    MyRunFastDeck.GetRunFastDeck().GetPromptCardByType(playerCardList, lastCardList, lastGroupType, unshieldedCard.rankList, promptCardList);
                }
                if (pokerGroupType != PokerGroupType.Error && promptCardList.Count > 0)
                {
                    if (pokerGroupType == PokerGroupType.SanZhang)
                    {
                        showCardList.AddRange(promptCardList[promptCardList.Count - 1].cardList);
                        int count = 0;
                        for(int i = playerCardList.Count; i > 0; --i)
                        {
                            Card card = playerCardList[i - 1];
                            if (card != null && !showCardList.Exists(element => (element.suit == card.suit && element.rank == card.rank)))
                            {
                                showCardList.Add(new Card { suit = card.suit, rank = card.rank });
                                count++;
                            }
                            if (count == 2)
                            {
                                //带2个就够了
                                break;
                            }
                        }
                    }
                    else if (pokerGroupType == PokerGroupType.ZhaDan || pokerGroupType == PokerGroupType.DanZhang)
                    {
                        showCardList.AddRange(promptCardList[0].cardList);
                    }
                    else
                    {
                        showCardList.AddRange(promptCardList[promptCardList.Count - 1].cardList);
                    }
                }
            }
            return showCardList;
        }
        private void SetRunFastPlayerHosted(RunFastPlayer player)
        {
            if (!player.bHosted)
            {
                player.bHosted = true;
                ModuleManager.Get<MainCityMain>().OnRecvPlayerHostedStatus(player.summonerId, player.proxyServerId, player.bHosted);
            }
        }
        private void InitRunFastBureauByHouseCard(string runFastBureauByHouseCard)
        {
            bureauByHouseCardList.Clear();
            string[] valueArr = runFastBureauByHouseCard.Split('|');
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
        public PlayerShowNode GetPlayerShowNode(RunFastPlayer housePlayer)
        {
            PlayerShowNode playerShowNode = new PlayerShowNode();
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
        public MyPlayerOnlineNode GetMyPlayerOnlineNode(RunFastPlayer housePlayer)
        {
            MyPlayerOnlineNode myPlayerOnline = new MyPlayerOnlineNode();
            myPlayerOnline.index = housePlayer.index;
            myPlayerOnline.housePlayerStatus = housePlayer.housePlayerStatus;
            myPlayerOnline.allIntegral = housePlayer.allIntegral;
            myPlayerOnline.voteStatus = housePlayer.voteStatus;
            myPlayerOnline.playerCardList.AddRange(housePlayer.cardList);
            myPlayerOnline.bHosted = housePlayer.bHosted;

            return myPlayerOnline;
        }
        public PlayerOnlineNode GetPlayerOnlineNode(RunFastPlayer housePlayer, bool bSurplusCardCount)
        {
            PlayerOnlineNode playerOnlineNode = new PlayerOnlineNode();
            playerOnlineNode.index = housePlayer.index;
            playerOnlineNode.nickName = housePlayer.nickName;
            playerOnlineNode.summonerId = housePlayer.summonerId;
            playerOnlineNode.sex = housePlayer.sex;
            playerOnlineNode.ip = housePlayer.ip;
            playerOnlineNode.housePlayerStatus = housePlayer.housePlayerStatus;
            playerOnlineNode.allIntegral = housePlayer.allIntegral;
            playerOnlineNode.voteStatus = housePlayer.voteStatus;
            playerOnlineNode.lineType = housePlayer.lineType;
            if (bSurplusCardCount)
            {
                playerOnlineNode.surplusCardCount = housePlayer.cardList.Count;
            }
            if (housePlayer.cardList.Count == 1)
            {
                playerOnlineNode.bDanZhangFalg = true;
            }
            else
            {
                playerOnlineNode.bDanZhangFalg = false;
            }
            return playerOnlineNode;
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
        public void OnHouseEndSettlement(RunFastHouse runFastHouse, RunFastHouseStatus houseStatus)
        {
            msg_proxy.OnHouseEndSettlement(runFastHouse, houseStatus);
        }
        public void OnRecvDissolveRunFastHouse(RunFastHouse runFastHouse)
        {
            foreach (RunFastPlayer housePlayer in runFastHouse.GetRunFastPlayer())
            {
                Summoner sender = SummonerManager.Instance.GetSummonerByUserId(housePlayer.userId);
                if (sender != null)
                {
                    sender.houseId = 0;
                    msg_proxy.OnRecvQuitRunFastHouse(housePlayer.summonerId, housePlayer.proxyServerId, 0);
                }
                msg_proxy.OnRequestSaveHouseId(housePlayer.userId, 0);
            }
            OnRecvGMDissolveRunFastHouse(runFastHouse);
        }
        public void OnRecvGMDissolveRunFastHouse(RunFastHouse runFastHouse)
        {
            runFastHouse.houseStatus = RunFastHouseStatus.RFHS_GMDissolved;
            //保存房间状态
            msg_proxy.OnRequestSaveRunFastHouseStatus(runFastHouse.houseId, runFastHouse.houseStatus);
            HouseManager.Instance.RemoveHouse(runFastHouse.houseId);
        }
        public int GetZhaDanWinIntegral(int playerCount)
        {
            if (playerCount == RunFastConstValue.RunFastThreePlayer)
            {
                return loseIntegral * 2;
            }
            return loseIntegral;
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
        public List<int> GetRunFastCardList(List<Card> cardList)
        {
            List<int> resultCardList = new List<int>();
            cardList.ForEach(card =>
            {
                resultCardList.Add(((int)card.rank * 10) + (int)card.suit);
            });
            return resultCardList;
        }
        public List<Card> GetRunFastCardList(List<int> cardList)
        {
            List<Card> resultCardList = new List<Card>();
            cardList.ForEach(card =>
            {
                Card node = new Card();
                int rank = card / 10;
                node.rank = (Rank)rank;
                int suit = card % 10;
                node.suit = (Suit)suit;
                resultCardList.Add(node);
            });
            return resultCardList;
        }
        public List<PlayerSaveCardNode> GetRunFastCardList(List<PlayerCardNode> playerInitCardList)
        {
            List<PlayerSaveCardNode> playerSaveCardList = new List<PlayerSaveCardNode>();
            playerInitCardList.ForEach(card =>
            {
                PlayerSaveCardNode node = new PlayerSaveCardNode();
                node.index = card.index;
                node.cardList.AddRange(GetRunFastCardList(card.cardList));
                playerSaveCardList.Add(node);
            });
            return playerSaveCardList;
        }
        public List<PlayerCardNode> GetRunFastCardList(List<PlayerSaveCardNode> playerInitCardList)
        {
            List<PlayerCardNode> playerSaveCardList = new List<PlayerCardNode>();
            playerInitCardList.ForEach(card =>
            {
                PlayerCardNode node = new PlayerCardNode();
                node.index = card.index;
                node.cardList.AddRange(GetRunFastCardList(card.cardList));
                playerSaveCardList.Add(node);
            });
            return playerSaveCardList;
        }
        public void TryJoinRunFastHouse(Summoner sender, RunFastHouse runfastHouse)
        {
            if (sender == null || runfastHouse == null) return;

            msg_proxy.JoinRunFastHouse(sender, runfastHouse);
        }
    }
}
#endif