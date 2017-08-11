using System.Collections.Generic;
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServerMarketManager;
using LegendServerWorldDefine;
using LegendServerWorld.Distributed;

namespace LegendServerWorld.SpecialActivities
{
    public class SpecialActivitiesMsgProxy : ServerMsgProxy
    {
        private SpecialActivitiesMain main;

        public SpecialActivitiesMsgProxy(SpecialActivitiesMain main)
            : base(main.root)
        {
            this.main = main;
        }

        public void OnRequestMarketKey(int peerId, bool inbound, object msg)
        {
            RequestMarketKey_P2W reqMsg = msg as RequestMarketKey_P2W;

            ReplyMarketKey_W2P replyMsg = new ReplyMarketKey_W2P();
            replyMsg.requesterPeerId = reqMsg.requesterPeerId;
            replyMsg.marketId = reqMsg.marketId;

            SpecialActivitiesConfigDB marketCfg = DBManager<SpecialActivitiesConfigDB>.Instance.GetSingleRecordInCache(e => e.ID == reqMsg.marketId);
            if (marketCfg == null)
            {
                //配置有错
                replyMsg.result = ResultCode.WrongMarketConfig;
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            string[] adminList = marketCfg.AdminId.Split(';');
            List<string> admins = new List<string>(adminList);
            if (!admins.Exists((e => e == reqMsg.requesterSummonerId.ToString())))
            {
                //无权限
                replyMsg.result = ResultCode.NoAuth;
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            Market market = MarketManager.Instance.TryGetMarketByID(reqMsg.marketId);
            if (market != null)
            {
                if (reqMsg.build)
                {
                    //此商场正在营业，只产生新的口令，旧的口令在生存期内仍然有效
                    MarketKey marketKey = market.BuildKey(MarketKeyType.EMK_Ordinary, marketCfg.DynamicPasswordIndate);
                    replyMsg.key = marketKey.key;
                }
                else
                {
                    //此商场正在营业，返回最新口令
                    replyMsg.key = market.latestKey;
                }
                replyMsg.result = ResultCode.OK;
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            else
            {
                if (reqMsg.build)
                {
                    int logicId = ModuleManager.Get<DistributedMain>().GetLogicId();
                    if (logicId == 0)
                    {
                        replyMsg.result = ResultCode.Wrong;
                        SendMsg(peerId, inbound, replyMsg);
                        return;
                    }
                    //此商场此刻开业，产生新口令
                    market = new Market(MarketKeyType.EMK_Ordinary, reqMsg.marketId, logicId, marketCfg.DynamicPasswordIndate);
                    bool res = MarketManager.Instance.TryAddMarket(reqMsg.marketId, market);
                    if (res)
                    {
                        replyMsg.key = market.latestKey;
                        replyMsg.result = ResultCode.OK;
                        SendMsg(peerId, inbound, replyMsg);
                        return;
                    }
                    else
                    {
                        replyMsg.result = ResultCode.Wrong;
                        SendMsg(peerId, inbound, replyMsg);
                        return;
                    }
                }
                else
                {
                    //此商场未营业，无法返回口令
                    replyMsg.result = ResultCode.MarketClosed;
                    SendMsg(peerId, inbound, replyMsg);
                    return;
                }
            }
        }
        public void OnRequestJoinMarket(int peerId, bool inbound, object msg)
        {
            RequestJoinMarket_P2W reqMsg = msg as RequestJoinMarket_P2W;

            MarketKey keyInfo = new MarketKey();
            Market market = MarketManager.Instance.TryGetMarketByKey(reqMsg.key, out keyInfo);
            if (market == null || keyInfo == null || keyInfo.keyType == MarketKeyType.EMK_None)
            {
                //此口令无效
                ReplyJoinMarket_W2P replyMsg_W2P = new ReplyJoinMarket_W2P();
                replyMsg_W2P.result = ResultCode.MarketKeyInvalid;
                replyMsg_W2P.requesterPeerId = reqMsg.requesterPeerId;
                SendMsg(peerId, inbound, replyMsg_W2P);
                return;
            }

            if (keyInfo.keyType == MarketKeyType.EMK_Ordinary)
            {
                if (!keyInfo.CanPlayerJoin(reqMsg.requesterSummonerId))
                {
                    //该玩家对此口令的使用次数已经达到上限
                    ReplyJoinMarket_W2P replyMsg_W2P = new ReplyJoinMarket_W2P();
                    replyMsg_W2P.result = ResultCode.MarketKeyUsedCountLimit;
                    replyMsg_W2P.requesterPeerId = reqMsg.requesterPeerId;
                    SendMsg(peerId, inbound, replyMsg_W2P);
                    return;
                }

                //记录该玩家使用了一次该口令
                keyInfo.OnPlayerJoin(reqMsg.requesterSummonerId);
            }

            //通知给目标游戏的逻辑服务器处理后续玩法
            NotifyJoinMarket_W2L notifyMsg = new NotifyJoinMarket_W2L();
            notifyMsg.requesterSummonerId = reqMsg.requesterSummonerId;
            notifyMsg.logicId = market.logicId;
            notifyMsg.joinMarketNode = new JoinMarketNode();
            notifyMsg.joinMarketNode.competitionKey = reqMsg.key;
            notifyMsg.joinMarketNode.marketId = market.id;
            notifyMsg.joinMarketNode.keyType = keyInfo.keyType;

            Summoner requester = SummonerManager.Instance.GetSummoner(reqMsg.requesterSummonerId);
            if (requester != null)
            {
                SendLogicMsg(notifyMsg, requester.logicId);
                //玩家切换逻辑服Id
                if (requester.logicId != market.logicId)
                {
                    requester.logicId = market.logicId;
                }
            }
        }
        public void OnRequestInitCompetitionKey(int peerId, bool inbound, object msg)
        {
            RequestInitCompetitionKey_L2W reqMsg = msg as RequestInitCompetitionKey_L2W;

            reqMsg.competitionKeyList.ForEach(competitionKey =>
            {
                MarketManager.Instance.TryRemoveMarketKey(competitionKey);
            });
            //发送DB清理口令
            OnNotifyDelCompetition(reqMsg.competitionKeyList);
        }
        public void OnRequestCreateMarketCompetition(int peerId, bool inbound, object msg)
        {
            RequestCreateMarketCompetition_P2W reqMsg_P2W = msg as RequestCreateMarketCompetition_P2W;

            SpecialActivitiesConfigDB marketCfg = DBManager<SpecialActivitiesConfigDB>.Instance.GetSingleRecordInCache(e => e.ID == reqMsg_P2W.marketId);
            if (marketCfg == null)
            {
                //配置有错
                ReplyCreateMarketCompetition_W2P replyMsg = new ReplyCreateMarketCompetition_W2P();
                replyMsg.requesterSummonerId = reqMsg_P2W.requesterSummonerId;
                replyMsg.result = ResultCode.WrongMarketConfig;
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            string[] adminList = marketCfg.AdminId.Split(';');
            List<string> admins = new List<string>(adminList);
            if (!admins.Exists((e => e == reqMsg_P2W.requesterSummonerId.ToString())))
            {
                //无权限
                ReplyCreateMarketCompetition_W2P replyMsg = new ReplyCreateMarketCompetition_W2P();
                replyMsg.requesterSummonerId = reqMsg_P2W.requesterSummonerId;
                replyMsg.result = ResultCode.NoAuth;
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            MarketKey keyInfo = null;
            Market market = MarketManager.Instance.TryGetMarketByID(reqMsg_P2W.marketId);
            if (market == null)
            {
                int logicId = ModuleManager.Get<DistributedMain>().GetLogicId();
                if (logicId == 0)
                {
                    ReplyCreateMarketCompetition_W2P replyMsg = new ReplyCreateMarketCompetition_W2P();
                    replyMsg.requesterSummonerId = reqMsg_P2W.requesterSummonerId;
                    replyMsg.result = ResultCode.Wrong;
                    SendMsg(peerId, inbound, replyMsg);
                    return;
                }
                //第一次创建商家
                market = new Market(logicId);
                keyInfo = market.BuildKey(MarketKeyType.EMK_Competition, reqMsg_P2W.marketId, marketCfg.DynamicPasswordIndate);
                if (!MarketManager.Instance.TryAddMarket(reqMsg_P2W.marketId, market))
                {
                    //加入商家有误
                    ReplyCreateMarketCompetition_W2P replyMsg = new ReplyCreateMarketCompetition_W2P();
                    replyMsg.requesterSummonerId = reqMsg_P2W.requesterSummonerId;
                    replyMsg.result = ResultCode.Wrong;
                    SendMsg(peerId, inbound, replyMsg);
                    return;
                }
            }
            else
            {
                //创建一个新的口令
                keyInfo = market.BuildKey(MarketKeyType.EMK_Competition, marketCfg.DynamicPasswordIndate);
            }
            if (keyInfo == null)
            {
                //创建新的口令失败
                ReplyCreateMarketCompetition_W2P replyMsg = new ReplyCreateMarketCompetition_W2P();
                replyMsg.requesterSummonerId = reqMsg_P2W.requesterSummonerId;
                replyMsg.result = ResultCode.Wrong;
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            Summoner requester = SummonerManager.Instance.GetSummoner(reqMsg_P2W.requesterSummonerId);
            if (requester == null)
            {
                //请求的玩家没有在world注册
                ReplyCreateMarketCompetition_W2P replyMsg = new ReplyCreateMarketCompetition_W2P();
                replyMsg.requesterSummonerId = reqMsg_P2W.requesterSummonerId;
                replyMsg.result = ResultCode.Wrong;
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            RequestCreateMarketCompetition_W2L reqMsg_W2L = new RequestCreateMarketCompetition_W2L();
            reqMsg_W2L.requesterSummonerId = reqMsg_P2W.requesterSummonerId;
            reqMsg_W2L.node = new CreateCompetitionNode();
            reqMsg_W2L.node.marketId = reqMsg_P2W.marketId;
            reqMsg_W2L.node.competitionKey = keyInfo.key;
            reqMsg_W2L.node.maxGameBureau = reqMsg_P2W.maxGameBureau;
            reqMsg_W2L.node.maxApplyNum = reqMsg_P2W.maxApplyNum;
            reqMsg_W2L.node.firstAdmitNum = reqMsg_P2W.firstAdmitNum;
            reqMsg_W2L.logicId = market.logicId;
            SendLogicMsg(reqMsg_W2L, requester.logicId);
        }
        public void OnReplyCreateMarketCompetition(int peerId, bool inbound, object msg)
        {
            ReplyCreateMarketCompetition_L2W replyMsg_L2W = msg as ReplyCreateMarketCompetition_L2W;
            ReplyCreateMarketCompetition_W2P replyMsg_W2P = new ReplyCreateMarketCompetition_W2P();

            if (replyMsg_L2W.result != ResultCode.OK)
            {
                //没有创建成功,删除创建的口令
                MarketManager.Instance.TryRemoveMarketKey(replyMsg_L2W.competitionKey);
            }
            else
            {
                //创建成功保存DB用于登陆的时候识别所在逻辑服
                OnNotifyCreateCompetition(replyMsg_L2W.competitionKey, replyMsg_L2W.logicId);
            }

            replyMsg_W2P.result = replyMsg_L2W.result;
            replyMsg_W2P.requesterSummonerId = replyMsg_L2W.requesterSummonerId;
            replyMsg_W2P.marketId = replyMsg_L2W.marketId;
            replyMsg_W2P.competitionKey = replyMsg_L2W.competitionKey;
            replyMsg_W2P.maxApplyNum = replyMsg_L2W.maxApplyNum;
            replyMsg_W2P.maxGameBureau = replyMsg_L2W.maxGameBureau;
            replyMsg_W2P.firstAdmitNum = replyMsg_L2W.firstAdmitNum;
            replyMsg_W2P.createTime = replyMsg_L2W.createTime;

            Summoner summoner = SummonerManager.Instance.GetSummoner(replyMsg_L2W.requesterSummonerId);
            if (summoner != null)
            {
                SendProxyMsg(replyMsg_W2P, summoner.proxyId);
                //修改玩家当前逻辑服Id
                if (replyMsg_L2W.logicId > 0 && summoner.logicId != replyMsg_L2W.logicId)
                {
                    summoner.logicId = replyMsg_L2W.logicId;
                }
            }
        }
        public void OnRequestMarketCompetitionBelong(int peerId, bool inbound, object msg)
        {
            RequestMarketCompetitionBelong_L2W reqMsg = msg as RequestMarketCompetitionBelong_L2W;

            Summoner sender = SummonerManager.Instance.GetSummoner(reqMsg.summonerId);
            if (sender == null) return;

            ReplyMarketCompetitionBelong_W2L replyMsg = new ReplyMarketCompetitionBelong_W2L();
            replyMsg.summonerId = reqMsg.summonerId;
            replyMsg.marketId = reqMsg.marketId;
            replyMsg.logicId = MarketManager.Instance.GetMarketCompetitionLogicId(reqMsg.marketId);
            SendMsg(peerId, inbound, replyMsg);

            if (replyMsg.logicId > 0)
            {
                //变更新的逻辑服，因为旧逻辑服即将迁移该Summoner
                sender.logicId = replyMsg.logicId;
            }

        }
        public void OnNotifyCreateCompetition(int competitionKey, int logicId)
        {
            NotifyCreateCompetition_W2D notify_W2D = new NotifyCreateCompetition_W2D();
            notify_W2D.competitionKey = competitionKey;
            notify_W2D.logicId = logicId;
            BroadCastDBMsg(notify_W2D);
        }
        public void OnNotifyDelCompetition(List<int> competitionKeyList)
        {
            NotifyDelCompetition_W2D notify_W2D = new NotifyDelCompetition_W2D();
            notify_W2D.competitionKeyList.AddRange(competitionKeyList);
            BroadCastDBMsg(notify_W2D);
        }
    }
}

