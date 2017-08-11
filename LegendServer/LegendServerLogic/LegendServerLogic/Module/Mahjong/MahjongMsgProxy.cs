#if MAHJONG
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
using System.Linq;

namespace LegendServerLogic.Mahjong
{
    public class MahjongMsgProxy : ServerMsgProxy
    {
        private MahjongMain main;

        public MahjongMsgProxy(MahjongMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnReqCreateMahjongHouse(int peerId, bool inbound, object msg)
        {
            RequestCreateMahjongHouse_P2L reqMsg = msg as RequestCreateMahjongHouse_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            OnReqCreateMahjongHouse(sender, reqMsg.maxPlayerNum, reqMsg.maxBureau, reqMsg.mahjongType, reqMsg.housePropertyType, reqMsg.catchBird, reqMsg.flutter);
        }
        public void OnReqCreateMahjongHouse(Summoner sender, int maxPlayerNum, int maxBureau, MahjongType mahjongType, int housePropertyType, int catchBird, int flutter, int businessId = 0)
        {
            ReplyCreateMahjongHouse_L2P replyMsg = new ReplyCreateMahjongHouse_L2P();
            replyMsg.summonerId = sender.id;

            if (sender.houseId > 0)
            {
                //已经有房间号了
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateMahjongHouse, 已经有房间号了! userId = " + sender.userId + ", houseId = " + sender.houseId);
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
                    ServerUtil.RecordLog(LogType.Debug, "OnReqCreateMahjongHouse, 报名参加了比赛场! userId = " + sender.userId + ", competitionKey = " + sender.competitionKey);
                    replyMsg.result = ResultCode.PlayerJoinCompetition;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
            }
            if (maxPlayerNum != MahjongConstValue.MahjongThreePlayer && maxPlayerNum != MahjongConstValue.MahjongFourPlayer)
            {
                //选择最多参战的玩家数有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateMahjongHouse, 选择最多参战的玩家数有误! userId = " + sender.userId + ", maxPlayerNum = " + maxPlayerNum);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (!main.CheckOpenCreateHouse())
            {
                //已经关闭创建房间接口
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateMahjongHouse, 已经关闭创建房间接口! userId = " + sender.userId + ", maxPlayerNum = " + maxPlayerNum);
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
            ModuleManager.Get<UIDAllocMain>().msg_proxy.RequestUID(UIDType.RoomID, sender.id, new PreMahjongRoomInfo(maxPlayerNum, maxBureau, mahjongType, housePropertyType, catchBird, flutter, businessId));
        }
        public bool OnCreateMahjongHouse(ulong summonerId, int houseCardId, PreMahjongRoomInfo mahjongRoomInfo)
        {
            Summoner sender = SummonerManager.Instance.GetSummonerById(summonerId);
            if (sender == null) return false;

            ReplyCreateMahjongHouse_L2P replyMsg = new ReplyCreateMahjongHouse_L2P();
            replyMsg.summonerId = sender.id;

            if (houseCardId < 100000 || houseCardId >= 1000000)
            {
                //获取房间Id出错
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateMahjongHouse, 获取房间Id出错! userId = " + sender.userId + ", houseCardId = " + houseCardId);
                replyMsg.result = ResultCode.GetHouseIdError;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return false;
            }
            MahjongHouse mahjongHouse = new MahjongHouse();
            if (mahjongHouse.SetMahjongSetTile(mahjongRoomInfo.mahjongType))
            {
                //选择麻将发牌类有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateMahjongHouse, 选择麻将发牌类有误! userId = " + sender.userId + ", mahjongType = " + mahjongRoomInfo.mahjongType);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return false;
            }
            mahjongHouse.houseId = MyGuid.NewGuid(ServiceType.Logic, (uint)root.ServerID);
            mahjongHouse.houseCardId = houseCardId;
            mahjongHouse.logicId = root.ServerID;
            mahjongHouse.maxBureau = mahjongRoomInfo.maxBureau;
            mahjongHouse.mahjongType = mahjongRoomInfo.mahjongType;
            mahjongHouse.maxPlayerNum = mahjongRoomInfo.maxPlayerNum;
            mahjongHouse.catchBird = mahjongRoomInfo.catchBird;
            mahjongHouse.flutter = mahjongRoomInfo.flutter;
            mahjongHouse.housePropertyType = mahjongRoomInfo.housePropertyType;
            mahjongHouse.businessId = mahjongRoomInfo.businessId;
            mahjongHouse.createTime = DateTime.Now;
            mahjongHouse.houseStatus = MahjongHouseStatus.MHS_FreeBureau;

            MahjongPlayer newHousePlayer = mahjongHouse.CreatHouse(sender);
            if (newHousePlayer == null)
            {
                //选择麻将逻辑类有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqCreateMahjongHouse, 选择麻将逻辑类有误! userId = " + sender.userId + ", mahjongType = " + mahjongRoomInfo.mahjongType);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return false;
            }

            HouseManager.Instance.AddHouse(mahjongHouse.houseId, mahjongHouse);

            sender.houseId = mahjongHouse.houseId;

            replyMsg.result = ResultCode.OK;
            replyMsg.maxBureau = mahjongRoomInfo.maxBureau;
            replyMsg.mahjongType = mahjongRoomInfo.mahjongType;
            replyMsg.maxPlayerNum = mahjongRoomInfo.maxPlayerNum;
            replyMsg.catchBird = mahjongRoomInfo.catchBird;
            replyMsg.flutter = mahjongRoomInfo.flutter;
            replyMsg.housePropertyType = mahjongRoomInfo.housePropertyType;
            replyMsg.allIntegral = newHousePlayer.allIntegral;
            replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
            replyMsg.houseId = houseCardId;
            replyMsg.businessId = mahjongRoomInfo.businessId;
            replyMsg.onlyHouseId = mahjongHouse.houseId;
            SendProxyMsg(replyMsg, sender.proxyServerId);

            //存数据库
            OnRequestSaveCreateMahjongInfo(mahjongHouse, newHousePlayer);
            return true;
        }
        //比赛场专用接口
        public void OnReqCreateMahjongHouse(PlayerInfo playerInfo, MahjongHouse mahjongHouse)
        {
            MahjongPlayer newHousePlayer = mahjongHouse.CreatHouse(playerInfo);
            if (newHousePlayer == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnReqCreateMahjongHouse, 比赛场专用接口 选择麻将逻辑类有误! userId = " + playerInfo.userId + ", mahjongType = " + mahjongHouse.mahjongType);
                return;
            }

            HouseManager.Instance.AddHouse(mahjongHouse.houseId, mahjongHouse);

            Summoner sender = SummonerManager.Instance.GetSummonerById(playerInfo.summonerId);
            if (sender != null)
            {
                //在线
                sender.houseId = mahjongHouse.houseId;
                newHousePlayer.proxyServerId = sender.proxyServerId;

                ReplyCreateMahjongHouse_L2P replyMsg = new ReplyCreateMahjongHouse_L2P();
                replyMsg.result = ResultCode.OK;
                replyMsg.summonerId = sender.id;
                replyMsg.maxBureau = mahjongHouse.maxBureau;
                replyMsg.mahjongType = mahjongHouse.mahjongType;
                replyMsg.maxPlayerNum = mahjongHouse.maxPlayerNum;
                replyMsg.catchBird = mahjongHouse.catchBird;
                replyMsg.flutter = mahjongHouse.flutter;
                replyMsg.housePropertyType = mahjongHouse.housePropertyType;
                replyMsg.allIntegral = newHousePlayer.allIntegral;
                replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
                replyMsg.houseId = mahjongHouse.houseCardId;
                replyMsg.onlyHouseId = mahjongHouse.houseId;
                replyMsg.businessId = mahjongHouse.businessId;
                replyMsg.competitionKey = mahjongHouse.competitionKey;
                SendProxyMsg(replyMsg, sender.proxyServerId);
            }
            else
            {
                newHousePlayer.lineType = LineType.OffLine;
            }

            //存数据库
            OnRequestSaveCreateMahjongInfo(mahjongHouse, newHousePlayer);
        }
        public void OnReqJoinMahjongHouse(int peerId, bool inbound, object msg)
        {
            RequestJoinMahjongHouse_P2L reqMsg = msg as RequestJoinMahjongHouse_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            OnReqJoinMahjongHouse(sender, reqMsg.houseId);
        }
        public void OnReqJoinMahjongHouse(Summoner sender, int houseCardId)
        {
            ReplyJoinMahjongHouse_L2P replyMsg = new ReplyJoinMahjongHouse_L2P();
            replyMsg.summonerId = sender.id;

            if (sender.houseId > 0)
            {
                //已经有房间号了
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinMahjongHouse, 已经有房间号了! userId = " + sender.userId + ", houseId = " + sender.houseId);
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
                    ServerUtil.RecordLog(LogType.Debug, "OnReqJoinMahjongHouse, 报名参加了比赛场! userId = " + sender.userId + ", competitionKey = " + sender.competitionKey);
                    replyMsg.result = ResultCode.PlayerJoinCompetition;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
            }
            House house = HouseManager.Instance.GetHouseById(houseCardId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //本逻辑服务器找不到房间则从世界服务器找
                RequestHouseBelong_L2W reqMsg = new RequestHouseBelong_L2W();
                reqMsg.summonerId = sender.id;
                reqMsg.houseId = houseCardId;
                SendWorldMsg(reqMsg);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            JoinMahjongHouse(sender, mahjongHouse, replyMsg);
        }
        public void JoinMahjongHouse(Summoner sender, MahjongHouse mahjongHouse, ReplyJoinMahjongHouse_L2P replyMsg = null)
        {
            if(replyMsg == null)
            {
                replyMsg = new ReplyJoinMahjongHouse_L2P();
                replyMsg.summonerId = sender.id;
            }
            if (mahjongHouse == null || mahjongHouse.houseStatus >= MahjongHouseStatus.MHS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinMahjongHouse, 房间不存在! userId = " + sender.userId + ", houseCardId = " + mahjongHouse.houseCardId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (mahjongHouse.CheckPlayer(sender.userId))
            {
                //已经在房间里面了
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinMahjongHouse, 已经在房间里面了! userId = " + sender.userId + ", houseCardId = " + mahjongHouse.houseCardId);
                replyMsg.result = ResultCode.PlayerHasBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (mahjongHouse.CheckPlayerFull())
            {
                //房间已满
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinMahjongHouse, 房间已满! userId = " + sender.userId + ", houseCardId = " + mahjongHouse.houseCardId);
                replyMsg.result = ResultCode.TheHouseIsFull;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer newHousePlayer = mahjongHouse.AddPlayer(sender);
            if (newHousePlayer == null)
            {
                //选择麻将逻辑类有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqJoinMahjongHouse, 选择麻将逻辑类有误! userId = " + sender.userId + ", mahjongType = " + mahjongHouse.mahjongType);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayerShowNode newPlayerShow = main.GetPlayerShowNode(newHousePlayer);

            sender.houseId = mahjongHouse.houseId;

            List<MahjongPlayerShowNode> playerShowList = new List<MahjongPlayerShowNode>();
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != sender.userId)
                {
                    playerShowList.Add(main.GetPlayerShowNode(housePlayer));
                    OnRecvJoinMahjongHouse(housePlayer.summonerId, housePlayer.proxyServerId, newPlayerShow);
                }
            });

            replyMsg.result = ResultCode.OK;
            replyMsg.playerShow = Serializer.tryCompressObject(playerShowList);
            replyMsg.myIndex = newHousePlayer.index;
            replyMsg.allIntegral = newHousePlayer.allIntegral;
            replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
            replyMsg.maxBureau = mahjongHouse.maxBureau;
            replyMsg.mahjongType = mahjongHouse.mahjongType;
            replyMsg.maxPlayerNum = mahjongHouse.maxPlayerNum;
            replyMsg.housePropertyType = mahjongHouse.housePropertyType;
            replyMsg.catchBird = mahjongHouse.catchBird;
            replyMsg.flutter = mahjongHouse.flutter;
            replyMsg.businessId = mahjongHouse.businessId;
            replyMsg.houseId = mahjongHouse.houseCardId;
            replyMsg.onlyHouseId = mahjongHouse.houseId;
            SendProxyMsg(replyMsg, sender.proxyServerId);

            //保存玩家数据
            OnRequestSaveMahjongNewPlayer(mahjongHouse.houseId, newHousePlayer);
            //开局
            BeginMahjongs(mahjongHouse);
        }
        //比赛场专用接口
        public void OnReqJoinMahjongHouse(PlayerInfo playerInfo, MahjongHouse mahjongHouse)
        {
            MahjongPlayer newHousePlayer = mahjongHouse.AddPlayer(playerInfo);
            if (newHousePlayer == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnReqJoinMahjongHouse, 比赛场专用接口 选择麻将逻辑类有误! userId = " + playerInfo.userId + ", mahjongType = " + mahjongHouse.mahjongType);
                return;
            }

            MahjongPlayerShowNode newPlayer = main.GetPlayerShowNode(newHousePlayer);
            List<MahjongPlayerShowNode> playerShowList = new List<MahjongPlayerShowNode>();
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != playerInfo.userId)
                {
                    playerShowList.Add(main.GetPlayerShowNode(housePlayer));
                    if (housePlayer.lineType == LineType.OnLine)
                    {
                        OnRecvJoinMahjongHouse(housePlayer.summonerId, housePlayer.proxyServerId, newPlayer);
                    }
                }
            });

            Summoner sender = SummonerManager.Instance.GetSummonerById(playerInfo.summonerId);
            if (sender != null)
            {
                //在线
                sender.houseId = mahjongHouse.houseId;
                newHousePlayer.proxyServerId = sender.proxyServerId;

                ReplyJoinMahjongHouse_L2P replyMsg = new ReplyJoinMahjongHouse_L2P();
                replyMsg.result = ResultCode.OK;
                replyMsg.summonerId = sender.id;
                replyMsg.playerShow = Serializer.tryCompressObject(playerShowList);
                replyMsg.myIndex = newHousePlayer.index;
                replyMsg.allIntegral = newHousePlayer.allIntegral;
                replyMsg.housePlayerStatus = newHousePlayer.housePlayerStatus;
                replyMsg.maxBureau = mahjongHouse.maxBureau;
                replyMsg.mahjongType = mahjongHouse.mahjongType;
                replyMsg.maxPlayerNum = mahjongHouse.maxPlayerNum;
                replyMsg.catchBird = mahjongHouse.catchBird;
                replyMsg.flutter = mahjongHouse.flutter;
                replyMsg.housePropertyType = mahjongHouse.housePropertyType;
                replyMsg.houseId = mahjongHouse.houseCardId;
                replyMsg.onlyHouseId = mahjongHouse.houseId;
                replyMsg.businessId = mahjongHouse.businessId;
                replyMsg.competitionKey = mahjongHouse.competitionKey;
                SendProxyMsg(replyMsg, sender.proxyServerId);
            }
            else
            {
                newHousePlayer.lineType = LineType.OffLine;
            }

            //保存玩家数据
            OnRequestSaveMahjongNewPlayer(mahjongHouse.houseId, newHousePlayer);
            //开局
            BeginMahjongs(mahjongHouse);
        }
        public void OnRecvJoinMahjongHouse(ulong summonerId, int proxyServerId, MahjongPlayerShowNode newPlayerShow)
        {
            RecvJoinMahjongHouse_L2P recvMsg = new RecvJoinMahjongHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.playerShow = newPlayerShow;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void BeginMahjongs(MahjongHouse mahjongHouse)
        {
            if (mahjongHouse.CheckBeginMahjongs())
            {
                MahjongHouseBureau houseBureau = new MahjongHouseBureau();
                List<MahjongCountNode> mahjongCountList = new List<MahjongCountNode>();
                List<PlayerTileNode> playerInitTileList = new List<PlayerTileNode>();
                List<int> csPendulumPlayerList = new List<int>();
                int zhuangIndex = 0;
                //处理一些开局信息
                mahjongHouse.BeginMahjongs(houseBureau, mahjongCountList, csPendulumPlayerList);
                //不用摆牌，直接开始
                if (mahjongHouse.houseStatus == MahjongHouseStatus.MHS_BeginBureau)
                {
                    MahjongPlayer zhuangPlayer = mahjongHouse.GetHouseZhuang();
                    if (zhuangPlayer != null && zhuangPlayer.zhuangLeisureType == ZhuangLeisureType.Zhuang)
                    {
                        mahjongHouse.currentShowCard = zhuangPlayer.index;
                        zhuangPlayer.housePlayerStatus = MahjongPlayerStatus.MahjongWaitCard;
                        zhuangIndex = zhuangPlayer.index;
                    }
                }
                else
                {
                    zhuangIndex = mahjongHouse.GetHouseZhuangIndex();
                    if (csPendulumPlayerList.Count == 1)
                    {
                        mahjongHouse.currentShowCard = csPendulumPlayerList[0];
                    }
                    else if (csPendulumPlayerList.Count > 1)
                    {
                        int pendulumPlayer = zhuangIndex;
                        for (int i = 0; i < mahjongHouse.GetHousePlayerCount(); ++i)
                        {
                            if (csPendulumPlayerList.Exists(element => element == pendulumPlayer))
                            {
                                //从庄开始逆时针找优先摆牌的人
                                mahjongHouse.currentShowCard = pendulumPlayer;
                                break;
                            }
                            pendulumPlayer = mahjongHouse.GetNextHousePlayerIndex(pendulumPlayer);
                        }
                    }
                }
                //发送开始信息给玩家
                mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                {
                    RecvBeginMahjong_L2P recvMsg = new RecvBeginMahjong_L2P();
                    recvMsg.summonerId = housePlayer.summonerId;
                    recvMsg.zhuangIndex = zhuangIndex;
                    recvMsg.currentBureau = mahjongHouse.currentBureau;
                    recvMsg.currentShowCard = mahjongHouse.currentShowCard;
                    recvMsg.houseStatus = mahjongHouse.houseStatus;
                    recvMsg.housePlayerStatus = housePlayer.housePlayerStatus;
                    recvMsg.startDisplayType = (int)housePlayer.startDisplayType;
                    recvMsg.remainMahjongCount = mahjongHouse.GetRemainMahjongCount();
                    housePlayer.GetPlayerHandTileList(recvMsg.mahjongList);
                    recvMsg.mahjongCountList.AddRange(mahjongCountList);
                    SendProxyMsg(recvMsg, housePlayer.proxyServerId);
                    //玩家初始化手牌
                    PlayerTileNode playerTileNode = new PlayerTileNode();
                    playerTileNode.playerIndex = housePlayer.index;
                    playerTileNode.tileList.AddRange(recvMsg.mahjongList);
                    playerInitTileList.Add(playerTileNode);
                });
                //开局保存每局信息
                OnRequestSaveHouseBureauInfo(mahjongHouse.houseId, houseBureau, playerInitTileList);
                if (mahjongHouse.SetHouseOperateBeginTime() && mahjongHouse.currentBureau == 1)
                {
                    main.CheckHouseOperateTimer();
                }
            }
        }
        public void OnReqQuitMahjongHouse(int peerId, bool inbound, object msg)
        {
            RequestQuitMahjongHouse_P2L reqMsg = msg as RequestQuitMahjongHouse_P2L;
            ReplyQuitMahjongHouse_L2P replyMsg = new ReplyQuitMahjongHouse_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号，不需要退出
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitMahjongHouse, 没有房间号，不需要退出! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitMahjongHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || mahjongHouse.houseStatus >= MahjongHouseStatus.MHS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitMahjongHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (mahjongHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间正在投票中
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitMahjongHouse, 房间正在投票中! userId = " + sender.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.HouseGoingDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (mahjongHouse.competitionKey > 0)
            {
                //比赛场房间不能投票
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitMahjongHouse, 比赛场房间不能投票! userId = " + sender.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.CompetitionNoDissolve;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqQuitMahjongHouse, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            replyMsg.dissolveVoteTime = main.houseDissolveVoteTime;
            if (mahjongHouse.businessId > 0)
            {
                replyMsg.dissolveVoteTime = main.businessDissolveVoteTime;
            }
            bool bVote = false;
            if (mahjongHouse.CheckPlayerFull() && mahjongHouse.houseStatus != MahjongHouseStatus.MHS_FreeBureau)
            {
                //要投票
                bVote = true;
                mahjongHouse.voteBeginTime = DateTime.Now;
                player.voteStatus = VoteStatus.LaunchVote;
                mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                {
                    if (sender.userId != housePlayer.userId)
                    {
                        OnRecvQuitMahjongHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index, bVote, replyMsg.dissolveVoteTime);
                    }
                });
                main.AddDissolveVoteHouse(mahjongHouse.houseId);
            }
            else
            {
                //房主
                if (player.index == 0 && mahjongHouse.businessId == 0)
                {
                    mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                    {
                        if (housePlayer.userId != sender.userId)
                        {
                            Summoner houseSender = SummonerManager.Instance.GetSummonerByUserId(housePlayer.userId);
                            if (houseSender != null)
                            {
                                houseSender.houseId = 0;
                                OnRecvQuitMahjongHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index);
                            }
                            //保存DB
                            OnRequestSaveHouseId(housePlayer.userId, 0);
                        }
                    });
                    //清除
                    InitSummonerHouseId(sender);
                    mahjongHouse.houseStatus = MahjongHouseStatus.MHS_Dissolved;
                    //保存房间状态
                    OnRequestSaveMahjongHouseStatus(mahjongHouse.houseId, mahjongHouse.houseStatus);
                    //删除房间
                    HouseManager.Instance.RemoveHouse(mahjongHouse.houseId);
                }
                else
                {
                    mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                    {
                        if (housePlayer.userId != sender.userId)
                        {
                            OnRecvLeaveMahjongHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index);
                        }
                    });

                    sender.houseId = 0;
                    //删除玩家
                    mahjongHouse.RemovePlayer(sender.userId);
                    //保存数据库删除玩家
                    OnRequestDelMahjongHousePlayer(mahjongHouse.houseId, sender.id);
                }
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.bVote = bVote;
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void OnRecvQuitMahjongHouse(ulong summonerId, int proxyServerId, int index, bool bVote = false, int dissolveVoteTime = 0)
        {
            RecvQuitMahjongHouse_L2P recvMsg = new RecvQuitMahjongHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.bVote = bVote;
            recvMsg.index = index;
            recvMsg.dissolveVoteTime = dissolveVoteTime;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnRecvLeaveMahjongHouse(ulong summonerId, int proxyServerId, int index)
        {
            RecvLeaveMahjongHouse_L2P recvMsg = new RecvLeaveMahjongHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.leaveIndex = index;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnReqDissolveHouseVote(int peerId, bool inbound, object msg)
        {
            RequestMahjongHouseVote_P2L reqMsg = msg as RequestMahjongHouseVote_P2L;
            ReplyMahjongHouseVote_L2P replyMsg = new ReplyMahjongHouseVote_L2P();

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
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || mahjongHouse.houseStatus >= MahjongHouseStatus.MHS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (mahjongHouse.voteBeginTime == DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间没有发起投票
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 房间没有发起投票! userId = " + sender.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.HouseNoNeedDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqDissolveHouseVote(mahjongHouse, player, reqMsg.voteStatus, replyMsg);
        }
        public void OnReqDissolveHouseVote(MahjongHouse mahjongHouse, MahjongPlayer player, VoteStatus voteStatus = VoteStatus.AgreeVote, ReplyMahjongHouseVote_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyMahjongHouseVote_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //发起者
            MahjongPlayer launchPlayer = mahjongHouse.GetMahjongVoteLaunchPlayer();
            if (launchPlayer == null)
            {
                //发起者不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 发起者不存在! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (launchPlayer.index == player.index)
            {
                //发起者不能投票
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 发起者不能投票! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (player.voteStatus != VoteStatus.FreeVote)
            {
                //玩家已经投过票了
                ServerUtil.RecordLog(LogType.Debug, "OnReqDissolveHouseVote, 玩家已经投过票了! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            player.voteStatus = voteStatus;

            //判断是否能够解散(free 表示继续等投票 agree 表示要解散 oppose 表示解散失败)
            VoteStatus houseVoteStatus = mahjongHouse.GetDissolveHouseVote();
            //告诉其他人
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (player.userId != housePlayer.userId)
                {
                    OnRecvMahjongHouseVote(housePlayer.summonerId, housePlayer.proxyServerId, player.index, voteStatus, houseVoteStatus);
                }
            });

            replyMsg.result = ResultCode.OK;
            replyMsg.voteStatus = voteStatus;
            replyMsg.houseVoteStatus = houseVoteStatus;
            SendProxyMsg(replyMsg, player.proxyServerId);
            
            //处理投票
            DisposeDissolveHouseVote(mahjongHouse, houseVoteStatus);
        }
        public void OnRecvMahjongHouseVote(ulong summonerId, int proxyServerId, int index, VoteStatus voteStatus, VoteStatus houseVoteStatus)
        {
            RecvMahjongHouseVote_L2P recvMsg = new RecvMahjongHouseVote_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.index = index;
            recvMsg.voteStatus = voteStatus;
            recvMsg.houseVoteStatus = houseVoteStatus;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void DisposeDissolveHouseVote(MahjongHouse mahjongHouse, VoteStatus houseVoteStatus)
        {
            if (houseVoteStatus == VoteStatus.AgreeVote)
            {
                //解散成功
                OnHouseEndSettlement(mahjongHouse, MahjongHouseStatus.MHS_Dissolved);
                main.DelDissolveVoteHouse(mahjongHouse.houseId);
            }
            else if (houseVoteStatus == VoteStatus.OpposeVote)
            {
                //解散失败
                mahjongHouse.voteBeginTime = DateTime.Parse("1970-01-01 00:00:00");
                mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                {
                    if (housePlayer.voteStatus != VoteStatus.FreeVote)
                    {
                        housePlayer.voteStatus = VoteStatus.FreeVote;
                    }
                });
                main.DelDissolveVoteHouse(mahjongHouse.houseId);
            }
        }
        public void SettlementMahjongHouse(MahjongHouse mahjongHouse)
        {
            if (mahjongHouse.SettlementMahjongs())
            {
                //小局结算
                List<MahjongSettlementNode> mahjongSettlementList = new List<MahjongSettlementNode>();
                mahjongHouse.GetPlayerSettlementList(mahjongSettlementList);
                //流局
                bool bLiuJu = mahjongHouse.CheckMahjongIsLiuJu();
                mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                {
                    RecvSettlementMahjong_L2P recvMsg_L2P = new RecvSettlementMahjong_L2P();
                    recvMsg_L2P.summonerId = housePlayer.summonerId;
                    recvMsg_L2P.bLiuJu = bLiuJu;
                    recvMsg_L2P.mahjongSettlement = Serializer.tryCompressObject(mahjongSettlementList);
                    SendProxyMsg(recvMsg_L2P, housePlayer.proxyServerId);
                    //清理牌
                    housePlayer.InitMahjongTileList();
                    //保存数据库玩家结算信息
                    OnRequestSavePlayerSettlement(mahjongHouse.houseId, housePlayer);
                });
                //清理房间
                mahjongHouse.InitMahjongHouse();
                //保存当局积分
                OnRequestSaveBureauIntegral(mahjongHouse.houseId, mahjongHouse.GetHouseBureau());
                //第一局打完之后扣房卡()
                if (mahjongHouse.CheckeFirstBureauEnd())
                {
                    //不是商家模式并且开启扣房卡模式
                    if (mahjongHouse.businessId == 0 && main.CheckOpenDelHouseCard())
                    {
                        MahjongPlayer housePlayer = mahjongHouse.GetHouseOwner();
                        if (housePlayer != null)
                        {
                            int houseCard = main.GetHouseCard(mahjongHouse.maxBureau);
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
                    mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                    {
                        ModuleManager.Get<MainCityMain>().RecordBusinessUsers(housePlayer.userId, mahjongHouse.businessId);
                    });
                }
                //大局结算
                if (mahjongHouse.CheckeHouseBureau())
                {
                    OnHouseEndSettlement(mahjongHouse, MahjongHouseStatus.MHS_EndBureau);
                }
            }
        }
        public void OnHouseEndSettlement(MahjongHouse mahjongHouse, MahjongHouseStatus houseStatus)
        {
            List<ComPlayerIntegral> playerIntegralList = new List<ComPlayerIntegral>();
            List<MahjongEndSettlementNode> mahjongEndSettlementList = mahjongHouse.GetMahjongEndSettlementList();
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                //优惠券
                TicketsNode tickets = null;
                if (houseStatus == MahjongHouseStatus.MHS_EndBureau && mahjongHouse.businessId > 0 && mahjongHouse.competitionKey == 0)
                {
                    //普通的商家模式才在这里奖励优惠劵
                    int rank = mahjongHouse.GetCurrentRanking(housePlayer.index, housePlayer.allIntegral);
                    tickets = main.GetTickets(mahjongHouse.businessId, rank);
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
                    RecvEndSettlementMahjong_L2P recvMsg = new RecvEndSettlementMahjong_L2P();
                    recvMsg.summonerId = housePlayer.summonerId;
                    recvMsg.houseStatus = houseStatus;
                    recvMsg.allIntegral = sender.allIntegral;
                    recvMsg.ticketsNode = tickets;
                    recvMsg.mahjongEndSettlementList.AddRange(mahjongEndSettlementList);
                    SendProxyMsg(recvMsg, sender.proxyServerId);
                }
                //比赛场积分
                if (mahjongHouse.competitionKey > 0)
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
                    ModuleManager.Get<ServiceBoxMain>().RecvHouseEndSettlement(housePlayer.summonerId, housePlayer.proxyServerId, mahjongHouse.houseCardId);
                }
            });
            mahjongHouse.houseStatus = houseStatus;
            mahjongHouse.operateBeginTime = DateTime.Parse("1970-01-01 00:00:00");
            //保存房间状态
            OnRequestSaveMahjongHouseStatus(mahjongHouse.houseId, mahjongHouse.houseStatus);
            HouseManager.Instance.RemoveHouse(mahjongHouse.houseId);
            //发送比赛场积分
            if (houseStatus == MahjongHouseStatus.MHS_EndBureau)
            {
                OnReqCompetitionIntegral(mahjongHouse.competitionKey, playerIntegralList);
            }
            //同步解散信息
            else if (houseStatus == MahjongHouseStatus.MHS_Dissolved || houseStatus == MahjongHouseStatus.MHS_GMDissolved)
            {
                OnnRequestSaveDissolveMahjongInfo(mahjongHouse);
            }
        }
        public void OnReqMahjongPendulum(int peerId, bool inbound, object msg)
        {
            RequestMahjongPendulum_P2L reqMsg = msg as RequestMahjongPendulum_P2L;
            ReplyMahjongPendulum_L2P replyMsg = new ReplyMahjongPendulum_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            CSStartDisplayType displayTypeMsg = (CSStartDisplayType)reqMsg.startDisplayType;
            if (displayTypeMsg == CSStartDisplayType.SDT_None || !Enum.IsDefined(typeof(CSStartDisplayType), displayTypeMsg))
            {
                //摆牌类型错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 摆牌类型错误! userId = " + sender.userId + ", startDisplayType = " + displayTypeMsg);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (displayTypeMsg == CSStartDisplayType.SDT_MidwayConcealedKong)
            {
                //中途四喜不走这里
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 中途四喜不走这里! userId = " + sender.userId + ", startDisplayType = " + displayTypeMsg);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || mahjongHouse.houseStatus != MahjongHouseStatus.MHS_PendulumBureau)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (!mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_PersonalisePendulum) && (displayTypeMsg == CSStartDisplayType.SDT_AFlower ||
                displayTypeMsg == CSStartDisplayType.SDT_ThreeSameOne || displayTypeMsg == CSStartDisplayType.SDT_ThreeSameTwo ||
                displayTypeMsg == CSStartDisplayType.SDT_SteadilyHighOne || displayTypeMsg == CSStartDisplayType.SDT_SteadilyHighTwo))
            {
                //个性摆牌玩法未开启
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 个性摆牌玩法未开启! userId = " + sender.userId + ", displayTypeMsg = " + displayTypeMsg);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (player.housePlayerStatus != MahjongPlayerStatus.MahjongPendulum || player.index != mahjongHouse.currentShowCard)
            {
                //玩家不是摆牌状态
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 玩家不是摆牌状态! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqMahjongPendulum(mahjongHouse, player, displayTypeMsg, replyMsg);
        }
        public void OnReqMahjongPendulum(MahjongHouse mahjongHouse, MahjongPlayer player, CSStartDisplayType displayTypeMsg, ReplyMahjongPendulum_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyMahjongPendulum_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //CSStartDisplayType startDisplayType = player.StartDisplay();
            //if (startDisplayType == CSStartDisplayType.SDT_None)
            //{
            //    //玩家不需要摆牌
            //    ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 玩家不需要摆牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
            //    replyMsg.result = ResultCode.Wrong;
            //    SendProxyMsg(replyMsg, player.proxyServerId);
            //    return;
            //}
            //if (displayTypeMsg != (startDisplayType & displayTypeMsg))
            //{
            //    //玩家摆牌类型不匹配
            //    ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 玩家摆牌类型不匹配! userId = " + player.userId + ", startDisplayType = " + startDisplayType + ", displayTypeMsg = " + displayTypeMsg);
            //    replyMsg.result = ResultCode.Wrong;
            //    SendProxyMsg(replyMsg, player.proxyServerId);
            //    return;
            //}
            if (displayTypeMsg != (player.startDisplayType & displayTypeMsg))
            {
                //玩家该类型摆牌已经做出选择
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulum, 玩家该类型摆牌已经做出选择! userId = " + player.userId + ", startDisplayType = " + player.startDisplayType + ", displayTypeMsg = " + displayTypeMsg);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            List<int> mahjongList = new List<int>();
            player.GetStartDisplayMahjong(displayTypeMsg, mahjongList);
            //告诉其他人
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != player.userId)
                {
                    OnRecvMahjongPendulum(housePlayer.summonerId, housePlayer.proxyServerId, player.index, (int)displayTypeMsg, mahjongList);
                }
            });
            //积分
            mahjongHouse.SetPlayerBureauStartDisplay(player.index, 0, displayTypeMsg);
            //表示已经做出选择
            player.startDisplayType &= ~displayTypeMsg;
            //掷骰子状态
            player.housePlayerStatus = MahjongPlayerStatus.MahjongPendulumDice;
            //设置房间操作时间
            mahjongHouse.SetHouseOperateBeginTime();

            replyMsg.result = ResultCode.OK;
            replyMsg.startDisplayType = (int)displayTypeMsg;
            replyMsg.mahjongList.AddRange(mahjongList);
            SendProxyMsg(replyMsg, player.proxyServerId);

            //保存摆牌
            OnRequestSaveMahjongPendulum(mahjongHouse.houseId, mahjongHouse.currentBureau, player.index, (int)displayTypeMsg, mahjongList);
        }
        public void OnRecvMahjongPendulum(ulong summonerId, int proxyServerId, int index, int startDisplayType, List<int> mahjongList)
        {
            RecvMahjongPendulum_L2P recvMsg = new RecvMahjongPendulum_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.index = index;
            recvMsg.startDisplayType = startDisplayType;
            recvMsg.mahjongList.AddRange(mahjongList);
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnReqMahjongPendulumDice(int peerId, bool inbound, object msg)
        {
            RequestMahjongPendulumDice_P2L reqMsg = msg as RequestMahjongPendulumDice_P2L;
            ReplyMahjongPendulumDice_L2P replyMsg = new ReplyMahjongPendulumDice_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulumDice, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulumDice, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || mahjongHouse.houseStatus != MahjongHouseStatus.MHS_PendulumBureau)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulumDice, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulumDice, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (player.housePlayerStatus != MahjongPlayerStatus.MahjongPendulumDice || player.index != mahjongHouse.currentShowCard)
            {
                //玩家不是摆牌状态
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulumDice, 玩家不是摆牌掷骰子状态! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqMahjongPendulumDice(mahjongHouse, player, replyMsg);
        }
        public void OnReqMahjongPendulumDice(MahjongHouse mahjongHouse, MahjongPlayer player, ReplyMahjongPendulumDice_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyMahjongPendulumDice_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //CSStartDisplayType startDisplayType = player.StartDisplay();
            //if (startDisplayType == CSStartDisplayType.SDT_None)
            //{
            //    //玩家不需要摆牌
            //    ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongPendulumDice, 玩家不需要摆牌掷骰子! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
            //    replyMsg.result = ResultCode.Wrong;
            //    SendProxyMsg(replyMsg, player.proxyServerId);
            //    return;
            //}
            //获取庄的位置
            int zhuangPlayerIndex = player.index;
            if (!mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_HuZhuang) && player.zhuangLeisureType != ZhuangLeisureType.Zhuang)
            {
                //不是胡牌为庄且自己不是庄，再找现庄的位置
                zhuangPlayerIndex = mahjongHouse.GetHouseZhuangIndex();
            }
            //随机骰子
            int leftDice = LegendProtocol.MyRandom.NextPrecise(1, 7);
            int rightDice = LegendProtocol.MyRandom.NextPrecise(1, 7);
            //算鸟
            int[] birdNumberArray = new int[] { 0, 0, 0, 0 };
            int remainder = leftDice % mahjongHouse.maxPlayerNum;
            birdNumberArray[remainder] += 1;
            remainder = rightDice % mahjongHouse.maxPlayerNum;
            birdNumberArray[remainder] += 1;
            //计算中鸟
            int[] playerBirdArray = new int[] { 0, 0, 0, 0 };
            int playerIndex = mahjongHouse.GetLastHousePlayerIndex(zhuangPlayerIndex);
            for (int i = 0; i < mahjongHouse.GetHousePlayerCount(); ++i)
            {
                playerBirdArray[playerIndex] += birdNumberArray[i];
                playerIndex = mahjongHouse.GetNextHousePlayerIndex(playerIndex);
            }
            int startDisplyWinIntegral = 0;
            //计算分
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != player.userId)
                {
                    //基础分
                    int startDisplyLoseIntegral = main.startDisplayIntegral;
                    //算庄
                    if (mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_ZhuangLeisure) && (player.index == zhuangPlayerIndex || housePlayer.index == zhuangPlayerIndex))
                    {
                        startDisplyLoseIntegral += 1;
                    }
                    //算鸟
                    if (playerBirdArray[player.index] > 0 || playerBirdArray[housePlayer.index] > 0)
                    {
                        startDisplyLoseIntegral += mahjongHouse.GetWinBirdIntegral(startDisplyLoseIntegral, playerBirdArray[housePlayer.index], playerBirdArray[player.index]);
                    }     
                    //算飘
                    if (mahjongHouse.flutter > 0)
                    {
                        startDisplyLoseIntegral += mahjongHouse.GetWinFlutterIntegral();
                    }
                    startDisplyWinIntegral += startDisplyLoseIntegral;
                    mahjongHouse.SetPlayerBureauStartDisplay(housePlayer.index, -startDisplyLoseIntegral);
                    housePlayer.allIntegral -= startDisplyLoseIntegral;
                    replyMsg.playerIntegralList.Add(new PlayerIntegral { playerIndex = housePlayer.index, integral = housePlayer.allIntegral });
                }
            });
            //积分
            mahjongHouse.SetPlayerBureauStartDisplay(player.index, startDisplyWinIntegral);
            player.allIntegral += startDisplyWinIntegral;
            replyMsg.playerIntegralList.Add(new PlayerIntegral { playerIndex = player.index, integral = player.allIntegral });
            //表示已经做出选择
            if (player.startDisplayType == CSStartDisplayType.SDT_None)
            {
                //恢复状态
                player.housePlayerStatus = MahjongPlayerStatus.MahjongFree;
                //计算下个摆牌的玩家
                mahjongHouse.DisposeNextPendulumPlayer(player.index);
            }
            else
            {
                player.housePlayerStatus = MahjongPlayerStatus.MahjongPendulum;
            }

            //骰子
            replyMsg.pendulumDice = leftDice * 10 + rightDice;

            //下发消息
            OnRecvMahjongPendulumDice(mahjongHouse, player.index, replyMsg.pendulumDice, replyMsg.playerIntegralList);
            
            //设置房间操作时间
            mahjongHouse.SetHouseOperateBeginTime();

            replyMsg.result = ResultCode.OK;
            replyMsg.nextPendulumIndex = mahjongHouse.currentShowCard;
            SendProxyMsg(replyMsg, player.proxyServerId);

            //保存摆牌掷骰子
            OnRequestSaveMahjongPendulumDice(mahjongHouse.houseId, mahjongHouse.currentBureau, player.index, replyMsg.pendulumDice);
            //处理摆牌
            DisposeMahjongPendulum(mahjongHouse);
        }
        public void OnRecvMahjongPendulumDice(MahjongHouse mahjongHouse, int playerIndex, int pendulumDice, List<PlayerIntegral> playerIntegralList)
        {
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.index != playerIndex)
                {
                    RecvMahjongPendulumDice_L2P recvMsg = new RecvMahjongPendulumDice_L2P();
                    recvMsg.summonerId = housePlayer.summonerId;
                    recvMsg.index = playerIndex;
                    recvMsg.nextPendulumIndex = mahjongHouse.currentShowCard;
                    recvMsg.pendulumDice = pendulumDice;
                    recvMsg.playerIntegralList.AddRange(playerIntegralList);
                    SendProxyMsg(recvMsg, housePlayer.proxyServerId);
                }
            });
        }
        public void DisposeMahjongPendulum(MahjongHouse mahjongHouse)
        {
            if (mahjongHouse.PendulumMahjongs())
            {
                mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                {
                    RecvMahjongEndPendulum_L2P recvMsg = new RecvMahjongEndPendulum_L2P();
                    recvMsg.summonerId = housePlayer.summonerId;
                    recvMsg.currentShowCard = mahjongHouse.currentShowCard;
                    recvMsg.houseStatus = mahjongHouse.houseStatus;
                    recvMsg.housePlayerStatus = housePlayer.housePlayerStatus;
                    SendProxyMsg(recvMsg, housePlayer.proxyServerId);
                });
            }
        }
        public void OnReqReadyMahjongHouse(int peerId, bool inbound, object msg)
        {
            RequestReadyMahjongHouse_P2L reqMsg = msg as RequestReadyMahjongHouse_P2L;
            ReplyReadyMahjongHouse_L2P replyMsg = new ReplyReadyMahjongHouse_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号，不需要退出
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyMahjongHouse, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyMahjongHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || (mahjongHouse.currentBureau > 0 && mahjongHouse.houseStatus != MahjongHouseStatus.MHS_Settlement) ||
               (mahjongHouse.currentBureau == 0 && mahjongHouse.houseStatus != MahjongHouseStatus.MHS_FreeBureau))
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyMahjongHouse, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqReadyMahjongHouse, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqReadyMahjongHouse(mahjongHouse, player, replyMsg);
        }
        public void OnReqReadyMahjongHouse(MahjongHouse mahjongHouse, MahjongPlayer player, ReplyReadyMahjongHouse_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyReadyMahjongHouse_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            player.housePlayerStatus = MahjongPlayerStatus.MahjongReady;

            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (player.userId != housePlayer.userId)
                {
                    OnRecvReadyMahjongHouse(housePlayer.summonerId, housePlayer.proxyServerId, player.index);
                }
            });

            replyMsg.result = ResultCode.OK;
            SendProxyMsg(replyMsg, player.proxyServerId);

            //开局
            BeginMahjongs(mahjongHouse);
        }
        public void OnRecvReadyMahjongHouse(ulong summonerId, int proxyServerId, int readyIndex)
        {
            RecvReadyMahjongHouse_L2P recvMsg = new RecvReadyMahjongHouse_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.readyIndex = readyIndex;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnReqMahjongHouseInfo(int peerId, bool inbound, object msg)
        {
            RequestMahjongHouseInfo_P2L reqMsg = msg as RequestMahjongHouseInfo_P2L;
            ReplyMahjongHouseInfo_L2P replyMsg = new ReplyMahjongHouseInfo_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongHouseInfo, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongHouseInfo, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                InitSummonerHouseId(sender);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || mahjongHouse.houseStatus >= MahjongHouseStatus.MHS_Dissolved)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongHouseInfo, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
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
                main.OnRecvGMDissolveMahjongHouse(mahjongHouse);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongHouseInfo, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                InitSummonerHouseId(sender);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            //返回房间信息
            replyMsg.houseId = mahjongHouse.houseCardId;
            replyMsg.onlyHouseId = mahjongHouse.houseId;
            replyMsg.mahjongType = mahjongHouse.mahjongType;
            replyMsg.currentBureau = mahjongHouse.currentBureau;
            replyMsg.maxBureau = mahjongHouse.maxBureau;
            replyMsg.currentShowCard = mahjongHouse.currentShowCard;
            replyMsg.currentWhoPlay = mahjongHouse.currentWhoPlay;
            replyMsg.houseStatus = mahjongHouse.houseStatus;
            replyMsg.housePropertyType = mahjongHouse.housePropertyType;
            replyMsg.catchBird = mahjongHouse.catchBird;
            replyMsg.flutter = mahjongHouse.flutter;
            replyMsg.bFakeHu = mahjongHouse.bFakeHu;
            replyMsg.maxPlayerNum = mahjongHouse.maxPlayerNum;
            replyMsg.businessId = mahjongHouse.businessId;
            replyMsg.competitionKey = mahjongHouse.competitionKey;
            replyMsg.mahjongSpecialType = mahjongHouse.mahjongSpecialType;
            replyMsg.remainMahjongCount = mahjongHouse.GetRemainMahjongCount();
            replyMsg.bNeedOperat = mahjongHouse.CheckPlayerOperat(player.index);
            replyMsg.zhuangIndex = mahjongHouse.GetHouseZhuangIndex();
            mahjongHouse.GetCurrentMahjongList(replyMsg.currentMahjongList);
            //获取玩家信息
            MahjongOnlineNode mahjongOnlineNode = new MahjongOnlineNode();
            mahjongOnlineNode.myPlayerOnline = main.GetMyPlayerOnlineNode(player);
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != player.userId)
                {
                    mahjongOnlineNode.playerOnlineList.Add(main.GetPlayerOnlineNode(housePlayer));
                    if (player.lineType != LineType.OnLine)
                    {
                        ModuleManager.Get<MainCityMain>().OnRecvPlayerLineType(housePlayer.summonerId, housePlayer.proxyServerId, player.index, LineType.OnLine);
                    }
                }
            });
            replyMsg.mahjongOnlineNode = Serializer.tryCompressObject(mahjongOnlineNode);
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
            if (mahjongHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                TimeSpan span = DateTime.Now.Subtract(mahjongHouse.voteBeginTime);
                if (span.TotalSeconds < main.houseDissolveVoteTime)
                {
                    replyMsg.houseVoteTime = main.houseDissolveVoteTime - span.TotalSeconds;
                }
            }

            replyMsg.result = ResultCode.OK;
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void OnReqShowMahjong(int peerId, bool inbound, object msg)
        {
            RequestShowMahjong_P2L reqMsg = msg as RequestShowMahjong_P2L;
            ReplyShowMahjong_L2P replyMsg = new ReplyShowMahjong_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowMahjong, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (reqMsg.mahjongNode == 0)
            {
                //出牌麻将错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowMahjong, 出牌麻将错误! userId = " + sender.userId + ", reqMsg.mahjongNode == null");
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowMahjong, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || mahjongHouse.houseStatus != MahjongHouseStatus.MHS_BeginBureau)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowMahjong, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (mahjongHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间正在投票中
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowMahjong, 房间正在投票中! userId = " + sender.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.HouseGoingDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongTile showMahjongTile = new MahjongTile(reqMsg.mahjongNode);
            if (showMahjongTile == null || (mahjongHouse.mahjongType == MahjongType.RedLaiZiMahjong && showMahjongTile.IsRed()))
            {
                //玩家出牌有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowMahjong, 玩家出牌有误! mahjongNode = " + reqMsg.mahjongNode);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowMahjong, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (player.index != mahjongHouse.currentShowCard || player.housePlayerStatus != MahjongPlayerStatus.MahjongWaitCard)
            {
                //玩家不是出牌状态
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowMahjong, 玩家不是出牌状态! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqShowMahjong(mahjongHouse, player, showMahjongTile, replyMsg);
        }
        public void OnReqShowMahjong(MahjongHouse mahjongHouse, MahjongPlayer player, MahjongTile showMahjongTile, ReplyShowMahjong_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyShowMahjong_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            if (!player.CheckPlayerShowMahjong())
            {
                //玩家手牌不能出牌
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowMahjong, 玩家手牌不能出牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (!player.CheckPlayerMahjong(showMahjongTile))
            {
                //发来的牌不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqShowMahjong, 发来的牌不存在! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            player.housePlayerStatus = MahjongPlayerStatus.MahjongShowCard;
            //从手牌中删除
            player.DelPlayerMahjong(showMahjongTile);
            //恢复胡牌
            if (player.m_bGiveUpWin)
            {
                player.m_bGiveUpWin = false;
            }
            //恢复新牌
            if (player.newMahjongTile != null)
            {
                player.newMahjongTile = null;
            }
            //恢复碰牌
            if (player.currPongTile != null)
            {
                player.currPongTile = null;
            }

            mahjongHouse.currentShowCard = -1;
            mahjongHouse.currentWhoPlay = player.index;
            mahjongHouse.currentMahjongList.Clear();
            mahjongHouse.currentMahjongList.Add(showMahjongTile);

            replyMsg.result = ResultCode.OK;
            replyMsg.mahjongNode = showMahjongTile.GetMahjongNode();
            SendProxyMsg(replyMsg, player.proxyServerId);

            //保存出牌
            OnRequestSaveShowMahjong(mahjongHouse.houseId, mahjongHouse.currentBureau, player.index, replyMsg.mahjongNode);

            //处理出牌
            if (!DiposeShowMahjong(player.userId, player.index, mahjongHouse, replyMsg.mahjongNode))
            {
                //出牌没人要 增加出牌列表
                player.AddShowMahjong(showMahjongTile);
                //发牌
                GiveOffMahjong(mahjongHouse, player.index);
            }
            else
            {
                //设置房间操作时间
                mahjongHouse.SetHouseOperateBeginTime();
            }
        }
        public bool DiposeShowMahjong(string userId, int playerIndex, MahjongHouse mahjongHouse, int mahjongNode)
        {
            bool bIsAllNeed = false;
            int remainMahjongCount = mahjongHouse.GetRemainMahjongCount();
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != userId)
                {
                    bool bIsNeed = false;
                    if (mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_EatHu) && !housePlayer.m_bGiveUpWin)
                    {
                        //可以吃胡
                        if (CSSpecialWinType.WT_None != housePlayer.WinHandTileCheck(mahjongNode, false))
                        {
                            mahjongHouse.AddMahjongOperat(housePlayer.index, MahjongOperatType.EMO_Hu);
                            bIsNeed = true;
                        }
                    }
                    //杠碰
                    if (!bIsNeed && remainMahjongCount > 0)
                    {
                        MeldType meldType = housePlayer.PongKongCheck(mahjongNode);
                        if (meldType == MeldType.EM_ExposedKong || meldType == MeldType.EM_Triplet)
                        {
                            if (meldType == MeldType.EM_ExposedKong && !(housePlayer.m_bReadyHand && !housePlayer.KongCheckByCSMahjong(mahjongNode, 3)))
                            {
                                if (remainMahjongCount > mahjongHouse.GetHouseSelectSeabedCount() || mahjongHouse.mahjongType != MahjongType.ChangShaMahjong)
                                {
                                    //听牌后,杠完能再听牌
                                    mahjongHouse.AddMahjongOperat(housePlayer.index, MahjongOperatType.EMO_Kong);
                                    bIsNeed = true;
                                }
                            }
                            else if (meldType == MeldType.EM_Triplet && !housePlayer.m_bReadyHand)
                            {
                                mahjongHouse.AddMahjongOperat(housePlayer.index, MahjongOperatType.EMO_Pong);
                                bIsNeed = true;
                            }
                        }
                    }
                    if (!bIsNeed && remainMahjongCount > 0 && housePlayer.index == mahjongHouse.GetNextHousePlayerIndex(playerIndex) && !housePlayer.m_bReadyHand)
                    {
                        //下家不听牌才能吃
                        if (housePlayer.ChowCheck(mahjongNode))
                        {
                            mahjongHouse.AddMahjongOperat(housePlayer.index, MahjongOperatType.EMO_Chow);
                            bIsNeed = true;
                        }
                    }
                    if (!bIsAllNeed && bIsNeed)
                    {
                        bIsAllNeed = true;
                    }
                    OnRecvShowMahjong(housePlayer.summonerId, housePlayer.proxyServerId, playerIndex, bIsNeed, mahjongNode);
                }
            });
            return bIsAllNeed;
        }
        public void OnRecvShowMahjong(ulong summonerId, int proxyServerId, int playerIndex, bool bIsNeed, int mahjongNode)
        {
            RecvShowMahjong_L2P recvMsg = new RecvShowMahjong_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.index = playerIndex;
            recvMsg.bIsNeed = bIsNeed;
            recvMsg.mahjongNode = mahjongNode;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void GiveOffMahjong(MahjongHouse mahjongHouse, int playerIndex)
        {
            int remainMahjongCount = mahjongHouse.GetRemainMahjongCount();
            if (0 == remainMahjongCount)
            {
                //流局 
                SetMahjongHouseSettlement(mahjongHouse);
                return;
            }
            else if (mahjongHouse.GetHouseSelectSeabedCount() == remainMahjongCount && mahjongHouse.mahjongType == MahjongType.ChangShaMahjong)
            {
                //海底捞月
                mahjongHouse.currentWhoPlay = mahjongHouse.GetNextHousePlayerIndex(playerIndex);
                mahjongHouse.currentMahjongList.Clear();
                mahjongHouse.currentShowCard = mahjongHouse.currentWhoPlay;
                mahjongHouse.mahjongSpecialType = MahjongSpecialType.EMS_Seabed;
                mahjongHouse.houseStatus = MahjongHouseStatus.MHS_SelectSeabed;
                //设置房间操作时间
                mahjongHouse.SetHouseOperateBeginTime();
                OnRecvSelectSeabedMahjong(mahjongHouse);
                return;
            }
            //给下一个玩家发牌 并告诉其他玩家
            MahjongTile newTile = GetNewTileByRemainMahjong(mahjongHouse);
            MahjongPlayer nextPlayer = mahjongHouse.GetNextHousePlayer(playerIndex);
            if (newTile != null && nextPlayer != null)
            {
                //轮到下家出牌
                mahjongHouse.currentShowCard = nextPlayer.index;
                //设置房间操作时间
                mahjongHouse.SetHouseOperateBeginTime();
                //保存手牌
                nextPlayer.AddNewHeadMahjong(newTile);
                //标志出牌
                nextPlayer.housePlayerStatus = MahjongPlayerStatus.MahjongWaitCard;
                //发送出去
                OnRecvGiveOffMahjong(nextPlayer.summonerId, nextPlayer.proxyServerId, mahjongHouse.currentShowCard, nextPlayer.housePlayerStatus, newTile);
                mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                {
                    if (housePlayer.userId != nextPlayer.userId)
                    {
                        OnRecvGiveOffMahjong(housePlayer.summonerId, housePlayer.proxyServerId, mahjongHouse.currentShowCard, housePlayer.housePlayerStatus);
                    }
                });
                //保存发新牌
                OnRequestSaveGiveOffMahjong(mahjongHouse.houseId, mahjongHouse.currentBureau, nextPlayer.index, newTile);
            }
        }
        public void OnRecvSelectSeabedMahjong(MahjongHouse mahjongHouse)
        {
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                RecvSelectSeabedMahjong_L2P recvMsg_L2P = new RecvSelectSeabedMahjong_L2P();
                recvMsg_L2P.summonerId = housePlayer.summonerId;
                recvMsg_L2P.selectSeabedIndex = mahjongHouse.currentShowCard;
                recvMsg_L2P.houseStatus = mahjongHouse.houseStatus;
                SendProxyMsg(recvMsg_L2P, housePlayer.proxyServerId);
            });
        }
        public void OnRecvGiveOffMahjong(ulong summonerId, int proxyServerId, int currentShowCard, MahjongPlayerStatus housePlayerStatus, MahjongTile newTile = null, List<PlayerIntegral> playerIntegralList = null)
        {
            RecvGiveOffMahjong_L2P recvMsg_L2P = new RecvGiveOffMahjong_L2P();
            recvMsg_L2P.summonerId = summonerId;
            recvMsg_L2P.currentShowCard = currentShowCard;
            recvMsg_L2P.housePlayerStatus = housePlayerStatus;
            if (playerIntegralList != null)
            {
                recvMsg_L2P.playerIntegralList.AddRange(playerIntegralList);
            }
            if (newTile != null)
            {
                recvMsg_L2P.mahjongNode = newTile.GetMahjongNode();
            }
            SendProxyMsg(recvMsg_L2P, proxyServerId);
        }
        public void OnReqOperatMahjong(int peerId, bool inbound, object msg)
        {
            RequestOperatMahjong_P2L reqMsg = msg as RequestOperatMahjong_P2L;
            ReplyOperatMahjong_L2P replyMsg = new ReplyOperatMahjong_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (reqMsg.mahjongNode == null || (reqMsg.operatType != MahjongOperatType.EMO_None && reqMsg.mahjongNode.Count == 0))
            {
                //操作麻将错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 操作麻将错误! userId = " + sender.userId + ", reqMsg.mahjongNode == null");
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || mahjongHouse.houseStatus != MahjongHouseStatus.MHS_BeginBureau)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (mahjongHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间正在投票中
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 房间正在投票中! userId = " + sender.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.HouseGoingDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (0 == mahjongHouse.GetRemainMahjongCount() && reqMsg.operatType != MahjongOperatType.EMO_Hu && reqMsg.operatType != MahjongOperatType.EMO_None)
            {
                //最后底牌的时候只能胡或者放弃
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 最后底牌的时候只能胡或者放弃! userId = " + sender.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (mahjongHouse.mahjongType == MahjongType.ChangShaMahjong && reqMsg.operatType == MahjongOperatType.EMO_Kong && mahjongHouse.GetHouseSelectSeabedCount() == mahjongHouse.GetRemainMahjongCount())
            {
                //长沙麻将最后海底牌的时候不能杠或者补
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 长沙麻将最后海底牌的时候不能杠或者补! userId = " + sender.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (mahjongHouse.currentShowCard == -1)
            {
                //操作
                OnReqOperatMahjong(mahjongHouse, player, reqMsg.operatType, reqMsg.mahjongNode, replyMsg);
            }
            else if (mahjongHouse.currentShowCard == -2)
            {
                //抢杠胡操作
                OnReqOperatMahjongByGrabKong(mahjongHouse, player, reqMsg.operatType, reqMsg.mahjongNode, replyMsg);
            }
            else
            {
                //对自己进行操作
                OnReqOperatMahjongByMyself(mahjongHouse, player, reqMsg.operatType, reqMsg.mahjongNode, replyMsg);
            }
        }
        public void OnReqOperatMahjong(MahjongHouse mahjongHouse, MahjongPlayer player, MahjongOperatType operatType, List<int> mahjongList, ReplyOperatMahjong_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyOperatMahjong_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //别人的
            if (!player.CheckPlayerShowMahjong(1))
            {
                //玩家手牌不能对别人进行操作
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 玩家手牌不能对别人进行操作! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (operatType == MahjongOperatType.EMO_Hu)
            {
                if (!mahjongHouse.CheckCurrentMahjong(mahjongList))
                {
                    //胡牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 胡牌操作错误! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_EatHu))
                {
                    //房间不允许放炮胡
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 房间不允许放炮胡! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                //if (player.m_bGiveUpWin && !mahjongHouse.CheckPlayerSeabedWin())
                if (player.m_bGiveUpWin)
                {
                    //胡牌操作错误 玩家放弃胡牌没恢复
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 胡牌操作错误 玩家放弃胡牌没恢复! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!player.WinHandTileCheck(mahjongList, mahjongHouse.bFakeHu))
                {
                    //胡牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 胡牌操作错误 不能胡! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                //催胡
                List<MahjongOperatNode> mahjongOperatHuList = mahjongHouse.GetPlayerMahjongOperatHu();
                if (mahjongOperatHuList!= null && mahjongOperatHuList.Count > 0)
                {
                    List<PlayerWinMahjongNode> winPlayerList = new List<PlayerWinMahjongNode>();
                    mahjongOperatHuList.ForEach(node =>
                    {
                        //是自己就赋值进去,不然再算一遍浪费时间
                        if (node.playerIndex == player.index)
                        {
                            node.operatMahjonList.AddRange(mahjongList);
                        }
                        PlayerWinMahjongNode tileNode = mahjongHouse.GetHousePlayerTile(node.playerIndex, node.operatMahjonList, mahjongHouse.bFakeHu);
                        if (tileNode != null)
                        {
                            winPlayerList.Add(tileNode);
                        }
                    });
                    OnRecvWinMahjong(mahjongHouse, WinMahjongType.EWM_BlastWin, winPlayerList, mahjongHouse.currentWhoPlay);
                    return;
                }
            }
            else if (operatType == MahjongOperatType.EMO_Kong)
            {
                if (!mahjongHouse.CheckCurrentMahjong(mahjongList[0]))
                {
                    //明杠牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 明杠牌操作错误! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (MeldType.EM_ExposedKong == player.PongKongCheck(mahjongList[0]))
                {
                    if (mahjongList.Count == 2)
                    {
                        if (mahjongHouse.mahjongType != MahjongType.ChangShaMahjong || !player.KongCheckByCSMahjong(mahjongList[0], 3))
                        {
                            //杠完不能听牌
                            ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 杠完不能听牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                            replyMsg.result = ResultCode.Wrong;
                            SendProxyMsg(replyMsg, player.proxyServerId);
                            return;
                        }
                    }
                    else if (player.m_bReadyHand && !player.KongCheckByCSMahjong(mahjongList[0], 3))
                    {
                        //听牌后,杠完不能再停牌
                        ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 听牌后,杠完不能再停牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                        replyMsg.result = ResultCode.Wrong;
                        SendProxyMsg(replyMsg, player.proxyServerId);
                        return;
                    }
                }
                else if (player.index == mahjongHouse.currentWhoPlay && null != player.ExposedKongCheck(new MahjongTile(mahjongList[0])))
                {     
                    //自己杠自己的牌先不判断能不能听牌               
                }
                else
                {
                    //杠牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 杠牌操作错误! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
            }
            else if (operatType == MahjongOperatType.EMO_Pong)
            {
                if (player.m_bReadyHand)
                {
                    //听牌后不能碰
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 听牌后不能碰! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (player.index == mahjongHouse.currentWhoPlay)
                {
                    //请不要碰自己的牌
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 请不要碰自己的牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!mahjongHouse.CheckCurrentMahjong(mahjongList[0]))
                {
                    //碰牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 碰牌操作错误! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                MeldType meldType = player.PongKongCheck(mahjongList[0]);
                if (MeldType.EM_Triplet != meldType && MeldType.EM_ExposedKong != meldType)
                {
                    //碰牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 碰牌操作错误! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
            }
            else if (operatType == MahjongOperatType.EMO_Chow)
            {
                if (player.m_bReadyHand)
                {
                    //听牌后不能吃
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 听牌后不能吃! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (mahjongList.Count != 3)
                {
                    //吃牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 吃牌操作错误 吃的牌个数不对! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!mahjongHouse.CheckCurrentMahjong(mahjongList[1]))
                {
                    //吃牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 吃牌操作错误 吃的牌不再操作列表里面! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (mahjongHouse.currentWhoPlay != mahjongHouse.GetLastHousePlayerIndex(player.index) || !player.ChowCheck(mahjongList[1]))
                {
                    //吃牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 吃牌操作错误 不能吃目标牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (!player.CheckChowMahjong(mahjongList))
                {
                    //吃牌选牌错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 吃牌选牌错误 选择的牌手牌里面没有! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
            }
            else if (operatType == MahjongOperatType.EMO_None)
            {
                //放弃胡牌
                if (MahjongOperatType.EMO_Hu == mahjongHouse.GetPlayerMahjongOperatType(player.index) && !player.m_bGiveUpWin)
                {
                    player.m_bGiveUpWin = true;
                }
            }
            else
            {
                //操作类型有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 操作类型有误! userId = " + player.userId + ", operatType = " + operatType);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (!mahjongHouse.SetMahjongOperat(player.index, operatType, mahjongList))
            {
                //操作错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjong, 操作错误! userId = " + player.userId + ", operatType = " + operatType);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            List<MahjongOperatNode> mahjongOperatList = mahjongHouse.GetPlayerMahjongOperat();
            if (mahjongOperatList == null || mahjongOperatList.Count <= 0)
            {
                //告诉玩家操作成功了
                replyMsg.result = ResultCode.OK;
                replyMsg.operatType = operatType;
                replyMsg.currentShowCard = mahjongHouse.currentShowCard;
                replyMsg.housePlayerStatus = player.housePlayerStatus;
                replyMsg.bGiveUpWin = player.m_bGiveUpWin;
                replyMsg.bFakeHu = mahjongHouse.bFakeHu;
                SendProxyMsg(replyMsg, player.proxyServerId);

                //处理发牌
                MahjongPlayer playPlayer = player;
                if (mahjongHouse.currentWhoPlay != player.index)
                {
                    playPlayer = mahjongHouse.GetMahjongPlayer(mahjongHouse.currentWhoPlay);
                }
                mahjongHouse.ClearMahjongOperat();
                if (mahjongHouse.mahjongSpecialType != MahjongSpecialType.EMS_None)
                {
                    mahjongHouse.mahjongSpecialType = MahjongSpecialType.EMS_None;
                }
                mahjongHouse.SetFakeHu(false);
                //出牌没人要 增加出牌列表
                playPlayer.AddShowMahjong(mahjongHouse.currentMahjongList);
                //发牌
                GiveOffMahjong(mahjongHouse, playPlayer.index);
                return;
            }
            if (mahjongOperatList.Exists(element => element.operatedType == MahjongOperatType.EMO_Hu && element.bWait))
            {
                //继续等待，但是告诉玩家操作成功了
                replyMsg.result = ResultCode.OK;
                replyMsg.operatType = operatType;
                replyMsg.currentShowCard = mahjongHouse.currentShowCard;
                replyMsg.housePlayerStatus = player.housePlayerStatus;
                replyMsg.bGiveUpWin = player.m_bGiveUpWin;
                replyMsg.bFakeHu = mahjongHouse.bFakeHu;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            else
            {
                MahjongOperatNode operatNode = main.GetMahjongOperatNode(mahjongHouse, mahjongOperatList);
                if (operatNode == null || operatNode.operatedType == MahjongOperatType.EMO_None)
                {
                    return;
                }
                if (operatNode.bWait)
                {
                    //继续等待，但是告诉玩家操作成功了
                    replyMsg.result = ResultCode.OK;
                    replyMsg.operatType = operatType;
                    replyMsg.currentShowCard = mahjongHouse.currentShowCard;
                    replyMsg.housePlayerStatus = player.housePlayerStatus;
                    replyMsg.bGiveUpWin = player.m_bGiveUpWin;
                    replyMsg.bFakeHu = mahjongHouse.bFakeHu;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                MahjongPlayer playPlayer = player;
                if (operatNode.playerIndex != player.index)
                {
                    playPlayer = mahjongHouse.GetMahjongPlayer(operatNode.playerIndex);
                }
                Meld meld = null;
                MahjongKongType kongType = MahjongKongType.EMK_None;
                if (operatNode.operatedType == MahjongOperatType.EMO_Kong)
                {
                    meld = playPlayer.Kong(operatNode.operatMahjonList, playPlayer.index == mahjongHouse.currentWhoPlay);
                    if (operatNode.operatMahjonList.Count == 2)
                    {
                        kongType = MahjongKongType.EMK_Kong;
                        mahjongHouse.mahjongSpecialType = MahjongSpecialType.EMS_Kong;
                    }
                    else
                    {
                        kongType = MahjongKongType.EMK_MakeUp;
                    }
                }
                else if (operatNode.operatedType == MahjongOperatType.EMO_Pong)
                {
                    meld = playPlayer.Pong(operatNode.operatMahjonList);
                }
                else if (operatNode.operatedType == MahjongOperatType.EMO_Chow)
                {
                    meld = playPlayer.Chow(operatNode.operatMahjonList);
                }
                if (meld != null)
                {
                    mahjongHouse.ClearMahjongOperat();
                    mahjongHouse.currentShowCard = playPlayer.index;
                    playPlayer.housePlayerStatus = MahjongPlayerStatus.MahjongWaitCard;
                    playPlayer.m_bGiveUpWin = true;
                    replyMsg.bOperatMySelf = playPlayer.index == mahjongHouse.currentWhoPlay;
                    replyMsg.bOperatHand = false;
                    replyMsg.kongType = mahjongHouse.GetRecordMahjongKongType(kongType);
                    if (mahjongHouse.currentMahjongList.Count > 1 && meld.m_meldTileList.Count > 1)
                    {
                        MahjongTile mahjongTile = mahjongHouse.currentMahjongList.Find(element => element.Equal(meld.m_meldTileList[1]));
                        if (mahjongTile != null)
                        {
                            mahjongHouse.currentMahjongList.Remove(mahjongTile);
                            MahjongPlayer kongPlayer = mahjongHouse.GetMahjongPlayer(mahjongHouse.currentWhoPlay);
                            if (kongPlayer != null && mahjongHouse.currentMahjongList.Count > 0)
                            {
                                kongPlayer.AddShowMahjong(mahjongHouse.currentMahjongList);
                            }
                        }
                    }
                    if(mahjongHouse.currentMahjongList.Count > 0)
                    {
                        mahjongHouse.currentMahjongList.Clear();
                    }
                    if (kongType != MahjongKongType.EMK_Kong && mahjongHouse.mahjongSpecialType != MahjongSpecialType.EMS_None)
                    {
                        mahjongHouse.SetFakeHu(false);
                        mahjongHouse.mahjongSpecialType = MahjongSpecialType.EMS_None;
                    }
                    else if (kongType == MahjongKongType.EMK_Kong)
                    {
                        mahjongHouse.SetPlayerKongMahjongByFakeHu(playPlayer.index, operatNode.operatMahjonList[0]);
                    }
                    if (mahjongHouse.mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongHouse.mahjongType == MahjongType.RedLaiZiMahjong)
                    {
                        if (operatNode.operatedType == MahjongOperatType.EMO_Kong && meld.m_eMeldType == MeldType.EM_ExposedKong)
                        {
                            MahjongPlayer kongPlayer = mahjongHouse.GetMahjongPlayer(mahjongHouse.currentWhoPlay);
                            if (kongPlayer != null)
                            {
                                //放杠 + 1
                                kongPlayer.m_SmallWinFangBlast += 1;
                                mahjongHouse.SetPlayerBureauFangKong(kongPlayer.index);
                                int integral = (mahjongHouse.maxPlayerNum - 1) * main.zzExposedKongIntegral;
                                kongPlayer.allIntegral -= integral;
                                mahjongHouse.SetPlayerBureauKongIntegral(kongPlayer.index, -integral);
                                //明杠 + 1
                                playPlayer.m_SmallWinJieBlast += 1;
                                playPlayer.allIntegral += integral;
                                mahjongHouse.SetPlayerBureauKongIntegral(playPlayer.index, integral);
                                //积分变了
                                mahjongHouse.bKongInChange = true;
                            }
                        }
                        else if (operatNode.operatedType == MahjongOperatType.EMO_Pong && meld.m_eMeldType == MeldType.EM_Triplet && meld.m_meldTileList.Count > 0 && playPlayer.CheckPlayerMahjong(meld.m_meldTileList[0]))
                        {
                            playPlayer.currPongTile = meld.m_meldTileList[0];
                            playPlayer.noInKongTileList.Add(playPlayer.currPongTile.GetMahjongNode());
                        }
                    }
                    List<int> meldMahjongList = main.GetMahjongNode(meld.m_meldTileList);
                    //保存操作成功
                    OnRequestSaveOperatMahjongSuccess(mahjongHouse.houseId, mahjongHouse.currentBureau, mahjongHouse.currentWhoPlay, playPlayer.index, replyMsg.kongType, meld.m_eMeldType, meldMahjongList);
                    //下发消息
                    OnRecvOperatMahjong(mahjongHouse, playPlayer, replyMsg, meld.m_eMeldType, operatNode.operatedType, kongType, meldMahjongList);
                }
            }
        }
        public void OnReqOperatMahjongByGrabKong(MahjongHouse mahjongHouse, MahjongPlayer player, MahjongOperatType operatType, List<int> mahjongList, ReplyOperatMahjong_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyOperatMahjong_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //抢杠胡操作
            if ((mahjongHouse.mahjongSpecialType != MahjongSpecialType.EMS_Kong && mahjongHouse.mahjongSpecialType != MahjongSpecialType.EMS_MakeUp) || mahjongHouse.kongHuPlayerList.Count == 0)
            {
                //房间没有抢杠胡
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByGrabKong, 房间没有抢杠胡! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (mahjongHouse.currentWhoPlay == player.index)
            {
                //自己不能抢杠胡自己
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByGrabKong, 自己不能抢杠胡自己! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            //if (!mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_GrabKongHu))//--因为抢杠胡选择是后期才加的，为照顾到线上的长沙麻将没有加此选择，后期版本这个选择也会加进去
            if (mahjongHouse.mahjongType != MahjongType.ChangShaMahjong && !mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_GrabKongHu))
            {
                //房间不允许抢杠胡
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByGrabKong, 房间不允许抢杠胡! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (player.m_bGiveUpWin)
            {
                //抢杠胡 玩家放弃胡牌没恢复
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByGrabKong, 抢杠胡 玩家放弃胡牌没恢复! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            MahjongSelectNode selectNode = mahjongHouse.kongHuPlayerList.Find(element => element.playerIndex == player.index);
            if (selectNode == null || !selectNode.bWait)
            {
                //玩家已经做出选择了
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByGrabKong, 玩家已经做出选择了! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (!player.CheckPlayerShowMahjong(1))
            {
                //玩家手牌不能进行抢杠胡操作
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 玩家手牌不能进行抢杠胡操作! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (operatType == MahjongOperatType.EMO_Hu)
            {
                if (!mahjongHouse.CheckCurrentMahjong(mahjongList[0]))
                {
                    //抢杠胡的牌有误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByGrabKong, 抢杠胡的牌有误! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                CSSpecialWinType winType = player.WinHandTileCheck(mahjongList[0], mahjongHouse.bFakeHu);
                if ((!mahjongHouse.bOnlyFakeHu && winType == CSSpecialWinType.WT_None) || (mahjongHouse.bOnlyFakeHu && winType != CSSpecialWinType.WT_FakeHu))
                {
                    //抢杠胡的牌有误, 不能胡
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByGrabKong, 抢杠胡的牌有误, 不能胡! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                //催胡
                List<PlayerWinMahjongNode> winPlayerList = new List<PlayerWinMahjongNode>();
                List<int> winMahjongList = main.GetMahjongNode(mahjongHouse.currentMahjongList);
                mahjongHouse.kongHuPlayerList.ForEach(node =>
                {
                    PlayerWinMahjongNode tileNode = mahjongHouse.GetHousePlayerTile(node.playerIndex, winMahjongList, mahjongHouse.bFakeHu);
                    if (tileNode != null)
                    {
                        winPlayerList.Add(tileNode);
                    }
                });
                OnRecvWinMahjong(mahjongHouse, WinMahjongType.EWM_BlastWin, winPlayerList, mahjongHouse.currentWhoPlay);
            }
            else
            {
                //放弃
                selectNode.bWait = false;

                if (!player.m_bGiveUpWin)
                {
                    player.m_bGiveUpWin = true;
                }

                //告诉玩家操作成功
                replyMsg.result = ResultCode.OK;
                replyMsg.operatType = operatType;
                replyMsg.currentShowCard = mahjongHouse.currentShowCard;
                replyMsg.housePlayerStatus = player.housePlayerStatus;
                replyMsg.bGiveUpWin = player.m_bGiveUpWin;
                replyMsg.bFakeHu = mahjongHouse.bFakeHu;
                SendProxyMsg(replyMsg, player.proxyServerId);

                if (!mahjongHouse.kongHuPlayerList.Exists(element => element.bWait))
                {
                    //没人抢杠胡 
                    int kongTileNode = mahjongHouse.currentMahjongList[0].GetMahjongNode();
                    mahjongHouse.currentShowCard = mahjongHouse.currentWhoPlay;
                    mahjongHouse.currentWhoPlay = -1;
                    mahjongHouse.currentMahjongList.Clear();
                    mahjongHouse.kongHuPlayerList.Clear();
                    mahjongHouse.SetFakeHu(false);
                    mahjongHouse.bOnlyFakeHu = false;
                    //设置房间操作时间
                    mahjongHouse.SetHouseOperateBeginTime();
                    //处理
                    MahjongPlayer playPlayer = mahjongHouse.GetMahjongPlayer(mahjongHouse.currentShowCard);
                    if (mahjongHouse.mahjongSpecialType == MahjongSpecialType.EMS_MakeUp)
                    {
                        mahjongHouse.mahjongSpecialType = MahjongSpecialType.EMS_None;
                        if ((mahjongHouse.mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongHouse.mahjongType == MahjongType.RedLaiZiMahjong))
                        {
                            //明杠 + 1
                            playPlayer.m_SmallWinJieBlast += 1;
                            if (!playPlayer.noInKongTileList.Exists(element => element == kongTileNode))
                            {
                                //有分杠
                                int allKongIntegral = 0;
                                mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                                {
                                    if (housePlayer.userId != playPlayer.userId)
                                    {
                                        housePlayer.allIntegral -= main.zzExposedKongIntegral;
                                        mahjongHouse.SetPlayerBureauKongIntegral(housePlayer.index, -main.zzExposedKongIntegral);
                                        allKongIntegral += main.zzExposedKongIntegral;
                                    }
                                });
                                if (allKongIntegral > 0)
                                {
                                    playPlayer.allIntegral += allKongIntegral;
                                    mahjongHouse.SetPlayerBureauKongIntegral(playPlayer.index, allKongIntegral);
                                }
                                mahjongHouse.bKongInChange = true;
                            }
                            else
                            {
                                playPlayer.noInKongTileList.RemoveAll(element => element == kongTileNode);
                                mahjongHouse.bKongInChange = false;
                            }
                        }
                        GiveOffMahjongByMakeUp(mahjongHouse, playPlayer);
                    }
                    else if (mahjongHouse.mahjongSpecialType == MahjongSpecialType.EMS_Kong)
                    {
                        GiveOffMahjongByKong(mahjongHouse, playPlayer);
                    }
                }
            }
        }
        public void OnReqOperatMahjongByMyself(MahjongHouse mahjongHouse, MahjongPlayer player, MahjongOperatType operatType, List<int> mahjongList, ReplyOperatMahjong_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyOperatMahjong_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //自己的
            if (player.index != mahjongHouse.currentShowCard || player.housePlayerStatus != MahjongPlayerStatus.MahjongWaitCard)
            {
                //玩家不是出牌状态
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 玩家不是出牌状态! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (!player.CheckPlayerShowMahjong())
            {
                //玩家手牌不能对自己进行操作
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 玩家手牌不能对自己进行操作! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (operatType == MahjongOperatType.EMO_Hu)
            {
                if (player.newMahjongTile != null && !player.newMahjongTile.Equal(new MahjongTile(mahjongList[0])))
                {
                    //玩家自摸胡牌不是刚刚摸得
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 玩家自摸胡牌不是刚刚摸得! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (player.m_bGiveUpWin)
                {
                    //自模胡 玩家放弃胡牌没恢复
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 自模胡 玩家放弃胡牌没恢复! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                if (CSSpecialWinType.WT_None == player.WinHandTileCheck())
                {
                    //胡牌操作错误
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 胡牌操作错误! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                OnRecvWinMahjong(mahjongHouse, WinMahjongType.EWM_SelfWin, player, mahjongList);
            }
            else if (operatType == MahjongOperatType.EMO_Kong)
            {
                if ((mahjongHouse.mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongHouse.mahjongType == MahjongType.RedLaiZiMahjong)
                    && player.currPongTile != null && player.currPongTile.Equal(new MahjongTile(mahjongList[0])))
                {
                    //碰完不能立马杠
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 碰完不能立马杠! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                    replyMsg.result = ResultCode.Wrong;
                    SendProxyMsg(replyMsg, player.proxyServerId);
                    return;
                }
                Meld meld = null;
                MahjongKongType kongType = MahjongKongType.EMK_None;
                if (player.PongKongCheck(mahjongList[0]) == MeldType.EM_ConcealedKong)
                {
                    if (mahjongList.Count == 2)
                    {
                        if (mahjongHouse.mahjongType != MahjongType.ChangShaMahjong || !player.KongCheckByCSMahjong(mahjongList[0], 4))
                        {
                            //杠完不能听牌
                            ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 杠完不能听牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                            replyMsg.result = ResultCode.Wrong;
                            SendProxyMsg(replyMsg, player.proxyServerId);
                            return;
                        }
                        kongType = MahjongKongType.EMK_Kong;
                        mahjongHouse.mahjongSpecialType = MahjongSpecialType.EMS_Kong;
                    }
                    else
                    {
                        if (player.m_bReadyHand && !player.KongCheckByCSMahjong(mahjongList[0], 4))
                        {
                            //听牌后,杠完不能再听牌
                            ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 听牌后,杠完不能再停牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                            replyMsg.result = ResultCode.Wrong;
                            SendProxyMsg(replyMsg, player.proxyServerId);
                            return;
                        }
                        kongType = MahjongKongType.EMK_MakeUp;
                    }
                    //暗杠
                    List<MahjongTile> tileList = player.GetTileList(mahjongList[0]);
                    if (tileList == null || tileList.Count != 4)
                    {
                        //获取暗杠牌出错
                        ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 获取暗杠牌出错! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                        replyMsg.result = ResultCode.Wrong;
                        SendProxyMsg(replyMsg, player.proxyServerId);
                        return;
                    }
                    meld = player.ConcealedKong(tileList[0], tileList[1], tileList[2], tileList[3]);
                }
                else
                {
                    //明杠
                    meld = player.ExposedKongCheck(mahjongList[0]);
                    if (meld != null)
                    {
                        if (mahjongList.Count == 2)
                        {
                            if (mahjongHouse.mahjongType != MahjongType.ChangShaMahjong || !player.KongCheckByCSMahjong(mahjongList[0], 1))
                            {
                                //杠完不能听牌
                                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 杠完不能听牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                                replyMsg.result = ResultCode.Wrong;
                                SendProxyMsg(replyMsg, player.proxyServerId);
                                return;
                            }
                        }
                        else if (player.m_bReadyHand && !player.KongCheckByCSMahjong(mahjongList[0], 1))
                        {
                            //听牌后,杠完不能再停牌
                            ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 听牌后,杠完不能再停牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                            replyMsg.result = ResultCode.Wrong;
                            SendProxyMsg(replyMsg, player.proxyServerId);
                            return;
                        }
                        meld.m_eMeldType = MeldType.EM_ExposedKong;
                        meld.m_meldTileList.Add(new MahjongTile(mahjongList[0]));
                        player.DelPlayerMahjong(mahjongList[0]);
                        //处理杠
                        kongType = mahjongHouse.SetPlayerKongMahjong(player, mahjongList);
                    }
                    else
                    {
                        //杠操作错误
                        ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 杠操作错误! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                        replyMsg.result = ResultCode.Wrong;
                        SendProxyMsg(replyMsg, player.proxyServerId);
                        return;
                    }
                }
                if (meld != null)
                {
                    //恢复新牌
                    if (player.newMahjongTile != null)
                    {
                        player.newMahjongTile = null;
                    }
                    bool bNoInKong = false;
                    int kongIntegral = 0;
                    if (mahjongHouse.mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongHouse.mahjongType == MahjongType.RedLaiZiMahjong)
                    {
                        if (meld.m_eMeldType == MeldType.EM_ConcealedKong)
                        {
                            //暗杠 + 1
                            player.m_SmallWinMyself += 1;
                            kongIntegral = main.zzConcealedKongIntegral;
                        }
                        else if (meld.m_eMeldType == MeldType.EM_ExposedKong)
                        {
                            bNoInKong = player.noInKongTileList.Exists(element => element == mahjongList[0]);
                            //抢杠胡不立即计算
                            if (mahjongHouse.kongHuPlayerList.Count == 0)
                            {
                                //明杠 + 1
                                player.m_SmallWinJieBlast += 1;
                                if (!bNoInKong)
                                {
                                    //有分杠
                                    kongIntegral = main.zzExposedKongIntegral;
                                }
                                else
                                {
                                    player.noInKongTileList.RemoveAll(element => element == mahjongList[0]);
                                }
                            }
                        }
                    }
                    replyMsg.bOperatMySelf = true;
                    replyMsg.bOperatHand = true;
                    replyMsg.kongType = mahjongHouse.GetRecordMahjongKongType(kongType, bNoInKong);
                    mahjongHouse.bKongInChange = kongIntegral > 0 ? true : false;
                    List <int> meldMahjongList = main.GetMahjongNode(meld.m_meldTileList);
                    //保存操作
                    OnRequestSavePlayerKongSelf(mahjongHouse.houseId, mahjongHouse.currentBureau, player.index, replyMsg.kongType, meld.m_eMeldType, meldMahjongList);
                    //发消息
                    OnRecvOperatMahjong(mahjongHouse, player, replyMsg, meld.m_eMeldType, operatType, kongType, meldMahjongList, kongIntegral);
                }
            }
            else if (operatType == MahjongOperatType.EMO_None)
            {
                //告诉玩家操作成功
                replyMsg.result = ResultCode.OK;
                replyMsg.operatType = operatType;
                replyMsg.currentShowCard = mahjongHouse.currentShowCard;
                replyMsg.housePlayerStatus = player.housePlayerStatus;
                replyMsg.bGiveUpWin = player.m_bGiveUpWin;
                replyMsg.bFakeHu = mahjongHouse.bFakeHu;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            else
            {
                //自己操作类型错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqOperatMahjongByMyself, 自己操作类型错误! userId = " + player.userId + ", operatType = " + operatType);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
        }
        public void OnRecvOperatMahjong(MahjongHouse mahjongHouse, MahjongPlayer player, ReplyOperatMahjong_L2P replyMsg, MeldType meldType, MahjongOperatType operatType, MahjongKongType kongType, List<int> mahjongNode, int kongIntegral = 0)
        {
            replyMsg.result = ResultCode.OK;
            replyMsg.summonerId = player.summonerId;
            replyMsg.meldType = meldType;
            replyMsg.operatType = operatType;
            if (mahjongHouse.currentShowCard == -2)
            {
                replyMsg.currentShowCard = mahjongHouse.currentWhoPlay;
            }
            else
            {
                replyMsg.currentShowCard = mahjongHouse.currentShowCard;
            }
            replyMsg.housePlayerStatus = player.housePlayerStatus;
            replyMsg.bGiveUpWin = player.m_bGiveUpWin;
            replyMsg.bFakeHu = mahjongHouse.bFakeHu;
            replyMsg.mahjongNode.AddRange(mahjongNode);
            SendProxyMsg(replyMsg, player.proxyServerId);

            int allKongIntegral = 0;
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != player.userId)
                {
                    OnRecvOperatMahjong(housePlayer.summonerId, housePlayer.proxyServerId, replyMsg.currentShowCard, housePlayer.housePlayerStatus, housePlayer.m_bGiveUpWin, replyMsg.bOperatMySelf,
                        replyMsg.bOperatHand, replyMsg.bFakeHu, meldType, operatType, replyMsg.kongType, mahjongNode, mahjongHouse.CheckPlayerKongWin(housePlayer.index));
                    if (kongIntegral > 0)
                    {
                        housePlayer.allIntegral -= kongIntegral;
                        mahjongHouse.SetPlayerBureauKongIntegral(housePlayer.index, -kongIntegral);
                        allKongIntegral += kongIntegral;
                    }
                }
            });
            if (allKongIntegral > 0)
            {
                player.allIntegral += allKongIntegral;
                mahjongHouse.SetPlayerBureauKongIntegral(player.index, allKongIntegral);
            }
            //设置房间操作时间
            mahjongHouse.SetHouseOperateBeginTime();
            if (operatType == MahjongOperatType.EMO_Kong)
            {
                DisMahjongByKong(mahjongHouse, player, kongType);
            }
        }
        public void OnRecvOperatMahjong(ulong summonerId, int proxyServerId, int currentShowCard, MahjongPlayerStatus housePlayerStatus, bool bGiveUpWin, bool bOperatMySelf, bool bOperatHand, bool bFakeHu, MeldType meldType, MahjongOperatType operatType, MahjongKongType kongType, List<int> mahjongNode, bool bNeedKongWin)
        {
            RecvOperatMahjong_L2P recvMsg_L2P = new RecvOperatMahjong_L2P();
            recvMsg_L2P.summonerId = summonerId;
            recvMsg_L2P.meldType = meldType;
            recvMsg_L2P.operatType = operatType;
            recvMsg_L2P.kongType = kongType;
            recvMsg_L2P.mahjongNode.AddRange(mahjongNode);
            recvMsg_L2P.currentShowCard = currentShowCard;
            recvMsg_L2P.housePlayerStatus = housePlayerStatus;
            recvMsg_L2P.bNeedKongWin = bNeedKongWin;
            recvMsg_L2P.bGiveUpWin = bGiveUpWin;
            recvMsg_L2P.bFakeHu = bFakeHu;
            recvMsg_L2P.bOperatMySelf = bOperatMySelf;
            recvMsg_L2P.bOperatHand = bOperatHand;
            SendProxyMsg(recvMsg_L2P, proxyServerId);
        }
        public void OnRecvWinMahjong(MahjongHouse mahjongHouse, WinMahjongType winType, MahjongPlayer player, List<int> winMahjongList)
        {
            List<PlayerWinMahjongNode> winPlayerList = new List<PlayerWinMahjongNode>();
            PlayerWinMahjongNode playerTileNode = new PlayerWinMahjongNode();
            playerTileNode.playerIndex = player.index;
            playerTileNode.winMahjongList.AddRange(winMahjongList);
            player.GetPlayerHandTileList(playerTileNode.handMahjongList);
            winPlayerList.Add(playerTileNode);
            bool bFrontClear = false;
            if (mahjongHouse.mahjongType == MahjongType.RedLaiZiMahjong && !player.CheckPlayerRedMahjong())
            {
                //门前清
                bFrontClear = true;
            }
            OnRecvWinMahjong(mahjongHouse, winType, winPlayerList, player.index, bFrontClear);
        }
        public void OnRecvWinMahjong(MahjongHouse mahjongHouse, WinMahjongType winType, List<PlayerWinMahjongNode> winPlayerList, int mahjongPlayerIndex, bool bFrontClear = false)
        {
            //鸟显示的位置
            int showBirdIndex = main.GetShowBirdIndex(mahjongHouse.currentWhoPlay, winPlayerList);
            //抓鸟
            List<MahjongTile> mahjongTileBirdList = GetNewTileByCatchBird(mahjongHouse, bFrontClear);
            main.DisPlayerWinBirdNumber(mahjongHouse, mahjongTileBirdList, showBirdIndex);
            //统计胡牌
            MahjongWinRecordNode mahjongWinRecord = new MahjongWinRecordNode();
            mahjongWinRecord.mahjongPlayerIndex = mahjongPlayerIndex;
            mahjongWinRecord.showBirdIndex = showBirdIndex;
            mahjongWinRecord.mahjongBirdList.AddRange(main.GetMahjongNode(mahjongTileBirdList));
            //特殊胡牌类型
            SpecialWinMahjongType specialWinMahjongType = main.GetSpecialWinMahjongType(mahjongHouse);
            mahjongWinRecord.specialWinMahjongType = (int)specialWinMahjongType;
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                OnRecvWinMahjong(housePlayer.summonerId, housePlayer.proxyServerId, housePlayer.index, winType, specialWinMahjongType, winPlayerList, showBirdIndex, mahjongWinRecord.mahjongBirdList, mahjongPlayerIndex);
                PlayerWinMahjongNode playerWinMahjong = winPlayerList.Find(element => element.playerIndex == housePlayer.index);
                if (playerWinMahjong != null)
                {
                    //统计胡牌
                    PlayerTileNode playerTile = new PlayerTileNode();
                    playerTile.playerIndex = playerWinMahjong.playerIndex;
                    playerTile.tileList.AddRange(playerWinMahjong.winMahjongList);
                    mahjongWinRecord.winPlayerList.Add(playerTile);
                    //标记胡牌玩家
                    housePlayer.housePlayerStatus = MahjongPlayerStatus.MahjongWinCard;
                }
            });
            //保存胡牌
            OnRequestSaveMahjongWinInfo(mahjongHouse.houseId, mahjongHouse.currentBureau, mahjongWinRecord);
            //结算
            SetMahjongHouseSettlement(mahjongHouse);
        }
        public void OnRecvWinMahjong(ulong summonerId, int proxyServerId, int playerIndex, WinMahjongType winType, SpecialWinMahjongType specialWinMahjongType, List<PlayerWinMahjongNode> winPlayerList, int showBirdIndex, List<int> mahjongBirdList, int mahjongPlayerIndex)
        {
            RecvPlayerWinMahjong_L2P recvMsg_L2P = new RecvPlayerWinMahjong_L2P();
            recvMsg_L2P.summonerId = summonerId;
            recvMsg_L2P.winType = winType;
            recvMsg_L2P.specialWinMahjongType = specialWinMahjongType;
            foreach (PlayerWinMahjongNode tileNode in winPlayerList)
            {
                if (tileNode.playerIndex != playerIndex)
                {
                    recvMsg_L2P.winPlayerList.Add(tileNode);
                }
                else
                {
                    PlayerWinMahjongNode playerWinMahjong = new PlayerWinMahjongNode();
                    playerWinMahjong.playerIndex = playerIndex;
                    playerWinMahjong.winMahjongList.AddRange(tileNode.winMahjongList);
                    recvMsg_L2P.winPlayerList.Add(playerWinMahjong);
                }
            }
            recvMsg_L2P.showBirdIndex = showBirdIndex;
            recvMsg_L2P.mahjongBirdList.AddRange(mahjongBirdList);
            recvMsg_L2P.mahjongPlayerIndex = mahjongPlayerIndex;
            SendProxyMsg(recvMsg_L2P, proxyServerId);
        }
        public void DisMahjongByKong(MahjongHouse mahjongHouse, MahjongPlayer player, MahjongKongType kongType)
        {
            if (mahjongHouse == null || player == null || kongType == MahjongKongType.EMK_None)
            {
                ServerUtil.RecordLog(LogType.Debug, "DisMahjongByKong, 数据有误! kongType = " + kongType);
                return;
            }
            if (mahjongHouse.kongHuPlayerList.Count > 0 && (mahjongHouse.mahjongSpecialType == MahjongSpecialType.EMS_Kong || mahjongHouse.mahjongSpecialType == MahjongSpecialType.EMS_MakeUp))
            {
                //可以抢杠胡
                ServerUtil.RecordLog(LogType.Debug, "DisMahjongByKong, 可以抢杠胡!");
                return;
            }
            if (kongType == MahjongKongType.EMK_MakeUp)
            {
                GiveOffMahjongByMakeUp(mahjongHouse, player);
            }
            else if (kongType == MahjongKongType.EMK_Kong)
            {
                GiveOffMahjongByKong(mahjongHouse, player);
            }
        }
        public void GiveOffMahjongByMakeUp(MahjongHouse mahjongHouse, MahjongPlayer player)
        {
            //给玩家发牌 并告诉其他玩家
            MahjongTile newTile = GetNewTileByRemainMahjong(mahjongHouse);
            if (newTile != null && player != null)
            {
                List<PlayerIntegral> playerIntegralList = new List<PlayerIntegral>();
                if (mahjongHouse.bKongInChange && (mahjongHouse.mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongHouse.mahjongType == MahjongType.RedLaiZiMahjong))
                {
                    playerIntegralList.Add(new PlayerIntegral { playerIndex = player.index, integral = player.allIntegral });
                    mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                    {
                        if (housePlayer.userId != player.userId)
                        {
                            playerIntegralList.Add(new PlayerIntegral { playerIndex = housePlayer.index, integral = housePlayer.allIntegral });
                        }
                    });
                }
                //保存手牌
                player.AddNewHeadMahjong(newTile);
                //保存发牌
                OnRequestSavePlayerNewHandMahjong(mahjongHouse.houseId, mahjongHouse.currentBureau, player.index, newTile);
                //下发消息
                OnRecvGiveOffMahjong(player.summonerId, player.proxyServerId, mahjongHouse.currentShowCard, player.housePlayerStatus, newTile, playerIntegralList);
                mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                {
                    if (housePlayer.userId != player.userId)
                    {
                        OnRecvGiveOffMahjong(housePlayer.summonerId, housePlayer.proxyServerId, mahjongHouse.currentShowCard, housePlayer.housePlayerStatus, null, playerIntegralList);
                    }
                });
            }
        }
        public void GiveOffMahjongByKong(MahjongHouse mahjongHouse, MahjongPlayer player)
        {
            //给玩家发牌 并告诉其他玩家
            List<MahjongTile> newMahjongTileList = GetNewTileByKongMahjong(mahjongHouse);
            if (newMahjongTileList != null && newMahjongTileList.Count != 0 && player != null)
            {
                mahjongHouse.currentWhoPlay = player.index;
                mahjongHouse.currentMahjongList.Clear();
                mahjongHouse.currentMahjongList.AddRange(newMahjongTileList);
                mahjongHouse.SetFakeHu();
                player.m_bReadyHand = true;
                player.m_bGiveUpWin = false;
                //玩家能胡的牌
                List<int> huMahjongList = new List<int>();
                foreach (MahjongTile mahjongTile in mahjongHouse.currentMahjongList)
                {
                    if (CSSpecialWinType.WT_None != player.WinHandTileCheck(mahjongTile, mahjongHouse.bFakeHu))
                    {
                        //自己能胡
                        huMahjongList.Add(mahjongTile.GetMahjongNode());
                    }
                }
                List<int> playerIndexList = new List<int>();
                if (huMahjongList.Count == 0)
                {
                    //其他人要不要
                    DiposeKongMahjong(player.index, mahjongHouse, mahjongHouse.currentMahjongList, playerIndexList);
                }

                //不管要不要都要发出去
                OnRecvKongMahjong(mahjongHouse, player.index, playerIndexList);

                //保存杠
                OnRequestSaveMahjongKong(mahjongHouse.houseId, mahjongHouse.currentBureau, player.index, mahjongHouse.currentMahjongList);

                if (huMahjongList.Count == 0 && playerIndexList.Count == 0)
                {
                    //不是杠了
                    mahjongHouse.mahjongSpecialType = MahjongSpecialType.EMS_None;
                    mahjongHouse.SetFakeHu(false);
                    //出牌没人要 增加出牌列表
                    player.AddShowMahjong(mahjongHouse.currentMahjongList);
                    //发牌
                    GiveOffMahjong(mahjongHouse, player.index);
                }
                else if (playerIndexList.Count > 0)
                {
                    mahjongHouse.currentShowCard = -1;
                }
                else if (huMahjongList.Count > 0)
                {
                    //杠胡
                    OnRecvWinMahjong(mahjongHouse, WinMahjongType.EWM_SelfWin, player, huMahjongList);
                }
            }
        }
        public void DiposeKongMahjong(int playerIndex, MahjongHouse mahjongHouse, List<MahjongTile> mahjongTileList, List<int> playerIndexList)
        {
            int remainMahjongCount = mahjongHouse.GetRemainMahjongCount();
            foreach (MahjongTile mahjongTile in mahjongTileList)
            {
                mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
                {
                    bool bIsNeed = false;
                    if (housePlayer.index != playerIndex)
                    {
                        //其他玩家判断
                        if (mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_EatHu) && !housePlayer.m_bGiveUpWin)
                        {
                            //可以吃胡
                            if (CSSpecialWinType.WT_None != housePlayer.WinHandTileCheck(mahjongTile, mahjongHouse.bFakeHu))
                            {
                                mahjongHouse.AddMahjongOperat(housePlayer.index, MahjongOperatType.EMO_Hu);
                                bIsNeed = true;
                            }
                        }
                        //杠碰
                        if (!bIsNeed && remainMahjongCount > 0)
                        {
                            MeldType meldType = housePlayer.PongKongCheck(mahjongTile);
                            if (meldType == MeldType.EM_ExposedKong || meldType == MeldType.EM_Triplet)
                            {
                                if (meldType == MeldType.EM_ExposedKong && !(housePlayer.m_bReadyHand && !housePlayer.KongCheckByCSMahjong(mahjongTile, 3)))
                                {
                                    if (remainMahjongCount > mahjongHouse.GetHouseSelectSeabedCount() || mahjongHouse.mahjongType != MahjongType.ChangShaMahjong)
                                    {
                                        //听牌后,杠完不能再听牌(长沙麻将必须留海底)
                                        mahjongHouse.AddMahjongOperat(housePlayer.index, MahjongOperatType.EMO_Kong);
                                        bIsNeed = true;
                                    }
                                }
                                else if (meldType == MeldType.EM_Triplet && !housePlayer.m_bReadyHand)
                                {
                                    mahjongHouse.AddMahjongOperat(housePlayer.index, MahjongOperatType.EMO_Pong);
                                    bIsNeed = true;
                                }
                            }
                        }
                        if (!bIsNeed && remainMahjongCount > 0 && housePlayer.index == mahjongHouse.GetNextHousePlayerIndex(playerIndex) && !housePlayer.m_bReadyHand)
                        {
                            //下家没听牌才能吃
                            if (housePlayer.ChowCheck(mahjongTile))
                            {
                                mahjongHouse.AddMahjongOperat(housePlayer.index, MahjongOperatType.EMO_Chow);
                                bIsNeed = true;
                            }
                        }
                    }
                    else
                    {
                        //自己只判断杠
                        if (null != housePlayer.ExposedKongCheck(mahjongTile))
                        {
                            //这个判断碰牌是否可以杠, 不会消耗手牌
                            bIsNeed = true;
                        }
                        else if (MeldType.EM_ExposedKong == housePlayer.PongKongCheck(mahjongTile) && !(housePlayer.m_bReadyHand && !housePlayer.KongCheckByCSMahjong(mahjongTile, 3)))
                        {
                            //听牌后,杠完不能再听牌
                            bIsNeed = true;
                        }
                        if (bIsNeed && (remainMahjongCount > mahjongHouse.GetHouseSelectSeabedCount() || mahjongHouse.mahjongType != MahjongType.ChangShaMahjong))
                        {
                            mahjongHouse.AddMahjongOperat(housePlayer.index, MahjongOperatType.EMO_Kong);
                        }
                    }
                    if (bIsNeed && !playerIndexList.Contains(housePlayer.index))
                    {
                        playerIndexList.Add(housePlayer.index);
                    }
                });
            }
        }
        public void OnRecvKongMahjong(MahjongHouse mahjongHouse, int playerIndex, List<int> playerIndexList)
        {
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                RecvKongMahjong_L2P recvMsg_L2P = new RecvKongMahjong_L2P();
                recvMsg_L2P.summonerId = housePlayer.summonerId;
                recvMsg_L2P.plyerIndex = playerIndex;
                recvMsg_L2P.bFakeHu = mahjongHouse.bFakeHu;
                recvMsg_L2P.bNeed = playerIndexList.Exists(element => element == housePlayer.index);
                mahjongHouse.GetCurrentMahjongList(recvMsg_L2P.kongMahjongList);
                SendProxyMsg(recvMsg_L2P, housePlayer.proxyServerId);
            });
        }
        public void OnReqPlayerSelectSeabed(int peerId, bool inbound, object msg)
        {
            RequestPlayerSelectSeabed_P2L reqMsg = msg as RequestPlayerSelectSeabed_P2L;
            ReplyPlayerSelectSeabed_L2P replyMsg = new ReplyPlayerSelectSeabed_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerSelectSeabed, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerSelectSeabed, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || mahjongHouse.houseStatus != MahjongHouseStatus.MHS_SelectSeabed)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerSelectSeabed, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (mahjongHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                //房间正在投票中
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerSelectSeabed, 房间正在投票中! userId = " + sender.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.HouseGoingDissolveVote;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerSelectSeabed, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqPlayerSelectSeabed(mahjongHouse, player, reqMsg.bNeed, replyMsg);
        }
        public void OnReqPlayerSelectSeabed(MahjongHouse mahjongHouse, MahjongPlayer player, bool bNeed = false, ReplyPlayerSelectSeabed_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyPlayerSelectSeabed_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            if (mahjongHouse.GetHouseSelectSeabedCount() != mahjongHouse.GetRemainMahjongCount())
            {
                //最后剩下的底牌不能选牌
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerSelectSeabed, 最后剩下的底牌不能选牌! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }
            if (player.index != mahjongHouse.currentShowCard)
            {
                //不是该玩家选择
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerSelectSeabed, 不是该玩家选择! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, player.proxyServerId);
                return;
            }

            if (bNeed)
            {
                //要
                List<MahjongTile> seabedMahjongTileList = GetNewTileBySeabedMahjong(mahjongHouse);
                if (seabedMahjongTileList != null && seabedMahjongTileList.Count != 0 && player != null)
                {
                    mahjongHouse.currentWhoPlay = player.index;
                    mahjongHouse.currentMahjongList.Clear();
                    mahjongHouse.currentMahjongList.AddRange(seabedMahjongTileList);
                    //海底可以假将胡
                    mahjongHouse.SetFakeHu();
                    player.m_bGiveUpWin = false;

                    //玩家能胡的牌
                    List<int> huMahjongList = new List<int>();
                    foreach (MahjongTile seabedMahjongTile in mahjongHouse.currentMahjongList)
                    {
                        if (CSSpecialWinType.WT_None != player.WinHandTileCheck(seabedMahjongTile, mahjongHouse.bFakeHu))
                        {
                            //自己能胡
                            huMahjongList.Add(seabedMahjongTile.GetMahjongNode());
                        }
                    }
                    List<int> playerIndexList = new List<int>();
                    if (huMahjongList.Count == 0)
                    {
                        //其他人要不要
                        DiposeSeabedMahjong(player.userId, player.index, mahjongHouse, mahjongHouse.currentMahjongList, playerIndexList);
                    }

                    //不管要不要都要发出去
                    replyMsg.result = ResultCode.OK;
                    replyMsg.bNeed = bNeed;
                    replyMsg.bFakeHu = mahjongHouse.bFakeHu;
                    replyMsg.seabedMahjongList.AddRange(main.GetMahjongNode(mahjongHouse.currentMahjongList));
                    SendProxyMsg(replyMsg, player.proxyServerId);

                    OnRecvPlayerSelectSeabed(mahjongHouse, player.userId, player.index, playerIndexList, replyMsg.seabedMahjongList);

                    //保存海底
                    OnRequestSavePlayerNeedSeabed(mahjongHouse.houseId, mahjongHouse.currentBureau, player.index, replyMsg.seabedMahjongList);

                    if (huMahjongList.Count == 0 && playerIndexList.Count == 0)
                    {
                        //出牌没人要 增加出牌列表
                        player.AddShowMahjong(mahjongHouse.currentMahjongList);
                        //取消假将胡
                        mahjongHouse.SetFakeHu(false);
                        //流局
                        SetMahjongHouseSettlement(mahjongHouse);
                    }
                    else if (playerIndexList.Count > 0)
                    {
                        mahjongHouse.currentShowCard = -1;
                        mahjongHouse.houseStatus = MahjongHouseStatus.MHS_BeginBureau;
                        //设置房间操作时间
                        mahjongHouse.SetHouseOperateBeginTime();
                    }
                    else if (huMahjongList.Count > 0)
                    {
                        //海底胡
                        OnRecvWinMahjong(mahjongHouse, WinMahjongType.EWM_SelfWin, player, huMahjongList);
                    }
                }
            }
            else
            {
                //不要
                int nextPlayerIndex = mahjongHouse.GetNextHousePlayerIndex(mahjongHouse.currentShowCard);
                if (nextPlayerIndex != mahjongHouse.currentWhoPlay)
                {
                    //下一个玩家选择
                    mahjongHouse.currentShowCard = nextPlayerIndex;
                    //设置房间操作时间
                    mahjongHouse.SetHouseOperateBeginTime();
                    OnRecvSelectSeabedMahjong(mahjongHouse);                    
                }
                else
                {
                    //都不要, 把最后海底牌给大家看
                    OnRecvPlayerSelectSeabed(mahjongHouse);
                    //流局
                    SetMahjongHouseSettlement(mahjongHouse);
                }
            }
        }
        public void OnRecvPlayerSelectSeabed(MahjongHouse mahjongHouse, string userId, int playerIndex, List<int> playerIndexList, List<int> seabedMahjongList)
        {
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if(housePlayer.userId != userId)
                {
                    RecvPlayerSelectSeabed_L2P recvMsg_L2P = new RecvPlayerSelectSeabed_L2P();
                    recvMsg_L2P.summonerId = housePlayer.summonerId;
                    recvMsg_L2P.playerIndex = playerIndex;
                    recvMsg_L2P.bFakeHu = mahjongHouse.bFakeHu;
                    recvMsg_L2P.bNeedHu = playerIndexList.Exists(element => element == housePlayer.index);
                    recvMsg_L2P.seabedMahjongList.AddRange(seabedMahjongList);
                    SendProxyMsg(recvMsg_L2P, housePlayer.proxyServerId);
                }
            });
        }
        public void OnRecvPlayerSelectSeabed(MahjongHouse mahjongHouse)
        {
            List<MahjongTile> seabedMahjongTileList = GetNewTileBySeabedMahjong(mahjongHouse);
            if (seabedMahjongTileList == null || seabedMahjongTileList.Count == 0)
            {
                //获取海底牌出错
                ServerUtil.RecordLog(LogType.Error, "OnRecvPlayerSelectSeabed, 获取海底牌出错! houseId = " + mahjongHouse.houseId);
                return;
            }
            List<int> seabedMahjongList = main.GetMahjongNode(seabedMahjongTileList);
            //保存海底
            OnRequestSavePlayerNeedSeabed(mahjongHouse.houseId, mahjongHouse.currentBureau, -1, seabedMahjongList);
            //下发
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                RecvPlayerSelectSeabed_L2P recvMsg_L2P = new RecvPlayerSelectSeabed_L2P();
                recvMsg_L2P.summonerId = housePlayer.summonerId;
                recvMsg_L2P.playerIndex = housePlayer.index;
                recvMsg_L2P.bNeedHu = false;
                recvMsg_L2P.seabedMahjongList.AddRange(seabedMahjongList);
                SendProxyMsg(recvMsg_L2P, housePlayer.proxyServerId);
            });
        }
        public void DiposeSeabedMahjong(string userId, int playerIndex, MahjongHouse mahjongHouse, List<MahjongTile> seabedMahjongTileList, List<int> playerIndexList)
        {
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != userId && !housePlayer.m_bGiveUpWin && mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_EatHu))
                {
                    //可以吃胡
                    seabedMahjongTileList.ForEach(mahjongTile =>
                    {
                        if (CSSpecialWinType.WT_None != housePlayer.WinHandTileCheck(mahjongTile, mahjongHouse.bFakeHu))
                        {
                            mahjongHouse.AddMahjongOperat(housePlayer.index, MahjongOperatType.EMO_Hu);
                            if (!playerIndexList.Contains(housePlayer.index))
                            {
                                playerIndexList.Add(housePlayer.index);
                            }
                        }
                    });
                }
            });
        }
        public void OnRecvPlayerHouseCard(ulong summonerId, int proxyServerId, int houseCard)
        {
            ModuleManager.Get<MainCityMain>().msg_proxy.OnRecvPlayerHouseCard(summonerId, proxyServerId, houseCard);
        }
        public MahjongTile GetNewTileByRemainMahjong(MahjongHouse mahjongHouse)
        {
            if (0 == mahjongHouse.GetRemainMahjongCount())
            {
                //流局 
                SetMahjongHouseSettlement(mahjongHouse);
                return null;
            }
            return mahjongHouse.GetNewTileByRemainMahjong();
        }
        public List<MahjongTile> GetNewTileByKongMahjong(MahjongHouse mahjongHouse)
        {
            List<MahjongTile> newMahjongTileList = new List<MahjongTile>();
            int remainMahjongCount = mahjongHouse.GetRemainMahjongCount();
            int kongCount = mahjongHouse.GetHouseKongCount();
            int seabedCount = mahjongHouse.GetHouseSelectSeabedCount();
            if (remainMahjongCount <= seabedCount)
            {
                //流局 
                SetMahjongHouseSettlement(mahjongHouse);
                return null;
            }
            else if (remainMahjongCount >= (kongCount + seabedCount))
            {
                for (int i = 0; i < kongCount; ++i)
                {
                    newMahjongTileList.Add(mahjongHouse.GetNewTileByRemainMahjong());
                }
            }
            else
            {
                for (int i = 0; i < remainMahjongCount - seabedCount; ++i)
                {
                    newMahjongTileList.Add(mahjongHouse.GetNewTileByRemainMahjong());
                }
            }
            return newMahjongTileList;
        }
        public List<MahjongTile> GetNewTileBySeabedMahjong(MahjongHouse mahjongHouse)
        {
            List<MahjongTile> newMahjongTileList = new List<MahjongTile>();
            if (mahjongHouse.GetRemainMahjongCount() != mahjongHouse.GetHouseSelectSeabedCount())
            {
                //流局 
                SetMahjongHouseSettlement(mahjongHouse);
                return null;
            }
            //所有牌就是海底
            newMahjongTileList.AddRange(mahjongHouse.GetRemainMahjong());
            mahjongHouse.InitRemainMahjong();
            return newMahjongTileList;
        }
        public List<MahjongTile> GetNewTileByCatchBird(MahjongHouse mahjongHouse, bool bFrontClear)
        {
            List<MahjongTile> newMahjongTileList = new List<MahjongTile>();
            if (mahjongHouse.catchBird > 0)
            {
                int catchBird = mahjongHouse.catchBird;
                if (bFrontClear)
                {
                    catchBird += 2;
                }
                int remainMahjongCount = mahjongHouse.GetRemainMahjongCount();
                if (0 == remainMahjongCount)
                {
                    MahjongTile endTile = mahjongHouse.currentMahjongList.LastOrDefault();
                    if (endTile != null)
                    {
                        for (int i = 0; i < catchBird; ++i)
                        {
                            newMahjongTileList.Add(endTile);
                        }
                    }
                }
                else if (remainMahjongCount <= catchBird && remainMahjongCount >= 1)
                {
                    newMahjongTileList.AddRange(mahjongHouse.GetRemainMahjong());
                    mahjongHouse.InitRemainMahjong();
                }
                else
                {
                    for (int i = 0; i < catchBird; ++i)
                    {
                        newMahjongTileList.Add(mahjongHouse.GetNewTileByRemainMahjong());
                    }
                }
            }
            return newMahjongTileList;
        }
        public void SetMahjongHouseSettlement(MahjongHouse mahjongHouse)
        {
            mahjongHouse.houseStatus = MahjongHouseStatus.MHS_Settlement;
            //设置房间操作时间
            mahjongHouse.SetHouseOperateBeginTime();
            //保存结算状态
            OnRequestSaveMahjongHouseStatus(mahjongHouse.houseId, mahjongHouse.houseStatus);
            //结算
            SettlementMahjongHouse(mahjongHouse);
        }
        public void OnReqMahjongMidwayPendulum(int peerId, bool inbound, object msg)
        {
            RequestMahjongMidwayPendulum_P2L reqMsg = msg as RequestMahjongMidwayPendulum_P2L;
            ReplyMahjongMidwayPendulum_L2P replyMsg = new ReplyMahjongMidwayPendulum_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulum, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (reqMsg.mahjongNode == 0)
            {
                //摆牌麻将错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulum, 摆牌麻将错误! userId = " + sender.userId + ", mahjongNode = " + reqMsg.mahjongNode);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongTile mahjongTile = new MahjongTile(reqMsg.mahjongNode);
            if (mahjongTile == null || mahjongTile.GetNumType() == TileNumType.ETN_None)
            {
                //摆牌麻将错误
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulum, 摆牌麻将错误! userId = " + sender.userId + ", mahjongNode = " + reqMsg.mahjongNode);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulum, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || mahjongHouse.houseStatus != MahjongHouseStatus.MHS_BeginBureau)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulum, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (!mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_PersonalisePendulum))
            {
                //个性摆牌玩法未开启
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulum, 个性摆牌玩法未开启! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulum, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (player.m_bGiveUpWin || player.housePlayerStatus != MahjongPlayerStatus.MahjongWaitCard || player.index != mahjongHouse.currentShowCard)
            {
                //玩家不是出牌状态
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulum, 玩家不是出牌状态! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (player.PongKongCheck(mahjongTile) != MeldType.EM_ConcealedKong)
            {
                //玩家摆牌麻将不够
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulum, 玩家摆牌麻将不够! userId = " + player.userId + ", mahjongTile = " + mahjongTile);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (player.CheckFourTile(mahjongTile))
            {
                //玩家摆牌类型不匹配
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulum, 玩家这个麻将已经摆牌过! userId = " + player.userId + ", mahjongTile = " + mahjongTile);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            List<int> mahjongList = new List<int>();
            mahjongList.Add(reqMsg.mahjongNode);
            mahjongList.Add(reqMsg.mahjongNode);
            mahjongList.Add(reqMsg.mahjongNode);
            mahjongList.Add(reqMsg.mahjongNode);
            //告诉其他人
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != player.userId)
                {
                    OnRecvMahjongMidwayPendulum(housePlayer.summonerId, housePlayer.proxyServerId, player.index, mahjongList);
                }
            });
            //摆牌次数
            mahjongHouse.SetPlayerBureauMidwayPendulum(player.index);
            //掷骰子状态
            player.housePlayerStatus = MahjongPlayerStatus.MahjongPendulumDice;
            //记录摆牌麻将
            player.displayFourTileList.Add(mahjongTile);
            //设置房间操作时间
            mahjongHouse.SetHouseOperateBeginTime();

            replyMsg.result = ResultCode.OK;
            replyMsg.mahjongList.AddRange(mahjongList);
            SendProxyMsg(replyMsg, sender.proxyServerId);

            //保存摆牌
            OnRequestSaveMahjongPendulum(mahjongHouse.houseId, mahjongHouse.currentBureau, player.index, (int)CSStartDisplayType.SDT_MidwayConcealedKong, mahjongList);
        }
        public void OnRecvMahjongMidwayPendulum(ulong summonerId, int proxyServerId, int index, List<int> mahjongList)
        {
            RecvMahjongMidwayPendulum_L2P recvMsg = new RecvMahjongMidwayPendulum_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.index = index;
            recvMsg.mahjongList.AddRange(mahjongList);
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnReqMahjongMidwayPendulumDice(int peerId, bool inbound, object msg)
        {
            RequestMahjongMidwayPendulumDice_P2L reqMsg = msg as RequestMahjongMidwayPendulumDice_P2L;
            ReplyMahjongMidwayPendulumDice_L2P replyMsg = new ReplyMahjongMidwayPendulumDice_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulumDice, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null || house.houseType != HouseType.MahjongHouse)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulumDice, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null || mahjongHouse.houseStatus != MahjongHouseStatus.MHS_BeginBureau)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulumDice, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (!mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_PersonalisePendulum))
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulumDice, 个性摆牌玩法未开启! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            MahjongPlayer player = mahjongHouse.GetMahjongPlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulumDice, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (player.housePlayerStatus != MahjongPlayerStatus.MahjongPendulumDice || player.index != mahjongHouse.currentShowCard)
            {
                //玩家不是摆牌状态
                ServerUtil.RecordLog(LogType.Debug, "OnReqMahjongMidwayPendulumDice, 玩家不是摆牌掷骰子状态! userId = " + player.userId + ", houseId = " + mahjongHouse.houseId);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            OnReqMahjongMidwayPendulumDice(mahjongHouse, player, replyMsg);
        }
        public void OnReqMahjongMidwayPendulumDice(MahjongHouse mahjongHouse, MahjongPlayer player, ReplyMahjongMidwayPendulumDice_L2P replyMsg = null)
        {
            if (replyMsg == null)
            {
                replyMsg = new ReplyMahjongMidwayPendulumDice_L2P();
                replyMsg.summonerId = player.summonerId;
            }
            //获取庄的位置
            int zhuangPlayerIndex = player.index;
            if (!mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_HuZhuang) && player.zhuangLeisureType != ZhuangLeisureType.Zhuang)
            {
                //不是胡牌为庄且自己不是庄，再找现庄的位置
                zhuangPlayerIndex = mahjongHouse.GetHouseZhuangIndex();
            }
            //随机骰子
            int leftDice = LegendProtocol.MyRandom.NextPrecise(1, 7);
            int rightDice = LegendProtocol.MyRandom.NextPrecise(1, 7);
            //算鸟
            int[] birdNumberArray = new int[] { 0, 0, 0, 0 };
            int remainder = leftDice % mahjongHouse.maxPlayerNum;
            birdNumberArray[remainder] += 1;
            remainder = rightDice % mahjongHouse.maxPlayerNum;
            birdNumberArray[remainder] += 1;
            //计算中鸟
            int[] playerBirdArray = new int[] { 0, 0, 0, 0 };
            int playerIndex = mahjongHouse.GetLastHousePlayerIndex(zhuangPlayerIndex);
            for (int i = 0; i < mahjongHouse.GetHousePlayerCount(); ++i)
            {
                playerBirdArray[playerIndex] += birdNumberArray[i];
                playerIndex = mahjongHouse.GetNextHousePlayerIndex(playerIndex);
            }
            int startDisplyWinIntegral = 0;
            //计算分
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.userId != player.userId)
                {
                    //基础分
                    int startDisplyLoseIntegral = main.startDisplayIntegral;
                    //算庄
                    if (mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_ZhuangLeisure) && (player.index == zhuangPlayerIndex || housePlayer.index == zhuangPlayerIndex))
                    {
                        startDisplyLoseIntegral += 1;
                    }
                    //算鸟
                    if (playerBirdArray[player.index] > 0 || playerBirdArray[housePlayer.index] > 0)
                    {
                        startDisplyLoseIntegral += mahjongHouse.GetWinBirdIntegral(startDisplyLoseIntegral, playerBirdArray[housePlayer.index], playerBirdArray[player.index]);
                    }
                    //算飘
                    if (mahjongHouse.flutter > 0)
                    {
                        startDisplyLoseIntegral += mahjongHouse.GetWinFlutterIntegral();
                    }
                    startDisplyWinIntegral += startDisplyLoseIntegral;
                    mahjongHouse.SetPlayerBureauStartDisplay(housePlayer.index, -startDisplyLoseIntegral);
                    housePlayer.allIntegral -= startDisplyLoseIntegral;
                    replyMsg.playerIntegralList.Add(new PlayerIntegral { playerIndex = housePlayer.index, integral = housePlayer.allIntegral });
                }
            });
            //积分
            mahjongHouse.SetPlayerBureauStartDisplay(player.index, startDisplyWinIntegral);
            player.allIntegral += startDisplyWinIntegral;
            replyMsg.playerIntegralList.Add(new PlayerIntegral { playerIndex = player.index, integral = player.allIntegral });
            //表示已经做出选择
            player.housePlayerStatus = MahjongPlayerStatus.MahjongWaitCard;

            //骰子
            replyMsg.pendulumDice = leftDice * 10 + rightDice;

            //下发消息
            OnRecvMahjongMidwayPendulumDice(mahjongHouse, player.index, replyMsg.pendulumDice, replyMsg.playerIntegralList);

            //设置房间操作时间
            mahjongHouse.SetHouseOperateBeginTime();

            replyMsg.result = ResultCode.OK;
            SendProxyMsg(replyMsg, player.proxyServerId);

            //保存摆牌掷骰子
            OnRequestSaveMahjongPendulumDice(mahjongHouse.houseId, mahjongHouse.currentBureau, player.index, replyMsg.pendulumDice);
        }
        public void OnRecvMahjongMidwayPendulumDice(MahjongHouse mahjongHouse, int playerIndex, int pendulumDice, List<PlayerIntegral> playerIntegralList)
        {
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.index != playerIndex)
                {
                    RecvMahjongMidwayPendulumDice_L2P recvMsg = new RecvMahjongMidwayPendulumDice_L2P();
                    recvMsg.summonerId = housePlayer.summonerId;
                    recvMsg.index = playerIndex;
                    recvMsg.pendulumDice = pendulumDice;
                    recvMsg.playerIntegralList.AddRange(playerIntegralList);
                    SendProxyMsg(recvMsg, housePlayer.proxyServerId);
                }
            });
        }
        public void OnReqMahjongOverallRecord(int peerId, bool inbound, object msg)
        {
            RequestMahjongOverallRecord_P2L reqMsg_P2L = msg as RequestMahjongOverallRecord_P2L;
            RequestMahjongOverallRecord_L2D reqMsg_L2D = new RequestMahjongOverallRecord_L2D();

            reqMsg_L2D.summonerId = reqMsg_P2L.summonerId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnReplyMahjongOverallRecord(int peerId, bool inbound, object msg)
        {
            ReplyMahjongOverallRecord_D2L replyMsg_D2L = msg as ReplyMahjongOverallRecord_D2L;
            ReplyMahjongOverallRecord_L2P replyMsg_L2P = new ReplyMahjongOverallRecord_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.summonerId);
            if (sender == null) return;

            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.summonerId = replyMsg_D2L.summonerId;
            replyMsg_L2P.overallRecord = replyMsg_D2L.overallRecord;
            SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
        }
        public void OnReqMahjongBureauRecord(int peerId, bool inbound, object msg)
        {
            RequestMahjongBureauRecord_P2L reqMsg_P2L = msg as RequestMahjongBureauRecord_P2L;
            RequestMahjongBureauRecord_L2D reqMsg_L2D = new RequestMahjongBureauRecord_L2D();
            
            reqMsg_L2D.summonerId = reqMsg_P2L.summonerId;
            reqMsg_L2D.onlyHouseId = reqMsg_P2L.onlyHouseId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnReplyMahjongBureauRecord(int peerId, bool inbound, object msg)
        {
            ReplyMahjongBureauRecord_D2L replyMsg_D2L = msg as ReplyMahjongBureauRecord_D2L;
            ReplyMahjongBureauRecord_L2P replyMsg_L2P = new ReplyMahjongBureauRecord_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.summonerId);
            if (sender == null) return;

            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.summonerId = replyMsg_D2L.summonerId;
            replyMsg_L2P.onlyHouseId = replyMsg_D2L.onlyHouseId;
            replyMsg_L2P.bureauRecord = replyMsg_D2L.bureauRecord;
            SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
        }
        public void OnReqMahjongBureauPlayback(int peerId, bool inbound, object msg)
        {
            RequestMahjongBureauPlayback_P2L reqMsg_P2L = msg as RequestMahjongBureauPlayback_P2L;
            RequestMahjongBureauPlayback_L2D reqMsg_L2D = new RequestMahjongBureauPlayback_L2D();
            
            reqMsg_L2D.summonerId = reqMsg_P2L.summonerId;
            reqMsg_L2D.onlyHouseId = reqMsg_P2L.onlyHouseId;
            reqMsg_L2D.bureau = reqMsg_P2L.bureau;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnReplyMahjongBureauPlayback(int peerId, bool inbound, object msg)
        {
            ReplyMahjongBureauPlayback_D2L replyMsg_D2L = msg as ReplyMahjongBureauPlayback_D2L;
            ReplyMahjongBureauPlayback_L2P replyMsg_L2P = new ReplyMahjongBureauPlayback_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.summonerId);
            if (sender == null) return;

            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.summonerId = replyMsg_D2L.summonerId;
            replyMsg_L2P.onlyHouseId = replyMsg_D2L.onlyHouseId;
            replyMsg_L2P.bureau = replyMsg_D2L.bureau;
            replyMsg_L2P.playerMahjong = replyMsg_D2L.playerMahjong;
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
        public void OnReplyMahjongHouseInfo(int peerId, bool inbound, object msg)
        {
            ReplyMahjongHouseInfo_D2L replyMsg = msg as ReplyMahjongHouseInfo_D2L;
            List<MahjongHouseNode> houseList = Serializer.tryUncompressObject<List<MahjongHouseNode>>(replyMsg.house);
            if (houseList != null && houseList.Count > 0)
            {
                ModuleManager.Get<DistributedMain>().SetLoadHouseCount(houseList.Count);
                houseList.ForEach(houseNode =>
                {
                    MahjongHouse mahjongHouse = new MahjongHouse();
                    if (!mahjongHouse.SetMahjongSetTile(houseNode.mahjongType))
                    {
                        mahjongHouse.houseId = houseNode.houseId;
                        mahjongHouse.houseCardId = houseNode.houseCardId;
                        mahjongHouse.logicId = houseNode.logicId;
                        mahjongHouse.currentBureau = houseNode.currentBureau;
                        mahjongHouse.maxBureau = houseNode.maxBureau;
                        mahjongHouse.maxPlayerNum = houseNode.maxPlayerNum;
                        mahjongHouse.businessId = houseNode.businessId;
                        mahjongHouse.catchBird = houseNode.catchBird;
                        mahjongHouse.flutter = houseNode.flutter;
                        mahjongHouse.housePropertyType = houseNode.housePropertyType;                        
                        mahjongHouse.houseType = houseNode.houseType;
                        mahjongHouse.mahjongType = houseNode.mahjongType;
                        mahjongHouse.houseStatus = houseNode.houseStatus;
                        mahjongHouse.createTime = Convert.ToDateTime(houseNode.createTime);

                        if (mahjongHouse.businessId > 0 && mahjongHouse.houseStatus == MahjongHouseStatus.MHS_Settlement)
                        {
                            mahjongHouse.operateBeginTime = DateTime.Now;
                        }
                        //保存房间
                        HouseManager.Instance.AddHouse(mahjongHouse.houseId, mahjongHouse);
                        //请求玩家信息和当局信息
                        OnRequestMahjongPlayerAndBureau(mahjongHouse.houseId);
                    }
                    else
                    {
                        //选择麻将发牌类有误
                        ServerUtil.RecordLog(LogType.Error, "OnReplyMahjongHouseInfo, 选择麻将发牌类有误! houseId = " + houseNode.houseId + ", mahjongType = " + houseNode.mahjongType);
                        ModuleManager.Get<DistributedMain>().DelLoadHouseCount();
                    }
                });
            }
            else
            {
                ModuleManager.Get<DistributedMain>().SetLoadHouseCount();
            }
        }
        public void OnReplyMahjongPlayerAndBureau(int peerId, bool inbound, object msg)
        {
            ReplyMahjongPlayerAndBureau_D2L replyMsg = msg as ReplyMahjongPlayerAndBureau_D2L;

            ModuleManager.Get<DistributedMain>().DelLoadHouseCount();

            House house = HouseManager.Instance.GetHouseById(replyMsg.houseId);
            if (house == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Error, "OnReplyMahjongPlayerAndBureau, 房间不存在! houseId = " + replyMsg.houseId); ;
                return;
            }
            MahjongHouse mahjongHouse = house as MahjongHouse;
            if (mahjongHouse == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Error, "OnReplyMahjongPlayerAndBureau, 房间不存在! houseId = " + replyMsg.houseId);
                return;
            }
            if (mahjongHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
            {
                main.AddDissolveVoteHouse(mahjongHouse.houseId);
            }
            MahjongPlayerBureauNode mahjongPlayerBureau = Serializer.tryUncompressObject<MahjongPlayerBureauNode>(replyMsg.housePlayerBureau);
            if (mahjongPlayerBureau != null && mahjongPlayerBureau.mahjongPlayerList != null && mahjongPlayerBureau.mahjongPlayerList.Count > 0)
            {
                //MahjongPlayerStatus housePlayerStatus = MahjongPlayerStatus.MahjongFree;
                //if (mahjongHouse.currentBureau == 0 && mahjongHouse.houseStatus == MahjongHouseStatus.MHS_FreeBureau)
                //{
                //    if (mahjongPlayerBureau.mahjongPlayerList.Count >= mahjongHouse.maxPlayerNum)
                //    {
                //        //第一局没打完
                //        HouseManager.Instance.RemoveHouse(mahjongHouse.houseId);
                //        //保存房间状态
                //        OnRequestSaveMahjongHouseStatus(mahjongHouse.houseId, MahjongHouseStatus.MHS_Dissolved);
                //        return;
                //    }
                //    if (mahjongHouse.businessId > 0)
                //    {
                //        //商家模式开服自动准备
                //        housePlayerStatus = MahjongPlayerStatus.MahjongReady;
                //    }
                //} 
                //没开始打牌或者第一局已经结算
                foreach (MahjongHousePlayerNode playerNode in mahjongPlayerBureau.mahjongPlayerList)
                {
                    if (mahjongHouse.GetMahjongPlayer().Exists(element => (element.userId == playerNode.userId || element.index == playerNode.playerIndex)))
                    {
                        ServerUtil.RecordLog(LogType.Error, "OnReplyMahjongPlayerAndBureau, 已经存在玩家信息! summonerId = " + playerNode.summonerId);
                        break;
                    }
                    MahjongStrategyBase strategy = MahjongManager.Instance.GetMahjongStrategy(mahjongHouse.mahjongType);
                    if (strategy == null)
                    {
                        ServerUtil.RecordLog(LogType.Error, "OnReplyMahjongPlayerAndBureau, 选择麻将逻辑类有误! mahjongType = " + mahjongHouse.mahjongType);
                        break;
                    }
                    mahjongHouse.AddPlayer(playerNode, strategy);
                    //mahjongHouse.AddPlayer(playerNode, housePlayerStatus);
                }
            }
            if (mahjongPlayerBureau != null && mahjongPlayerBureau.mahjongBureauList != null && mahjongPlayerBureau.mahjongBureauList.Count > 0)
            {
                foreach (MahjongHouseBureau bureauNode in mahjongPlayerBureau.mahjongBureauList)
                {
                    if (mahjongHouse.houseBureauList.Exists(element => element.bureau == bureauNode.bureau))
                    {
                        ServerUtil.RecordLog(LogType.Error, "OnReplyMahjongPlayerAndBureau, 已经存在当局信息! bureau = " + bureauNode.bureau);
                        break;
                    }
                    mahjongHouse.houseBureauList.Add(bureauNode);
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
            RequestMahjongHouseInfo_L2D reqMsg_L2D = new RequestMahjongHouseInfo_L2D();
            reqMsg_L2D.logicId = root.ServerID;                                                     
            SendDBMsg(reqMsg_L2D, dbServerID);
        }
        public void OnRequestMahjongPlayerAndBureau(ulong houseId)
        {
            RequestMahjongPlayerAndBureau_L2D reqMsg_L2D = new RequestMahjongPlayerAndBureau_L2D();
            reqMsg_L2D.houseId = houseId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveCreateMahjongInfo(MahjongHouse mahjongHouse, MahjongPlayer newHousePlayer)
        {
            RequestSaveCreateMahjongInfo_L2D reqMsg_L2D = new RequestSaveCreateMahjongInfo_L2D();
            reqMsg_L2D.houseId = mahjongHouse.houseId;
            reqMsg_L2D.houseCardId = mahjongHouse.houseCardId;
            reqMsg_L2D.logicId = mahjongHouse.logicId;
            reqMsg_L2D.maxBureau = mahjongHouse.maxBureau;
            reqMsg_L2D.maxPlayerNum = mahjongHouse.maxPlayerNum;
            reqMsg_L2D.businessId = mahjongHouse.businessId;
            reqMsg_L2D.catchBird = mahjongHouse.catchBird;
            reqMsg_L2D.flutter = mahjongHouse.flutter;
            reqMsg_L2D.housePropertyType = mahjongHouse.housePropertyType;
            reqMsg_L2D.houseType = mahjongHouse.houseType;
            reqMsg_L2D.mahjongType = mahjongHouse.mahjongType;
            reqMsg_L2D.createTime = mahjongHouse.createTime.ToString();
            reqMsg_L2D.summonerId = newHousePlayer.summonerId;
            reqMsg_L2D.index = newHousePlayer.index;
            reqMsg_L2D.allIntegral = newHousePlayer.allIntegral;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveMahjongNewPlayer(ulong houseId, MahjongPlayer newHousePlayer)
        {
            RequestSaveMahjongNewPlayer_L2D reqMsg_L2D = new RequestSaveMahjongNewPlayer_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.summonerId = newHousePlayer.summonerId;
            reqMsg_L2D.index = newHousePlayer.index;
            reqMsg_L2D.allIntegral = newHousePlayer.allIntegral;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestDelMahjongHousePlayer(ulong houseId, ulong summonerId)
        {
            RequestDelMahjongHousePlayer_L2D reqMsg_L2D = new RequestDelMahjongHousePlayer_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.summonerId = summonerId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveMahjongHouseStatus(ulong houseId, MahjongHouseStatus houseStatus)
        {
            RequestSaveMahjongHouseStatus_L2D reqMsg_L2D = new RequestSaveMahjongHouseStatus_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.houseStatus = houseStatus;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveTickets(ulong summonerId, TicketsNode ticketsNode)
        {
            ModuleManager.Get<SpecialActivitiesMain>().msg_proxy.OnRequestSaveTickets(summonerId, ticketsNode);
        }
        public void OnRequestSaveHouseBureauInfo(ulong houseId, MahjongHouseBureau houseBureau, List<PlayerTileNode> playerInitTileList)
        {
            if (houseBureau == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseBureauInfo, houseBureau == null ");
                return;
            }
            RequestSaveMahjongBureauInfo_L2D reqMsg_L2D = new RequestSaveMahjongBureauInfo_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.currentBureau = houseBureau.bureau;
            reqMsg_L2D.bureauTime = houseBureau.bureauTime;
            houseBureau.playerBureauList.ForEach(playerBureau => {
                reqMsg_L2D.playerIndexList.Add(playerBureau.playerIndex);
            });
            reqMsg_L2D.playerInitTile = Serializer.tryCompressObject(playerInitTileList);

            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveMahjongPendulum(ulong houseId, int currentBureau, int playerIndex, int operatDisplayType, List<int> mahjongList)
        {
            PendulumRecordNode pendulumRecord = new PendulumRecordNode { playerIndex = playerIndex, operatDisplayType = operatDisplayType };
            pendulumRecord.mahjongList.AddRange(mahjongList);
            MahjongRecordNode mahjongRecordNode = new MahjongRecordNode { recordType = MahjongRecordType.EMR_Pendulum, recordData = Serializer.trySerializerObject(pendulumRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, mahjongRecordNode);
        }
        public void OnRequestSaveMahjongPendulumDice(ulong houseId, int currentBureau, int playerIndex, int pendulumDice)
        {
            PendulumDiceRecordNode pendulumDiceRecord = new PendulumDiceRecordNode { playerIndex = playerIndex, pendulumDice = pendulumDice };
            MahjongRecordNode mahjongRecordNode = new MahjongRecordNode { recordType = MahjongRecordType.EMR_PendulumDice, recordData = Serializer.trySerializerObject(pendulumDiceRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, mahjongRecordNode);
        }
        public void OnRequestSaveShowMahjong(ulong houseId, int currentBureau, int playerIndex, int mahjongNode)
        {
            MahjongNodeRecordNode mahjongNodeRecord = new MahjongNodeRecordNode { playerIndex = playerIndex, mahjongNode = mahjongNode };
            MahjongRecordNode mahjongRecordNode = new MahjongRecordNode { recordType = MahjongRecordType.EMR_Show, recordData = Serializer.trySerializerObject(mahjongNodeRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, mahjongRecordNode);
        }
        public void OnRequestSaveGiveOffMahjong(ulong houseId, int currentBureau, int playerIndex, MahjongTile newMahjongTile)
        {
            MahjongNodeRecordNode mahjongNodeRecord = new MahjongNodeRecordNode { playerIndex = playerIndex, mahjongNode = newMahjongTile.GetMahjongNode() };
            MahjongRecordNode mahjongRecordNode = new MahjongRecordNode { recordType = MahjongRecordType.EMR_GiveOff, recordData = Serializer.trySerializerObject(mahjongNodeRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, mahjongRecordNode);
        }
        public void OnRequestSaveOperatMahjongSuccess(ulong houseId, int currentBureau, int currentWhoPlay, int playerIndex, MahjongKongType kongType, MeldType meldType, List<int> meldMahjongList)
        {
            MahjongRecordType recordType = MahjongRecordType.EMR_None;
            if (meldType == MeldType.EM_Sequence)
            {
                recordType = MahjongRecordType.EMR_Chow;
            }
            else if (meldType == MeldType.EM_Triplet)
            {
                recordType = MahjongRecordType.EMR_Pong;
            }
            else if (meldType == MeldType.EM_ExposedKong || meldType == MeldType.EM_ConcealedKong)
            {
                recordType = MahjongRecordType.EMR_Kong;
            }
            if (recordType != MahjongRecordType.EMR_None)
            {
                MahjongOperatRecordNode mahjongOperatRecord = new MahjongOperatRecordNode();
                mahjongOperatRecord.playerIndex = playerIndex;
                mahjongOperatRecord.meldType = meldType;
                mahjongOperatRecord.kongType = kongType;
                mahjongOperatRecord.meldMahjongList.AddRange(meldMahjongList);
                mahjongOperatRecord.lastPlayerIndex = currentWhoPlay;
                mahjongOperatRecord.bOperatHand = false;
                MahjongRecordNode mahjongRecordNode = new MahjongRecordNode { recordType = recordType, recordData = Serializer.trySerializerObject(mahjongOperatRecord) };
                //保存回放节点
                OnRequestSaveRecord(houseId, currentBureau, mahjongRecordNode);
            }
        }
        public void OnRequestSavePlayerNewHandMahjong(ulong houseId, int currentBureau, int playerIndex, MahjongTile newMahjongTile)
        {
            MahjongNodeRecordNode mahjongNodeRecord = new MahjongNodeRecordNode { playerIndex = playerIndex, mahjongNode = newMahjongTile.GetMahjongNode() };
            MahjongRecordNode mahjongRecordNode = new MahjongRecordNode { recordType = MahjongRecordType.EMR_GiveOff, recordData = Serializer.trySerializerObject(mahjongNodeRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, mahjongRecordNode);
        }
        public void OnRequestSaveMahjongKong(ulong houseId, int currentBureau, int playerIndex, List<MahjongTile> mahjongTileList)
        {
            MahjongKongRecordNode mahjongKongRecord = new MahjongKongRecordNode();
            mahjongKongRecord.playerIndex = playerIndex;
            mahjongKongRecord.kongMahjongList.AddRange(main.GetMahjongNode(mahjongTileList));
            MahjongRecordNode mahjongRecordNode = new MahjongRecordNode { recordType = MahjongRecordType.EMR_KongMahjong, recordData = Serializer.trySerializerObject(mahjongKongRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, mahjongRecordNode);
        }
        public void OnRequestSavePlayerKongSelf(ulong houseId, int currentBureau, int playerIndex, MahjongKongType kongType, MeldType meldType, List<int> meldMahjongList)
        {
            MahjongOperatRecordNode mahjongOperatRecord = new MahjongOperatRecordNode();
            mahjongOperatRecord.playerIndex = playerIndex;
            mahjongOperatRecord.meldType = meldType;
            mahjongOperatRecord.kongType = kongType;
            mahjongOperatRecord.meldMahjongList.AddRange(meldMahjongList);
            mahjongOperatRecord.lastPlayerIndex = playerIndex;
            mahjongOperatRecord.bOperatHand = true;
            MahjongRecordNode mahjongRecordNode = new MahjongRecordNode { recordType = MahjongRecordType.EMR_Kong, recordData = Serializer.trySerializerObject(mahjongOperatRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, mahjongRecordNode);
        }
        public void OnRequestSavePlayerNeedSeabed(ulong houseId, int currentBureau, int playerIndex, List<int> seabedMahjongList)
        {
            MahjongSeabedRecordNode mahjongSeabedRecord = new MahjongSeabedRecordNode { playerIndex = playerIndex};
            mahjongSeabedRecord.mahjongList.AddRange(seabedMahjongList);
            MahjongRecordNode mahjongRecordNode = new MahjongRecordNode { recordType = MahjongRecordType.EMR_Seabed, recordData = Serializer.trySerializerObject(mahjongSeabedRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, mahjongRecordNode);
        }
        public void OnRequestSaveMahjongWinInfo(ulong houseId, int currentBureau, MahjongWinRecordNode mahjongWinRecord)
        {
            if (mahjongWinRecord == null)
            {
                ServerUtil.RecordLog(LogType.Debug, "RequestSaveMahjongWinInfo, mahjongWinRecord == null ");
                return;
            }
            MahjongRecordNode mahjongRecordNode = new MahjongRecordNode { recordType = MahjongRecordType.EMR_Hu, recordData = Serializer.trySerializerObject(mahjongWinRecord) };
            //保存回放节点
            OnRequestSaveRecord(houseId, currentBureau, mahjongRecordNode);
        }
        public void OnRequestSaveRecord(ulong houseId, int currentBureau, MahjongRecordNode mahjongRecordNode)
        {
            RequestSaveMahjongRecord_L2D reqMsg_L2D = new RequestSaveMahjongRecord_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.currentBureau = (ulong)currentBureau;
            reqMsg_L2D.mahjongRecordNode = mahjongRecordNode;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSavePlayerSettlement(ulong houseId, MahjongPlayer housePlayer)
        {
            RequestSaveMahjongPlayerSettlement_L2D reqMsg_L2D = new RequestSaveMahjongPlayerSettlement_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.summonerId = housePlayer.summonerId;
            reqMsg_L2D.smallWinFangBlast = housePlayer.m_SmallWinFangBlast;
            reqMsg_L2D.smallWinJieBlast = housePlayer.m_SmallWinJieBlast;
            reqMsg_L2D.smallWinMyself = housePlayer.m_SmallWinMyself;
            reqMsg_L2D.bigWinFangBlast = housePlayer.m_BigWinFangBlast;
            reqMsg_L2D.bigWinJieBlast = housePlayer.m_BigWinJieBlast;
            reqMsg_L2D.bigWinMyself = housePlayer.m_BigWinMyself;
            reqMsg_L2D.allIntegral = housePlayer.allIntegral;
            reqMsg_L2D.zhuangLeisureType = housePlayer.zhuangLeisureType;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveBureauIntegral(ulong houseId, MahjongHouseBureau houseBureau)
        {
            if (houseBureau == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveBureauIntegral, houseBureau == null ");
                return;
            }
            RequestSaveMahjongBureauIntegral_L2D reqMsg_L2D = new RequestSaveMahjongBureauIntegral_L2D();
            reqMsg_L2D.houseId = houseId;
            reqMsg_L2D.bureau = houseBureau.bureau;
            reqMsg_L2D.playerBureauList.AddRange(houseBureau.playerBureauList);
            SendDBMsg(reqMsg_L2D);
        }
        public void OnnRequestSaveDissolveMahjongInfo(MahjongHouse mahjongHouse)
        {
            RequestSaveDissolveMahjongInfo_L2D reqMsg_L2D = new RequestSaveDissolveMahjongInfo_L2D();
            reqMsg_L2D.houseId = mahjongHouse.houseId;
            reqMsg_L2D.currentBureau = (ulong)mahjongHouse.currentBureau;
            reqMsg_L2D.playerBureauList.AddRange(mahjongHouse.GetHouseBureau().playerBureauList);
            mahjongHouse.GetMahjongPlayer().ForEach(housePlayer =>
            {
                reqMsg_L2D.playerIntegralList.Add(new PlayerIntegral { playerIndex = housePlayer.index, integral = housePlayer.allIntegral });
            });
            SendDBMsg(reqMsg_L2D);
        }
    }
}
#endif
