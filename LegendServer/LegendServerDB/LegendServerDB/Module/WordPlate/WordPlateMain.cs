#if WORDPLATE
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.WordPlate;
using LegendServer.Util;
using LegendServerDB.Distributed;
using LegendServerDB.MainCity;
using System;
using System.Collections.Generic;

namespace LegendServerDB.WordPlate
{
    public class WordPlateMain : Module
    {
        public WordPlateMsgProxy msg_proxy;
        public int getTheOverallRecordNumber;
        public double getTheOverallRecordTime;
        public double roomInformationRetentionTime;
        public double zombieHouseRetentionTime;
        public WordPlateMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new WordPlateMsgProxy(this);
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
            MsgFactory.Regist(MsgID.L2D_RequestWordPlateHouseInfo, new MsgComponent(msg_proxy.OnRequestWordPlateHouseInfo, typeof(RequestWordPlateHouseInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestWordPlatePlayerAndBureau, new MsgComponent(msg_proxy.OnRequestWordPlatePlayerAndBureau, typeof(RequestWordPlatePlayerAndBureau_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveWordPlateNewPlayer, new MsgComponent(msg_proxy.OnRequestSaveWordPlateNewPlayer, typeof(RequestSaveWordPlateNewPlayer_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveCreateWordPlateInfo, new MsgComponent(msg_proxy.OnRequestSaveCreateWordPlateInfo, typeof(RequestSaveCreateWordPlateInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestDelWordPlateHousePlayer, new MsgComponent(msg_proxy.OnRequestDelWordPlateHousePlayer, typeof(RequestDelWordPlateHousePlayer_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveWordPlateHouseStatus, new MsgComponent(msg_proxy.OnRequestSaveWordPlateHouseStatus, typeof(RequestSaveWordPlateHouseStatus_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveWordPlateBureauInfo, new MsgComponent(msg_proxy.OnRequestSaveWordPlateBureauInfo, typeof(RequestSaveWordPlateBureauInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveWordPlatePlayerSettlement, new MsgComponent(msg_proxy.OnRequestSaveWordPlatePlayerSettlement, typeof(RequestSaveWordPlatePlayerSettlement_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveWordPlateBureauIntegral, new MsgComponent(msg_proxy.OnRequestSaveWordPlateBureauIntegral, typeof(RequestSaveWordPlateBureauIntegral_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveWordPlateRecord, new MsgComponent(msg_proxy.OnRequestSaveWordPlateRecord, typeof(RequestSaveWordPlateRecord_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestWordPlateOverallRecord, new MsgComponent(msg_proxy.OnRequestWordPlateOverallRecord, typeof(RequestWordPlateOverallRecord_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestWordPlateBureauRecord, new MsgComponent(msg_proxy.OnRequestWordPlateBureauRecord, typeof(RequestWordPlateBureauRecord_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestWordPlateBureauPlayback, new MsgComponent(msg_proxy.OnRequestWordPlateBureauPlayback, typeof(RequestWordPlateBureauPlayback_L2D)));
        }
        public override void OnRegistTimer()
        {
            TimerManager.Instance.Regist(TimerId.HouseCheck, 0, 60 * 60 * 1000, int.MaxValue, OnWordPlateHouseTimer, null, null, null);
        }
        public override void OnStart()
        {
        }
        public override void OnDestroy()
        {
        }
        private void OnWordPlateHouseTimer(object obj)
        {
            if (ModuleManager.Get<DistributedMain>().ServerStatus != ServerInternalStatus.Running) return;

            DateTime nowTime = DateTime.Now;
            List<WordPlateHouseDB> wordPlateHouseList = DBManager<WordPlateHouseDB>.Instance.GetRecordsInCache(element => (nowTime - element.createTime).TotalDays >= zombieHouseRetentionTime || (element.houseStatus >= WordPlateHouseStatus.EWPS_Dissolved && (nowTime - element.createTime).TotalDays >= roomInformationRetentionTime));
            if (wordPlateHouseList == null || wordPlateHouseList.Count <= 0)
            {
                return;
            }
            List<ulong> delHouseId = new List<ulong>();
            List<ulong> delZombieHouseId = new List<ulong>();
            foreach (WordPlateHouseDB wordPlateHouseDB in wordPlateHouseList)
            {
                //只处理映射到我自己DB服务器的逻辑服务器中的房间
                if (msg_proxy.root.AllLogicMappingMe.Exists(e => e == wordPlateHouseDB.logicId))
                {
                    delHouseId.Add(wordPlateHouseDB.houseId);
                    if (wordPlateHouseDB.houseStatus < WordPlateHouseStatus.EWPS_Dissolved)
                    {
                        delZombieHouseId.Add(wordPlateHouseDB.houseId);
                    }
                }
            }
            foreach (ulong houseId in delHouseId)
            {
                DBManager<WordPlateHouseDB>.Instance.DeleteRecordInCache(element => element.houseId == houseId);
                DBManager<WordPlatePlayerDB>.Instance.DeleteRecordInCache(element => element.houseId == houseId);
                DBManager<WordPlateBureauDB>.Instance.DeleteRecordInCache(element => element.houseId == houseId);
            }
            if (delZombieHouseId.Count > 0)
            {
                //删除一个月以上僵尸房
                OnRequestDelZombieHouse(delZombieHouseId);
            }
        }
        public void CheckRegistTimer()
        {
            if (!TimerManager.Instance.Exist(TimerId.HouseCheck))
            {
                TimerManager.Instance.Regist(TimerId.HouseCheck, 0, 30 * 60 * 1000, int.MaxValue, OnWordPlateHouseTimer, null, null, null);
            }
        }
        private void OnRequestDelZombieHouse(List<ulong> delZombieHouseId)
        {
            ModuleManager.Get<MainCityMain>().OnRequestDelZombieHouse(delZombieHouseId);
        }
        public void ProcessHouseInfo(int logicId)
        {
            ReplyWordPlateHouseInfo_D2L replyMsg = new ReplyWordPlateHouseInfo_D2L();

            List<WordPlateHouseDB> wordPlateHouseList = DBManager<WordPlateHouseDB>.Instance.GetRecordsInCache(element => (element.logicId == logicId && element.houseType == HouseType.WordPlateHouse && element.houseStatus >= WordPlateHouseStatus.EWPS_FreeBureau && element.houseStatus < WordPlateHouseStatus.EWPS_Dissolved));
            if (wordPlateHouseList != null && wordPlateHouseList.Count > 0)
            {
                List<WordPlateHouseNode> houseList = new List<WordPlateHouseNode>();
                wordPlateHouseList.ForEach(wordPlateHouseDB =>
                {
                    //if (wordPlateHouseDB.businessId == 0 && !(wordPlateHouseDB.currentBureau == 0 && wordPlateHouseDB.houseStatus == WordPlateHouseStatus.EWPS_FreeBureau && wordPlateHouseDB.curPlayerNum >= wordPlateHouseDB.maxPlayerNum))
                    if (wordPlateHouseDB.businessId == 0)                    
                    {
                        WordPlateHouseNode houseNode = new WordPlateHouseNode();
                        houseNode.houseId = wordPlateHouseDB.houseId;
                        houseNode.houseCardId = wordPlateHouseDB.houseCardId;
                        houseNode.logicId = wordPlateHouseDB.logicId;
                        houseNode.currentBureau = wordPlateHouseDB.currentBureau;
                        houseNode.maxBureau = wordPlateHouseDB.maxBureau;
                        houseNode.maxWinScore = wordPlateHouseDB.maxWinScore;
                        houseNode.maxPlayerNum = wordPlateHouseDB.maxPlayerNum;
                        houseNode.businessId = wordPlateHouseDB.businessId;
                        houseNode.baseWinScore = wordPlateHouseDB.baseWinScore;
                        houseNode.beginGodType = wordPlateHouseDB.beginGodType;
                        houseNode.housePropertyType = wordPlateHouseDB.housePropertyType;
                        houseNode.houseType = wordPlateHouseDB.houseType;
                        houseNode.wordPlateType = wordPlateHouseDB.wordPlateType;
                        houseNode.houseStatus = wordPlateHouseDB.houseStatus;
                        houseNode.createTime = wordPlateHouseDB.createTime.ToString();

                        houseList.Add(houseNode);
                    }
                    else
                    {
                        //比赛场的房间直接解散掉
                        wordPlateHouseDB.endTime = DateTime.Now;
                        wordPlateHouseDB.houseStatus = WordPlateHouseStatus.EWPS_Dissolved;
                        //保存数据库
                        DBManager<WordPlateHouseDB>.Instance.UpdateRecordInCache(wordPlateHouseDB, e => e.houseId == wordPlateHouseDB.houseId, true, false);
                    }
                });
                replyMsg.dbServerID = msg_proxy.root.ServerID;
                replyMsg.house = Serializer.tryCompressObject(houseList);
            }
            msg_proxy.SendLogicMsg(replyMsg, logicId);

            //做完逻辑
            ModuleManager.Get<DistributedMain>().AddCurrentExeLogic();
        }
        public void ProcessClearHouse(int logicId)
        {
            List<WordPlateHouseDB> wordPlateHouseList = DBManager<WordPlateHouseDB>.Instance.GetRecordsInCache(element => (element.logicId == logicId && element.houseType == HouseType.WordPlateHouse &&
                element.houseStatus >= WordPlateHouseStatus.EWPS_FreeBureau && element.houseStatus < WordPlateHouseStatus.EWPS_Dissolved && element.businessId > 0));
                //(element.businessId > 0 || (element.currentBureau == 0 && element.houseStatus == WordPlateHouseStatus.EWPS_FreeBureau && element.curPlayerNum >= element.maxPlayerNum))));
            if (wordPlateHouseList != null && wordPlateHouseList.Count > 0)
            {
                wordPlateHouseList.ForEach(wordPlateHouseDB =>
                {
                    //比赛场的房间直接解散掉
                    wordPlateHouseDB.endTime = DateTime.Now;
                    wordPlateHouseDB.houseStatus = WordPlateHouseStatus.EWPS_Dissolved;
                    //保存数据库
                    DBManager<WordPlateHouseDB>.Instance.UpdateRecordInCache(wordPlateHouseDB, e => e.houseId == wordPlateHouseDB.houseId, false, false);
                });
            }

            //做完逻辑
            ModuleManager.Get<DistributedMain>().AddCurrentExeLogic();
        }
    }
}
#endif