using System.Collections.Generic;
using LegendProtocol;
using LegendServer.Database;
using System;
using LegendServerProxy.Core;
using LegendServerProxy.Distributed;

namespace LegendServerProxy.SpecialActivities
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
            RequestMarketKey_X2P reqMsg_X2P = msg as RequestMarketKey_X2P;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestMarketKey_P2W reqMsg_P2W = new RequestMarketKey_P2W();
            reqMsg_P2W.marketId = reqMsg_X2P.marketId;
            reqMsg_P2W.requesterPeerId = peerId;
            reqMsg_P2W.requesterSummonerId = session.summonerId;
            reqMsg_P2W.build = reqMsg_X2P.build;
            SendWorldMsg(reqMsg_P2W);
        }

        public void OnReplyMarketKey(int peerId, bool inbound, object msg)
        {
            ReplyMarketKey_W2P replyMsg_W2P = msg as ReplyMarketKey_W2P;

            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(replyMsg_W2P.requesterPeerId);
            if (requesterSession != null && requesterSession.status == SessionStatus.Connected)
            {
                ReplyMarketKey_P2X replyMsg_P2X = new ReplyMarketKey_P2X();
                replyMsg_P2X.result = replyMsg_W2P.result;
                replyMsg_P2X.key = replyMsg_W2P.key;
                replyMsg_P2X.marketId = replyMsg_W2P.marketId;
                SendClientMsg(replyMsg_W2P.requesterPeerId, replyMsg_P2X);
            }
        }

        public void OnRequestMarketVersion(int peerId, bool inbound, object msg)
        {
            RequestMarketVersion_X2P reqMsg = msg as RequestMarketVersion_X2P;

            ReplyMarketVersion_P2X replyMsg = new ReplyMarketVersion_P2X();
            replyMsg.version = main.Version;
            SendClientMsg(peerId, replyMsg);
        }

        public void OnRequestJoinMarket(int peerId, bool inbound, object msg)
        {
            RequestJoinMarket_X2P reqMsg_X2P = msg as RequestJoinMarket_X2P;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestJoinMarket_P2W reqMsg_P2W = new RequestJoinMarket_P2W();
            reqMsg_P2W.key = reqMsg_X2P.key;
            reqMsg_P2W.requesterSummonerId = session.summonerId;
            reqMsg_P2W.requesterPeerId = peerId;
            SendWorldMsg(reqMsg_P2W);
        }
        public void OnReplyJoinMarket(int peerId, bool inbound, object msg)
        {
            ReplyJoinMarket_W2P replyMsg_W2P = msg as ReplyJoinMarket_W2P;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(replyMsg_W2P.requesterPeerId);
            if (session != null && session.status == SessionStatus.Connected)
            {
                ReplyJoinMarket_P2X replyMsg_P2X = new ReplyJoinMarket_P2X();
                replyMsg_P2X.result = replyMsg_W2P.result;
                SendClientMsg(replyMsg_W2P.requesterPeerId, replyMsg_P2X);
            }
        }
        public void OnRequestUseTickets(int peerId, bool inbound, object msg)
        {
            RequestUseTickets_T2P reqMsg_T2P = msg as RequestUseTickets_T2P;
            RequestUseTickets_P2L reqMsg_P2L = new RequestUseTickets_P2L();

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            reqMsg_P2L.ticketsOnlyId = reqMsg_T2P.ticketsOnlyId;
            reqMsg_P2L.summonerId = session.summonerId;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyUseTickets(int peerId, bool inbound, object msg)
        {
            ReplyUseTickets_L2P replyMsg_L2P = msg as ReplyUseTickets_L2P;
            ReplyUseTickets_P2T replyMsg_P2T = new ReplyUseTickets_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.ticketsOnlyId = replyMsg_L2P.ticketsOnlyId;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRequestTicketsInfo(int peerId, bool inbound, object msg)
        {
            RequestTicketsInfo_T2P reqMsg_T2P = msg as RequestTicketsInfo_T2P;
            RequestTicketsInfo_P2L reqMsg_P2L = new RequestTicketsInfo_P2L();

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);
            
            reqMsg_P2L.summonerId = session.summonerId;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyTicketsInfo(int peerId, bool inbound, object msg)
        {
            ReplyTicketsInfo_L2P replyMsg_L2P = msg as ReplyTicketsInfo_L2P;
            ReplyTicketsInfo_P2T replyMsg_P2T = new ReplyTicketsInfo_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.ticketsList.AddRange(replyMsg_L2P.ticketsList);
            replyMsg_P2T.delTicketsIdList.AddRange(replyMsg_L2P.delTicketsIdList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRequestQuitMarketCompetition(int peerId, bool inbound, object msg)
        {
            RequestQuitMarketCompetition_T2P reqMsg_T2P = msg as RequestQuitMarketCompetition_T2P;
            RequestQuitMarketCompetition_P2L reqMsg_P2L = new RequestQuitMarketCompetition_P2L();

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            reqMsg_P2L.summonerId = session.summonerId;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyQuitMarketCompetition(int peerId, bool inbound, object msg)
        {
            ReplyQuitMarketCompetition_L2P replyMsg_L2P = msg as ReplyQuitMarketCompetition_L2P;
            ReplyQuitMarketCompetition_P2T replyMsg_P2T = new ReplyQuitMarketCompetition_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReplyJoinMarketCompetition(int peerId, bool inbound, object msg)
        {
            ReplyJoinMarketCompetition_L2P replyMsg_L2P = msg as ReplyJoinMarketCompetition_L2P;
            ReplyJoinMarketCompetition_P2T replyMsg_P2T = new ReplyJoinMarketCompetition_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.joinPalyerNum = replyMsg_L2P.joinPalyerNum;
            replyMsg_P2T.maxApplyNum = replyMsg_L2P.maxApplyNum;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRequestCreateMarketCompetition(int peerId, bool inbound, object msg)
        {
            RequestCreateMarketCompetition_T2P reqMsg_T2P = msg as RequestCreateMarketCompetition_T2P;

            if (main.maxPlayerNum <= 0 || reqMsg_T2P.maxApplyNum <= 0 || reqMsg_T2P.maxGameBureau <= 0 || reqMsg_T2P.firstAdmitNum <= 0)
            {
                //数据有误
                ReplyCreateMarketCompetition_P2T replyMsg = new ReplyCreateMarketCompetition_P2T();
                replyMsg.result = ResultCode.CreateCompetitionDataError;
                SendClientMsg(peerId, replyMsg);
                return;
            }
            int maxApplyNum = reqMsg_T2P.maxApplyNum * main.maxPlayerNum;
            if (reqMsg_T2P.firstAdmitNum >= maxApplyNum)
            {
                //数据有误,第一次录取的人数大于报名人数
                ReplyCreateMarketCompetition_P2T replyMsg = new ReplyCreateMarketCompetition_P2T();
                replyMsg.result = ResultCode.CreateCompetitionDataError;
                SendClientMsg(peerId, replyMsg);
                return;
            }
            if (!main.IsPow(reqMsg_T2P.firstAdmitNum, main.maxPlayerNum))
            {
                //数据有误,第一次录取的人数不是人数的幂
                ReplyCreateMarketCompetition_P2T replyMsg = new ReplyCreateMarketCompetition_P2T();
                replyMsg.result = ResultCode.CreateCompetitionDataError;
                SendClientMsg(peerId, replyMsg);
                return;
            }
            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestCreateMarketCompetition_P2W reqMsg_P2W = new RequestCreateMarketCompetition_P2W();
            reqMsg_P2W.requesterSummonerId = session.summonerId;
            reqMsg_P2W.marketId = reqMsg_T2P.marketId;
            reqMsg_P2W.maxGameBureau = reqMsg_T2P.maxGameBureau;
            reqMsg_P2W.maxApplyNum = maxApplyNum;
            reqMsg_P2W.firstAdmitNum = reqMsg_T2P.firstAdmitNum;
            SendWorldMsg(reqMsg_P2W);
        }
        public void OnReplyCreateMarketCompetition(int peerId, bool inbound, object msg)
        {
            ReplyCreateMarketCompetition_W2P replyMsg_W2P = msg as ReplyCreateMarketCompetition_W2P;
            ReplyCreateMarketCompetition_P2T replyMsg_P2T = new ReplyCreateMarketCompetition_P2T();
            replyMsg_P2T.result = replyMsg_W2P.result;
            replyMsg_P2T.marketId = replyMsg_W2P.marketId;
            replyMsg_P2T.competitionKey = replyMsg_W2P.competitionKey;
            replyMsg_P2T.maxGameBureau = replyMsg_W2P.maxGameBureau;
            replyMsg_P2T.maxApplyNum = replyMsg_W2P.maxApplyNum;
            replyMsg_P2T.firstAdmitNum = replyMsg_W2P.firstAdmitNum;
            replyMsg_P2T.createTime = replyMsg_W2P.createTime;

            SendClientMsgBySummonerId(replyMsg_W2P.requesterSummonerId, replyMsg_P2T);
        }
        public void OnRequestMarketCompetitionInfo(int peerId, bool inbound, object msg)
        {
            RequestMarketCompetitionInfo_T2P reqMsg_T2P = msg as RequestMarketCompetitionInfo_T2P;
            RequestMarketCompetitionInfo_P2L reqMsg_P2L = new RequestMarketCompetitionInfo_P2L();

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            reqMsg_P2L.summonerId = session.summonerId;
            reqMsg_P2L.marketId = reqMsg_T2P.marketId;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyMarketCompetitionInfo(int peerId, bool inbound, object msg)
        {
            ReplyMarketCompetitionInfo_L2P replyMsg_L2P = msg as ReplyMarketCompetitionInfo_L2P;
            ReplyMarketCompetitionInfo_P2T replyMsg_P2T = new ReplyMarketCompetitionInfo_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.marketId = replyMsg_L2P.marketId;
            replyMsg_P2T.marketCompetition = replyMsg_L2P.marketCompetition;
            //replyMsg_P2T.marketComList.AddRange(replyMsg_L2P.marketComList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRequestCompetitionPlayerInfo(int peerId, bool inbound, object msg)
        {
            RequestCompetitionPlayerInfo_T2P reqMsg_T2P = msg as RequestCompetitionPlayerInfo_T2P;
            RequestCompetitionPlayerInfo_P2L reqMsg_P2L = new RequestCompetitionPlayerInfo_P2L();

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            reqMsg_P2L.summonerId = session.summonerId;
            reqMsg_P2L.competitionKey = reqMsg_T2P.competitionKey;
            reqMsg_P2L.page = reqMsg_T2P.page;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyCompetitionPlayerInfo(int peerId, bool inbound, object msg)
        {
            ReplyCompetitionPlayerInfo_L2P replyMsg_L2P = msg as ReplyCompetitionPlayerInfo_L2P;
            ReplyCompetitionPlayerInfo_P2T replyMsg_P2T = new ReplyCompetitionPlayerInfo_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;
            replyMsg_P2T.page = replyMsg_L2P.page;
            replyMsg_P2T.competitionPlayer = replyMsg_L2P.competitionPlayer;
            replyMsg_P2T.joinPalyerNum = replyMsg_L2P.joinPalyerNum;
            //replyMsg_P2T.comPlayerList.AddRange(replyMsg_L2P.comPlayerList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvCompetitionPlayerRank(int peerId, bool inbound, object msg)
        {
            RecvCompetitionPlayerRank_L2P replyMsg_L2P = msg as RecvCompetitionPlayerRank_L2P;
            RecvCompetitionPlayerRank_P2T replyMsg_P2T = new RecvCompetitionPlayerRank_P2T();
            replyMsg_P2T.rank = replyMsg_L2P.rank;
            replyMsg_P2T.admitNum = replyMsg_L2P.admitNum;
            replyMsg_P2T.houseCount = replyMsg_L2P.houseCount;
            replyMsg_P2T.bOnline = replyMsg_L2P.bOnline;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvCompetitionPlayerOverRank(int peerId, bool inbound, object msg)
        {
            RecvCompetitionPlayerOverRank_L2P replyMsg_L2P = msg as RecvCompetitionPlayerOverRank_L2P;
            RecvCompetitionPlayerOverRank_P2T replyMsg_P2T = new RecvCompetitionPlayerOverRank_P2T();
            replyMsg_P2T.rank = replyMsg_L2P.rank;
            replyMsg_P2T.ticketsNode = replyMsg_L2P.ticketsNode;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRequestCompetitionPlayerOnline(int peerId, bool inbound, object msg)
        {
            RequestCompetitionPlayerOnline_T2P reqMsg_T2P = msg as RequestCompetitionPlayerOnline_T2P;
            RequestCompetitionPlayerOnline_P2L reqMsg_P2L = new RequestCompetitionPlayerOnline_P2L();

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            reqMsg_P2L.summonerId = session.summonerId;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnRecvCompetitionPlayerApplyNum(int peerId, bool inbound, object msg)
        {
            RecvCompetitionPlayerApplyNum_L2P replyMsg_L2P = msg as RecvCompetitionPlayerApplyNum_L2P;
            RecvCompetitionPlayerApplyNum_P2T replyMsg_P2T = new RecvCompetitionPlayerApplyNum_P2T();
            replyMsg_P2T.joinPalyerNum = replyMsg_L2P.joinPalyerNum;
            replyMsg_P2T.maxApplyNum = replyMsg_L2P.maxApplyNum;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRequestDelMarketCompetition(int peerId, bool inbound, object msg)
        {
            RequestDelMarketCompetition_T2P reqMsg_T2P = msg as RequestDelMarketCompetition_T2P;
            RequestDelMarketCompetition_P2L reqMsg_P2L = new RequestDelMarketCompetition_P2L();

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            reqMsg_P2L.summonerId = session.summonerId;
            reqMsg_P2L.competitionKey = reqMsg_T2P.competitionKey;
            SendLogicMsg(reqMsg_P2L, session.logicServerID);
        }
        public void OnReplyDelMarketCompetition(int peerId, bool inbound, object msg)
        {
            ReplyDelMarketCompetition_L2P replyMsg_L2P = msg as ReplyDelMarketCompetition_L2P;
            ReplyDelMarketCompetition_P2T replyMsg_P2T = new ReplyDelMarketCompetition_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
    }
}

