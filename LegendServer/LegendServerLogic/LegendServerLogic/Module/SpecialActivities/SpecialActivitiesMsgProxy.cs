using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServerCompetitionManager;
using LegendServerLogic.Actor.Summoner;
using LegendServerLogic.Distributed;
using LegendServerLogic.Entity.Base;
using LegendServerLogic.Entity.Houses;
using LegendServerLogic.Entity.Players;
using LegendServerLogic.MainCity;
using LegendServerLogicDefine;
using System.Collections.Generic;
#if RUNFAST
using LegendServerLogic.RunFast;
#elif MAHJONG
using LegendServerLogic.Mahjong;
#elif WORDPLATE
using LegendServerLogic.WordPlate;
#endif

namespace LegendServerLogic.SpecialActivities
{
    public class SpecialActivitiesMsgProxy : ServerMsgProxy
    {
        private SpecialActivitiesMain main;

        public SpecialActivitiesMsgProxy(SpecialActivitiesMain main)
            : base(main.root)
        {
            this.main = main;
        }

        public void OnNotifyJoinMarket(int peerId, bool inbound, object msg)
        {
            NotifyJoinMarket_W2L notifyMsg = msg as NotifyJoinMarket_W2L;

            if (notifyMsg.joinMarketNode == null)
            {
                //返回结果有误
                ServerUtil.RecordLog(LogType.Error, "OnNotifyJoinMarket, 返回结果有误! requesterSummonerId = " + notifyMsg.requesterSummonerId + ", joinMarketNode = null");
                return;
            }
            if (notifyMsg.joinMarketNode.marketId <= 0 || notifyMsg.joinMarketNode.competitionKey <= 0 || notifyMsg.joinMarketNode.keyType == MarketKeyType.EMK_None)
            {
                //商城id有误
                ServerUtil.RecordLog(LogType.Error, "OnNotifyJoinMarket, 返回结果有误! marketId = " + notifyMsg.joinMarketNode.marketId + ", competitionKey = " + notifyMsg.joinMarketNode.competitionKey + ", keyType = " + notifyMsg.joinMarketNode.keyType);
                return;
            }
            Summoner sender = SummonerManager.Instance.GetSummonerById(notifyMsg.requesterSummonerId);
            if (sender == null)
            {
                //玩家查找有误
                ServerUtil.RecordLog(LogType.Error, "OnNotifyJoinMarket, 玩家查找有误! requesterSummonerId = " + notifyMsg.requesterSummonerId);
                return;
            }
            if (sender.houseId > 0 || sender.competitionKey > 0)
            {
                //正在打牌或者已经参加比赛不能参加比赛
                ServerUtil.RecordLog(LogType.Error, "OnNotifyJoinMarket, 正在打牌或者已经参加比赛不能参加比赛! requesterSummonerId = " + notifyMsg.requesterSummonerId);
                return;
            }
            if (notifyMsg.logicId != root.ServerID)
            {
                //在别的逻辑服务器找到了房间则将自己的信息打包通过自己网关转发给房间对应的逻辑服务器
                //然后从当前旧逻辑服退出，在目标逻辑服务器继续加入比赛场的操作
                TransmitPlayerInfo_L2P transmitMsg = new TransmitPlayerInfo_L2P();
                transmitMsg.summoner = ServerUtil.Serialize(sender);
                transmitMsg.summonerId = sender.id;
                transmitMsg.type = TransmitPlayerType.ETP_JoinMarket;
                transmitMsg.parameter = Serializer.trySerializerObject(notifyMsg.joinMarketNode);
                transmitMsg.targetLogic = notifyMsg.logicId;
                SendProxyMsg(transmitMsg, sender.proxyServerId);

                SummonerManager.Instance.RemoveSummoner(sender.id);
                return;
            }
            JoinMarket(sender, notifyMsg.joinMarketNode);
        }
        public void JoinMarket(Summoner sender, JoinMarketNode joinMarketNode)
        {
            if (joinMarketNode.keyType == MarketKeyType.EMK_Ordinary)
            {
                JoinMarketOrdinary(sender, joinMarketNode.marketId);
            }
            else if (joinMarketNode.keyType == MarketKeyType.EMK_Competition)
            {
                JoinMarketCompetition(sender, joinMarketNode.competitionKey);
            }
        }
        private void JoinMarketOrdinary(Summoner sender, int marketId)
        {
            if (sender == null) return;

#if RUNFAST
            House house = HouseManager.Instance.GetHouseByCondition(element => (element.businessId == marketId && element.houseType == HouseType.RunFastHouse && element.GetRunFastHouseStatus() == RunFastHouseStatus.RFHS_FreeBureau && element.GetHousePlayerCount() < element.maxPlayerNum));
            if (house == null)
            {
                //创建房间（跑得快经典玩法）
                ModuleManager.Get<RunFastMain>().msg_proxy.OnReqCreateRunFastHouse(sender, main.runFastMaxPlayerNum, main.runFastMaxBureau, main.runFastType, (int)main.housePropertyType, marketId);
            }
            else
            {
                //加入房间
                ModuleManager.Get<RunFastMain>().msg_proxy.OnReqJoinRunFastHouse(sender, house.houseCardId);
            }
#elif MAHJONG
            House house = HouseManager.Instance.GetHouseByCondition(element => (element.businessId == marketId && element.houseType == HouseType.MahjongHouse && element.GetMahjongHouseStatus() == MahjongHouseStatus.MHS_FreeBureau && element.GetHousePlayerCount() < element.maxPlayerNum));
            if (house == null)
            {
                //创建房间
                ModuleManager.Get<MahjongMain>().msg_proxy.OnReqCreateMahjongHouse(sender, main.mahjongMaxPlayerNum, main.mahjongMaxBureau, main.mahjongType, (int)main.housePropertyType, main.catchBird, main.flutter, marketId);
            }
            else
            {
                //加入房间
                ModuleManager.Get<MahjongMain>().msg_proxy.OnReqJoinMahjongHouse(sender, house.houseCardId);
            }
#elif WORDPLATE
            House house = HouseManager.Instance.GetHouseByCondition(element => (element.businessId == marketId && element.houseType == HouseType.WordPlateHouse && element.GetWordPlateHouseStatus() == WordPlateHouseStatus.EWPS_FreeBureau && element.GetHousePlayerCount() < element.maxPlayerNum));
            if (house == null)
            {
                //创建房间
                ModuleManager.Get<WordPlateMain>().msg_proxy.OnReqCreateWordPlateHouse(sender, main.maxWinScore, main.wordPlateMaxBureau, main.wordPlateType, (int)main.housePropertyType, main.baseWinScore, marketId);
            }
            else
            {
                //加入房间
                ModuleManager.Get<WordPlateMain>().msg_proxy.OnReqJoinWordPlateHouse(sender, house.houseCardId);
            }
#endif
        }
        public void OnRequestTicketsInfo(int peerId, bool inbound, object msg)
        {
            RequestTicketsInfo_P2L reqMsg = msg as RequestTicketsInfo_P2L;
            ReplyTicketsInfo_L2P replyMsg = new ReplyTicketsInfo_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            List<ulong> ticketsOnlyIdList = new List<ulong>();
            foreach (TicketsNode node in sender.ticketsList)
            {
                if (main.CheckTicketsTime(node.ticketsId, node.beginTime))
                {
                    ticketsOnlyIdList.Add(node.id);
                }
            }
            if (ticketsOnlyIdList.Count > 0)
            {
                foreach (ulong ticketsOnlyId in ticketsOnlyIdList)
                {
                    sender.DelTicketsNode(ticketsOnlyId);
                }
                //删除优惠卷
                OnRequestDelTickets(sender.userId, ticketsOnlyIdList);
            }

            replyMsg.result = ResultCode.OK;
            if (!sender.bTicketsInfoFlag)
            {
                sender.bTicketsInfoFlag = true;
                replyMsg.ticketsList.AddRange(sender.ticketsList);
            }
            else
            {
                replyMsg.delTicketsIdList.AddRange(ticketsOnlyIdList);
            }
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void OnRequestUseTickets(int peerId, bool inbound, object msg)
        {
            RequestUseTickets_P2L reqMsg = msg as RequestUseTickets_P2L;
            ReplyUseTickets_L2P replyMsg = new ReplyUseTickets_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            TicketsNode ticketsNode = sender.ticketsList.Find(element => element.id == reqMsg.ticketsOnlyId);
            if (ticketsNode == null)
            {
                //没有找到该优惠券
                replyMsg.result = ResultCode.TicketsDoesNotExist;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (main.CheckTicketsTime(ticketsNode.ticketsId, ticketsNode.beginTime))
            {
                //过期了
                replyMsg.result = ResultCode.ThisTicketsExpired;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                //删掉优惠券
                sender.DelTicketsNode(ticketsNode.id);
                OnRequestDelTickets(sender.userId, ticketsNode.id);
                return;
            }
            if (ticketsNode.useStatus == UseStatus.Used)
            {
                //已经使用过该优惠券
                replyMsg.result = ResultCode.UsedThisTicket;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }

            ticketsNode.useStatus = UseStatus.Used;

            replyMsg.result = ResultCode.OK;
            replyMsg.ticketsOnlyId = reqMsg.ticketsOnlyId;
            SendProxyMsg(replyMsg, sender.proxyServerId);

            //使用优惠券
            OnRequestUseTickets(sender.userId, reqMsg.ticketsOnlyId);
        }
        public void OnRequestCreateMarketCompetition(int peerId, bool inbound, object msg)
        {
            RequestCreateMarketCompetition_W2L reqMsg = msg as RequestCreateMarketCompetition_W2L;
            ReplyCreateMarketCompetition_L2W replyMsg = new ReplyCreateMarketCompetition_L2W();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.requesterSummonerId);
            if (sender == null) return;

            replyMsg.requesterSummonerId = reqMsg.requesterSummonerId;
            replyMsg.marketId = reqMsg.node.marketId;
            replyMsg.competitionKey = reqMsg.node.competitionKey;
            replyMsg.logicId = root.ServerID;

            if (sender.houseId > 0 || sender.competitionKey > 0)
            {
                //商家创建非法请求
                ServerUtil.RecordLog(LogType.Error, "CreateMarketCompetition, 商家创建非法请求! marketId = " + reqMsg.node.marketId);
                replyMsg.result = ResultCode.Wrong;
                SendWorldMsg(replyMsg);
                return;
            }
            if (reqMsg.logicId != root.ServerID)
            {
                //在别的逻辑服务器找到了房间则将自己的信息打包通过自己网关转发给房间对应的逻辑服务器
                //然后从当前旧逻辑服退出，在目标逻辑服务器继续创建比赛场的操作
                TransmitPlayerInfo_L2P transmitMsg = new TransmitPlayerInfo_L2P();
                transmitMsg.summoner = ServerUtil.Serialize(sender);
                transmitMsg.summonerId = sender.id;
                transmitMsg.type = TransmitPlayerType.ETP_CreateCompetition;
                transmitMsg.parameter = Serializer.trySerializerObject(reqMsg.node);
                transmitMsg.targetLogic = reqMsg.logicId;
                SendProxyMsg(transmitMsg, sender.proxyServerId);

                SummonerManager.Instance.RemoveSummoner(sender.id);
                return;
            }
            CreateMarketCompetition(sender, reqMsg.node, replyMsg);
        }
        public void CreateMarketCompetition(Summoner sender, CreateCompetitionNode comNode, ReplyCreateMarketCompetition_L2W replyMsg = null)
        {   
            if (replyMsg == null)
            {
                replyMsg = new ReplyCreateMarketCompetition_L2W();
                replyMsg.requesterSummonerId = sender.id;
                replyMsg.marketId = comNode.marketId;
                replyMsg.competitionKey = comNode.competitionKey;
                replyMsg.logicId = root.ServerID;
            }
            int maxBureau = main.GetMaxBureau(comNode.firstAdmitNum);
            if (maxBureau <= 1)
            {
                //计算匹配最大局数有误
                ServerUtil.RecordLog(LogType.Debug, "CreateMarketCompetition, 计算匹配最大局数有误! maxBureau = " + maxBureau);
                replyMsg.result = ResultCode.CreateCompetitionDataError;
                SendWorldMsg(replyMsg);
                return;
            }
            Market market = CompetitionManager.Instance.GetMarket(comNode.marketId);
            if (market == null)
            {
                //商家第一次创建比赛场
                CompetitionManager.Instance.CreateMarket(comNode.marketId, out market);
            }
            if (market.GetApplyCompetitionNum() >= main.competitionCreateLimitNum)
            {
                //商家创建已经达到上限
                ServerUtil.RecordLog(LogType.Debug, "CreateMarketCompetition, 商家创建已经达到上限! marketId = " + comNode.marketId);
                replyMsg.result = ResultCode.MarketCompetitionLimitMax;
                SendWorldMsg(replyMsg);
                return;
            }
            if (market.IsCompetitionExist(comNode.competitionKey))
            {
                //这个动态口令已经存在了
                ServerUtil.RecordLog(LogType.Debug, "CreateMarketCompetition, 这个动态口令已经存在了! competitionKey = " + comNode.competitionKey);
                replyMsg.result = ResultCode.CompetitionKeyInvalid;
                SendWorldMsg(replyMsg);
                return;
            }
            Competition competition = new Competition(sender.id, comNode.marketId, comNode.competitionKey, comNode.maxApplyNum, comNode.firstAdmitNum, maxBureau, comNode.maxGameBureau);
            market.AddCompetition(comNode.competitionKey, competition);
            //进行计时
            main.AddMarketsCompetitionEndTimer(comNode.competitionKey);

            replyMsg.result = ResultCode.OK;
            replyMsg.maxGameBureau = comNode.maxGameBureau;
            replyMsg.maxApplyNum = comNode.maxApplyNum;
            replyMsg.firstAdmitNum = comNode.firstAdmitNum;
            replyMsg.createTime = competition.createTime.ToString();
            SendWorldMsg(replyMsg);
        }
        private void JoinMarketCompetition(Summoner sender, int competitionKey)
        {
            if (sender == null) return;

            ReplyJoinMarketCompetition_L2P replyMsg = new ReplyJoinMarketCompetition_L2P();

            replyMsg.summonerId = sender.id;

            if (CompetitionManager.Instance.IsCompetitionExistByKey(sender.competitionKey, sender.id))
            {
                //已经在参加比赛场了
                ServerUtil.RecordLog(LogType.Debug, "JoinMarketCompetition, 已经在参加比赛场了! summonerId = " + sender.id + ", competitionKey = " + sender.competitionKey);
                replyMsg.result = ResultCode.PlayerJoinCompetition;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            Competition competition = CompetitionManager.Instance.GetCompetitionByKey(competitionKey);
            if (competition == null)
            {
                //无效的动态口令
                ServerUtil.RecordLog(LogType.Debug, "JoinMarketCompetition, 无效的动态口令! competitionKey = " + sender.competitionKey);
                replyMsg.result = ResultCode.CompetitionKeyInvalid;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (competition.status != CompetitionStatus.ECS_Apply)
            {
                //不是报名状态
                ServerUtil.RecordLog(LogType.Debug, "JoinMarketCompetition, 不是报名状态! competitionKey = " + sender.competitionKey);
                replyMsg.result = ResultCode.CompetitionNoApplyStatus;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (competition.CheckPlayerExist(sender.id))
            {
                //已经存在了
                ServerUtil.RecordLog(LogType.Debug, "JoinMarketCompetition, 已经存在了! summonerId = " + sender.id + ", competitionKey = " + sender.competitionKey);
                replyMsg.result = ResultCode.PlayerJoinCompetition;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (competition.CheckFullPlayerNum())
            {
                //人满了
                ServerUtil.RecordLog(LogType.Debug, "JoinMarketCompetition, 人满了! competitionKey = " + sender.competitionKey);
                replyMsg.result = ResultCode.CompetitionPlayerNumFull;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            competition.AddCompetitionPlayer(sender.id, sender.nickName, sender.userId, sender.ip, sender.sex);

            sender.competitionKey = competitionKey;
            PlayerSaveCompetitionKey(sender.id, competitionKey, true);

            replyMsg.result = ResultCode.OK;
            replyMsg.joinPalyerNum = competition.GetComPlayerCount();
            replyMsg.maxApplyNum = competition.maxApplyNum;
            SendProxyMsg(replyMsg, sender.proxyServerId);

            //下发人数变化(本人和不在线的不发)
            competition.comPlayerList.ForEach(player =>
            {
                if (player.summonerId != sender.id)
                {
                    Summoner summoner = SummonerManager.Instance.GetSummonerById(player.summonerId);
                    if (summoner != null)
                    {
                        OnRecvCompetitionPlayerApplyNum(summoner.id, summoner.proxyServerId, replyMsg.joinPalyerNum, replyMsg.maxApplyNum);
                    }
                }
            });
            //加机器人(凑一桌) --yishan
            //int max = competition.maxApplyNum / main.maxPlayerNum;
            //for (int i = 1; i < max; ++i)
            //{
            //    JoinMarketCompetition(new PlayerInfo((sender.id + (ulong)i * 100000), sender.nickName + i, sender.userId + i, sender.ip, sender.sex), competitionKey);
            //}
        }
        //这个接口用来增加机器人用
        private void JoinMarketCompetition(PlayerInfo playerInfo, int competitionKey)
        {
            Competition competition = CompetitionManager.Instance.GetCompetitionByKey(competitionKey);
            if (competition == null)
            {
                //无效的动态口令
                return;
            }
            if (competition.CheckPlayerExist(playerInfo.summonerId))
            {
                //已经存在了
                return;
            }
            if (competition.status != CompetitionStatus.ECS_Apply)
            {
                //不是报名状态
                return;
            }
            if (competition.CheckFullPlayerNum())
            {
                //人满了
                return;
            }
            competition.AddCompetitionPlayer(playerInfo.summonerId, playerInfo.nickName, playerInfo.userId, playerInfo.ip, playerInfo.sex);

            //下发人数变化(本人和不在线的不发)
            int joinPalyerNum = competition.GetComPlayerCount();
            competition.comPlayerList.ForEach(player =>
            {
                if (player.summonerId != playerInfo.summonerId)
                {
                    Summoner summoner = SummonerManager.Instance.GetSummonerById(player.summonerId);
                    if (summoner != null)
                    {
                        OnRecvCompetitionPlayerApplyNum(summoner.id, summoner.proxyServerId, joinPalyerNum, competition.maxApplyNum);
                    }
                }
            });
        }

        public void OnRequestQuitMarketCompetition(int peerId, bool inbound, object msg)
        {
            RequestQuitMarketCompetition_P2L reqMsg = msg as RequestQuitMarketCompetition_P2L;
            ReplyQuitMarketCompetition_L2P replyMsg = new ReplyQuitMarketCompetition_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.competitionKey == 0)
            {
                //没有参加比赛场
                ServerUtil.RecordLog(LogType.Debug, "OnRequestQuitMarketCompetition, 没有参加比赛场! summonerId = " + sender.id);
                replyMsg.result = ResultCode.PlayerNoJoinCompetition;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            Competition competition = CompetitionManager.Instance.GetCompetitionByKey(sender.competitionKey);
            if (competition == null)
            {
                //无效的动态口令
                ServerUtil.RecordLog(LogType.Debug, "OnRequestQuitMarketCompetition, 无效的动态口令! summonerId = " + sender.id + ", competitionKey" + sender.competitionKey);
                InitPlayerCompetitionKey(sender);
                replyMsg.result = ResultCode.CompetitionKeyInvalid;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (!competition.CheckPlayerExist(sender.id))
            {
                //玩家没在比赛场
                ServerUtil.RecordLog(LogType.Debug, "OnRequestQuitMarketCompetition, 玩家没在比赛场! summonerId = " + sender.id + ", competitionKey" + sender.competitionKey);
                InitPlayerCompetitionKey(sender);
                replyMsg.result = ResultCode.PlayerNoJoinCompetition;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (competition.status != CompetitionStatus.ECS_Apply)
            {
                //不是报名状态
                ServerUtil.RecordLog(LogType.Debug, "OnRequestQuitMarketCompetition, 不是报名状态! summonerId = " + sender.id);
                replyMsg.result = ResultCode.CompetitionNoApplyStatus;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (competition.CheckFullPlayerNum())
            {
                //人满了
                ServerUtil.RecordLog(LogType.Debug, "OnRequestQuitMarketCompetition, 人满了! summonerId = " + sender.id);
                replyMsg.result = ResultCode.CompetitionPlayerNumFull;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            //退出比赛场
            competition.RemoveCompetitionPlayer(reqMsg.summonerId);
            //清理玩家比赛场key
            sender.competitionKey = 0;
            PlayerSaveCompetitionKey(sender.id, sender.competitionKey, true);

            replyMsg.result = ResultCode.OK;
            SendProxyMsg(replyMsg, sender.proxyServerId);

            //下发人数变化(本人和不在线的不发)
            int joinPalyerNum = competition.GetComPlayerCount();
            competition.comPlayerList.ForEach(player =>
            {
                if (player.summonerId != sender.id)
                {
                    Summoner summoner = SummonerManager.Instance.GetSummonerById(player.summonerId);
                    if (summoner != null)
                    {
                        OnRecvCompetitionPlayerApplyNum(summoner.id, summoner.proxyServerId, joinPalyerNum, competition.maxApplyNum);
                    }
                }
            });
        }

        public void OnRequestCompetitionPlayerOnline(int peerId, bool inbound, object msg)
        {
            RequestCompetitionPlayerOnline_P2L reqMsg = msg as RequestCompetitionPlayerOnline_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;
            
            if (sender.competitionKey == 0)
            {
                //没有参加比赛场
                ServerUtil.RecordLog(LogType.Debug, "OnRequestCompetitionPlayerApplyNum, 没有参加比赛场! summonerId = " + sender.id);
                return;
            }
            Competition competition = CompetitionManager.Instance.GetCompetitionByKey(sender.competitionKey);
            if (competition == null || competition.status == CompetitionStatus.ECS_None || competition.status == CompetitionStatus.ECS_End)
            {
                //无效的动态口令
                ServerUtil.RecordLog(LogType.Debug, "OnRequestCompetitionPlayerApplyNum, 无效的动态口令! summonerId = " + sender.id + ", competitionKey" + sender.competitionKey);
                InitPlayerCompetitionKey(sender);
                return;
            }
            CompetitionPlayer player = competition.comPlayerList.Find(element => element.summonerId == sender.id);
            if (player == null || player.status == CompPlayerStatus.ECPS_None || player.status == CompPlayerStatus.ECPS_Over)
            {
                //玩家没在比赛场中
                ServerUtil.RecordLog(LogType.Debug, "OnRequestCompetitionPlayerApplyNum, 玩家没在比赛场! summonerId = " + sender.id);
                InitPlayerCompetitionKey(sender);
                return;
            }
            if (player.status == CompPlayerStatus.ECPS_Apply)
            {
                OnRecvCompetitionPlayerApplyNum(sender.id, sender.proxyServerId, competition.GetComPlayerCount(), competition.maxApplyNum);
            }
            else if (player.status == CompPlayerStatus.ECPS_Wait)
            {
                RecvCompetitionPlayerRank(sender.id, sender.proxyServerId, player.rank, competition.curAdmitNum, competition.houseCount, true);
            }
        }

        public void OnRequestDelMarketCompetition(int peerId, bool inbound, object msg)
        {
            RequestDelMarketCompetition_P2L reqMsg = msg as RequestDelMarketCompetition_P2L;
            ReplyDelMarketCompetition_L2P replyMsg = new ReplyDelMarketCompetition_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            Competition competition = CompetitionManager.Instance.GetCompetitionByKey(reqMsg.competitionKey);
            if (competition == null || competition.status != CompetitionStatus.ECS_Apply)
            {
                //无效的动态口令
                ServerUtil.RecordLog(LogType.Debug, "OnRequestDelMarketCompetition, 无效的动态口令! competitionKey" + reqMsg.competitionKey);
                replyMsg.result = ResultCode.CompetitionKeyInvalid;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            SpecialActivitiesConfigDB marketCfg = DBManager<SpecialActivitiesConfigDB>.Instance.GetSingleRecordInCache(e => e.ID == competition.marketId);
            if (marketCfg == null)
            {
                //配置有错
                ServerUtil.RecordLog(LogType.Debug, "OnRequestDelMarketCompetition, 配置有错! marketId = " + competition.marketId);
                replyMsg.result = ResultCode.WrongMarketConfig;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            string[] adminList = marketCfg.AdminId.Split(';');
            List<string> admins = new List<string>(adminList);
            if (!admins.Exists((e => e == reqMsg.summonerId.ToString())))
            {
                //无权限
                ServerUtil.RecordLog(LogType.Debug, "OnRequestDelMarketCompetition, 无权限! summonerId = " + reqMsg.summonerId);
                replyMsg.result = ResultCode.NoAuth;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            main.DelMarketsCompetition(reqMsg.competitionKey);

            replyMsg.result = ResultCode.OK;
            replyMsg.competitionKey = reqMsg.competitionKey;
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void OnRecvCompetitionPlayerApplyNum(ulong summonerId, int proxyServerId, int joinPalyerNum, int maxApplyNum)
        {
            RecvCompetitionPlayerApplyNum_L2P recvMsg = new RecvCompetitionPlayerApplyNum_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.joinPalyerNum = joinPalyerNum;
            recvMsg.maxApplyNum = maxApplyNum;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnRequestInitCompetitionKey(List<int> competitionKeyList)
        {
            RequestInitCompetitionKey_L2W reqMsg = new RequestInitCompetitionKey_L2W();
            reqMsg.competitionKeyList.AddRange(competitionKeyList);
            SendWorldMsg(reqMsg);
        }

        public void OnRequestMarketCompetitionInfo(int peerId, bool inbound, object msg)
        {
            RequestMarketCompetitionInfo_P2L reqMsg = msg as RequestMarketCompetitionInfo_P2L;
            ReplyMarketCompetitionInfo_L2P replyMsg = new ReplyMarketCompetitionInfo_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            SpecialActivitiesConfigDB marketCfg = DBManager<SpecialActivitiesConfigDB>.Instance.GetSingleRecordInCache(e => e.ID == reqMsg.marketId);
            if (marketCfg == null)
            {
                //配置有错
                ServerUtil.RecordLog(LogType.Debug, "OnRequestMarketCompetitionInfo, 配置有错! marketId = " + reqMsg.marketId);
                replyMsg.result = ResultCode.WrongMarketConfig;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            string[] adminList = marketCfg.AdminId.Split(';');
            List<string> admins = new List<string>(adminList);
            if (!admins.Exists((e => e == reqMsg.summonerId.ToString())))
            {
                //无权限
                ServerUtil.RecordLog(LogType.Debug, "OnRequestMarketCompetitionInfo, 无权限! summonerId = " + reqMsg.summonerId);
                replyMsg.result = ResultCode.NoAuth;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            Market market = CompetitionManager.Instance.GetMarket(reqMsg.marketId);
            if (market == null)
            {
                //本逻辑服务器找不到商家比赛场则从世界服务器找
                RequestMarketCompetitionBelong_L2W reqMsg_L2W = new RequestMarketCompetitionBelong_L2W();
                reqMsg_L2W.summonerId = sender.id;
                reqMsg_L2W.marketId = reqMsg.marketId;
                SendWorldMsg(reqMsg_L2W);
                return;
            }
            MarketCompetitionInfo(sender, market, replyMsg);
        }
        public void MarketCompetitionInfo(Summoner sender, Market market, ReplyMarketCompetitionInfo_L2P replyMsg = null)
        { 
            if (replyMsg == null)
            {
                replyMsg = new ReplyMarketCompetitionInfo_L2P();
                replyMsg.summonerId = sender.id;
            }
            List<MarketComNode> marketComList = new List<MarketComNode>();
            market.GetCompetitionInfo(marketComList);

            replyMsg.result = ResultCode.OK;
            replyMsg.marketId = market.marketId;
            replyMsg.marketCompetition = Serializer.tryCompressObject(marketComList);
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }

        public void OnRequestCompetitionPlayerInfo(int peerId, bool inbound, object msg)
        {
            RequestCompetitionPlayerInfo_P2L reqMsg = msg as RequestCompetitionPlayerInfo_P2L;
            ReplyCompetitionPlayerInfo_L2P replyMsg = new ReplyCompetitionPlayerInfo_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            Competition competition = CompetitionManager.Instance.GetCompetitionByKey(reqMsg.competitionKey);
            if (competition == null)
            {
                //无效的动态口令
                ServerUtil.RecordLog(LogType.Debug, "OnRequestCompetitionPlayerInfo, 无效的动态口令! competitionKey = " + reqMsg.competitionKey);
                replyMsg.result = ResultCode.CompetitionKeyInvalid;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            SpecialActivitiesConfigDB marketCfg = DBManager<SpecialActivitiesConfigDB>.Instance.GetSingleRecordInCache(e => e.ID == competition.marketId);
            if (marketCfg == null)
            {
                //配置有错
                ServerUtil.RecordLog(LogType.Debug, "OnRequestCompetitionPlayerInfo, 配置有错! marketId = " + competition.marketId);
                replyMsg.result = ResultCode.WrongMarketConfig;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            string[] adminList = marketCfg.AdminId.Split(';');
            List<string> admins = new List<string>(adminList);
            if (!admins.Exists((e => e == reqMsg.summonerId.ToString())))
            {
                //无权限
                ServerUtil.RecordLog(LogType.Debug, "OnRequestCompetitionPlayerInfo, 无权限! summonerId = " + reqMsg.summonerId);
                replyMsg.result = ResultCode.NoAuth;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (reqMsg.page <= 0)
            {
                //页数超标
                ServerUtil.RecordLog(LogType.Debug, "OnRequestCompetitionPlayerInfo, 页数超标! page = " + reqMsg.page);
                replyMsg.result = ResultCode.Wrong;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            replyMsg.joinPalyerNum = competition.GetComPlayerCount();
            //获取玩家
            List<ComPlayerNode> comPlayerList = new List<ComPlayerNode>();
            if (replyMsg.joinPalyerNum > 0)
            {
                int min = (reqMsg.page - 1) * main.competitionPlayerPageNum;
                int max = reqMsg.page * main.competitionPlayerPageNum;
                competition.GetComPlayerInfo(min, max, comPlayerList);
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.competitionKey = reqMsg.competitionKey;
            replyMsg.page = reqMsg.page;
            replyMsg.competitionPlayer = Serializer.tryCompressObject(comPlayerList);
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void RecvCompetitionPlayerRank(ulong summonerId, int proxyServerId, int rank, int admitNum, int houseCount, bool bOnline = false)
        {
            RecvCompetitionPlayerRank_L2P recvMsg_L2P = new RecvCompetitionPlayerRank_L2P();
            recvMsg_L2P.summonerId = summonerId;
            recvMsg_L2P.rank = rank;
            recvMsg_L2P.admitNum = admitNum;
            recvMsg_L2P.houseCount = houseCount;
            recvMsg_L2P.bOnline = bOnline;
            SendProxyMsg(recvMsg_L2P, proxyServerId);
        }
        public void RecvCompetitionPlayerOverRank(ulong summonerId, int proxyServerId, int rank, TicketsNode ticketsNode = null)
        {
            RecvCompetitionPlayerOverRank_L2P recvMsg_L2P = new RecvCompetitionPlayerOverRank_L2P();
            recvMsg_L2P.summonerId = summonerId;
            recvMsg_L2P.rank = rank;
            recvMsg_L2P.ticketsNode = ticketsNode;
            SendProxyMsg(recvMsg_L2P, proxyServerId);
        }
        public void RecvQuitMarketCompetition(ulong summonerId, int proxyServerId)
        {
            ReplyQuitMarketCompetition_L2P replyMsg = new ReplyQuitMarketCompetition_L2P();
            replyMsg.result = ResultCode.OK;
            replyMsg.summonerId = summonerId;
            SendProxyMsg(replyMsg, proxyServerId);
        }
        public void OnReplyMarketCompetitionBelong(int peerId, bool inbound, object msg)
        {
            ReplyMarketCompetitionBelong_W2L replyMsg_W2L = msg as ReplyMarketCompetitionBelong_W2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_W2L.summonerId);
            if (sender == null) return;

            if (replyMsg_W2L.logicId <= 0)
            {
                //商家无比赛场
                ReplyMarketCompetitionInfo_L2P replyMsg = new ReplyMarketCompetitionInfo_L2P();
                ServerUtil.RecordLog(LogType.Debug, "OnRequestMarketCompetitionInfo, 商家无比赛场! marketId = " + replyMsg_W2L.marketId);
                replyMsg.result = ResultCode.MarketCompetitionClosed;
                replyMsg.summonerId = replyMsg_W2L.summonerId;
                SendProxyMsg(replyMsg, sender.proxyServerId);
            }
            else
            {
                //在别的逻辑服务器找到了房间则将自己的信息打包通过自己的网关转发给商家比赛场对应的逻辑服务器
                //然后从当前旧逻辑服退出，在目标逻辑服务器继续商家比赛场信息的操作
                TransmitPlayerInfo_L2P transmitMsg = new TransmitPlayerInfo_L2P();
                transmitMsg.summoner = ServerUtil.Serialize(sender);
                transmitMsg.summonerId = sender.id;
                transmitMsg.type = TransmitPlayerType.ETP_MarketCompetition;
                transmitMsg.parameter = Serializer.trySerializerObject(replyMsg_W2L.marketId);
                transmitMsg.targetLogic = replyMsg_W2L.logicId;
                SendProxyMsg(transmitMsg, sender.proxyServerId);

                SummonerManager.Instance.RemoveSummoner(sender.id);
            }
        }
        ///////////////////////////////////////////////////////////////////
        public void PlayerSaveCompetitionKey(ulong summonerId, int competitionKey = 0, bool bOnlySaveDB = false)
        {
            ModuleManager.Get<MainCityMain>().PlayerSaveCompetitionKey(summonerId, competitionKey, bOnlySaveDB);
        }
        public void InitPlayerCompetitionKey(Summoner sender)
        {
            sender.competitionKey = 0;
            PlayerSaveCompetitionKey(sender.id, sender.competitionKey, true);
        }
        public void OnRequestSaveTickets(ulong summonerId, TicketsNode ticketsNode)
        {
            if (ticketsNode == null)
            {
                return;
            }
            RequestSaveTickets_L2D reqMsg_L2D = new RequestSaveTickets_L2D();
            reqMsg_L2D.summonerId = summonerId;
            reqMsg_L2D.ticketsNode = ticketsNode;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestDelTickets(string userId, ulong ticketsOnlyId)
        {
            List<ulong> ticketsOnlyIdList = new List<ulong>();
            ticketsOnlyIdList.Add(ticketsOnlyId);
            OnRequestDelTickets(userId, ticketsOnlyIdList);
        }
        public void OnRequestDelTickets(string userId, List<ulong> ticketsOnlyIdList)
        {
            if (ticketsOnlyIdList == null && ticketsOnlyIdList.Count == 0)
            {
                return;
            }
            RequestDelTickets_L2D reqMsg_L2D = new RequestDelTickets_L2D();
            reqMsg_L2D.userId = userId;
            reqMsg_L2D.ticketsOnlyIdList.AddRange(ticketsOnlyIdList);
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestUseTickets(string userId, ulong ticketsOnlyId)
        {
            RequestUseTickets_L2D reqMsg_L2D = new RequestUseTickets_L2D();
            reqMsg_L2D.userId = userId;
            reqMsg_L2D.ticketsOnlyId = ticketsOnlyId;
            SendDBMsg(reqMsg_L2D);
        }
    }
}

