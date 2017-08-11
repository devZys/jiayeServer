#if RUNFAST
using LegendProtocol;
using LegendServerCompetitionManager;
using LegendServerLogic.Actor.Summoner;
using LegendServerLogic.Distributed;
using LegendServerLogic.Entity.Base;
using LegendServerLogic.Entity.Houses;
using LegendServerLogic.Entity.Players;
using LegendServerLogic.MainCity;
using LegendServerLogic.ServiceBox;
using LegendServerLogic.SpecialActivities;
using LegendServerLogic.UIDAlloc;
using LegendServerLogicDefine;
using System;
using System.Collections.Generic;

namespace LegendServerLogic.RunFast
{
    public class RunFastMsgProxy : ServerMsgProxy
    {
        private RunFastMain main;

        public RunFastMsgProxy(RunFastMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnReqCreateRunFastHouse(int peerId, bool inbound, object msg)
        {
            RequestCreateRunFastHouse_P2L reqMsg = msg as RequestCreateRunFastHouse_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            OnReqCreateRunFastHouse(sender, reqMsg.maxPlayerNum, reqMsg.maxBureau, reqMsg.runFastType, reqMsg.housePropertyType);
        }
        public void OnReqCreateRunFastHouse(Summoner sender, int maxPlayerNum, int maxBureau, RunFastType runFastType, int housePropertyType, int businessId = 0)
        {
            ReplyCreateRunFastHouse_L2P replyMsg = new ReplyCreateRunFastHouse_L2P();

            replyMsg.summonerId = sender.id;

            if (sender.houseId > 0)
            {
                //已经有房间号了
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateRunFastHouse, 已经有房间号了! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (sender.competitionKey > 0)
            {
                if (!CompetitionManager.Instance.IsCompetitionExistByKey(sender.competitionKey, sender.id))
                {
                    //口令无效
                    InitSummonerCompetitionKey(sender);
                }
                else
                {
                    //报名参加了比赛场
                    ServerUtil.RecordLog(LogType.Debug, "OnReqCreateRunFastHouse, 报名参加了比赛场! userId = " + sender.userId + ", competitionKey = " + sender.competitionKey);
                    replyMsg.result = ResultCode.PlayerJoinCompetition;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
            }
            if (!main.CheckOpenCreateHouse())
            {
                //已经关闭创建房间接口
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateRunFastHouse, 已经关闭创建房间接口! userId = " + sender.userId + ", maxPlayerNum = " + maxPlayerNum);
                replyMsg.result = ResultCode.ClosedCreateHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (maxPlayerNum != RunFastConstValue.RunFastThreePlayer && maxPlayerNum != RunFastConstValue.RunFastTwoPlayer)
            {
                //选择最多参战的玩家数有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateRunFastHouse, 选择最多参战的玩家数有误! userId = " + sender.userId + ", maxPlayerNum = " + maxPlayerNum);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            //是否开启扣房卡模式
            if (businessId == 0)
            {
                //商场模式不扣卡
                if (main.CheckOpenDelHouseCard() && (sender.roomCard < main.GetHouseCard(maxBureau)))
                {
                    //房卡不够
                    ServerUtil.RecordLog(LogType.Debug, "OnReqCreateRunFastHouse, 房卡不够! userId = " + sender.userId + ", maxBureau = " + maxBureau);
                    replyMsg.result = ResultCode.HouseCardNotEnough;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
            }  
            //没啥问题 我要开始请求房间Id啦
            ModuleManager.Get<UIDAllocMain>().msg_proxy.RequestUID(UIDType.RoomID, sender.id, new PreRunFastRoomInfo(maxPlayerNum, maxBureau, runFastType, housePropertyType, businessId));
        }
        public bool OnCreateRunFastHouse(ulong summonerId, int houseCardId, PreRunFastRoomInfo runFastRoomInfo)
        {
            Summoner sender = SummonerManager.Instance.GetSummonerById(summonerId);
            if (sender == null) return false;

            ReplyCreateRunFastHouse_L2P replyMsg = new ReplyCreateRunFastHouse_L2P();
            replyMsg.summonerId = sender.id;
            
            if (houseCardId < 100000 || houseCardId >= 1000000)
            {
                //获取房间Id出错
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateRunFastHouse, 获取房间Id出错! userId = " + sender.userId + ", houseCardId = " + houseCardId);
                replyMsg.result = ResultCode.GetHouseIdError;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return false;
            }
            RunFastHouse runFastHouse = new RunFastHouse();
            runFastHouse.houseId = MyGuid.NewGuid(ServiceType.Logic, (uint)root.ServerID);
            runFastHouse.houseCardId = houseCardId;
            runFastHouse.logicId = root.ServerID;
            runFastHouse.maxBureau = runFastRoomInfo.maxBureau;
            runFastHouse.runFastType = runFastRoomInfo.runFastType;
            runFastHouse.maxPlayerNum = runFastRoomInfo.maxPlayerNum;
            runFastHouse.housePropertyType = runFastRoomInfo.housePropertyType;
            runFastHouse.businessId = runFastRoomInfo.businessId;
            runFastHouse.createTime = DateTime.Now;
            runFastHouse.houseStatus = RunFastHouseStatus.RFHS_FreeBureau;
            RunFastPlayer newHousePlayer = runFastHouse.CreatHouse(sender);

            HouseManager.Instance.AddHouse(runFastHouse.houseId, runFastHouse);

            sender.houseId = runFastHouse.houseId;

            replyMsg.result = ResultCode.OK;
            replyMsg.maxBureau = runFastRoomInfo.maxBureau;
            replyMsg.runFastType = runFastRoomInfo.runFastType;
            replyMsg.maxPlayerNum = runFastRoomInfo.maxPlayerNum;
            replyMsg.housePropertyType = runFastRoomInfo.housePropertyType;
            replyMsg.allIntegral = newHousePlayer.allIntegral;
            replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
            replyMsg.houseId = houseCardId;
            replyMsg.onlyHouseId = runFastHouse.houseId;
            replyMsg.businessId = runFastRoomInfo.businessId;
            SendProxyMsg(replyMsg, sender.proxyServerId);
            
            //存数据库
            OnRequestSaveCreateRunFastInfo(runFastHouse, newHousePlayer);
            return true;
        }
        //比赛场专用接口
        public void OnReqCreateRunFastHouse(PlayerInfo playerInfo, RunFastHouse runFastHouse)
        {
            RunFastPlayer newHousePlayer = runFastHouse.CreatHouse(playerInfo);

            HouseManager.Instance.AddHouse(runFastHouse.houseId, runFastHouse);

            Summoner sender = SummonerManager.Instance.GetSummonerById(playerInfo.summonerId);
            if (sender != null)
            {
                //在线
                sender.houseId = runFastHouse.houseId;
                newHousePlayer.proxyServerId = sender.proxyServerId;

                ReplyCreateRunFastHouse_L2P replyMsg = new ReplyCreateRunFastHouse_L2P();
                replyMsg.result = ResultCode.OK;
                replyMsg.summonerId = sender.id;
                replyMsg.maxBureau = runFastHouse.maxBureau;
                replyMsg.runFastType = runFastHouse.runFastType;
                replyMsg.maxPlayerNum = runFastHouse.maxPlayerNum;
                replyMsg.housePropertyType = runFastHouse.housePropertyType;
                replyMsg.allIntegral = newHousePlayer.allIntegral;
                replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
                replyMsg.houseId = runFastHouse.houseCardId;
                replyMsg.onlyHouseId = runFastHouse.houseId;
                replyMsg.businessId = runFastHouse.businessId;
                replyMsg.competitionKey = runFastHouse.competitionKey;
                SendProxyMsg(replyMsg, sender.proxyServerId);
            }
            else
            {
                newHousePlayer.lineType = LineType.OffLine;
            }
            //存数据库
            OnRequestSaveCreateRunFastInfo(runFastHouse, newHousePlayer);
        }
        public void OnReqJoinRunFastHouse(int peerId, bool inbound, object msg)
        {
            RequestJoinRunFastHouse_P2L reqMsg = msg as RequestJoinRunFastHouse_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            OnReqJoinRunFastHouse(sender, reqMsg.houseId);
        }
        public void OnReqJoinRunFastHouse(Summoner sender, int houseCardId)
        {
            ReplyJoinRunFastHouse_L2P replyMsg = new ReplyJoinRunFastHouse_L2P();

            replyMsg.summonerId = sender.id;

            if (sender.houseId > 0)
            {
                //已经有房间号了
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinRunFastHouse, 已经有房间号了! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (sender.competitionKey > 0)
            {
                if (!CompetitionManager.Instance.IsCompetitionExistByKey(sender.competitionKey, sender.id))
                {
                    //口令无效
                    InitSummonerCompetitionKey(sender);
                }
                else
                {
                    //报名参加了比赛场
                    ServerUtil.RecordLog(LogType.Debug, "OnReqJoinRunFastHouse, 报名参加了比赛场! userId = " + sender.userId + ", competitionKey = " + sender.competitionKey);
                    replyMsg.result = ResultCode.PlayerJoinCompetition;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
            }
            House house = HouseManager.Instance.GetHouseById(houseCardId);
            if (house == null || house.houseType != HouseType.RunFastHouse)
            {
                //本逻辑服务器找不到房间则从世界服务器找
                RequestHouseBelong_L2W reqMsg = new RequestHouseBelong_L2W();
                reqMsg.summonerId = sender.id;
                reqMsg.houseId = houseCardId;
                SendWorldMsg(reqMsg);
                return;
            }
            RunFastHouse runFastHouse = house as RunFastHouse;
            if (runFastHouse == null || runFastHouse.houseStatus >= RunFastHouseStatus.RFHS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinRunFastHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + houseCardId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            JoinRunFastHouse(sender, runFastHouse);
        }
        public void JoinRunFastHouse(Summoner sender, RunFastHouse runFastHouse)
        {
            ReplyJoinRunFastHouse_L2P replyMsg = new ReplyJoinRunFastHouse_L2P();
            replyMsg.summonerId = sender.id;
            
            if (runFastHouse.CheckPlayer(sender.userId))
            {
                //已经在房间里面了
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinRunFastHouse, 已经在房间里面了! userId = " + sender.userId + ", houseId = " + replyMsg.houseId);
                replyMsg.result = ResultCode.PlayerHasBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (runFastHouse.CheckPlayerFull())
            {
                //房间已满
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinRunFastHouse, 房间已满! userId = " + sender.userId + ", houseId = " + replyMsg.houseId);
                replyMsg.result = ResultCode.TheHouseIsFull;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastPlayer newHousePlayer = runFastHouse.AddPlayer(sender);

            sender.houseId = runFastHouse.houseId;

            PlayerShowNode newPlayer = main.GetPlayerShowNode(newHousePlayer);
            runFastHouse.GetRunFastPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != sender.userId)
                {
                    replyMsg.playerShowList.Add(main.GetPlayerShowNode(housePlayer));
                    OnRecvJoinRunFastHouse(housePlayer.summonerId, housePlayer.proxyServerId, newPlayer);
                }
            });

            replyMsg.result = ResultCode.OK;
            replyMsg.allIntegral = newHousePlayer.allIntegral;
            replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
            replyMsg.myIndex = newHousePlayer.index;
            replyMsg.maxBureau = runFastHouse.maxBureau;
            replyMsg.runFastType = runFastHouse.runFastType;
            replyMsg.maxPlayerNum = runFastHouse.maxPlayerNum;
            replyMsg.housePropertyType = runFastHouse.housePropertyType;
            replyMsg.houseId = runFastHouse.houseCardId;
            replyMsg.onlyHouseId = runFastHouse.houseId;
            replyMsg.businessId = runFastHouse.businessId;
            SendProxyMsg(replyMsg, sender.proxyServerId);

            //保存玩家数据
            OnRequestSaveRunFastNewPlayer(runFastHouse.houseId, newHousePlayer);
            //开局
            DisBeginRunFastDecks(runFastHouse);
        }
        //比赛场专用接口
        public void OnReqJoinRunFastHouse(PlayerInfo playerInfo, RunFastHouse runFastHouse)
        {
            RunFastPlayer newHousePlayer = runFastHouse.AddPlayer(playerInfo);
            
            PlayerShowNode newPlayer = main.GetPlayerShowNode(newHousePlayer);
            List<PlayerShowNode> playerShowList = new List<PlayerShowNode>();
            runFastHouse.GetRunFastPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != playerInfo.userId)
                {
                    playerShowList.Add(main.GetPlayerShowNode(housePlayer));
                    if (housePlayer.lineType == LineType.OnLine)
                    {
                        OnRecvJoinRunFastHouse(housePlayer.summonerId, housePlayer.proxyServerId, newPlayer);
                    }
                }
            });

            Summoner sender = SummonerManager.Instance.GetSummonerById(playerInfo.summonerId);
            if (sender != null)
            {
                //在线
                sender.houseId = runFastHouse.houseId;
                newHousePlayer.proxyServerId = sender.proxyServerId;

                ReplyJoinRunFastHouse_L2P replyMsg = new ReplyJoinRunFastHouse_L2P();
                replyMsg.result = ResultCode.OK;
                replyMsg.summonerId = sender.id;
                replyMsg.playerShowList.AddRange(playerShowList);
                replyMsg.allIntegral = newHousePlayer.allIntegral;
                replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
                replyMsg.myIndex = newHousePlayer.index;
                replyMsg.maxBureau = runFastHouse.maxBureau;
                replyMsg.runFastType = runFastHouse.runFastType;
                replyMsg.maxPlayerNum = runFastHouse.maxPlayerNum;
                replyMsg.housePropertyType = runFastHouse.housePropertyType;
                replyMsg.houseId = runFastHouse.houseCardId;
                replyMsg.onlyHouseId = runFastHouse.houseId;
                replyMsg.businessId = runFastHouse.businessId;
                replyMsg.competitionKey = runFastHouse.competitionKey;
                SendProxyMsg(replyMsg, sender.proxyServerId);
            }
            else
            {
                newHousePlayer.lineType = LineType.OffLine;
            }

            //保存玩家数据
            OnRequestSaveRunFastNewPlayer(runFastHouse.houseId, newHousePlayer);
            //开局
            DisBeginRunFastDecks(runFastHouse);
        }
        public void OnRecvJoinRunFastHouse(ulong summonerId, int proxyServerId, PlayerShowNode newPlayer)
        {
            RecvJoinRunFastHouse_L2P recvMsg = new RecvJoinRunFastHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.playerShow = newPlayer;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnRecvPlayerHouseCard(ulong summonerId, int proxyServerId, int houseCard)
        {
            ModuleManager.Get<MainCityMain>().msg_proxy.OnRecvPlayerHouseCard(summonerId, proxyServerId, houseCard);
        }
        public void DisBeginRunFastDecks(RunFastHouse runFastHouse)
        {
            if (runFastHouse.CheckBeginRunFastDecks())
            {
                BeginRunFastDecks(runFastHouse);
            }
        }
        public void BeginRunFastDecks(RunFastHouse runFastHouse)
        {
            HouseBureau houseBureau = new HouseBureau();
            if (runFastHouse.BeginRunFastDecks(houseBureau))
            {
                List<PlayerCardNode> playerInitCardList = new List<PlayerCardNode>();
                runFastHouse.GetRunFastPlayer().ForEach(housePlayer =>
                {
                    RecvBeginRunFast_L2P recvMsg = new RecvBeginRunFast_L2P();
                    recvMsg.summonerId = housePlayer.summonerId;
                    recvMsg.currentBureau = runFastHouse.currentBureau;
                    recvMsg.currentShowCard = runFastHouse.currentShowCard;
                    recvMsg.housePlayerStatus = housePlayer.housePlayerStatus;
                    recvMsg.cardList.AddRange(housePlayer.cardList);
                    SendProxyMsg(recvMsg, housePlayer.proxyServerId);
                    //对手牌进行排序
                    MyRunFastDeck.GetRunFastDeck().SortCardList(housePlayer.cardList);
                    //玩家初始化手牌
                    PlayerCardNode playerCardNode = new PlayerCardNode();
                    playerCardNode.index = housePlayer.index;
                    playerCardNode.cardList.AddRange(housePlayer.cardList);
                    playerInitCardList.Add(playerCardNode);
                });
                //开局保存每局信息
                OnRequestSaveHouseBureauInfo(runFastHouse.houseId, houseBureau, playerInitCardList);
                if (runFastHouse.SetHouseOperateBeginTime() && runFastHouse.currentBureau == 1)
                {
                    main.CheckHouseOperateTimer();
                }
            }
        }
        public void OnReqQuitRunFastHouse(int peerId, bool inbound, object msg)
        {
            RequestQuitRunFastHouse_P2L reqMsg = msg as RequestQuitRunFastHouse_P2L;
            ReplyQuitRunFastHouse_L2P replyMsg = new ReplyQuitRunFastHouse_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号，不需要退出
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitRunFastHouse, 没有房间号，不需要退出! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.RunFastHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitRunFastHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastHouse runFastHouse = house as RunFastHouse;
            if (runFastHouse == null || runFastHouse.houseStatus >= RunFastHouseStatus.RFHS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitRunFastHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (runFastHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间正在投票中
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitRunFastHouse, 房间正在投票中! userId = " + sender.userId + ", houseId = " + runFastHouse.houseId);
                replyMsg.result = ResultCode.HouseGoingDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (runFastHouse.competitionKey > 0)
            {
                //比赛场房间不能投票
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitRunFastHouse, 比赛场房间不能投票! userId = " + sender.userId + ", houseId = " + runFastHouse.houseId);
                replyMsg.result = ResultCode.CompetitionNoDissolve;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastPlayer player = runFastHouse.GetRunFastPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitRunFastHouse, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            replyMsg.dissolveVoteTime = main.houseDissolveVoteTime;
            if (runFastHouse.businessId > 0)
            {
                replyMsg.dissolveVoteTime = main.businessDissolveVoteTime;
            }
            bool bVote = false;
            if (runFastHouse.CheckPlayerFull() && runFastHouse.houseStatus != RunFastHouseStatus.RFHS_FreeBureau)
            {
                //要投票
                bVote = true;
                runFastHouse.voteBeginTime = DateTime.Now;
                player.voteStatus = VoteStatus.LaunchVote;
                foreach (RunFastPlayer housePlayer in runFastHouse.GetOtherRunFastPlayer(sender.userId))
                {
                    OnRecvQuitRunFastHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index, bVote, replyMsg.dissolveVoteTime);
                }
                main.AddDissolveVoteHouse(runFastHouse.houseId);
            }
            else
            {
                //房主
                if (player.index == 0 && runFastHouse.businessId == 0)
                {
                    runFastHouse.GetRunFastPlayer().ForEach(housePlayer =>
                    {
                        if (housePlayer.userId != sender.userId)
                        {
                            Summoner houseSender = SummonerManager.Instance.GetSummonerByUserId(housePlayer.userId);
                            if (houseSender != null)
                            {
                                houseSender.houseId = 0;
                                OnRecvQuitRunFastHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index);
                            }
                            OnRequestSaveHouseId(housePlayer.userId, 0);
                        }
                    });
                    InitSummonerHouseId(sender);
                    runFastHouse.houseStatus = RunFastHouseStatus.RFHS_Dissolved;
                    //保存房间状态
                    OnRequestSaveRunFastHouseStatus(runFastHouse.houseId, runFastHouse.houseStatus);
                    HouseManager.Instance.RemoveHouse(runFastHouse.houseId);
                }
                else
                {
                    OnRecvLeaveRunFastHouse(runFastHouse, sender.userId, player.index);
                    runFastHouse.RemovePlayer(sender.userId);
                    sender.houseId = 0;
                    //保存数据库删除玩家
                    OnRequestDelHousePlayer(runFastHouse.houseId, sender.id);
                }
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.bVote = bVote;
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void OnRecvQuitRunFastHouse(ulong summonerId, int proxyServerId, int index, bool bVote = false, int dissolveVoteTime = 0)
        {
            RecvQuitRunFastHouse_L2P recvMsg = new RecvQuitRunFastHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.bVote = bVote;
            recvMsg.index = index;
            recvMsg.dissolveVoteTime = dissolveVoteTime;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnRecvLeaveRunFastHouse(RunFastHouse runFastHouse, string userId, int index)
        {
            foreach (RunFastPlayer housePlayer in runFastHouse.GetOtherRunFastPlayer(userId))
            {
                RecvLeaveRunFastHouse_L2P recvMsg = new RecvLeaveRunFastHouse_L2P();
                recvMsg.summonerId = housePlayer.summonerId;
                recvMsg.leaveIndex = index;
                SendProxyMsg(recvMsg, housePlayer.proxyServerId);
            }
        }
        public void OnReqShowRunFastCard(int peerId, bool inbound, object msg)
        {
            RequestShowRunFastCard_P2L reqMsg = msg as RequestShowRunFastCard_P2L;
            ReplyShowRunFastCard_L2P replyMsg = new ReplyShowRunFastCard_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (reqMsg.cardList.Count <= 0)
            {
                //发送过来的牌有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 发送过来的牌有误! userId = " + sender.userId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.RunFastHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastHouse runFastHouse = house as RunFastHouse;
            if (runFastHouse == null || runFastHouse.houseStatus != RunFastHouseStatus.RFHS_BeginBureau)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (runFastHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间正在投票中
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 房间正在投票中! userId = " + sender.userId + ", houseId = " + runFastHouse.houseId);
                replyMsg.result = ResultCode.HouseGoingDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastPlayer player = runFastHouse.GetRunFastPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqShowRunFastCard(runFastHouse, player, reqMsg.cardList, replyMsg);
        }
        public void OnReqShowRunFastCard(RunFastHouse runFastHouse, RunFastPlayer player, List<Card> cardList, ReplyShowRunFastCard_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyShowRunFastCard_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            if (player.index != runFastHouse.currentShowCard || player.housePlayerStatus != HousePlayerStatus.WaitShowCard)
            {
                //玩家不是出牌状态
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 玩家不是出牌状态! userId = " + player.userId + ", houseId = " + runFastHouse.houseId);
                replyMsg.result = ResultCode.PlayerHouserStutasErrod;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (player.cardList.Count < cardList.Count)
            {
                //发送过来的牌有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 发送过来的牌有误! userId = " + player.userId);
                replyMsg.result = ResultCode.PlayerSendShowCardError;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (!MyRunFastDeck.GetRunFastDeck().ExistsCardList(player.cardList, cardList))
            {
                //发送过来的牌有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 发送过来的牌有误! userId = " + player.userId);
                replyMsg.result = ResultCode.PlayerSendShowCardError;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            RulesReturn rulesReturn = MyRunFastDeck.GetRunFastDeck().IsRules(cardList);
            if (rulesReturn.type == PokerGroupType.Error || (rulesReturn.type == PokerGroupType.SanZhang && !MyRunFastDeck.GetRunFastDeck().IsThreePokersRules(rulesReturn.parameter, player.cardList.Count, cardList.Count)))
            {
                //发送过来的牌有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 发送过来的牌有误! userId = " + player.userId);
                replyMsg.result = ResultCode.PlayerSendShowCardError;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (runFastHouse.currentCardList.Count > 0 && runFastHouse.pokerGroupType != PokerGroupType.None)
            {
                //不是第一把或者上一把出牌也不是自己就要判断出牌能不能够大于上次的牌
                if (runFastHouse.currentWhoPlay != player.index && !MyRunFastDeck.GetRunFastDeck().CheckCurrentCardByType(runFastHouse.currentCardList, runFastHouse.pokerGroupType, cardList, rulesReturn.type))
                {
                    //发送过来的牌有误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 出牌不能够小于上次的牌! userId = " + player.userId);
                    replyMsg.result = ResultCode.PlayerSendShowCardError;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
            }
            else
            {
                //第一局第一把要有黑桃三
                if (runFastHouse.currentBureau == 1 && runFastHouse.CheckHousePropertyType(RunFastHousePropertyType.ERFHP_SpadeThree) && !cardList.Exists(element => (element.suit == Suit.Spade && element.rank == Rank.Three)))
                {
                    //发送过来的牌有误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 第一局第一把要有黑桃三! userId = " + player.userId);
                    replyMsg.result = ResultCode.PlayerSendShowCardError;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
            }
            RunFastPlayer lastPlayer = runFastHouse.GetLastHousePlayer(player.index);
            if (lastPlayer == null)
            {
                //上家玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 上家玩家不存在! userId = " + player.userId);
                replyMsg.result = ResultCode.HouseNextPlayerError;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            RunFastPlayer nextPlayer = runFastHouse.GetNextHousePlayer(player.index);
            if (nextPlayer == null)
            {
                //下家玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 下家玩家不存在! userId = " + player.userId);
                replyMsg.result = ResultCode.HouseNextPlayerError;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            //下一家报单，必须出最大的单张
            if (nextPlayer.cardList.Count == 1 && rulesReturn.type == PokerGroupType.DanZhang && player.cardList.Exists(element => element.rank > cardList[0].rank))
            {
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowRunFastCard, 下一家报单，必须出最大的单张! userId = " + player.userId);
                replyMsg.result = ResultCode.PlayerSendShowCardError;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            //当前玩家手牌,用来判断强关用
            List<Card> playerCardList = new List<Card>();
            playerCardList.AddRange(player.cardList);
            //删除出牌
            foreach (Card card in cardList)
            {
                player.cardList.Remove(player.cardList.Find(element => (element.suit == card.suit && element.rank == card.rank)));
            }
            player.housePlayerStatus = HousePlayerStatus.ShowCard;
            if (player.bStrongOff)
            {
                //出牌成功以后，强关不成立
                player.bStrongOff = false;
            }
            replyMsg.bDanZhangFalg = false;
            if (player.cardList.Count != 0)
            {
                runFastHouse.currentShowCard = nextPlayer.index;
                //要不要的起
                if (MyRunFastDeck.GetRunFastDeck().CheckShowCardByType(nextPlayer.cardList, cardList, rulesReturn.type))
                {
                    nextPlayer.housePlayerStatus = HousePlayerStatus.WaitShowCard;
                }
                else
                {
                    nextPlayer.housePlayerStatus = HousePlayerStatus.Pass;
                    // 都可以被强关
                    if (runFastHouse.CheckHousePropertyType(RunFastHousePropertyType.ERFHP_StrongOff) && runFastHouse.maxPlayerNum == RunFastConstValue.RunFastThreePlayer &&
                        nextPlayer.cardList.Count == runFastHouse.GetAllCardCount() && !nextPlayer.bStrongOff &&
                        DisPlayerStrongOff(playerCardList, nextPlayer, runFastHouse.currentCardList, runFastHouse.pokerGroupType))
                    {
                        nextPlayer.bStrongOff = true;
                    }
                }
                if (player.cardList.Count == 1)
                {
                    replyMsg.bDanZhangFalg = true;
                }
            }
            else
            {
                //最后一把是炸弹
                if (rulesReturn.type == PokerGroupType.ZhaDan)
                {
                    OnRecvZhaDanIntegral(runFastHouse, player.index);
                }
                runFastHouse.houseStatus = RunFastHouseStatus.RFHS_Settlement;
                //保存房间状态
                OnRequestSaveRunFastHouseStatus(runFastHouse.houseId, runFastHouse.houseStatus);
            }

            runFastHouse.currentWhoPlay = player.index;
            runFastHouse.pokerGroupType = rulesReturn.type;
            runFastHouse.currentCardList.Clear();
            runFastHouse.currentCardList.AddRange(cardList);

            foreach (RunFastPlayer housePlayer in runFastHouse.GetOtherRunFastPlayer(player.userId))
            {
                OnRecvShowRunFastCard(housePlayer.summonerId, housePlayer.proxyServerId, runFastHouse, housePlayer.housePlayerStatus, replyMsg.bDanZhangFalg);
            }

            runFastHouse.SetHouseOperateBeginTime();

            replyMsg.result = ResultCode.OK;
            replyMsg.cardList.AddRange(cardList);
            replyMsg.currentShowCard = runFastHouse.currentShowCard;
            replyMsg.pokerGroupType = runFastHouse.pokerGroupType;
            SendProxyMsg(replyMsg, player.proxyServerId);
            
            //保存当局出牌
            OnRequestSaveBureauShowCard(runFastHouse, player.index);

            //是否结算
            DisSettlementRunFastDecks(runFastHouse);
        }
        public bool DisPlayerStrongOff(List<Card> playerCardList, RunFastPlayer nextPlayer, List<Card> lastCardList, PokerGroupType lastGroupType)
        {
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
                    List<Card> minCardList = promptCardList[promptCardList.Count - 1].cardList;
                    if (MyRunFastDeck.GetRunFastDeck().CheckShowCardByType(nextPlayer.cardList, minCardList, pokerGroupType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public void OnRecvShowRunFastCard(ulong summonerId, int proxyServerId, RunFastHouse runFastHouse, HousePlayerStatus housePlayerStatus, bool bDanZhangFalg)
        {
            RecvShowRunFastCard_L2P recvMsg = new RecvShowRunFastCard_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.currentWhoPlay = runFastHouse.currentWhoPlay;
            recvMsg.cardList.AddRange(runFastHouse.currentCardList);
            recvMsg.currentShowCard = runFastHouse.currentShowCard;
            recvMsg.housePlayerStatus = housePlayerStatus;
            recvMsg.bDanZhangFalg = bDanZhangFalg;
            recvMsg.pokerGroupType = runFastHouse.pokerGroupType;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void DisSettlementRunFastDecks(RunFastHouse runFastHouse)
        {
            if (runFastHouse.CheckSettlementRunFastDecks())
            {
                SettlementRunFastDecks(runFastHouse);
            }
        }
        public void SettlementRunFastDecks(RunFastHouse runFastHouse)
        {
            if (runFastHouse.SettlementRunFastDecks())
            {
                //小局结算
                List<PlayerSettlementNode> playerSettlementList = runFastHouse.GetPlayerSettlementList();
                foreach (RunFastPlayer housePlayer in runFastHouse.GetRunFastPlayer())
                {
                    RecvSettlementRunFast_L2P recvMsg = new RecvSettlementRunFast_L2P();
                    recvMsg.summonerId = housePlayer.summonerId;
                    recvMsg.settlementType = runFastHouse.GetPlayerSettlementType(housePlayer.cardList.Count);
                    recvMsg.playerSettlement = Serializer.tryCompressObject(playerSettlementList);
                    SendProxyMsg(recvMsg, housePlayer.proxyServerId);
                    //清理掉手牌
                    housePlayer.cardList.Clear();
                    //保存数据库玩家结算信息
                    OnRequestSavePlayerSettlement(runFastHouse.houseId, housePlayer);
                }
                //保存当局积分
                OnRequestSaveBureauIntegral(runFastHouse.houseId, runFastHouse.GetHouseBureau(), runFastHouse.zhuangPlayerIndex);
                //第一局打完之后扣房卡()
                if (runFastHouse.CheckeFirstBureauEnd())
                {
                    //不是商家模式并且开启扣房卡模式
                    if (runFastHouse.businessId == 0 && main.CheckOpenDelHouseCard())
                    {
                        RunFastPlayer housePlayer = runFastHouse.GetHouseOwner();
                        if (housePlayer != null)
                        {
                            int houseCard = main.GetHouseCard(runFastHouse.maxBureau);
                            //处理玩家房卡
                            Summoner sender = SummonerManager.Instance.GetSummonerByUserId(housePlayer.userId);
                            if (sender != null)
                            {
                                if (sender.roomCard >= houseCard)
                                {
                                    sender.roomCard -= houseCard;
                                    OnRecvPlayerHouseCard(sender.id, sender.proxyServerId, sender.roomCard);
                                    OnRequestSaveHouseCard(sender.id, OperationType.DelData, houseCard);
                                }
                            }
                            else
                            {
                                OnRequestSaveHouseCard(housePlayer.summonerId, OperationType.DelData, houseCard);
                            }
                        }
                    }
                    //做统计
                    foreach (RunFastPlayer housePlayer in runFastHouse.GetRunFastPlayer())
                    {
                        ModuleManager.Get<MainCityMain>().RecordBusinessUsers(housePlayer.userId, runFastHouse.businessId);
                    }
                }
                //大局结算
                if (runFastHouse.CheckeHouseBureau())
                {
                    OnHouseEndSettlement(runFastHouse, RunFastHouseStatus.RFHS_EndBureau);
                }
            }
        }
        public void OnHouseEndSettlement(RunFastHouse runFastHouse, RunFastHouseStatus houseStatus)
        {
            List<ComPlayerIntegral> playerIntegralList = new List<ComPlayerIntegral>();
            List<PlayerEndSettlementNode> playerEndSettlementList = runFastHouse.GetPlayerEndSettlementList();
            foreach (RunFastPlayer housePlayer in runFastHouse.GetRunFastPlayer())
            {
                //优惠券
                TicketsNode tickets = null;
                if (houseStatus == RunFastHouseStatus.RFHS_EndBureau && runFastHouse.businessId > 0 && runFastHouse.competitionKey == 0)
                {
                    //普通的商家模式才在这里奖励优惠劵
                    int rank = runFastHouse.GetCurrentRanking(housePlayer.index, housePlayer.allIntegral);
                    tickets = main.GetTickets(runFastHouse.businessId, rank);
                }
                Summoner sender = SummonerManager.Instance.GetSummonerByUserId(housePlayer.userId);
                if (sender != null)
                {
                    //处理玩家房间Id和总积分
                    sender.houseId = 0;
                    sender.AddAllIntegral(housePlayer.allIntegral);
                    //加入优惠券
                    sender.AddTicketsNode(tickets);
                    //下发消息
                    RecvEndSettlementRunFast_L2P recvMsg = new RecvEndSettlementRunFast_L2P();
                    recvMsg.summonerId = housePlayer.summonerId;
                    recvMsg.houseStatus = houseStatus;
                    recvMsg.allIntegral = sender.allIntegral;
                    recvMsg.ticketsNode = tickets;
                    recvMsg.playerEndSettlementList.AddRange(playerEndSettlementList);
                    SendProxyMsg(recvMsg, sender.proxyServerId);
                }
                //比赛场积分
                if (runFastHouse.competitionKey > 0)
                {
                    playerIntegralList.Add(new ComPlayerIntegral(housePlayer.summonerId, housePlayer.allIntegral));
                }
                //玩家清理房间Id和保存总积分
                OnRequestSaveAllIntegral(housePlayer.userId, housePlayer.allIntegral);
                //玩家保存优惠券
                OnRequestSaveTickets(housePlayer.summonerId, tickets);
                //发送结束通知
                if (ModuleManager.Get<ServiceBoxMain>().bOpenBoxAutoPlaying && housePlayer.index == 0)
                {
                    ModuleManager.Get<ServiceBoxMain>().RecvHouseEndSettlement(housePlayer.summonerId, housePlayer.proxyServerId, runFastHouse.houseCardId);
                }
            }
            runFastHouse.houseStatus = houseStatus;
            runFastHouse.operateBeginTime = DateTime.Parse("1970-01-01 00:00:00");
            //保存房间状态
            OnRequestSaveRunFastHouseStatus(runFastHouse.houseId, runFastHouse.houseStatus);
            HouseManager.Instance.RemoveHouse(runFastHouse.houseId);
            //发送比赛场积分
            if (houseStatus == RunFastHouseStatus.RFHS_EndBureau)
            {
                OnReqCompetitionIntegral(runFastHouse.competitionKey, playerIntegralList);
            }
            //同步解散信息
            else if (houseStatus == RunFastHouseStatus.RFHS_Dissolved || houseStatus == RunFastHouseStatus.RFHS_GMDissolved)
            {
                OnnRequestSaveDissolveRunFastInfo(runFastHouse);
            }
        }
        public void OnReqPassRunFastCard(int peerId, bool inbound, object msg)
        {
            RequestPassRunFastCard_P2L reqMsg = msg as RequestPassRunFastCard_P2L;
            ReplyPassRunFastCard_L2P replyMsg = new ReplyPassRunFastCard_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.RunFastHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqPassRunFastCard, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastHouse runFastHouse = house as RunFastHouse;
            if (runFastHouse == null || runFastHouse.houseStatus != RunFastHouseStatus.RFHS_BeginBureau)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqPassRunFastCard, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastPlayer player = runFastHouse.GetRunFastPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqPassRunFastCard, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }

            OnReqPassRunFastCard(runFastHouse, player, replyMsg);
        }
        public void OnReqPassRunFastCard(RunFastHouse runFastHouse, RunFastPlayer player, ReplyPassRunFastCard_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyPassRunFastCard_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            if (player.index != runFastHouse.currentShowCard || player.housePlayerStatus != HousePlayerStatus.Pass)
            {
                //玩家不是出牌状态
                ServerUtil.RecordLog(LogType.Debug, "OnReqPassRunFastCard, 玩家不是过牌状态! userId = " + player.userId + ", houseId = " + runFastHouse.houseId);
                replyMsg.result = ResultCode.PlayerHouserStutasErrod;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (MyRunFastDeck.GetRunFastDeck().CheckShowCardByType(player.cardList, runFastHouse.currentCardList, runFastHouse.pokerGroupType))
            {
                //当前玩家要得起上一次的牌
                ServerUtil.RecordLog(LogType.Debug, "OnReqPassRunFastCard, 当前玩家要得起上一次的牌! userId = " + player.userId);
                replyMsg.result = ResultCode.PlayerSendShowCardError;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            RunFastPlayer nextPlayer = runFastHouse.GetNextHousePlayer(player.index);
            if (nextPlayer == null)
            {
                //下家玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqPassRunFastCard, 下家玩家不存在! userId = " + player.userId);
                replyMsg.result = ResultCode.HouseNextPlayerError;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            player.housePlayerStatus = HousePlayerStatus.ShowCard;
            runFastHouse.currentShowCard = nextPlayer.index;

            if (runFastHouse.currentWhoPlay != nextPlayer.index)
            {
                //要不要的起
                if (MyRunFastDeck.GetRunFastDeck().CheckShowCardByType(nextPlayer.cardList, runFastHouse.currentCardList, runFastHouse.pokerGroupType))
                {
                    nextPlayer.housePlayerStatus = HousePlayerStatus.WaitShowCard;
                }
                else
                {
                    nextPlayer.housePlayerStatus = HousePlayerStatus.Pass;
                }
            }
            else
            {
                nextPlayer.housePlayerStatus = HousePlayerStatus.WaitShowCard;
            }

            foreach (RunFastPlayer housePlayer in runFastHouse.GetOtherRunFastPlayer(player.userId))
            {
                OnRecvPassRunFastCard(housePlayer.summonerId, housePlayer.proxyServerId, runFastHouse.currentShowCard, housePlayer.housePlayerStatus);
            }
            runFastHouse.SetHouseOperateBeginTime();

            replyMsg.result = ResultCode.OK;
            replyMsg.currentShowCard = runFastHouse.currentShowCard;
            SendProxyMsg(replyMsg, player.proxyServerId);
            
            //我出的炸弹都要不起，给积分
            if (runFastHouse.pokerGroupType == PokerGroupType.ZhaDan && runFastHouse.currentWhoPlay == nextPlayer.index)
            {
                OnRecvZhaDanIntegral(runFastHouse, nextPlayer.index);
            }
        }
        public void OnRecvPassRunFastCard(ulong summonerId, int proxyServerId, int currentShowCard, HousePlayerStatus housePlayerStatus)
        {
            RecvPassRunFastCard_L2P recvMsg = new RecvPassRunFastCard_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.currentShowCard = currentShowCard;
            recvMsg.housePlayerStatus = housePlayerStatus;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnRecvZhaDanIntegral(RunFastHouse runFastHouse, int playerIndex)
        {
            foreach (RunFastPlayer housePlayer in runFastHouse.GetRunFastPlayer())
            {
                int integral = -main.loseIntegral;
                if (playerIndex == housePlayer.index)
                {
                    integral = main.GetZhaDanWinIntegral(runFastHouse.maxPlayerNum);
                    housePlayer.bombIntegral += integral;
                }
                runFastHouse.SetPlayerZhaDanIntegral(housePlayer.index, integral);
                housePlayer.allIntegral += integral;
            }
        }
        public void OnReqReadyRunFastHouse(int peerId, bool inbound, object msg)
        {
            RequestReadyRunFastHouse_P2L reqMsg = msg as RequestReadyRunFastHouse_P2L;
            ReplyReadyRunFastHouse_L2P replyMsg = new ReplyReadyRunFastHouse_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号，不需要退出
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyRunFastHouse, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.RunFastHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyRunFastHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastHouse runFastHouse = house as RunFastHouse;
            if (runFastHouse == null || (runFastHouse.currentBureau > 0 && runFastHouse.houseStatus != RunFastHouseStatus.RFHS_Settlement) ||
               (runFastHouse.currentBureau == 0 && runFastHouse.houseStatus != RunFastHouseStatus.RFHS_FreeBureau))
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyRunFastHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastPlayer player = runFastHouse.GetRunFastPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyRunFastHouse, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqReadyRunFastHouse(runFastHouse, player, replyMsg);
        }
        public void OnReqReadyRunFastHouse(RunFastHouse runFastHouse, RunFastPlayer player, ReplyReadyRunFastHouse_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyReadyRunFastHouse_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            player.housePlayerStatus = HousePlayerStatus.Ready;

            foreach (RunFastPlayer housePlayer in runFastHouse.GetOtherRunFastPlayer(player.userId))
            {
                OnRecvReadyRunFastHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index);
            }

            replyMsg.result = ResultCode.OK;
            SendProxyMsg(replyMsg, player.proxyServerId);
            
            //开局
            DisBeginRunFastDecks(runFastHouse);
        }
        public void OnRecvReadyRunFastHouse(ulong summonerId, int proxyServerId, int readyIndex)
        {
            RecvReadyRunFastHouse_L2P recvMsg = new RecvReadyRunFastHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.readyIndex = readyIndex;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnReqRunFastHouseInfo(int peerId, bool inbound, object msg)
        {
            RequestRunFastHouseInfo_P2L reqMsg = msg as RequestRunFastHouseInfo_P2L;
            ReplyRunFastHouseInfo_L2P replyMsg = new ReplyRunFastHouseInfo_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqRunFastHouseInfo, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.RunFastHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqRunFastHouseInfo, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                InitSummonerHouseId(sender);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastHouse runFastHouse = house as RunFastHouse;
            if (runFastHouse == null || runFastHouse.houseStatus >= RunFastHouseStatus.RFHS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqRunFastHouseInfo, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                InitSummonerHouseId(sender);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (sender.competitionKey > 0 && !CompetitionManager.Instance.IsCompetitionExistByKey(sender.competitionKey, sender.id))
            {
                //如果玩家的比赛口令不存在，则解散该房间
                ServerUtil.RecordLog(LogType.Debug, "OnReqRunFastHouseInfo, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                PlayerInitHouseIdAndComKey(sender);
                main.OnRecvGMDissolveRunFastHouse(runFastHouse);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastPlayer player = runFastHouse.GetRunFastPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqRunFastHouseInfo, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                InitSummonerHouseId(sender);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            //返回房间信息
            replyMsg.houseCardId = runFastHouse.houseCardId;
            replyMsg.onlyHouseId = runFastHouse.houseId;
            replyMsg.runFastType = runFastHouse.runFastType;
            replyMsg.currentBureau = runFastHouse.currentBureau;
            replyMsg.maxBureau = runFastHouse.maxBureau;
            replyMsg.currentShowCard = runFastHouse.currentShowCard;
            replyMsg.currentWhoPlay = runFastHouse.currentWhoPlay;
            replyMsg.houseStatus = runFastHouse.houseStatus;
            replyMsg.pokerGroupType = runFastHouse.pokerGroupType;
            replyMsg.maxPlayerNum = runFastHouse.maxPlayerNum;
            replyMsg.housePropertyType = runFastHouse.housePropertyType;
            replyMsg.businessId = runFastHouse.businessId;
            replyMsg.competitionKey = runFastHouse.competitionKey;
            replyMsg.zhuangPlayerIndex = runFastHouse.zhuangPlayerIndex;
            replyMsg.houseCardList.AddRange(runFastHouse.currentCardList);
            //玩家信息
            RunFastOnlineNode runFastOnlineNode = new RunFastOnlineNode();
            runFastOnlineNode.myPlayerOnline = main.GetMyPlayerOnlineNode(player);
            foreach (RunFastPlayer housePlayer in runFastHouse.GetOtherRunFastPlayer(sender.userId))
            {
                runFastOnlineNode.playerOnlineList.Add(main.GetPlayerOnlineNode(housePlayer, runFastHouse.CheckHousePropertyType(RunFastHousePropertyType.ERFHP_SurplusCardCount)));
                if (player.lineType != LineType.OnLine)
                {
                    ModuleManager.Get<MainCityMain>().OnRecvPlayerLineType(housePlayer.summonerId, housePlayer.proxyServerId, player.index, LineType.OnLine);
                }
            }
            replyMsg.runFastOnlineNode = Serializer.tryCompressObject(runFastOnlineNode);
            //玩家上线状态
            player.lineType = LineType.OnLine;
            //ip
            if (!string.IsNullOrEmpty(sender.ip) && player.ip != null && player.ip != sender.ip)
            {
                player.ip = sender.ip;
            }
            //网关id
            if(player.proxyServerId != sender.proxyServerId)
            {
                player.proxyServerId = sender.proxyServerId;
            }
            //是否处于投票阶段
            if (runFastHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                TimeSpan span = DateTime.Now.Subtract(runFastHouse.voteBeginTime);
                if (span.TotalSeconds < main.houseDissolveVoteTime)
                {
                    replyMsg.houseVoteTime = main.houseDissolveVoteTime - span.TotalSeconds;
                }
            }

            replyMsg.result = ResultCode.OK;
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void OnReqDissolveHouseVote(int peerId, bool inbound, object msg)
        {
            RequestDissolveHouseVote_P2L reqMsg = msg as RequestDissolveHouseVote_P2L;
            ReplyDissolveHouseVote_L2P replyMsg = new ReplyDissolveHouseVote_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (reqMsg.voteStatus == VoteStatus.FreeVote || reqMsg.voteStatus == VoteStatus.LaunchVote)
            {
                //投票状态错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 投票状态错误! userId = " + sender.userId + ", voteStatus = " + reqMsg.voteStatus);
                replyMsg.result = ResultCode.PlayerSendVoteStatusError;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.RunFastHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastHouse runFastHouse = house as RunFastHouse;
            if (runFastHouse == null || runFastHouse.houseStatus >= RunFastHouseStatus.RFHS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (runFastHouse.voteBeginTime == DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间没有发起投票
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 房间没有发起投票! userId = " + sender.userId + ", houseId = " + runFastHouse.houseId);
                replyMsg.result = ResultCode.HouseNoNeedDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            RunFastPlayer player = runFastHouse.GetRunFastPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqDissolveHouseVote(runFastHouse, player, reqMsg.voteStatus, replyMsg);
        }
        public void OnReqDissolveHouseVote(RunFastHouse runFastHouse, RunFastPlayer player, VoteStatus voteStatus, ReplyDissolveHouseVote_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyDissolveHouseVote_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //发起者
            RunFastPlayer launchPlayer = runFastHouse.GetRunFastVoteLaunchPlayer();  
            if (launchPlayer == null)
            {
                //发起者不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 发起者不存在! userId = " + player.userId + ", houseId = " + runFastHouse.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            player.voteStatus = voteStatus;

            //判断是否能够解散(free 表示继续等投票 agree 表示要解散 oppose 表示解散失败)
            VoteStatus houseVoteStatus = runFastHouse.GetDissolveHouseVote();
            //告诉其他人
            foreach (RunFastPlayer housePlayer in runFastHouse.GetOtherRunFastPlayer(player.userId))
            {
                OnRecvDissolveHouseVote(housePlayer.summonerId, housePlayer.proxyServerId, player.index, voteStatus, houseVoteStatus);
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.voteStatus = voteStatus;
            replyMsg.houseVoteStatus = houseVoteStatus;
            SendProxyMsg(replyMsg, player.proxyServerId);
            
            //处理投票
            DisposeDissolveHouseVote(runFastHouse, houseVoteStatus);
        }
        public void OnRecvDissolveHouseVote(ulong summonerId, int proxyServerId, int index, VoteStatus voteStatus, VoteStatus houseVoteStatus)
        {
            RecvDissolveHouseVote_L2P recvMsg = new RecvDissolveHouseVote_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.index = index;
            recvMsg.voteStatus = voteStatus;
            recvMsg.houseVoteStatus = houseVoteStatus;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void DisposeDissolveHouseVote(RunFastHouse runFastHouse, VoteStatus houseVoteStatus)
        {
            if (houseVoteStatus != VoteStatus.FreeVote)
            {
                DissolveHouseVote(runFastHouse, houseVoteStatus);
            }
        }
        public void DissolveHouseVote(RunFastHouse runFastHouse, VoteStatus houseVoteStatus)
        {
            if (houseVoteStatus == VoteStatus.AgreeVote)
            {
                //解散成功
                OnHouseEndSettlement(runFastHouse, RunFastHouseStatus.RFHS_Dissolved);
                main.DelDissolveVoteHouse(runFastHouse.houseId);
            } 
            else if(houseVoteStatus == VoteStatus.OpposeVote)
            {
                //解散失败
                List<PlayerVoteNode> playerVoteList = new List<PlayerVoteNode>();
                runFastHouse.voteBeginTime = DateTime.Parse("1970-01-01 00:00:00");
                foreach (RunFastPlayer housePlayer in runFastHouse.GetRunFastPlayer())
                {
                    if (housePlayer.voteStatus != VoteStatus.FreeVote)
                    {
                        housePlayer.voteStatus = VoteStatus.FreeVote;
                        playerVoteList.Add(new PlayerVoteNode { summonerId = housePlayer.summonerId, voteStatus = housePlayer.voteStatus });
                    }
                }
                main.DelDissolveVoteHouse(runFastHouse.houseId);
            }
        }
        public void OnReplyHouseInfo(int peerId, bool inbound, object msg)
        {
            ReplyHouseInfo_D2L replyMsg = msg as ReplyHouseInfo_D2L;
            List<RunFastHouseNode> houseList = Serializer.tryUncompressObject<List<RunFastHouseNode>>(replyMsg.house);
            if (houseList != null && houseList.Count > 0)
            {
                ModuleManager.Get<DistributedMain>().SetLoadHouseCount(houseList.Count);
                houseList.ForEach(houseNode =>
                {
                    RunFastHouse runFastHouse = new RunFastHouse();
                    runFastHouse.houseId = houseNode.houseId;
                    runFastHouse.houseCardId = houseNode.houseCardId;
                    runFastHouse.logicId = houseNode.logicId;
                    runFastHouse.currentBureau = houseNode.currentBureau;
                    runFastHouse.maxBureau = houseNode.maxBureau;
                    runFastHouse.maxPlayerNum = houseNode.maxPlayerNum;
                    runFastHouse.businessId = houseNode.businessId;
                    runFastHouse.housePropertyType = houseNode.housePropertyType;
                    runFastHouse.zhuangPlayerIndex = houseNode.zhuangPlayerIndex;
                    runFastHouse.houseType = houseNode.houseType;
                    runFastHouse.runFastType = (RunFastType)houseNode.runFastType;
                    runFastHouse.houseStatus = houseNode.houseStatus;
                    runFastHouse.createTime = Convert.ToDateTime(houseNode.createTime);
                    if (runFastHouse.businessId > 0 && runFastHouse.houseStatus == RunFastHouseStatus.RFHS_Settlement)
                    {
                        runFastHouse.operateBeginTime = DateTime.Now;
                    }
                    //保存房间
                    HouseManager.Instance.AddHouse(runFastHouse.houseId, runFastHouse);
                    //请求玩家信息和当局信息
                    OnRequestHousePlayerAndBureau(runFastHouse.houseId);
                });
            }
            else
            {
                ModuleManager.Get<DistributedMain>().SetLoadHouseCount();
            }
        }
        public void OnReplyHousePlayerAndBureau(int peerId, bool inbound, object msg)
        {
            ReplyHousePlayerAndBureau_D2L replyMsg = msg as ReplyHousePlayerAndBureau_D2L;

            ModuleManager.Get<DistributedMain>().DelLoadHouseCount();

            House house = HouseManager.Instance.GetHouseById(replyMsg.houseId);
            if (house == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReplyHousePlayerAndBureau, 房间不存在! houseId = " + replyMsg.houseId);;
                return;
            }
            RunFastHouse runFastHouse = house as RunFastHouse;
            if (runFastHouse == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReplyHousePlayerAndBureau, 房间不存在! houseId = " + replyMsg.houseId);
                return;
            }
            if (runFastHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                main.AddDissolveVoteHouse(runFastHouse.houseId);
            }
            HousePlayerBureau housePlayerBureau = Serializer.tryUncompressObject<HousePlayerBureau>(replyMsg.housePlayerBureau);
            if (housePlayerBureau != null && housePlayerBureau.housePlayerList != null && housePlayerBureau.housePlayerList.Count > 0)
            {
                //HousePlayerStatus housePlayerStatus = HousePlayerStatus.Free;
                //if (runFastHouse.currentBureau == 0 && runFastHouse.houseStatus == RunFastHouseStatus.RFHS_FreeBureau)
                //{
                //    if (housePlayerBureau.housePlayerList.Count >= runFastHouse.maxPlayerNum)
                //    {
                //        //第一局没打完
                //        HouseManager.Instance.RemoveHouse(runFastHouse.houseId);
                //        //保存房间状态
                //        OnRequestSaveRunFastHouseStatus(runFastHouse.houseId, RunFastHouseStatus.RFHS_Dissolved);
                //        return;
                //    }
                //    if (runFastHouse.businessId > 0)
                //    {
                //        //商家模式开服自动准备
                //        housePlayerStatus = HousePlayerStatus.Ready;
                //    }
                //}
                //没开始打牌或者第一局已经结算
                foreach (HousePlayerNode playerNode in housePlayerBureau.housePlayerList)
                {
                    if (runFastHouse.GetRunFastPlayer().Exists(element => (element.userId == playerNode.userId || element.index == playerNode.index)))
                    {
                        ServerUtil.RecordLog(LogType.Error, "OnReplyHousePlayerAndBureau, 已经存在玩家信息! userId = " + playerNode.userId);
                        break;
                    }
                    runFastHouse.AddPlayer(playerNode);
                    //runFastHouse.AddPlayer(playerNode, housePlayerStatus);
                }
            }
            if (housePlayerBureau != null && housePlayerBureau.houseBureauList != null && housePlayerBureau.houseBureauList.Count > 0)
            {
                foreach (HouseBureau bureauNode in housePlayerBureau.houseBureauList)
                {
                    if (runFastHouse.houseBureauList.Exists(element => element.bureau == bureauNode.bureau))
                    {
                        ServerUtil.RecordLog(LogType.Debug, "OnReplyHousePlayerAndBureau, 已经存在当局信息! bureau = " + bureauNode.bureau);
                        break;
                    }
                    runFastHouse.houseBureauList.Add(bureauNode);
                }
            }
        }
        public void OnReqRunFastOverallRecord(int peerId, bool inbound, object msg)
        {
            RequestRunFastOverallRecord_P2L reqMsg_P2L = msg as RequestRunFastOverallRecord_P2L;
            RequestRunFastOverallRecord_L2D reqMsg_L2D = new RequestRunFastOverallRecord_L2D();
            
            reqMsg_L2D.summonerId = reqMsg_P2L.summonerId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnReplyRunFastOverallRecord(int peerId, bool inbound, object msg)
        {
            ReplyRunFastOverallRecord_D2L replyMsg_D2L = msg as ReplyRunFastOverallRecord_D2L;
            ReplyRunFastOverallRecord_L2P replyMsg_L2P = new ReplyRunFastOverallRecord_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.summonerId);
            if (sender == null) return;

            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.summonerId = replyMsg_D2L.summonerId;
            replyMsg_L2P.overallRecord = replyMsg_D2L.overallRecord;
            SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
        }
        public void OnReqRunFastBureauRecord(int peerId, bool inbound, object msg)
        {
            RequestRunFastBureauRecord_P2L reqMsg_P2L = msg as RequestRunFastBureauRecord_P2L;
            RequestRunFastBureauRecord_L2D reqMsg_L2D = new RequestRunFastBureauRecord_L2D();
            
            reqMsg_L2D.summonerId = reqMsg_P2L.summonerId;
            reqMsg_L2D.onlyHouseId = reqMsg_P2L.onlyHouseId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnReplyRunFastBureauRecord(int peerId, bool inbound, object msg)
        {
            ReplyRunFastBureauRecord_D2L replyMsg_D2L = msg as ReplyRunFastBureauRecord_D2L;
            ReplyRunFastBureauRecord_L2P replyMsg_L2P = new ReplyRunFastBureauRecord_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.summonerId);
            if (sender == null) return;

            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.summonerId = replyMsg_D2L.summonerId;
            replyMsg_L2P.onlyHouseId = replyMsg_D2L.onlyHouseId;
            replyMsg_L2P.bureauRecord = replyMsg_D2L.bureauRecord;
            SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
        }
        public void OnReqRunFastBureauPlayback(int peerId, bool inbound, object msg)
        {
            RequestRunFastBureauPlayback_P2L reqMsg_P2L = msg as RequestRunFastBureauPlayback_P2L;
            RequestRunFastBureauPlayback_L2D reqMsg_L2D = new RequestRunFastBureauPlayback_L2D();

            reqMsg_L2D.summonerId = reqMsg_P2L.summonerId;
            reqMsg_L2D.onlyHouseId = reqMsg_P2L.onlyHouseId;
            reqMsg_L2D.bureau = reqMsg_P2L.bureau;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnReplyRunFastBureauPlayback(int peerId, bool inbound, object msg)
        {
            ReplyRunFastBureauPlayback_D2L replyMsg_D2L = msg as ReplyRunFastBureauPlayback_D2L;
            ReplyRunFastBureauPlayback_L2P replyMsg_L2P = new ReplyRunFastBureauPlayback_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.summonerId);
            if (sender == null) return;

            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.summonerId = replyMsg_D2L.summonerId;
            replyMsg_L2P.onlyHouseId = replyMsg_D2L.onlyHouseId;
            replyMsg_L2P.bureau = replyMsg_D2L.bureau;
            replyMsg_L2P.playerCard = replyMsg_D2L.playerCard;
            SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
        }
        public void OnReqCompetitionIntegral(int marketKey, List<ComPlayerIntegral> playerIntegralList)
        {
            if (playerIntegralList.Count > 0)
            {
                ModuleManager.Get<SpecialActivitiesMain>().OnReqCompetitionIntegral(marketKey, playerIntegralList);
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////////
        public void OnRequestSaveHouseId(string userId, ulong houseId)
        {
            ModuleManager.Get<MainCityMain>().msg_proxy.OnRequestSaveHouseId(userId, houseId);
        }
        public void InitSummonerHouseId(Summoner sender)
        {
            sender.houseId = 0;
            OnRequestSaveHouseId(sender.userId, sender.houseId);
        }
        public void PlayerInitHouseIdAndComKey(ulong summonerId)
        {
            ModuleManager.Get<MainCityMain>().PlayerInitHouseIdAndComKey(summonerId);
        }
        public void PlayerInitHouseIdAndComKey(Summoner sender)
        {
            sender.houseId = 0;
            sender.competitionKey = 0;
            PlayerInitHouseIdAndComKey(sender.id);
        }
        public void OnRequestSaveCompetitionKey(ulong summonerId, int competitionKey)
        {
            ModuleManager.Get<MainCityMain>().PlayerSaveCompetitionKey(summonerId, competitionKey, true);
        }
        public void InitSummonerCompetitionKey(Summoner sender)
        {
            sender.competitionKey = 0;
            OnRequestSaveCompetitionKey(sender.id, sender.competitionKey);
        }
        public void OnRequestSaveAllIntegral(string userId, int addIntegral)
        {
            ModuleManager.Get<MainCityMain>().msg_proxy.OnRequestSaveAllIntegral(userId, addIntegral);
        }        
        public void OnRequestSaveHouseCard(ulong guid, OperationType type, int houseCard)
        {
            ModuleManager.Get<MainCityMain>().msg_proxy.OnRequestSaveHouseCard(guid, type, houseCard);
        }
        public void OnRequestSaveTickets(ulong summonerId, TicketsNode ticketsNode)
        {
            ModuleManager.Get<SpecialActivitiesMain>().msg_proxy.OnRequestSaveTickets(summonerId, ticketsNode);
        }
        public void OnRequestSaveCreateRunFastInfo(RunFastHouse runFastHouse, RunFastPlayer housePlayer)
        {
            RequestSaveCreateRunFastInfo_L2D reqMsg_L2D = new RequestSaveCreateRunFastInfo_L2D();
            reqMsg_L2D.houseId = runFastHouse.houseId;
            reqMsg_L2D.houseCardId = runFastHouse.houseCardId;
            reqMsg_L2D.logicId = runFastHouse.logicId;
            reqMsg_L2D.maxBureau = runFastHouse.maxBureau;
            reqMsg_L2D.maxPlayerNum = runFastHouse.maxPlayerNum;
            reqMsg_L2D.businessId = runFastHouse.businessId;
            reqMsg_L2D.housePropertyType = runFastHouse.housePropertyType;
            reqMsg_L2D.houseType = runFastHouse.houseType;
            reqMsg_L2D.runFastType = (int)runFastHouse.runFastType;
            reqMsg_L2D.createTime = runFastHouse.createTime.ToString();
            reqMsg_L2D.summonerId = housePlayer.summonerId;
            reqMsg_L2D.index = housePlayer.index;
            reqMsg_L2D.allIntegral = housePlayer.allIntegral;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveRunFastNewPlayer(ulong houseId, RunFastPlayer housePlayer)
        {
            RequestSaveRunFastNewPlayer_L2D reqMsg_L2D = new RequestSaveRunFastNewPlayer_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.summonerId = housePlayer.summonerId;
            reqMsg_L2D.index = housePlayer.index;
            reqMsg_L2D.allIntegral = housePlayer.allIntegral;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestDelHousePlayer(ulong houseId, ulong summonerId)
        {
            RequestDelHousePlayer_L2D reqMsg_L2D = new RequestDelHousePlayer_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.summonerId = summonerId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSavePlayerSettlement(ulong houseId, RunFastPlayer housePlayer)
        {
            RequestSaveRunFastPlayerSettlement_L2D reqMsg_L2D = new RequestSaveRunFastPlayerSettlement_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.summonerId = housePlayer.summonerId;
            reqMsg_L2D.winBureau = housePlayer.winBureau;
            reqMsg_L2D.loseBureau = housePlayer.loseBureau;
            reqMsg_L2D.allIntegral = housePlayer.allIntegral;
            reqMsg_L2D.bombIntegral = housePlayer.bombIntegral;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveRunFastHouseStatus(ulong houseId, RunFastHouseStatus houseStatus)
        {
            RequestSaveRunFastHouseStatus_L2D reqMsg_L2D = new RequestSaveRunFastHouseStatus_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.houseStatus = houseStatus;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveHouseBureauInfo(ulong houseId, HouseBureau houseBureau, List<PlayerCardNode> playerInitCardList)
        {
            if (houseBureau == null)
            {
                ServerUtil.RecordLog(LogType.Debug, "OnRequestSaveHouseBureauInfo, houseBureau == null ");
                return;
            }
            RequestSaveRunFastBureauInfo_L2D reqMsg_L2D = new RequestSaveRunFastBureauInfo_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.bureau = houseBureau.bureau;
            reqMsg_L2D.bureauTime = houseBureau.bureauTime;
            houseBureau.playerBureauList.ForEach(playerBureau =>
            {
                reqMsg_L2D.playerBureauList.Add(playerBureau.playerIndex);
            });
            reqMsg_L2D.playerInitCard = Serializer.tryCompressObject(main.GetRunFastCardList(playerInitCardList));

            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveBureauIntegral(ulong houseId, HouseBureau houseBureau, int zhuangPlayerIndex)
        {
            if (houseBureau == null)
            {
                ServerUtil.RecordLog(LogType.Debug, "OnRequestSaveBureauIntegral, houseBureau == null ");
                return;
            }
            RequestSaveRunFastBureauIntegral_L2D reqMsg_L2D = new RequestSaveRunFastBureauIntegral_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.bureau = houseBureau.bureau;
            reqMsg_L2D.zhuangPlayerIndex = zhuangPlayerIndex;
            foreach (PlayerBureauIntegral houseBureauNode in houseBureau.playerBureauList)
            {
                //玩家当局积分
                PlayerBureauIntegral integralNode = new PlayerBureauIntegral();
                integralNode.playerIndex = houseBureauNode.playerIndex;
                integralNode.cardIntegral = houseBureauNode.cardIntegral;
                integralNode.bombIntegral = houseBureauNode.bombIntegral;
                integralNode.bureauIntegral = houseBureauNode.bureauIntegral;
                reqMsg_L2D.bureauIntegralList.Add(integralNode);
            }
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveBureauShowCard(RunFastHouse runFastHouse, int playerIndex)
        {
            RequestSaveBureauShowCard_L2D reqMsg_L2D = new RequestSaveBureauShowCard_L2D();
            reqMsg_L2D.houseId = runFastHouse.houseId;
            reqMsg_L2D.bureau = (ulong)runFastHouse.currentBureau;
            reqMsg_L2D.playerCard = new PlayerCardNode();
            reqMsg_L2D.playerCard.index = playerIndex;
            reqMsg_L2D.playerCard.cardList.AddRange(runFastHouse.currentCardList);
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestHouseInfo(int dbServerID)
        {
            RequestHouseInfo_L2D reqMsg_L2D = new RequestHouseInfo_L2D();
            reqMsg_L2D.logicId = root.ServerID;
            SendDBMsg(reqMsg_L2D, dbServerID);
        }
        public void OnRequestHousePlayerAndBureau(ulong houseId)
        {
            RequestHousePlayerAndBureau_L2D reqMsg_L2D = new RequestHousePlayerAndBureau_L2D();
            reqMsg_L2D.houseId = houseId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnnRequestSaveDissolveRunFastInfo(RunFastHouse runFastHouse)
        {
            RequestSaveDissolveRunFastInfo_L2D reqMsg_L2D = new RequestSaveDissolveRunFastInfo_L2D();
            reqMsg_L2D.houseId = runFastHouse.houseId;
            reqMsg_L2D.currentBureau = (ulong)runFastHouse.currentBureau;
            reqMsg_L2D.playerBureauList.AddRange(runFastHouse.GetHouseBureau().playerBureauList);
            runFastHouse.GetRunFastPlayer().ForEach(housePlayer =>
            {
                reqMsg_L2D.playerIntegralList.Add(new PlayerIntegral { playerIndex = housePlayer.index, integral = housePlayer.allIntegral });
            });
            SendDBMsg(reqMsg_L2D);
        }
    }
}
#endif
