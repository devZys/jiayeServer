#if WORDPLATE
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

namespace LegendServerLogic.WordPlate
{
    public class WordPlateMsgProxy : ServerMsgProxy
    {
        private WordPlateMain main;

        public WordPlateMsgProxy(WordPlateMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnReqCreateWordPlateHouse(int peerId, bool inbound, object msg)
        {
            RequestCreateWordPlateHouse_P2L reqMsg = msg as RequestCreateWordPlateHouse_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;
            
            OnReqCreateWordPlateHouse(sender, reqMsg.maxWinScore, reqMsg.maxBureau, reqMsg.wordPlateType, reqMsg.housePropertyType, reqMsg.baseScore);
        }
        public void OnReqCreateWordPlateHouse(Summoner sender, int maxWinScore, int maxBureau, WordPlateType wordPlateType, int housePropertyType, int baseWinScore, int businessId = 0)
        {
            ReplyCreateWordPlateHouse_L2P replyMsg = new ReplyCreateWordPlateHouse_L2P();
            replyMsg.summonerId = sender.id;

            if (sender.houseId > 0)
            {
                //已经有房间号了
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateWordPlateHouse, 已经有房间号了! userId = " + sender.userId + ", houseId = " + sender.houseId);
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
                    ServerUtil.RecordLog(LogType.Debug, "OnReqCreateWordPlateHouse, 报名参加了比赛场! userId = " + sender.userId + ", competitionKey = " + sender.competitionKey);
                    replyMsg.result = ResultCode.PlayerJoinCompetition;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
            }
            if (!main.whzBasaScoreList.Exists(element => element == baseWinScore))
            {
                //选择基础胡息有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateWordPlateHouse, 选择基础胡息有误! userId = " + sender.userId + ", maxWinScore = " + baseWinScore);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (!main.whzMaxScoreList.Exists(element => element == maxWinScore))
            {
                //选择最大胡息有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateWordPlateHouse, 选择最大胡息有误! userId = " + sender.userId + ", maxWinScore = " + maxWinScore);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (!main.CheckOpenCreateHouse())
            {
                //已经关闭创建房间接口
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateWordPlateHouse, 已经关闭创建房间接口! userId = " + sender.userId);
                replyMsg.result = ResultCode.ClosedCreateHouse;
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
                    ServerUtil.RecordLog(LogType.Debug, "OnReqCreateMahjongHouse, 房卡不够! userId = " + sender.userId + ", maxBureau = " + maxBureau);
                    replyMsg.result = ResultCode.HouseCardNotEnough;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
            }
            //没啥问题 我要开始请求房间Id啦
            ModuleManager.Get<UIDAllocMain>().msg_proxy.RequestUID(UIDType.RoomID, sender.id, new PreWordPlateRoomInfo(WordPlateConstValue.WordPlateMaxPlayer, maxBureau, wordPlateType, housePropertyType, maxWinScore, baseWinScore, businessId));
        }
        public bool OnCreateWordPlateHouse(ulong summonerId, int houseCardId, PreWordPlateRoomInfo wordPlateRoomInfo)
        {
            Summoner sender = SummonerManager.Instance.GetSummonerById(summonerId);
            if (sender == null) return false;

            ReplyCreateWordPlateHouse_L2P replyMsg = new ReplyCreateWordPlateHouse_L2P();
            replyMsg.summonerId = sender.id;
            
            if (houseCardId < 100000 || houseCardId >= 1000000)
            {
                //获取房间Id出错
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateWordPlateHouse, 获取房间Id出错! userId = " + sender.userId + ", houseCardId = " + houseCardId);
                replyMsg.result = ResultCode.GetHouseIdError;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return false;
            }
            WordPlateHouse wordPlateHouse = new WordPlateHouse();
            if (wordPlateHouse.SetWordPlateStrategy(wordPlateRoomInfo.wordPlateType))
            {
                //选择字牌逻辑类有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateWordPlateHouse, 选择字牌逻辑类有误! userId = " + sender.userId + ", wordPlateType = " + wordPlateRoomInfo.wordPlateType);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return false;
            }
            wordPlateHouse.houseId = MyGuid.NewGuid(ServiceType.Logic, (uint)root.ServerID);
            wordPlateHouse.houseCardId = houseCardId;
            wordPlateHouse.logicId = root.ServerID;
            wordPlateHouse.maxPlayerNum = wordPlateRoomInfo.maxPlayerNum;
            wordPlateHouse.maxBureau = wordPlateRoomInfo.maxBureau;
            wordPlateHouse.wordPlateType = wordPlateRoomInfo.wordPlateType;
            wordPlateHouse.maxWinScore = wordPlateRoomInfo.maxWinScore;
            wordPlateHouse.baseWinScore = wordPlateRoomInfo.baseWinScore;
            wordPlateHouse.housePropertyType = wordPlateRoomInfo.housePropertyType;
            wordPlateHouse.businessId = wordPlateRoomInfo.businessId;
            wordPlateHouse.createTime = DateTime.Now;
            wordPlateHouse.houseStatus = WordPlateHouseStatus.EWPS_FreeBureau;
            WordPlatePlayer newHousePlayer = wordPlateHouse.CreatHouse(sender);

            HouseManager.Instance.AddHouse(wordPlateHouse.houseId, wordPlateHouse);

            sender.houseId = wordPlateHouse.houseId;

            replyMsg.result = ResultCode.OK;
            replyMsg.maxBureau = wordPlateHouse.maxBureau;
            replyMsg.wordPlateType = wordPlateHouse.wordPlateType;
            replyMsg.maxPlayerNum = wordPlateHouse.maxPlayerNum;
            replyMsg.maxWinScore = wordPlateHouse.maxWinScore;
            replyMsg.baseScore = wordPlateHouse.baseWinScore;
            replyMsg.housePropertyType = wordPlateHouse.housePropertyType;
            replyMsg.allIntegral = newHousePlayer.allIntegral;
            replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
            replyMsg.houseId = houseCardId;
            replyMsg.businessId = wordPlateHouse.businessId;
            replyMsg.onlyHouseId = wordPlateHouse.houseId;
            SendProxyMsg(replyMsg, sender.proxyServerId);

            //存数据库
            OnRequestSaveCreateWordPlateInfo(wordPlateHouse, newHousePlayer);
            return true;
        }
        //比赛场专用接口
        public void OnReqCreateWordPlateHouse(PlayerInfo playerInfo, WordPlateHouse wordPlateHouse)
        {
            WordPlatePlayer newHousePlayer = wordPlateHouse.CreatHouse(playerInfo);

            HouseManager.Instance.AddHouse(wordPlateHouse.houseId, wordPlateHouse);

            Summoner sender = SummonerManager.Instance.GetSummonerById(playerInfo.summonerId);
            if (sender != null)
            {
                //在线
                sender.houseId = wordPlateHouse.houseId;
                newHousePlayer.proxyServerId = sender.proxyServerId;

                ReplyCreateWordPlateHouse_L2P replyMsg = new ReplyCreateWordPlateHouse_L2P();
                replyMsg.result = ResultCode.OK;
                replyMsg.summonerId = sender.id;
                replyMsg.maxBureau = wordPlateHouse.maxBureau;
                replyMsg.wordPlateType = wordPlateHouse.wordPlateType;
                replyMsg.maxWinScore = wordPlateHouse.maxWinScore;
                replyMsg.maxPlayerNum = wordPlateHouse.maxPlayerNum;
                replyMsg.baseScore = wordPlateHouse.baseWinScore;
                replyMsg.housePropertyType = wordPlateHouse.housePropertyType;
                replyMsg.allIntegral = newHousePlayer.allIntegral;
                replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
                replyMsg.houseId = wordPlateHouse.houseCardId;
                replyMsg.onlyHouseId = wordPlateHouse.houseId;
                replyMsg.businessId = wordPlateHouse.businessId;
                replyMsg.competitionKey = wordPlateHouse.competitionKey;
                SendProxyMsg(replyMsg, sender.proxyServerId);
            }
            else
            {
                newHousePlayer.lineType = LineType.OffLine;
            }

            //存数据库
            OnRequestSaveCreateWordPlateInfo(wordPlateHouse, newHousePlayer);
        }
        public void OnReqJoinWordPlateHouse(int peerId, bool inbound, object msg)
        {
            RequestJoinWordPlateHouse_P2L reqMsg = msg as RequestJoinWordPlateHouse_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            OnReqJoinWordPlateHouse(sender, reqMsg.houseId);
        }
        public void OnReqJoinWordPlateHouse(Summoner sender, int houseCardId)
        {
            ReplyJoinWordPlateHouse_L2P replyMsg = new ReplyJoinWordPlateHouse_L2P();
            replyMsg.summonerId = sender.id;

            if (sender.houseId > 0)
            {
                //已经有房间号了
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinWordPlateHouse, 已经有房间号了! userId = " + sender.userId + ", houseId = " + sender.houseId);
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
                    ServerUtil.RecordLog(LogType.Debug, "OnReqJoinWordPlateHouse, 报名参加了比赛场! userId = " + sender.userId + ", competitionKey = " + sender.competitionKey);
                    replyMsg.result = ResultCode.PlayerJoinCompetition;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
            }
            House house = HouseManager.Instance.GetHouseById(houseCardId);
            if (house == null || house.houseType != HouseType.WordPlateHouse)
            {
                //本逻辑服务器找不到房间则从世界服务器找
                RequestHouseBelong_L2W reqMsg = new RequestHouseBelong_L2W();
                reqMsg.summonerId = sender.id;
                reqMsg.houseId = houseCardId;
                SendWorldMsg(reqMsg);
                return;
            }
            WordPlateHouse wordPlateHouse = house as WordPlateHouse;
            JoinWordPlateHouse(sender, wordPlateHouse, replyMsg);
        }
        public void JoinWordPlateHouse(Summoner sender, WordPlateHouse wordPlateHouse, ReplyJoinWordPlateHouse_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyJoinWordPlateHouse_L2P();
                replyMsg.summonerId = sender.id;
                replyMsg.houseId = wordPlateHouse.houseCardId;
            }

            if (wordPlateHouse.houseStatus >= WordPlateHouseStatus.EWPS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinWordPlateHouse, 房间不存在! userId = " + sender.userId + ", houseCardId = " + wordPlateHouse.houseCardId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (wordPlateHouse.CheckPlayer(sender.userId))
            {
                //已经在房间里面了
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinWordPlateHouse, 已经在房间里面了! userId = " + sender.userId + ", houseCardId = " + wordPlateHouse.houseCardId);
                replyMsg.result = ResultCode.PlayerHasBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (wordPlateHouse.CheckPlayerFull())
            {
                //房间已满
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinWordPlateHouse, 房间已满! userId = " + sender.userId + ", houseCardId = " + wordPlateHouse.houseCardId);
                replyMsg.result = ResultCode.TheHouseIsFull;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlatePlayer newHousePlayer = wordPlateHouse.AddPlayer(sender);
            WordPlatePlayerShowNode newPlayerShow = main.GetPlayerShowNode(newHousePlayer);

            sender.houseId = wordPlateHouse.houseId;

            List<WordPlatePlayerShowNode> playerShowList = new List<WordPlatePlayerShowNode>();
            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != sender.userId)
                {
                    playerShowList.Add(main.GetPlayerShowNode(housePlayer));
                    OnRecvJoinWordPlateHouse(housePlayer.summonerId, housePlayer.proxyServerId, newPlayerShow);
                }
            });

            replyMsg.result = ResultCode.OK;
            replyMsg.playerShow = Serializer.tryCompressObject(playerShowList);
            replyMsg.myIndex = newHousePlayer.index;
            replyMsg.allIntegral = newHousePlayer.allIntegral;
            replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
            replyMsg.maxBureau = wordPlateHouse.maxBureau;
            replyMsg.wordPlateType = wordPlateHouse.wordPlateType;
            replyMsg.maxWinScore = wordPlateHouse.maxWinScore;
            replyMsg.maxPlayerNum = wordPlateHouse.maxPlayerNum;
            replyMsg.housePropertyType = wordPlateHouse.housePropertyType;
            replyMsg.baseScore = wordPlateHouse.baseWinScore;
            replyMsg.businessId = wordPlateHouse.businessId;
            replyMsg.houseId = wordPlateHouse.houseCardId;
            replyMsg.onlyHouseId = wordPlateHouse.houseId;
            SendProxyMsg(replyMsg, sender.proxyServerId);

            //保存玩家数据
            OnRequestSaveWordPlateNewPlayer(wordPlateHouse.houseId, newHousePlayer);
            //开局
            DisBeginWordPlates(wordPlateHouse);
        }
        //比赛场专用接口
        public void OnReqJoinWordPlateHouse(PlayerInfo playerInfo, WordPlateHouse wordPlateHouse)
        {
            WordPlatePlayer newHousePlayer = wordPlateHouse.AddPlayer(playerInfo);

            WordPlatePlayerShowNode newPlayer = main.GetPlayerShowNode(newHousePlayer);
            List<WordPlatePlayerShowNode> playerShowList = new List<WordPlatePlayerShowNode>();
            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != playerInfo.userId)
                {
                    playerShowList.Add(main.GetPlayerShowNode(housePlayer));
                    if (housePlayer.lineType == LineType.OnLine)
                    {
                        OnRecvJoinWordPlateHouse(housePlayer.summonerId, housePlayer.proxyServerId, newPlayer);
                    }
                }
            });

            Summoner sender = SummonerManager.Instance.GetSummonerById(playerInfo.summonerId);
            if (sender != null)
            {
                //在线
                sender.houseId = wordPlateHouse.houseId;
                newHousePlayer.proxyServerId = sender.proxyServerId;

                ReplyJoinWordPlateHouse_L2P replyMsg = new ReplyJoinWordPlateHouse_L2P();
                replyMsg.result = ResultCode.OK;
                replyMsg.summonerId = sender.id;
                replyMsg.playerShow = Serializer.tryCompressObject(playerShowList);
                replyMsg.myIndex = newHousePlayer.index;
                replyMsg.allIntegral = newHousePlayer.allIntegral;
                replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
                replyMsg.maxBureau = wordPlateHouse.maxBureau;
                replyMsg.wordPlateType = wordPlateHouse.wordPlateType;
                replyMsg.maxWinScore = wordPlateHouse.maxWinScore;
                replyMsg.maxPlayerNum = wordPlateHouse.maxPlayerNum;
                replyMsg.baseScore = wordPlateHouse.baseWinScore;
                replyMsg.housePropertyType = wordPlateHouse.housePropertyType;
                replyMsg.houseId = wordPlateHouse.houseCardId;
                replyMsg.onlyHouseId = wordPlateHouse.houseId;
                replyMsg.businessId = wordPlateHouse.businessId;
                replyMsg.competitionKey = wordPlateHouse.competitionKey;
                SendProxyMsg(replyMsg, sender.proxyServerId);
            }
            else
            {
                newHousePlayer.lineType = LineType.OffLine;
            }

            //保存玩家数据
            OnRequestSaveWordPlateNewPlayer(wordPlateHouse.houseId, newHousePlayer);
            //开局
            DisBeginWordPlates(wordPlateHouse);
        }
        public void OnRecvJoinWordPlateHouse(ulong summonerId, int proxyServerId, WordPlatePlayerShowNode newPlayerShow)
        {
            RecvJoinWordPlateHouse_L2P recvMsg = new RecvJoinWordPlateHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.playerShow = newPlayerShow;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void DisBeginWordPlates(WordPlateHouse wordPlateHouse)
        {
            if (wordPlateHouse.CheckBeginWordPlates())
            {
                main.AddActionHouse(wordPlateHouse.houseId, 1);
            }
        }
        public void BeginWordPlates(WordPlateHouse wordPlateHouse)
        {
            WordPlateHouseBureau houseBureau = new WordPlateHouseBureau();
            List<PlayerTileNode> playerInitTileList = new List<PlayerTileNode>();
            //处理一些开局信息
            wordPlateHouse.BeginWordPlates(houseBureau);
            //获取神牌
            WordPlateTile godTile = wordPlateHouse.GetGodWordPlateTile();
            //发送开始信息给玩家
            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                RecvBeginWordPlate_L2P recvMsg = new RecvBeginWordPlate_L2P();
                recvMsg.summonerId = housePlayer.summonerId;
                recvMsg.zhuangIndex = wordPlateHouse.currentShowCard;
                recvMsg.currentBureau = wordPlateHouse.currentBureau;
                recvMsg.currentShowCard = wordPlateHouse.currentShowCard;
                recvMsg.houseStatus = wordPlateHouse.houseStatus;
                recvMsg.housePlayerStatus = housePlayer.housePlayerStatus;
                recvMsg.remainWordPlateCount = wordPlateHouse.GetRemainWordPlateCount();
                if(godTile != null)
                {
                    recvMsg.godWordPlateTile = godTile.GetWordPlateNode();
                }
                housePlayer.GetPlayerHandTileList(recvMsg.wordPlateList);
                SendProxyMsg(recvMsg, housePlayer.proxyServerId);
                //玩家初始化手牌
                PlayerTileNode playerTileNode = new PlayerTileNode();
                playerTileNode.playerIndex = housePlayer.index;
                playerTileNode.tileList.AddRange(recvMsg.wordPlateList);
                playerInitTileList.Add(playerTileNode);
            });
            //开局保存每局信息
            OnRequestSaveHouseBureauInfo(wordPlateHouse.houseId, houseBureau, godTile, playerInitTileList);
            if (wordPlateHouse.SetHouseOperateBeginTime() && wordPlateHouse.currentBureau == 1)
            {
                main.CheckHouseOperateTimer();
            }
        }
        public void OnReqQuitWordPlateHouse(int peerId, bool inbound, object msg)
        {
            RequestQuitWordPlateHouse_P2L reqMsg = msg as RequestQuitWordPlateHouse_P2L;
            ReplyQuitWordPlateHouse_L2P replyMsg = new ReplyQuitWordPlateHouse_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号，不需要退出
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitWordPlateHouse, 没有房间号，不需要退出! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.WordPlateHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitWordPlateHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlateHouse wordPlateHouse = house as WordPlateHouse;
            if (wordPlateHouse == null || wordPlateHouse.houseStatus >= WordPlateHouseStatus.EWPS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitWordPlateHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (wordPlateHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间正在投票中
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitWordPlateHouse, 房间正在投票中! userId = " + sender.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.HouseGoingDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (wordPlateHouse.competitionKey > 0)
            {
                //比赛场房间不能投票
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitWordPlateHouse, 比赛场房间不能投票! userId = " + sender.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.CompetitionNoDissolve;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlatePlayer player = wordPlateHouse.GetWordPlatePlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitWordPlateHouse, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            replyMsg.dissolveVoteTime = main.houseDissolveVoteTime;
            if (wordPlateHouse.businessId > 0)
            {
                replyMsg.dissolveVoteTime = main.businessDissolveVoteTime;
            }
            bool bVote = false;
            if (wordPlateHouse.CheckPlayerFull() && wordPlateHouse.houseStatus != WordPlateHouseStatus.EWPS_FreeBureau)
            {
                //要投票
                bVote = true;
                wordPlateHouse.voteBeginTime = DateTime.Now;
                player.voteStatus = VoteStatus.LaunchVote;
                wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
                {
                    if (sender.userId != housePlayer.userId)
                    {
                        OnRecvQuitWordPlateHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index, bVote, replyMsg.dissolveVoteTime);
                    }
                });
                main.AddDissolveVoteHouse(wordPlateHouse.houseId);
            }
            else
            {
                //房主
                if (player.index == 0 && wordPlateHouse.businessId == 0)
                {
                    wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
                    {
                        if (housePlayer.userId != sender.userId)
                        {
                            Summoner houseSender = SummonerManager.Instance.GetSummonerByUserId(housePlayer.userId);
                            if (houseSender != null)
                            {
                                houseSender.houseId = 0;
                                OnRecvQuitWordPlateHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index);
                            }
                            //保存DB
                            OnRequestSaveHouseId(housePlayer.userId, 0);
                        }
                    });
                    //清除
                    InitSummonerHouseId(sender);
                    wordPlateHouse.houseStatus = WordPlateHouseStatus.EWPS_Dissolved;
                    //保存房间状态
                    OnRequestSaveWordPlateHouseStatus(wordPlateHouse.houseId, wordPlateHouse.houseStatus);
                    //删除房间
                    HouseManager.Instance.RemoveHouse(wordPlateHouse.houseId);
                }
                else
                {
                    wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
                    {
                        if (housePlayer.userId != sender.userId)
                        {
                            OnRecvLeaveWordPlateHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index);
                        }
                    });
                    sender.houseId = 0;
                    //删除用户
                    wordPlateHouse.RemovePlayer(sender.userId);
                    //保存数据库删除玩家
                    OnRequestDelWordPlateHousePlayer(wordPlateHouse.houseId, sender.id);
                }
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.bVote = bVote;
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void OnRecvQuitWordPlateHouse(ulong summonerId, int proxyServerId, int index, bool bVote = false, int dissolveVoteTime = 0)
        {
            RecvQuitWordPlateHouse_L2P recvMsg = new RecvQuitWordPlateHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.bVote = bVote;
            recvMsg.index = index;
            recvMsg.dissolveVoteTime = dissolveVoteTime;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnRecvLeaveWordPlateHouse(ulong summonerId, int proxyServerId, int index)
        {
            RecvLeaveWordPlateHouse_L2P recvMsg = new RecvLeaveWordPlateHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.leaveIndex = index;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnReqDissolveHouseVote(int peerId, bool inbound, object msg)
        {
            RequestWordPlateHouseVote_P2L reqMsg = msg as RequestWordPlateHouseVote_P2L;
            ReplyWordPlateHouseVote_L2P replyMsg = new ReplyWordPlateHouseVote_L2P();

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
            if (house == null || house.houseType != HouseType.WordPlateHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlateHouse wordPlateHouse = house as WordPlateHouse;
            if (wordPlateHouse == null || wordPlateHouse.houseStatus >= WordPlateHouseStatus.EWPS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (wordPlateHouse.voteBeginTime == DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间没有发起投票
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 房间没有发起投票! userId = " + sender.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.HouseNoNeedDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlatePlayer player = wordPlateHouse.GetWordPlatePlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqDissolveHouseVote(wordPlateHouse, player, reqMsg.voteStatus, replyMsg);
        }
        public void OnReqDissolveHouseVote(WordPlateHouse wordPlateHouse, WordPlatePlayer player, VoteStatus voteStatus = VoteStatus.AgreeVote, ReplyWordPlateHouseVote_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyWordPlateHouseVote_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //发起者
            WordPlatePlayer launchPlayer = wordPlateHouse.GetWordPlateVoteLaunchPlayer();
            if (launchPlayer == null)
            {
                //发起者不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 发起者不存在! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (launchPlayer.index == player.index)
            {
                //发起者不能投票
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 发起者不能投票! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (player.voteStatus != VoteStatus.FreeVote)
            {
                //玩家已经投过票了
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 玩家已经投过票了! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            player.voteStatus = voteStatus;

            //判断是否能够解散(free 表示继续等投票 agree 表示要解散 oppose 表示解散失败)
            VoteStatus houseVoteStatus = wordPlateHouse.GetDissolveHouseVote();
            //告诉其他人
            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                if (player.userId != housePlayer.userId)
                {
                    OnRecvWordPlateHouseVote(housePlayer.summonerId, housePlayer.proxyServerId, player.index, voteStatus, houseVoteStatus);
                }
            });

            replyMsg.result = ResultCode.OK;
            replyMsg.voteStatus = voteStatus;
            replyMsg.houseVoteStatus = houseVoteStatus;
            SendProxyMsg(replyMsg, player.proxyServerId);

            //处理投票
            DissolveHouseVote(wordPlateHouse, houseVoteStatus);
        }
        public void OnRecvWordPlateHouseVote(ulong summonerId, int proxyServerId, int index, VoteStatus voteStatus, VoteStatus houseVoteStatus)
        {
            RecvWordPlateHouseVote_L2P recvMsg = new RecvWordPlateHouseVote_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.index = index;
            recvMsg.voteStatus = voteStatus;
            recvMsg.houseVoteStatus = houseVoteStatus;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void DissolveHouseVote(WordPlateHouse wordPlateHouse, VoteStatus houseVoteStatus)
        {
            if (houseVoteStatus == VoteStatus.AgreeVote)
            {
                //解散成功
                OnHouseEndSettlement(wordPlateHouse, WordPlateHouseStatus.EWPS_Dissolved);
                main.DelDissolveVoteHouse(wordPlateHouse.houseId);
            }
            else if (houseVoteStatus == VoteStatus.OpposeVote)
            {
                //解散失败
                wordPlateHouse.voteBeginTime = DateTime.Parse("1970-01-01 00:00:00");
                wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
                {
                    if (housePlayer.voteStatus != VoteStatus.FreeVote)
                    {
                        housePlayer.voteStatus = VoteStatus.FreeVote;
                    }
                });
                main.DelDissolveVoteHouse(wordPlateHouse.houseId);
            }
        }
        public void OnReqReadyWordPlateHouse(int peerId, bool inbound, object msg)
        {
            RequestReadyWordPlateHouse_P2L reqMsg = msg as RequestReadyWordPlateHouse_P2L;
            ReplyReadyWordPlateHouse_L2P replyMsg = new ReplyReadyWordPlateHouse_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号，不需要退出
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyWordPlateHouse, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.WordPlateHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyWordPlateHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlateHouse wordPlateHouse = house as WordPlateHouse;
            if (wordPlateHouse == null || (wordPlateHouse.currentBureau > 0 && wordPlateHouse.houseStatus != WordPlateHouseStatus.EWPS_Settlement) || 
               (wordPlateHouse.currentBureau == 0 && wordPlateHouse.houseStatus != WordPlateHouseStatus.EWPS_FreeBureau))
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyWordPlateHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlatePlayer player = wordPlateHouse.GetWordPlatePlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyWordPlateHouse, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqReadyWordPlateHouse(wordPlateHouse, player, replyMsg);
        }
        public void OnReqReadyWordPlateHouse(WordPlateHouse wordPlateHouse, WordPlatePlayer player, ReplyReadyWordPlateHouse_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyReadyWordPlateHouse_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            player.housePlayerStatus = WordPlatePlayerStatus.WordPlateReady;

            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                if (player.userId != housePlayer.userId)
                {
                    OnRecvReadyWordPlateHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index);
                }
            });

            replyMsg.result = ResultCode.OK;
            SendProxyMsg(replyMsg, player.proxyServerId);

            //开局
            DisBeginWordPlates(wordPlateHouse);
        }
        public void OnRecvReadyWordPlateHouse(ulong summonerId, int proxyServerId, int readyIndex)
        {
            RecvReadyWordPlateHouse_L2P recvMsg = new RecvReadyWordPlateHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.readyIndex = readyIndex;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnReqWordPlateHouseInfo(int peerId, bool inbound, object msg)
        {
            RequestWordPlateHouseInfo_P2L reqMsg = msg as RequestWordPlateHouseInfo_P2L;
            ReplyWordPlateHouseInfo_L2P replyMsg = new ReplyWordPlateHouseInfo_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqWordPlateHouseInfo, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.WordPlateHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqWordPlateHouseInfo, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                InitSummonerHouseId(sender);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlateHouse wordPlateHouse = house as WordPlateHouse;
            if (wordPlateHouse == null || wordPlateHouse.houseStatus >= WordPlateHouseStatus.EWPS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqWordPlateHouseInfo, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
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
                main.OnRecvGMDissolveWordPlateHouse(wordPlateHouse);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlatePlayer player = wordPlateHouse.GetWordPlatePlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqWordPlateHouseInfo, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                InitSummonerHouseId(sender);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            //返回房间信息
            replyMsg.houseId = wordPlateHouse.houseCardId;
            replyMsg.onlyHouseId = wordPlateHouse.houseId;
            replyMsg.wordPlateType = wordPlateHouse.wordPlateType;
            replyMsg.currentBureau = wordPlateHouse.currentBureau;
            replyMsg.maxBureau = wordPlateHouse.maxBureau;
            replyMsg.currentShowCard = wordPlateHouse.currentShowCard;
            replyMsg.currentWhoPlay = wordPlateHouse.currentWhoPlay;
            replyMsg.houseStatus = wordPlateHouse.houseStatus;
            replyMsg.housePropertyType = wordPlateHouse.housePropertyType;
            replyMsg.baseScore = wordPlateHouse.baseWinScore;
            replyMsg.maxWinScore = wordPlateHouse.maxWinScore;
            replyMsg.maxPlayerNum = wordPlateHouse.maxPlayerNum;
            replyMsg.beginGodType = wordPlateHouse.beginGodType;
            replyMsg.businessId = wordPlateHouse.businessId;
            replyMsg.competitionKey = wordPlateHouse.competitionKey;
            replyMsg.remainWordPlateCount = wordPlateHouse.GetRemainWordPlateCount();
            wordPlateHouse.GetPlayerOperat(player.index, replyMsg.operatTypeList);
            replyMsg.zhuangIndex = wordPlateHouse.GetHouseZhuangIndex();
            replyMsg.bIsPlayerShow = wordPlateHouse.bPlayerShowPlate;
            replyMsg.currentWordPlate = wordPlateHouse.GetCurrentWordPlateNode();
            //获取玩家信息
            WordPlateOnlineNode wordPlateOnlineNode = new WordPlateOnlineNode();
            wordPlateOnlineNode.myPlayerOnline = main.GetMyPlayerOnlineNode(player);
            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != player.userId)
                {
                    wordPlateOnlineNode.playerOnlineList.Add(main.GetPlayerOnlineNode(housePlayer));
                    if (player.lineType != LineType.OnLine)
                    {
                        ModuleManager.Get<MainCityMain>().OnRecvPlayerLineType(housePlayer.summonerId, housePlayer.proxyServerId, player.index, LineType.OnLine);
                    }
                }
            });
            replyMsg.wordPlateOnlineNode = Serializer.tryCompressObject(wordPlateOnlineNode);
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
            if (wordPlateHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                TimeSpan span = DateTime.Now.Subtract(wordPlateHouse.voteBeginTime);
                if (span.TotalSeconds < main.houseDissolveVoteTime)
                {
                    replyMsg.houseVoteTime = main.houseDissolveVoteTime - span.TotalSeconds;
                }
            }

            replyMsg.result = ResultCode.OK;
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void OnReqShowWordPlate(int peerId, bool inbound, object msg)
        {
            RequestShowWordPlate_P2L reqMsg = msg as RequestShowWordPlate_P2L;
            ReplyShowWordPlate_L2P replyMsg = new ReplyShowWordPlate_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowWordPlate, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (reqMsg.wordPlateNode == 0)
            {
                //出牌字牌错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowWordPlate, 出牌字牌错误! userId = " + sender.userId + ", reqMsg.wordPlateNode == null");
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.WordPlateHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowWordPlate, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlateHouse wordPlateHouse = house as WordPlateHouse;
            if (wordPlateHouse == null || wordPlateHouse.houseStatus != WordPlateHouseStatus.EWPS_BeginBureau)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowWordPlate, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (wordPlateHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间正在投票中
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowWordPlate, 房间正在投票中! userId = " + sender.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.HouseGoingDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlateTile showWordPlateTile = new WordPlateTile(reqMsg.wordPlateNode);
            if (showWordPlateTile == null)
            {
                //玩家出牌有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowWordPlate, 玩家出牌有误! wordPlateNode = " + reqMsg.wordPlateNode);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlatePlayer player = wordPlateHouse.GetWordPlatePlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowWordPlate, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (player.index != wordPlateHouse.currentShowCard || player.housePlayerStatus != WordPlatePlayerStatus.WordPlateWaitCard)
            {
                //玩家不是出牌状态
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowWordPlate, 玩家不是出牌状态! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqShowWordPlate(wordPlateHouse, player, showWordPlateTile, replyMsg);
        }
        public void OnReqShowWordPlate(WordPlateHouse wordPlateHouse, WordPlatePlayer player, WordPlateTile showWordPlateTile, ReplyShowWordPlate_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyShowWordPlate_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            if (!player.CheckPlayerShowWordPlate())
            {
                //玩家手牌不能出牌
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowWordPlate, 玩家手牌不能出牌! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (!player.CheckPlayerWordPlate(showWordPlateTile))
            {
                //发来的牌不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowWordPlate, 发来的牌不存在! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            player.housePlayerStatus = WordPlatePlayerStatus.WordPlateShowCard;
            //从手牌中删除
            player.DelPlayerWordPlate(showWordPlateTile, true);
            //可以胡牌了
            if (player.m_bGiveUpWin)
            {
                player.m_bGiveUpWin = false;
            }
            //死手
            if (player.DeadHandMeldCheck(showWordPlateTile))
            {
                player.m_bDeadHand = true;
            }
            //清理上一次歪牌
            if (player.lastOperatTile != null)
            {
                player.lastOperatTile = null;
            }
            //设置臭牌
            player.SetShowPassTile(showWordPlateTile);

            //玩家牌
            wordPlateHouse.currentShowCard = -1;
            wordPlateHouse.currentWhoPlay = player.index;
            wordPlateHouse.currentWordPlate = showWordPlateTile;
            wordPlateHouse.bPlayerShowPlate = true;

            replyMsg.result = ResultCode.OK;
            replyMsg.bGiveUpWin = player.m_bGiveUpWin;
            replyMsg.bDeadHand = player.m_bDeadHand;
            replyMsg.wordPlateNode = showWordPlateTile.GetWordPlateNode();
            SendProxyMsg(replyMsg, player.proxyServerId);

            //保存出牌
            OnRequestSaveShowWordPlate(wordPlateHouse.houseId, wordPlateHouse.currentBureau, player.index, replyMsg.wordPlateNode);

            //处理出牌
            if (!DiposeShowWordPlate(player.index, wordPlateHouse, showWordPlateTile, player.m_bDeadHand))
            {
                //出牌没人要 增加出牌列表
                player.AddShowWordPlate(showWordPlateTile);
                //发牌
                GiveOffWordPlateToNextPlayer(wordPlateHouse, player.index);
            }
            else
            {
                //设置房间操作时间
                wordPlateHouse.SetHouseOperateBeginTime();
            }
        }
        public bool DiposeShowWordPlate(int playerIndex, WordPlateHouse wordPlateHouse, WordPlateTile wordPlateTile, bool bDeadHand)
        {
            bool bIsAllNeed = false;
            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                if (housePlayer.index != playerIndex)
                {
                    List<WordPlateOperatType> operatTypeList = new List<WordPlateOperatType>();
                    //没有死手
                    if (!housePlayer.m_bDeadHand)
                    {
                        //碰 飘
                        PlateMeldType meldType = housePlayer.PongSlipCheck(wordPlateTile);
                        if (meldType == PlateMeldType.EPM_Flutter || meldType == PlateMeldType.EPM_Pong)
                        {
                            operatTypeList.Add(WordPlateOperatType.EWPO_Pong);
                            if (meldType == PlateMeldType.EPM_Flutter)
                            {
                                operatTypeList.Add(WordPlateOperatType.EWPO_Flutter);
                            }
                        }
                        //下家才能吃
                        if (housePlayer.index == wordPlateHouse.GetNextHousePlayerIndex(playerIndex) && housePlayer.ChowCheck(wordPlateTile))
                        {
                            operatTypeList.Add(WordPlateOperatType.EWPO_Chow);
                        }
                        if (operatTypeList.Count > 0)
                        {
                            wordPlateHouse.AddWordPlateOperat(housePlayer.index, operatTypeList);
                            if (!bIsAllNeed)
                            {
                                bIsAllNeed = true;
                            }
                        }
                    }
                    OnRecvShowWordPlate(housePlayer.summonerId, housePlayer.proxyServerId, playerIndex, operatTypeList, wordPlateTile, bDeadHand);
                }
            });
            return bIsAllNeed;
        }
        public void GiveOffWordPlateToNextPlayer(WordPlateHouse wordPlateHouse, int playerIndex, int waitTime = 1)
        {
            //给下一个玩家发牌
            WordPlatePlayer nextPlayer = wordPlateHouse.GetNextHousePlayer(playerIndex);
            if (nextPlayer != null)
            {
                //延迟用
                wordPlateHouse.waitTime = waitTime;
                main.AddActionHouse(wordPlateHouse.houseId, 2, nextPlayer.index);
            }
        }
        public bool GiveOffWordPlatePlayer(WordPlateHouse wordPlateHouse, int playerIndex)
        {
            //给玩家发牌 并告诉其他玩家
            WordPlateTile newTile = wordPlateHouse.GetNewTileByRemainWordPlate();
            if (newTile == null)
            {
                //流局 
                OnRecvWinWordPlate(wordPlateHouse);
                return false;
            }
            WordPlatePlayer player = wordPlateHouse.GetWordPlatePlayer(playerIndex);
            if (player == null)
            {
                return true;
            }
            int lastPlayerIndex = -1;
            int lastWordPlate = 0;
            if (wordPlateHouse.currentWordPlate != null)
            {
                lastPlayerIndex = wordPlateHouse.currentWhoPlay;
                lastWordPlate = wordPlateHouse.currentWordPlate.GetWordPlateNode();
            }
            //操作
            wordPlateHouse.currentShowCard = -1;
            //记录位置
            wordPlateHouse.currentWhoPlay = player.index;
            //记录牌
            wordPlateHouse.currentWordPlate = newTile;
            //记录玩家
            wordPlateHouse.giveOffPlayerIndex = player.index;
            //系统牌
            wordPlateHouse.bPlayerShowPlate = false;
            //标志摸牌
            player.housePlayerStatus = WordPlatePlayerStatus.WordPlateMoCard;
            //保存发新牌
            OnRequestSaveGiveOffWordPlate(wordPlateHouse.houseId, wordPlateHouse.currentBureau, player.index, newTile, lastPlayerIndex, lastWordPlate);

            //处理摸牌
            if (!DiposeMoWordPlate(player.index, wordPlateHouse, newTile))
            {
                //出牌没人要 增加出牌列表
                player.AddShowWordPlate(newTile);
                //发牌
                GiveOffWordPlateToNextPlayer(wordPlateHouse, player.index, 0);

                return false;
            }

            //设置房间操作时间
            wordPlateHouse.SetHouseOperateBeginTime();
            return true;
        }
        public bool DiposeMoWordPlate(int playerIndex, WordPlateHouse wordPlateHouse, WordPlateTile wordPlateTile)
        {
            bool bIsAllNeed = false;
            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                List<WordPlateOperatType> operatTypeList = new List<WordPlateOperatType>();
                //没有死手
                if (!housePlayer.m_bDeadHand)
                {
                    bool bMySelf = housePlayer.index == playerIndex;
                    if (housePlayer.WinHandTileCheck(wordPlateTile, bMySelf))
                    {
                        operatTypeList.Add(WordPlateOperatType.EWPO_Hu);
                    }
                    //碰 飘
                    PlateMeldType meldType = housePlayer.PongSlipCheck(wordPlateTile);
                    if (meldType == PlateMeldType.EPM_Flutter || meldType == PlateMeldType.EPM_Pong)
                    {
                        if (bMySelf)
                        {
                            operatTypeList.Add(WordPlateOperatType.EWPO_Wai);
                        }
                        else
                        {
                            operatTypeList.Add(WordPlateOperatType.EWPO_Pong);
                        }
                        if (meldType == PlateMeldType.EPM_Flutter)
                        {
                            if (bMySelf)
                            {
                                operatTypeList.Add(WordPlateOperatType.EWPO_Slip);
                            }
                            else
                            {
                                operatTypeList.Add(WordPlateOperatType.EWPO_Flutter);
                            }
                        }
                    }
                    else if (bMySelf)
                    {
                        WordPlateMeld meld = housePlayer.FlutterSlipCheck(wordPlateTile);
                        if (meld != null)
                        {
                            if (meld.m_eMeldType == PlateMeldType.EPM_Wai)
                            {
                                operatTypeList.Add(WordPlateOperatType.EWPO_Slip);
                            }
                            else if (meld.m_eMeldType == PlateMeldType.EPM_Pong)
                            {
                                operatTypeList.Add(WordPlateOperatType.EWPO_Flutter);
                            }
                        }
                    }
                    if (housePlayer.index == wordPlateHouse.GetNextHousePlayerIndex(playerIndex) || bMySelf)
                    {
                        //下家和自己才能吃
                        if (housePlayer.ChowCheck(wordPlateTile))
                        {
                            operatTypeList.Add(WordPlateOperatType.EWPO_Chow);
                        }
                    }
                    if (operatTypeList.Count > 0)
                    {
                        wordPlateHouse.AddWordPlateOperat(housePlayer.index, operatTypeList);
                        if (!bIsAllNeed)
                        {
                            bIsAllNeed = true;
                        }
                    }
                }
                OnRecvShowWordPlate(housePlayer.summonerId, housePlayer.proxyServerId, playerIndex, operatTypeList, wordPlateTile, housePlayer.m_bDeadHand, false);
            });
            return bIsAllNeed;
        }
        public void OnRecvShowWordPlate(ulong summonerId, int proxyServerId, int playerIndex, List<WordPlateOperatType> operatTypeList, WordPlateTile wordPlateTile, bool bDeadHand, bool bIsPlayerShow = true)
        {
            RecvShowWordPlate_L2P recvMsg = new RecvShowWordPlate_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.index = playerIndex;
            recvMsg.operatTypeList.AddRange(operatTypeList);
            recvMsg.wordPlateNode = wordPlateTile.GetWordPlateNode();
            recvMsg.bDeadHand = bDeadHand;
            recvMsg.bIsPlayerShow = bIsPlayerShow;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnReqOperatWordPlate(int peerId, bool inbound, object msg)
        {
            RequestOperatWordPlate_P2L reqMsg = msg as RequestOperatWordPlate_P2L;
            ReplyOperatWordPlate_L2P replyMsg = new ReplyOperatWordPlate_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (reqMsg.wordPlateList == null || (reqMsg.operatType != WordPlateOperatType.EWPO_None && reqMsg.operatType != WordPlateOperatType.EWPO_Hu && reqMsg.wordPlateList.Count == 0))
            {
                //操作字牌错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 操作字牌错误! userId = " + sender.userId + ", reqMsg.wordPlateNode == null");
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.WordPlateHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlateHouse wordPlateHouse = house as WordPlateHouse;
            if (wordPlateHouse == null || wordPlateHouse.houseStatus != WordPlateHouseStatus.EWPS_BeginBureau)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (wordPlateHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间正在投票中
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 房间正在投票中! userId = " + sender.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.HouseGoingDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            WordPlatePlayer player = wordPlateHouse.GetWordPlatePlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (player.m_bDeadHand)
            {
                //玩家已经死手
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 玩家已经死手! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (wordPlateHouse.currentShowCard == -1)
            {
                //操作
                OnReqOperatWordPlate(wordPlateHouse, player, reqMsg.operatType, reqMsg.wordPlateList, replyMsg);
            }
            else if (wordPlateHouse.currentShowCard == player.index)
            {
                //对自己进行操作
                OnReqOperatWordPlateByMyself(wordPlateHouse, player, reqMsg.operatType, reqMsg.wordPlateList, replyMsg);
            }
        }
        public void OnReqOperatWordPlate(WordPlateHouse wordPlateHouse, WordPlatePlayer player, WordPlateOperatType operatType, List<int> wordPlateList, ReplyOperatWordPlate_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyOperatWordPlate_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //别人的
            if (!player.CheckPlayerShowWordPlate(1))
            {
                //玩家手牌不能对别人进行操作
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 玩家手牌不能对别人进行操作! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            bool bMySelf = player.index == wordPlateHouse.currentWhoPlay;
            if (wordPlateHouse.bPlayerShowPlate && bMySelf)
            {
                //玩家自己打的牌不能操作
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 玩家自己打的牌不能操作! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (operatType == WordPlateOperatType.EWPO_Hu)
            {
                if (wordPlateHouse.bPlayerShowPlate)
                {
                    //玩家打的牌不能胡
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 玩家打的牌不能胡! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!wordPlateHouse.CheckCurrentWordPlate(wordPlateList[0]))
                {
                    //胡牌不是当前出的牌
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 胡牌不是当前出的牌! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!player.WinHandTileCheck(wordPlateList, bMySelf))
                {
                    //胡牌逻辑不能胡
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 胡牌逻辑不能胡! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
            }
            else if (operatType == WordPlateOperatType.EWPO_Flutter)
            {
                if (!wordPlateHouse.CheckCurrentWordPlate(wordPlateList[0]))
                {
                    //飘牌不是当前出的牌
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 飘牌不是当前出的牌! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (bMySelf)
                {
                    WordPlateMeld meld = player.FlutterSlipCheck(wordPlateList[0], false);
                    if (meld == null || meld.m_eMeldType != PlateMeldType.EPM_Pong)
                    {
                        //玩家不能飘
                        ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 玩家不能飘! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                        replyMsg.result = ResultCode.Wrong;
                        SendProxyMsg(replyMsg, player.proxyServerId);
                        return;
                    }
                }
                else
                {
                    if (PlateMeldType.EPM_Flutter != player.PongSlipCheck(wordPlateList[0]))
                    {
                        //玩家不能飘
                        ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 玩家不能飘! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                        replyMsg.result = ResultCode.Wrong;
                        SendProxyMsg(replyMsg, player.proxyServerId);
                        return;
                    }
                }
            }
            else if (operatType == WordPlateOperatType.EWPO_Pong)
            {
                if (!wordPlateHouse.CheckCurrentWordPlate(wordPlateList[0]))
                {
                    //碰牌不是当前出的牌
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 碰牌不是当前出的牌! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (bMySelf)
                {
                    //自己碰的叫歪
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 自己碰的叫歪! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                PlateMeldType meldType = player.PongSlipCheck(wordPlateList[0]);
                if (PlateMeldType.EPM_Pong != meldType && PlateMeldType.EPM_Flutter != meldType)
                {
                    //玩家不能碰
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 玩家不能碰! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
            }
            else if (operatType == WordPlateOperatType.EWPO_Slip)
            {
                if (!wordPlateHouse.CheckCurrentWordPlate(wordPlateList[0]))
                {
                    //溜牌不是当前出的牌
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 溜牌不是当前出的牌! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!bMySelf)
                {
                    //溜是自己进行操作
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 溜是自己进行操作! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (PlateMeldType.EPM_Flutter != player.PongSlipCheck(wordPlateList[0]))
                {
                    WordPlateMeld meld = player.FlutterSlipCheck(wordPlateList[0], false);
                    if (meld == null || meld.m_eMeldType != PlateMeldType.EPM_Wai)
                    {
                        //玩家不能溜
                        ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 玩家不能溜! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                        replyMsg.result = ResultCode.Wrong;
                        SendProxyMsg(replyMsg, player.proxyServerId);
                        return;
                    }
                }
            }
            else if (operatType == WordPlateOperatType.EWPO_Wai)
            {
                if (!wordPlateHouse.CheckCurrentWordPlate(wordPlateList[0]))
                {
                    //歪牌不是当前出的牌
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 歪牌不是当前出的牌! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!bMySelf)
                {
                    //歪是自己进行操作
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 歪是自己进行操作! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                PlateMeldType meldType = player.PongSlipCheck(wordPlateList[0]);
                if (PlateMeldType.EPM_Pong != meldType && PlateMeldType.EPM_Flutter != meldType)
                {
                    //玩家不能歪
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 玩家不能歪! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
            }
            else if (operatType == WordPlateOperatType.EWPO_Chow)
            {
                if (wordPlateList.Count != 3)
                {
                    //吃牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 吃牌操作错误 吃的牌个数不对! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!wordPlateHouse.CheckCurrentWordPlate(wordPlateList[0]))
                {
                    //吃牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 吃牌操作错误 吃的牌不再操作列表里面! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if ((!bMySelf && wordPlateHouse.currentWhoPlay != wordPlateHouse.GetLastHousePlayerIndex(player.index)) || !player.ChowCheck(wordPlateList[0]))
                {
                    //吃牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 吃牌操作错误 不能吃目标牌! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!player.CheckChowWordPlate(wordPlateList))
                {
                    //吃牌选牌错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 吃牌选牌错误 选择的牌手牌里面没有! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
            }
            else if (operatType == WordPlateOperatType.EWPO_None)
            {
            }
            else
            {
                //操作类型有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 操作类型有误! userId = " + player.userId + ", operatType = " + operatType);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (!wordPlateHouse.SetWordPlateOperat(player.index, operatType, wordPlateList))
            {
                //操作错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlate, 操作错误! userId = " + player.userId + ", operatType = " + operatType);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            List<WordPlateOperatNode> wordPlateOperatList = wordPlateHouse.GetPlayerWordPlateOperat();
            if (wordPlateOperatList == null || wordPlateOperatList.Count <= 0)
            {
                //告诉玩家操作成功了
                replyMsg.result = ResultCode.OK;
                replyMsg.operatType = operatType;
                replyMsg.currentShowCard = wordPlateHouse.currentShowCard;
                replyMsg.housePlayerStatus = player.housePlayerStatus;
                replyMsg.bGiveUpWin = player.m_bGiveUpWin;
                replyMsg.bDeadHand = player.m_bDeadHand;
                SendProxyMsg(replyMsg, player.proxyServerId);

                //处理发牌
                WordPlatePlayer playPlayer = player;
                if (wordPlateHouse.currentWhoPlay != player.index)
                {
                    playPlayer = wordPlateHouse.GetWordPlatePlayer(wordPlateHouse.currentWhoPlay);
                }
                //都放弃了计算臭牌
                wordPlateHouse.DisWordPlatePassOperat();
                //保存详细操作
                OnRequestSaveOperatInfoWordPlate(wordPlateHouse.houseId, wordPlateHouse.currentBureau, wordPlateHouse.GetWordPlateOperat());
                //清理
                wordPlateHouse.ClearWordPlateOperat();
                //出牌没人要 增加出牌列表
                playPlayer.AddShowWordPlate(wordPlateHouse.currentWordPlate);
                //发牌
                GiveOffWordPlateToNextPlayer(wordPlateHouse, playPlayer.index, 0); 
            }
            else
            {
                //找出优先级最高的
                WordPlateOperatNode operatNode = main.GetWordPlateOperatNode(wordPlateHouse, wordPlateOperatList);
                if (operatNode == null || operatNode.operatType == WordPlateOperatType.EWPO_None)
                {
                    return;
                }
                if (operatNode.bWait)
                {
                    //继续等待，但是告诉玩家操作成功了
                    replyMsg.result = ResultCode.OK;
                    replyMsg.operatType = operatType;
                    replyMsg.currentShowCard = wordPlateHouse.currentShowCard;
                    replyMsg.housePlayerStatus = player.housePlayerStatus;
                    replyMsg.bGiveUpWin = player.m_bGiveUpWin;
                    replyMsg.bDeadHand = player.m_bDeadHand;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                WordPlatePlayer playPlayer = player;
                if (operatNode.playerIndex != player.index)
                {
                    playPlayer = wordPlateHouse.GetWordPlatePlayer(operatNode.playerIndex);
                }
                WordPlateMeld meld = null;
                if (operatNode.operatType == WordPlateOperatType.EWPO_Hu)
                {
                    //保存详细操作
                    OnRequestSaveOperatInfoWordPlate(wordPlateHouse.houseId, wordPlateHouse.currentBureau, wordPlateHouse.GetWordPlateOperat());
                    //胡
                    OnRecvWinWordPlate(wordPlateHouse, playPlayer.index, operatNode.operatWordPlateList[0]);
                }
                else if (operatNode.operatType == WordPlateOperatType.EWPO_NFlutter)
                {
                    meld = playPlayer.Flutter(operatNode.operatWordPlateList, true);
                }
                else if (operatNode.operatType == WordPlateOperatType.EWPO_WFlutter)
                {
                    meld = playPlayer.Flutter(operatNode.operatWordPlateList, false);
                }
                else if (operatNode.operatType == WordPlateOperatType.EWPO_Slip)
                {
                    meld = playPlayer.Slip(operatNode.operatWordPlateList);
                }
                else if (operatNode.operatType == WordPlateOperatType.EWPO_Pong || operatNode.operatType == WordPlateOperatType.EWPO_Wai)
                {
                    meld = playPlayer.PongWai(operatNode.operatWordPlateList, operatNode.operatType);
                }
                else if (operatNode.operatType == WordPlateOperatType.EWPO_Chow)
                {
                    meld = playPlayer.Chow(operatNode.operatWordPlateList);
                }
                if (meld != null)
                {
                    WordPlateOperatType plateOperatType = operatNode.operatType;
                    if (operatNode.operatType == WordPlateOperatType.EWPO_NFlutter || operatNode.operatType == WordPlateOperatType.EWPO_WFlutter)
                    {
                        plateOperatType = WordPlateOperatType.EWPO_Flutter;
                    }
                    if (plateOperatType != WordPlateOperatType.EWPO_Slip && plateOperatType != WordPlateOperatType.EWPO_Flutter)
                    {
                        //飘和溜后不用出牌
                        playPlayer.housePlayerStatus = WordPlatePlayerStatus.WordPlateWaitCard;
                        //处理死手
                        if (plateOperatType == WordPlateOperatType.EWPO_Pong && meld.m_meldTileList.Count > 0 && (playPlayer.CheckPassPongTile(meld.m_meldTileList[0]) || playPlayer.ChowMeldCheck(meld.m_meldTileList[0])))
                        {
                            //死手
                            playPlayer.m_bDeadHand = true;
                        }
                        if (plateOperatType == WordPlateOperatType.EWPO_Chow)
                        {
                            //处理臭牌
                            wordPlateHouse.DisWordPlatePassOperat(WordPlateOperatType.EWPO_Chow);
                        }
                        //处理胡牌
                        if (wordPlateHouse.bPlayerShowPlate)
                        {
                            playPlayer.m_bGiveUpWin = true;
                        }
                        else
                        {
                            playPlayer.lastOperatTile = new WordPlateTile(wordPlateHouse.currentWordPlate);
                        }
                    }
                    wordPlateHouse.currentShowCard = playPlayer.index;
                    wordPlateHouse.currentWordPlate = null;
                    //保存详细操作
                    OnRequestSaveOperatInfoWordPlate(wordPlateHouse.houseId, wordPlateHouse.currentBureau, wordPlateHouse.GetWordPlateOperat());
                    //清理
                    wordPlateHouse.ClearWordPlateOperat();
                    List<int> meldWordPlateList = main.GetWordPlateNode(meld.m_meldTileList);
                    //表示是桌面上面的牌
                    replyMsg.bOperatMyHand = false;
                    //保存操作成功
                    OnRequestSaveOperatWordPlateSuccess(wordPlateHouse.houseId, wordPlateHouse.currentBureau, wordPlateHouse.currentWhoPlay, playPlayer.index, playPlayer.bOperatMeld, meld.m_eMeldType, meldWordPlateList);
                    //下发消息
                    OnRecvOperatWordPlate(wordPlateHouse, playPlayer, replyMsg, plateOperatType, meld.m_eMeldType, meldWordPlateList);
                }
            }
        }
        public void OnReqOperatWordPlateByMyself(WordPlateHouse wordPlateHouse, WordPlatePlayer player, WordPlateOperatType operatType, List<int> wordPlateList, ReplyOperatWordPlate_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyOperatWordPlate_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //自己的
            if (player.index != wordPlateHouse.currentShowCard || player.housePlayerStatus != WordPlatePlayerStatus.WordPlateWaitCard)
            {
                //玩家不是出牌状态
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlateByMyself, 玩家不是出牌状态! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (!player.CheckPlayerShowWordPlate())
            {
                //玩家手牌不能对自己进行操作
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlateByMyself, 玩家手牌不能对自己进行操作! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            WordPlateMeld meld = null;
            if (operatType == WordPlateOperatType.EWPO_Hu)
            {
                if (player.m_bGiveUpWin)
                {
                    //放弃胡牌状态没恢复
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlateByMyself, 放弃胡牌状态没恢复! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!player.WinHandTileCheck())
                {
                    //胡牌不能胡
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlateByMyself, 胡牌不能胡! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                int winPlateTile = 0;
                if (player.lastOperatTile != null)
                {
                    winPlateTile = player.lastOperatTile.GetWordPlateNode();
                }
                //胡
                OnRecvWinWordPlate(wordPlateHouse, player.index, winPlateTile, winPlateTile > 0);
            }
            else if (operatType == WordPlateOperatType.EWPO_Slip)
            {
                if (!player.CheckPlayerWordPlate(wordPlateList[0]))
                {
                    //溜操作牌不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlateByMyself, 溜操作牌不存在! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (player.PongSlipCheck(wordPlateList[0]) == PlateMeldType.EPM_Slip)
                {
                    //溜
                    List<WordPlateTile> tileList = player.GetTileList(wordPlateList[0]);
                    if (tileList == null || tileList.Count != 4)
                    {
                        //获取溜牌出错
                        ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlateByMyself, 获取溜牌出错! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                        replyMsg.result = ResultCode.Wrong;
                        SendProxyMsg(replyMsg, player.proxyServerId);
                        return;
                    }
                    meld = player.Slip(tileList[0], tileList[1], tileList[2], tileList[3]);
                }
                else
                {
                    meld = player.FlutterSlipCheck(wordPlateList[0]);
                    if (meld == null || meld.m_eMeldType != PlateMeldType.EPM_Wai)
                    {
                        //没有歪牌可以溜
                        ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlateByMyself, 没有歪牌可以溜! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                        replyMsg.result = ResultCode.Wrong;
                        SendProxyMsg(replyMsg, player.proxyServerId);
                        return;
                    }
                    meld.m_eMeldType = PlateMeldType.EPM_Slip;
                    meld.m_meldTileList.Add(new WordPlateTile(wordPlateList[0]));
                    player.DelPlayerWordPlate(wordPlateList[0]);
                    player.bOperatMeld = true;
                }
            }
            else if (operatType == WordPlateOperatType.EWPO_Flutter)
            {
                if (!player.CheckPlayerWordPlate(wordPlateList[0]))
                {
                    //飘操作牌不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlateByMyself, 飘操作牌不存在! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                meld = player.FlutterSlipCheck(wordPlateList[0]);
                if (meld == null || meld.m_eMeldType != PlateMeldType.EPM_Pong)
                {
                    //没有碰牌可以飘
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlateByMyself, 没有碰牌可以飘! userId = " + player.userId + ", houseId = " + wordPlateHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                meld.m_eMeldType = PlateMeldType.EPM_Flutter;
                meld.m_meldTileList.Add(new WordPlateTile(wordPlateList[0]));
                player.DelPlayerWordPlate(wordPlateList[0]);
                player.bOperatMeld = true;
            }
            else if (operatType == WordPlateOperatType.EWPO_None)
            {
                //告诉玩家操作成功
                replyMsg.result = ResultCode.OK;
                replyMsg.operatType = operatType;
                replyMsg.currentShowCard = wordPlateHouse.currentShowCard;
                replyMsg.housePlayerStatus = player.housePlayerStatus;
                replyMsg.bGiveUpWin = player.m_bGiveUpWin;
                replyMsg.bDeadHand = player.m_bDeadHand;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            else
            {
                //自己操作类型错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatWordPlateByMyself, 自己操作类型错误! userId = " + player.userId + ", operatType = " + operatType);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (meld != null)
            {
                //可以胡牌了
                if (player.m_bGiveUpWin)
                {
                    player.m_bGiveUpWin = false;
                }
                //清理上一次歪牌
                if (player.lastOperatTile != null)
                {
                    player.lastOperatTile = null;
                }
                //报听胡
                player.SetBaoTingHu();
                player.housePlayerStatus = WordPlatePlayerStatus.WordPlateShowCard;
                List<int> meldWordPlateList = main.GetWordPlateNode(meld.m_meldTileList);
                //表示是操作自己的手牌
                replyMsg.bOperatMyHand = true;
                //保存操作
                OnRequestSavePlayerKongSelf(wordPlateHouse.houseId, wordPlateHouse.currentBureau, player.index, player.bOperatMeld, meld.m_eMeldType, meldWordPlateList);
                //发消息
                OnRecvOperatWordPlate(wordPlateHouse, player, replyMsg, operatType, meld.m_eMeldType, meldWordPlateList);
            }
        }
        public void OnRecvOperatWordPlate(WordPlateHouse wordPlateHouse, WordPlatePlayer player, ReplyOperatWordPlate_L2P replyMsg, WordPlateOperatType operatType, PlateMeldType meldType, List<int> wordPlateList)
        {
            replyMsg.result = ResultCode.OK;
            replyMsg.summonerId = player.summonerId;
            replyMsg.meldType = meldType;
            replyMsg.operatType = operatType;
            replyMsg.currentShowCard = wordPlateHouse.currentShowCard;
            replyMsg.housePlayerStatus = player.housePlayerStatus;
            replyMsg.bDeadHand = player.m_bDeadHand;
            replyMsg.bGiveUpWin = player.m_bGiveUpWin;
            replyMsg.wordPlateList.AddRange(wordPlateList);
            SendProxyMsg(replyMsg, player.proxyServerId);

            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != player.userId)
                {
                    OnRecvOperatWordPlate(housePlayer.summonerId, housePlayer.proxyServerId, wordPlateHouse.currentShowCard, player.m_bDeadHand, replyMsg.bOperatMyHand, meldType, operatType, wordPlateList);
                }
            });
            //设置房间操作时间
            wordPlateHouse.SetHouseOperateBeginTime();
            //如果是溜 飘
            if (operatType == WordPlateOperatType.EWPO_Flutter || operatType == WordPlateOperatType.EWPO_Slip)
            {
                GiveOffWordPlateToNextPlayer(wordPlateHouse, player.index);
            }
        }
        public void OnRecvOperatWordPlate(ulong summonerId, int proxyServerId, int currentShowCard, bool bDeadHand, bool bOperatMyHand, PlateMeldType meldType, WordPlateOperatType operatType, List<int> wordPlateList)
        {
            RecvOperatWordPlate_L2P recvMsg_L2P = new RecvOperatWordPlate_L2P();
            recvMsg_L2P.summonerId = summonerId;
            recvMsg_L2P.meldType = meldType;
            recvMsg_L2P.operatType = operatType;
            recvMsg_L2P.wordPlateList.AddRange(wordPlateList);
            recvMsg_L2P.currentShowCard = currentShowCard;
            recvMsg_L2P.bDeadHand = bDeadHand;
            recvMsg_L2P.bOperatMyHand = bOperatMyHand;
            SendProxyMsg(recvMsg_L2P, proxyServerId);
        }
        public void OnRecvWinWordPlate(WordPlateHouse wordPlateHouse, int winPlayerIndex = -1, int winPlateTile = 0, bool bOperatHu = false)
        {
            HouseEndPlateInfo houseEndPlateInfo = new HouseEndPlateInfo();
            if (winPlayerIndex == -1)
            {
                wordPlateHouse.bLiuJu = true;
            }
            else
            {
                wordPlateHouse.bLiuJu = false;
                houseEndPlateInfo.endGodTile = wordPlateHouse.GetEndGodWordPlateTile();
            }
            houseEndPlateInfo.bOperatHu = bOperatHu;
            houseEndPlateInfo.winPlateTile = winPlateTile;
            //玩家手牌
            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                if (housePlayer.index == winPlayerIndex)
                {
                    housePlayer.housePlayerStatus = WordPlatePlayerStatus.WordPlateWinCard;
                    if (houseEndPlateInfo.endGodTile == 0 && housePlayer.lastOperatTile != null)
                    {
                        houseEndPlateInfo.endGodTile = housePlayer.lastOperatTile.GetWordPlateNode();
                        wordPlateHouse.endGodTile = housePlayer.lastOperatTile;
                    }
                }
                PlayerTileNode tileNode = new PlayerTileNode();
                tileNode.playerIndex = housePlayer.index;
                housePlayer.GetPlayerHandTileList(tileNode.tileList);
                houseEndPlateInfo.playerTileList.Add(tileNode);
            });
            //剩余牌
            wordPlateHouse.GetRemainWordPlateList(houseEndPlateInfo.remainWordPlateList);
            byte[] housePlateInfo = Serializer.tryCompressObject(houseEndPlateInfo);
            //下发
            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                OnRecvWinWordPlate(housePlayer.summonerId, housePlayer.proxyServerId, winPlayerIndex, housePlateInfo);
            });
            //保存胡牌
            OnRequestSaveWordPlateWinInfo(wordPlateHouse.houseId, wordPlateHouse.currentBureau, winPlayerIndex, houseEndPlateInfo.endGodTile, winPlateTile);
            //结算
            SetWordPlateHouseSettlement(wordPlateHouse);
        }
        public void OnRecvWinWordPlate(ulong summonerId, int proxyServerId, int winPlayerIndex, byte[] housePlateInfo)
        {
            RecvPlayerWinWordPlate_L2P recvMsg_L2P = new RecvPlayerWinWordPlate_L2P();
            recvMsg_L2P.summonerId = summonerId;
            recvMsg_L2P.winPlayerIndex = winPlayerIndex;
            recvMsg_L2P.housePlateInfo = housePlateInfo;
            SendProxyMsg(recvMsg_L2P, proxyServerId);
        }
        public void SendPassChowWordPlate(ulong summonerId, int proxyServerId, WordPlateTile wordPlateTile)
        {
            RecvPlayerPassChowWordPlate_L2P recvMsg_L2P = new RecvPlayerPassChowWordPlate_L2P();
            recvMsg_L2P.summonerId = summonerId;
            recvMsg_L2P.wordPlateNode = wordPlateTile.GetWordPlateNode();
            SendProxyMsg(recvMsg_L2P, proxyServerId);
        }
        public void OnRecvPlayerHouseCard(ulong summonerId, int proxyServerId, int houseCard)
        {
            ModuleManager.Get<MainCityMain>().msg_proxy.OnRecvPlayerHouseCard(summonerId, proxyServerId, houseCard);
        }
        public void SetWordPlateHouseSettlement(WordPlateHouse wordPlateHouse)
        {
            wordPlateHouse.houseStatus = WordPlateHouseStatus.EWPS_Settlement;
            //设置房间操作时间
            wordPlateHouse.SetHouseOperateBeginTime();
            //保存结算状态
            OnRequestSaveWordPlateHouseStatus(wordPlateHouse.houseId, wordPlateHouse.houseStatus);
            //结算
            DisSettlementWordPlateHouse(wordPlateHouse);
        }
        public void DisSettlementWordPlateHouse(WordPlateHouse wordPlateHouse)
        {
            if (wordPlateHouse.CheckSettlementWordPlates())
            {
                main.AddActionHouse(wordPlateHouse.houseId, 5);
            }
        }
        public void SettlementWordPlateHouse(WordPlateHouse wordPlateHouse)
        {
            WordPlateSettlementNode wordPlateSettlement = new WordPlateSettlementNode();
            if (wordPlateHouse.SettlementWordPlates(wordPlateSettlement))
            {
                //小局结算
                if (!wordPlateHouse.bLiuJu)
                {
                    byte[] _wordPlateSettlement = Serializer.tryCompressObject(wordPlateSettlement);
                    wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
                    {
                        RecvSettlementWordPlate_L2P recvMsg_L2P = new RecvSettlementWordPlate_L2P();
                        recvMsg_L2P.summonerId = housePlayer.summonerId;
                        recvMsg_L2P.wordPlateSettlement = _wordPlateSettlement;
                        SendProxyMsg(recvMsg_L2P, housePlayer.proxyServerId);
                        //保存数据库玩家结算信息
                        OnRequestSavePlayerSettlement(wordPlateHouse.houseId, housePlayer);
                    });
                }
                else
                {
                    wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
                    {
                        //保存数据库玩家结算信息
                        OnRequestSavePlayerSettlement(wordPlateHouse.houseId, housePlayer);
                    });
                }
                //清理房间
                wordPlateHouse.InitWordPlateHouse();
                //保存当局积分
                OnRequestSaveBureauIntegral(wordPlateHouse.houseId, wordPlateHouse.GetHouseBureau());
                //第一局打完之后扣房卡()
                if (wordPlateHouse.CheckeFirstBureauEnd())
                {
                    //不是商家模式并且开启扣房卡模式
                    if (wordPlateHouse.businessId == 0 && main.CheckOpenDelHouseCard())
                    {
                        WordPlatePlayer housePlayer = wordPlateHouse.GetHouseOwner();
                        if (housePlayer != null)
                        {
                            int houseCard = main.GetHouseCard(wordPlateHouse.maxBureau);
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
                    wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
                    {
                        ModuleManager.Get<MainCityMain>().RecordBusinessUsers(housePlayer.userId, wordPlateHouse.businessId);
                    });
                }
                //大局结算
                if (wordPlateHouse.CheckeHouseBureau())
                {
                    OnHouseEndSettlement(wordPlateHouse, WordPlateHouseStatus.EWPS_EndBureau);
                }
            }
        }
        public void OnHouseEndSettlement(WordPlateHouse wordPlateHouse, WordPlateHouseStatus houseStatus)
        {
            List<ComPlayerIntegral> playerIntegralList = new List<ComPlayerIntegral>();
            List<WordPlateEndSettlementNode> wordPlateEndSettlementList = wordPlateHouse.GetWordPlateEndSettlementList();
            wordPlateHouse.GetWordPlatePlayer().ForEach(housePlayer =>
            {
                //优惠券
                TicketsNode tickets = null;
                if (houseStatus == WordPlateHouseStatus.EWPS_EndBureau && wordPlateHouse.businessId > 0 && wordPlateHouse.competitionKey == 0)
                {
                    //普通的商家模式才在这里奖励优惠劵
                    int rank = wordPlateHouse.GetCurrentRanking(housePlayer.index, housePlayer.allIntegral);
                    tickets = main.GetTickets(wordPlateHouse.businessId, rank);
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
                    RecvEndSettlementWordPlate_L2P recvMsg = new RecvEndSettlementWordPlate_L2P();
                    recvMsg.summonerId = housePlayer.summonerId;
                    recvMsg.houseStatus = houseStatus;
                    recvMsg.allIntegral = sender.allIntegral;
                    recvMsg.ticketsNode = tickets;
                    recvMsg.wordPlateEndSettlementList.AddRange(wordPlateEndSettlementList);
                    SendProxyMsg(recvMsg, housePlayer.proxyServerId);
                }
                //比赛场积分
                if (wordPlateHouse.competitionKey > 0)
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
                    ModuleManager.Get<ServiceBoxMain>().RecvHouseEndSettlement(housePlayer.summonerId, housePlayer.proxyServerId, wordPlateHouse.houseCardId);
                }
            });
            wordPlateHouse.houseStatus = houseStatus;
            wordPlateHouse.operateBeginTime = DateTime.Parse("1970-01-01 00:00:00");
            //保存房间状态
            OnRequestSaveWordPlateHouseStatus(wordPlateHouse.houseId, wordPlateHouse.houseStatus);
            HouseManager.Instance.RemoveHouse(wordPlateHouse.houseId);
            //发送比赛场积分
            if (houseStatus == WordPlateHouseStatus.EWPS_EndBureau)
            {
                OnReqCompetitionIntegral(wordPlateHouse.competitionKey, playerIntegralList);
            }
        }
        public void OnReqWordPlateOverallRecord(int peerId, bool inbound, object msg)
        {
            RequestWordPlateOverallRecord_P2L reqMsg_P2L = msg as RequestWordPlateOverallRecord_P2L;
            RequestWordPlateOverallRecord_L2D reqMsg_L2D = new RequestWordPlateOverallRecord_L2D();

            reqMsg_L2D.summonerId = reqMsg_P2L.summonerId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnReplyWordPlateOverallRecord(int peerId, bool inbound, object msg)
        {
            ReplyWordPlateOverallRecord_D2L replyMsg_D2L = msg as ReplyWordPlateOverallRecord_D2L;
            ReplyWordPlateOverallRecord_L2P replyMsg_L2P = new ReplyWordPlateOverallRecord_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.summonerId);
            if (sender == null) return;

            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.summonerId = replyMsg_D2L.summonerId;
            replyMsg_L2P.overallRecord = replyMsg_D2L.overallRecord;
            SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
        }
        public void OnReqWordPlateBureauRecord(int peerId, bool inbound, object msg)
        {
            RequestWordPlateBureauRecord_P2L reqMsg_P2L = msg as RequestWordPlateBureauRecord_P2L;
            RequestWordPlateBureauRecord_L2D reqMsg_L2D = new RequestWordPlateBureauRecord_L2D();

            reqMsg_L2D.summonerId = reqMsg_P2L.summonerId;
            reqMsg_L2D.onlyHouseId = reqMsg_P2L.onlyHouseId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnReplyWordPlateBureauRecord(int peerId, bool inbound, object msg)
        {
            ReplyWordPlateBureauRecord_D2L replyMsg_D2L = msg as ReplyWordPlateBureauRecord_D2L;
            ReplyWordPlateBureauRecord_L2P replyMsg_L2P = new ReplyWordPlateBureauRecord_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.summonerId);
            if (sender == null) return;

            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.summonerId = replyMsg_D2L.summonerId;
            replyMsg_L2P.onlyHouseId = replyMsg_D2L.onlyHouseId;
            replyMsg_L2P.bureauRecord = replyMsg_D2L.bureauRecord;
            SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
        }
        public void OnReqWordPlateBureauPlayback(int peerId, bool inbound, object msg)
        {
            RequestWordPlateBureauPlayback_P2L reqMsg_P2L = msg as RequestWordPlateBureauPlayback_P2L;
            RequestWordPlateBureauPlayback_L2D reqMsg_L2D = new RequestWordPlateBureauPlayback_L2D();

            reqMsg_L2D.summonerId = reqMsg_P2L.summonerId;
            reqMsg_L2D.onlyHouseId = reqMsg_P2L.onlyHouseId;
            reqMsg_L2D.bureau = reqMsg_P2L.bureau;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnReplyWordPlateBureauPlayback(int peerId, bool inbound, object msg)
        {
            ReplyWordPlateBureauPlayback_D2L replyMsg_D2L = msg as ReplyWordPlateBureauPlayback_D2L;
            ReplyWordPlateBureauPlayback_L2P replyMsg_L2P = new ReplyWordPlateBureauPlayback_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.summonerId);
            if (sender == null) return;

            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.summonerId = replyMsg_D2L.summonerId;
            replyMsg_L2P.onlyHouseId = replyMsg_D2L.onlyHouseId;
            replyMsg_L2P.bureau = replyMsg_D2L.bureau;
            replyMsg_L2P.playerWordPlate = replyMsg_D2L.playerWordPlate;
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
        public void OnReplyWordPlateHouseInfo(int peerId, bool inbound, object msg)
        {
            ReplyWordPlateHouseInfo_D2L replyMsg = msg as ReplyWordPlateHouseInfo_D2L;
            List<WordPlateHouseNode> houseList = Serializer.tryUncompressObject<List<WordPlateHouseNode>>(replyMsg.house);
            if (houseList != null && houseList.Count > 0)
            {
                ModuleManager.Get<DistributedMain>().SetLoadHouseCount(houseList.Count);
                houseList.ForEach(houseNode =>
                {
                    WordPlateHouse wordPlateHouse = new WordPlateHouse();
                    if (!wordPlateHouse.SetWordPlateStrategy(houseNode.wordPlateType))
                    {
                        wordPlateHouse.houseId = houseNode.houseId;
                        wordPlateHouse.houseCardId = houseNode.houseCardId;
                        wordPlateHouse.logicId = houseNode.logicId;
                        wordPlateHouse.currentBureau = houseNode.currentBureau;
                        wordPlateHouse.maxBureau = houseNode.maxBureau;
                        wordPlateHouse.maxPlayerNum = houseNode.maxPlayerNum;
                        wordPlateHouse.maxWinScore = houseNode.maxWinScore;
                        wordPlateHouse.businessId = houseNode.businessId;
                        wordPlateHouse.baseWinScore = houseNode.baseWinScore;
                        wordPlateHouse.beginGodType = houseNode.beginGodType;
                        wordPlateHouse.housePropertyType = houseNode.housePropertyType;
                        wordPlateHouse.houseType = houseNode.houseType;
                        wordPlateHouse.wordPlateType = houseNode.wordPlateType;
                        wordPlateHouse.houseStatus = houseNode.houseStatus;
                        wordPlateHouse.createTime = Convert.ToDateTime(houseNode.createTime);

                        if (wordPlateHouse.businessId > 0 && wordPlateHouse.houseStatus == WordPlateHouseStatus.EWPS_Settlement)
                        {
                            wordPlateHouse.operateBeginTime = DateTime.Now;
                        }
                        //保存房间
                        HouseManager.Instance.AddHouse(wordPlateHouse.houseId, wordPlateHouse);
                        //请求玩家信息和当局信息
                        OnRequestWordPlatePlayerAndBureau(wordPlateHouse.houseId, replyMsg.dbServerID);
                    }
                    else
                    {
                        //选择字牌逻辑类有误
                        ServerUtil.RecordLog(LogType.Error, "OnReplyWordPlateHouseInfo, 选择字牌逻辑类有误! houseId = " + houseNode.houseId + ", wordPlateType = " + houseNode.wordPlateType);
                        ModuleManager.Get<DistributedMain>().DelLoadHouseCount();
                    }
                });
            }
            else
            {
                ModuleManager.Get<DistributedMain>().SetLoadHouseCount();
            }
        }
        public void OnReplyWordPlatePlayerAndBureau(int peerId, bool inbound, object msg)
        {
            ReplyWordPlatePlayerAndBureau_D2L replyMsg = msg as ReplyWordPlatePlayerAndBureau_D2L;

            ModuleManager.Get<DistributedMain>().DelLoadHouseCount();

            House house = HouseManager.Instance.GetHouseById(replyMsg.houseId);
            if (house == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Error, "OnReplyWordPlatePlayerAndBureau, 房间不存在! houseId = " + replyMsg.houseId); ;
                return;
            }
            WordPlateHouse wordPlateHouse = house as WordPlateHouse;
            if (wordPlateHouse == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Error, "OnReplyWordPlatePlayerAndBureau, 房间不存在! houseId = " + replyMsg.houseId);
                return;
            }
            WordPlatePlayerBureauNode wordPlatePlayerBureau = Serializer.tryUncompressObject<WordPlatePlayerBureauNode>(replyMsg.housePlayerBureau);
            if (wordPlatePlayerBureau != null && wordPlatePlayerBureau.wordPlatePlayerList != null && wordPlatePlayerBureau.wordPlatePlayerList.Count > 0)
            {
                //WordPlatePlayerStatus housePlayerStatus = WordPlatePlayerStatus.WordPlateFree;
                //if (wordPlateHouse.currentBureau == 0 && wordPlateHouse.houseStatus == WordPlateHouseStatus.EWPS_FreeBureau)
                //{
                //    if (wordPlatePlayerBureau.wordPlatePlayerList.Count == wordPlateHouse.maxPlayerNum)
                //    {
                //        //第一局没打完
                //        HouseManager.Instance.RemoveHouse(wordPlateHouse.houseId);
                //        //保存房间状态
                //        OnRequestSaveWordPlateHouseStatus(wordPlateHouse.houseId, WordPlateHouseStatus.EWPS_Dissolved);
                //        return;
                //    }
                //    if (wordPlateHouse.businessId > 0)
                //    {
                //        //商家模式开服自动准备
                //        housePlayerStatus = WordPlatePlayerStatus.WordPlateReady;
                //    }
                //}
                //没开始打牌或者第一局已经结算
                foreach (WordPlateHousePlayerNode playerNode in wordPlatePlayerBureau.wordPlatePlayerList)
                {
                    if (wordPlateHouse.GetWordPlatePlayer().Exists(element => (element.userId == playerNode.userId || element.index == playerNode.playerIndex)))
                    {
                        ServerUtil.RecordLog(LogType.Error, "OnReplyWordPlatePlayerAndBureau, 已经存在玩家信息! userId = " + playerNode.userId);
                        break;
                    }
                    wordPlateHouse.AddPlayer(playerNode);
                    //wordPlateHouse.AddPlayer(playerNode, housePlayerStatus);
                }
            }
            if (wordPlatePlayerBureau != null && wordPlatePlayerBureau.wordPlateBureauList != null && wordPlatePlayerBureau.wordPlateBureauList.Count > 0)
            {
                foreach (WordPlateHouseBureau bureauNode in wordPlatePlayerBureau.wordPlateBureauList)
                {
                    if (wordPlateHouse.houseBureauList.Exists(element => element.bureau == bureauNode.bureau))
                    {
                        ServerUtil.RecordLog(LogType.Error, "OnReplyWordPlatePlayerAndBureau, 已经存在当局信息! bureau = " + bureauNode.bureau);
                        break;
                    }
                    wordPlateHouse.houseBureauList.Add(bureauNode);
                }
            }
        }
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
        public void OnRequestHouseInfo(int dbServerID)
        {
            RequestWordPlateHouseInfo_L2D reqMsg_L2D = new RequestWordPlateHouseInfo_L2D();
            reqMsg_L2D.logicId = root.ServerID;
            SendDBMsg(reqMsg_L2D, dbServerID);
        }
        public void OnRequestWordPlatePlayerAndBureau(ulong houseId, int dbServerID)
        {
            RequestWordPlatePlayerAndBureau_L2D reqMsg_L2D = new RequestWordPlatePlayerAndBureau_L2D();
            reqMsg_L2D.houseId = houseId;
            SendDBMsg(reqMsg_L2D, dbServerID);
        }
        public void OnRequestSaveCreateWordPlateInfo(WordPlateHouse wordPlateHouse, WordPlatePlayer newHousePlayer)
        {
            RequestSaveCreateWordPlateInfo_L2D reqMsg_L2D = new RequestSaveCreateWordPlateInfo_L2D();
            reqMsg_L2D.houseId = wordPlateHouse.houseId;
            reqMsg_L2D.houseCardId = wordPlateHouse.houseCardId;
            reqMsg_L2D.logicId = wordPlateHouse.logicId;
            reqMsg_L2D.maxBureau = wordPlateHouse.maxBureau;
            reqMsg_L2D.maxWinScore = wordPlateHouse.maxWinScore;
            reqMsg_L2D.maxPlayerNum = wordPlateHouse.maxPlayerNum;
            reqMsg_L2D.businessId = wordPlateHouse.businessId;
            reqMsg_L2D.baseWinScore = wordPlateHouse.baseWinScore;
            reqMsg_L2D.housePropertyType = wordPlateHouse.housePropertyType;
            reqMsg_L2D.houseType = wordPlateHouse.houseType;
            reqMsg_L2D.wordPlateType = wordPlateHouse.wordPlateType;
            reqMsg_L2D.createTime = wordPlateHouse.createTime.ToString();
            reqMsg_L2D.summonerId = newHousePlayer.summonerId;
            reqMsg_L2D.index = newHousePlayer.index;
            reqMsg_L2D.allIntegral = newHousePlayer.allIntegral;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveWordPlateNewPlayer(ulong houseId, WordPlatePlayer newHousePlayer)
        {
            RequestSaveWordPlateNewPlayer_L2D reqMsg_L2D = new RequestSaveWordPlateNewPlayer_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.summonerId = newHousePlayer.summonerId;
            reqMsg_L2D.index = newHousePlayer.index;
            reqMsg_L2D.allIntegral = newHousePlayer.allIntegral;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestDelWordPlateHousePlayer(ulong houseId, ulong summonerId)
        {
            RequestDelWordPlateHousePlayer_L2D reqMsg_L2D = new RequestDelWordPlateHousePlayer_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.summonerId = summonerId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveWordPlateHouseStatus(ulong houseId, WordPlateHouseStatus houseStatus)
        {
            RequestSaveWordPlateHouseStatus_L2D reqMsg_L2D = new RequestSaveWordPlateHouseStatus_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.houseStatus = houseStatus;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveTickets(ulong summonerId, TicketsNode ticketsNode)
        {
            ModuleManager.Get<SpecialActivitiesMain>().msg_proxy.OnRequestSaveTickets(summonerId, ticketsNode);
        }
        public void OnRequestSaveHouseBureauInfo(ulong houseId, WordPlateHouseBureau houseBureau, WordPlateTile godTile, List<PlayerTileNode> playerInitTileList)
        {
            if (houseBureau == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseBureauInfo, houseBureau == null ");
                return;
            }
            RequestSaveWordPlateBureauInfo_L2D reqMsg_L2D = new RequestSaveWordPlateBureauInfo_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.currentBureau = houseBureau.bureau;
            reqMsg_L2D.bureauTime = houseBureau.bureauTime;
            houseBureau.playerBureauList.ForEach(playerBureau =>
            {
                reqMsg_L2D.playerIndexList.Add(playerBureau.playerIndex);
            });
            reqMsg_L2D.playerInitTile = Serializer.tryCompressObject(playerInitTileList);
            if (godTile != null)
            {
                reqMsg_L2D.beginGodTile = godTile.GetWordPlateNode();
            }

            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveShowWordPlate(ulong houseId, int currentBureau, int playerIndex, int wordPlateNode)
        {
            WordPlateNodeRecordNode wordPlateNodeRecord = new WordPlateNodeRecordNode { playerIndex = playerIndex, wordPlateNode = wordPlateNode };
            WordPlateRecordNode wordPlateRecordNode = new WordPlateRecordNode { recordType = WordPlateRecordType.EWPR_Show, recordData = Serializer.trySerializerObject(wordPlateNodeRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, wordPlateRecordNode);
        }
        public void OnRequestSaveGiveOffWordPlate(ulong houseId, int currentBureau, int playerIndex, WordPlateTile newWordPlateTile, int lastPlayerIndex, int lastWordPlateNode)
        {
            WordPlateGiveOffRecordNode wordPlateNodeRecord = new WordPlateGiveOffRecordNode();
            wordPlateNodeRecord.playerIndex = playerIndex;
            wordPlateNodeRecord.wordPlateNode = newWordPlateTile.GetWordPlateNode();
            wordPlateNodeRecord.lastPlayerIndex = lastPlayerIndex;
            wordPlateNodeRecord.lastWordPlateNode = lastWordPlateNode;
            WordPlateRecordNode wordPlateRecordNode = new WordPlateRecordNode { recordType = WordPlateRecordType.EWPR_GiveOff, recordData = Serializer.trySerializerObject(wordPlateNodeRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, wordPlateRecordNode);
        }
        public void OnRequestSaveOperatInfoWordPlate(ulong houseId, int currentBureau, List<WordPlateOperatNode> wordPlateOperatList)
        {
            List<WordPlateOperatInfoRecordNode> wordPlateOperatInfoRecordList = new List<WordPlateOperatInfoRecordNode>();
            wordPlateOperatList.ForEach(wordPlateOperat =>
            {
                WordPlateOperatInfoRecordNode node = new WordPlateOperatInfoRecordNode();
                node.playerIndex = wordPlateOperat.playerIndex;
                node.bOperat = !wordPlateOperat.bWait;
                node.operatType = wordPlateOperat.operatType;
                node.operatTypeList.AddRange(wordPlateOperat.operatTypeList);
                wordPlateOperatInfoRecordList.Add(node);
            });
            WordPlateRecordNode wordPlateRecordNode = new WordPlateRecordNode { recordType = WordPlateRecordType.EWPR_OperatInfo, recordData = Serializer.trySerializerObject(wordPlateOperatInfoRecordList) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, wordPlateRecordNode);
        }
        public void OnRequestSaveOperatWordPlateSuccess(ulong houseId, int currentBureau, int currentWhoPlay, int playerIndex, bool bOperatMeld, PlateMeldType meldType, List<int> meldWordPlateList)
        {
            WordPlateRecordType recordType = WordPlateRecordType.EWPR_None;
            if (meldType == PlateMeldType.EPM_Sequence)
            {
                recordType = WordPlateRecordType.EWPR_Chow;
            }
            else if (meldType == PlateMeldType.EPM_Pong)
            {
                recordType = WordPlateRecordType.EWPR_Pong;
            }
            else if (meldType == PlateMeldType.EPM_Wai)
            {
                recordType = WordPlateRecordType.EWPR_Wai;
            }
            else if (meldType == PlateMeldType.EPM_Flutter)
            {
                recordType = WordPlateRecordType.EWPR_Flutter;
            }
            else if (meldType == PlateMeldType.EPM_Slip)
            {
                recordType = WordPlateRecordType.EWPR_Slip;
            }
            if (recordType != WordPlateRecordType.EWPR_None)
            {
                WordPlateOperatRecordNode wordPlateOperatRecord = new WordPlateOperatRecordNode();
                wordPlateOperatRecord.playerIndex = playerIndex;
                wordPlateOperatRecord.meldType = meldType;
                wordPlateOperatRecord.meldWordPlateList.AddRange(meldWordPlateList);
                wordPlateOperatRecord.lastPlayerIndex = currentWhoPlay;
                wordPlateOperatRecord.bOperatHand = false;
                wordPlateOperatRecord.bOperatMeld = bOperatMeld;
                WordPlateRecordNode wordPlateRecordNode = new WordPlateRecordNode { recordType = recordType, recordData = Serializer.trySerializerObject(wordPlateOperatRecord) };
                //保存回放节点
                OnRequestSaveRecord(houseId, currentBureau, wordPlateRecordNode);
            }
        }
        public void OnRequestSavePlayerKongSelf(ulong houseId, int currentBureau, int playerIndex, bool bOperatMeld, PlateMeldType meldType, List<int> meldWordPlateList)
        {
            WordPlateRecordType recordType = WordPlateRecordType.EWPR_None;
            if (meldType == PlateMeldType.EPM_Flutter)
            {
                recordType = WordPlateRecordType.EWPR_Flutter;
            }
            else if (meldType == PlateMeldType.EPM_Slip)
            {
                recordType = WordPlateRecordType.EWPR_Slip;
            }
            if (recordType != WordPlateRecordType.EWPR_None)
            {
                WordPlateOperatRecordNode wordPlateOperatRecord = new WordPlateOperatRecordNode();
                wordPlateOperatRecord.playerIndex = playerIndex;
                wordPlateOperatRecord.meldType = meldType;
                wordPlateOperatRecord.meldWordPlateList.AddRange(meldWordPlateList);
                wordPlateOperatRecord.lastPlayerIndex = playerIndex;
                wordPlateOperatRecord.bOperatHand = true;
                wordPlateOperatRecord.bOperatMeld = bOperatMeld;
                WordPlateRecordNode wordPlateRecordNode = new WordPlateRecordNode { recordType = recordType, recordData = Serializer.trySerializerObject(wordPlateOperatRecord) };
                //保存回放节点
                OnRequestSaveRecord(houseId, currentBureau, wordPlateRecordNode);
            }
        }
        public void OnRequestSaveWordPlateWinInfo(ulong houseId, int currentBureau, int winPlayerIndex, int endGodTile, int winPlateTile)
        {
            WordPlateWinRecordNode wordPlateWinRecord = new WordPlateWinRecordNode();
            wordPlateWinRecord.playerIndex = winPlayerIndex;
            wordPlateWinRecord.endGodTile = endGodTile;
            wordPlateWinRecord.winWordPlate = winPlateTile;
            WordPlateRecordNode wordPlateRecordNode = new WordPlateRecordNode { recordType = WordPlateRecordType.EWPR_Hu, recordData = Serializer.trySerializerObject(wordPlateWinRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, wordPlateRecordNode);
        }
        public void OnRequestSaveRecord(ulong houseId, int currentBureau, WordPlateRecordNode wordPlateRecordNode)
        {
            RequestSaveWordPlateRecord_L2D reqMsg_L2D = new RequestSaveWordPlateRecord_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.currentBureau = (ulong)currentBureau;
            reqMsg_L2D.wordPlateRecordNode = wordPlateRecordNode;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSavePlayerSettlement(ulong houseId, WordPlatePlayer housePlayer)
        {
            RequestSaveWordPlatePlayerSettlement_L2D reqMsg_L2D = new RequestSaveWordPlatePlayerSettlement_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.summonerId = housePlayer.summonerId;
            reqMsg_L2D.winAmount = housePlayer.m_nWinAmount;
            reqMsg_L2D.allWinScore = housePlayer.m_nAllWinScore;
            reqMsg_L2D.allIntegral = housePlayer.allIntegral;
            reqMsg_L2D.zhuangLeisureType = housePlayer.zhuangLeisureType;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveBureauIntegral(ulong houseId, WordPlateHouseBureau houseBureau)
        {
            if (houseBureau == null)
            {
                ServerUtil.RecordLog(LogType.Debug, "OnRequestSaveBureauIntegral, houseBureau == null ");
                return;
            }
            RequestSaveWordPlateBureauIntegral_L2D reqMsg_L2D = new RequestSaveWordPlateBureauIntegral_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.bureau = houseBureau.bureau;
            reqMsg_L2D.playerBureauList.AddRange(houseBureau.playerBureauList);
            SendDBMsg(reqMsg_L2D);
        }
    }
}
#endif
