#if RUNFAST
using LegendProtocol;
using LegendServerProxy.Core;
using LegendServerProxy.Distributed;
using LegendServerProxy.Login;
using System;
using System.Text;
using LegendServerProxy.ServiceBox;

namespace LegendServerProxy.RunFast
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
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestCreateRunFastHouse_T2P reqMsg_T2P = msg as RequestCreateRunFastHouse_T2P;
            RequestCreateRunFastHouse_P2L reqMsg_P2L = new RequestCreateRunFastHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.maxBureau = reqMsg_T2P.maxBureau;
            reqMsg_P2L.runFastType = reqMsg_T2P.runFastType;
            reqMsg_P2L.maxPlayerNum = reqMsg_T2P.maxPlayerNum;
            reqMsg_P2L.housePropertyType = reqMsg_T2P.housePropertyType;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }   
        public void OnReplyCreateRunFastHouse(int peerId, bool inbound, object msg)
        {
            ReplyCreateRunFastHouse_L2P replyMsg_L2P = msg as ReplyCreateRunFastHouse_L2P;
            ReplyCreateRunFastHouse_P2T replyMsg_P2T = new ReplyCreateRunFastHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.maxBureau = replyMsg_L2P.maxBureau;
            replyMsg_P2T.runFastType = replyMsg_L2P.runFastType;
            replyMsg_P2T.houseId = replyMsg_L2P.houseId;
            replyMsg_P2T.allIntegral = replyMsg_L2P.allIntegral;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.maxPlayerNum = replyMsg_L2P.maxPlayerNum;
            replyMsg_P2T.housePropertyType = replyMsg_L2P.housePropertyType;
            replyMsg_P2T.businessId = replyMsg_L2P.businessId;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);

            //盒子测试用 -- yishan
            if(ModuleManager.Get<ServiceBoxMain>().bOpenBoxAutoPlaying)
            {
                ModuleManager.Get<ServiceBoxMain>().ReplyCreateHouse(replyMsg_L2P.businessId, replyMsg_L2P.summonerId, replyMsg_L2P.houseId);
            }
        }
        public void OnReqJoinRunFastHouse(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestJoinRunFastHouse_T2P reqMsg_T2P = msg as RequestJoinRunFastHouse_T2P;
            RequestJoinRunFastHouse_P2L reqMsg_P2L = new RequestJoinRunFastHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.houseId = reqMsg_T2P.houseId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyJoinRunFastHouse(int peerId, bool inbound, object msg)
        {
            ReplyJoinRunFastHouse_L2P replyMsg_L2P = msg as ReplyJoinRunFastHouse_L2P;
            ReplyJoinRunFastHouse_P2T replyMsg_P2T = new ReplyJoinRunFastHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.maxBureau = replyMsg_L2P.maxBureau;
            replyMsg_P2T.runFastType = replyMsg_L2P.runFastType;
            replyMsg_P2T.houseId = replyMsg_L2P.houseId;
            replyMsg_P2T.allIntegral = replyMsg_L2P.allIntegral;
            replyMsg_P2T.playerShowList.AddRange(replyMsg_L2P.playerShowList);
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.maxPlayerNum = replyMsg_L2P.maxPlayerNum;
            replyMsg_P2T.housePropertyType = replyMsg_L2P.housePropertyType;
            replyMsg_P2T.businessId = replyMsg_L2P.businessId;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;
            replyMsg_P2T.myIndex = replyMsg_L2P.myIndex;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
            
            //盒子测试用 -- yishan
            if(ModuleManager.Get<ServiceBoxMain>().bOpenBoxAutoPlaying)
            {
                ModuleManager.Get<ServiceBoxMain>().ReplyJoinHouse(replyMsg_L2P.summonerId, replyMsg_L2P.result == ResultCode.OK, replyMsg_L2P.houseId);
            }
        }
        public void OnRecvJoinRunFastHouse(int peerId, bool inbound, object msg)
        {
            RecvJoinRunFastHouse_L2P replyMsg_L2P = msg as RecvJoinRunFastHouse_L2P;
            RecvJoinRunFastHouse_P2T replyMsg_P2T = new RecvJoinRunFastHouse_P2T();
            replyMsg_P2T.playerShow = replyMsg_L2P.playerShow;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvBeginRunFast(int peerId, bool inbound, object msg)
        {
            RecvBeginRunFast_L2P replyMsg_L2P = msg as RecvBeginRunFast_L2P;
            RecvBeginRunFast_P2T replyMsg_P2T = new RecvBeginRunFast_P2T();
            replyMsg_P2T.currentBureau = replyMsg_L2P.currentBureau;
            replyMsg_P2T.currentShowCard = replyMsg_L2P.currentShowCard;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;
            replyMsg_P2T.cardList.AddRange(replyMsg_L2P.cardList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqShowRunFastCard(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestShowRunFastCard_T2P reqMsg_T2P = msg as RequestShowRunFastCard_T2P;
            RequestShowRunFastCard_P2L reqMsg_P2L = new RequestShowRunFastCard_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.cardList.AddRange(reqMsg_T2P.cardList);

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyShowRunFastCard(int peerId, bool inbound, object msg)
        {
            ReplyShowRunFastCard_L2P replyMsg_L2P = msg as ReplyShowRunFastCard_L2P;
            ReplyShowRunFastCard_P2T replyMsg_P2T = new ReplyShowRunFastCard_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.cardList.AddRange(replyMsg_L2P.cardList);
            replyMsg_P2T.currentShowCard = replyMsg_L2P.currentShowCard;
            replyMsg_P2T.bDanZhangFalg = replyMsg_L2P.bDanZhangFalg;
            replyMsg_P2T.pokerGroupType = replyMsg_L2P.pokerGroupType;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvShowRunFastCard(int peerId, bool inbound, object msg)
        {
            RecvShowRunFastCard_L2P replyMsg_L2P = msg as RecvShowRunFastCard_L2P;
            RecvShowRunFastCard_P2T replyMsg_P2T = new RecvShowRunFastCard_P2T();
            replyMsg_P2T.currentShowCard = replyMsg_L2P.currentShowCard;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;
            replyMsg_P2T.currentWhoPlay = replyMsg_L2P.currentWhoPlay;
            replyMsg_P2T.cardList.AddRange(replyMsg_L2P.cardList);
            replyMsg_P2T.bDanZhangFalg = replyMsg_L2P.bDanZhangFalg;
            replyMsg_P2T.pokerGroupType = replyMsg_L2P.pokerGroupType;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqPassRunFastCard(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestPassRunFastCard_T2P reqMsg_T2P = msg as RequestPassRunFastCard_T2P;
            RequestPassRunFastCard_P2L reqMsg_P2L = new RequestPassRunFastCard_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyPassRunFastCard(int peerId, bool inbound, object msg)
        {
            ReplyPassRunFastCard_L2P replyMsg_L2P = msg as ReplyPassRunFastCard_L2P;
            ReplyPassRunFastCard_P2T replyMsg_P2T = new ReplyPassRunFastCard_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.currentShowCard = replyMsg_L2P.currentShowCard;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvPassRunFastCard(int peerId, bool inbound, object msg)
        {
            RecvPassRunFastCard_L2P replyMsg_L2P = msg as RecvPassRunFastCard_L2P;
            RecvPassRunFastCard_P2T replyMsg_P2T = new RecvPassRunFastCard_P2T();
            replyMsg_P2T.currentShowCard = replyMsg_L2P.currentShowCard;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqQuitRunFastHouse(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestQuitRunFastHouse_T2P reqMsg_T2P = msg as RequestQuitRunFastHouse_T2P;
            RequestQuitRunFastHouse_P2L reqMsg_P2L = new RequestQuitRunFastHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyQuitRunFastHouse(int peerId, bool inbound, object msg)
        {
            ReplyQuitRunFastHouse_L2P replyMsg_L2P = msg as ReplyQuitRunFastHouse_L2P;
            ReplyQuitRunFastHouse_P2T replyMsg_P2T = new ReplyQuitRunFastHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.bVote = replyMsg_L2P.bVote;
            replyMsg_P2T.dissolveVoteTime = replyMsg_L2P.dissolveVoteTime;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvQuitRunFastHouse(int peerId, bool inbound, object msg)
        {
            RecvQuitRunFastHouse_L2P recvMsg_L2P = msg as RecvQuitRunFastHouse_L2P;
            RecvQuitRunFastHouse_P2T recvMsg_P2T = new RecvQuitRunFastHouse_P2T();
            recvMsg_P2T.bVote = recvMsg_L2P.bVote;
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.dissolveVoteTime = recvMsg_L2P.dissolveVoteTime;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }        
        public void OnRecvLeaveRunFastHouse(int peerId, bool inbound, object msg)
        {
            RecvLeaveRunFastHouse_L2P replyMsg_L2P = msg as RecvLeaveRunFastHouse_L2P;
            RecvLeaveRunFastHouse_P2T replyMsg_P2T = new RecvLeaveRunFastHouse_P2T();
            replyMsg_P2T.leaveIndex = replyMsg_L2P.leaveIndex;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvZhaDanIntegral(int peerId, bool inbound, object msg)
        {
            RecvZhaDanIntegral_L2P replyMsg_L2P = msg as RecvZhaDanIntegral_L2P;
            RecvZhaDanIntegral_P2T replyMsg_P2T = new RecvZhaDanIntegral_P2T();
            replyMsg_P2T.playerIntegralList.AddRange(replyMsg_L2P.playerIntegralList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvSettlementRunFast(int peerId, bool inbound, object msg)
        {
            RecvSettlementRunFast_L2P replyMsg_L2P = msg as RecvSettlementRunFast_L2P;
            RecvSettlementRunFast_P2T replyMsg_P2T = new RecvSettlementRunFast_P2T();
            replyMsg_P2T.settlementType = replyMsg_L2P.settlementType;
            replyMsg_P2T.playerSettlement = replyMsg_L2P.playerSettlement;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvEndSettlementRunFast(int peerId, bool inbound, object msg)
        {
            RecvEndSettlementRunFast_L2P replyMsg_L2P = msg as RecvEndSettlementRunFast_L2P;
            RecvEndSettlementRunFast_P2T replyMsg_P2T = new RecvEndSettlementRunFast_P2T();
            replyMsg_P2T.houseStatus = replyMsg_L2P.houseStatus;
            replyMsg_P2T.allIntegral = replyMsg_L2P.allIntegral;
            replyMsg_P2T.ticketsNode = replyMsg_L2P.ticketsNode;
            replyMsg_P2T.playerEndSettlementList.AddRange(replyMsg_L2P.playerEndSettlementList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqReadyRunFastHouse(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestReadyRunFastHouse_T2P reqMsg_T2P = msg as RequestReadyRunFastHouse_T2P;
            RequestReadyRunFastHouse_P2L reqMsg_P2L = new RequestReadyRunFastHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyReadyRunFastHouse(int peerId, bool inbound, object msg)
        {
            ReplyReadyRunFastHouse_L2P replyMsg_L2P = msg as ReplyReadyRunFastHouse_L2P;
            ReplyReadyRunFastHouse_P2T replyMsg_P2T = new ReplyReadyRunFastHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvReadyRunFastHouse(int peerId, bool inbound, object msg)
        {
            RecvReadyRunFastHouse_L2P replyMsg_L2P = msg as RecvReadyRunFastHouse_L2P;
            RecvReadyRunFastHouse_P2T replyMsg_P2T = new RecvReadyRunFastHouse_P2T();
            replyMsg_P2T.readyIndex = replyMsg_L2P.readyIndex;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqRunFastHouseInfo(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestRunFastHouseInfo_T2P reqMsg_T2P = msg as RequestRunFastHouseInfo_T2P;
            RequestRunFastHouseInfo_P2L reqMsg_P2L = new RequestRunFastHouseInfo_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyRunFastHouseInfo(int peerId, bool inbound, object msg)
        {
            ReplyRunFastHouseInfo_L2P replyMsg_L2P = msg as ReplyRunFastHouseInfo_L2P;
            ReplyRunFastHouseInfo_P2T replyMsg_P2T = new ReplyRunFastHouseInfo_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.houseCardId = replyMsg_L2P.houseCardId;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.runFastType = replyMsg_L2P.runFastType;
            replyMsg_P2T.currentBureau = replyMsg_L2P.currentBureau;
            replyMsg_P2T.maxBureau = replyMsg_L2P.maxBureau;
            replyMsg_P2T.currentShowCard = replyMsg_L2P.currentShowCard;
            replyMsg_P2T.currentWhoPlay = replyMsg_L2P.currentWhoPlay;
            replyMsg_P2T.houseStatus = replyMsg_L2P.houseStatus;
            replyMsg_P2T.pokerGroupType = replyMsg_L2P.pokerGroupType;
            replyMsg_P2T.houseCardList.AddRange(replyMsg_L2P.houseCardList);
            replyMsg_P2T.runFastOnlineNode = replyMsg_L2P.runFastOnlineNode;
            replyMsg_P2T.houseVoteTime = replyMsg_L2P.houseVoteTime;
            replyMsg_P2T.maxPlayerNum = replyMsg_L2P.maxPlayerNum;
            replyMsg_P2T.housePropertyType = replyMsg_L2P.housePropertyType;
            replyMsg_P2T.businessId = replyMsg_L2P.businessId;
            replyMsg_P2T.zhuangPlayerIndex = replyMsg_L2P.zhuangPlayerIndex;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqDissolveHouseVote(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestDissolveHouseVote_T2P reqMsg_T2P = msg as RequestDissolveHouseVote_T2P;
            RequestDissolveHouseVote_P2L reqMsg_P2L = new RequestDissolveHouseVote_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.voteStatus = reqMsg_T2P.voteStatus;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyDissolveHouseVote(int peerId, bool inbound, object msg)
        {
            ReplyDissolveHouseVote_L2P replyMsg_L2P = msg as ReplyDissolveHouseVote_L2P;
            ReplyDissolveHouseVote_P2T replyMsg_P2T = new ReplyDissolveHouseVote_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.voteStatus = replyMsg_L2P.voteStatus;
            replyMsg_P2T.houseVoteStatus = replyMsg_L2P.houseVoteStatus;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvDissolveHouseVote(int peerId, bool inbound, object msg)
        {
            RecvDissolveHouseVote_L2P replyMsg_L2P = msg as RecvDissolveHouseVote_L2P;
            RecvDissolveHouseVote_P2T replyMsg_P2T = new RecvDissolveHouseVote_P2T();
            replyMsg_P2T.index = replyMsg_L2P.index;
            replyMsg_P2T.voteStatus = replyMsg_L2P.voteStatus;
            replyMsg_P2T.houseVoteStatus = replyMsg_L2P.houseVoteStatus;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqRunFastOverallRecord(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestRunFastOverallRecord_T2P reqMsg_T2P = msg as RequestRunFastOverallRecord_T2P;
            RequestRunFastOverallRecord_P2L reqMsg_P2L = new RequestRunFastOverallRecord_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyRunFastOverallRecord(int peerId, bool inbound, object msg)
        {
            ReplyRunFastOverallRecord_L2P replyMsg_L2P = msg as ReplyRunFastOverallRecord_L2P;
            ReplyRunFastOverallRecord_P2T replyMsg_P2T = new ReplyRunFastOverallRecord_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.overallRecord = replyMsg_L2P.overallRecord;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqRunFastBureauRecord(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestRunFastBureauRecord_T2P reqMsg_T2P = msg as RequestRunFastBureauRecord_T2P;
            RequestRunFastBureauRecord_P2L reqMsg_P2L = new RequestRunFastBureauRecord_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.onlyHouseId = reqMsg_T2P.onlyHouseId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyRunFastBureauRecord(int peerId, bool inbound, object msg)
        {
            ReplyRunFastBureauRecord_L2P replyMsg_L2P = msg as ReplyRunFastBureauRecord_L2P;
            ReplyRunFastBureauRecord_P2T replyMsg_P2T = new ReplyRunFastBureauRecord_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.bureauRecord = replyMsg_L2P.bureauRecord;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqRunFastBureauPlayback(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestRunFastBureauPlayback_T2P reqMsg_T2P = msg as RequestRunFastBureauPlayback_T2P;
            RequestRunFastBureauPlayback_P2L reqMsg_P2L = new RequestRunFastBureauPlayback_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.onlyHouseId = reqMsg_T2P.onlyHouseId;
            reqMsg_P2L.bureau = reqMsg_T2P.bureau;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyRunFastBureauPlayback(int peerId, bool inbound, object msg)
        {
            ReplyRunFastBureauPlayback_L2P replyMsg_L2P = msg as ReplyRunFastBureauPlayback_L2P;
            ReplyRunFastBureauPlayback_P2T replyMsg_P2T = new ReplyRunFastBureauPlayback_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.bureau = replyMsg_L2P.bureau;
            replyMsg_P2T.playerCard = replyMsg_L2P.playerCard;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
    }
}
#endif

