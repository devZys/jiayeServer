using System;
using System.Collections.Generic;
using LegendProtocol;
using LegendServerDB.Core;
using LegendServer.Database;
using LegendServer.Database.Summoner;
using LegendServerDB.Distributed;
using LegendServerDBDefine;
using System.Collections.Concurrent;
#if MAHJONG
using LegendServer.Database.Mahjong;
#elif RUNFAST
using LegendServer.Database.RunFast;
#elif WORDPLATE
using LegendServer.Database.WordPlate;
#endif

namespace LegendServerDB.Login
{
    public class LoginMsgProxy : ServerMsgProxy
    {
        private LoginMain main;
        private Dictionary<string, PreRegistSummoner> preRegistSummomer = new Dictionary<string, PreRegistSummoner>();

        public LoginMsgProxy(LoginMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnReqLogin(int peerId, bool inbound, object msg)
        {
            if (inbound == false) return;

            RequestLogin_A2D reqMsg = msg as RequestLogin_A2D;

            if (string.IsNullOrEmpty(reqMsg.userId) || reqMsg.nickName == null || reqMsg.headImgUrl == null)
            {
                ReplyLogin_D2A replyMsg = new ReplyLogin_D2A();
                replyMsg.acPeerId = reqMsg.acPeerId;
                replyMsg.result = ResultCode.InvalidPlayer;
                replyMsg.unlockTime = "";
                SendMsg(peerId, inbound, replyMsg);
                return;
            }

            SummonerDB playerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(player => player.userId == reqMsg.userId);
            if (playerDB != null)
            {
                if (playerDB.auth == UserAuthority.Illegal)
                {
                    //处于封号阶段
                    ReplyLogin_D2A replyMsg = new ReplyLogin_D2A();
                    replyMsg.acPeerId = reqMsg.acPeerId;
                    replyMsg.result = ResultCode.NoAuth;
                    replyMsg.unlockTime = playerDB.unLockTime.ToString();
                    SendMsg(peerId, inbound, replyMsg);
                    return;
                }
            }
            else
            {
                //临时存储准备注册的玩家信息
                preRegistSummomer[reqMsg.userId] = new PreRegistSummoner(reqMsg.nickName, reqMsg.headImgUrl, reqMsg.sex, reqMsg.acPeerId, reqMsg.acId, reqMsg.requesterIp);

                //请求分配UID（因为玩家的UID长度限制，所以不采用自己产生UID的方式）
                //等收到回复消息时继续创建Summmoner流程
                RequestUID_X2X reqUIDMsg = new RequestUID_X2X();
                reqUIDMsg.userId = reqMsg.userId;
                reqUIDMsg.uidType = UIDType.SummoerID;
                SendCenterMsg(reqUIDMsg);

                return;
            }

            bool updateDB = false;
            if (!string.Equals(reqMsg.nickName, "") && !string.Equals(playerDB.nickName, reqMsg.nickName))
            {
                playerDB.nickName = main.GetDefaultEncodingString(reqMsg.nickName);
                NHibernateHelper.SQLInsertOrUpdateOrDelete("update roomcard set nickName = '" + playerDB.nickName + "' where id = " + playerDB.id);
                updateDB = true;
            }
            if (!string.Equals(reqMsg.headImgUrl, "") && !string.Equals(playerDB.headImgUrl, reqMsg.headImgUrl))
            {
                playerDB.headImgUrl = reqMsg.headImgUrl;
                updateDB = true;
            }
            if (reqMsg.sex != UserSex.Shemale && playerDB.sex != reqMsg.sex)
            {
                playerDB.sex = reqMsg.sex;
                updateDB = true;
            }

            //通知给中心服来计算出最佳网关与逻辑服后将会自动继续登陆流程
            NotifyVerifyPassed_D2C msg_D2C = new NotifyVerifyPassed_D2C();
            msg_D2C.acId = reqMsg.acId;
            msg_D2C.acPeerId = reqMsg.acPeerId;
            msg_D2C.requesterUserId = playerDB.userId;
            msg_D2C.requesterNickName = playerDB.nickName;
            msg_D2C.requesterIp = reqMsg.requesterIp;
            msg_D2C.auth = playerDB.auth;
            msg_D2C.summonerId = playerDB.id;

            //房间信息
            if (playerDB.houseId != 0)
            {
#if MAHJONG
                //如果存在房间则强制分配到该房间所在的逻辑服
                MahjongHouseDB house = DBManager<MahjongHouseDB>.Instance.GetSingleRecordInCache(e => e.houseId == playerDB.houseId);
                if (house != null && house.logicId > 0 && house.houseStatus < MahjongHouseStatus.MHS_Dissolved)
                {
                    msg_D2C.logicId = house.logicId;
                }
#elif RUNFAST
                RunFastHouseDB house = DBManager<RunFastHouseDB>.Instance.GetSingleRecordInCache(e => e.houseId == playerDB.houseId);
                if (house != null && house.logicId > 0 && house.houseStatus < RunFastHouseStatus.RFHS_Dissolved)
                {
                    msg_D2C.logicId = house.logicId;
                }
#elif WORDPLATE
                WordPlateHouseDB house = DBManager<WordPlateHouseDB>.Instance.GetSingleRecordInCache(e => e.houseId == playerDB.houseId);
                if (house != null && house.logicId > 0 && house.houseStatus < WordPlateHouseStatus.EWPS_Dissolved)
                {
                    msg_D2C.logicId = house.logicId;
                }
#endif
                else
                {
                    playerDB.houseId = 0;
                    updateDB = true;
                }
            }
            //比赛场
            if (playerDB.competitionKey != 0)
            {
                int logicId = CompetitionKeyManager.Instance.GetLogicIdByCompetitionKey(playerDB.competitionKey);
                if (logicId <= 0)
                {
                    playerDB.competitionKey = 0;
                    updateDB = true;
                }
                else if (msg_D2C.logicId != logicId)
                {
                    msg_D2C.logicId = logicId;
                }
            }

            if (updateDB)
            {
                //只有当玩家传过来的字符串为非空时才处理更新，因为为空串则说明无变化，性别如果是传过人妖说明也是无变化
                DBManager<SummonerDB>.Instance.UpdateRecordInCache(playerDB, e => e.id == playerDB.id);
            }

            SendCenterMsg(msg_D2C);
        }
        private SummonerDB CreateSummoner(ulong uid, string userId, string nickName, string headImgUrl, UserSex sex)
        {
            if (nickName.Length <= 0)
            {
                //数据库清空过，但玩家游戏没卸载从沙盒里登陆时就会走到这儿来
                nickName = "游客";
            }
            if (headImgUrl.Length <= 0)
            {
                //数据库清空过，但玩家游戏没卸载从沙盒里登陆时就会走到这儿来
                headImgUrl = main.defaultHeadIcon;
            }

            SummonerDB playerDB = new SummonerDB();
            playerDB.id = uid;
            playerDB.userId = userId;
            playerDB.nickName = main.GetDefaultEncodingString(nickName);
            playerDB.loginTime = DateTime.Parse("1970-01-01 00:00:00");
            playerDB.registTime = DateTime.Now;
            playerDB.unLockTime = DateTime.Parse("1970-01-01 00:00:00");
            playerDB.auth = UserAuthority.Guest;
            playerDB.headImgUrl = headImgUrl;
            playerDB.sex = sex;
            playerDB.dailySharingTime = DateTime.Parse("1970-01-01 00:00:00");
            playerDB.bOpenHouse = false;
            List<RecordBusinessNode> recordBusinessList = new List<RecordBusinessNode>();
            playerDB.business = Serializer.tryCompressObject(recordBusinessList);
            List<TicketsNode> ticketsList = new List<TicketsNode>();
            playerDB.tickets = Serializer.tryCompressObject(ticketsList);
            playerDB.belong = 0;
            playerDB.belongBindTime = DateTime.Parse("1970-01-01 00:00:00");

            bool result = DBManager<SummonerDB>.Instance.AddRecordToCache(playerDB, element => element.id == playerDB.id);
            if (!result)
            {
                //首先登陆创建本地帐号时失败
                ServerUtil.RecordLog(LogType.Error, "首先登陆创建本地帐号时失败 userid = " + userId + ", nickName = " + nickName);
                return null;
            }

            //加入房卡
            RoomCardDB roomCard = new RoomCardDB();
            roomCard.id = uid;
            roomCard.nickName = playerDB.nickName;
            roomCard.roomCard = main.initRoomCard;
            result = NHibernateHelper.InsertOrUpdateOrDelete<RoomCardDB>(roomCard, DataOperate.Insert, true);
            if (!result)
            {
                //首先登陆创建本地帐号时失败
                ServerUtil.RecordLog(LogType.Error, "首先登陆创建本地帐号时失败 userid = " + userId + ", nickName = " + nickName);
                DBManager<SummonerDB>.Instance.DeleteRecordInCache(element => element.id == playerDB.id);
                return null;
            }

            return playerDB;
        }
        public void OnReplyUID(int peerId, bool inbound, object msg)
        {
            ReplyUID_X2X replyMsg = msg as ReplyUID_X2X;

            if (replyMsg.result == ResultCode.OK)
            {
                switch (replyMsg.uidType)
                {
                    case UIDType.SummoerID:
                        OnNewSummonerID(replyMsg);
                        break;
                    default:
                        ServerUtil.RecordLog(LogType.Error, "OnReplyUID userid = " + replyMsg.userId + ", uidType = " + replyMsg.uidType);
                        break;
                }
            }
            else
            {
                ServerUtil.RecordLog(LogType.Error, "OnReplyUID userid = " + replyMsg.userId + ", result = " + replyMsg.result);
            }
        }
        private void OnNewSummonerID(ReplyUID_X2X replyUIDMsg)
        {
            PreRegistSummoner registInfo = null;
            if (preRegistSummomer.TryGetValue(replyUIDMsg.userId, out registInfo))
            {
                SummonerDB playerDB = CreateSummoner(replyUIDMsg.uid, replyUIDMsg.userId, registInfo.nickName, registInfo.headImgUrl, registInfo.sex);
                if (playerDB == null)
                {
                    preRegistSummomer.Remove(replyUIDMsg.userId);

                    ReplyLogin_D2A replyMsg = new ReplyLogin_D2A();
                    replyMsg.acPeerId = registInfo.acPeerId;
                    replyMsg.result = ResultCode.InvalidPlayer;
                    replyMsg.unlockTime = "";
                    SendACMsg(replyMsg, registInfo.acServerId);
                    return;
                }

                byte[] msgBytes = ServerUtil.Serialize(playerDB);
                if (msgBytes != null)
                {
                    //往web后台发通知消息
                    MQSender.Instance.Send(MQID.NotifyNewPlayerInfo, msgBytes);
                }

                //通知给中心服来计算出最佳网关与逻辑服后将会自动继续登陆流程
                NotifyVerifyPassed_D2C msg_D2C = new NotifyVerifyPassed_D2C();
                msg_D2C.acId = registInfo.acServerId;
                msg_D2C.acPeerId = registInfo.acPeerId;
                msg_D2C.requesterUserId = playerDB.userId;
                msg_D2C.requesterNickName = playerDB.nickName;
                msg_D2C.requesterIp = registInfo.requesterIp;
                msg_D2C.auth = playerDB.auth;
                msg_D2C.summonerId = playerDB.id;

                SendCenterMsg(msg_D2C);
            }
            else
            {
                ServerUtil.RecordLog(LogType.Error, "OnNewSummonerID userId = " + replyUIDMsg.userId);
            }
        }
        public void OnNotifyCreateCompetition(int peerId, bool inbound, object msg)
        {
            NotifyCreateCompetition_W2D notifyMsg = msg as NotifyCreateCompetition_W2D;

            if (notifyMsg.competitionKey > 0 && notifyMsg.logicId > 0)
            {
                CompetitionKeyManager.Instance.AddCompetitionKey(notifyMsg.competitionKey, notifyMsg.logicId);
            }
        }
        public void OnNotifyDelCompetition(int peerId, bool inbound, object msg)
        {
            NotifyDelCompetition_W2D notifyMsg = msg as NotifyDelCompetition_W2D;

            notifyMsg.competitionKeyList.ForEach(competitionKey =>
            {
                CompetitionKeyManager.Instance.RemoveCompetitionKey(competitionKey);
            });
        }
    }
}

