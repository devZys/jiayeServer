using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.Record;
using LegendServerRecord.Core;
using System;
using System.Collections.Generic;

namespace LegendServerRecord.Record
{
    public class RecordMsgProxy : ServerMsgProxy
    {
        private RecordMain main;

        public RecordMsgProxy(RecordMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnNotifyRecordLog(int peerId, bool inbound, object msg)
        {
            NotifyRecordLog_X2R notifyMsg = msg as NotifyRecordLog_X2R;

            if (!main.isOpenDebug && notifyMsg.logType == LogType.Debug) return;

            if (notifyMsg.isSaveToDB)
            {
                main.SaveLogToDB(notifyMsg.logType, notifyMsg.serverName, notifyMsg.serverID, notifyMsg.context);
            }
            else
            {
                ServerUtil.PrintLog(notifyMsg.logType, "【当前服务器名：" + notifyMsg.serverName + " 服务器ID：" + notifyMsg.serverID + "】===》" + " " + notifyMsg.context);
            }
        }
        public void OnNotifyRecordLoginUser(int peerId, bool inbound, object msg)
        {
            NotifyRecordLoginUser_X2R notifyMsg = msg as NotifyRecordLoginUser_X2R;

            DataOperate dataOperate = DataOperate.Update;
            DateTime nowTime = DateTime.Now;
            RecordLoginUserDB recordLoginUser = NHibernateHelper.DirectGetSingleRecordByCondition<RecordLoginUserDB>(element => (element.recordTime.Date == nowTime.Date));
            if (recordLoginUser == null)
            {
                recordLoginUser = new RecordLoginUserDB();
                recordLoginUser.recordTime = nowTime.Date;
                recordLoginUser.newUsers = 0;
                recordLoginUser.loginUsers = 0;
                dataOperate = DataOperate.Insert;
            }
            if ((RecordLoginType)notifyMsg.recordType == RecordLoginType.NewUser)
            {
                recordLoginUser.newUsers += 1;
                recordLoginUser.loginUsers += 1;
            }
            else if ((RecordLoginType)notifyMsg.recordType == RecordLoginType.LoginUser)
            {
                recordLoginUser.loginUsers += 1;
            }
            else
            {
                return;
            }
            bool result = NHibernateHelper.InsertOrUpdateOrDelete<RecordLoginUserDB>(recordLoginUser, dataOperate, true);
            if (!result)
            {
                //统计服务器登陆玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "统计服务器登陆玩家信息失败 Date = " + nowTime.Date + ", dataOperate = " + dataOperate);
            }
        }
        public void OnNotifyRecordRoomCard(int peerId, bool inbound, object msg)
        {
            NotifyRecordRoomCard_X2R notifyMsg = msg as NotifyRecordRoomCard_X2R;

            RecordRoomCardDB recordRoomCard = new RecordRoomCardDB();
            recordRoomCard.recordID = MyGuid.NewGuid(ServiceType.Record, (uint)root.ServerID);
            recordRoomCard.summonerId = notifyMsg.summonerId;
            recordRoomCard.recordRoomCardType = (RecordRoomCardType)notifyMsg.recordType;
            recordRoomCard.roomCard = notifyMsg.roomCard;
            recordRoomCard.time = DateTime.Now;
            bool result = NHibernateHelper.InsertOrUpdateOrDelete<RecordRoomCardDB>(recordRoomCard, DataOperate.Insert, true);
            if (!result)
            {
                //创建玩家房卡消耗记录时失败
                ServerUtil.RecordLog(LogType.Error, "创建玩家房卡消耗记录时失败 summonerId = " + notifyMsg.summonerId + ", roomCard = " + notifyMsg.roomCard);
            }
        }
        public void OnNotifyRecordBusinessUser(int peerId, bool inbound, object msg)
        {
            NotifyRecordBusinessUser_X2R notifyMsg = msg as NotifyRecordBusinessUser_X2R;

            DateTime dateTime = new DateTime();
            DateTime.TryParse(notifyMsg.lastTime, out dateTime);
            if (dateTime == new DateTime())
            {
                return;
            }
            DataOperate dataOperate = DataOperate.Update;
            RecordBusinessUserDB recordBusinessUser = NHibernateHelper.DirectGetSingleRecordByCondition<RecordBusinessUserDB>(element => (element.businessId == notifyMsg.businessId && element.recordTime.Date == dateTime.Date));
            if (recordBusinessUser == null)
            {
                recordBusinessUser = new RecordBusinessUserDB();
                recordBusinessUser.recordTime = dateTime;
                recordBusinessUser.businessId = notifyMsg.businessId;
                recordBusinessUser.allUsers = 0;
                recordBusinessUser.newUsers = 0;
                recordBusinessUser.effectiveUsers = 0;
                recordBusinessUser.useOneTickets = 0;
                recordBusinessUser.useTwoTickets = 0;
                recordBusinessUser.useThreeTickets = 0;
                recordBusinessUser.useFourTickets = 0;
                dataOperate = DataOperate.Insert;
            }
            if (notifyMsg.recordType == 0)
            {
                //新增有效人数
                recordBusinessUser.effectiveUsers += 1;
            }
            else if (notifyMsg.recordType == 1)
            {
                //新增人数,包括总人数
                recordBusinessUser.allUsers += 1;
                recordBusinessUser.newUsers += 1;
            }
            else if (notifyMsg.recordType == 2)
            {
                //总人数
                recordBusinessUser.allUsers += 1;
            }
            else if (notifyMsg.recordType == 3)
            {
                //一等优惠券使用个数
                recordBusinessUser.useOneTickets += 1;
            }
            else if (notifyMsg.recordType == 4)
            {
                //二等优惠券使用个数
                recordBusinessUser.useTwoTickets += 1;
            }
            else if (notifyMsg.recordType == 5)
            {
                //三等优惠券使用个数
                recordBusinessUser.useThreeTickets += 1;
            }
            else if (notifyMsg.recordType == 6)
            {
                //四等优惠券使用个数
                recordBusinessUser.useFourTickets += 1;
            }
            else
            {
                return;
            }
            bool result = NHibernateHelper.InsertOrUpdateOrDelete<RecordBusinessUserDB>(recordBusinessUser, dataOperate, true);
            if (!result)
            {
                //统计商家信息失败
                ServerUtil.RecordLog(LogType.Error, "统计商家信息失败 businessId = " + notifyMsg.businessId + ", dataOperate = " + dataOperate);
            }
        }
    }
}

