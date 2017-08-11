#if RUNFAST
using System.Collections.Generic;
using LegendProtocol;
using LegendServerDB.Core;
using LegendServer.Database;
using LegendServer.Database.Summoner;
using System;
using LegendServer.Database.RunFast;
using System.Linq;
using LegendServerDB.Distributed;
using LegendServerDBDefine;

namespace LegendServerDB.RunFast
{
    public class RunFastMsgProxy : ServerMsgProxy
    {
        private RunFastMain main;

        public RunFastMsgProxy(RunFastMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestSaveCreateRunFastInfo(int peerId, bool inbound, object msg)
        {
            RequestSaveCreateRunFastInfo_L2D reqMsg = msg as RequestSaveCreateRunFastInfo_L2D;

            //保存新房间
            RunFastHouseDB runFastHouseDB = new RunFastHouseDB();
            runFastHouseDB.houseId = reqMsg.houseId;
            runFastHouseDB.houseCardId = reqMsg.houseCardId;
            runFastHouseDB.logicId = reqMsg.logicId;
            runFastHouseDB.currentBureau = 0;
            runFastHouseDB.maxBureau = reqMsg.maxBureau;
            runFastHouseDB.curPlayerNum = 1;
            runFastHouseDB.maxPlayerNum = reqMsg.maxPlayerNum;
            runFastHouseDB.businessId = reqMsg.businessId;
            runFastHouseDB.housePropertyType = reqMsg.housePropertyType;
            runFastHouseDB.zhuangPlayerIndex = 0;
            runFastHouseDB.houseType = reqMsg.houseType;
            runFastHouseDB.runFastType = reqMsg.runFastType;
            runFastHouseDB.houseStatus = RunFastHouseStatus.RFHS_FreeBureau;
            runFastHouseDB.createTime = Convert.ToDateTime(reqMsg.createTime);
            runFastHouseDB.endTime = DateTime.Parse("1970-01-01 00:00:00");
            bool result = DBManager<RunFastHouseDB>.Instance.AddRecordToCache(runFastHouseDB, element => element.houseId == runFastHouseDB.houseId);
            if (!result)
            {
                //创建房间信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveCreateRunFastInfo 创建房间信息失败 houseId = " + runFastHouseDB.houseId);
            }
            //保存房间新玩家
            RunFastPlayerDB runFastPlayerDB = new RunFastPlayerDB();
            runFastPlayerDB.houseId = reqMsg.houseId;
            runFastPlayerDB.summonerId = reqMsg.summonerId;
            runFastPlayerDB.playerIndex = reqMsg.index;
            runFastPlayerDB.bombIntegral = 0;
            runFastPlayerDB.winBureau = 0;
            runFastPlayerDB.loseBureau = 0;
            runFastPlayerDB.allIntegral = reqMsg.allIntegral;
            result = DBManager<RunFastPlayerDB>.Instance.AddRecordToCache(runFastPlayerDB, element => (element.houseId == runFastPlayerDB.houseId && element.summonerId == runFastPlayerDB.summonerId));
            if (!result)
            {
                //创建房间玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveCreateRunFastInfo 创建房间玩家信息失败 houseId = " + runFastPlayerDB.houseId + ", summonerId = " + runFastPlayerDB.summonerId);
            }
            //保存玩家房间id
            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg.summonerId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveCreateRunFastInfo error!! summonerId = " + reqMsg.summonerId);
                return;
            }
            summonerDB.houseId = reqMsg.houseId;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
        }
        public void OnRequestSaveRunFastNewPlayer(int peerId, bool inbound, object msg)
        {
            RequestSaveRunFastNewPlayer_L2D reqMsg = msg as RequestSaveRunFastNewPlayer_L2D;

            //保存房间新玩家
            RunFastPlayerDB runFastPlayerDB = new RunFastPlayerDB();
            runFastPlayerDB.houseId = reqMsg.houseId;
            runFastPlayerDB.summonerId = reqMsg.summonerId;
            runFastPlayerDB.playerIndex = reqMsg.index;
            runFastPlayerDB.bombIntegral = 0;
            runFastPlayerDB.winBureau = 0;
            runFastPlayerDB.loseBureau = 0;
            runFastPlayerDB.allIntegral = reqMsg.allIntegral;
            bool result = DBManager<RunFastPlayerDB>.Instance.AddRecordToCache(runFastPlayerDB, element => (element.houseId == runFastPlayerDB.houseId && element.summonerId == runFastPlayerDB.summonerId));
            if (!result)
            {
                //创建房间玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveRunFastNewPlayer 创建房间玩家信息失败 houseId = " + runFastPlayerDB.houseId + ", summonerId = " + runFastPlayerDB.summonerId);
            }
            //保存玩家房间id
            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg.summonerId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveRunFastNewPlayer error!! summonerId = " + reqMsg.summonerId);
                return;
            }
            summonerDB.houseId = reqMsg.houseId;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
            //保存房间人数
            RunFastHouseDB runFastHouseDB = DBManager<RunFastHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (runFastHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveRunFastNewPlayer error!! runFastHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            runFastHouseDB.curPlayerNum += 1;
            DBManager<RunFastHouseDB>.Instance.UpdateRecordInCache(runFastHouseDB, e => e.houseId == runFastHouseDB.houseId);
        }
        public void OnRequestSaveRunFastHouseStatus(int peerId, bool inbound, object msg)
        {
            RequestSaveRunFastHouseStatus_L2D reqMsg = msg as RequestSaveRunFastHouseStatus_L2D;

            RunFastHouseDB runFastHouseDB = DBManager<RunFastHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (runFastHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseStatusIndex error!! runFastHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            if (runFastHouseDB.houseStatus != reqMsg.houseStatus)
            {
                runFastHouseDB.houseStatus = reqMsg.houseStatus;
                if (runFastHouseDB.houseStatus >= RunFastHouseStatus.RFHS_Dissolved)
                {
                    //房间解散或者结束了
                    runFastHouseDB.endTime = DateTime.Now;
                }
                DBManager<RunFastHouseDB>.Instance.UpdateRecordInCache(runFastHouseDB, e => e.houseId == runFastHouseDB.houseId);
            }
        }
        public void OnRequestDelHousePlayer(int peerId, bool inbound, object msg)
        {
            RequestDelHousePlayer_L2D reqMsg = msg as RequestDelHousePlayer_L2D;

            int count = DBManager<RunFastPlayerDB>.Instance.DeleteRecordInCache(element => (element.houseId == reqMsg.houseId && element.summonerId == reqMsg.summonerId));
            if (count == 0)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelHousePlayer error!!  houseId = " + reqMsg.houseId + ", summonerId = " + reqMsg.summonerId);
            }
            //保存玩家房间id
            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg.summonerId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelHousePlayer error!! summonerId = " + reqMsg.summonerId);
                return;
            }
            summonerDB.houseId = 0;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
            //保存房间人数
            RunFastHouseDB runFastHouseDB = DBManager<RunFastHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (runFastHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelHousePlayer error!! runFastHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            if (runFastHouseDB.curPlayerNum > 0)
            {
                runFastHouseDB.curPlayerNum -= 1;
                DBManager<RunFastHouseDB>.Instance.UpdateRecordInCache(runFastHouseDB, e => e.houseId == runFastHouseDB.houseId);
            }
        }
        public void OnRequestSavePlayerSettlement(int peerId, bool inbound, object msg)
        {
            RequestSaveRunFastPlayerSettlement_L2D reqMsg = msg as RequestSaveRunFastPlayerSettlement_L2D;

            RunFastPlayerDB runFastPlayerDB = DBManager<RunFastPlayerDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.summonerId == reqMsg.summonerId));
            if (runFastPlayerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSavePlayerSettlement error!! runFastPlayerDB == null houseId = " + reqMsg.houseId + ", summonerId = " + reqMsg.summonerId);
                return;
            }
            runFastPlayerDB.winBureau = reqMsg.winBureau;
            runFastPlayerDB.loseBureau = reqMsg.loseBureau;
            runFastPlayerDB.bombIntegral = reqMsg.bombIntegral;
            runFastPlayerDB.allIntegral = reqMsg.allIntegral;
            if (!runFastPlayerDB.bGetRecord)
            {
                runFastPlayerDB.bGetRecord = true;
            }

            DBManager<RunFastPlayerDB>.Instance.UpdateRecordInCache(runFastPlayerDB, e => e.houseId == runFastPlayerDB.houseId && e.summonerId == runFastPlayerDB.summonerId);
        }
        public void OnRequestSaveRunFastBureauInfo(int peerId, bool inbound, object msg)
        {
            RequestSaveRunFastBureauInfo_L2D reqMsg = msg as RequestSaveRunFastBureauInfo_L2D;

            if (reqMsg.bureau == 0 || string.IsNullOrEmpty(reqMsg.bureauTime) || reqMsg.playerInitCard == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseBureauInfo error!! reqMsg.playerInitCard  == null houseId = " + reqMsg.houseId);
                return;
            }
            //当局信息
            bool bInsert = false;
            RunFastBureauDB runFastBureauDB = DBManager<RunFastBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.bureau));
            if (runFastBureauDB == null)
            {
                runFastBureauDB = new RunFastBureauDB();
                runFastBureauDB.houseId = reqMsg.houseId;
                runFastBureauDB.bureau = reqMsg.bureau;
                bInsert = true;
            }
            List<PlayerBureauIntegral> playerBureauList = new List<PlayerBureauIntegral>();
            reqMsg.playerBureauList.ForEach(playerBureau =>
            {
                PlayerBureauIntegral node = new PlayerBureauIntegral();
                node.playerIndex = playerBureau;
                playerBureauList.Add(node);
            });
            runFastBureauDB.playerinfo = Serializer.tryCompressObject(playerBureauList);
            runFastBureauDB.bureauTime = Convert.ToDateTime(reqMsg.bureauTime);

            List<PlayerSaveCardNode> playerInitSaveCardList = Serializer.tryUncompressObject<List<PlayerSaveCardNode>>(reqMsg.playerInitCard);
            runFastBureauDB.playercard = Serializer.tryCompressObject(main.GetRunFastCardList(playerInitSaveCardList));

            List<PlayerCardNode> playerCardList = new List<PlayerCardNode>();
            runFastBureauDB.showcard = Serializer.tryCompressObject(playerCardList);

            if (bInsert)
            {
                bool result = DBManager<RunFastBureauDB>.Instance.AddRecordToCache(runFastBureauDB, element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.bureau));
                if (!result)
                {
                    //创建房间当局信息失败
                    ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseBureauInfo 创建房间当局信息失败 houseId = " + reqMsg.houseId + ", bureau = " + reqMsg.bureau);
                }
            }
            else
            {
                DBManager<RunFastBureauDB>.Instance.UpdateRecordInCache(runFastBureauDB, e => e.houseId == runFastBureauDB.houseId && e.bureau == runFastBureauDB.bureau);
            }
        }
        public void OnRequestSaveBureauIntegral(int peerId, bool inbound, object msg)
        {
            RequestSaveRunFastBureauIntegral_L2D reqMsg = msg as RequestSaveRunFastBureauIntegral_L2D;

            RunFastHouseDB runFastHouseDB = DBManager<RunFastHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (runFastHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveRunFastBureauIntegral error!! runFastHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            RunFastBureauDB runFastBureauDB = DBManager<RunFastBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.bureau));
            if (runFastBureauDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveBureauIntegral error!! runFastBureauDB == null houseId = " + reqMsg.houseId + ", bureau = " + reqMsg.bureau);
                return;
            }
            //房间当前局数
            if (runFastHouseDB.currentBureau != (int)reqMsg.bureau || runFastHouseDB.zhuangPlayerIndex != reqMsg.zhuangPlayerIndex)
            {
                runFastHouseDB.currentBureau = (int)reqMsg.bureau;
                runFastHouseDB.zhuangPlayerIndex = reqMsg.zhuangPlayerIndex;
                DBManager<RunFastHouseDB>.Instance.UpdateRecordInCache(runFastHouseDB, e => e.houseId == runFastHouseDB.houseId);
            }
            //局数
            if (reqMsg.bureauIntegralList.Count > 0)
            {
                runFastBureauDB.playerinfo = Serializer.tryCompressObject(reqMsg.bureauIntegralList);
                DBManager<RunFastBureauDB>.Instance.UpdateRecordInCache(runFastBureauDB, e => e.houseId == runFastBureauDB.houseId && e.bureau == runFastBureauDB.bureau);
            }
        }
        public void OnRequestSaveBureauShowCard(int peerId, bool inbound, object msg)
        {
            RequestSaveBureauShowCard_L2D reqMsg = msg as RequestSaveBureauShowCard_L2D;

            RunFastBureauDB runFastBureauDB = DBManager<RunFastBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.bureau));
            if (runFastBureauDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveBureauShowCard error!! runFastBureauDB == null houseId = " + reqMsg.houseId + ", bureau = " + reqMsg.bureau);
                return;
            }
            if (reqMsg.playerCard != null)
            {
                List<PlayerCardNode> showCardList = Serializer.tryUncompressObject<List<PlayerCardNode>>(runFastBureauDB.showcard);
                if (showCardList != null)
                {
                    showCardList.Add(reqMsg.playerCard);
                    runFastBureauDB.showcard = Serializer.tryCompressObject(showCardList);
                    //DBManager<RunFastBureauDB>.Instance.UpdateRecordInCache(runFastBureauDB, e => e.houseId == runFastBureauDB.houseId && e.bureau == runFastBureauDB.bureau);
                }
            }
        }
        public void OnRequestHouseInfo(int peerId, bool inbound, object msg)
        {
            RequestHouseInfo_L2D reqMsg = msg as RequestHouseInfo_L2D;

            if (!ModuleManager.Get<DistributedMain>().DBLoaded)
            {
                //DB服务器自己还没加载数据的情况下要延迟处理
                ModuleManager.Get<DistributedMain>().msg_proxy.RequestDBConfig();
                //不是运行态时延迟执行
                ModuleManager.Get<DistributedMain>().RegistLoadDBCompletedCallBack(() => { main.ProcessHouseInfo(reqMsg.logicId); });
                return;
            }
            main.ProcessHouseInfo(reqMsg.logicId);
        }
        public void OnRequestHousePlayerAndBureau(int peerId, bool inbound, object msg)
        {
            RequestHousePlayerAndBureau_L2D reqMsg = msg as RequestHousePlayerAndBureau_L2D;
            ReplyHousePlayerAndBureau_D2L replyMsg = new ReplyHousePlayerAndBureau_D2L();

            HousePlayerBureau housePlayerBureau = new HousePlayerBureau();
            //玩家信息
            List<RunFastPlayerDB> runFastPlayerList = DBManager<RunFastPlayerDB>.Instance.GetRecordsInCache(element => element.houseId == reqMsg.houseId);
            if (runFastPlayerList != null && runFastPlayerList.Count > 0)
            {
                foreach (RunFastPlayerDB runFastPlayerDB in runFastPlayerList)
                {
                    SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == runFastPlayerDB.summonerId);
                    if (summonerDB == null)
                    {
                        ServerUtil.RecordLog(LogType.Error, "OnRequestHousePlayerAndBureau error!! userId = " + runFastPlayerDB.summonerId);
                        continue;
                    }
                    HousePlayerNode playerNode = new HousePlayerNode();
                    playerNode.summonerId = runFastPlayerDB.summonerId;
                    playerNode.userId = summonerDB.userId;
                    playerNode.nickName = summonerDB.nickName;
                    playerNode.sex = summonerDB.sex;
                    playerNode.index = runFastPlayerDB.playerIndex;
                    playerNode.bombIntegral = runFastPlayerDB.bombIntegral;
                    playerNode.winBureau = runFastPlayerDB.winBureau;
                    playerNode.loseBureau = runFastPlayerDB.loseBureau;
                    playerNode.allIntegral = runFastPlayerDB.allIntegral;
                    housePlayerBureau.housePlayerList.Add(playerNode);
                }
            }
            //每局信息
            List<RunFastBureauDB> runFastBureauList = DBManager<RunFastBureauDB>.Instance.GetRecordsInCache(element => element.houseId == reqMsg.houseId);
            if (runFastBureauList != null && runFastBureauList.Count > 0)
            {
                foreach (RunFastBureauDB runFastBureauDB in runFastBureauList)
                {
                    HouseBureau bureauNode = new HouseBureau();
                    bureauNode.bureau = runFastBureauDB.bureau;
                    bureauNode.playerBureauList.AddRange(Serializer.tryUncompressObject<List<PlayerBureauIntegral>>(runFastBureauDB.playerinfo));
                    bureauNode.bureauTime = runFastBureauDB.bureauTime.ToString();

                    housePlayerBureau.houseBureauList.Add(bureauNode);
                }
            }
            replyMsg.houseId = reqMsg.houseId;
            replyMsg.housePlayerBureau = Serializer.tryCompressObject(housePlayerBureau);
            SendMsg(peerId, inbound, replyMsg);
        }
        public void OnRequestSaveDissolveRunFastInfo(int peerId, bool inbound, object msg)
        {
            RequestSaveDissolveRunFastInfo_L2D reqMsg = msg as RequestSaveDissolveRunFastInfo_L2D;

            RunFastHouseDB runFastHouseDB = DBManager<RunFastHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (runFastHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveDissolveRunFastInfo error!! runFastHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            RunFastBureauDB runFastBureauDB = DBManager<RunFastBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.currentBureau));
            if (runFastBureauDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveDissolveRunFastInfo error!! runFastBureauDB == null houseId = " + reqMsg.houseId + ", currentBureau = " + reqMsg.currentBureau);
                return;
            }
            //房间当前局数
            if (runFastHouseDB.currentBureau != (int)reqMsg.currentBureau)
            {
                runFastHouseDB.currentBureau = (int)reqMsg.currentBureau;
                DBManager<RunFastHouseDB>.Instance.UpdateRecordInCache(runFastHouseDB, e => e.houseId == runFastHouseDB.houseId);
            }
            //局数
            if (reqMsg.playerBureauList.Count > 0)
            {
                runFastBureauDB.playerinfo = Serializer.tryCompressObject(reqMsg.playerBureauList);
                DBManager<RunFastBureauDB>.Instance.UpdateRecordInCache(runFastBureauDB, e => e.houseId == runFastBureauDB.houseId && e.bureau == runFastBureauDB.bureau);
            }
            //玩家分数
            reqMsg.playerIntegralList.ForEach(playerIntegral =>
            {
                RunFastPlayerDB runFastPlayerDB = DBManager<RunFastPlayerDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.playerIndex == playerIntegral.playerIndex));
                if (runFastPlayerDB != null && runFastPlayerDB.allIntegral != playerIntegral.integral)
                {
                    runFastPlayerDB.allIntegral = playerIntegral.integral;
                    DBManager<RunFastPlayerDB>.Instance.UpdateRecordInCache(runFastPlayerDB, e => e.houseId == runFastPlayerDB.houseId && e.playerIndex == playerIntegral.playerIndex);
                }
            });
        }
        public void OnRequestRunFastOverallRecord(int peerId, bool inbound, object msg)
        {
            RequestRunFastOverallRecord_L2D reqMsg = msg as RequestRunFastOverallRecord_L2D;
            ReplyRunFastOverallRecord_D2L replyMsg = new ReplyRunFastOverallRecord_D2L();

            replyMsg.summonerId = reqMsg.summonerId;

            List<OverallRecordNode> overallRecordList = new List<OverallRecordNode>();
            List<RunFastPlayerDB> runFastPlayerList = DBManager<RunFastPlayerDB>.Instance.GetRecordsInCache(element => (element.summonerId == reqMsg.summonerId && (element.loseBureau != 0 || element.winBureau != 0)));
            if (runFastPlayerList == null || runFastPlayerList.Count <= 0)
            {
                replyMsg.result = ResultCode.OK;
                replyMsg.overallRecord = Serializer.tryCompressObject(overallRecordList);
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            DateTime nowTime = DateTime.Now;
            List<RunFastHouseDB> runFastHouseList = DBManager<RunFastHouseDB>.Instance.GetRecordsInCache(element => (element.houseStatus >= RunFastHouseStatus.RFHS_Dissolved && (nowTime - element.createTime).TotalDays <= main.getTheOverallRecordTime && runFastPlayerList.Exists(house => house.houseId == element.houseId)));
            if (runFastHouseList == null || runFastHouseList.Count <= 0)
            {
                replyMsg.result = ResultCode.OK;
                replyMsg.overallRecord = Serializer.tryCompressObject(overallRecordList);
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            //排序(时间大的在前面)
            runFastHouseList.Sort((houseOne, houseTwo) => { return (houseOne.createTime > houseTwo.createTime) ? -1 : (houseOne.createTime < houseTwo.createTime ? 1 : 0); });
            if (runFastHouseList.Count > main.getTheOverallRecordNumber)
            {
                runFastHouseList = runFastHouseList.Take(main.getTheOverallRecordNumber).ToList();
            }
            foreach (RunFastHouseDB houseNode in runFastHouseList)
            {
                OverallRecordNode recordNode = new OverallRecordNode();
                recordNode.onlyHouseId = houseNode.houseId;
                recordNode.houseCardId = houseNode.houseCardId;
                recordNode.createTime = houseNode.createTime.ToString();
                List<RunFastPlayerDB> housePlayerList = DBManager<RunFastPlayerDB>.Instance.GetRecordsInCache(element => element.houseId == houseNode.houseId);
                if (housePlayerList != null && housePlayerList.Count >= RunFastConstValue.RunFastTwoPlayer && housePlayerList.Count <= RunFastConstValue.RunFastThreePlayer)
                {
                    foreach (RunFastPlayerDB playerNode in housePlayerList)
                    {
                        if (playerNode.summonerId == reqMsg.summonerId)
                        {
                            recordNode.myIndex = playerNode.playerIndex;
                        }
                        OverallIntegralNode integralNode = new OverallIntegralNode();
                        integralNode.playerIndex = playerNode.playerIndex;
                        integralNode.allIntegral = playerNode.allIntegral;
                        SummonerDB summoner = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == playerNode.summonerId);
                        if (summoner != null)
                        {
                            integralNode.nickName = summoner.nickName;
                            integralNode.summonerId = summoner.id;
                            integralNode.userSex = summoner.sex;
                        }
                        else
                        {
                            integralNode.nickName = "游客";
                            integralNode.userSex = UserSex.Male;
                        }
                        recordNode.overallIntegralList.Add(integralNode);
                    }
                }
                overallRecordList.Add(recordNode);
            }
            replyMsg.overallRecord = Serializer.tryCompressObject(overallRecordList);
            replyMsg.result = ResultCode.OK;
            SendMsg(peerId, inbound, replyMsg);
        }
        public void OnRequestRunFastBureauRecord(int peerId, bool inbound, object msg)
        {
            RequestRunFastBureauRecord_L2D reqMsg = msg as RequestRunFastBureauRecord_L2D;
            ReplyRunFastBureauRecord_D2L replyMsg = new ReplyRunFastBureauRecord_D2L();

            replyMsg.summonerId = reqMsg.summonerId;
            replyMsg.onlyHouseId = reqMsg.onlyHouseId;

            List<BureauRecordNode> bureauRecordList = new List<BureauRecordNode>();
            List<RunFastBureauDB> runFastBureauList = DBManager<RunFastBureauDB>.Instance.GetRecordsInCache(element => element.houseId == reqMsg.onlyHouseId);
            if (runFastBureauList != null && runFastBureauList.Count > 0)
            {
                foreach (RunFastBureauDB runFastBureauDB in runFastBureauList)
                {
                    List<PlayerBureauIntegral> playerBureauList = Serializer.tryUncompressObject<List<PlayerBureauIntegral>>(runFastBureauDB.playerinfo);
                    List<PlayerCardNode> playerInitCardList = Serializer.tryUncompressObject<List<PlayerCardNode>>(runFastBureauDB.playercard);
                    List<PlayerCardNode> playerShowCardList = Serializer.tryUncompressObject<List<PlayerCardNode>>(runFastBureauDB.showcard);
                    if (playerBureauList == null || playerInitCardList == null || playerShowCardList == null || playerBureauList.Count <= 0 || playerInitCardList.Count <= 0 || playerShowCardList.Count <= 0)
                    {
                        continue;
                    }
                    BureauRecordNode bureauNode = new BureauRecordNode();
                    bureauNode.bureau = runFastBureauDB.bureau;
                    bureauNode.bureauTime = runFastBureauDB.bureauTime.ToLongTimeString().ToString();
                    foreach(PlayerBureauIntegral bureauIntegral in playerBureauList)
                    {
                        PlayerIntegral playerIntegral = new PlayerIntegral();
                        playerIntegral.playerIndex = bureauIntegral.playerIndex;
                        playerIntegral.integral = bureauIntegral.bureauIntegral;
                        bureauNode.playerIntegralList.Add(playerIntegral);
                    }
                    bureauRecordList.Add(bureauNode);
                }
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.bureauRecord = Serializer.tryCompressObject(bureauRecordList);
            SendMsg(peerId, inbound, replyMsg);
        }
        public void OnRequestRunFastBureauPlayback(int peerId, bool inbound, object msg)
        {
            RequestRunFastBureauPlayback_L2D reqMsg = msg as RequestRunFastBureauPlayback_L2D;
            ReplyRunFastBureauPlayback_D2L replyMsg = new ReplyRunFastBureauPlayback_D2L();

            replyMsg.summonerId = reqMsg.summonerId;
            replyMsg.onlyHouseId = reqMsg.onlyHouseId;
            replyMsg.bureau = reqMsg.bureau;

            PlayerPlaybackCard playerCard = new PlayerPlaybackCard();
            RunFastBureauDB runFastBureauDB = DBManager<RunFastBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.onlyHouseId && element.bureau == reqMsg.bureau));
            if (runFastBureauDB != null)
            {
                List<PlayerCardNode> playerInitCardList = Serializer.tryUncompressObject<List<PlayerCardNode>>(runFastBureauDB.playercard);
                List<PlayerCardNode> playerShowCardList = Serializer.tryUncompressObject<List<PlayerCardNode>>(runFastBureauDB.showcard);
                if (playerInitCardList != null && playerShowCardList != null)
                {
                    playerCard.playerInitCardList.AddRange(playerInitCardList);
                    playerCard.playerShowCardList.AddRange(playerShowCardList);
                }
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.playerCard = Serializer.tryCompressObject(playerCard);
            SendMsg(peerId, inbound, replyMsg);
        }
    }
}
#endif

