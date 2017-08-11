using LegendProtocol;
using LegendServerLogic.Core;
using LegendServerLogic.Actor.Summoner;
using System;
using LegendServerLogic.Record;
using System.Text;
using LegendServerLogic.Entity.Base;
using LegendServerLogic.Entity.Houses;
using LegendServerLogic.Distributed;
using System.Collections.Generic;
using LegendServerLogic.SpecialActivities;
using LegendServerCompetitionManager;
#if MAHJONG
using LegendServerLogic.Mahjong;
#elif RUNFAST
using LegendServerLogic.RunFast;
#elif WORDPLATE
using LegendServerLogic.WordPlate;
#endif

namespace LegendServerLogic.MainCity
{
    public class MainCityMsgProxy : ServerMsgProxy
    {
        private MainCityMain main;

        public MainCityMsgProxy(MainCityMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnReqOnline(int peerId, bool inbound, object msg)
        {
            RequestOnline_P2L reqMsg_P2L = msg as RequestOnline_P2L;

            OutboundSession proxySession = SessionManager.Instance.GetOutboundSessionByPeerId(peerId);

            RequestSummonerData_L2D reqMsg_L2D = new RequestSummonerData_L2D();
            reqMsg_L2D.userId = reqMsg_P2L.userId;
            reqMsg_L2D.ip = reqMsg_P2L.ip;
            reqMsg_L2D.acServerId = reqMsg_P2L.acServerId;
            reqMsg_L2D.proxyServerId = proxySession.serverID;

            SendDBMsg(reqMsg_L2D);
        }
        public void OnReplySummonerData(int peerId, bool inbound, object msg)
        {
            ReplySummonerData_D2L replyMsg_D2L = msg as ReplySummonerData_D2L;

            DateTime nowTime = DateTime.Now;

            EnterWorld_L2W enterWorldMsg = new EnterWorld_L2W();
            enterWorldMsg.id = replyMsg_D2L.id;
            enterWorldMsg.userId = replyMsg_D2L.userId;
            enterWorldMsg.nickName = replyMsg_D2L.nickName;
            enterWorldMsg.loginTime = nowTime.ToString();
            enterWorldMsg.acServerId = replyMsg_D2L.acServerId;
            enterWorldMsg.proxyServerId = replyMsg_D2L.proxyServerId;
            enterWorldMsg.logicServerId = root.ServerID;
            SendWorldMsg(enterWorldMsg);

            Summoner summoner = ObjectPoolManager<Summoner>.Instance.NewObject(
            replyMsg_D2L.id,
            replyMsg_D2L.userId,
            replyMsg_D2L.nickName,
            replyMsg_D2L.auth,
            replyMsg_D2L.houseId,
            nowTime,
            replyMsg_D2L.sex,
            replyMsg_D2L.belong,
            replyMsg_D2L.ip,
            replyMsg_D2L.allIntegral,
            replyMsg_D2L.bFirstLogin,
            replyMsg_D2L.dailySharingTime,
            replyMsg_D2L.bOpenHouse,
            replyMsg_D2L.recordBusinessList,
            replyMsg_D2L.ticketsList,
            replyMsg_D2L.competitionKey,
            replyMsg_D2L.proxyServerId,
            replyMsg_D2L.acServerId,
            replyMsg_D2L.roomCard);

            SummonerManager.Instance.AddSummoner(summoner.id, summoner);

            ReplyOnline_L2P replyMsg = new ReplyOnline_L2P();
            replyMsg.result = ResultCode.OK;
            replyMsg.userId = summoner.userId;
            replyMsg.summonerId = summoner.id;
            replyMsg.roomCard = summoner.roomCard;
            replyMsg.houseId = summoner.houseId;
            replyMsg.competitionKey = summoner.competitionKey;
            replyMsg.loginTime = summoner.loginTime.ToString();
            replyMsg.ip = summoner.ip;
            replyMsg.allIntegral = summoner.allIntegral;
            replyMsg.bFirstLogin = summoner.bFirstLogin;
            replyMsg.md5Announcement = MyMD5.EncryptToStr16(main.announcement);
            replyMsg.bDailySharing = summoner.dailySharingTime.Date == nowTime.Date;
            replyMsg.bindCode = summoner.belong;

            SendProxyMsg(replyMsg, summoner.proxyServerId);
        }
        public void OnReqLogout(int peerId, bool inbound, object msg)
        {
            RequestLogout_P2L reqMsg = msg as RequestLogout_P2L;

            Summoner request = SummonerManager.Instance.GetSummonerByUserId(reqMsg.userId);
            if (request == null) return;

            if (request.houseId > 0)
            {
                main.OnRecvPlayerLineType(request.houseId, request.userId, LineType.OffLine);
            }

            SummonerManager.Instance.RemoveSummoner(request.id);

            if (!reqMsg.byPlaceOtherLogin)
            {
                LeaveWorld_L2W leaveWorldMsg = new LeaveWorld_L2W();
                leaveWorldMsg.summonerId = request.id;
                SendWorldMsg(leaveWorldMsg);
            }
        }
        public void OnSendFeedback(int peerId, bool inbound, object msg)
        {
            SendFeedback_P2L sendFeedback_P2L = msg as SendFeedback_P2L;

            if (string.IsNullOrEmpty(sendFeedback_P2L.phoneNumber) || string.IsNullOrEmpty(sendFeedback_P2L.feedback)) return;

            Summoner sender = SummonerManager.Instance.GetSummonerById(sendFeedback_P2L.requesterId);
            if (sender == null) return;
            if (Encoding.Default.GetByteCount(sendFeedback_P2L.phoneNumber) > main.feedbackPhoneNumberSizeLimit ||
                Encoding.Default.GetByteCount(sendFeedback_P2L.feedback) > main.feedbackTextSizeLimit)
            {
                //有数据超过长度
                return;
            }
            if ((DateTime.Now - sender.lastSendFeedbackMsgTime).TotalMilliseconds < main.sendFeedbackMsgCD * 1000)
            {
                //发的太频繁
                return;
            }
            //将消息转发给该玩家所在的DB服务器做处理
            SendFeedback_L2D sendFeedback_L2D = new SendFeedback_L2D();
            sendFeedback_L2D.id = sender.id;
            sendFeedback_L2D.phoneNumber = sendFeedback_P2L.phoneNumber;
            sendFeedback_L2D.feedback = sendFeedback_P2L.feedback;
            SendDBMsg(sendFeedback_L2D);

            sender.lastSendFeedbackMsgTime = DateTime.Now;
        }
        public void OnReqBindBelong(int peerId, bool inbound, object msg)
        {
            RequestBindBelong_P2L msg_P2L = msg as RequestBindBelong_P2L;
            RequestBindBelong_L2D msg_L2D = new RequestBindBelong_L2D();

            msg_L2D.requestSummonerId = msg_P2L.requestSummonerId;
            msg_L2D.bindCode = msg_P2L.bindCode;
            SendDBMsg(msg_L2D);
        }
        public void OnReplyBindBelong(int peerId, bool inbound, object msg)
        {
            ReplyBindBelong_D2L replyMsg_D2L = msg as ReplyBindBelong_D2L;
            ReplyBindBelong_L2P replyMsg_L2P = new ReplyBindBelong_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.requestSummonerId);
            if (sender == null) return;

            replyMsg_L2P.requestSummonerId = replyMsg_D2L.requestSummonerId;
            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.bindCode = replyMsg_D2L.bindCode;
            replyMsg_L2P.rewardRoomCard = replyMsg_D2L.rewardRoomCard;
            SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
        }
        public void OnReqPlayerHeadImgUrl(int peerId, bool inbound, object msg)
        {
            RequestPlayerHeadImgUrl_P2L msg_P2L = msg as RequestPlayerHeadImgUrl_P2L;
            RequestPlayerHeadImgUrl_L2D msg_L2D = new RequestPlayerHeadImgUrl_L2D();

            msg_L2D.requestSummonerId = msg_P2L.requestSummonerId;
            msg_L2D.summonerId = msg_P2L.summonerId;
            SendDBMsg(msg_L2D);
        }
        public void OnReplyPlayerHeadImgUrl(int peerId, bool inbound, object msg)
        {
            ReplyPlayerHeadImgUrl_D2L replyMsg_D2L = msg as ReplyPlayerHeadImgUrl_D2L;
            ReplyPlayerHeadImgUrl_L2P replyMsg_L2P = new ReplyPlayerHeadImgUrl_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_D2L.requestSummonerId);
            if (sender == null) return;

            replyMsg_L2P.requestSummonerId = replyMsg_D2L.requestSummonerId;
            replyMsg_L2P.result = replyMsg_D2L.result;
            replyMsg_L2P.summonerId = replyMsg_D2L.summonerId;
            replyMsg_L2P.headImgUrl = replyMsg_D2L.headImgUrl;
            SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
        }
        public void OnReqAnnouncement(int peerId, bool inbound, object msg)
        {
            RequestAnnouncement_P2L reqMsg = msg as RequestAnnouncement_P2L;
            ReplyAnnouncement_L2P replyMsg = new ReplyAnnouncement_L2P();

            replyMsg.requestSummonerId = reqMsg.requestSummonerId;
            replyMsg.announcement = main.announcement;
            SendMsg(peerId, replyMsg);
        }
        public void OnReqGetDailySharing(int peerId, bool inbound, object msg)
        {
            RequestGetDailySharing_P2L reqMsg = msg as RequestGetDailySharing_P2L;
            ReplyGetDailySharing_L2P replyMsg = new ReplyGetDailySharing_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (main.dailySharingRewardHouseCard <= 0)
            {
                //每日分享奖励有误
                ServerUtil.RecordLog(LogType.Debug, "OnReqGetDailySharing, 每日分享奖励有误! userId = " + sender.userId);
                replyMsg.result = ResultCode.GetServerConfigError;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            DateTime nowTime = DateTime.Now;
            if (sender.dailySharingTime == null || (sender.dailySharingTime.Date == nowTime.Date))
            {
                //已经领取每日分享奖励
                ServerUtil.RecordLog(LogType.Debug, "OnReqGetDailySharing, 已经领取每日分享奖励! userId = " + sender.userId);
                replyMsg.result = ResultCode.PlayerGetDailySharing;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            int sumAmount = sender.roomCard + main.dailySharingRewardHouseCard;
            if (sumAmount <= sender.roomCard && sumAmount < main.dailySharingRewardHouseCard)
            {
                //加爆了
                ServerUtil.RecordLog(LogType.Debug, "OnReqGetDailySharing error!! roomCard = " + sender.roomCard + ", dailySharingRewardHouseCard = " + main.dailySharingRewardHouseCard);
                sumAmount = int.MaxValue;
            }
            sender.dailySharingTime = nowTime;
            sender.roomCard = sumAmount;

            replyMsg.result = ResultCode.OK;
            replyMsg.houseCard = sender.roomCard;
            SendProxyMsg(replyMsg, sender.proxyServerId);

            //保存DB
            OnRequestSaveDailySharing(sender.userId, sender.dailySharingTime.ToString());
        }

        public void OnRequestSaveDailySharing(string userId, string dailySharingTime)
        {
            RequestSaveDailySharing_L2D reqMsg_L2D = new RequestSaveDailySharing_L2D();
            reqMsg_L2D.userId = userId;
            reqMsg_L2D.addHouseCard = main.dailySharingRewardHouseCard;
            reqMsg_L2D.dailySharingTime = dailySharingTime;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveHouseId(string userId, ulong houseId)
        {
            RequestSaveHouseId_L2D reqMsg_L2D = new RequestSaveHouseId_L2D();
            reqMsg_L2D.userId = userId;
            reqMsg_L2D.houseId = houseId;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveAllIntegral(string userId, int addIntegral)
        {
            RequestSavePlayerAllIntegral_L2D reqMsg_L2D = new RequestSavePlayerAllIntegral_L2D();
            reqMsg_L2D.userId = userId;
            reqMsg_L2D.addIntegral = addIntegral;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRecvPlayerHouseCard(ulong summonerId, int proxyServerId, int houseCard)
        {
            RecvPlayerHouseCard_L2P reqMsg_L2P = new RecvPlayerHouseCard_L2P();
            reqMsg_L2P.summonerId = summonerId;
            reqMsg_L2P.houseCard = houseCard;
            SendProxyMsg(reqMsg_L2P, proxyServerId);
        }
        public void OnRequestSaveHouseCard(ulong guid, OperationType type, int houseCard)
        {
            RequestSaveHouseCard_L2D reqMsg_L2D = new RequestSaveHouseCard_L2D();
            reqMsg_L2D.guid = guid;
            reqMsg_L2D.houseCard = houseCard;
            reqMsg_L2D.type = type;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestSaveRecordBusiness(string userId, int businessId, string lastTime = "")
        {
            RequestSaveRecordBusiness_L2D reqMsg_L2D = new RequestSaveRecordBusiness_L2D();
            reqMsg_L2D.userId = userId;
            reqMsg_L2D.businessId = businessId;
            reqMsg_L2D.lastTime = lastTime;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnReqSendChat(int peerId, bool inbound, object msg)
        {
            RequestSendChat_P2L reqMsg = msg as RequestSendChat_P2L;
            ReplySendChat_L2P replyMsg = new ReplySendChat_L2P();

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            if (string.IsNullOrEmpty(reqMsg.chatText)) return;
            if ((DateTime.Now - sender.lastSendChatMsgTime).TotalMilliseconds < (main.sendMsgCD * 1000)) return;
            if (Encoding.Default.GetByteCount(reqMsg.chatText) > main.chatMsgSizeLimit) return;

            replyMsg.summonerId = reqMsg.summonerId;

            if (sender.houseId == 0)
            {
                //没有房间号，不需要聊天
                ServerUtil.RecordLog(LogType.Debug, "OnReqSendChat, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHaveNotTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqSendChat, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (house.houseType == HouseType.RunFastHouse)
            {
#if RUNFAST
                if (house.GetRunFastHouseStatus() >= RunFastHouseStatus.RFHS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqSendChat, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                    replyMsg.result = ResultCode.TheHouseNonexistence;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
#endif
            }
            else if (house.houseType == HouseType.MahjongHouse)
            {
#if MAHJONG
                if (house.GetMahjongHouseStatus() >= MahjongHouseStatus.MHS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqSendChat, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                    replyMsg.result = ResultCode.TheHouseNonexistence;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
#endif
            }
            else if (house.houseType == HouseType.WordPlateHouse)
            {
#if WORDPLATE
                if (house.GetWordPlateHouseStatus() >= WordPlateHouseStatus.EWPS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqSendChat, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                    replyMsg.result = ResultCode.TheHouseNonexistence;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
#endif
            }
            else
            {
                ServerUtil.RecordLog(LogType.Debug, "OnRecvPlayerLineType, 玩家房间类型有误! userId = " + sender.userId + ", house = " + house.houseType);
                return;
            }

            Player player = house.GetHousePlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqSendChat, 玩家不存在! userId = " + player.userId + ", houseId = " + house.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }

            sender.lastSendChatMsgTime = DateTime.Now;

            foreach (Player otherPlayer in house.GetOtherHousePlayer(player.userId))
            {
                OnRecvSendChat(otherPlayer.summonerId, otherPlayer.proxyServerId, player.index, reqMsg.chatType, reqMsg.chatText, reqMsg.chatTime);
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.chatType = reqMsg.chatType;
            replyMsg.chatText = reqMsg.chatText;
            replyMsg.chatTime = reqMsg.chatTime;
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void OnRecvSendChat(ulong summonerId, int proxyServerId, int sendIndex, ChatType chatType, string chatText, float chatTime)
        {
            RecvSendChat_L2P recvMsg = new RecvSendChat_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.sendIndex = sendIndex;
            recvMsg.chatType = chatType;
            recvMsg.chatText = chatText;
            recvMsg.chatTime = chatTime;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnReqPlayerLineType(int peerId, bool inbound, object msg)
        {
            RequestPlayerLineType_P2L reqMsg = msg as RequestPlayerLineType_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.summonerId);
            if (sender == null) return;

            OnRecvPlayerLineType(sender.houseId, sender.userId, reqMsg.lineType);
        }
        public void OnRecvPlayerLineType(ulong summonerId, int proxyServerId, int index, LineType lineType)
        {
            RecvPlayerLineType_L2P recvMsg = new RecvPlayerLineType_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.index = index;
            recvMsg.lineType = lineType;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnRecvPlayerLineType(ulong houseId, string userId, LineType lineType)
        {
            if (houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnRecvPlayerLineType, 没有房间号! userId = " + userId + ", houseId = " + houseId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(houseId);
            if (house == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnRecvPlayerLineType, 房间不存在! userId = " + userId + ", houseId = " + houseId);
                return;
            }
            if (house.houseType == HouseType.RunFastHouse)
            {
#if RUNFAST
                if (house.GetRunFastHouseStatus() >= RunFastHouseStatus.RFHS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnRecvPlayerLineType, 房间不存在! userId = " + userId + ", houseId = " + houseId);
                    return;
                }
#endif
            }
            else if (house.houseType == HouseType.MahjongHouse)
            {
#if MAHJONG
                if (house.GetMahjongHouseStatus() >= MahjongHouseStatus.MHS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnRecvPlayerLineType, 房间不存在! userId = " + userId + ", houseId = " + houseId);
                    return;
                }
#endif
            }
            else if (house.houseType == HouseType.WordPlateHouse)
            {
#if WORDPLATE
                if (house.GetWordPlateHouseStatus() >= WordPlateHouseStatus.EWPS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnRecvPlayerLineType, 房间不存在! userId = " + userId + ", houseId = " + houseId);
                    return;
                }
#endif
            }
            else
            {
                ServerUtil.RecordLog(LogType.Debug, "OnRecvPlayerLineType, 玩家房间类型有误! userId = " + userId + ", house = " + house.houseType);
                return;
            }
            Player player = house.GetHousePlayer(userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnRecvPlayerLineType, 玩家不存在! userId = " + userId + ", houseId = " + houseId);
                return;
            }
            if (player.lineType == lineType)
            {
                //玩家状态相同
                return;
            }
            player.lineType = lineType;

            foreach (Player housePlayer in house.GetOtherHousePlayer(userId))
            {
                OnRecvPlayerLineType(housePlayer.summonerId, housePlayer.proxyServerId, player.index, lineType);
            }
        }
        public void OnReqSavePlayerAddress(int peerId, bool inbound, object msg)
        {
            RequestSavePlayerAddress_P2L reqMsg = msg as RequestSavePlayerAddress_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.requestSummonerId);
            if (sender == null) return;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqSavePlayerAddress, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqSavePlayerAddress, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                return;
            }
            if (house.houseType == HouseType.RunFastHouse)
            {
#if RUNFAST
                if (house.GetRunFastHouseStatus() >= RunFastHouseStatus.RFHS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqSavePlayerAddress, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                    return;
                }
#endif
            }
            else if (house.houseType == HouseType.MahjongHouse)
            {
#if MAHJONG
                if (house.GetMahjongHouseStatus() >= MahjongHouseStatus.MHS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqSavePlayerAddress, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                    return;
                }
#endif
            }
            else if (house.houseType == HouseType.WordPlateHouse)
            {
#if WORDPLATE
                if (house.GetWordPlateHouseStatus() >= WordPlateHouseStatus.EWPS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqSavePlayerAddress, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                    return;
                }
#endif
            }
            else
            {
                ServerUtil.RecordLog(LogType.Debug, "OnReqSavePlayerAddress, 玩家房间类型有误! userId = " + sender.userId + ", house = " + house.houseType);
                return;
            }
            Player player = house.GetHousePlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqSavePlayerAddress, 玩家不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                return;
            }
            if (player.longitude != reqMsg.longitude)
            {
                player.longitude = reqMsg.longitude;
            }
            if (player.latitude != reqMsg.latitude)
            {
                player.latitude = reqMsg.latitude;
            }
        }
        public void OnReqOtherPlayerAddress(int peerId, bool inbound, object msg)
        {
            RequestOtherPlayerAddress_P2L reqMsg = msg as RequestOtherPlayerAddress_P2L;
            ReplyOtherPlayerAddress_L2P replyMsg = new ReplyOtherPlayerAddress_L2P();

            replyMsg.requestSummonerId = reqMsg.requestSummonerId;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.requestSummonerId);
            if (sender == null) return;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqOtherPlayerAddress, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqOtherPlayerAddress, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            if (house.houseType == HouseType.RunFastHouse)
            {
#if RUNFAST
                if (house.GetRunFastHouseStatus() >= RunFastHouseStatus.RFHS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOtherPlayerAddress, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                    replyMsg.result = ResultCode.TheHouseNonexistence;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
#endif
            }
            else if (house.houseType == HouseType.MahjongHouse)
            {
#if MAHJONG
                if (house.GetMahjongHouseStatus() >= MahjongHouseStatus.MHS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOtherPlayerAddress, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                    replyMsg.result = ResultCode.TheHouseNonexistence;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
#endif
            }
            else if (house.houseType == HouseType.WordPlateHouse)
            {
#if WORDPLATE
                if (house.GetWordPlateHouseStatus() >= WordPlateHouseStatus.EWPS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqOtherPlayerAddress, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                    replyMsg.result = ResultCode.TheHouseNonexistence;
                    SendProxyMsg(replyMsg, sender.proxyServerId);
                    return;
                }
#endif
            }
            else
            {
                ServerUtil.RecordLog(LogType.Debug, "OnReqOtherPlayerAddress, 玩家房间类型有误! userId = " + sender.userId + ", house = " + house.houseType);
                replyMsg.result = ResultCode.TheHouseNonexistence;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }
            Player player = house.GetHousePlayerBySummonerId(reqMsg.playerId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqOtherPlayerAddress, 玩家不存在! summonerId = " + reqMsg.playerId + ", houseId = " + sender.houseId);
                replyMsg.result = ResultCode.PlayerHasNotBeenInTheHouse;
                SendProxyMsg(replyMsg, sender.proxyServerId);
                return;
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.playerIndex = player.index;
            replyMsg.longitude = player.longitude;
            replyMsg.latitude = player.latitude;
            SendProxyMsg(replyMsg, sender.proxyServerId);
        }
        public void OnRequestDelZombieHouse(int peerId, bool inbound, object msg)
        {
            RequestDelZombieHouse_D2L reqMsg = msg as RequestDelZombieHouse_D2L;
            reqMsg.delHouseIdList.ForEach(houseId =>
            {
                HouseManager.Instance.RemoveHouse(houseId);
            });
        }
        public void OnReqPlayerOperateHosted(int peerId, bool inbound, object msg)
        {
            RequestPlayerOperateHosted_P2L reqMsg = msg as RequestPlayerOperateHosted_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg.requestSummonerId);
            if (sender == null) return;

            if (sender.houseId == 0)
            {
                //没有房间号
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerOperateHosted, 没有房间号! userId = " + sender.userId + ", houseId = " + sender.houseId);
                return;
            }
            if (reqMsg.bHosted)
            {
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerOperateHosted, 暂不支持主动托管! userId = " + sender.userId + ", houseId = " + sender.houseId);
                return;
            }
            House house = HouseManager.Instance.GetHouseById(sender.houseId);
            if (house == null)
            {
                //房间不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerOperateHosted, 房间不存在! userId = " + sender.userId + ", houseId = " + sender.houseId);
                return;
            }
            if (house.houseType == HouseType.RunFastHouse)
            {
#if RUNFAST
                if (house.GetRunFastHouseStatus() >= RunFastHouseStatus.RFHS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerOperateHosted, 房间不存在! userId = " + sender.userId + ", houseId = " + house.houseId);
                    return;
                }
#endif
            }
            else if (house.houseType == HouseType.MahjongHouse)
            {
#if MAHJONG
                if (house.GetMahjongHouseStatus() >= MahjongHouseStatus.MHS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerOperateHosted, 房间不存在! userId = " + sender.userId + ", houseId = " + house.houseId);
                    return;
                }
#endif
            }
            else if (house.houseType == HouseType.WordPlateHouse)
            {
#if WORDPLATE
                if (house.GetWordPlateHouseStatus() >= WordPlateHouseStatus.EWPS_Dissolved)
                {
                    //房间不存在
                    ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerOperateHosted, 房间不存在! userId = " + sender.userId + ", houseId = " + house.houseId);
                    return;
                }
#endif
            }
            else
            {
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerOperateHosted, 玩家房间类型有误! userId = " + sender.userId + ", houseType = " + house.houseType);
                return;
            }
            if (house.businessId <= 0)
            {
                ServerUtil.RecordLog(LogType.Debug, "OnReqPlayerOperateHosted, 玩家房间模式不能托管! userId = " + sender.userId + ", businessId = " + house.businessId);
                return;
            }
            Player player = house.GetHousePlayer(sender.userId);
            if (player == null)
            {
                //玩家不存在
                ServerUtil.RecordLog(LogType.Debug, "OnReqOtherPlayerAddress, 玩家不存在! summonerId = " + sender.id + ", houseId = " + house.houseId);
                return;
            }
            if (player.bHosted != reqMsg.bHosted)
            {
                player.bHosted = reqMsg.bHosted;
                OnRecvPlayerHostedStatus(player.summonerId, player.proxyServerId, reqMsg.bHosted);
            }
        }
        public void OnRecvPlayerHostedStatus(ulong summonerId, int proxyServerId, bool bHosted)
        {
            RecvPlayerHostedStatus_L2P recvMsg = new RecvPlayerHostedStatus_L2P();
            recvMsg.requestSummonerId = summonerId;
            recvMsg.bHosted = bHosted;
            SendProxyMsg(recvMsg, proxyServerId);
        }
        public void OnRequestSaveCompetitionKey(ulong summonerId, int competitionKey)
        {
            RequestSaveCompetitionKey_L2D reqMsg_L2D = new RequestSaveCompetitionKey_L2D();
            reqMsg_L2D.requestSummonerId = summonerId;
            reqMsg_L2D.competitionKey = competitionKey;
            SendDBMsg(reqMsg_L2D);
        }
        public void OnRequestInitHouseIdAndComKey(ulong summonerId)
        {
            RequestInitHouseIdAndComKey_L2D reqMsg_L2D = new RequestInitHouseIdAndComKey_L2D();
            reqMsg_L2D.requestSummonerId = summonerId;
            SendDBMsg(reqMsg_L2D);
        }

        public void OnReplyHouseBelong(int peerId, bool inbound, object msg)
        {
            ReplyHouseBelong_W2L replyMsg_W2L = msg as ReplyHouseBelong_W2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(replyMsg_W2L.summonerId);
            if (sender == null) return;

            if (replyMsg_W2L.logicId <= 0)
            {
#if MAHJONG
                ReplyJoinMahjongHouse_L2P replyMsg_L2P = new ReplyJoinMahjongHouse_L2P();
                replyMsg_L2P.result = ResultCode.TheHouseNonexistence;
                replyMsg_L2P.summonerId = replyMsg_W2L.summonerId;
                replyMsg_L2P.houseId = replyMsg_W2L.houseId;
                SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
#elif RUNFAST
                ReplyJoinRunFastHouse_L2P replyMsg_L2P = new ReplyJoinRunFastHouse_L2P();
                replyMsg_L2P.result = ResultCode.TheHouseNonexistence;
                replyMsg_L2P.summonerId = replyMsg_W2L.summonerId;
                replyMsg_L2P.houseId = replyMsg_W2L.houseId;
                SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
#elif WORDPLATE
                ReplyJoinWordPlateHouse_L2P replyMsg_L2P = new ReplyJoinWordPlateHouse_L2P();
                replyMsg_L2P.result = ResultCode.TheHouseNonexistence;
                replyMsg_L2P.summonerId = replyMsg_W2L.summonerId;
                replyMsg_L2P.houseId = replyMsg_W2L.houseId;
                SendProxyMsg(replyMsg_L2P, sender.proxyServerId);
#endif
                //房间在所有逻辑服务器都找不到
                ServerUtil.RecordLog(LogType.Debug, "OnReplyHouseBelong, 房间不存在! userId = " + sender.userId + ", houseCardId = " + replyMsg_W2L.houseId);
            }
            else
            {
                //在别的逻辑服务器找到了房间则将自己的信息打包通过自己的网关转发给房间对应的逻辑服务器
                //然后从当前旧逻辑服退出，在目标逻辑服务器继续加入房间的操作
                TransmitPlayerInfo_L2P transmitMsg = new TransmitPlayerInfo_L2P();
                transmitMsg.summoner = ServerUtil.Serialize(sender);
                transmitMsg.summonerId = sender.id;
                transmitMsg.type = TransmitPlayerType.ETP_JoinHouse;
                transmitMsg.parameter = Serializer.trySerializerObject(replyMsg_W2L.houseId);
                transmitMsg.targetLogic = replyMsg_W2L.logicId;
                SendProxyMsg(transmitMsg, sender.proxyServerId);

                SummonerManager.Instance.RemoveSummoner(sender.id);
            }
        }
        public void OnTransmitPlayerInfo(int peerId, bool inbound, object msg)
        {
            TransmitPlayerInfo_P2L transmitMsg = msg as TransmitPlayerInfo_P2L;

            if (transmitMsg.summoner == null || transmitMsg.parameter == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnTransmitPlayerInfo ： 逻辑服务器收到另一个逻辑服务器的打包Summoner信息有误！");
                return;
            }
            //收到网关转发过来的另一个逻辑服务器的打包好的玩家信息后先加入到当前的玩家管理器中
            Summoner sender = (Summoner)ServerUtil.UnSerialize(transmitMsg.summoner, typeof(Summoner));
            if (sender == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnTransmitPlayerInfo ： 逻辑服务器收到另一个逻辑服务器的打包Summoner信息有误！");
                return;
            }
            Summoner summoner = SummonerManager.Instance.GetSummonerById(sender.id);
            if (summoner != null)
            {
                sender = summoner;
                ServerUtil.RecordLog(LogType.Error, "OnTransmitPlayerInfo ： 逻辑服务器收到另一个逻辑服务器的打包Summoner信息时发现该逻辑服务器有该玩家，逻辑继续，但一定要调试原因！");
            }
            else
            {
                SummonerManager.Instance.AddSummoner(sender.id, sender);
                //更新ccu
                ModuleManager.Get<DistributedMain>().UpdateLoadBlanceStatus(true);
            }
            if (transmitMsg.type == TransmitPlayerType.ETP_JoinHouse)
            {
                int houseId = Serializer.tryUnSerializerObject<int>(transmitMsg.parameter);
#if MAHJONG
                MahjongHouse house = (MahjongHouse)HouseManager.Instance.GetHouseById(houseId);

                //接下来进行做在另一个逻辑服务器没能做完的加入房间操作
                ModuleManager.Get<MahjongMain>().TryJoinMahjongHouse(sender, house);
#elif RUNFAST
                RunFastHouse house = (RunFastHouse)HouseManager.Instance.GetHouseById(houseId);

                //接下来进行做在另一个逻辑服务器没能做完的加入房间操作
                ModuleManager.Get<RunFastMain>().TryJoinRunFastHouse(sender, house);
#elif WORDPLATE
                WordPlateHouse house = (WordPlateHouse)HouseManager.Instance.GetHouseById(houseId);

                //接下来进行做在另一个逻辑服务器没能做完的加入房间操作
                ModuleManager.Get<WordPlateMain>().TryJoinWordPlateHouse(sender, house);
#endif
            }
            else if (transmitMsg.type == TransmitPlayerType.ETP_CreateCompetition)
            {
                CreateCompetitionNode comNode = Serializer.tryUnSerializerObject<CreateCompetitionNode>(transmitMsg.parameter);
                if (comNode != null)
                {
                    ModuleManager.Get<SpecialActivitiesMain>().msg_proxy.CreateMarketCompetition(sender, comNode);
                }
            }
            else if (transmitMsg.type == TransmitPlayerType.ETP_JoinMarket)
            {
                JoinMarketNode joinMarketNode = Serializer.tryUnSerializerObject<JoinMarketNode>(transmitMsg.parameter);
                if (joinMarketNode != null)
                {
                    ModuleManager.Get<SpecialActivitiesMain>().msg_proxy.JoinMarket(sender, joinMarketNode);
                }
            }
            else if (transmitMsg.type == TransmitPlayerType.ETP_MarketCompetition)
            {
                int marketId = Serializer.tryUnSerializerObject<int>(transmitMsg.parameter);
                Market market = CompetitionManager.Instance.GetMarket(marketId);
                if (market != null)
                {
                    ModuleManager.Get<SpecialActivitiesMain>().msg_proxy.MarketCompetitionInfo(sender, market);
                }
            }
        }
        public void OnNotifyDBClearHouse(int logicId, int DBServer)
        {
            NotifyDBClearHouse_L2D notifyMsg = new NotifyDBClearHouse_L2D();
            notifyMsg.logicId = logicId;
            SendDBMsg(notifyMsg, DBServer);
        }
        public void OnNotifyUpdateAnnouncement(int peerId, bool inbound, object msg)
        {
            NotifyUpdateAnnouncement_D2L notifyMsg = msg as NotifyUpdateAnnouncement_D2L;

            if (string.IsNullOrEmpty(notifyMsg.announcement) || main.announcement == notifyMsg.announcement)
            {
                //空公告或者跟以前的公告相等，不用发
                return;
            }
            //保存公告
            main.announcement = notifyMsg.announcement;
            //群发公告
            foreach (KeyValuePair<ulong, Summoner> element in SummonerManager.Instance.GetSummonerCollection())
            {
                if (element.Value == null)
                {
                    continue;
                }
                OnRecvUpdateAnnouncement(element.Value.id, element.Value.proxyServerId, notifyMsg.announcement);
            }
        }
        public void OnRecvUpdateAnnouncement(ulong summonerId, int proxyServerId, string announcement)
        {
            RecvUpdateAnnouncement_L2P recvMsg_L2P = new RecvUpdateAnnouncement_L2P();
            recvMsg_L2P.summonerId = summonerId;
            recvMsg_L2P.announcement = announcement;
            SendProxyMsg(recvMsg_L2P, proxyServerId);
        }
        public void OnNotifyGameServerSendGoods(int peerId, bool inbound, object msg)
        {
            NotifyGameServerSendGoods_X2X notifyMsg = msg as NotifyGameServerSendGoods_X2X;

            Summoner summoner = SummonerManager.Instance.GetSummonerById(notifyMsg.summonerId);
            if (summoner != null)
            {
                summoner.roomCard += notifyMsg.addRoomCardNum;
                OnRecvPlayerHouseCard(summoner.id, summoner.proxyServerId, summoner.roomCard);
            }
        }
    }
}

