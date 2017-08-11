using System;
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServerLogic.Actor.Summoner;
using LegendServerLogicDefine;
using System.Collections.Generic;
using LegendServerLogic.Record;

namespace LegendServerLogic.MainCity
{
    public class MainCityMain : Module
    {
        public MainCityMsgProxy msg_proxy;
        public int feedbackPhoneNumberSizeLimit;
        public int feedbackTextSizeLimit;
        public int sendFeedbackMsgCD;
        public string announcement = "";
        public int dailySharingRewardHouseCard;
        public int sendMsgCD;
        public int chatMsgSizeLimit;
        private bool bOpenDelHouseCard;
        private bool bOpenCreateHouse;
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
            //开服初始公告
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "InitAnnouncement");
            if (serverConfigDB != null && !string.IsNullOrEmpty(serverConfigDB.value) && string.IsNullOrEmpty(announcement))
            {
                announcement = serverConfigDB.value;
            }
            //每日分享奖励房卡数(张)
            dailySharingRewardHouseCard = 3;
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "DailySharingRewardHouseCard");
            if (serverConfigDB != null && !string.IsNullOrEmpty(serverConfigDB.value))
            {
                int.TryParse(serverConfigDB.value, out dailySharingRewardHouseCard);
            }
            //聊天CD
            sendMsgCD = 3;
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "SendMsgCD");
            if (serverConfigDB != null)
            {
                int.TryParse(serverConfigDB.value, out sendMsgCD);
            }
            //聊天长度
            chatMsgSizeLimit = 255;
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MsgSizeLimit");
            if (serverConfigDB != null)
            {
                int.TryParse(serverConfigDB.value, out chatMsgSizeLimit);
            }
            //是否开启扣卡模式
            bOpenDelHouseCard = true;
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "IsOpenDelHouseCard");
            if (serverConfigDB != null && !string.IsNullOrEmpty(serverConfigDB.value))
            {
                bool.TryParse(serverConfigDB.value, out bOpenDelHouseCard);
            }
            //是否开启创建房间
            bOpenCreateHouse = true;
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "IsOpenCreateHouse");
            if (serverConfigDB != null && !string.IsNullOrEmpty(serverConfigDB.value))
            {
                bool.TryParse(serverConfigDB.value, out bOpenCreateHouse);
            }
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.D2L_RequestDelZombieHouse, new MsgComponent(msg_proxy.OnRequestDelZombieHouse, typeof(RequestDelZombieHouse_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestOnline, new MsgComponent(msg_proxy.OnReqOnline, typeof(RequestOnline_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplySummonerData, new MsgComponent(msg_proxy.OnReplySummonerData, typeof(ReplySummonerData_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestLogout, new MsgComponent(msg_proxy.OnReqLogout, typeof(RequestLogout_P2L)));
            MsgFactory.Regist(MsgID.P2L_SendFeedback, new MsgComponent(msg_proxy.OnSendFeedback, typeof(SendFeedback_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestGetDailySharing, new MsgComponent(msg_proxy.OnReqGetDailySharing, typeof(RequestGetDailySharing_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestSendChat, new MsgComponent(msg_proxy.OnReqSendChat, typeof(RequestSendChat_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestPlayerLineType, new MsgComponent(msg_proxy.OnReqPlayerLineType, typeof(RequestPlayerLineType_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestBindBelong, new MsgComponent(msg_proxy.OnReqBindBelong, typeof(RequestBindBelong_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyBindBelong, new MsgComponent(msg_proxy.OnReplyBindBelong, typeof(ReplyBindBelong_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestPlayerHeadImgUrl, new MsgComponent(msg_proxy.OnReqPlayerHeadImgUrl, typeof(RequestPlayerHeadImgUrl_P2L)));
            MsgFactory.Regist(MsgID.D2L_ReplyPlayerHeadImgUrl, new MsgComponent(msg_proxy.OnReplyPlayerHeadImgUrl, typeof(ReplyPlayerHeadImgUrl_D2L)));
            MsgFactory.Regist(MsgID.P2L_RequestAnnouncement, new MsgComponent(msg_proxy.OnReqAnnouncement, typeof(RequestAnnouncement_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestSavePlayerAddress, new MsgComponent(msg_proxy.OnReqSavePlayerAddress, typeof(RequestSavePlayerAddress_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestOtherPlayerAddress, new MsgComponent(msg_proxy.OnReqOtherPlayerAddress, typeof(RequestOtherPlayerAddress_P2L)));
            MsgFactory.Regist(MsgID.P2L_RequestPlayerOperateHosted, new MsgComponent(msg_proxy.OnReqPlayerOperateHosted, typeof(RequestPlayerOperateHosted_P2L)));
            MsgFactory.Regist(MsgID.W2L_ReplyHouseBelong, new MsgComponent(msg_proxy.OnReplyHouseBelong, typeof(ReplyHouseBelong_W2L)));
            MsgFactory.Regist(MsgID.P2L_TransmitPlayerInfo, new MsgComponent(msg_proxy.OnTransmitPlayerInfo, typeof(TransmitPlayerInfo_P2L)));
            MsgFactory.Regist(MsgID.D2L_NotifyUpdateAnnouncement, new MsgComponent(msg_proxy.OnNotifyUpdateAnnouncement, typeof(NotifyUpdateAnnouncement_D2L)));
            MsgFactory.Regist(MsgID.X2X_NotifyGameServerSendGoods, new MsgComponent(msg_proxy.OnNotifyGameServerSendGoods, typeof(NotifyGameServerSendGoods_X2X)));
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
        public void OnRecvPlayerLineType(ulong summonerId, int proxyServerId, int index, LineType lineType)
        {
            msg_proxy.OnRecvPlayerLineType(summonerId, proxyServerId, index, lineType);
        }
        public void OnRecvPlayerLineType(ulong houseId, string userId, LineType lineType)
        {
            msg_proxy.OnRecvPlayerLineType(houseId, userId, lineType);
        }
        public bool CheckOpenDelHouseCard()
        {
            return bOpenDelHouseCard;
        }
        public bool CheckOpenCreateHouse()
        {
            return bOpenCreateHouse;
        }
        public void RecordBusinessUsers(string userId, int businessId)
        {
            Summoner sender = SummonerManager.Instance.GetSummonerByUserId(userId);
            if (sender == null)
            {
                return;
            }
            DateTime nowTime = DateTime.Now;
            if (businessId == 0)
            {
                //正常开房模式
                if (!sender.bOpenHouse)
                {
                    //第一次打开房模式
                    sender.bOpenHouse = true;
                    //保存
                    OnRequestSaveRecordBusiness(userId, businessId);
                }
            }
            else
            {
                //商家口令模式
                RecordBusinessNode businessNode = sender.recordBusinessList.Find(element => element.businessId == businessId);
                if (businessNode == null)
                {
                    businessNode = new RecordBusinessNode();
                    businessNode.businessId = businessId;
                    businessNode.lastTime = nowTime.Date.ToShortDateString();
                    sender.recordBusinessList.Add(businessNode);
                }
                else
                {
                    if (businessNode.lastTime == nowTime.Date.ToShortDateString())
                    {
                        //今天已经统计过了
                        return;
                    }
                    businessNode.lastTime = nowTime.Date.ToShortDateString();
                }
                OnRequestSaveRecordBusiness(userId, businessId, businessNode.lastTime);
            }
        }
        public void OnRequestSaveRecordBusiness(string userId, int businessId, string lastTime = "")
        {
            msg_proxy.OnRequestSaveRecordBusiness(userId, businessId, lastTime);
        }
        public void OnRecvPlayerHostedStatus(ulong summonerId, int proxyServerId, bool bHosted)
        {
            msg_proxy.OnRecvPlayerHostedStatus(summonerId, proxyServerId, bHosted);
        }
        public void PlayerSaveCompetitionKey(ulong summonerId, int competitionKey, bool bOnlySaveDB = false)
        {
            if (!bOnlySaveDB)
            {
                Summoner sender = SummonerManager.Instance.GetSummonerById(summonerId);
                if (sender != null)
                {
                    sender.competitionKey = competitionKey;
                }
            }
            msg_proxy.OnRequestSaveCompetitionKey(summonerId, competitionKey);
        }
        public void PlayerInitHouseIdAndComKey(ulong summonerId)
        {
            msg_proxy.OnRequestInitHouseIdAndComKey(summonerId);
        }
        public void OnNotifyDBClearHouse(int logicId, int DBServer)
        {
            msg_proxy.OnNotifyDBClearHouse(logicId, DBServer);
        }
    }
}