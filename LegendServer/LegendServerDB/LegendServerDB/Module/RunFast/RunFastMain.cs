#if RUNFAST
using LegendProtocol;
using LegendServer.Database;
using System.Collections.Generic;
using System;
using LegendServer.Database.Config;
using LegendServer.Util;
using LegendServer.Database.RunFast;
using LegendServerDB.MainCity;
using LegendServerDB.Distributed;

namespace LegendServerDB.RunFast
{
    public class RunFastMain : Module
    {
        public RunFastMsgProxy msg_proxy;
        public int getTheOverallRecordNumber;
        public double getTheOverallRecordTime;
        public double roomInformationRetentionTime;
        public double zombieHouseRetentionTime;
        public RunFastMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new RunFastMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
            //获取大局战绩局数
            getTheOverallRecordNumber = 10;
            ServerConfigDB serverConfig = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(element => element.key == "GetTheOverallRecordNumber");
            if (serverConfig != null && !string.IsNullOrEmpty(serverConfig.value))
            {
                int.TryParse(serverConfig.value, out getTheOverallRecordNumber);
            }
            //获取大局战绩时间(天)
            getTheOverallRecordTime = 1.00d;
            serverConfig = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "GetTheOverallRecordTime");
            if (serverConfig != null && !string.IsNullOrEmpty(serverConfig.value))
            {
                double.TryParse(serverConfig.value, out getTheOverallRecordTime);
            }
            //房间信息保存时间(天)
            roomInformationRetentionTime = 3.00d;
            serverConfig = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "RoomInformationRetentionTime");
            if (serverConfig != null && !string.IsNullOrEmpty(serverConfig.value))
            {
                double.TryParse(serverConfig.value, out roomInformationRetentionTime);
            }
            //僵尸房间保存时间(天)
            zombieHouseRetentionTime = 30.0f;
            serverConfig = DBManager<ServerConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "ZombieHouseRetentionTime");
            if (serverConfig != null && !string.IsNullOrEmpty(serverConfig.value))
            {
                double.TryParse(serverConfig.value, out zombieHouseRetentionTime);
            }
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.L2D_RequestSaveCreateRunFastInfo, new MsgComponent(msg_proxy.OnRequestSaveCreateRunFastInfo, typeof(RequestSaveCreateRunFastInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveRunFastNewPlayer, new MsgComponent(msg_proxy.OnRequestSaveRunFastNewPlayer, typeof(RequestSaveRunFastNewPlayer_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestDelHousePlayer, new MsgComponent(msg_proxy.OnRequestDelHousePlayer, typeof(RequestDelHousePlayer_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveRunFastPlayerSettlement, new MsgComponent(msg_proxy.OnRequestSavePlayerSettlement, typeof(RequestSaveRunFastPlayerSettlement_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveRunFastHouseStatus, new MsgComponent(msg_proxy.OnRequestSaveRunFastHouseStatus, typeof(RequestSaveRunFastHouseStatus_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveRunFastBureauInfo, new MsgComponent(msg_proxy.OnRequestSaveRunFastBureauInfo, typeof(RequestSaveRunFastBureauInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveRunFastBureauIntegral, new MsgComponent(msg_proxy.OnRequestSaveBureauIntegral, typeof(RequestSaveRunFastBureauIntegral_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveBureauShowCard, new MsgComponent(msg_proxy.OnRequestSaveBureauShowCard, typeof(RequestSaveBureauShowCard_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestHouseInfo, new MsgComponent(msg_proxy.OnRequestHouseInfo, typeof(RequestHouseInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestHousePlayerAndBureau, new MsgComponent(msg_proxy.OnRequestHousePlayerAndBureau, typeof(RequestHousePlayerAndBureau_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveDissolveRunFastInfo, new MsgComponent(msg_proxy.OnRequestSaveDissolveRunFastInfo, typeof(RequestSaveDissolveRunFastInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestRunFastOverallRecord, new MsgComponent(msg_proxy.OnRequestRunFastOverallRecord, typeof(RequestRunFastOverallRecord_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestRunFastBureauRecord, new MsgComponent(msg_proxy.OnRequestRunFastBureauRecord, typeof(RequestRunFastBureauRecord_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestRunFastBureauPlayback, new MsgComponent(msg_proxy.OnRequestRunFastBureauPlayback, typeof(RequestRunFastBureauPlayback_L2D)));
        }
        public override void OnRegistTimer()
        {
            TimerManager.Instance.Regist(TimerId.HouseCheck, 0, 60 * 60 * 1000, int.MaxValue, OnRunFastHouseTimer, null, null, null);
        }
        public override void OnStart()
        {
           
        }
        public override void OnDestroy()
        {
        }
        private void OnRunFastHouseTimer(object obj)
        {
            DateTime nowTime = DateTime.Now;
            List<RunFastHouseDB> runFastHouseList = DBManager<RunFastHouseDB>.Instance.GetRecordsInCache(element => (nowTime - element.createTime).TotalDays >= zombieHouseRetentionTime || (element.houseStatus >= RunFastHouseStatus.RFHS_Dissolved && (nowTime - element.createTime).TotalDays >= roomInformationRetentionTime));
            if (runFastHouseList == null || runFastHouseList.Count <= 0)
            {
                return;
            }
            List<ulong> delHouseId = new List<ulong>();
            List<ulong> delZombieHouseId = new List<ulong>();
            foreach (RunFastHouseDB runFastHouseDB in runFastHouseList)
            {
                //只处理映射到我自己DB服务器的逻辑服务器中的房间
                if (msg_proxy.root.AllLogicMappingMe.Exists(e => e == runFastHouseDB.logicId))
                {
                    delHouseId.Add(runFastHouseDB.houseId);
                    if (runFastHouseDB.houseStatus < RunFastHouseStatus.RFHS_Dissolved)
                    {
                        delZombieHouseId.Add(runFastHouseDB.houseId);
                    }
                }
            }
            foreach (ulong houseId in delHouseId)
            {
                DBManager<RunFastHouseDB>.Instance.DeleteRecordInCache(element => element.houseId == houseId);
                DBManager<RunFastPlayerDB>.Instance.DeleteRecordInCache(element => element.houseId == houseId);
                DBManager<RunFastBureauDB>.Instance.DeleteRecordInCache(element => element.houseId == houseId);
            }
            if (delZombieHouseId.Count > 0)
            {
                //删除一个月以上僵尸房
                OnRequestDelZombieHouse(delZombieHouseId);
            }
        }
        private void OnRequestDelZombieHouse(List<ulong> delZombieHouseId)
        {
            ModuleManager.Get<MainCityMain>().OnRequestDelZombieHouse(delZombieHouseId);
        }
        public void CheckRegistTimer()
        {
            if (!TimerManager.Instance.Exist(TimerId.HouseCheck))
            {
                TimerManager.Instance.Regist(TimerId.HouseCheck, 0, 30 * 60 * 1000, int.MaxValue, OnRunFastHouseTimer, null, null, null);
            }
        }
        public string GetCardList(List<Card> cardList)
        {
            string strCard = "";
            for (int i = 0; i < cardList.Count; ++i)
            {
                strCard = strCard + (int)cardList[i].suit + ',' + (int)cardList[i].rank;
                if (i != cardList.Count - 1)
                {
                    strCard += '|';
                }
            }
            return strCard;
        }
        public string GetPlayerBureauIntegralList(List<PlayerBureauIntegral> bureauIntegralList)
        {
            string strCard = "";
            for (int i = 0; i < bureauIntegralList.Count; ++i)
            {
                strCard = strCard + bureauIntegralList[i].playerIndex + ',' + bureauIntegralList[i].cardIntegral
                     + ',' + bureauIntegralList[i].bombIntegral + ',' + bureauIntegralList[i].bureauIntegral;
                if (i != bureauIntegralList.Count - 1)
                {
                    strCard += '|';
                }
            }
            return strCard;
        }
        public List<int> GetRunFastCardList(List<Card> cardList)
        {
            List<int> resultCardList = new List<int>();
            cardList.ForEach(card =>
            {
                resultCardList.Add(((int)card.rank * 10) + (int)card.suit);
            });
            return resultCardList;
        }
        public List<Card> GetRunFastCardList(List<int> cardList)
        {
            List<Card> resultCardList = new List<Card>();
            cardList.ForEach(card =>
            {
                Card node = new Card();
                int rank = card / 10;
                node.rank = (Rank)rank;
                int suit = card % 10;
                node.suit = (Suit)suit;
                resultCardList.Add(node);
            });
            return resultCardList;
        }
        public List<PlayerSaveCardNode> GetRunFastCardList(List<PlayerCardNode> playerInitCardList)
        {
            List<PlayerSaveCardNode> playerSaveCardList = new List<PlayerSaveCardNode>();
            playerInitCardList.ForEach(card =>
            {
                PlayerSaveCardNode node = new PlayerSaveCardNode();
                node.index = card.index;
                node.cardList.AddRange(GetRunFastCardList(card.cardList));
                playerSaveCardList.Add(node);
            });
            return playerSaveCardList;
        }
        public List<PlayerCardNode> GetRunFastCardList(List<PlayerSaveCardNode> playerInitCardList)
        {
            List<PlayerCardNode> playerSaveCardList = new List<PlayerCardNode>();
            playerInitCardList.ForEach(card =>
            {
                PlayerCardNode node = new PlayerCardNode();
                node.index = card.index;
                node.cardList.AddRange(GetRunFastCardList(card.cardList));
                playerSaveCardList.Add(node);
            });
            return playerSaveCardList;
        }
        public void ProcessHouseInfo(int logicId)
        {
            ReplyHouseInfo_D2L replyMsg = new ReplyHouseInfo_D2L();

            List<RunFastHouseDB> runFastHouseList = DBManager<RunFastHouseDB>.Instance.GetRecordsInCache(element => (element.logicId == logicId && element.houseType == HouseType.RunFastHouse && element.houseStatus >= RunFastHouseStatus.RFHS_FreeBureau && element.houseStatus < RunFastHouseStatus.RFHS_Dissolved));
            if (runFastHouseList != null && runFastHouseList.Count > 0)
            {
                List<RunFastHouseNode> houseList = new List<RunFastHouseNode>();
                runFastHouseList.ForEach(runFastHouseDB =>
                {
                    //if (runFastHouseDB.businessId == 0 && !(runFastHouseDB.currentBureau == 0 && runFastHouseDB.houseStatus == RunFastHouseStatus.RFHS_FreeBureau && runFastHouseDB.curPlayerNum >= runFastHouseDB.maxPlayerNum))
                    if (runFastHouseDB.businessId == 0)
                    {
                        RunFastHouseNode houseNode = new RunFastHouseNode();
                        houseNode.houseId = runFastHouseDB.houseId;
                        houseNode.houseCardId = runFastHouseDB.houseCardId;
                        houseNode.logicId = runFastHouseDB.logicId;
                        houseNode.currentBureau = runFastHouseDB.currentBureau;
                        houseNode.maxBureau = runFastHouseDB.maxBureau;
                        houseNode.maxPlayerNum = runFastHouseDB.maxPlayerNum;
                        houseNode.businessId = runFastHouseDB.businessId;
                        houseNode.housePropertyType = runFastHouseDB.housePropertyType;
                        houseNode.zhuangPlayerIndex = runFastHouseDB.zhuangPlayerIndex;
                        houseNode.houseType = runFastHouseDB.houseType;
                        houseNode.runFastType = runFastHouseDB.runFastType;
                        houseNode.houseStatus = runFastHouseDB.houseStatus;
                        houseNode.createTime = runFastHouseDB.createTime.ToString();

                        houseList.Add(houseNode);
                    }
                    else
                    {
                        //比赛场的房间直接解散掉
                        runFastHouseDB.endTime = DateTime.Now;
                        runFastHouseDB.houseStatus = RunFastHouseStatus.RFHS_Dissolved;
                        DBManager<RunFastHouseDB>.Instance.UpdateRecordInCache(runFastHouseDB, e => e.houseId == runFastHouseDB.houseId, true, false);
                    }
                });
                replyMsg.house = Serializer.tryCompressObject(houseList);
            }
            msg_proxy.SendLogicMsg(replyMsg, logicId);

            //做完逻辑
            ModuleManager.Get<DistributedMain>().AddCurrentExeLogic();
        }
        public void ProcessClearHouse(int logicId)
        {
            List<RunFastHouseDB> runFastHouseList = DBManager<RunFastHouseDB>.Instance.GetRecordsInCache(element => (element.logicId == logicId && element.houseType == HouseType.RunFastHouse &&
                element.houseStatus >= RunFastHouseStatus.RFHS_FreeBureau && element.houseStatus < RunFastHouseStatus.RFHS_Dissolved && element.businessId > 0));
                //(element.businessId > 0 || (element.currentBureau == 0 && element.houseStatus == RunFastHouseStatus.RFHS_FreeBureau && element.curPlayerNum >= element.maxPlayerNum))));
            if (runFastHouseList != null && runFastHouseList.Count > 0)
            {
                runFastHouseList.ForEach(runFastHouseDB =>
                {
                    //比赛场的房间直接解散掉
                    runFastHouseDB.endTime = DateTime.Now;
                    runFastHouseDB.houseStatus = RunFastHouseStatus.RFHS_Dissolved;
                    DBManager<RunFastHouseDB>.Instance.UpdateRecordInCache(runFastHouseDB, e => e.houseId == runFastHouseDB.houseId, false, false);
                });
            }

            //做完逻辑
            ModuleManager.Get<DistributedMain>().AddCurrentExeLogic();
        }
    }
}
#endif