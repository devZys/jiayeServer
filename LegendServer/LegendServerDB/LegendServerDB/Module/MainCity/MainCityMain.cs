using LegendProtocol;
using LegendServer.Database;
using System.Collections.Generic;
using LegendServerDB.Update;
using System;
using LegendServer.Database.Summoner;
using LegendServerDB.Core;
using LegendServer.Database.Config;
using System.Linq;
using LegendServer.Database.Record;
using LegendServerDB.Record;

namespace LegendServerDB.MainCity
{
    public class MainCityMain : Module
    {
        public MainCityMsgProxy msg_proxy;
        public int feedbackPhoneNumberSizeLimit;
        public int feedbackTextSizeLimit;
        public int bindCodeRewardRoomCard;
        public int maxSignInDays;
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
            //反馈手机限制
            this.feedbackPhoneNumberSizeLimit = 20;
            ServerConfigDB serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "feedbackPhoneNumberSizeLimit");
            if (serverConfigDB != null)
            {
                int.TryParse(serverConfigDB.value, out this.feedbackPhoneNumberSizeLimit);
            }
            //反馈内容限制
            this.feedbackTextSizeLimit = 200;
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "feedbackTextSizeLimit");
            if (serverConfigDB != null)
            {
                int.TryParse(serverConfigDB.value, out this.feedbackTextSizeLimit);
            }
            //最大签到天数
            this.maxSignInDays = 7;
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "MaxSignInDays");
            if (serverConfigDB != null && !string.IsNullOrEmpty(serverConfigDB.value))
            {
                int.TryParse(serverConfigDB.value, out this.maxSignInDays);
            }
            //绑定邀请码时的奖励房卡(张)
            this.bindCodeRewardRoomCard = 5;
            serverConfigDB = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "bindCodeRewardRoomCard");
            if (serverConfigDB != null && !string.IsNullOrEmpty(serverConfigDB.value))
            {
                int.TryParse(serverConfigDB.value, out bindCodeRewardRoomCard);
                if (this.bindCodeRewardRoomCard < 0)
                {
                    this.bindCodeRewardRoomCard = 0;
                }
            }
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.L2D_RequestSummonerData, new MsgComponent(msg_proxy.OnRequestSummonerData, typeof(RequestSummonerData_L2D)));
            MsgFactory.Regist(MsgID.L2D_SendFeedback, new MsgComponent(msg_proxy.OnSendFeedback, typeof(SendFeedback_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveHouseId, new MsgComponent(msg_proxy.OnRequestSaveHouseId, typeof(RequestSaveHouseId_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveHouseCard, new MsgComponent(msg_proxy.OnRequestSaveHouseCard, typeof(RequestSaveHouseCard_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSavePlayerAllIntegral, new MsgComponent(msg_proxy.OnRequestSavePlayerAllIntegral, typeof(RequestSavePlayerAllIntegral_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveDailySharing, new MsgComponent(msg_proxy.OnRequestSaveDailySharing, typeof(RequestSaveDailySharing_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveRecordBusiness, new MsgComponent(msg_proxy.OnRequestSaveRecordBusiness, typeof(RequestSaveRecordBusiness_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveTickets, new MsgComponent(msg_proxy.OnRequestSaveTickets, typeof(RequestSaveTickets_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestDelTickets, new MsgComponent(msg_proxy.OnRequestDelTickets, typeof(RequestDelTickets_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestUseTickets, new MsgComponent(msg_proxy.OnRequestUseTickets, typeof(RequestUseTickets_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestBindBelong, new MsgComponent(msg_proxy.OnRequestBindBelong, typeof(RequestBindBelong_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestPlayerHeadImgUrl, new MsgComponent(msg_proxy.OnRequestPlayerHeadImgUrl, typeof(RequestPlayerHeadImgUrl_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveCompetitionKey, new MsgComponent(msg_proxy.OnRequestSaveCompetitionKey, typeof(RequestSaveCompetitionKey_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestInitHouseIdAndComKey, new MsgComponent(msg_proxy.OnRequestInitHouseIdAndComKey, typeof(RequestInitHouseIdAndComKey_L2D)));
            MsgFactory.Regist(MsgID.L2D_NotifyDBClearHouse, new MsgComponent(msg_proxy.OnNotifyDBClearHouse, typeof(NotifyDBClearHouse_L2D)));
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
        public void RecordLoginUser(RecordLoginType recordType)
        {
            ModuleManager.Get<RecordMain>().msg_proxy.NotifyRecordLoginUser(recordType);
        }
        public void OnRecordRoomCard(ulong summonerId, RecordRoomCardType recordType, int roomCard)
        {
            ModuleManager.Get<RecordMain>().msg_proxy.NotifyRecordRoomCard(summonerId, recordType, roomCard);
        }
        public void RecordBusinessUsers(int businessId, string lastTime, int type)
        {
            ModuleManager.Get<RecordMain>().msg_proxy.NotifyRecordBusinessUser(businessId, lastTime, type);
        }
        public void RecordBusinessUsersByTickets(int ticketsId)
        {
            TicketsConfigDB ticketsConfig = DBManager<TicketsConfigDB>.Instance.GetSingleRecordInCache(config => config.ID == ticketsId);
            if (ticketsConfig == null || ticketsConfig.MarketID == 0)
            {
                return;
            }
            SpecialActivitiesConfigDB activitiesConfig = DBManager<SpecialActivitiesConfigDB>.Instance.GetSingleRecordInCache(config => config.ID == ticketsConfig.MarketID);
            if (activitiesConfig == null || string.IsNullOrEmpty(activitiesConfig.Tickets))
            {
                return;
            }
            string[] strTickets = activitiesConfig.Tickets.Split(';');
            for (int i = 0; i < strTickets.Length; ++i)
            {
                int cfTicketsId = 0;
                int.TryParse(strTickets[i], out cfTicketsId);
                if (cfTicketsId == ticketsId)
                {
                    RecordBusinessUsers(ticketsConfig.MarketID, DateTime.Now.Date.ToShortDateString(), 3 + i);
                }
            }
        }
        public void OnRequestDelZombieHouse(List<ulong> delZombieHouseId)
        {
            msg_proxy.OnRequestDelZombieHouse(delZombieHouseId);
        }
        public void NotifyUpdateAnnouncement(string announcement)
        {
            msg_proxy.OnNotifyUpdateAnnouncement(announcement);
        }
        public void OnNotifyGameServerSendGoods(ulong summonerId, int addRoomCardNum)
        {
            msg_proxy.OnNotifyGameServerSendGoods(summonerId, addRoomCardNum);
        }
    }

}