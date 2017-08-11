using System;
using LegendProtocol;
using LegendServerLogic.Actor.Summoner;
using LegendServerLogic.Core;
using LegendServer.Util;
using LegendServerLogic.Distributed;
using LegendServer.Database.Config;
using LegendServer.Database;
using System.Text;
using LegendServerLogic.SpecialActivities;
using LegendServerLogic.MainCity;
#if MAHJONG
using LegendServerLogic.Mahjong;
#elif RUNFAST
using LegendServerLogic.RunFast;
#elif WORDPLATE
using LegendServerLogic.WordPlate;
#endif

namespace LegendServerLogic.ServiceBox
{
    public class ServiceBoxMsgProxy : ServerMsgProxy
    {
        private ServiceBoxMain main;

        public ServiceBoxMsgProxy(ServiceBoxMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestTestCalcLogic(int peerId, bool inbound, object msg)
        {
            RequestTestCalcLogic_P2L reqMsg_P2L = msg as RequestTestCalcLogic_P2L;            

            ReplyTestCalcLogic_L2P replyMsg_L2P = new ReplyTestCalcLogic_L2P();
            replyMsg_L2P.userId = reqMsg_P2L.userId;

            double result = 0.0;
            for (int index = 0; index < reqMsg_P2L.param1; index++)
            {
                result++;
            }
            replyMsg_L2P.result = result;
            SendMsg(peerId, replyMsg_L2P);
        }

        public void OnRequestTestDB(int peerId, bool inbound, object msg)
        {
            RequestTestDB_P2L reqMsg_P2L = msg as RequestTestDB_P2L;

            RequestTestDB_L2D reqMsg_L2D = new RequestTestDB_L2D();
            reqMsg_L2D.userId = reqMsg_P2L.userId;
            reqMsg_L2D.operate = reqMsg_P2L.operate;
            reqMsg_L2D.strategy = reqMsg_P2L.strategy;
            reqMsg_L2D.loop = reqMsg_P2L.loop;
            SendDBMsg(reqMsg_L2D);
        }

        public void OnReplyTestDB(int peerId, bool inbound, object msg)
        {
            ReplyTestDB_D2L replyMsg_D2L = msg as ReplyTestDB_D2L;

            Summoner request = SummonerManager.Instance.GetSummonerByUserId(replyMsg_D2L.userId);
            if (request == null) return;

            ReplyTestDB_L2P replyMsg_L2P = new ReplyTestDB_L2P();
            replyMsg_L2P.userId = replyMsg_D2L.userId;
            replyMsg_L2P.result = replyMsg_D2L.result;
            SendProxyMsg(replyMsg_L2P, request.proxyServerId);
        }
        public void OnRequestTestDBCacheSync(int peerId, bool inbound, object msg)
        {
            RequestTestDBCacheSync_P2L reqMsg_P2L = msg as RequestTestDBCacheSync_P2L;

            RequestTestDBCacheSync_L2D reqMsg_L2D = new RequestTestDBCacheSync_L2D();
            reqMsg_L2D.userId = reqMsg_P2L.userId;
            reqMsg_L2D.data = reqMsg_P2L.data;
            SendMsg("db", 1, reqMsg_L2D);
        }
        public void OnReplyTestDBCacheSync(int peerId, bool inbound, object msg)
        {
            ReplyTestDBCacheSync_D2L replyMsg_D2L = msg as ReplyTestDBCacheSync_D2L;

            Summoner request = SummonerManager.Instance.GetSummonerByUserId(replyMsg_D2L.userId);
            if (request == null) return;

            ReplyTestDBCacheSync_L2P replyMsg_L2P = new ReplyTestDBCacheSync_L2P();
            replyMsg_L2P.userId = replyMsg_D2L.userId;
            replyMsg_L2P.data = replyMsg_D2L.data;
            SendProxyMsg(replyMsg_L2P, request.proxyServerId);
        }
        public void OnRequestGetDBCacheData(int peerId, bool inbound, object msg)
        {
            RequestGetDBCacheData_P2L reqMsg_P2L = msg as RequestGetDBCacheData_P2L;

            RequestGetDBCacheData_L2D reqMsg_L2D = new RequestGetDBCacheData_L2D();
            reqMsg_L2D.userId = reqMsg_P2L.userId;
            reqMsg_L2D.dbServerId = reqMsg_P2L.dbServerId;
            reqMsg_L2D.guid = reqMsg_P2L.guid;
            SendMsg("db", reqMsg_P2L.dbServerId, reqMsg_L2D);
        }
        public void OnReplyGetDBCacheData(int peerId, bool inbound, object msg)
        {
            ReplyGetDBCacheData_D2L replyMsg_D2L = msg as ReplyGetDBCacheData_D2L;

            Summoner request = SummonerManager.Instance.GetSummonerByUserId(replyMsg_D2L.userId);
            if (request == null) return;

            ReplyGetDBCacheData_L2P replyMsg_L2P = new ReplyGetDBCacheData_L2P();
            replyMsg_L2P.userId = replyMsg_D2L.userId;
            replyMsg_L2P.dbServerId = replyMsg_D2L.dbServerId;
            replyMsg_L2P.paramTotal = replyMsg_D2L.paramTotal;
            SendProxyMsg(replyMsg_L2P, request.proxyServerId);
        }
        public void OnRequestCreateHouse(int peerId, bool inbound, object msg)
        {
            RequestCreateHouse_P2L reqMsg_P2L = msg as RequestCreateHouse_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg_P2L.summonerId);
            if (sender == null)
            {
                //玩家查找有误
                ServerUtil.RecordLog(LogType.Error, "OnRequestCreateHouse, 玩家查找有误! requesterSummonerId = " + reqMsg_P2L.summonerId);
                return;
            }
#if MAHJONG
            //创建房间
            ModuleManager.Get<MahjongMain>().msg_proxy.OnReqCreateMahjongHouse(sender, ModuleManager.Get<SpecialActivitiesMain>().mahjongMaxPlayerNum,
                ModuleManager.Get<SpecialActivitiesMain>().mahjongMaxBureau, ModuleManager.Get<SpecialActivitiesMain>().mahjongType,
                (int)ModuleManager.Get<SpecialActivitiesMain>().housePropertyType, ModuleManager.Get<SpecialActivitiesMain>().catchBird, reqMsg_P2L.marketId);
#elif RUNFAST
            //创建房间（跑得快经典玩法）
            ModuleManager.Get<RunFastMain>().msg_proxy.OnReqCreateRunFastHouse(sender, ModuleManager.Get<SpecialActivitiesMain>().runFastMaxPlayerNum, 
                ModuleManager.Get<SpecialActivitiesMain>().runFastMaxBureau, ModuleManager.Get<SpecialActivitiesMain>().runFastType,
                (int)ModuleManager.Get<SpecialActivitiesMain>().housePropertyType, reqMsg_P2L.marketId);
#elif WORDPLATE
            //创建房间
            ModuleManager.Get<WordPlateMain>().msg_proxy.OnReqCreateWordPlateHouse(sender, ModuleManager.Get<SpecialActivitiesMain>().maxWinScore, 
                ModuleManager.Get<SpecialActivitiesMain>().wordPlateMaxBureau, ModuleManager.Get<SpecialActivitiesMain>().wordPlateType, 
                (int)ModuleManager.Get<SpecialActivitiesMain>().housePropertyType, ModuleManager.Get<SpecialActivitiesMain>().baseWinScore, reqMsg_P2L.marketId);
#endif
        }
        public void OnRequestJoinHouse(int peerId, bool inbound, object msg)
        {
            RequestJoinHouse_P2L reqMsg_P2L = msg as RequestJoinHouse_P2L;

            Summoner sender = SummonerManager.Instance.GetSummonerById(reqMsg_P2L.summonerId);
            if (sender == null)
            {
                //玩家查找有误
                ServerUtil.RecordLog(LogType.Error, "OnRequestJoinHouse, 玩家查找有误! requesterSummonerId = " + reqMsg_P2L.summonerId);
                return;
            }
#if MAHJONG
            //加入房间
            ModuleManager.Get<MahjongMain>().msg_proxy.OnReqJoinMahjongHouse(sender, reqMsg_P2L.houseId);
#elif RUNFAST
            //加入房间
            ModuleManager.Get<RunFastMain>().msg_proxy.OnReqJoinRunFastHouse(sender, reqMsg_P2L.houseId);
#elif WORDPLATE
            //加入房间
            ModuleManager.Get<WordPlateMain>().msg_proxy.OnReqJoinWordPlateHouse(sender, reqMsg_P2L.houseId);
#endif
        }
        public void OnRecvHouseEndSettlement(ulong summonerId, int proxyServerId, int houseId)
        {
            RecvHouseEndSettlement_L2P recvMsg = new RecvHouseEndSettlement_L2P();
            recvMsg.summonerId = summonerId;
            recvMsg.houseId = houseId;
            SendProxyMsg(recvMsg, proxyServerId);
        }
    }
}

