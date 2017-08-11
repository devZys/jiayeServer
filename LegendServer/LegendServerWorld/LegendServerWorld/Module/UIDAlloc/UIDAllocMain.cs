using LegendProtocol;
using LegendServer.Database;
using System.Collections.Generic;
using LegendServer.Database.House;

namespace LegendServerWorld.UIDAlloc
{
    public class UIDAllocMain : Module
    {
        public UIDAllocMsgProxy msg_proxy;
        private List<int> houseCardIdList = new List<int>();//当前可以被分配的房间ID
        public Dictionary<int, int> HouseIdToLogicId = new Dictionary<int, int>();//房间到逻辑服务器的映射
        public UIDAllocMain(object root)
            : base(root)
        {
        }
        public override void OnCreate()
        {
            msg_proxy = new UIDAllocMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.X2X_RequestUID, new MsgComponent(msg_proxy.OnRequestUID, typeof(RequestUID_X2X)));
            MsgFactory.Regist(MsgID.X2X_NotifyRecycleUID, new MsgComponent(msg_proxy.OnNotifyRecycleUID, typeof(NotifyRecycleUID_X2X)));
            MsgFactory.Regist(MsgID.L2W_RequestHouseBelong, new MsgComponent(msg_proxy.OnRequestHouseBelong, typeof(RequestHouseBelong_L2W)));
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
        public void BuildRoomIDPool()
        {
            ServerUtil.RecordLog(LogType.Info, "开始生成房间ID池....");
            //生成
            for (int i = 100000; i < 1000000; ++i)
            {
                houseCardIdList.Add(i);
            }
            //去重
#if MAHJONG
            List<MahjongHouseDB> houseList = DBManager<MahjongHouseDB>.Instance.GetRecordsInCache(e => e.houseStatus < MahjongHouseStatus.MHS_Dissolved && e.businessId == 0);
                //e.businessId == 0 && !(e.currentBureau == 0 && e.houseStatus == MahjongHouseStatus.MHS_FreeBureau && e.curPlayerNum >= e.maxPlayerNum));
            if (houseList != null && houseList.Count > 0)
            {
                houseList.ForEach(e => 
                {
                    houseCardIdList.Remove(e.houseCardId);

                    //做房间到逻辑服务器的映射
                    HouseIdToLogicId[e.houseCardId] = e.logicId;
                });
            }
            //清理缓存
            DBManager<MahjongHouseDB>.Instance.ClearRecordInCache();
#elif RUNFAST
            List<RunFastHouseDB> houseList = DBManager<RunFastHouseDB>.Instance.GetRecordsInCache(e => e.houseStatus < RunFastHouseStatus.RFHS_Dissolved && e.businessId == 0);
                //e.businessId == 0 && !(e.currentBureau == 0 && e.houseStatus == RunFastHouseStatus.RFHS_FreeBureau && e.curPlayerNum >= e.maxPlayerNum));
            if (houseList != null && houseList.Count > 0)
            {
                houseList.ForEach(e =>
                {
                    houseCardIdList.Remove(e.houseCardId);

                    //做房间到逻辑服务器的映射
                    HouseIdToLogicId[e.houseCardId] = e.logicId;
                });
            }
            //清理缓存
            DBManager<RunFastHouseDB>.Instance.ClearRecordInCache();
#elif WORDPLATE
            List<WordPlateHouseDB> houseList = DBManager<WordPlateHouseDB>.Instance.GetRecordsInCache(e => e.houseStatus < WordPlateHouseStatus.EWPS_Dissolved && e.businessId == 0);
                //e.businessId == 0 && !(e.currentBureau == 0 && e.houseStatus == WordPlateHouseStatus.EWPS_FreeBureau && e.curPlayerNum >= e.maxPlayerNum));
            if (houseList != null && houseList.Count > 0)
            {
                houseList.ForEach(e =>
                {
                    houseCardIdList.Remove(e.houseCardId);

                    //做房间到逻辑服务器的映射
                    HouseIdToLogicId[e.houseCardId] = e.logicId;
                });
            }
            //清理缓存
            DBManager<WordPlateHouseDB>.Instance.ClearRecordInCache();
#endif
            ServerUtil.RecordLog(LogType.Info, "房间ID池生成完毕！");
        }
        //归还房间ID
        public void RevertHouseCardId(int houseCardId)
        {
            if (houseCardId > 0 && !houseCardIdList.Contains(houseCardId))
            {
                houseCardIdList.Add(houseCardId);

                //去掉房间与逻辑服务器的映射
                HouseIdToLogicId.Remove(houseCardId);
            }
        }
        //获取房间ID
        public ulong GetHouseCardId(int requestServerID)
        {
            if (houseCardIdList.Count == 0)
            {
                //没有了
                return 0;
            }
            int index = MyRandom.NextPrecise(0, houseCardIdList.Count);
            int houseCardId = houseCardIdList[index];
            houseCardIdList.Remove(houseCardId);			
			
            //做房间与逻辑服务器的映射
            HouseIdToLogicId[houseCardId] = requestServerID;

            return (ulong)houseCardId;
        }
    }
}