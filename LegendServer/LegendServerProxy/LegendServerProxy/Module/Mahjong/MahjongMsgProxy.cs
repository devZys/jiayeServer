#if MAHJONG
using LegendProtocol;
using LegendServerProxy.Core;
using LegendServerProxy.ServiceBox;
using System.Collections.Generic;

namespace LegendServerProxy.Mahjong
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
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestCreateMahjongHouse_T2P reqMsg_T2P = msg as RequestCreateMahjongHouse_T2P;
            RequestCreateMahjongHouse_P2L reqMsg_P2L = new RequestCreateMahjongHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.maxBureau = reqMsg_T2P.maxBureau;
            reqMsg_P2L.mahjongType = reqMsg_T2P.mahjongType;
            reqMsg_P2L.maxPlayerNum = reqMsg_T2P.maxPlayerNum;
            reqMsg_P2L.housePropertyType = reqMsg_T2P.housePropertyType;
            reqMsg_P2L.catchBird = reqMsg_T2P.catchBird;
            reqMsg_P2L.flutter = reqMsg_T2P.flutter;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyCreateMahjongHouse(int peerId, bool inbound, object msg)
        {
            ReplyCreateMahjongHouse_L2P replyMsg_L2P = msg as ReplyCreateMahjongHouse_L2P;
            ReplyCreateMahjongHouse_P2T replyMsg_P2T = new ReplyCreateMahjongHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.maxBureau = replyMsg_L2P.maxBureau;
            replyMsg_P2T.mahjongType = replyMsg_L2P.mahjongType;
            replyMsg_P2T.houseId = replyMsg_L2P.houseId;
            replyMsg_P2T.allIntegral = replyMsg_L2P.allIntegral;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.maxPlayerNum = replyMsg_L2P.maxPlayerNum;
            replyMsg_P2T.housePropertyType = replyMsg_L2P.housePropertyType;
            replyMsg_P2T.catchBird = replyMsg_L2P.catchBird;
            replyMsg_P2T.businessId = replyMsg_L2P.businessId;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;
            replyMsg_P2T.flutter = replyMsg_L2P.flutter;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);

            //盒子测试用 -- yishan
            if (ModuleManager.Get<ServiceBoxMain>().bOpenBoxAutoPlaying)
            {
                ModuleManager.Get<ServiceBoxMain>().ReplyCreateHouse(replyMsg_L2P.businessId, replyMsg_L2P.summonerId, replyMsg_L2P.houseId);
            }
        }
        public void OnReqJoinMahjongHouse(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestJoinMahjongHouse_T2P reqMsg_T2P = msg as RequestJoinMahjongHouse_T2P;
            RequestJoinMahjongHouse_P2L reqMsg_P2L = new RequestJoinMahjongHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.houseId = reqMsg_T2P.houseId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyJoinMahjongHouse(int peerId, bool inbound, object msg)
        {
            ReplyJoinMahjongHouse_L2P replyMsg_L2P = msg as ReplyJoinMahjongHouse_L2P;
            ReplyJoinMahjongHouse_P2T replyMsg_P2T = new ReplyJoinMahjongHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.maxBureau = replyMsg_L2P.maxBureau;
            replyMsg_P2T.mahjongType = replyMsg_L2P.mahjongType;
            replyMsg_P2T.houseId = replyMsg_L2P.houseId;
            replyMsg_P2T.myIndex = replyMsg_L2P.myIndex;
            replyMsg_P2T.allIntegral = replyMsg_L2P.allIntegral;
            replyMsg_P2T.playerShow = replyMsg_L2P.playerShow;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.maxPlayerNum = replyMsg_L2P.maxPlayerNum;
            replyMsg_P2T.housePropertyType = replyMsg_L2P.housePropertyType;
            replyMsg_P2T.catchBird = replyMsg_L2P.catchBird;
            replyMsg_P2T.businessId = replyMsg_L2P.businessId;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;
            replyMsg_P2T.flutter = replyMsg_L2P.flutter;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);

            //盒子测试用 -- yishan
            if (ModuleManager.Get<ServiceBoxMain>().bOpenBoxAutoPlaying)
            {
                ModuleManager.Get<ServiceBoxMain>().ReplyJoinHouse(replyMsg_L2P.summonerId, replyMsg_L2P.result == ResultCode.OK, replyMsg_L2P.houseId);
            }
        }
        public void OnRecvJoinMahjongHouse(int peerId, bool inbound, object msg)
        {
            RecvJoinMahjongHouse_L2P recvMsg_L2P = msg as RecvJoinMahjongHouse_L2P;
            RecvJoinMahjongHouse_P2T recvMsg_P2T = new RecvJoinMahjongHouse_P2T();
            recvMsg_P2T.playerShow = recvMsg_L2P.playerShow;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqQuitMahjongHouse(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestQuitMahjongHouse_T2P reqMsg_T2P = msg as RequestQuitMahjongHouse_T2P;
            RequestQuitMahjongHouse_P2L reqMsg_P2L = new RequestQuitMahjongHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyQuitMahjongHouse(int peerId, bool inbound, object msg)
        {
            ReplyQuitMahjongHouse_L2P replyMsg_L2P = msg as ReplyQuitMahjongHouse_L2P;
            ReplyQuitMahjongHouse_P2T replyMsg_P2T = new ReplyQuitMahjongHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.bVote = replyMsg_L2P.bVote;
            replyMsg_P2T.dissolveVoteTime = replyMsg_L2P.dissolveVoteTime;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvQuitMahjongHouse(int peerId, bool inbound, object msg)
        {
            RecvQuitMahjongHouse_L2P recvMsg_L2P = msg as RecvQuitMahjongHouse_L2P;
            RecvQuitMahjongHouse_P2T recvMsg_P2T = new RecvQuitMahjongHouse_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.bVote = recvMsg_L2P.bVote;
            recvMsg_P2T.dissolveVoteTime = recvMsg_L2P.dissolveVoteTime;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvLeaveMahjongHouse(int peerId, bool inbound, object msg)
        {
            RecvLeaveMahjongHouse_L2P recvMsg_L2P = msg as RecvLeaveMahjongHouse_L2P;
            RecvLeaveMahjongHouse_P2T recvMsg_P2T = new RecvLeaveMahjongHouse_P2T();
            recvMsg_P2T.leaveIndex = recvMsg_L2P.leaveIndex;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqMahjongHouseVote(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestMahjongHouseVote_T2P reqMsg_T2P = msg as RequestMahjongHouseVote_T2P;
            RequestMahjongHouseVote_P2L reqMsg_P2L = new RequestMahjongHouseVote_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.voteStatus = reqMsg_T2P.voteStatus;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyMahjongHouseVote(int peerId, bool inbound, object msg)
        {
            ReplyMahjongHouseVote_L2P replyMsg_L2P = msg as ReplyMahjongHouseVote_L2P;
            ReplyMahjongHouseVote_P2T replyMsg_P2T = new ReplyMahjongHouseVote_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.houseVoteStatus = replyMsg_L2P.houseVoteStatus;
            replyMsg_P2T.voteStatus = replyMsg_L2P.voteStatus;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvMahjongHouseVote(int peerId, bool inbound, object msg)
        {
            RecvMahjongHouseVote_L2P recvMsg_L2P = msg as RecvMahjongHouseVote_L2P;
            RecvMahjongHouseVote_P2T recvMsg_P2T = new RecvMahjongHouseVote_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.houseVoteStatus = recvMsg_L2P.houseVoteStatus;
            recvMsg_P2T.voteStatus = recvMsg_L2P.voteStatus;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqReadyMahjongHouse(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestReadyMahjongHouse_T2P reqMsg_T2P = msg as RequestReadyMahjongHouse_T2P;
            RequestReadyMahjongHouse_P2L reqMsg_P2L = new RequestReadyMahjongHouse_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyReadyMahjongHouse(int peerId, bool inbound, object msg)
        {
            ReplyReadyMahjongHouse_L2P replyMsg_L2P = msg as ReplyReadyMahjongHouse_L2P;
            ReplyReadyMahjongHouse_P2T replyMsg_P2T = new ReplyReadyMahjongHouse_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvReadyMahjongHouse(int peerId, bool inbound, object msg)
        {
            RecvReadyMahjongHouse_L2P replyMsg_L2P = msg as RecvReadyMahjongHouse_L2P;
            RecvReadyMahjongHouse_P2T replyMsg_P2T = new RecvReadyMahjongHouse_P2T();
            replyMsg_P2T.readyIndex = replyMsg_L2P.readyIndex;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvBeginMahjong(int peerId, bool inbound, object msg)
        {
            RecvBeginMahjong_L2P recvMsg_L2P = msg as RecvBeginMahjong_L2P;
            RecvBeginMahjong_P2T recvMsg_P2T = new RecvBeginMahjong_P2T();
            recvMsg_P2T.currentBureau = recvMsg_L2P.currentBureau;
            recvMsg_P2T.currentShowCard = recvMsg_L2P.currentShowCard;
            recvMsg_P2T.houseStatus = recvMsg_L2P.houseStatus;
            recvMsg_P2T.housePlayerStatus = recvMsg_L2P.housePlayerStatus;
            recvMsg_P2T.zhuangIndex = recvMsg_L2P.zhuangIndex;
            recvMsg_P2T.remainMahjongCount = recvMsg_L2P.remainMahjongCount;
            recvMsg_P2T.startDisplayType = recvMsg_L2P.startDisplayType;
            recvMsg_P2T.mahjongList.AddRange(recvMsg_L2P.mahjongList);
            recvMsg_P2T.mahjongCountList.AddRange(recvMsg_L2P.mahjongCountList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqMahjongPendulum(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestMahjongPendulum_T2P reqMsg_T2P = msg as RequestMahjongPendulum_T2P;
            RequestMahjongPendulum_P2L reqMsg_P2L = new RequestMahjongPendulum_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.startDisplayType = reqMsg_T2P.startDisplayType;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyMahjongPendulum(int peerId, bool inbound, object msg)
        {
            ReplyMahjongPendulum_L2P replyMsg_L2P = msg as ReplyMahjongPendulum_L2P;
            ReplyMahjongPendulum_P2T replyMsg_P2T = new ReplyMahjongPendulum_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.startDisplayType = replyMsg_L2P.startDisplayType;
            replyMsg_P2T.mahjongList.AddRange(replyMsg_L2P.mahjongList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvMahjongPendulum(int peerId, bool inbound, object msg)
        {
            RecvMahjongPendulum_L2P recvMsg_L2P = msg as RecvMahjongPendulum_L2P;
            RecvMahjongPendulum_P2T recvMsg_P2T = new RecvMahjongPendulum_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.startDisplayType = recvMsg_L2P.startDisplayType;
            recvMsg_P2T.mahjongList.AddRange(recvMsg_L2P.mahjongList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqMahjongPendulumDice(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestMahjongPendulumDice_T2P reqMsg_T2P = msg as RequestMahjongPendulumDice_T2P;
            RequestMahjongPendulumDice_P2L reqMsg_P2L = new RequestMahjongPendulumDice_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyMahjongPendulumDice(int peerId, bool inbound, object msg)
        {
            ReplyMahjongPendulumDice_L2P replyMsg_L2P = msg as ReplyMahjongPendulumDice_L2P;
            ReplyMahjongPendulumDice_P2T replyMsg_P2T = new ReplyMahjongPendulumDice_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.nextPendulumIndex = replyMsg_L2P.nextPendulumIndex;
            replyMsg_P2T.pendulumDice = replyMsg_L2P.pendulumDice;
            replyMsg_P2T.playerIntegralList.AddRange(replyMsg_L2P.playerIntegralList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvMahjongPendulumDice(int peerId, bool inbound, object msg)
        {
            RecvMahjongPendulumDice_L2P recvMsg_L2P = msg as RecvMahjongPendulumDice_L2P;
            RecvMahjongPendulumDice_P2T recvMsg_P2T = new RecvMahjongPendulumDice_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.nextPendulumIndex = recvMsg_L2P.nextPendulumIndex;
            recvMsg_P2T.pendulumDice = recvMsg_L2P.pendulumDice;
            recvMsg_P2T.playerIntegralList.AddRange(recvMsg_L2P.playerIntegralList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvMahjongEndPendulum(int peerId, bool inbound, object msg)
        {
            RecvMahjongEndPendulum_L2P recvMsg_L2P = msg as RecvMahjongEndPendulum_L2P;
            RecvMahjongEndPendulum_P2T recvMsg_P2T = new RecvMahjongEndPendulum_P2T();
            recvMsg_P2T.currentShowCard = recvMsg_L2P.currentShowCard;
            recvMsg_P2T.houseStatus = recvMsg_L2P.houseStatus;
            recvMsg_P2T.housePlayerStatus = recvMsg_L2P.housePlayerStatus;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqMahjongHouseInfo(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestMahjongHouseInfo_T2P reqMsg_T2P = msg as RequestMahjongHouseInfo_T2P;
            RequestMahjongHouseInfo_P2L reqMsg_P2L = new RequestMahjongHouseInfo_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyMahjongHouseInfo(int peerId, bool inbound, object msg)
        {
            ReplyMahjongHouseInfo_L2P replyMsg_L2P = msg as ReplyMahjongHouseInfo_L2P;
            ReplyMahjongHouseInfo_P2T replyMsg_P2T = new ReplyMahjongHouseInfo_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.houseId = replyMsg_L2P.houseId;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.mahjongType = replyMsg_L2P.mahjongType;
            replyMsg_P2T.currentBureau = replyMsg_L2P.currentBureau;
            replyMsg_P2T.maxBureau = replyMsg_L2P.maxBureau;
            replyMsg_P2T.currentShowCard = replyMsg_L2P.currentShowCard;
            replyMsg_P2T.currentWhoPlay = replyMsg_L2P.currentWhoPlay;
            replyMsg_P2T.houseStatus = replyMsg_L2P.houseStatus;
            replyMsg_P2T.catchBird = replyMsg_L2P.catchBird;
            replyMsg_P2T.housePropertyType = replyMsg_L2P.housePropertyType;
            replyMsg_P2T.currentMahjongList.AddRange(replyMsg_L2P.currentMahjongList);
            replyMsg_P2T.mahjongOnlineNode = replyMsg_L2P.mahjongOnlineNode;
            replyMsg_P2T.houseVoteTime = replyMsg_L2P.houseVoteTime;
            replyMsg_P2T.maxPlayerNum = replyMsg_L2P.maxPlayerNum;
            replyMsg_P2T.remainMahjongCount = replyMsg_L2P.remainMahjongCount;
            replyMsg_P2T.bNeedOperat = replyMsg_L2P.bNeedOperat;
            replyMsg_P2T.zhuangIndex = replyMsg_L2P.zhuangIndex;
            replyMsg_P2T.mahjongSpecialType = replyMsg_L2P.mahjongSpecialType;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;
            replyMsg_P2T.flutter = replyMsg_L2P.flutter;
            replyMsg_P2T.bFakeHu = replyMsg_L2P.bFakeHu;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqShowMahjong(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestShowMahjong_T2P reqMsg_T2P = msg as RequestShowMahjong_T2P;
            RequestShowMahjong_P2L reqMsg_P2L = new RequestShowMahjong_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.mahjongNode = reqMsg_T2P.mahjongNode;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyShowMahjong(int peerId, bool inbound, object msg)
        {
            ReplyShowMahjong_L2P replyMsg_L2P = msg as ReplyShowMahjong_L2P;
            ReplyShowMahjong_P2T replyMsg_P2T = new ReplyShowMahjong_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.mahjongNode = replyMsg_L2P.mahjongNode;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvShowMahjong(int peerId, bool inbound, object msg)
        {
            RecvShowMahjong_L2P recvMsg_L2P = msg as RecvShowMahjong_L2P;
            RecvShowMahjong_P2T recvMsg_P2T = new RecvShowMahjong_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.bIsNeed = recvMsg_L2P.bIsNeed;
            recvMsg_P2T.mahjongNode = recvMsg_L2P.mahjongNode;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvGiveOffMahjong(int peerId, bool inbound, object msg)
        {
            RecvGiveOffMahjong_L2P recvMsg_L2P = msg as RecvGiveOffMahjong_L2P;
            RecvGiveOffMahjong_P2T recvMsg_P2T = new RecvGiveOffMahjong_P2T();
            recvMsg_P2T.housePlayerStatus = recvMsg_L2P.housePlayerStatus;
            recvMsg_P2T.currentShowCard = recvMsg_L2P.currentShowCard;
            recvMsg_P2T.mahjongNode = recvMsg_L2P.mahjongNode;
            recvMsg_P2T.playerIntegralList.AddRange(recvMsg_L2P.playerIntegralList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqOperatMahjong(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestOperatMahjong_T2P reqMsg_T2P = msg as RequestOperatMahjong_T2P;
            RequestOperatMahjong_P2L reqMsg_P2L = new RequestOperatMahjong_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.operatType = reqMsg_T2P.operatType;
            reqMsg_P2L.mahjongNode.AddRange(reqMsg_T2P.mahjongNode);

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyOperatMahjong(int peerId, bool inbound, object msg)
        {
            ReplyOperatMahjong_L2P replyMsg_L2P = msg as ReplyOperatMahjong_L2P;
            ReplyOperatMahjong_P2T replyMsg_P2T = new ReplyOperatMahjong_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.operatType = replyMsg_L2P.operatType;
            replyMsg_P2T.kongType = replyMsg_L2P.kongType;
            replyMsg_P2T.meldType = replyMsg_L2P.meldType;
            replyMsg_P2T.currentShowCard = replyMsg_L2P.currentShowCard;
            replyMsg_P2T.housePlayerStatus = replyMsg_L2P.housePlayerStatus;
            replyMsg_P2T.bGiveUpWin = replyMsg_L2P.bGiveUpWin;
            replyMsg_P2T.bOperatMySelf = replyMsg_L2P.bOperatMySelf;
            replyMsg_P2T.bOperatHand = replyMsg_L2P.bOperatHand;
            replyMsg_P2T.bFakeHu = replyMsg_L2P.bFakeHu;
            replyMsg_P2T.mahjongNode.AddRange(replyMsg_L2P.mahjongNode);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvOperatMahjong(int peerId, bool inbound, object msg)
        {
            RecvOperatMahjong_L2P recvMsg_L2P = msg as RecvOperatMahjong_L2P;
            RecvOperatMahjong_P2T recvMsg_P2T = new RecvOperatMahjong_P2T();
            recvMsg_P2T.operatType = recvMsg_L2P.operatType;
            recvMsg_P2T.kongType = recvMsg_L2P.kongType;
            recvMsg_P2T.meldType = recvMsg_L2P.meldType;
            recvMsg_P2T.currentShowCard = recvMsg_L2P.currentShowCard;
            recvMsg_P2T.housePlayerStatus = recvMsg_L2P.housePlayerStatus;
            recvMsg_P2T.bNeedKongWin = recvMsg_L2P.bNeedKongWin;
            recvMsg_P2T.bGiveUpWin = recvMsg_L2P.bGiveUpWin;
            recvMsg_P2T.bOperatMySelf = recvMsg_L2P.bOperatMySelf;
            recvMsg_P2T.bOperatHand = recvMsg_L2P.bOperatHand;
            recvMsg_P2T.bFakeHu = recvMsg_L2P.bFakeHu;
            recvMsg_P2T.mahjongNode.AddRange(recvMsg_L2P.mahjongNode);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvPlayerWinMahjong(int peerId, bool inbound, object msg)
        {
            RecvPlayerWinMahjong_L2P recvMsg_L2P = msg as RecvPlayerWinMahjong_L2P;
            RecvPlayerWinMahjong_P2T recvMsg_P2T = new RecvPlayerWinMahjong_P2T();
            recvMsg_P2T.winType = recvMsg_L2P.winType;
            recvMsg_P2T.winPlayerList.AddRange(recvMsg_L2P.winPlayerList);
            recvMsg_P2T.showBirdIndex = recvMsg_L2P.showBirdIndex;
            recvMsg_P2T.mahjongBirdList.AddRange(recvMsg_L2P.mahjongBirdList);
            recvMsg_P2T.mahjongPlayerIndex = recvMsg_L2P.mahjongPlayerIndex;
            recvMsg_P2T.specialWinMahjongType = recvMsg_L2P.specialWinMahjongType;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvKongMahjong(int peerId, bool inbound, object msg)
        {
            RecvKongMahjong_L2P recvMsg_L2P = msg as RecvKongMahjong_L2P;
            RecvKongMahjong_P2T recvMsg_P2T = new RecvKongMahjong_P2T();
            recvMsg_P2T.plyerIndex = recvMsg_L2P.plyerIndex;
            recvMsg_P2T.bNeed = recvMsg_L2P.bNeed;
            recvMsg_P2T.bFakeHu = recvMsg_L2P.bFakeHu;
            recvMsg_P2T.kongMahjongList.AddRange(recvMsg_L2P.kongMahjongList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvSettlementMahjong(int peerId, bool inbound, object msg)
        {
            RecvSettlementMahjong_L2P recvMsg_L2P = msg as RecvSettlementMahjong_L2P;
            RecvSettlementMahjong_P2T recvMsg_P2T = new RecvSettlementMahjong_P2T();
            recvMsg_P2T.bLiuJu = recvMsg_L2P.bLiuJu;
            recvMsg_P2T.mahjongSettlement = recvMsg_L2P.mahjongSettlement;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvEndSettlementMahjong(int peerId, bool inbound, object msg)
        {
            RecvEndSettlementMahjong_L2P recvMsg_L2P = msg as RecvEndSettlementMahjong_L2P;
            RecvEndSettlementMahjong_P2T recvMsg_P2T = new RecvEndSettlementMahjong_P2T();
            recvMsg_P2T.houseStatus = recvMsg_L2P.houseStatus;
            recvMsg_P2T.allIntegral = recvMsg_L2P.allIntegral;
            recvMsg_P2T.ticketsNode = recvMsg_L2P.ticketsNode;
            recvMsg_P2T.mahjongEndSettlementList.AddRange(recvMsg_L2P.mahjongEndSettlementList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnRecvSelectSeabedMahjong(int peerId, bool inbound, object msg)
        {
            RecvSelectSeabedMahjong_L2P recvMsg_L2P = msg as RecvSelectSeabedMahjong_L2P;
            RecvSelectSeabedMahjong_P2T recvMsg_P2T = new RecvSelectSeabedMahjong_P2T();
            recvMsg_P2T.houseStatus = recvMsg_L2P.houseStatus;
            recvMsg_P2T.selectSeabedIndex = recvMsg_L2P.selectSeabedIndex;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqPlayerSelectSeabed(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestPlayerSelectSeabed_T2P reqMsg_T2P = msg as RequestPlayerSelectSeabed_T2P;
            RequestPlayerSelectSeabed_P2L reqMsg_P2L = new RequestPlayerSelectSeabed_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.bNeed = reqMsg_T2P.bNeed;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyPlayerSelectSeabed(int peerId, bool inbound, object msg)
        {
            ReplyPlayerSelectSeabed_L2P replyMsg_L2P = msg as ReplyPlayerSelectSeabed_L2P;
            ReplyPlayerSelectSeabed_P2T replyMsg_P2T = new ReplyPlayerSelectSeabed_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.bNeed = replyMsg_L2P.bNeed;
            replyMsg_P2T.bFakeHu = replyMsg_L2P.bFakeHu;
            replyMsg_P2T.seabedMahjongList.AddRange(replyMsg_L2P.seabedMahjongList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvPlayerSelectSeabed(int peerId, bool inbound, object msg)
        {
            RecvPlayerSelectSeabed_L2P recvMsg_L2P = msg as RecvPlayerSelectSeabed_L2P;
            RecvPlayerSelectSeabed_P2T recvMsg_P2T = new RecvPlayerSelectSeabed_P2T();
            recvMsg_P2T.bNeedHu = recvMsg_L2P.bNeedHu;
            recvMsg_P2T.playerIndex = recvMsg_L2P.playerIndex;
            recvMsg_P2T.bFakeHu = recvMsg_L2P.bFakeHu;
            recvMsg_P2T.seabedMahjongList.AddRange(recvMsg_L2P.seabedMahjongList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqMahjongMidwayPendulum(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestMahjongMidwayPendulum_T2P reqMsg_T2P = msg as RequestMahjongMidwayPendulum_T2P;
            RequestMahjongMidwayPendulum_P2L reqMsg_P2L = new RequestMahjongMidwayPendulum_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.mahjongNode = reqMsg_T2P.mahjongNode;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyMahjongMidwayPendulum(int peerId, bool inbound, object msg)
        {
            ReplyMahjongMidwayPendulum_L2P replyMsg_L2P = msg as ReplyMahjongMidwayPendulum_L2P;
            ReplyMahjongMidwayPendulum_P2T replyMsg_P2T = new ReplyMahjongMidwayPendulum_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.mahjongList.AddRange(replyMsg_L2P.mahjongList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvMahjongMidwayPendulum(int peerId, bool inbound, object msg)
        {
            RecvMahjongMidwayPendulum_L2P recvMsg_L2P = msg as RecvMahjongMidwayPendulum_L2P;
            RecvMahjongMidwayPendulum_P2T recvMsg_P2T = new RecvMahjongMidwayPendulum_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.mahjongList.AddRange(recvMsg_L2P.mahjongList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqMahjongMidwayPendulumDice(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestMahjongMidwayPendulumDice_T2P reqMsg_T2P = msg as RequestMahjongMidwayPendulumDice_T2P;
            RequestMahjongMidwayPendulumDice_P2L reqMsg_P2L = new RequestMahjongMidwayPendulumDice_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyMahjongMidwayPendulumDice(int peerId, bool inbound, object msg)
        {
            ReplyMahjongMidwayPendulumDice_L2P replyMsg_L2P = msg as ReplyMahjongMidwayPendulumDice_L2P;
            ReplyMahjongMidwayPendulumDice_P2T replyMsg_P2T = new ReplyMahjongMidwayPendulumDice_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.pendulumDice = replyMsg_L2P.pendulumDice;
            replyMsg_P2T.playerIntegralList.AddRange(replyMsg_L2P.playerIntegralList);

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvMahjongMidwayPendulumDice(int peerId, bool inbound, object msg)
        {
            RecvMahjongMidwayPendulumDice_L2P recvMsg_L2P = msg as RecvMahjongMidwayPendulumDice_L2P;
            RecvMahjongMidwayPendulumDice_P2T recvMsg_P2T = new RecvMahjongMidwayPendulumDice_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.pendulumDice = recvMsg_L2P.pendulumDice;
            recvMsg_P2T.playerIntegralList.AddRange(recvMsg_L2P.playerIntegralList);

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqMahjongOverallRecord(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestMahjongOverallRecord_T2P reqMsg_T2P = msg as RequestMahjongOverallRecord_T2P;
            RequestMahjongOverallRecord_P2L reqMsg_P2L = new RequestMahjongOverallRecord_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyMahjongOverallRecord(int peerId, bool inbound, object msg)
        {
            ReplyMahjongOverallRecord_L2P replyMsg_L2P = msg as ReplyMahjongOverallRecord_L2P;
            ReplyMahjongOverallRecord_P2T replyMsg_P2T = new ReplyMahjongOverallRecord_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.overallRecord = replyMsg_L2P.overallRecord;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqMahjongBureauRecord(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestMahjongBureauRecord_T2P reqMsg_T2P = msg as RequestMahjongBureauRecord_T2P;
            RequestMahjongBureauRecord_P2L reqMsg_P2L = new RequestMahjongBureauRecord_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.onlyHouseId = reqMsg_T2P.onlyHouseId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyMahjongBureauRecord(int peerId, bool inbound, object msg)
        {
            ReplyMahjongBureauRecord_L2P replyMsg_L2P = msg as ReplyMahjongBureauRecord_L2P;
            ReplyMahjongBureauRecord_P2T replyMsg_P2T = new ReplyMahjongBureauRecord_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.bureauRecord = replyMsg_L2P.bureauRecord;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqMahjongBureauPlayback(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestMahjongBureauPlayback_T2P reqMsg_T2P = msg as RequestMahjongBureauPlayback_T2P;
            RequestMahjongBureauPlayback_P2L reqMsg_P2L = new RequestMahjongBureauPlayback_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.onlyHouseId = reqMsg_T2P.onlyHouseId;
            reqMsg_P2L.bureau = reqMsg_T2P.bureau;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyMahjongBureauPlayback(int peerId, bool inbound, object msg)
        {
            ReplyMahjongBureauPlayback_L2P replyMsg_L2P = msg as ReplyMahjongBureauPlayback_L2P;
            ReplyMahjongBureauPlayback_P2T replyMsg_P2T = new ReplyMahjongBureauPlayback_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.onlyHouseId = replyMsg_L2P.onlyHouseId;
            replyMsg_P2T.bureau = replyMsg_L2P.bureau;
            replyMsg_P2T.playerMahjong = replyMsg_L2P.playerMahjong;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
    }
}
#endif

