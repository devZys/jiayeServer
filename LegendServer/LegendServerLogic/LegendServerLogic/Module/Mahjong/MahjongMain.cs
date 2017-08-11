#if MAHJONG
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

namespace LegendServerLogic.Mahjong
{
    public class MahjongMain : Module
    {
        public MahjongMsgProxy msg_proxy;
        public List<BureauByHouseCard> bureauByHouseCardList = new List<BureauByHouseCard>();
        public List<ulong> houseVateList = new List<ulong>();
        public int houseDissolveVoteTime;
        public int businessDissolveVoteTime;
        public int startDisplayIntegral;
        public int csPingWinIntegral;
        public int csSpecialWinIntegral;
        public int zzConcealedKongIntegral;
        public int zzExposedKongIntegral;
        public int zzWinFangBlastIntegral;
        public int zzWinMyselfIntegral;
        public int zz7PairsAddIntegral;
        public int integralCapped;
        private bool alreadyRequestHouseInfo = false;
        public MahjongMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new MahjongMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
            //麻将开局数对应的房卡消耗
            MahjongConfigDB mahjongConfig = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(element => element.key == "MahjongBureauByHouseCard");
            if (mahjongConfig != null && !string.IsNullOrEmpty(mahjongConfig.value))
            {
                InitMahjongBureauByHouseCard(mahjongConfig.value);
            }
            //长沙麻将摆牌基础积分
            startDisplayIntegral = 1;
            mahjongConfig = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "CSMahjongStartDisplayIntegral");
            if (mahjongConfig != null && !string.IsNullOrEmpty(mahjongConfig.value))
            {
                int.TryParse(mahjongConfig.value, out startDisplayIntegral);
            }
            //长沙麻将小胡基础积分
            csPingWinIntegral = 1;
            mahjongConfig = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "CSMahjongPingWinIntegral");
            if (mahjongConfig != null && !string.IsNullOrEmpty(mahjongConfig.value))
            {
                int.TryParse(mahjongConfig.value, out csPingWinIntegral);
            }
            //长沙麻将大胡基础积分
            csSpecialWinIntegral = 6;
            mahjongConfig = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "CSMahjongSpecialWinIntegral");
            if (mahjongConfig != null && !string.IsNullOrEmpty(mahjongConfig.value))
            {
                int.TryParse(mahjongConfig.value, out csSpecialWinIntegral);
            }
            //转转麻将暗杠基础分
            zzConcealedKongIntegral = 2;
            mahjongConfig = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "ZZConcealedKongIntegral");
            if (mahjongConfig != null && !string.IsNullOrEmpty(mahjongConfig.value))
            {
                int.TryParse(mahjongConfig.value, out zzConcealedKongIntegral);
            }
            //转转麻将明杠基础分
            zzExposedKongIntegral = 1;
            mahjongConfig = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "ZZExposedKongIntegral");
            if (mahjongConfig != null && !string.IsNullOrEmpty(mahjongConfig.value))
            {
                int.TryParse(mahjongConfig.value, out zzExposedKongIntegral);
            }
            //转转麻将放炮基础分
            zzWinFangBlastIntegral = 1;
            mahjongConfig = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "ZZWinFangBlastIntegral");
            if (mahjongConfig != null && !string.IsNullOrEmpty(mahjongConfig.value))
            {
                int.TryParse(mahjongConfig.value, out zzWinFangBlastIntegral);
            }
            //转转麻将自摸基础分
            zzWinMyselfIntegral = 2;
            mahjongConfig = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "ZZWinMyselfIntegral");
            if (mahjongConfig != null && !string.IsNullOrEmpty(mahjongConfig.value))
            {
                int.TryParse(mahjongConfig.value, out zzWinMyselfIntegral);
            }
            //转转麻将小七对增加基础分
            zz7PairsAddIntegral = 1;
            mahjongConfig = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "ZZ7PairsAddIntegral");
            if (mahjongConfig != null && !string.IsNullOrEmpty(mahjongConfig.value))
            {
                int.TryParse(mahjongConfig.value, out zz7PairsAddIntegral);
            }    
            //麻将积分封顶数
            integralCapped = 1;
            mahjongConfig = DBManager<MahjongConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MahjongIntegralCapped");
            if (mahjongConfig != null && !string.IsNullOrEmpty(mahjongConfig.value))
            {
                int.TryParse(mahjongConfig.value, out integralCapped);
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
            MsgFactory.Regist(MsgID.D2L_ReplyMahjongHouseInfo, new MsgComponent(msg_proxy.OnReplyMahjongHouseInfo, typeof(ReplyMahjongHouseInfo_D2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyMahjongPlayerAndBureau, new MsgComponent(msg_proxy.OnReplyMahjongPlayerAndBureau, typeof(ReplyMahjongPlayerAndBureau_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestCreateMahjongHouse, new MsgComponent(msg_proxy.OnReqCreateMahjongHouse, typeof(RequestCreateMahjongHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestJoinMahjongHouse, new MsgComponent(msg_proxy.OnReqJoinMahjongHouse, typeof(RequestJoinMahjongHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestQuitMahjongHouse, new MsgComponent(msg_proxy.OnReqQuitMahjongHouse, typeof(RequestQuitMahjongHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestReadyMahjongHouse, new MsgComponent(msg_proxy.OnReqReadyMahjongHouse, typeof(RequestReadyMahjongHouse_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestMahjongHouseVote, new MsgComponent(msg_proxy.OnReqDissolveHouseVote, typeof(RequestMahjongHouseVote_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestMahjongPendulum, new MsgComponent(msg_proxy.OnReqMahjongPendulum, typeof(RequestMahjongPendulum_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestMahjongPendulumDice, new MsgComponent(msg_proxy.OnReqMahjongPendulumDice, typeof(RequestMahjongPendulumDice_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestMahjongHouseInfo, new MsgComponent(msg_proxy.OnReqMahjongHouseInfo, typeof(RequestMahjongHouseInfo_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestShowMahjong, new MsgComponent(msg_proxy.OnReqShowMahjong, typeof(RequestShowMahjong_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestOperatMahjong, new MsgComponent(msg_proxy.OnReqOperatMahjong, typeof(RequestOperatMahjong_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestPlayerSelectSeabed, new MsgComponent(msg_proxy.OnReqPlayerSelectSeabed, typeof(RequestPlayerSelectSeabed_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestMahjongMidwayPendulum, new MsgComponent(msg_proxy.OnReqMahjongMidwayPendulum, typeof(RequestMahjongMidwayPendulum_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestMahjongMidwayPendulumDice, new MsgComponent(msg_proxy.OnReqMahjongMidwayPendulumDice, typeof(RequestMahjongMidwayPendulumDice_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestMahjongOverallRecord, new MsgComponent(msg_proxy.OnReqMahjongOverallRecord, typeof(RequestMahjongOverallRecord_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyMahjongOverallRecord, new MsgComponent(msg_proxy.OnReplyMahjongOverallRecord, typeof(ReplyMahjongOverallRecord_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestMahjongBureauRecord, new MsgComponent(msg_proxy.OnReqMahjongBureauRecord, typeof(RequestMahjongBureauRecord_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyMahjongBureauRecord, new MsgComponent(msg_proxy.OnReplyMahjongBureauRecord, typeof(ReplyMahjongBureauRecord_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestMahjongBureauPlayback, new MsgComponent(msg_proxy.OnReqMahjongBureauPlayback, typeof(RequestMahjongBureauPlayback_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyMahjongBureauPlayback, new MsgComponent(msg_proxy.OnReplyMahjongBureauPlayback, typeof(ReplyMahjongBureauPlayback_D2L)));
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
                if (house.houseType == HouseType.MahjongHouse)
                {
                    MahjongHouse mahjongHouse = house as MahjongHouse;
                    if (mahjongHouse == null || mahjongHouse.houseStatus >= MahjongHouseStatus.MHS_Dissolved)
                    {
                        //房间不存在
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseDissolveVoteTimer, 房间不存在!  houseId = " + houseId);
                        delHouseIdList.Add(houseId);
                        continue;
                    }
                    if (mahjongHouse.voteBeginTime == DateTime.Parse("1970-01-01 00:00:00"))
                    {
                        //房间没有发起投票
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseDissolveVoteTimer, 房间没有发起投票! houseId = " + houseId);
                        delHouseIdList.Add(houseId);
                        continue;
                    }
                    int dissolveVoteTime = this.houseDissolveVoteTime;
                    if (mahjongHouse.businessId > 0)
                    {
                        dissolveVoteTime = this.businessDissolveVoteTime;
                    }
                    TimeSpan span = DateTime.Now.Subtract(mahjongHouse.voteBeginTime);
                    if (span.TotalSeconds < dissolveVoteTime)
                    {
                        //时间未到
                        continue;
                    }
                    OnHouseEndSettlement(mahjongHouse, MahjongHouseStatus.MHS_Dissolved);
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
        public void OnHouseEndSettlement(MahjongHouse mahjongHouse, MahjongHouseStatus houseStatus)
        {
            msg_proxy.OnHouseEndSettlement(mahjongHouse, houseStatus);
        }
        private void OnHouseAutomaticOperateTimer(object obj)
        {
            //玩家行为
            List<House> mahjongHouseList = HouseManager.Instance.GetHouseListByCondition(element => element.businessId > 0 && element.houseType == HouseType.MahjongHouse &&
                element.operateBeginTime != DateTime.Parse("1970-01-01 00:00:00") && element.GetMahjongHouseStatus() != MahjongHouseStatus.MHS_FreeBureau);
            if (mahjongHouseList != null && mahjongHouseList.Count > 0)
            {
                foreach (House house in mahjongHouseList)
                {
                    MahjongHouse mahjongHouse = house as MahjongHouse;
                    if (mahjongHouse == null || mahjongHouse.houseStatus >= MahjongHouseStatus.MHS_Dissolved)
                    {
                        //房间不存在
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间不存在!");
                        continue;
                    }
                    if (mahjongHouse.voteBeginTime != DateTime.Parse("1970-01-01 00:00:00"))
                    {
                        //房间正在发起投票
                        ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间正在发起投票!");
                        mahjongHouse.operateBeginTime.AddSeconds(1);
                        continue;
                    }
                    TimeSpan span = DateTime.Now.Subtract(mahjongHouse.operateBeginTime);
                    if (mahjongHouse.houseStatus == MahjongHouseStatus.MHS_PendulumBureau)
                    {
                        MahjongPlayer mahjongPlayer = mahjongHouse.GetMahjongPlayer(mahjongHouse.currentShowCard);
                        if (mahjongPlayer == null)
                        {
                            //房间摆牌玩家不存在
                            ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间摆牌玩家不存在! currentShowCard = " + mahjongHouse.currentShowCard);
                            continue;
                        }
                        if ((span.TotalSeconds < 15 && !mahjongPlayer.bHosted) || (mahjongPlayer.bHosted && span.TotalSeconds < 3))
                        {
                            //时间未到
                            continue;
                        }
                        SetMahjongPlayerHosted(mahjongPlayer);
                        if (mahjongPlayer.housePlayerStatus == MahjongPlayerStatus.MahjongPendulum)
                        {
                            for (int i = 0; i < Enum.GetNames(typeof(CSStartDisplayType)).Length; ++i)
                            {
                                CSStartDisplayType type = (CSStartDisplayType)(1 << i);
                                if ((mahjongPlayer.startDisplayType & type) == type)
                                {
                                    msg_proxy.OnReqMahjongPendulum(mahjongHouse, mahjongPlayer, type);
                                    break;
                                }
                            }
                        }
                        else if (mahjongPlayer.housePlayerStatus == MahjongPlayerStatus.MahjongPendulumDice)
                        {
                            msg_proxy.OnReqMahjongPendulumDice(mahjongHouse, mahjongPlayer);
                        }
                        else
                        {
                            //房间摆牌玩家状态有误
                            ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间摆牌玩家状态有误! housePlayerStatus = " + mahjongPlayer.housePlayerStatus);
                            continue;
                        }
                    }
                    else if (mahjongHouse.houseStatus == MahjongHouseStatus.MHS_BeginBureau)
                    {
                        if (mahjongHouse.currentShowCard == -1)
                        {
                            MahjongOperatNode mahjongOperatNode = mahjongHouse.GetPlayerMahjongOperatNode();
                            if (mahjongOperatNode != null)
                            {
                                MahjongPlayer mahjongPlayer = mahjongHouse.GetMahjongPlayer(mahjongOperatNode.playerIndex);
                                if (mahjongPlayer == null)
                                {
                                    //房间等待操作玩家不存在
                                    ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间等待操作玩家不存在! playerIndex = " + mahjongOperatNode.playerIndex);
                                    continue;
                                }
                                if ((span.TotalSeconds < 15 && !mahjongPlayer.bHosted) || (mahjongPlayer.bHosted && span.TotalSeconds < 2))
                                {
                                    //时间未到
                                    continue;
                                }
                                SetMahjongPlayerHosted(mahjongPlayer);
                                List<int> mahjongList = new List<int>();
                                MahjongOperatType operatType = MahjongOperatType.EMO_None;
                                if (mahjongOperatNode.operatedType == MahjongOperatType.EMO_Hu)
                                {
                                    if (mahjongHouse.currentMahjongList.Count > 1)
                                    {
                                        foreach(MahjongTile mahjongTile in mahjongHouse.currentMahjongList)
                                        {
                                            if (CSSpecialWinType.WT_None != mahjongPlayer.WinHandTileCheck(mahjongTile, mahjongHouse.bFakeHu))
                                            {
                                                mahjongList.Add(mahjongTile.GetMahjongNode());
                                            }
                                        }
                                    }
                                    else
                                    {
                                        mahjongList.AddRange(GetMahjongNode(mahjongHouse.currentMahjongList));
                                    }
                                    if (mahjongList.Count > 0)
                                    {
                                        operatType = MahjongOperatType.EMO_Hu;
                                    }
                                }
                                msg_proxy.OnReqOperatMahjong(mahjongHouse, mahjongPlayer, operatType, mahjongList);
                            }
                        }
                        else if (mahjongHouse.currentShowCard == -2)
                        {
                            MahjongSelectNode kongHuPlayer = mahjongHouse.kongHuPlayerList.Find(element => element.bWait);
                            if (kongHuPlayer != null)
                            {
                                MahjongPlayer mahjongPlayer = mahjongHouse.GetMahjongPlayer(kongHuPlayer.playerIndex);
                                if (mahjongPlayer == null)
                                {
                                    //房间抢杠胡玩家不存在
                                    ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间抢杠胡玩家不存在! kongHuPlayerIndex = " + kongHuPlayer.playerIndex);
                                    continue;
                                }
                                if ((span.TotalSeconds < 15 && !mahjongPlayer.bHosted) || (mahjongPlayer.bHosted && span.TotalSeconds < 2))
                                {
                                    //时间未到
                                    continue;
                                }
                                SetMahjongPlayerHosted(mahjongPlayer);
                                List<int> mahjongList = GetMahjongNode(mahjongHouse.currentMahjongList);
                                msg_proxy.OnReqOperatMahjongByGrabKong(mahjongHouse, mahjongPlayer, MahjongOperatType.EMO_Hu, mahjongList);
                            }
                        }
                        else
                        {
                            MahjongPlayer mahjongPlayer = mahjongHouse.GetMahjongPlayer(mahjongHouse.currentShowCard);
                            if (mahjongPlayer == null)
                            {
                                //房间玩家不存在
                                ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间玩家不存在! currentShowCard = " + mahjongHouse.currentShowCard);
                                continue;
                            }
                            if ((span.TotalSeconds < 15 && !mahjongPlayer.bHosted) || (mahjongPlayer.bHosted && span.TotalSeconds < 2))
                            {
                                //时间未到
                                continue;
                            }
                            SetMahjongPlayerHosted(mahjongPlayer);
                            if (mahjongPlayer.housePlayerStatus == MahjongPlayerStatus.MahjongWaitCard)
                            {
                                int showMahjongTile = 0;
                                if (mahjongPlayer.newMahjongTile != null)
                                {
                                    if (!mahjongPlayer.m_bGiveUpWin && CSSpecialWinType.WT_None != mahjongPlayer.WinHandTileCheck())
                                    {
                                        List<int> mahjongList = new List<int>();
                                        mahjongList.Add(mahjongPlayer.newMahjongTile.GetMahjongNode());
                                        msg_proxy.OnReqOperatMahjongByMyself(mahjongHouse, mahjongPlayer, MahjongOperatType.EMO_Hu, mahjongList);
                                        continue;
                                    }
                                    else if (mahjongHouse.mahjongType == MahjongType.RedLaiZiMahjong && mahjongPlayer.newMahjongTile.IsRed())
                                    {
                                        showMahjongTile = mahjongPlayer.GetPlayerHandMahjongEndNode();
                                    }
                                    else
                                    {
                                        showMahjongTile = mahjongPlayer.newMahjongTile.GetMahjongNode();
                                    }
                                }
                                else
                                {
                                    showMahjongTile = mahjongPlayer.GetPlayerHandMahjongEndNode();
                                }
                                if (showMahjongTile != 0)
                                {
                                    msg_proxy.OnReqShowMahjong(mahjongHouse, mahjongPlayer, new MahjongTile(showMahjongTile));
                                }
                                else
                                {
                                    //要出的牌有误
                                    ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 要出的牌有误! showMahjongTile = " + showMahjongTile);
                                    continue;
                                }
                            }
                            else if (mahjongPlayer.housePlayerStatus == MahjongPlayerStatus.MahjongPendulumDice)
                            {
                                //中途四喜打骰
                                msg_proxy.OnReqMahjongMidwayPendulumDice(mahjongHouse, mahjongPlayer);
                            }
                        }
                    }
                    else if (mahjongHouse.houseStatus == MahjongHouseStatus.MHS_SelectSeabed)
                    {
                        MahjongPlayer mahjongPlayer = mahjongHouse.GetMahjongPlayer(mahjongHouse.currentShowCard);
                        if (mahjongPlayer == null)
                        {
                            //房间海底选择玩家不存在
                            ServerUtil.RecordLog(LogType.Debug, "OnHouseAutomaticOperateTimer, 房间海底选择玩家不存在! currentShowCard = " + mahjongHouse.currentShowCard);
                            continue;
                        }
                        if ((span.TotalSeconds < 15 && !mahjongPlayer.bHosted) || (mahjongPlayer.bHosted && span.TotalSeconds < 2))
                        {
                            //时间未到
                            continue;
                        }
                        SetMahjongPlayerHosted(mahjongPlayer);
                        //默认不要
                        msg_proxy.OnReqPlayerSelectSeabed(mahjongHouse, mahjongPlayer);
                    }
                    else if (mahjongHouse.houseStatus == MahjongHouseStatus.MHS_Settlement)
                    {
                        if (span.TotalSeconds < 5)
                        {
                            //时间未到
                            continue;
                        }
                        MahjongPlayer mahjongPlayer = mahjongHouse.GetMahjongPlayerByCondition(element => element.housePlayerStatus != MahjongPlayerStatus.MahjongReady);
                        if (mahjongPlayer != null)
                        {
                            msg_proxy.OnReqReadyMahjongHouse(mahjongHouse, mahjongPlayer);
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
        public void OnRecvDissolveMahjongHouse(MahjongHouse mahjongHouse)
        {
            foreach (MahjongPlayer housePlayer in mahjongHouse.GetMahjongPlayer())
            {
                Summoner sender = SummonerManager.Instance.GetSummonerByUserId(housePlayer.userId);
                if (sender != null)
                {
                    sender.houseId = 0;
                    msg_proxy.OnRecvQuitMahjongHouse(housePlayer.summonerId, housePlayer.proxyServerId, 0);
                }
                msg_proxy.OnRequestSaveHouseId(housePlayer.userId, 0);
            }
            OnRecvGMDissolveMahjongHouse(mahjongHouse);
        }
        public void OnRecvGMDissolveMahjongHouse(MahjongHouse mahjongHouse)
        {
            mahjongHouse.houseStatus = MahjongHouseStatus.MHS_GMDissolved;
            //保存房间状态
            msg_proxy.OnRequestSaveMahjongHouseStatus(mahjongHouse.houseId, mahjongHouse.houseStatus);
            HouseManager.Instance.RemoveHouse(mahjongHouse.houseId);
        }
        private void InitMahjongBureauByHouseCard(string mahjongBureauByHouseCard)
        {
            bureauByHouseCardList.Clear();
            string[] valueArr = mahjongBureauByHouseCard.Split('|');
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
        public MahjongPlayerShowNode GetPlayerShowNode(MahjongPlayer housePlayer)
        {
            MahjongPlayerShowNode playerShowNode = new MahjongPlayerShowNode();
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
        public MahjongMyPlayerOnlineNode GetMyPlayerOnlineNode(MahjongPlayer housePlayer)
        {
            MahjongMyPlayerOnlineNode myPlayerOnline = new MahjongMyPlayerOnlineNode();
            myPlayerOnline.index = housePlayer.index;
            myPlayerOnline.housePlayerStatus = housePlayer.housePlayerStatus;
            myPlayerOnline.allIntegral = housePlayer.allIntegral;
            myPlayerOnline.voteStatus = housePlayer.voteStatus;
            myPlayerOnline.startDisplayType = (int)housePlayer.startDisplayType;
            myPlayerOnline.bReadyHand = housePlayer.m_bReadyHand;
            myPlayerOnline.bGiveUpWin = housePlayer.m_bGiveUpWin;
            if (housePlayer.newMahjongTile != null)
            {
                myPlayerOnline.newMahjongNode = housePlayer.newMahjongTile.GetMahjongNode();
            }
            if (housePlayer.currPongTile != null)
            {
                myPlayerOnline.currPongNode = housePlayer.currPongTile.GetMahjongNode();
            }
            housePlayer.GetPlayerHandTileList(myPlayerOnline.playerMahjongList);
            housePlayer.GetPlayerMeldList(myPlayerOnline.displayMahjongList);
            housePlayer.GetPlayerShowTileList(myPlayerOnline.showMahjongList);
            housePlayer.GetPlayerFourTileList(myPlayerOnline.displayFourTileList);
            myPlayerOnline.bHosted = housePlayer.bHosted;

            return myPlayerOnline;
        }
        public MahjongPlayerOnlineNode GetPlayerOnlineNode(MahjongPlayer housePlayer, bool bOnLineFlag = false)
        {
            MahjongPlayerOnlineNode playerOnlineNode = new MahjongPlayerOnlineNode();
            playerOnlineNode.index = housePlayer.index;
            playerOnlineNode.nickName = housePlayer.nickName;
            playerOnlineNode.summonerId = housePlayer.summonerId;
            playerOnlineNode.sex = housePlayer.sex;
            playerOnlineNode.ip = housePlayer.ip;
            playerOnlineNode.housePlayerStatus = housePlayer.housePlayerStatus;
            playerOnlineNode.allIntegral = housePlayer.allIntegral;
            playerOnlineNode.voteStatus = housePlayer.voteStatus;
            playerOnlineNode.headMahjongCount = housePlayer.GetHeadMahjongCount();
            housePlayer.GetPlayerMeldList(playerOnlineNode.displayMahjongList);
            housePlayer.GetPlayerShowTileList(playerOnlineNode.showMahjongList);
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
        public List<int> GetMahjongNode(List<MahjongTile> mahjongTileList)
        {
            List<int> mahjongList = new List<int>();
            if (mahjongTileList == null || mahjongTileList.Count <= 0)
            {
                return mahjongList;
            }
            foreach (MahjongTile tile in mahjongTileList)
            {
                mahjongList.Add(tile.GetMahjongNode());
            }
            return mahjongList;
        }
        public MahjongOperatNode GetMahjongOperatNode(MahjongHouse mahjongHouse, List<MahjongOperatNode> mahjongOperatList)
        {
            int playerIndex = mahjongHouse.currentWhoPlay;
            for (int i = 0; i < mahjongHouse.GetHousePlayerCount(); ++i)
            {
                MahjongOperatNode operatNode = mahjongOperatList.Find(element => element.playerIndex == playerIndex);
                if (operatNode != null)
                {
                    return operatNode;
                }
                playerIndex = mahjongHouse.GetNextHousePlayerIndex(playerIndex);
            }
            return mahjongOperatList.FirstOrDefault();
        }
        public int GetShowBirdIndex(int currentWhoPlay, List<PlayerWinMahjongNode> winPlayerList)
        {
            if (winPlayerList != null && winPlayerList.Count == 1)
            {
                return winPlayerList[0].playerIndex;
            }
            return currentWhoPlay;
        }
        public void DisPlayerWinBirdNumber(MahjongHouse mahjongHouse, List<MahjongTile> mahjongTileBirdList, int showBirdIndex)
        {
            if (mahjongHouse == null || mahjongTileBirdList == null || mahjongTileBirdList.Count == 0)
            {
                return;
            }
            MahjongPlayer zhuangPlayer = mahjongHouse.GetHouseZhuang();
            if (zhuangPlayer == null)
            {
                return;
            }
            MahjongHouseBureau houseBureau = mahjongHouse.GetHouseBureau();
            if (houseBureau == null)
            {
                return;
            }
            int[] birdNumberArray = new int[] { 0, 0, 0, 0 };
            mahjongTileBirdList.ForEach(mahjongTile =>
            {
                if (!mahjongTile.IsRed())
                {
                    int remainder = (int)mahjongTile.GetNumType() % mahjongHouse.maxPlayerNum;
                    birdNumberArray[remainder] += 1;
                }
                else
                {
                    birdNumberArray[1] += 1;
                }
            });
            int beginIndex = zhuangPlayer.index;
            //if (mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_HuZhuang))  --后期才加的为了照顾线上红中 转转麻将才没有统一判断
            if (mahjongHouse.CheckHousePropertyType(MahjongHousePropertyType.EMHP_HuZhuang) || mahjongHouse.mahjongType == MahjongType.RedLaiZiMahjong || mahjongHouse.mahjongType == MahjongType.ZhuanZhuanMahjong)
            {
                beginIndex = showBirdIndex;
            }
            int playerIndex = mahjongHouse.GetLastHousePlayerIndex(beginIndex);
            for(int i = 0; i < mahjongHouse.GetHousePlayerCount(); ++i)
            {
                if (birdNumberArray[i] > 0)
                {
                    MahjongPlayerBureau playerBureau = houseBureau.playerBureauList.Find(element => element.playerIndex == playerIndex);
                    if (playerBureau != null)
                    {
                        playerBureau.winBirdNumber = birdNumberArray[i];
                    }
                }
                playerIndex = mahjongHouse.GetNextHousePlayerIndex(playerIndex);
            }
        }
        public SpecialWinMahjongType GetSpecialWinMahjongType(MahjongHouse mahjongHouse)
        {
            SpecialWinMahjongType specialWinMahjongType = SpecialWinMahjongType.ESWM_None;
            if (mahjongHouse.currentShowCard == -2)
            {
                //抢杠胡
                if (mahjongHouse.mahjongSpecialType == MahjongSpecialType.EMS_Kong || 
                    (mahjongHouse.mahjongType != MahjongType.ChangShaMahjong && mahjongHouse.mahjongSpecialType == MahjongSpecialType.EMS_MakeUp))
                {
                    specialWinMahjongType = SpecialWinMahjongType.ESWM_GrabKong;
                }
            }
            else
            {
                if (mahjongHouse.mahjongSpecialType == MahjongSpecialType.EMS_Kong)
                {
                    specialWinMahjongType = SpecialWinMahjongType.ESWM_KongWin;
                }
                else if (mahjongHouse.CheckPlayerSeabedWin())
                {
                    specialWinMahjongType = SpecialWinMahjongType.ESWM_SeabedWin;
                }
            }

            return specialWinMahjongType;
        }
        private void SetMahjongPlayerHosted(MahjongPlayer player)
        {
            if (!player.bHosted)
            {
                player.bHosted = true;
                ModuleManager.Get<MainCityMain>().OnRecvPlayerHostedStatus(player.summonerId, player.proxyServerId, player.bHosted);
            }
        }
        public void CheckWinHandTileCheckOne(MahjongPlayer player)
        {
            List<MahjongTile> resultList = new List<MahjongTile>();
            resultList.Add(new MahjongTile(TileBlurType.ETB_Honor, TileDescType.ETD_Dragon, TileNumType.ETN_Red));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Six));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Six));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Six));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_One));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_One));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_One));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Four));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Four));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Four));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven));
            resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Six));

            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_One));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Two));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Three));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Nine));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Three));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Four));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Five));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Bamboo, TileNumType.ETN_Nine));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_One));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_One));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_One));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Nine));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Nine));
            //resultList.Add(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Dot, TileNumType.ETN_Nine));

            List<Meld> meldList = new List<Meld>();
            Meld meld = new Meld(new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven),
                new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven),
                new MahjongTile(TileBlurType.ETB_Suit, TileDescType.ETD_Character, TileNumType.ETN_Seven), MeldType.EM_Triplet);
            meldList.Add(meld);

            MahjongStrategyBase strategy = MahjongManager.Instance.GetMahjongStrategy(MahjongType.RedLaiZiMahjong);
            bool bResult = strategy.AnalyseHandTile(resultList, meldList, true);
            //CSSpecialWinType type = player.WinHandTileCheckOne(resultList, meldList);
            //bool bResult = player.AnalyseHandTileOne(resultList);
            ServerUtil.RecordLog(LogType.Debug, "CheckWinHandTileCheckOne, bResult = " + bResult);
        }

        public void TryJoinMahjongHouse(Summoner sender, MahjongHouse mahjongHouse)
        {
            msg_proxy.JoinMahjongHouse(sender, mahjongHouse);
        }
    }
}
#endif