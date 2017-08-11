#if MAHJONG
using System.Collections.Generic;
using LegendProtocol;
using LegendServer.Database;
using System;
using LegendServer.Database.Mahjong;
using LegendServerDB.Core;
using LegendServer.Database.Summoner;
using System.Linq;
using LegendServerDB.Distributed;
using LegendServerDBDefine;

namespace LegendServerDB.Mahjong
{
    public class MahjongMsgProxy : ServerMsgProxy
    {
        private MahjongMain main;

        public MahjongMsgProxy(MahjongMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestMahjongHouseInfo(int peerId, bool inbound, object msg)
        {
            RequestMahjongHouseInfo_L2D reqMsg = msg as RequestMahjongHouseInfo_L2D;

            if (!ModuleManager.Get<DistributedMain>().DBLoaded)
            {
                ModuleManager.Get<DistributedMain>().msg_proxy.RequestDBConfig();
                //不是运行态时延迟执行
                ModuleManager.Get<DistributedMain>().RegistLoadDBCompletedCallBack(() => { main.ProcessHouseInfo(reqMsg.logicId); });
                return;
            }
            main.ProcessHouseInfo(reqMsg.logicId);
        }
        public void OnRequestMahjongPlayerAndBureau(int peerId, bool inbound, object msg)
        {
            RequestMahjongPlayerAndBureau_L2D reqMsg = msg as RequestMahjongPlayerAndBureau_L2D;
            ReplyMahjongPlayerAndBureau_D2L replyMsg = new ReplyMahjongPlayerAndBureau_D2L();

            MahjongPlayerBureauNode mahjongPlayerBureau = new MahjongPlayerBureauNode();
            //玩家信息
            List<MahjongPlayerDB> mahjongPlayerList = DBManager<MahjongPlayerDB>.Instance.GetRecordsInCache(element => element.houseId == reqMsg.houseId);
            if (mahjongPlayerList != null && mahjongPlayerList.Count > 0)
            {
                mahjongPlayerList.ForEach(mahjongPlayerDB =>
                {
                    SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == mahjongPlayerDB.summonerId);
                    if (summonerDB == null)
                    {
                        ServerUtil.RecordLog(LogType.Error, "OnRequestMahjongPlayerAndBureau error!! summonerId = " + mahjongPlayerDB.summonerId);
                    }
                    else
                    {
                        MahjongHousePlayerNode playerNode = new MahjongHousePlayerNode();
                        playerNode.summonerId = mahjongPlayerDB.summonerId;
                        playerNode.userId = summonerDB.userId;
                        playerNode.playerIndex = mahjongPlayerDB.playerIndex;
                        playerNode.nickName = summonerDB.nickName;
                        playerNode.sex = summonerDB.sex;
                        playerNode.zhuangLeisureType = mahjongPlayerDB.zhuangLeisureType;
                        playerNode.bigWinMyself = mahjongPlayerDB.bigWinMyself;
                        playerNode.smallWinMyself = mahjongPlayerDB.smallWinMyself;
                        playerNode.bigWinFangBlast = mahjongPlayerDB.bigWinFangBlast;
                        playerNode.smallWinFangBlast = mahjongPlayerDB.smallWinFangBlast;
                        playerNode.bigWinJieBlast = mahjongPlayerDB.bigWinJieBlast;
                        playerNode.smallWinJieBlast = mahjongPlayerDB.smallWinJieBlast;
                        playerNode.allIntegral = mahjongPlayerDB.allIntegral;
                        mahjongPlayerBureau.mahjongPlayerList.Add(playerNode);
                    }
                });
            }
            //每局信息
            List<MahjongBureauDB> mahjongBureauList = DBManager<MahjongBureauDB>.Instance.GetRecordsInCache(element => element.houseId == reqMsg.houseId);
            if (mahjongBureauList != null && mahjongBureauList.Count > 0)
            {
                mahjongBureauList.ForEach(mahjongBureauDB =>
                {
                    MahjongHouseBureau bureauNode = new MahjongHouseBureau();
                    bureauNode.bureau = mahjongBureauDB.bureau;
                    bureauNode.playerBureauList.AddRange(Serializer.tryUncompressObject<List<MahjongPlayerBureau>>(mahjongBureauDB.playerinfo));
                    bureauNode.bureauTime = mahjongBureauDB.bureauTime.ToString();

                    mahjongPlayerBureau.mahjongBureauList.Add(bureauNode);
                });
            }
            replyMsg.houseId = reqMsg.houseId;
            replyMsg.housePlayerBureau = Serializer.tryCompressObject(mahjongPlayerBureau);
            SendMsg(peerId, inbound, replyMsg);
        }
        public void OnRequestSaveMahjongNewPlayer(int peerId, bool inbound, object msg)
        {
            RequestSaveMahjongNewPlayer_L2D reqMsg = msg as RequestSaveMahjongNewPlayer_L2D;

            MahjongPlayerDB mahjongPlayerDB = new MahjongPlayerDB();
            mahjongPlayerDB.houseId = reqMsg.houseId;
            mahjongPlayerDB.summonerId = reqMsg.summonerId;
            mahjongPlayerDB.playerIndex = reqMsg.index;
            mahjongPlayerDB.allIntegral = reqMsg.allIntegral;
            mahjongPlayerDB.zhuangLeisureType = ZhuangLeisureType.Leisure;
            mahjongPlayerDB.smallWinFangBlast = 0;
            mahjongPlayerDB.smallWinJieBlast = 0;
            mahjongPlayerDB.smallWinMyself = 0;
            mahjongPlayerDB.bigWinFangBlast = 0;
            mahjongPlayerDB.bigWinJieBlast = 0;
            mahjongPlayerDB.bigWinMyself = 0;
            mahjongPlayerDB.bGetRecord = false;

            bool result = DBManager<MahjongPlayerDB>.Instance.AddRecordToCache(mahjongPlayerDB, element => (element.houseId == mahjongPlayerDB.houseId && element.summonerId == mahjongPlayerDB.summonerId));
            if (!result)
            {
                //创建房间玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongNewPlayer 创建房间玩家信息失败 houseId = " + mahjongPlayerDB.houseId + ", summonerId = " + mahjongPlayerDB.summonerId);
            }

            //保存房间人数
            MahjongHouseDB mahjongHouseDB = DBManager<MahjongHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (mahjongHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongNewPlayer error!! mahjongHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            if (mahjongHouseDB.curPlayerNum < mahjongHouseDB.maxPlayerNum)
            {
                mahjongHouseDB.curPlayerNum += 1;
                //保存数据库
                DBManager<MahjongHouseDB>.Instance.UpdateRecordInCache(mahjongHouseDB, e => e.houseId == mahjongHouseDB.houseId);
            }

            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg.summonerId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseId error!! userId = " + reqMsg.summonerId);
                return;
            }
            summonerDB.houseId = reqMsg.houseId;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
        }
        public void OnRequestSaveCreateMahjongInfo(int peerId, bool inbound, object msg)
        {
            RequestSaveCreateMahjongInfo_L2D reqMsg = msg as RequestSaveCreateMahjongInfo_L2D;

            MahjongHouseDB mahjongHouseDB = new MahjongHouseDB();
            mahjongHouseDB.houseId = reqMsg.houseId;
            mahjongHouseDB.houseCardId = reqMsg.houseCardId;
            mahjongHouseDB.logicId = reqMsg.logicId;
            mahjongHouseDB.maxBureau = reqMsg.maxBureau;
            mahjongHouseDB.curPlayerNum = 1;
            mahjongHouseDB.maxPlayerNum = reqMsg.maxPlayerNum;
            mahjongHouseDB.businessId = reqMsg.businessId;
            mahjongHouseDB.housePropertyType = reqMsg.housePropertyType;
            mahjongHouseDB.catchBird = reqMsg.catchBird;
            mahjongHouseDB.flutter = reqMsg.flutter;
            mahjongHouseDB.houseType = reqMsg.houseType;
            mahjongHouseDB.mahjongType = reqMsg.mahjongType;
            mahjongHouseDB.createTime = Convert.ToDateTime(reqMsg.createTime);
            mahjongHouseDB.currentBureau = 0;
            mahjongHouseDB.houseStatus = MahjongHouseStatus.MHS_FreeBureau;
            mahjongHouseDB.endTime = DateTime.Parse("1970-01-01 00:00:00");

            bool result = DBManager<MahjongHouseDB>.Instance.AddRecordToCache(mahjongHouseDB, element => element.houseId == mahjongHouseDB.houseId);
            if (!result)
            {
                //创建房间玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveCreateMahjongInfo 创建房间玩家信息失败 houseId = " + mahjongHouseDB.houseId);
            }

            MahjongPlayerDB mahjongPlayerDB = new MahjongPlayerDB();
            mahjongPlayerDB.houseId = reqMsg.houseId;
            mahjongPlayerDB.summonerId = reqMsg.summonerId;
            mahjongPlayerDB.playerIndex = reqMsg.index;
            mahjongPlayerDB.allIntegral = reqMsg.allIntegral;
            mahjongPlayerDB.zhuangLeisureType = ZhuangLeisureType.Leisure;
            mahjongPlayerDB.smallWinFangBlast = 0;
            mahjongPlayerDB.smallWinJieBlast = 0;
            mahjongPlayerDB.smallWinMyself = 0;
            mahjongPlayerDB.bigWinFangBlast = 0;
            mahjongPlayerDB.bigWinJieBlast = 0;
            mahjongPlayerDB.bigWinMyself = 0;
            mahjongPlayerDB.bGetRecord = false;

            result = DBManager<MahjongPlayerDB>.Instance.AddRecordToCache(mahjongPlayerDB, element => (element.houseId == mahjongPlayerDB.houseId && element.summonerId == mahjongPlayerDB.summonerId));
            if (!result)
            {
                //创建房间玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveCreateMahjongInfo 创建房间玩家信息失败 houseId = " + mahjongPlayerDB.houseId + ", summonerId = " + mahjongPlayerDB.summonerId);
            }

            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg.summonerId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseId error!! summonerId = " + reqMsg.summonerId);
                return;
            }
            summonerDB.houseId = reqMsg.houseId;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
        }
        public void OnRequestDelMahjongHousePlayer(int peerId, bool inbound, object msg)
        {
            RequestDelMahjongHousePlayer_L2D reqMsg = msg as RequestDelMahjongHousePlayer_L2D;

            int result = DBManager<MahjongPlayerDB>.Instance.DeleteRecordInCache(element => (element.houseId == reqMsg.houseId && element.summonerId == reqMsg.summonerId));
            if (result == 0)
            {
                //创建房间玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelMahjongHousePlayer 删除房间玩家信息失败 houseId = " + reqMsg.houseId + ", summonerId = " + reqMsg.summonerId);
            }
            //保存房间人数
            MahjongHouseDB mahjongHouseDB = DBManager<MahjongHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (mahjongHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelMahjongHousePlayer error!! mahjongHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            if (mahjongHouseDB.curPlayerNum > 0)
            {
                mahjongHouseDB.curPlayerNum -= 1;
                //保存数据库
                DBManager<MahjongHouseDB>.Instance.UpdateRecordInCache(mahjongHouseDB, e => e.houseId == mahjongHouseDB.houseId);
            }
            //清理掉房间Id
            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg.summonerId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelMahjongHousePlayer error!! summonerId = " + reqMsg.summonerId);
                return;
            }
            summonerDB.houseId = 0;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == reqMsg.summonerId);
        }
        public void OnRequestSaveMahjongHouseStatus(int peerId, bool inbound, object msg)
        {
            RequestSaveMahjongHouseStatus_L2D reqMsg = msg as RequestSaveMahjongHouseStatus_L2D;

            MahjongHouseDB mahjongHouseDB = DBManager<MahjongHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (mahjongHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongHouseStatus error!! mahjongHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            if (mahjongHouseDB.houseStatus != reqMsg.houseStatus)
            {
                mahjongHouseDB.houseStatus = reqMsg.houseStatus;
                if (mahjongHouseDB.houseStatus >= MahjongHouseStatus.MHS_Dissolved)
                {
                    //房间解散或者结束了
                    mahjongHouseDB.endTime = DateTime.Now;
                }
                //保存数据库
                DBManager<MahjongHouseDB>.Instance.UpdateRecordInCache(mahjongHouseDB, e => e.houseId == mahjongHouseDB.houseId);
            }
        }
        public void OnRequestSaveMahjongBureauInfo(int peerId, bool inbound, object msg)
        {
            RequestSaveMahjongBureauInfo_L2D reqMsg = msg as RequestSaveMahjongBureauInfo_L2D;

            if (reqMsg.currentBureau == 0 || string.IsNullOrEmpty(reqMsg.bureauTime) || reqMsg.playerInitTile == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongBureauInfo error!! reqMsg.houseBureau  == null houseId = " + reqMsg.houseId);
                return;
            }
            //当局信息
            bool bInsert = false;
            MahjongBureauDB mahjongBureauDB = DBManager<MahjongBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.currentBureau));
            if (mahjongBureauDB == null)
            {
                mahjongBureauDB = new MahjongBureauDB();
                mahjongBureauDB.houseId = reqMsg.houseId;
                mahjongBureauDB.bureau = reqMsg.currentBureau;
                bInsert = true;
            }
            List<MahjongPlayerBureau> playerBureauList = new List<MahjongPlayerBureau>();
            reqMsg.playerIndexList.ForEach(playerIndex =>
            {
                MahjongPlayerBureau playerBureau = new MahjongPlayerBureau();
                playerBureau.playerIndex = playerIndex;
                playerBureauList.Add(playerBureau);
            });
            mahjongBureauDB.playerinfo = Serializer.tryCompressObject(playerBureauList);
            mahjongBureauDB.playermahjong = reqMsg.playerInitTile;
            List<MahjongRecordNode> showMahongList = new List<MahjongRecordNode>();
            mahjongBureauDB.showmahjong = Serializer.tryCompressObject(showMahongList);
            mahjongBureauDB.bureauTime = Convert.ToDateTime(reqMsg.bureauTime);

            if (bInsert)
            {
                bool result = DBManager<MahjongBureauDB>.Instance.AddRecordToCache(mahjongBureauDB, element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.currentBureau));
                if (!result)
                {
                    //创建房间当局信息失败
                    ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongBureauInfo 创建房间当局信息失败 houseId = " + reqMsg.houseId + ", bureau = " + reqMsg.currentBureau);
                }
            }
            else
            {
                DBManager<MahjongBureauDB>.Instance.UpdateRecordInCache(mahjongBureauDB, e => e.houseId == mahjongBureauDB.houseId && e.bureau == mahjongBureauDB.bureau);
            }
        }
        public void OnRequestSaveMahjongRecord(int peerId, bool inbound, object msg)
        {
            RequestSaveMahjongRecord_L2D reqMsg = msg as RequestSaveMahjongRecord_L2D;

            MahjongBureauDB mahjongBureauDB = DBManager<MahjongBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.currentBureau));
            if (mahjongBureauDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongRecord error!! mahjongBureauDB == null houseId = " + reqMsg.houseId + ", currentBureau = " + reqMsg.currentBureau);
                return;
            }
            List<MahjongRecordNode> playerShowMahjongList = Serializer.tryUncompressObject<List<MahjongRecordNode>>(mahjongBureauDB.showmahjong);
            if (playerShowMahjongList == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongRecord error!! playerShowMahjongList == null houseId = " + reqMsg.houseId + ", currentBureau = " + reqMsg.currentBureau);
                return;
            }
            if (reqMsg.mahjongRecordNode == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongRecord error!! mahjongRecordNode == null houseId = " + reqMsg.houseId + ", currentBureau = " + reqMsg.currentBureau);
                return;
            }
            //记录回放节点          
            playerShowMahjongList.Add(reqMsg.mahjongRecordNode);
            mahjongBureauDB.showmahjong = Serializer.tryCompressObject(playerShowMahjongList);
        }
        public void OnRequestSaveMahjongPlayerSettlement(int peerId, bool inbound, object msg)
        {
            RequestSaveMahjongPlayerSettlement_L2D reqMsg = msg as RequestSaveMahjongPlayerSettlement_L2D;

            MahjongPlayerDB mahjongPlayerDB = DBManager<MahjongPlayerDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.summonerId == reqMsg.summonerId));
            if (mahjongPlayerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongPlayerSettlement error!! mahjongPlayerDB == null houseId = " + reqMsg.houseId + ", summonerId = " + reqMsg.summonerId);
                return;
            }
            mahjongPlayerDB.smallWinFangBlast = reqMsg.smallWinFangBlast;
            mahjongPlayerDB.smallWinJieBlast = reqMsg.smallWinJieBlast;
            mahjongPlayerDB.smallWinMyself = reqMsg.smallWinMyself;
            mahjongPlayerDB.bigWinFangBlast = reqMsg.bigWinFangBlast;
            mahjongPlayerDB.bigWinJieBlast = reqMsg.bigWinJieBlast;
            mahjongPlayerDB.bigWinMyself = reqMsg.bigWinMyself;
            mahjongPlayerDB.allIntegral = reqMsg.allIntegral;
            mahjongPlayerDB.zhuangLeisureType = reqMsg.zhuangLeisureType;
            if (!mahjongPlayerDB.bGetRecord)
            {
                mahjongPlayerDB.bGetRecord = true;
            }

            DBManager<MahjongPlayerDB>.Instance.UpdateRecordInCache(mahjongPlayerDB, e => e.houseId == mahjongPlayerDB.houseId && e.summonerId == mahjongPlayerDB.summonerId);
        }
        public void OnRequestSaveMahjongBureauIntegral(int peerId, bool inbound, object msg)
        {
            RequestSaveMahjongBureauIntegral_L2D reqMsg = msg as RequestSaveMahjongBureauIntegral_L2D;

            MahjongHouseDB mahjongHouseDB = DBManager<MahjongHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (mahjongHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongBureauIntegral error!! mahjongHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            MahjongBureauDB mahjongBureauDB = DBManager<MahjongBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.bureau));
            if (mahjongBureauDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveMahjongBureauIntegral error!! mahjongBureauDB == null houseId = " + reqMsg.houseId + ", currentBureau = " + reqMsg.bureau);
                return;
            }
            //房间当前局数
            if (mahjongHouseDB.currentBureau != (int)reqMsg.bureau)
            {
                mahjongHouseDB.currentBureau = (int)reqMsg.bureau;
                DBManager<MahjongHouseDB>.Instance.UpdateRecordInCache(mahjongHouseDB, e => e.houseId == mahjongHouseDB.houseId);
            }
            //局数
            if (reqMsg.playerBureauList.Count > 0)
            {
                mahjongBureauDB.playerinfo = Serializer.tryCompressObject(reqMsg.playerBureauList);
                DBManager<MahjongBureauDB>.Instance.UpdateRecordInCache(mahjongBureauDB, e => e.houseId == mahjongBureauDB.houseId && e.bureau == mahjongBureauDB.bureau);
            }
        }
        public void OnRequestSaveDissolveMahjongInfo(int peerId, bool inbound, object msg)
        {
            RequestSaveDissolveMahjongInfo_L2D reqMsg = msg as RequestSaveDissolveMahjongInfo_L2D;

            MahjongHouseDB mahjongHouseDB = DBManager<MahjongHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (mahjongHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveDissolveMahjongInfo error!! mahjongHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            MahjongBureauDB mahjongBureauDB = DBManager<MahjongBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.currentBureau));
            if (mahjongBureauDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveDissolveMahjongInfo error!! mahjongBureauDB == null houseId = " + reqMsg.houseId + ", currentBureau = " + reqMsg.currentBureau);
                return;
            }
            //房间当前局数
            if (mahjongHouseDB.currentBureau != (int)reqMsg.currentBureau)
            {
                mahjongHouseDB.currentBureau = (int)reqMsg.currentBureau;
                DBManager<MahjongHouseDB>.Instance.UpdateRecordInCache(mahjongHouseDB, e => e.houseId == mahjongHouseDB.houseId);
            }
            //局数
            if (reqMsg.playerBureauList.Count > 0)
            {
                mahjongBureauDB.playerinfo = Serializer.tryCompressObject(reqMsg.playerBureauList);
                DBManager<MahjongBureauDB>.Instance.UpdateRecordInCache(mahjongBureauDB, e => e.houseId == mahjongBureauDB.houseId && e.bureau == mahjongBureauDB.bureau);
            }
            //玩家分数
            reqMsg.playerIntegralList.ForEach(playerIntegral =>
            {
                MahjongPlayerDB mahjongPlayerDB = DBManager<MahjongPlayerDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.playerIndex == playerIntegral.playerIndex));
                if (mahjongPlayerDB != null && mahjongPlayerDB.allIntegral != playerIntegral.integral)
                {
                    mahjongPlayerDB.allIntegral = playerIntegral.integral;
                    DBManager<MahjongPlayerDB>.Instance.UpdateRecordInCache(mahjongPlayerDB, e => e.houseId == mahjongPlayerDB.houseId && e.playerIndex == playerIntegral.playerIndex);
                }
            });
        }
        public void OnRequestMahjongOverallRecord(int peerId, bool inbound, object msg)
        {
            RequestMahjongOverallRecord_L2D reqMsg = msg as RequestMahjongOverallRecord_L2D;
            ReplyMahjongOverallRecord_D2L replyMsg = new ReplyMahjongOverallRecord_D2L();

            replyMsg.summonerId = reqMsg.summonerId;

            List<OverallRecordNode> overallRecordList = new List<OverallRecordNode>();
            List<MahjongPlayerDB> mahjongPlayerList = DBManager<MahjongPlayerDB>.Instance.GetRecordsInCache(element => (element.summonerId == reqMsg.summonerId && element.bGetRecord));
            if (mahjongPlayerList == null || mahjongPlayerList.Count <= 0)
            {
                replyMsg.result = ResultCode.OK;
                replyMsg.overallRecord = Serializer.tryCompressObject(overallRecordList);
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            DateTime nowTime = DateTime.Now;
            List<MahjongHouseDB> mahjongHouseList = DBManager<MahjongHouseDB>.Instance.GetRecordsInCache(element => (element.houseStatus >= MahjongHouseStatus.MHS_Dissolved && (nowTime - element.createTime).TotalDays <= main.getTheOverallRecordTime && mahjongPlayerList.Exists(house => house.houseId == element.houseId)));
            if (mahjongHouseList == null || mahjongHouseList.Count <= 0)
            {
                replyMsg.result = ResultCode.OK;
                replyMsg.overallRecord = Serializer.tryCompressObject(overallRecordList);
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            //排序(时间大的在前面)
            mahjongHouseList.Sort((houseOne, houseTwo) => { return (houseOne.createTime > houseTwo.createTime) ? -1 : (houseOne.createTime < houseTwo.createTime ? 1 : 0); });
            if (mahjongHouseList.Count > main.getTheOverallRecordNumber)
            {
                mahjongHouseList = mahjongHouseList.Take(main.getTheOverallRecordNumber).ToList();
            }
            mahjongHouseList.ForEach(houseNode =>
            {
                OverallRecordNode recordNode = new OverallRecordNode();
                recordNode.onlyHouseId = houseNode.houseId;
                recordNode.houseCardId = houseNode.houseCardId;
                recordNode.createTime = houseNode.createTime.ToString();
                recordNode.myIndex = (int)houseNode.mahjongType * 10;
                List<MahjongPlayerDB> housePlayerList = DBManager<MahjongPlayerDB>.Instance.GetRecordsInCache(element => element.houseId == houseNode.houseId);
                if (housePlayerList != null && housePlayerList.Count >= MahjongConstValue.MahjongThreePlayer && housePlayerList.Count <= MahjongConstValue.MahjongFourPlayer)
                {
                    housePlayerList.ForEach(playerNode =>
                    {
                        if (playerNode.summonerId == reqMsg.summonerId)
                        {
                            recordNode.myIndex += playerNode.playerIndex;
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
                    });
                }
                overallRecordList.Add(recordNode);
            });
            replyMsg.overallRecord = Serializer.tryCompressObject(overallRecordList);
            replyMsg.result = ResultCode.OK;
            SendMsg(peerId, inbound, replyMsg);
        }
        public void OnRequestMahjongBureauRecord(int peerId, bool inbound, object msg)
        {
            RequestMahjongBureauRecord_L2D reqMsg = msg as RequestMahjongBureauRecord_L2D;
            ReplyMahjongBureauRecord_D2L replyMsg = new ReplyMahjongBureauRecord_D2L();

            replyMsg.summonerId = reqMsg.summonerId;
            replyMsg.onlyHouseId = reqMsg.onlyHouseId;

            List<BureauRecordNode> bureauRecordList = new List<BureauRecordNode>();
            List<MahjongBureauDB> mahjongBureauList = DBManager<MahjongBureauDB>.Instance.GetRecordsInCache(element => element.houseId == reqMsg.onlyHouseId);
            if (mahjongBureauList != null && mahjongBureauList.Count > 0)
            {
                mahjongBureauList.ForEach(mahjongBureauDB =>
                {
                    List<MahjongPlayerBureau> playerBureauList = Serializer.tryUncompressObject<List<MahjongPlayerBureau>>(mahjongBureauDB.playerinfo);
                    List<PlayerTileNode> playerInitTileList = Serializer.tryUncompressObject<List<PlayerTileNode>>(mahjongBureauDB.playermahjong);
                    List<MahjongRecordNode> mahjongRecordList = Serializer.tryUncompressObject<List<MahjongRecordNode>>(mahjongBureauDB.showmahjong);
                    if (playerBureauList != null && playerInitTileList != null && mahjongRecordList != null && playerBureauList.Count > 0 && playerInitTileList.Count > 0 && mahjongRecordList.Count > 0)
                    {
                        BureauRecordNode bureauNode = new BureauRecordNode();
                        bureauNode.bureau = mahjongBureauDB.bureau;
                        bureauNode.bureauTime = mahjongBureauDB.bureauTime.ToLongTimeString().ToString();
                        foreach (MahjongPlayerBureau bureauIntegral in playerBureauList)
                        {
                            PlayerIntegral playerIntegral = new PlayerIntegral();
                            playerIntegral.playerIndex = bureauIntegral.playerIndex;
                            playerIntegral.integral = bureauIntegral.bureauIntegral;
                            bureauNode.playerIntegralList.Add(playerIntegral);
                        }
                        bureauRecordList.Add(bureauNode);
                    }
                });
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.bureauRecord = Serializer.tryCompressObject(bureauRecordList);
            SendMsg(peerId, inbound, replyMsg);
        }
        public void OnRequestMahjongBureauPlayback(int peerId, bool inbound, object msg)
        {
            RequestMahjongBureauPlayback_L2D reqMsg = msg as RequestMahjongBureauPlayback_L2D;
            ReplyMahjongBureauPlayback_D2L replyMsg = new ReplyMahjongBureauPlayback_D2L();

            replyMsg.summonerId = reqMsg.summonerId;
            replyMsg.onlyHouseId = reqMsg.onlyHouseId;
            replyMsg.bureau = reqMsg.bureau;

            PlayerPlaybackMahjong playerMahjong = new PlayerPlaybackMahjong();
            MahjongBureauDB mahjongBureauDB = DBManager<MahjongBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.onlyHouseId && element.bureau == reqMsg.bureau));
            if (mahjongBureauDB != null)
            {
                List<PlayerTileNode> playerInitTileList = Serializer.tryUncompressObject<List<PlayerTileNode>>(mahjongBureauDB.playermahjong);
                List<MahjongRecordNode> mahjongRecordList = Serializer.tryUncompressObject<List<MahjongRecordNode>>(mahjongBureauDB.showmahjong);
                if (playerInitTileList != null && mahjongRecordList != null)
                {
                    playerMahjong.playerInitTileList.AddRange(playerInitTileList);
                    playerMahjong.mahjongRecordList.AddRange(mahjongRecordList);
                }
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.playerMahjong = Serializer.tryCompressObject(playerMahjong);
            SendMsg(peerId, inbound, replyMsg);
        }
    }
}
#endif