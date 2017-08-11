#if MAHJONG
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.Mahjong;
using LegendServer.Util;
using LegendServerDB.Core;
using LegendServerDB.Distributed;
using LegendServerDB.MainCity;
using System;
using System.Collections.Generic;

namespace LegendServerDB.Mahjong
{
    public class MahjongMain : Module
    {
        public MahjongMsgProxy msg_proxy;
        public int getTheOverallRecordNumber;
        public double getTheOverallRecordTime;
        public double roomInformationRetentionTime;
        public double zombieHouseRetentionTime;
        public MahjongMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new MahjongMsgProxy(this);
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
            MsgFactory.Regist(MsgID.L2D_RequestMahjongHouseInfo, new MsgComponent(msg_proxy.OnRequestMahjongHouseInfo, typeof(RequestMahjongHouseInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestMahjongPlayerAndBureau, new MsgComponent(msg_proxy.OnRequestMahjongPlayerAndBureau, typeof(RequestMahjongPlayerAndBureau_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveMahjongNewPlayer, new MsgComponent(msg_proxy.OnRequestSaveMahjongNewPlayer, typeof(RequestSaveMahjongNewPlayer_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveCreateMahjongInfo, new MsgComponent(msg_proxy.OnRequestSaveCreateMahjongInfo, typeof(RequestSaveCreateMahjongInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestDelMahjongHousePlayer, new MsgComponent(msg_proxy.OnRequestDelMahjongHousePlayer, typeof(RequestDelMahjongHousePlayer_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveMahjongHouseStatus, new MsgComponent(msg_proxy.OnRequestSaveMahjongHouseStatus, typeof(RequestSaveMahjongHouseStatus_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveMahjongBureauInfo, new MsgComponent(msg_proxy.OnRequestSaveMahjongBureauInfo, typeof(RequestSaveMahjongBureauInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveMahjongRecord, new MsgComponent(msg_proxy.OnRequestSaveMahjongRecord, typeof(RequestSaveMahjongRecord_L2D)));            
            MsgFactory.Regist(MsgID.L2D_RequestSaveMahjongPlayerSettlement, new MsgComponent(msg_proxy.OnRequestSaveMahjongPlayerSettlement, typeof(RequestSaveMahjongPlayerSettlement_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveMahjongBureauIntegral, new MsgComponent(msg_proxy.OnRequestSaveMahjongBureauIntegral, typeof(RequestSaveMahjongBureauIntegral_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestSaveDissolveMahjongInfo, new MsgComponent(msg_proxy.OnRequestSaveDissolveMahjongInfo, typeof(RequestSaveDissolveMahjongInfo_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestMahjongOverallRecord, new MsgComponent(msg_proxy.OnRequestMahjongOverallRecord, typeof(RequestMahjongOverallRecord_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestMahjongBureauRecord, new MsgComponent(msg_proxy.OnRequestMahjongBureauRecord, typeof(RequestMahjongBureauRecord_L2D)));
            MsgFactory.Regist(MsgID.L2D_RequestMahjongBureauPlayback, new MsgComponent(msg_proxy.OnRequestMahjongBureauPlayback, typeof(RequestMahjongBureauPlayback_L2D)));
        }
        public override void OnRegistTimer()
        {
            //只让一个DBServer做定时器即可
            TimerManager.Instance.Regist(TimerId.HouseCheck, 0, 60 * 60 * 1000, int.MaxValue, OnMahjongHouseTimer, null, null, null);
        }
        public override void OnStart()
        {
        }
        public override void OnDestroy()
        {
        }
        private void OnMahjongHouseTimer(object obj)
        {
            if (ModuleManager.Get<DistributedMain>().ServerStatus != ServerInternalStatus.Running) return;

            DateTime nowTime = DateTime.Now;
            List<MahjongHouseDB> mahjongHouseList = DBManager<MahjongHouseDB>.Instance.GetRecordsInCache(element => (nowTime - element.createTime).TotalDays >= zombieHouseRetentionTime || (element.houseStatus >= MahjongHouseStatus.MHS_Dissolved && (nowTime - element.createTime).TotalDays >= roomInformationRetentionTime));
            if (mahjongHouseList == null || mahjongHouseList.Count <= 0)
            {
                return;
            }
            List<ulong> delHouseId = new List<ulong>();
            List<ulong> delZombieHouseId = new List<ulong>();
            foreach (MahjongHouseDB mahjongHouseDB in mahjongHouseList)
            {
                //只处理映射到我自己DB服务器的逻辑服务器中的房间
                if (msg_proxy.root.AllLogicMappingMe.Exists(e => e == mahjongHouseDB.logicId))
                {
                    delHouseId.Add(mahjongHouseDB.houseId);
                    if (mahjongHouseDB.houseStatus < MahjongHouseStatus.MHS_Dissolved)
                    {
                        delZombieHouseId.Add(mahjongHouseDB.houseId);
                    }
                }
            }
            foreach (ulong houseId in delHouseId)
            {
                DBManager<MahjongHouseDB>.Instance.DeleteRecordInCache(element => element.houseId == houseId);
                DBManager<MahjongPlayerDB>.Instance.DeleteRecordInCache(element => element.houseId == houseId);
                DBManager<MahjongBureauDB>.Instance.DeleteRecordInCache(element => element.houseId == houseId);
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
                TimerManager.Instance.Regist(TimerId.HouseCheck, 0, 30 * 60 * 1000, int.MaxValue, OnMahjongHouseTimer, null, null, null);
            }
        }
        private void OnRequestDelZombieHouse(List<ulong> delZombieHouseId)
        {
            ModuleManager.Get<MainCityMain>().OnRequestDelZombieHouse(delZombieHouseId);
        }
        public void ProcessHouseInfo(int logicId)
        {
            ReplyMahjongHouseInfo_D2L replyMsg = new ReplyMahjongHouseInfo_D2L();

            List<MahjongHouseDB> mahjongHouseList = DBManager<MahjongHouseDB>.Instance.GetRecordsInCache(element => (element.logicId == logicId && element.houseType == HouseType.MahjongHouse && element.houseStatus >= MahjongHouseStatus.MHS_FreeBureau && element.houseStatus < MahjongHouseStatus.MHS_Dissolved));
            if (mahjongHouseList != null && mahjongHouseList.Count > 0)
            {
                List<MahjongHouseNode> houseList = new List<MahjongHouseNode>();
                mahjongHouseList.ForEach(mahjongHouseDB =>
                {
                    //if (mahjongHouseDB.businessId == 0 && !(mahjongHouseDB.currentBureau == 0 && mahjongHouseDB.houseStatus == MahjongHouseStatus.MHS_FreeBureau && mahjongHouseDB.curPlayerNum >= mahjongHouseDB.maxPlayerNum))
                    if (mahjongHouseDB.businessId == 0)
                    {
                        MahjongHouseNode houseNode = new MahjongHouseNode();
                        houseNode.houseId = mahjongHouseDB.houseId;
                        houseNode.houseCardId = mahjongHouseDB.houseCardId;
                        houseNode.logicId = mahjongHouseDB.logicId;
                        houseNode.currentBureau = mahjongHouseDB.currentBureau;
                        houseNode.maxBureau = mahjongHouseDB.maxBureau;
                        houseNode.maxPlayerNum = mahjongHouseDB.maxPlayerNum;
                        houseNode.businessId = mahjongHouseDB.businessId;
                        houseNode.catchBird = mahjongHouseDB.catchBird;
                        houseNode.flutter = mahjongHouseDB.flutter;
                        houseNode.housePropertyType = mahjongHouseDB.housePropertyType;
                        houseNode.houseType = mahjongHouseDB.houseType;
                        houseNode.mahjongType = mahjongHouseDB.mahjongType;
                        houseNode.houseStatus = mahjongHouseDB.houseStatus;
                        houseNode.createTime = mahjongHouseDB.createTime.ToString();

                        houseList.Add(houseNode);
                    }
                    else
                    {
                        //比赛场的房间直接解散掉
                        mahjongHouseDB.endTime = DateTime.Now;
                        mahjongHouseDB.houseStatus = MahjongHouseStatus.MHS_Dissolved;
                        DBManager<MahjongHouseDB>.Instance.UpdateRecordInCache(mahjongHouseDB, e => e.houseId == mahjongHouseDB.houseId, true, false);
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
            List<MahjongHouseDB> mahjongHouseList = DBManager<MahjongHouseDB>.Instance.GetRecordsInCache(element => (element.logicId == logicId && element.houseType == HouseType.MahjongHouse &&
                element.houseStatus >= MahjongHouseStatus.MHS_FreeBureau && element.houseStatus < MahjongHouseStatus.MHS_Dissolved && element.businessId > 0));
                //(element.businessId > 0 || (element.currentBureau == 0 && element.houseStatus == MahjongHouseStatus.MHS_FreeBureau && element.curPlayerNum >= element.maxPlayerNum))));
            if (mahjongHouseList != null && mahjongHouseList.Count > 0)
            {
                mahjongHouseList.ForEach(mahjongHouseDB =>
                {
                    //比赛场的房间直接解散掉
                    mahjongHouseDB.endTime = DateTime.Now;
                    mahjongHouseDB.houseStatus = MahjongHouseStatus.MHS_Dissolved;
                    DBManager<MahjongHouseDB>.Instance.UpdateRecordInCache(mahjongHouseDB, e => e.houseId == mahjongHouseDB.houseId, false, false);
                });
            }

            //做完逻辑
            ModuleManager.Get<DistributedMain>().AddCurrentExeLogic();
        }
    }
}
#endif