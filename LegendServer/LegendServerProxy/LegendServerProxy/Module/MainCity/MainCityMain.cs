using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServerProxy.Core;
using LegendServerProxy.Distributed;
using System;

namespace LegendServerProxy.MainCity
{
    public class MainCityMain : Module
    {
        public MainCityMsgProxy msg_proxy;
        public int feedbackPhoneNumberSizeLimit;
        public int feedbackTextSizeLimit;
        public int sendFeedbackMsgCD;
        public int sendMsgCD;
        public int chatMsgSizeLimit;
        public MainCityMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new MainCityMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
            this.feedbackPhoneNumberSizeLimit = 20;
            ServerConfigDB serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "feedbackPhoneNumberSizeLimit");
            if (serverConfigDB != null)
            {
                int.TryParse(serverConfigDB.value, out this.feedbackPhoneNumberSizeLimit);
            }

            this.feedbackTextSizeLimit = 200;
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "feedbackTextSizeLimit");
            if (serverConfigDB != null)
            {
                int.TryParse(serverConfigDB.value, out this.feedbackTextSizeLimit);
            }

            this.sendFeedbackMsgCD = 300;
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "sendFeedbackMsgCD");
            if (serverConfigDB != null)
            {
                int.TryParse(serverConfigDB.value, out this.sendFeedbackMsgCD);
            }

            //聊天CD
            sendMsgCD = 3;
            ServerConfigDB serverConfig = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "sendMsgCD");
            if (serverConfig != null)
            {
                int.TryParse(serverConfig.value, out sendMsgCD);
            }
            //聊天长度
            chatMsgSizeLimit = 255;
            serverConfig = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "msgSizeLimit");
            if (serverConfig != null)
            {
                int.TryParse(serverConfig.value, out chatMsgSizeLimit);
            }
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.T2P_RequestOnline, new MsgComponent(msg_proxy.OnReqOnline, typeof(RequestOnline_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyOnline, new MsgComponent(msg_proxy.OnReplyOnline, typeof(ReplyOnline_L2P)));
            MsgFactory.Regist(MsgID.T2P_SendFeedback, new MsgComponent(msg_proxy.OnSendFeedback, typeof(SendFeedback_T2P)));
            MsgFactory.Regist(MsgID.L2P_RecvPlayerHouseCard, new MsgComponent(msg_proxy.OnRecvPlayerHouseCard, typeof(RecvPlayerHouseCard_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestGetDailySharing, new MsgComponent(msg_proxy.OnReqGetDailySharing, typeof(RequestGetDailySharing_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyGetDailySharing, new MsgComponent(msg_proxy.OnReplyGetDailySharing, typeof(ReplyGetDailySharing_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestSendChat, new MsgComponent(msg_proxy.OnReqSendChat, typeof(RequestSendChat_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplySendChat, new MsgComponent(msg_proxy.OnReplySendChat, typeof(ReplySendChat_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvSendChat, new MsgComponent(msg_proxy.OnRecvSendChat, typeof(RecvSendChat_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestPlayerLineType, new MsgComponent(msg_proxy.OnReqPlayerLineType, typeof(RequestPlayerLineType_T2P)));
            MsgFactory.Regist(MsgID.L2P_RecvPlayerLineType, new MsgComponent(msg_proxy.OnRecvPlayerLineType, typeof(RecvPlayerLineType_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestBindBelong, new MsgComponent(msg_proxy.OnReqBindBelong, typeof(RequestBindBelong_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyBindBelong, new MsgComponent(msg_proxy.OnReplyBindBelong, typeof(ReplyBindBelong_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestPlayerHeadImgUrl, new MsgComponent(msg_proxy.OnReqPlayerHeadImgUrl, typeof(RequestPlayerHeadImgUrl_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyPlayerHeadImgUrl, new MsgComponent(msg_proxy.OnReplyPlayerHeadImgUrl, typeof(ReplyPlayerHeadImgUrl_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestAnnouncement, new MsgComponent(msg_proxy.OnReqAnnouncement, typeof(RequestAnnouncement_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyAnnouncement, new MsgComponent(msg_proxy.OnReplyAnnouncement, typeof(ReplyAnnouncement_L2P)));
            MsgFactory.Regist(MsgID.L2P_RecvUpdateAnnouncement, new MsgComponent(msg_proxy.OnRecvUpdateAnnouncement, typeof(RecvUpdateAnnouncement_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestSavePlayerAddress, new MsgComponent(msg_proxy.OnReqSavePlayerAddress, typeof(RequestSavePlayerAddress_T2P)));
            MsgFactory.Regist(MsgID.T2P_RequestOtherPlayerAddress, new MsgComponent(msg_proxy.OnReqOtherPlayerAddress, typeof(RequestOtherPlayerAddress_T2P)));
            MsgFactory.Regist(MsgID.L2P_ReplyOtherPlayerAddress, new MsgComponent(msg_proxy.OnReplyOtherPlayerAddress, typeof(ReplyOtherPlayerAddress_L2P)));
            MsgFactory.Regist(MsgID.T2P_RequestPlayerOperateHosted, new MsgComponent(msg_proxy.OnReqPlayerOperateHosted, typeof(RequestPlayerOperateHosted_T2P)));
            MsgFactory.Regist(MsgID.L2P_RecvPlayerHostedStatus, new MsgComponent(msg_proxy.OnRecvPlayerHostedStatus, typeof(RecvPlayerHostedStatus_L2P)));
            MsgFactory.Regist(MsgID.W2P_KickOut, new MsgComponent(msg_proxy.OnKickOut, typeof(KickLogout_W2P)));
            MsgFactory.Regist(MsgID.L2P_TransmitPlayerInfo, new MsgComponent(msg_proxy.OnTransmitPlayerInfo, typeof(TransmitPlayerInfo_L2P)));
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
        public void KickOutInvalidClient(string userId)
        {
            msg_proxy.KickOutInvalidClient(userId);
        }
        public bool IsOnline(string userId)
        {
            InboundClientSession session = SessionManager.Instance.GetInboundClientSessionByUserId(userId);
            return (session != null);
        }
        public void LogicServerPlayerLogout(int logicServerId, string userId, bool byPlaceOtherLogin)
        {
            msg_proxy.NotifyLogicServerPlayerLogout(logicServerId, userId, byPlaceOtherLogin);
        }
    }
}