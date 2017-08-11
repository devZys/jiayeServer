using LegendProtocol;
using LegendServerProxy.Core;
using LegendServerProxy.Distributed;
using LegendServerProxy.Login;
using LegendServerProxyDefine;
using System;
using System.Text;

namespace LegendServerProxy.MainCity
{
    public class MainCityMsgProxy : ServerMsgProxy
    {
        private MainCityMain main;

        public MainCityMsgProxy(MainCityMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void KickOutInvalidClient(string userId)
        {
            int myselfServerId = ModuleManager.Get<DistributedMain>().GetMyselfServerId();
            ServerUtil.RecordLog(LogType.Error, "UserId：" + userId + " 为非法客户端! 当前服务器：【proxy:" + myselfServerId + "】");

            SessionManager.Instance.KickOutClient(userId);
            return;
        }
        public void OnReqOnline(int peerId, bool inbound, object msg)
        {
            RequestOnline_T2P reqMsg_T2P = msg as RequestOnline_T2P;

            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);           

            if (!ModuleManager.Get<LoginMain>().TokenIsOK(reqMsg_T2P.accessToken, reqMsg_T2P.userId, requesterSession.ip))
            {
                KickOutInvalidClient(reqMsg_T2P.userId);
                return;
            }
            int routeServerID = ModuleManager.Get<LoginMain>().GetRouteServerID(reqMsg_T2P.userId);
            if (routeServerID <= 0)
            {
                ServerUtil.RecordLog(LogType.Error, "上线时找不到分配好的逻辑服务器：【routeServerID:" + routeServerID + "】");
                KickOutInvalidClient(reqMsg_T2P.userId);
                return;
            }

            requesterSession.userId = reqMsg_T2P.userId;
            TokenInfo token = ModuleManager.Get<LoginMain>().GetToken(reqMsg_T2P.userId);
            requesterSession.summonerId = token.summonerId;
            requesterSession.logicServerID = routeServerID;
            requesterSession.auth = ModuleManager.Get<LoginMain>().GetUserAuthority(reqMsg_T2P.userId);

            InboundClientSession oldSession = SessionManager.Instance.GetInboundClientSessionByUserId(requesterSession.userId, requesterSession.peer.ConnectionId);
            if (oldSession != null && oldSession.peer != null)
            {
                //在相同网关异地登陆踢除旧连接（不同网关异地登陆处理是在Enter World服务器时由World通知旧网关做的）
                oldSession.peer.Disconnect();
                
                if (oldSession.logicServerID != routeServerID)
                {
                    //相同网关但不同逻辑服则需要通知旧逻辑服删除Summoner,将由逻辑服更新自己的CCU
                    SessionManager.Instance.RemoveInboundClientSession(oldSession.peer.ConnectionId, true, true);
                }
                else
                {
                    //相同网关且相同的逻辑服则只需要删除网关旧Session,不需要通知逻辑服删除Summoner,因为即将要到来，但会在World里通知该逻辑服CCU减1，因为在Center分配时已加1，哪怕是在同一个逻辑服
                    SessionManager.Instance.RemoveInboundClientSession(oldSession.peer.ConnectionId, false, true);
                }
            }

            //往逻辑服务器登陆
            RequestOnline_P2L reqMsg_P2L = new RequestOnline_P2L();
            reqMsg_P2L.userId = requesterSession.userId;
            reqMsg_P2L.acServerId = requesterSession.acServerID;
            reqMsg_P2L.ip = requesterSession.ip;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);

            //网关认证完成移除token
            ModuleManager.Get<LoginMain>().RemoveToken(requesterSession.userId);

            ServerUtil.RecordLog(LogType.Debug, "玩家：" + requesterSession.userId + " 进入网关!");
        }   
        public void OnReplyOnline(int peerId, bool inbound, object msg)
        {
            ReplyOnline_L2P replyMsg_L2P = msg as ReplyOnline_L2P;

            ReplyOnline_P2T replyMsg_P2T = new ReplyOnline_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.summonerId = replyMsg_L2P.summonerId;
            replyMsg_P2T.roomCard = replyMsg_L2P.roomCard;
            replyMsg_P2T.houseId = replyMsg_L2P.houseId;
            replyMsg_P2T.competitionKey = replyMsg_L2P.competitionKey;
            replyMsg_P2T.loginTime = replyMsg_L2P.loginTime;
            replyMsg_P2T.ip = replyMsg_L2P.ip;
            replyMsg_P2T.allIntegral = replyMsg_L2P.allIntegral;
            replyMsg_P2T.bFirstLogin = replyMsg_L2P.bFirstLogin;
            replyMsg_P2T.md5Announcement = replyMsg_L2P.md5Announcement;
            replyMsg_P2T.bDailySharing = replyMsg_L2P.bDailySharing;
            replyMsg_P2T.bindCode = replyMsg_L2P.bindCode;

            SendClientMsg(replyMsg_L2P.userId, replyMsg_P2T);
        }   
        public void OnKickOut(int peerId, bool inbound, object msg)
        {
            KickLogout_W2P msg_W2P = msg as KickLogout_W2P;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByUserId(msg_W2P.userId);
            if (session != null && session.peer != null)
            {
                //踢除旧连接
                session.peer.Disconnect();

                if (msg_W2P.isPlaceOtherLogin)
                {
                    //在不同网关异地登陆
                    if (session.logicServerID != msg_W2P.logicServerId)
                    {
                        //如果分配不同逻辑服则需要通知旧逻辑服删除Summoner,将由逻辑服更新自己的CCU
                        SessionManager.Instance.RemoveInboundClientSession(session.peer.ConnectionId, true, true);
                    }
                    else
                    {
                        //如果分配相同的逻辑服则只需要删除网关旧Session,不需要通知逻辑服删除Summoner,因为玩家刚刚才进逻辑服
                        SessionManager.Instance.RemoveInboundClientSession(session.peer.ConnectionId, false, true);
                    }
                }
                else
                {
                    //普通踢人
                    SessionManager.Instance.RemoveInboundClientSession(session.peer.ConnectionId, true, false);
                }
            }
        }
        public void NotifyLogicServerPlayerLogout(int logicServerId, string userId, bool byPlaceOtherLogin)
        {
            RequestLogout_P2L reqMsg_P2L = new RequestLogout_P2L();            
            reqMsg_P2L.userId = userId;
            reqMsg_P2L.byPlaceOtherLogin = byPlaceOtherLogin;
            SendLogicMsg(reqMsg_P2L, logicServerId);
        }
        public void OnTransmitPlayerInfo(int peerId, bool inbound, object msg)
        {
            TransmitPlayerInfo_L2P transmitMsg_L2P = msg as TransmitPlayerInfo_L2P;


            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionBySummonerId(transmitMsg_L2P.summonerId);
            if (session != null && transmitMsg_L2P.summoner != null && transmitMsg_L2P.parameter != null)
            {
                //转给目标从自己的逻辑服务器转给目标逻辑服务器
                TransmitPlayerInfo_P2L transmitMsg_P2L = new TransmitPlayerInfo_P2L();
                transmitMsg_P2L.summoner = transmitMsg_L2P.summoner;
                transmitMsg_P2L.parameter = transmitMsg_L2P.parameter;
                transmitMsg_P2L.type = transmitMsg_L2P.type;
                SendLogicMsg(transmitMsg_P2L, transmitMsg_L2P.targetLogic);

                //修改目标在本网关的逻辑服指向
                session.logicServerID = transmitMsg_L2P.targetLogic;
            }
        }

        public void OnSendFeedback(int peerId, bool inbound, object msg)
        {
            SendFeedback_T2P sendFeedback_T2P = msg as SendFeedback_T2P;
            if (string.IsNullOrEmpty(sendFeedback_T2P.phoneNumber) || string.IsNullOrEmpty(sendFeedback_T2P.feedback)) return;

            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);
            if (Encoding.Default.GetByteCount(sendFeedback_T2P.phoneNumber) > main.feedbackPhoneNumberSizeLimit ||
                Encoding.Default.GetByteCount(sendFeedback_T2P.feedback) > main.feedbackTextSizeLimit)
            {
                //有数据超过长度
                return;
            }
            if ((DateTime.Now - session.lastSendFeedbackMsgTime).TotalMilliseconds < main.sendFeedbackMsgCD * 1000)
            {
                //发的太频繁
                return;
            }

            //将消息转发给该玩家所在的逻辑服务器做处理
            SendFeedback_P2L sendFeedback_P2L = new SendFeedback_P2L();
            sendFeedback_P2L.requesterId = session.summonerId;
            sendFeedback_P2L.phoneNumber = sendFeedback_T2P.phoneNumber;
            sendFeedback_P2L.feedback = sendFeedback_T2P.feedback;
            SendLogicMsg(sendFeedback_P2L, session.logicServerID);

            session.lastSendFeedbackMsgTime = DateTime.Now;
        }
        public void OnReqGetDailySharing(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestGetDailySharing_T2P reqMsg_T2P = msg as RequestGetDailySharing_T2P;
            RequestGetDailySharing_P2L reqMsg_P2L = new RequestGetDailySharing_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyGetDailySharing(int peerId, bool inbound, object msg)
        {
            ReplyGetDailySharing_L2P replyMsg_L2P = msg as ReplyGetDailySharing_L2P;
            ReplyGetDailySharing_P2T replyMsg_P2T = new ReplyGetDailySharing_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.houseCard = replyMsg_L2P.houseCard;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvPlayerHouseCard(int peerId, bool inbound, object msg)
        {
            RecvPlayerHouseCard_L2P replyMsg_L2P = msg as RecvPlayerHouseCard_L2P;
            RecvPlayerHouseCard_P2T replyMsg_P2T = new RecvPlayerHouseCard_P2T();
            replyMsg_P2T.houseCard = replyMsg_L2P.houseCard;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqSendChat(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestSendChat_T2P reqMsg_T2P = msg as RequestSendChat_T2P;
            RequestSendChat_P2L reqMsg_P2L = new RequestSendChat_P2L();

            if (string.IsNullOrEmpty(reqMsg_T2P.chatText))
            {
                ReplySendChat_P2T replyMsg_P2T = new ReplySendChat_P2T();
                replyMsg_P2T.result = ResultCode.Wrong;
                SendClientMsg(peerId, replyMsg_P2T);
                return;
            }
            if (Encoding.Default.GetByteCount(reqMsg_T2P.chatText) > main.chatMsgSizeLimit)
            {
                //消息太长
                ReplySendChat_P2T replyMsg_P2T = new ReplySendChat_P2T();
                replyMsg_P2T.result = ResultCode.ChatContentIsTooLong;
                SendClientMsg(peerId, replyMsg_P2T);
                return;
            }
            if ((DateTime.Now - requesterSession.lastSendChatMsgTime).TotalMilliseconds < main.sendMsgCD * 1000)
            {
                //发送太频繁
                ReplySendChat_P2T replyMsg_P2T = new ReplySendChat_P2T();
                replyMsg_P2T.result = ResultCode.ChatFrequencyTooFast;
                SendClientMsg(peerId, replyMsg_P2T);
                return;
            }
            else
            {
                requesterSession.lastSendChatMsgTime = DateTime.Now;
            }
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.chatType = reqMsg_T2P.chatType;
            reqMsg_P2L.chatText = reqMsg_T2P.chatText;
            reqMsg_P2L.chatTime = reqMsg_T2P.chatTime;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplySendChat(int peerId, bool inbound, object msg)
        {
            ReplySendChat_L2P replyMsg_L2P = msg as ReplySendChat_L2P;
            ReplySendChat_P2T replyMsg_P2T = new ReplySendChat_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.chatType = replyMsg_L2P.chatType;
            replyMsg_P2T.chatText = replyMsg_L2P.chatText;
            replyMsg_P2T.chatTime = replyMsg_L2P.chatTime;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnRecvSendChat(int peerId, bool inbound, object msg)
        {
            RecvSendChat_L2P replyMsg_L2P = msg as RecvSendChat_L2P;
            RecvSendChat_P2T replyMsg_P2T = new RecvSendChat_P2T();
            replyMsg_P2T.sendIndex = replyMsg_L2P.sendIndex;
            replyMsg_P2T.chatType = replyMsg_L2P.chatType;
            replyMsg_P2T.chatText = replyMsg_L2P.chatText;
            replyMsg_P2T.chatTime = replyMsg_L2P.chatTime;

            SendClientMsgBySummonerId(replyMsg_L2P.summonerId, replyMsg_P2T);
        }
        public void OnReqPlayerLineType(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestPlayerLineType_T2P reqMsg_T2P = msg as RequestPlayerLineType_T2P;
            RequestPlayerLineType_P2L reqMsg_P2L = new RequestPlayerLineType_P2L();
            reqMsg_P2L.summonerId = requesterSession.summonerId;
            reqMsg_P2L.lineType = reqMsg_T2P.lineType;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnRecvPlayerLineType(int peerId, bool inbound, object msg)
        {
            RecvPlayerLineType_L2P recvMsg_L2P = msg as RecvPlayerLineType_L2P;
            RecvPlayerLineType_P2T recvMsg_P2T = new RecvPlayerLineType_P2T();
            recvMsg_P2T.index = recvMsg_L2P.index;
            recvMsg_P2T.lineType = recvMsg_L2P.lineType;

            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqBindBelong(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestBindBelong_T2P msg_T2P = msg as RequestBindBelong_T2P;
            RequestBindBelong_P2L msg_P2L = new RequestBindBelong_P2L();

            msg_P2L.requestSummonerId = requesterSession.summonerId;
            msg_P2L.bindCode = msg_T2P.bindCode;
            SendLogicMsg(msg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyBindBelong(int peerId, bool inbound, object msg)
        {
            ReplyBindBelong_L2P msg_L2P = msg as ReplyBindBelong_L2P;
            ReplyBindBelong_P2T msg_P2T = new ReplyBindBelong_P2T();

            msg_P2T.result = msg_L2P.result;
            msg_P2T.bindCode = msg_L2P.bindCode;
            msg_P2T.rewardRoomCard = msg_L2P.rewardRoomCard;
            SendClientMsgBySummonerId(msg_L2P.requestSummonerId, msg_P2T);
        }
        public void OnReqPlayerHeadImgUrl(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestPlayerHeadImgUrl_T2P reqMsg_T2P = msg as RequestPlayerHeadImgUrl_T2P;
            RequestPlayerHeadImgUrl_P2L reqMsg_P2L = new RequestPlayerHeadImgUrl_P2L();

            reqMsg_P2L.requestSummonerId = requesterSession.summonerId;
            reqMsg_P2L.summonerId = reqMsg_T2P.summonerId;
            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyPlayerHeadImgUrl(int peerId, bool inbound, object msg)
        {
            ReplyPlayerHeadImgUrl_L2P replyMsg_L2P = msg as ReplyPlayerHeadImgUrl_L2P;
            ReplyPlayerHeadImgUrl_P2T replyMsg_P2T = new ReplyPlayerHeadImgUrl_P2T();

            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.summonerId = replyMsg_L2P.summonerId;
            replyMsg_P2T.headImgUrl = replyMsg_L2P.headImgUrl;
            SendClientMsgBySummonerId(replyMsg_L2P.requestSummonerId, replyMsg_P2T);
        }
        public void OnReqAnnouncement(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestAnnouncement_T2P reqMsg_T2P = msg as RequestAnnouncement_T2P;
            RequestAnnouncement_P2L reqMsg_P2L = new RequestAnnouncement_P2L();

            reqMsg_P2L.requestSummonerId = requesterSession.summonerId;
            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyAnnouncement(int peerId, bool inbound, object msg)
        {
            ReplyAnnouncement_L2P replyMsg_L2P = msg as ReplyAnnouncement_L2P;
            ReplyAnnouncement_P2T replyMsg_P2T = new ReplyAnnouncement_P2T();

            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.announcement = replyMsg_L2P.announcement;
            SendClientMsgBySummonerId(replyMsg_L2P.requestSummonerId, replyMsg_P2T);
        }
        public void OnRecvUpdateAnnouncement(int peerId, bool inbound, object msg)
        {
            RecvUpdateAnnouncement_L2P recvMsg_L2P = msg as RecvUpdateAnnouncement_L2P;
            RecvUpdateAnnouncement_P2T recvMsg_P2T = new RecvUpdateAnnouncement_P2T();

            recvMsg_P2T.announcement = recvMsg_L2P.announcement;
            SendClientMsgBySummonerId(recvMsg_L2P.summonerId, recvMsg_P2T);
        }
        public void OnReqSavePlayerAddress(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestSavePlayerAddress_T2P reqMsg_T2P = msg as RequestSavePlayerAddress_T2P;
            RequestSavePlayerAddress_P2L reqMsg_P2L = new RequestSavePlayerAddress_P2L();
            reqMsg_P2L.requestSummonerId = requesterSession.summonerId;
            reqMsg_P2L.longitude = reqMsg_T2P.longitude;
            reqMsg_P2L.latitude = reqMsg_T2P.latitude;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReqOtherPlayerAddress(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestOtherPlayerAddress_T2P reqMsg_T2P = msg as RequestOtherPlayerAddress_T2P;
            RequestOtherPlayerAddress_P2L reqMsg_P2L = new RequestOtherPlayerAddress_P2L();
            reqMsg_P2L.requestSummonerId = requesterSession.summonerId;
            reqMsg_P2L.playerId = reqMsg_T2P.playerId;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnReplyOtherPlayerAddress(int peerId, bool inbound, object msg)
        {
            ReplyOtherPlayerAddress_L2P replyMsg_L2P = msg as ReplyOtherPlayerAddress_L2P;
            ReplyOtherPlayerAddress_P2T replyMsg_P2T = new ReplyOtherPlayerAddress_P2T();
            replyMsg_P2T.result = replyMsg_L2P.result;
            replyMsg_P2T.playerIndex = replyMsg_L2P.playerIndex;
            replyMsg_P2T.longitude = replyMsg_L2P.longitude;
            replyMsg_P2T.latitude = replyMsg_L2P.latitude;

            SendClientMsgBySummonerId(replyMsg_L2P.requestSummonerId, replyMsg_P2T);
        }
        public void OnReqPlayerOperateHosted(int peerId, bool inbound, object msg)
        {
            InboundClientSession requesterSession = SessionManager.Instance.GetInboundClientSessionByPeerId(peerId);

            RequestPlayerOperateHosted_T2P reqMsg_T2P = msg as RequestPlayerOperateHosted_T2P;
            RequestPlayerOperateHosted_P2L reqMsg_P2L = new RequestPlayerOperateHosted_P2L();
            reqMsg_P2L.requestSummonerId = requesterSession.summonerId;
            reqMsg_P2L.bHosted = reqMsg_T2P.bHosted;

            SendLogicMsg(reqMsg_P2L, requesterSession.logicServerID);
        }
        public void OnRecvPlayerHostedStatus(int peerId, bool inbound, object msg)
        {
            RecvPlayerHostedStatus_L2P replyMsg_L2P = msg as RecvPlayerHostedStatus_L2P;
            RecvPlayerHostedStatus_P2T replyMsg_P2T = new RecvPlayerHostedStatus_P2T();
            replyMsg_P2T.bHosted = replyMsg_L2P.bHosted;

            SendClientMsgBySummonerId(replyMsg_L2P.requestSummonerId, replyMsg_P2T);
        }
    }
}

