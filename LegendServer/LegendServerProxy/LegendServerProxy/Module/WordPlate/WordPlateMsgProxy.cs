#if WORDPLATE
using LegendProtocol;
using LegendServerProxy.Core;
using LegendServerProxy.ServiceBox;
using System.Collections.Generic;

namespace LegendServerProxy.WordPlate
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
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestCreateWordPlateHouse_T2P reqMsg_T2P = msg as RequestCreateWordPlateHouse_T2P;
            RequestCreateWordPlateHouse_P2L reqMsg_P2L = new RequestCreateWordPlateHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.maxBureau = reqMsg_T2P.maxBureau;
            reqMsg_P2L.wordPlateType = reqMsg_T2P.wordPlateType;
            reqMsg_P2L.maxWinScore = reqMsg_T2P.maxWinScore;
            reqMsg_P2L.housePropertyType = reqMsg_T2P.housePropertyType;
            reqMsg_P2L.baseScore = reqMsg_T2P.baseScore;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }   
        public void OnReplyCreateWordPlateHouse(int peerId, bool inbound, object msg)
        {
            ReplyCreateWordPlateHouse_L2P replyMsg_L2P = msg as ReplyCreateWordPlateHouse_L2P;
            ReplyCreateWordPlateHouse_P2T replyMsg_P2T = new ReplyCreateWordPlateHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.maxBureau = replyMsg_L2P.maxBureau;
            replyMsg_P2T.wordPlateType = replyMsg_L2P.wordPlateType;
            replyMsg_P2T.houseId = replyMsg_L2P.houseId;
            replyMsg_P2T.allIntegral = replyMsg_L2P.allIntegral;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.maxWinScore = replyMsg_L2P.maxWinScore;
            replyMsg_P2T.maxPlayerNum = replyMsg_L2P.maxPlayerNum;
            replyMsg_P2T.housePropertyType = replyMsg_L2P.housePropertyType;
            replyMsg_P2T.baseScore = replyMsg_L2P.baseScore;
            replyMsg_P2T.businessId = replyMsg_L2P.businessId;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);

            //盒子测试用 -- yishan
            if (ModuleManager.Get<ServiceBoxMain>().bOpenBoxAutoPlaying)
            {
                ModuleManager.Get<ServiceBoxMain>().ReplyCreateHouse(replyMsg_L2P.businessId, replyMsg_L2P.summonerId, replyMsg_L2P.houseId);
            }
        }
        public void OnReqJoinWordPlateHouse(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestJoinWordPlateHouse_T2P reqMsg_T2P = msg as RequestJoinWordPlateHouse_T2P;
            RequestJoinWordPlateHouse_P2L reqMsg_P2L = new RequestJoinWordPlateHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.houseId = reqMsg_T2P.houseId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyJoinWordPlateHouse(int peerId, bool inbound, object msg)
        {
            ReplyJoinWordPlateHouse_L2P replyMsg_L2P = msg as ReplyJoinWordPlateHouse_L2P;
            ReplyJoinWordPlateHouse_P2T replyMsg_P2T = new ReplyJoinWordPlateHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.maxBureau = replyMsg_L2P.maxBureau;
            replyMsg_P2T.wordPlateType = replyMsg_L2P.wordPlateType;
            replyMsg_P2T.houseId = replyMsg_L2P.houseId;
            replyMsg_P2T.myIndex = replyMsg_L2P.myIndex;
            replyMsg_P2T.allIntegral = replyMsg_L2P.allIntegral;
            replyMsg_P2T.playerShow = replyMsg_L2P.playerShow;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.maxWinScore = replyMsg_L2P.maxWinScore;
            replyMsg_P2T.maxPlayerNum = replyMsg_L2P.maxPlayerNum;
            replyMsg_P2T.housePropertyType = replyMsg_L2P.housePropertyType;
            replyMsg_P2T.baseScore = replyMsg_L2P.baseScore;
            replyMsg_P2T.businessId = replyMsg_L2P.businessId;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);

            //盒子测试用 -- yishan
            if (ModuleManager.Get<ServiceBoxMain>().bOpenBoxAutoPlaying)
            {
                ModuleManager.Get<ServiceBoxMain>().ReplyJoinHouse(replyMsg_L2P.summonerId, replyMsg_L2P.result == ResultCode.OK, replyMsg_L2P.houseId);
            }
        }
        public void OnRecvJoinWordPlateHouse(int peerId, bool inbound, object msg)
        {
            RecvJoinWordPlateHouse_L2P recvMsg_L2P = msg as RecvJoinWordPlateHouse_L2P;
            RecvJoinWordPlateHouse_P2T recvMsg_P2T = new RecvJoinWordPlateHouse_P2T();
            recvMsg_P2T.playerShow = recvMsg_L2P.playerShow;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqQuitWordPlateHouse(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestQuitWordPlateHouse_T2P reqMsg_T2P = msg as RequestQuitWordPlateHouse_T2P;
            RequestQuitWordPlateHouse_P2L reqMsg_P2L = new RequestQuitWordPlateHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyQuitWordPlateHouse(int peerId, bool inbound, object msg)
        {
            ReplyQuitWordPlateHouse_L2P replyMsg_L2P = msg as ReplyQuitWordPlateHouse_L2P;
            ReplyQuitWordPlateHouse_P2T replyMsg_P2T = new ReplyQuitWordPlateHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.bVote = replyMsg_L2P.bVote;
            replyMsg_P2T.dissolveVoteTime = replyMsg_L2P.dissolveVoteTime;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvQuitWordPlateHouse(int peerId, bool inbound, object msg)
        {
            RecvQuitWordPlateHouse_L2P recvMsg_L2P = msg as RecvQuitWordPlateHouse_L2P;
            RecvQuitWordPlateHouse_P2T recvMsg_P2T = new RecvQuitWordPlateHouse_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.bVote = recvMsg_L2P.bVote;
            recvMsg_P2T.dissolveVoteTime = recvMsg_L2P.dissolveVoteTime;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvLeaveWordPlateHouse(int peerId, bool inbound, object msg)
        {
            RecvLeaveWordPlateHouse_L2P recvMsg_L2P = msg as RecvLeaveWordPlateHouse_L2P;
            RecvLeaveWordPlateHouse_P2T recvMsg_P2T = new RecvLeaveWordPlateHouse_P2T();
            recvMsg_P2T.leaveIndex = recvMsg_L2P.leaveIndex;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqWordPlateHouseVote(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestWordPlateHouseVote_T2P reqMsg_T2P = msg as RequestWordPlateHouseVote_T2P;
            RequestWordPlateHouseVote_P2L reqMsg_P2L = new RequestWordPlateHouseVote_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.voteStatus = reqMsg_T2P.voteStatus;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyWordPlateHouseVote(int peerId, bool inbound, object msg)
        {
            ReplyWordPlateHouseVote_L2P replyMsg_L2P = msg as ReplyWordPlateHouseVote_L2P;
            ReplyWordPlateHouseVote_P2T replyMsg_P2T = new ReplyWordPlateHouseVote_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.houseVoteStatus = replyMsg_L2P.houseVoteStatus;
            replyMsg_P2T.voteStatus = replyMsg_L2P.voteStatus;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvWordPlateHouseVote(int peerId, bool inbound, object msg)
        {
            RecvWordPlateHouseVote_L2P recvMsg_L2P = msg as RecvWordPlateHouseVote_L2P;
            RecvWordPlateHouseVote_P2T recvMsg_P2T = new RecvWordPlateHouseVote_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.houseVoteStatus = recvMsg_L2P.houseVoteStatus;
            recvMsg_P2T.voteStatus = recvMsg_L2P.voteStatus;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqReadyWordPlateHouse(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestReadyWordPlateHouse_T2P reqMsg_T2P = msg as RequestReadyWordPlateHouse_T2P;
            RequestReadyWordPlateHouse_P2L reqMsg_P2L = new RequestReadyWordPlateHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyReadyWordPlateHouse(int peerId, bool inbound, object msg)
        {
            ReplyReadyWordPlateHouse_L2P replyMsg_L2P = msg as ReplyReadyWordPlateHouse_L2P;
            ReplyReadyWordPlateHouse_P2T replyMsg_P2T = new ReplyReadyWordPlateHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvReadyWordPlateHouse(int peerId, bool inbound, object msg)
        {
            RecvReadyWordPlateHouse_L2P replyMsg_L2P = msg as RecvReadyWordPlateHouse_L2P;
            RecvReadyWordPlateHouse_P2T replyMsg_P2T = new RecvReadyWordPlateHouse_P2T();
            replyMsg_P2T.readyIndex = replyMsg_L2P.readyIndex;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvBeginWordPlate(int peerId, bool inbound, object msg)
        {
            RecvBeginWordPlate_L2P recvMsg_L2P = msg as RecvBeginWordPlate_L2P;
            RecvBeginWordPlate_P2T recvMsg_P2T = new RecvBeginWordPlate_P2T();
            recvMsg_P2T.currentBureau = recvMsg_L2P.currentBureau;
            recvMsg_P2T.currentShowCard = recvMsg_L2P.currentShowCard;
            recvMsg_P2T.houseStatus = recvMsg_L2P.houseStatus;
            recvMsg_P2T.housePlayerStatus = recvMsg_L2P.housePlayerStatus;
            recvMsg_P2T.zhuangIndex = recvMsg_L2P.zhuangIndex;
            recvMsg_P2T.remainWordPlateCount = recvMsg_L2P.remainWordPlateCount;
            recvMsg_P2T.godWordPlateTile = recvMsg_L2P.godWordPlateTile;
            recvMsg_P2T.wordPlateList.AddRange(recvMsg_L2P.wordPlateList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqWordPlateHouseInfo(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestWordPlateHouseInfo_T2P reqMsg_T2P = msg as RequestWordPlateHouseInfo_T2P;
            RequestWordPlateHouseInfo_P2L reqMsg_P2L = new RequestWordPlateHouseInfo_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyWordPlateHouseInfo(int peerId, bool inbound, object msg)
        {
            ReplyWordPlateHouseInfo_L2P replyMsg_L2P = msg as ReplyWordPlateHouseInfo_L2P;
            ReplyWordPlateHouseInfo_P2T replyMsg_P2T = new ReplyWordPlateHouseInfo_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.houseId = replyMsg_L2P.houseId;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.wordPlateType = replyMsg_L2P.wordPlateType;
            replyMsg_P2T.currentBureau = replyMsg_L2P.currentBureau;
            replyMsg_P2T.maxBureau = replyMsg_L2P.maxBureau;
            replyMsg_P2T.currentShowCard = replyMsg_L2P.currentShowCard;
            replyMsg_P2T.currentWhoPlay = replyMsg_L2P.currentWhoPlay;
            replyMsg_P2T.houseStatus = replyMsg_L2P.houseStatus;
            replyMsg_P2T.baseScore = replyMsg_L2P.baseScore;
            replyMsg_P2T.housePropertyType = replyMsg_L2P.housePropertyType;
            replyMsg_P2T.bIsPlayerShow = replyMsg_L2P.bIsPlayerShow;
            replyMsg_P2T.currentWordPlate = replyMsg_L2P.currentWordPlate;
            replyMsg_P2T.wordPlateOnlineNode = replyMsg_L2P.wordPlateOnlineNode;
            replyMsg_P2T.houseVoteTime = replyMsg_L2P.houseVoteTime;
            replyMsg_P2T.maxWinScore = replyMsg_L2P.maxWinScore;
            replyMsg_P2T.maxPlayerNum = replyMsg_L2P.maxPlayerNum;
            replyMsg_P2T.remainWordPlateCount = replyMsg_L2P.remainWordPlateCount;
            replyMsg_P2T.operatTypeList.AddRange(replyMsg_L2P.operatTypeList);
            replyMsg_P2T.zhuangIndex = replyMsg_L2P.zhuangIndex;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;
            replyMsg_P2T.beginGodType = replyMsg_L2P.beginGodType;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqShowWordPlate(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestShowWordPlate_T2P reqMsg_T2P = msg as RequestShowWordPlate_T2P;
            RequestShowWordPlate_P2L reqMsg_P2L = new RequestShowWordPlate_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.wordPlateNode = reqMsg_T2P.wordPlateNode;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyShowWordPlate(int peerId, bool inbound, object msg)
        {
            ReplyShowWordPlate_L2P replyMsg_L2P = msg as ReplyShowWordPlate_L2P;
            ReplyShowWordPlate_P2T replyMsg_P2T = new ReplyShowWordPlate_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.wordPlateNode = replyMsg_L2P.wordPlateNode;
            replyMsg_P2T.bGiveUpWin = replyMsg_L2P.bGiveUpWin;
            replyMsg_P2T.bDeadHand = replyMsg_L2P.bDeadHand;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvShowWordPlate(int peerId, bool inbound, object msg)
        {
            RecvShowWordPlate_L2P recvMsg_L2P = msg as RecvShowWordPlate_L2P;
            RecvShowWordPlate_P2T recvMsg_P2T = new RecvShowWordPlate_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.operatTypeList.AddRange(recvMsg_L2P.operatTypeList);
            recvMsg_P2T.wordPlateNode = recvMsg_L2P.wordPlateNode;
            recvMsg_P2T.bIsPlayerShow = recvMsg_L2P.bIsPlayerShow;
            recvMsg_P2T.bDeadHand = recvMsg_L2P.bDeadHand;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqOperatWordPlate(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestOperatWordPlate_T2P reqMsg_T2P = msg as RequestOperatWordPlate_T2P;
            RequestOperatWordPlate_P2L reqMsg_P2L = new RequestOperatWordPlate_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.operatType = reqMsg_T2P.operatType;
            reqMsg_P2L.wordPlateList.AddRange(reqMsg_T2P.wordPlateList);

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyOperatWordPlate(int peerId, bool inbound, object msg)
        {
            ReplyOperatWordPlate_L2P replyMsg_L2P = msg as ReplyOperatWordPlate_L2P;
            ReplyOperatWordPlate_P2T replyMsg_P2T = new ReplyOperatWordPlate_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.operatType = replyMsg_L2P.operatType;
            replyMsg_P2T.meldType = replyMsg_L2P.meldType;
            replyMsg_P2T.currentShowCard = replyMsg_L2P.currentShowCard;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;
            replyMsg_P2T.bGiveUpWin = replyMsg_L2P.bGiveUpWin;
            replyMsg_P2T.bDeadHand = replyMsg_L2P.bDeadHand;
            replyMsg_P2T.bOperatMyHand = replyMsg_L2P.bOperatMyHand;
            replyMsg_P2T.wordPlateList.AddRange(replyMsg_L2P.wordPlateList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvOperatWordPlate(int peerId, bool inbound, object msg)
        {
            RecvOperatWordPlate_L2P recvMsg_L2P = msg as RecvOperatWordPlate_L2P;
            RecvOperatWordPlate_P2T recvMsg_P2T = new RecvOperatWordPlate_P2T();
            recvMsg_P2T.operatType = recvMsg_L2P.operatType;
            recvMsg_P2T.meldType = recvMsg_L2P.meldType;
            recvMsg_P2T.currentShowCard = recvMsg_L2P.currentShowCard;
            recvMsg_P2T.bDeadHand = recvMsg_L2P.bDeadHand;
            recvMsg_P2T.bOperatMyHand = recvMsg_L2P.bOperatMyHand;
            recvMsg_P2T.wordPlateList.AddRange(recvMsg_L2P.wordPlateList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvPlayerWinWordPlate(int peerId, bool inbound, object msg)
        {
            RecvPlayerWinWordPlate_L2P recvMsg_L2P = msg as RecvPlayerWinWordPlate_L2P;
            RecvPlayerWinWordPlate_P2T recvMsg_P2T = new RecvPlayerWinWordPlate_P2T();
            recvMsg_P2T.winPlayerIndex = recvMsg_L2P.winPlayerIndex;
            recvMsg_P2T.housePlateInfo = recvMsg_L2P.housePlateInfo;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvSettlementWordPlate(int peerId, bool inbound, object msg)
        {
            RecvSettlementWordPlate_L2P recvMsg_L2P = msg as RecvSettlementWordPlate_L2P;
            RecvSettlementWordPlate_P2T recvMsg_P2T = new RecvSettlementWordPlate_P2T();
            recvMsg_P2T.wordPlateSettlement = recvMsg_L2P.wordPlateSettlement;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvEndSettlementWordPlate(int peerId, bool inbound, object msg)
        {
            RecvEndSettlementWordPlate_L2P recvMsg_L2P = msg as RecvEndSettlementWordPlate_L2P;
            RecvEndSettlementWordPlate_P2T recvMsg_P2T = new RecvEndSettlementWordPlate_P2T();
            recvMsg_P2T.houseStatus = recvMsg_L2P.houseStatus;
            recvMsg_P2T.allIntegral = recvMsg_L2P.allIntegral;
            recvMsg_P2T.ticketsNode = recvMsg_L2P.ticketsNode;
            recvMsg_P2T.wordPlateEndSettlementList.AddRange(recvMsg_L2P.wordPlateEndSettlementList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvPlayerPassChowWordPlate(int peerId, bool inbound, object msg)
        {
            RecvPlayerPassChowWordPlate_L2P recvMsg_L2P = msg as RecvPlayerPassChowWordPlate_L2P;
            RecvPlayerPassChowWordPlate_P2T recvMsg_P2T = new RecvPlayerPassChowWordPlate_P2T();
            recvMsg_P2T.wordPlateNode = recvMsg_L2P.wordPlateNode;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqWordPlateOverallRecord(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestWordPlateOverallRecord_T2P reqMsg_T2P = msg as RequestWordPlateOverallRecord_T2P;
            RequestWordPlateOverallRecord_P2L reqMsg_P2L = new RequestWordPlateOverallRecord_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyWordPlateOverallRecord(int peerId, bool inbound, object msg)
        {
            ReplyWordPlateOverallRecord_L2P replyMsg_L2P = msg as ReplyWordPlateOverallRecord_L2P;
            ReplyWordPlateOverallRecord_P2T replyMsg_P2T = new ReplyWordPlateOverallRecord_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.overallRecord = replyMsg_L2P.overallRecord;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqWordPlateBureauRecord(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestWordPlateBureauRecord_T2P reqMsg_T2P = msg as RequestWordPlateBureauRecord_T2P;
            RequestWordPlateBureauRecord_P2L reqMsg_P2L = new RequestWordPlateBureauRecord_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.onlyHouseId = reqMsg_T2P.onlyHouseId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyWordPlateBureauRecord(int peerId, bool inbound, object msg)
        {
            ReplyWordPlateBureauRecord_L2P replyMsg_L2P = msg as ReplyWordPlateBureauRecord_L2P;
            ReplyWordPlateBureauRecord_P2T replyMsg_P2T = new ReplyWordPlateBureauRecord_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.bureauRecord = replyMsg_L2P.bureauRecord;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqWordPlateBureauPlayback(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestWordPlateBureauPlayback_T2P reqMsg_T2P = msg as RequestWordPlateBureauPlayback_T2P;
            RequestWordPlateBureauPlayback_P2L reqMsg_P2L = new RequestWordPlateBureauPlayback_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.onlyHouseId = reqMsg_T2P.onlyHouseId;
            reqMsg_P2L.bureau = reqMsg_T2P.bureau;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyWordPlateBureauPlayback(int peerId, bool inbound, object msg)
        {
            ReplyWordPlateBureauPlayback_L2P replyMsg_L2P = msg as ReplyWordPlateBureauPlayback_L2P;
            ReplyWordPlateBureauPlayback_P2T replyMsg_P2T = new ReplyWordPlateBureauPlayback_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.bureau = replyMsg_L2P.bureau;
            replyMsg_P2T.playerWordPlate = replyMsg_L2P.playerWordPlate;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
    }
}
#endif
