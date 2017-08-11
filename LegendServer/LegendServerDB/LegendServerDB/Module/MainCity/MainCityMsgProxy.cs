using System.Collections.Generic;
using LegendProtocol;
using LegendServerDB.Core;
using LegendServer.Database;
using LegendServer.Database.Summoner;
using System;
using System.Text;
using LegendServerDB.Distributed;
#if MAHJONG
using LegendServerDB.Mahjong;
#elif RUNFAST
using LegendServerDB.RunFast;
#elif WORDPLATE
using LegendServerDB.WordPlate;
#endif

namespace LegendServerDB.MainCity
{
    public class MainCityMsgProxy : ServerMsgProxy
    {
        private MainCityMain main;

        public MainCityMsgProxy(MainCityMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestSummonerData(int peerId, bool inbound, object msg)
        {
            RequestSummonerData_L2D reqMsg = msg as RequestSummonerData_L2D;
            ReplySummonerData_D2L replyMsg = new ReplySummonerData_D2L();
            replyMsg.userId = reqMsg.userId;
            replyMsg.acServerId = reqMsg.acServerId;
            replyMsg.proxyServerId = reqMsg.proxyServerId;

            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.userId == reqMsg.userId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "DB OnRequestSummonerData SummonerDB 无效召唤师 userId = " + reqMsg.userId);
                replyMsg.result = ResultCode.InvalidPlayer;
                SendMsg(peerId, true, replyMsg);
                return;
            }
            
            replyMsg.result = ResultCode.OK;
            replyMsg.userId = reqMsg.userId;
            replyMsg.id = summonerDB.id;
            replyMsg.nickName = summonerDB.nickName;
            replyMsg.auth = summonerDB.auth;
            replyMsg.houseId = summonerDB.houseId;
            replyMsg.competitionKey = summonerDB.competitionKey;
            replyMsg.sex = summonerDB.sex;
            replyMsg.allIntegral = summonerDB.allIntegral;
            replyMsg.ip = reqMsg.ip;
            replyMsg.dailySharingTime = summonerDB.dailySharingTime.ToString();
            replyMsg.bOpenHouse = summonerDB.bOpenHouse;
            replyMsg.recordBusinessList.AddRange(Serializer.tryUncompressObject<List<RecordBusinessNode>>(summonerDB.business));
            replyMsg.ticketsList.AddRange(Serializer.tryUncompressObject<List<TicketsNode>>(summonerDB.tickets));
            DateTime nowTime = DateTime.Now;
            if (summonerDB.loginTime == DateTime.Parse("1970-01-01 00:00:00"))
            {
                //新用户第一次登陆上线
                main.RecordLoginUser(RecordLoginType.NewUser);
                replyMsg.bFirstLogin = true;
            }
            else
            {
                if (summonerDB.loginTime.Date != nowTime.Date)
                {
                    //今天第一次登陆上线
                    main.RecordLoginUser(RecordLoginType.LoginUser);
                }
                replyMsg.bFirstLogin = false;
            }
            EmployeeInfoDB myEmployee = NHibernateHelper.DirectGetSingleRecordByCondition<EmployeeInfoDB>(e => e.summonerId == summonerDB.id);
            if (myEmployee != null && myEmployee.jobTitle >= JobTitle.Shopkeeper)
            {
                //如果自己是店长或者以上身份的雇员则将自己的ID做为邀请码
                replyMsg.belong = summonerDB.id;
            }
            else
            {
                //否则拿自己的归属者的ID作为邀请码（此归属者必定是店长或者以上身份的雇员）
                replyMsg.belong = summonerDB.belong;
            }
            RoomCardDB roomCard = NHibernateHelper.DirectGetSingleRecordByCondition<RoomCardDB>(element => element.id == summonerDB.id);
            if (roomCard != null)
            {
                replyMsg.roomCard = roomCard.roomCard;
            }
            else
            {
                replyMsg.roomCard = 0;
            }

            SendMsg(peerId, true, replyMsg);

            summonerDB.loginTime = DateTime.Now;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
        }

        public void OnSendFeedback(int peerId, bool inbound, object msg)
        {
            SendFeedback_L2D sendFeedback_L2D = msg as SendFeedback_L2D;

            if (string.IsNullOrEmpty(sendFeedback_L2D.phoneNumber) || string.IsNullOrEmpty(sendFeedback_L2D.feedback)) return;

            if (Encoding.Default.GetByteCount(sendFeedback_L2D.phoneNumber) > main.feedbackPhoneNumberSizeLimit ||
                Encoding.Default.GetByteCount(sendFeedback_L2D.feedback) > main.feedbackTextSizeLimit)
            {
                //有数据超过长度
                return;
            }

            string sql = "INSERT INTO feedback(id, context, phoneNumber, time) SELECT '" + sendFeedback_L2D.id + "', '" + sendFeedback_L2D.feedback + "', '" + sendFeedback_L2D.phoneNumber +
                 "', '" + DateTime.Now + "' FROM DUAL WHERE NOT EXISTS(SELECT id, context FROM feedback WHERE id = '" + sendFeedback_L2D.id + "' AND context = '" + sendFeedback_L2D.feedback + "')";

            NHibernateHelper.SQLInsertOrUpdateOrDelete(sql);
        }

        public void OnRequestSaveHouseId(int peerId, bool inbound, object msg)
        {
            RequestSaveHouseId_L2D reqMsg = msg as RequestSaveHouseId_L2D;

            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.userId == reqMsg.userId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseId error!! userId = " + reqMsg.userId);
                return;
            }
            summonerDB.houseId = reqMsg.houseId;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
        }
        public void OnRequestSaveHouseCard(int peerId, bool inbound, object msg)
        {
            RequestSaveHouseCard_L2D reqMsg = msg as RequestSaveHouseCard_L2D;

            if (reqMsg.houseCard <= 0)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseCard 操作的房卡数错误!! guid = " + reqMsg.guid + ", houseCard = " + reqMsg.houseCard);
                return;
            }
            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg.guid);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseCard summonerDB == null !! guid = " + reqMsg.guid);
                return;
            }
            RoomCardDB roomCard = NHibernateHelper.DirectGetSingleRecordByCondition<RoomCardDB>(element => element.id == reqMsg.guid);
            if (roomCard == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseCard roomCard == null !! guid = " + reqMsg.guid);
                return;
            }
            if (reqMsg.type == OperationType.AddData)
            {
                int sumAmount = roomCard.roomCard + reqMsg.houseCard;
                if (sumAmount <= roomCard.roomCard && sumAmount < reqMsg.houseCard)
                {
                    //加爆了
                    ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseCard 加爆了!! guid = " + reqMsg.guid + ", roomCard = " + roomCard.roomCard + ", houseCard = " + reqMsg.houseCard);
                    sumAmount = int.MaxValue;
                }
                roomCard.roomCard = sumAmount;

                NHibernateHelper.InsertOrUpdateOrDelete<RoomCardDB>(roomCard, DataOperate.Update, true);
                //记录归还卡
                main.OnRecordRoomCard(summonerDB.id, RecordRoomCardType.DissolvedRecycle, reqMsg.houseCard);
            }
            else if (reqMsg.type == OperationType.DelData)
            {
                if (roomCard.roomCard < reqMsg.houseCard)
                {
                    ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseCard 房卡不够减!! guid = " + reqMsg.guid + ", roomCard = " + roomCard.roomCard + ", houseCard = " + reqMsg.houseCard);
                    return;
                }
                roomCard.roomCard -= reqMsg.houseCard;

                NHibernateHelper.InsertOrUpdateOrDelete<RoomCardDB>(roomCard, DataOperate.Update, true);
                //记录扣卡
                main.OnRecordRoomCard(summonerDB.id, RecordRoomCardType.NormalConsumption, reqMsg.houseCard);
            }
            else
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseCard error!! guid = " + reqMsg.guid + ", type = " + reqMsg.type);
            }
        }
        public void OnRequestSavePlayerAllIntegral(int peerId, bool inbound, object msg)
        {
            RequestSavePlayerAllIntegral_L2D reqMsg = msg as RequestSavePlayerAllIntegral_L2D;

            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.userId == reqMsg.userId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSavePlayerAllIntegral error!! userId = " + reqMsg.userId);
                return;
            }
            bool bSave = false;
            if (!(reqMsg.addIntegral == 0 || (summonerDB.allIntegral == int.MaxValue && reqMsg.addIntegral > 0) || (summonerDB.allIntegral == int.MinValue && reqMsg.addIntegral < 0)))
            {
                int sumIntegral = summonerDB.allIntegral + reqMsg.addIntegral;
                if (summonerDB.allIntegral > 0 && sumIntegral <= summonerDB.allIntegral && sumIntegral < reqMsg.addIntegral)
                {
                    //正数加爆了
                    ServerUtil.RecordLog(LogType.Error, "OnRequestSavePlayerAllIntegral 正数加爆了!! allIntegral = " + summonerDB.allIntegral + ", addIntegral = " + reqMsg.addIntegral);
                    summonerDB.allIntegral = int.MaxValue;
                    bSave = true;
                }
                else if (summonerDB.allIntegral < 0 && sumIntegral >= summonerDB.allIntegral && sumIntegral > reqMsg.addIntegral)
                {
                    //负数加爆了
                    ServerUtil.RecordLog(LogType.Error, "OnRequestSavePlayerAllIntegral 负数加爆了!! allIntegral = " + summonerDB.allIntegral + ", addIntegral = " + reqMsg.addIntegral);
                    summonerDB.allIntegral = int.MinValue;
                    bSave = true;
                }
                else
                {
                    summonerDB.allIntegral += reqMsg.addIntegral;
                    bSave = true;
                }
            }
            //清除房间号
            if (summonerDB.houseId > 0)
            {
                summonerDB.houseId = 0;
                bSave = true;
            }
            if (bSave)
            {
                DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
            }
        }

        public void OnRequestSaveDailySharing(int peerId, bool inbound, object msg)
        {
            RequestSaveDailySharing_L2D reqMsg = msg as RequestSaveDailySharing_L2D;

            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.userId == reqMsg.userId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveDailySharing error!! userId = " + reqMsg.userId);
                return;
            }
            RoomCardDB roomCard = NHibernateHelper.DirectGetSingleRecordByCondition<RoomCardDB>(element => element.id == summonerDB.id);
            if (roomCard == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveDailySharing error!! guid = " + summonerDB.id);
                return;
            }
            DateTime dailySharingTime = DateTime.Now;
            if (reqMsg.addHouseCard == 0 || string.IsNullOrEmpty(reqMsg.dailySharingTime) || !DateTime.TryParse(reqMsg.dailySharingTime, out dailySharingTime))
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveDailySharing error!! addHouseCard = " + reqMsg.addHouseCard + ", dailySharingTime = " + reqMsg.dailySharingTime);
                return;
            }
            //房卡
            int sumAmount = roomCard.roomCard + reqMsg.addHouseCard;
            if (sumAmount <= roomCard.roomCard && sumAmount < reqMsg.addHouseCard)
            {
                //加爆了
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveDailySharing error!! roomCard = " + roomCard.roomCard + ", addHouseCard = " + reqMsg.addHouseCard);
                sumAmount = int.MaxValue;
            }
            roomCard.roomCard = sumAmount;

            NHibernateHelper.InsertOrUpdateOrDelete<RoomCardDB>(roomCard, DataOperate.Update, true);

            //每日分享领奖励时间
            summonerDB.dailySharingTime = dailySharingTime;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);

            //记录房卡变化
            main.OnRecordRoomCard(summonerDB.id, RecordRoomCardType.DailySharing, reqMsg.addHouseCard);
        }
        public void OnRequestSaveRecordBusiness(int peerId, bool inbound, object msg)
        {
            RequestSaveRecordBusiness_L2D reqMsg = msg as RequestSaveRecordBusiness_L2D;

            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.userId == reqMsg.userId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveRecordBusiness error!! userId = " + reqMsg.userId);
                return;
            }
            List<RecordBusinessNode> recordBusinessList = Serializer.tryUncompressObject<List<RecordBusinessNode>>(summonerDB.business);
            if (recordBusinessList == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveRecordBusiness error!! userId = " + reqMsg.userId);
                return;
            }
            bool bSave = false;
            if (reqMsg.businessId == 0)
            {
                if (!summonerDB.bOpenHouse)
                {
                    summonerDB.bOpenHouse = true;
                    bSave = true;
                    //统计
                    DateTime nowTime = DateTime.Now;
                    foreach (RecordBusinessNode node in recordBusinessList)
                    {
                        if (node.lastTime == nowTime.Date.ToShortDateString())
                        {
                            //属于今天本商家的新增有效用户
                            main.RecordBusinessUsers(node.businessId, node.lastTime, 0);
                        }
                    }
                }
            }
            else
            {
                RecordBusinessNode businessNode = recordBusinessList.Find(element => element.businessId == reqMsg.businessId);
                if (businessNode == null)
                {
                    businessNode = new RecordBusinessNode();
                    businessNode.businessId = reqMsg.businessId;
                    businessNode.lastTime = reqMsg.lastTime;
                    recordBusinessList.Add(businessNode);
                    bSave = true;
                    //统计
                    if (!summonerDB.bOpenHouse)
                    {
                        //新增人数，必是总人数
                        main.RecordBusinessUsers(businessNode.businessId, businessNode.lastTime, 1);
                    }
                    else
                    {
                        //统计商家总人数
                        main.RecordBusinessUsers(businessNode.businessId, businessNode.lastTime, 2);
                    }
                }
                else
                {
                    if (businessNode.lastTime != reqMsg.lastTime)
                    {
                        businessNode.lastTime = reqMsg.lastTime;
                        bSave = true;
                        //统计商家总人数
                        main.RecordBusinessUsers(businessNode.businessId, businessNode.lastTime, 2);
                    }
                }
                if (bSave)
                {
                    summonerDB.business = Serializer.tryCompressObject(recordBusinessList);
                }
            }
            if (bSave)
            {
                DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
            }
        }
        public void OnRequestSaveTickets(int peerId, bool inbound, object msg)
        {
            RequestSaveTickets_L2D reqMsg = msg as RequestSaveTickets_L2D;

            if (reqMsg.ticketsNode == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveTickets error!! ticketsNode == null");
                return;
            }
            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg.summonerId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveTickets error!! summonerId = " + reqMsg.summonerId);
                return;
            }
            List<TicketsNode> ticketsList = Serializer.tryUncompressObject<List<TicketsNode>>(summonerDB.tickets);
            if (ticketsList == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveTickets error!! summonerId = " + reqMsg.summonerId);
                return;
            }
            if (!ticketsList.Contains(reqMsg.ticketsNode))
            {
                ticketsList.Add(reqMsg.ticketsNode);
                summonerDB.tickets = Serializer.tryCompressObject(ticketsList);
                DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
            }
        }
        public void OnRequestDelTickets(int peerId, bool inbound, object msg)
        {
            RequestDelTickets_L2D reqMsg = msg as RequestDelTickets_L2D;

            if (reqMsg.ticketsOnlyIdList == null || reqMsg.ticketsOnlyIdList.Count == 0)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelTickets ticketsOnlyIdList error!!");
                return;
            }
            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.userId == reqMsg.userId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelTickets error!! userId = " + reqMsg.userId);
                return;
            }
            List<TicketsNode> ticketsList = Serializer.tryUncompressObject<List<TicketsNode>>(summonerDB.tickets);
            if (ticketsList == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelTickets error!! userId = " + reqMsg.userId);
                return;
            }
            bool bSave = false;
            foreach (ulong ticketsOnlyId in reqMsg.ticketsOnlyIdList)
            {
                int count = ticketsList.RemoveAll(element => element.id == ticketsOnlyId);
                if (!bSave && count > 0)
                {
                    bSave = true;
                }
            }
            if (bSave)
            {
                summonerDB.tickets = Serializer.tryCompressObject(ticketsList);
                DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
            }
        }
        public void OnRequestUseTickets(int peerId, bool inbound, object msg)
        {
            RequestUseTickets_L2D reqMsg = msg as RequestUseTickets_L2D;

            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.userId == reqMsg.userId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestUseTickets error!! userId = " + reqMsg.userId);
                return;
            }
            List<TicketsNode> ticketsList = Serializer.tryUncompressObject<List<TicketsNode>>(summonerDB.tickets);
            if (ticketsList == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestUseTickets error!! userId = " + reqMsg.userId);
                return;
            }
            TicketsNode ticketsNode = ticketsList.Find(element => element.id == reqMsg.ticketsOnlyId);
            if (ticketsNode != null && ticketsNode.useStatus != UseStatus.Used)
            {
                ticketsNode.useStatus = UseStatus.Used;
                summonerDB.tickets = Serializer.tryCompressObject(ticketsList);
                DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
                //统计
                main.RecordBusinessUsersByTickets(ticketsNode.ticketsId);
            }
        }
        public void OnRequestBindBelong(int peerId, bool inbound, object msg)
        {
            RequestBindBelong_L2D msg_L2D = msg as RequestBindBelong_L2D;
            ReplyBindBelong_D2L reply_D2L = new ReplyBindBelong_D2L();

            reply_D2L.requestSummonerId = msg_L2D.requestSummonerId;

            SummonerDB my = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(e => e.id == msg_L2D.requestSummonerId);
            if (my == null)
            {
                //内部错误
                reply_D2L.result = ResultCode.Wrong;
                SendMsg(peerId, inbound, reply_D2L);
                return;
            }
            if (my.belong > 0)
            {
                //自己已绑定过
                reply_D2L.result = ResultCode.AlreadyBelong;
                SendMsg(peerId, inbound, reply_D2L);
                return;
            }
            if (msg_L2D.bindCode == my.id)
            {
                //不能绑定自己
                reply_D2L.result = ResultCode.InvalidCode;
                SendMsg(peerId, inbound, reply_D2L);
                return;
            }
            SummonerDB target = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(e => e.id == msg_L2D.bindCode);
            if (target == null)
            {
                //目标无效
                reply_D2L.result = ResultCode.InvalidCode;
                SendMsg(peerId, inbound, reply_D2L);
                return;
            }
            if (target.belong == my.id)
            {
                //目标已绑定过我
                reply_D2L.result = ResultCode.AlreadyTargetBindMe;
                SendMsg(peerId, inbound, reply_D2L);
                return;
            }
            EmployeeInfoDB targetEmployee = NHibernateHelper.DirectGetSingleRecordByCondition<EmployeeInfoDB>(e => e.summonerId == target.id);
            if (targetEmployee == null || targetEmployee.jobTitle < JobTitle.Shopkeeper)
            {
                //目标必须是店长或以上管理员才行
                reply_D2L.result = ResultCode.InvalidCode;
                SendMsg(peerId, inbound, reply_D2L);
                return;
            }
            EmployeeInfoDB myEmployee = NHibernateHelper.DirectGetSingleRecordByCondition<EmployeeInfoDB>(e => e.summonerId == my.id);
            if (myEmployee != null)
            {
                if (myEmployee.ExistJunnior(targetEmployee.account) || targetEmployee.ExistJunnior(myEmployee.account))
                {
                    //双方已经是间接的绑定关系
                    reply_D2L.result = ResultCode.AlreadyMiddleman;
                    SendMsg(peerId, inbound, reply_D2L);
                    return;
                }
            }
            //绑定成功
            my.belong = target.id;
            my.belongBindTime = DateTime.Now;
            if (DBManager<SummonerDB>.Instance.UpdateRecordInCache(my, e => e.id == my.id))
            {
                //往web后台发通知消息
                MQSender.Instance.Send(MQID.NotifyPlayerBindInfo, my.id + "&" + target.id + "&" + my.belongBindTime.ToString());
            }

            reply_D2L.result = ResultCode.OK;
            if (myEmployee != null && myEmployee.jobTitle >= JobTitle.Shopkeeper)
            {
                //如果自己是店长或者以上身份的雇员则将自己的ID做为邀请码
                reply_D2L.bindCode = my.id;
            }
            else
            {
                //否则拿自己的归属者的ID作为邀请码（此归属者必定是店长或者以上身份的雇员）
                reply_D2L.bindCode = my.belong;
            }

            //以下做绑定之后的房卡奖励操作
            RoomCardDB roomCard = NHibernateHelper.DirectGetSingleRecordByCondition<RoomCardDB>(element => element.id == my.id);
            if (roomCard != null && main.bindCodeRewardRoomCard > 0)
            {
                //房卡
                int sumAmount = roomCard.roomCard + main.bindCodeRewardRoomCard;
                if (sumAmount <= roomCard.roomCard && sumAmount < main.bindCodeRewardRoomCard)
                {
                    //加爆了
                    ServerUtil.RecordLog(LogType.Error, "OnRequestBindBelong error!! roomCard = " + roomCard.roomCard + ", bindCodeRewardRoomCard = " + main.bindCodeRewardRoomCard);
                    sumAmount = int.MaxValue;
                }
                roomCard.roomCard = sumAmount;
                NHibernateHelper.InsertOrUpdateOrDelete<RoomCardDB>(roomCard, DataOperate.Update, true);

                //记录房卡变化
                main.OnRecordRoomCard(my.id, RecordRoomCardType.BindCodeRewardRoomCard, main.bindCodeRewardRoomCard);

                reply_D2L.rewardRoomCard = main.bindCodeRewardRoomCard;
            }

            SendMsg(peerId, inbound, reply_D2L);

            //往客户web系统发通知消息
            //MQSender.Instance.Send(MQID.NotifyPlayerBindInfo, my.id + "&" + target.id + "&" + my.belongBindTime.ToString());
        }
        public void OnRequestPlayerHeadImgUrl(int peerId, bool inbound, object msg)
        {
            RequestPlayerHeadImgUrl_L2D reqMsg_L2D = msg as RequestPlayerHeadImgUrl_L2D;
            ReplyPlayerHeadImgUrl_D2L reply_D2L = new ReplyPlayerHeadImgUrl_D2L();

            reply_D2L.requestSummonerId = reqMsg_L2D.requestSummonerId;

            SummonerDB myPlayer = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(e => e.id == reqMsg_L2D.summonerId);
            if (myPlayer == null)
            {
                //内部错误
                reply_D2L.result = ResultCode.Wrong;
                SendMsg(peerId, inbound, reply_D2L);
                return;
            }

            reply_D2L.summonerId = reqMsg_L2D.summonerId;
            reply_D2L.headImgUrl = myPlayer.headImgUrl;
            SendMsg(peerId, inbound, reply_D2L);
        }

        public void OnRequestDelZombieHouse(List<ulong> delZombieHouseId)
        {
            RequestDelZombieHouse_D2L reqMsg_D2L = new RequestDelZombieHouse_D2L();
            reqMsg_D2L.delHouseIdList.AddRange(delZombieHouseId);

            //广播给所有的逻辑服务器移除僵尸房
            BroadCastLogicMsg(reqMsg_D2L);
        }
        public void OnNotifyUpdateAnnouncement(string announcement)
        {
            NotifyUpdateAnnouncement_D2L notifyMsg = new NotifyUpdateAnnouncement_D2L();
            notifyMsg.announcement = announcement;

            //广播给所属的逻辑服务器公告
            BroadCastLogicMsg(notifyMsg);
        }
        public void OnNotifyGameServerSendGoods(ulong summonerId, int addRoomCardNum)
        {
            NotifyGameServerSendGoods_X2X notifyMsg = new NotifyGameServerSendGoods_X2X();
            notifyMsg.summonerId = summonerId;
            notifyMsg.addRoomCardNum = addRoomCardNum;
            SendWorldMsg(notifyMsg);
        }
        public void OnRequestSaveCompetitionKey(int peerId, bool inbound, object msg)
        {
            RequestSaveCompetitionKey_L2D reqMsg_L2D = msg as RequestSaveCompetitionKey_L2D;

            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(e => e.id == reqMsg_L2D.requestSummonerId);
            if (summonerDB != null && summonerDB.competitionKey != reqMsg_L2D.competitionKey)
            {
                summonerDB.competitionKey = reqMsg_L2D.competitionKey;
                DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
            }
        }
        public void OnRequestInitHouseIdAndComKey(int peerId, bool inbound, object msg)
        {
            RequestInitHouseIdAndComKey_L2D reqMsg_L2D = msg as RequestInitHouseIdAndComKey_L2D;

            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(e => e.id == reqMsg_L2D.requestSummonerId);
            if (summonerDB != null)
            {
                summonerDB.houseId = 0;
                summonerDB.competitionKey = 0;
                DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
            }
        }
        public void OnNotifyDBClearHouse(int peerId, bool inbound, object msg)
        {
            NotifyDBClearHouse_L2D notifyMsg = msg as NotifyDBClearHouse_L2D;

            if (!ModuleManager.Get<DistributedMain>().DBLoaded)
            {
                ModuleManager.Get<DistributedMain>().msg_proxy.RequestDBConfig();
                //不是运行态时延迟执行
#if MAHJONG
                ModuleManager.Get<DistributedMain>().RegistLoadDBCompletedCallBack(() => { ModuleManager.Get<MahjongMain>().ProcessClearHouse(notifyMsg.logicId); });
#elif RUNFAST
                ModuleManager.Get<DistributedMain>().RegistLoadDBCompletedCallBack(() => { ModuleManager.Get<RunFastMain>().ProcessClearHouse(notifyMsg.logicId); });
#elif WORDPLATE
                ModuleManager.Get<DistributedMain>().RegistLoadDBCompletedCallBack(() => { ModuleManager.Get<WordPlateMain>().ProcessClearHouse(notifyMsg.logicId); });
#endif
                return;
            }
#if MAHJONG
            ModuleManager.Get<MahjongMain>().ProcessClearHouse(notifyMsg.logicId);
#elif RUNFAST
            ModuleManager.Get<RunFastMain>().ProcessClearHouse(notifyMsg.logicId);
#elif WORDPLATE
            ModuleManager.Get<WordPlateMain>().ProcessClearHouse(notifyMsg.logicId);
#endif
        }
    }
}

