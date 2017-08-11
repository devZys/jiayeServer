#if WORDPLATE
using System.Collections.Generic;
using LegendProtocol;
using LegendServer.Database;
using System;
using LegendServer.Database.WordPlate;
using LegendServerDB.Core;
using LegendServer.Database.Summoner;
using System.Linq;
using LegendServerDB.Distributed;

namespace LegendServerDB.WordPlate
{
    public class WordPlateMsgProxy : ServerMsgProxy
    {
        private WordPlateMain main;

        public WordPlateMsgProxy(WordPlateMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestWordPlateHouseInfo(int peerId, bool inbound, object msg)
        {
            RequestWordPlateHouseInfo_L2D reqMsg = msg as RequestWordPlateHouseInfo_L2D;

            if (!ModuleManager.Get<DistributedMain>().DBLoaded)
            {
                ModuleManager.Get<DistributedMain>().msg_proxy.RequestDBConfig();
                //不是运行态时延迟执行
                ModuleManager.Get<DistributedMain>().RegistLoadDBCompletedCallBack(() => { main.ProcessHouseInfo(reqMsg.logicId); });
                return;
            }
            main.ProcessHouseInfo(reqMsg.logicId);
        }
        public void OnRequestWordPlatePlayerAndBureau(int peerId, bool inbound, object msg)
        {
            RequestWordPlatePlayerAndBureau_L2D reqMsg = msg as RequestWordPlatePlayerAndBureau_L2D;
            ReplyWordPlatePlayerAndBureau_D2L replyMsg = new ReplyWordPlatePlayerAndBureau_D2L();

            WordPlatePlayerBureauNode wordPlatePlayerBureau = new WordPlatePlayerBureauNode();
            //玩家信息
            List<WordPlatePlayerDB> wordPlatePlayerList = DBManager<WordPlatePlayerDB>.Instance.GetRecordsInCache(element => element.houseId == reqMsg.houseId);
            if (wordPlatePlayerList != null && wordPlatePlayerList.Count > 0)
            {
                wordPlatePlayerList.ForEach(wordPlatePlayerDB =>
                {
                    SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == wordPlatePlayerDB.summonerId);
                    if (summonerDB == null)
                    {
                        ServerUtil.RecordLog(LogType.Error, "OnRequestWordPlatePlayerAndBureau error!! summonerId = " + wordPlatePlayerDB.summonerId);
                    }
                    else
                    {
                        WordPlateHousePlayerNode playerNode = new WordPlateHousePlayerNode();
                        playerNode.summonerId = wordPlatePlayerDB.summonerId;
                        playerNode.playerIndex = wordPlatePlayerDB.playerIndex;
                        playerNode.userId = summonerDB.userId;
                        playerNode.nickName = summonerDB.nickName;
                        playerNode.sex = summonerDB.sex;
                        playerNode.zhuangLeisureType = wordPlatePlayerDB.zhuangLeisureType;
                        playerNode.winAmount = wordPlatePlayerDB.winAmount;
                        playerNode.allWinScore = wordPlatePlayerDB.allWinScore;
                        playerNode.allIntegral = wordPlatePlayerDB.allIntegral;
                        wordPlatePlayerBureau.wordPlatePlayerList.Add(playerNode);
                    }
                });
            }
            //每局信息
            List<WordPlateBureauDB> wordPlateBureauList = DBManager<WordPlateBureauDB>.Instance.GetRecordsInCache(element => element.houseId == reqMsg.houseId);
            if (wordPlateBureauList != null && wordPlateBureauList.Count > 0)
            {
                wordPlateBureauList.ForEach(wordPlateBureauDB =>
                {
                    WordPlateHouseBureau bureauNode = new WordPlateHouseBureau();
                    bureauNode.bureau = wordPlateBureauDB.bureau;
                    bureauNode.playerBureauList.AddRange(Serializer.tryUncompressObject<List<WordPlatePlayerBureau>>(wordPlateBureauDB.playerinfo));
                    bureauNode.bureauTime = wordPlateBureauDB.bureauTime.ToString();

                    wordPlatePlayerBureau.wordPlateBureauList.Add(bureauNode);
                });
            }
            replyMsg.houseId = reqMsg.houseId;
            replyMsg.housePlayerBureau = Serializer.tryCompressObject(wordPlatePlayerBureau);
            SendMsg(peerId, inbound, replyMsg);
        }
        public void OnRequestSaveWordPlateNewPlayer(int peerId, bool inbound, object msg)
        {
            RequestSaveWordPlateNewPlayer_L2D reqMsg = msg as RequestSaveWordPlateNewPlayer_L2D;

            WordPlateHouseDB wordPlateHouseDB = DBManager<WordPlateHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (wordPlateHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlateNewPlayer error!! wordPlateHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg.summonerId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseId error!! summonerId = " + reqMsg.summonerId);
                return;
            }

            WordPlatePlayerDB wordPlatePlayerDB = new WordPlatePlayerDB();
            wordPlatePlayerDB.houseId = reqMsg.houseId;
            wordPlatePlayerDB.summonerId = reqMsg.summonerId;
            wordPlatePlayerDB.playerIndex = reqMsg.index;
            wordPlatePlayerDB.allIntegral = reqMsg.allIntegral;
            wordPlatePlayerDB.zhuangLeisureType = ZhuangLeisureType.Leisure;
            wordPlatePlayerDB.winAmount = 0;
            wordPlatePlayerDB.allWinScore = 0;
            wordPlatePlayerDB.bGetRecord = false;

            bool result = DBManager<WordPlatePlayerDB>.Instance.AddRecordToCache(wordPlatePlayerDB, element => (element.houseId == wordPlatePlayerDB.houseId && element.summonerId == wordPlatePlayerDB.summonerId));
            if (!result)
            {
                //创建房间玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlateNewPlayer 创建房间玩家信息失败 houseId = " + wordPlatePlayerDB.houseId + ", summonerId = " + wordPlatePlayerDB.summonerId);
            }

            //保存房间人数
            if (wordPlateHouseDB.curPlayerNum < wordPlateHouseDB.maxPlayerNum)
            {
                wordPlateHouseDB.curPlayerNum += 1;
                //保存数据库
                DBManager<WordPlateHouseDB>.Instance.UpdateRecordInCache(wordPlateHouseDB, e => e.houseId == wordPlateHouseDB.houseId);
            }

            //保存房间号
            summonerDB.houseId = reqMsg.houseId;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
        }
        public void OnRequestSaveCreateWordPlateInfo(int peerId, bool inbound, object msg)
        {
            RequestSaveCreateWordPlateInfo_L2D reqMsg = msg as RequestSaveCreateWordPlateInfo_L2D;

            WordPlateHouseDB wordPlateHouseDB = new WordPlateHouseDB();
            wordPlateHouseDB.houseId = reqMsg.houseId;
            wordPlateHouseDB.houseCardId = reqMsg.houseCardId;
            wordPlateHouseDB.logicId = reqMsg.logicId;
            wordPlateHouseDB.maxBureau = reqMsg.maxBureau;
            wordPlateHouseDB.maxWinScore = reqMsg.maxWinScore;
            wordPlateHouseDB.baseWinScore = reqMsg.baseWinScore;
            wordPlateHouseDB.curPlayerNum = 1;
            wordPlateHouseDB.maxPlayerNum = reqMsg.maxPlayerNum;
            wordPlateHouseDB.businessId = reqMsg.businessId;
            wordPlateHouseDB.housePropertyType = reqMsg.housePropertyType;
            wordPlateHouseDB.beginGodType = -1;
            wordPlateHouseDB.houseType = reqMsg.houseType;
            wordPlateHouseDB.wordPlateType = reqMsg.wordPlateType;
            wordPlateHouseDB.createTime = Convert.ToDateTime(reqMsg.createTime);
            wordPlateHouseDB.currentBureau = 0;
            wordPlateHouseDB.houseStatus = WordPlateHouseStatus.EWPS_FreeBureau;
            wordPlateHouseDB.endTime = DateTime.Parse("1970-01-01 00:00:00");

            bool result = DBManager<WordPlateHouseDB>.Instance.AddRecordToCache(wordPlateHouseDB, element => element.houseId == wordPlateHouseDB.houseId);
            if (!result)
            {
                //创建房间玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveCreateWordPlateInfo 创建房间玩家信息失败 houseId = " + wordPlateHouseDB.houseId);
            }

            WordPlatePlayerDB wordPlatePlayerDB = new WordPlatePlayerDB();
            wordPlatePlayerDB.houseId = reqMsg.houseId;
            wordPlatePlayerDB.summonerId = reqMsg.summonerId;
            wordPlatePlayerDB.playerIndex = reqMsg.index;
            wordPlatePlayerDB.allIntegral = reqMsg.allIntegral;
            wordPlatePlayerDB.zhuangLeisureType = ZhuangLeisureType.Leisure;
            wordPlatePlayerDB.winAmount = 0;
            wordPlatePlayerDB.allWinScore = 0;
            wordPlatePlayerDB.bGetRecord = false;

            result = DBManager<WordPlatePlayerDB>.Instance.AddRecordToCache(wordPlatePlayerDB, element => (element.houseId == wordPlatePlayerDB.houseId && element.summonerId == wordPlatePlayerDB.summonerId));
            if (!result)
            {
                //创建房间玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveCreateWordPlateInfo 创建房间玩家信息失败 houseId = " + wordPlatePlayerDB.houseId + ", summonerId = " + wordPlatePlayerDB.summonerId);
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
        public void OnRequestDelWordPlateHousePlayer(int peerId, bool inbound, object msg)
        {
            RequestDelWordPlateHousePlayer_L2D reqMsg = msg as RequestDelWordPlateHousePlayer_L2D;

            int result = DBManager<WordPlatePlayerDB>.Instance.DeleteRecordInCache(element => (element.houseId == reqMsg.houseId && element.summonerId == reqMsg.summonerId));
            if (result == 0)
            {
                //创建房间玩家信息失败
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelWordPlateHousePlayer 删除房间玩家信息失败 houseId = " + reqMsg.houseId + ", summonerId = " + reqMsg.summonerId);
            }  
            
            //保存房间人数
            WordPlateHouseDB wordPlateHouseDB = DBManager<WordPlateHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (wordPlateHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestDelWordPlateHousePlayer error!! wordPlateHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            if (wordPlateHouseDB.curPlayerNum > 0)
            {
                wordPlateHouseDB.curPlayerNum -= 1;
                //保存数据库
                DBManager<WordPlateHouseDB>.Instance.UpdateRecordInCache(wordPlateHouseDB, e => e.houseId == wordPlateHouseDB.houseId);
            }

            //清理掉房间Id
            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg.summonerId);
            if (summonerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveHouseId error!! summonerId = " + reqMsg.summonerId);
                return;
            }
            summonerDB.houseId = 0;
            DBManager<SummonerDB>.Instance.UpdateRecordInCache(summonerDB, e => e.id == summonerDB.id);
        }
        public void OnRequestSaveWordPlateHouseStatus(int peerId, bool inbound, object msg)
        {
            RequestSaveWordPlateHouseStatus_L2D reqMsg = msg as RequestSaveWordPlateHouseStatus_L2D;

            WordPlateHouseDB wordPlateHouseDB = DBManager<WordPlateHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (wordPlateHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlateHouseStatus error!! wordPlateHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            if (wordPlateHouseDB.houseStatus != reqMsg.houseStatus)
            {
                wordPlateHouseDB.houseStatus = reqMsg.houseStatus;
                if (wordPlateHouseDB.houseStatus >= WordPlateHouseStatus.EWPS_Dissolved)
                {
                    //房间解散或者结束了
                    wordPlateHouseDB.endTime = DateTime.Now;
                }
                //保存数据库
                DBManager<WordPlateHouseDB>.Instance.UpdateRecordInCache(wordPlateHouseDB, e => e.houseId == wordPlateHouseDB.houseId);
            }
        }
        public void OnRequestSaveWordPlateBureauInfo(int peerId, bool inbound, object msg)
        {
            RequestSaveWordPlateBureauInfo_L2D reqMsg = msg as RequestSaveWordPlateBureauInfo_L2D;

            if (reqMsg.currentBureau == 0 || string.IsNullOrEmpty(reqMsg.bureauTime) || reqMsg.playerInitTile == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlateBureauInfo error!! reqMsg.houseBureau  == null houseId = " + reqMsg.houseId);
                return;
            }
            //当局信息
            bool bInsert = false;
            WordPlateBureauDB wordPlateBureauDB = DBManager<WordPlateBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.currentBureau));
            if (wordPlateBureauDB == null)
            {
                wordPlateBureauDB = new WordPlateBureauDB();
                wordPlateBureauDB.houseId = reqMsg.houseId;
                wordPlateBureauDB.bureau = reqMsg.currentBureau;
                bInsert = true;
            }
            List<WordPlatePlayerBureau> playerBureauList = new List<WordPlatePlayerBureau>();
            reqMsg.playerIndexList.ForEach(playerIndex =>
            {
                WordPlatePlayerBureau playerBureau = new WordPlatePlayerBureau();
                playerBureau.playerIndex = playerIndex;
                playerBureauList.Add(playerBureau);
            });
            wordPlateBureauDB.playerinfo = Serializer.tryCompressObject(playerBureauList);
            wordPlateBureauDB.playerWordPlate = reqMsg.playerInitTile;
            wordPlateBureauDB.bureauTime = Convert.ToDateTime(reqMsg.bureauTime);
            //记录
            List<WordPlateRecordNode> showMahongList = new List<WordPlateRecordNode>();
            if (reqMsg.currentBureau == 1 && reqMsg.beginGodTile != 0)
            {
                //第一把才需要抓神
                showMahongList.Add(new WordPlateRecordNode { recordType = WordPlateRecordType.EWPR_BeginGodTile, recordData = Serializer.trySerializerObject(reqMsg.beginGodTile) });
                SaveWordPlateHouseGodType(reqMsg.houseId, reqMsg.beginGodTile);
            }
            wordPlateBureauDB.showWordPlate = Serializer.tryCompressObject(showMahongList);

            if (bInsert)
            {
                bool result = DBManager<WordPlateBureauDB>.Instance.AddRecordToCache(wordPlateBureauDB, element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.currentBureau));
                if (!result)
                {
                    //创建房间当局信息失败
                    ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlateBureauInfo 创建房间当局信息失败 houseId = " + reqMsg.houseId + ", bureau = " + reqMsg.currentBureau);
                }
            }
            else
            {
                DBManager<WordPlateBureauDB>.Instance.UpdateRecordInCache(wordPlateBureauDB, element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.currentBureau));
            }
        }
        private void SaveWordPlateHouseGodType(ulong houseId, int beginGodTile)
        {
            WordPlateHouseDB wordPlateHouseDB = DBManager<WordPlateHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == houseId);
            if (wordPlateHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "SaveWordPlateHouseGodType error!! wordPlateHouseDB == null houseId = " + houseId);
                return;
            }
            WordPlateTile wordPlateTile = new WordPlateTile(beginGodTile);
            int beginGodType = (int)wordPlateTile.GetNumType() % 2;
            if (wordPlateHouseDB.beginGodType != beginGodType)
            {
                wordPlateHouseDB.beginGodType = beginGodType;
            }
        }
        public void OnRequestSaveWordPlateRecord(int peerId, bool inbound, object msg)
        {
            RequestSaveWordPlateRecord_L2D reqMsg = msg as RequestSaveWordPlateRecord_L2D;

            WordPlateBureauDB wordPlateBureauDB = DBManager<WordPlateBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.currentBureau));
            if (wordPlateBureauDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlateRecord error!! wordPlateBureauDB == null houseId = " + reqMsg.houseId + ", currentBureau = " + reqMsg.currentBureau);
                return;
            }
            List<WordPlateRecordNode> playerShowWordPlateList = Serializer.tryUncompressObject<List<WordPlateRecordNode>>(wordPlateBureauDB.showWordPlate);
            if (playerShowWordPlateList == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlateRecord error!! playerShowWordPlateList == null houseId = " + reqMsg.houseId + ", currentBureau = " + reqMsg.currentBureau);
                return;
            }
            if (reqMsg.wordPlateRecordNode == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlateRecord error!! wordPlateRecordNode == null houseId = " + reqMsg.houseId + ", currentBureau = " + reqMsg.currentBureau);
                return;
            }
            //记录回放节点          
            playerShowWordPlateList.Add(reqMsg.wordPlateRecordNode);
            wordPlateBureauDB.showWordPlate = Serializer.tryCompressObject(playerShowWordPlateList);
        }
        public void OnRequestSaveWordPlatePlayerSettlement(int peerId, bool inbound, object msg)
        {
            RequestSaveWordPlatePlayerSettlement_L2D reqMsg = msg as RequestSaveWordPlatePlayerSettlement_L2D;

            WordPlatePlayerDB wordPlatePlayerDB = DBManager<WordPlatePlayerDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.summonerId == reqMsg.summonerId));
            if (wordPlatePlayerDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlatePlayerSettlement error!! wordPlatePlayerDB == null houseId = " + reqMsg.houseId + ", summonerId = " + reqMsg.summonerId);
                return;
            }
            wordPlatePlayerDB.winAmount = reqMsg.winAmount;
            wordPlatePlayerDB.allWinScore = reqMsg.allWinScore;
            wordPlatePlayerDB.allIntegral = reqMsg.allIntegral;
            wordPlatePlayerDB.zhuangLeisureType = reqMsg.zhuangLeisureType;
            if (!wordPlatePlayerDB.bGetRecord)
            {
                wordPlatePlayerDB.bGetRecord = true;
            }

            DBManager<WordPlatePlayerDB>.Instance.UpdateRecordInCache(wordPlatePlayerDB, element => (element.houseId == reqMsg.houseId && element.summonerId == reqMsg.summonerId));
        }
        public void OnRequestSaveWordPlateBureauIntegral(int peerId, bool inbound, object msg)
        {
            RequestSaveWordPlateBureauIntegral_L2D reqMsg = msg as RequestSaveWordPlateBureauIntegral_L2D;

            WordPlateHouseDB wordPlateHouseDB = DBManager<WordPlateHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.houseId);
            if (wordPlateHouseDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlateBureauIntegral error!! wordPlateHouseDB == null houseId = " + reqMsg.houseId);
                return;
            }
            WordPlateBureauDB wordPlateBureauDB = DBManager<WordPlateBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.bureau));
            if (wordPlateBureauDB == null)
            {
                ServerUtil.RecordLog(LogType.Error, "OnRequestSaveWordPlateBureauIntegral error!! wordPlateBureauDB == null houseId = " + reqMsg.houseId + ", currentBureau = " + reqMsg.bureau);
                return;
            }
            //房间当前局数
            if (wordPlateHouseDB.currentBureau != (int)reqMsg.bureau)
            {
                wordPlateHouseDB.currentBureau = (int)reqMsg.bureau;
                DBManager<WordPlateHouseDB>.Instance.UpdateRecordInCache(wordPlateHouseDB, e => e.houseId == wordPlateHouseDB.houseId);
            }
            //局数
            if (reqMsg.playerBureauList.Count > 0)
            {
                wordPlateBureauDB.playerinfo = Serializer.tryCompressObject(reqMsg.playerBureauList);
                DBManager<WordPlateBureauDB>.Instance.UpdateRecordInCache(wordPlateBureauDB, element => (element.houseId == reqMsg.houseId && element.bureau == reqMsg.bureau));
            }
        }
        public void OnRequestWordPlateOverallRecord(int peerId, bool inbound, object msg)
        {
            RequestWordPlateOverallRecord_L2D reqMsg = msg as RequestWordPlateOverallRecord_L2D;
            ReplyWordPlateOverallRecord_D2L replyMsg = new ReplyWordPlateOverallRecord_D2L();

            replyMsg.summonerId = reqMsg.summonerId;

            List<OverallRecordNode> overallRecordList = new List<OverallRecordNode>();
            List<WordPlatePlayerDB> wordPlatePlayerList = DBManager<WordPlatePlayerDB>.Instance.GetRecordsInCache(element => (element.summonerId == reqMsg.summonerId && element.bGetRecord));
            if (wordPlatePlayerList == null || wordPlatePlayerList.Count <= 0)
            {
                replyMsg.result = ResultCode.OK;
                replyMsg.overallRecord = Serializer.tryCompressObject(overallRecordList);
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            DateTime nowTime = DateTime.Now;
            List<WordPlateHouseDB> wordPlateHouseList = DBManager<WordPlateHouseDB>.Instance.GetRecordsInCache(element => (element.houseStatus >= WordPlateHouseStatus.EWPS_Dissolved && (nowTime - element.createTime).TotalDays <= main.getTheOverallRecordTime && wordPlatePlayerList.Exists(house => house.houseId == element.houseId)));
            if (wordPlateHouseList == null || wordPlateHouseList.Count <= 0)
            {
                replyMsg.result = ResultCode.OK;
                replyMsg.overallRecord = Serializer.tryCompressObject(overallRecordList);
                SendMsg(peerId, inbound, replyMsg);
                return;
            }
            //排序(时间大的在前面)
            wordPlateHouseList.Sort((houseOne, houseTwo) => { return (houseOne.createTime > houseTwo.createTime) ? -1 : (houseOne.createTime < houseTwo.createTime ? 1 : 0); });
            if (wordPlateHouseList.Count > main.getTheOverallRecordNumber)
            {
                wordPlateHouseList = wordPlateHouseList.Take(main.getTheOverallRecordNumber).ToList();
            }
            wordPlateHouseList.ForEach(houseNode =>
            {
                OverallRecordNode recordNode = new OverallRecordNode();
                recordNode.onlyHouseId = houseNode.houseId;
                recordNode.houseCardId = houseNode.houseCardId;
                recordNode.createTime = houseNode.createTime.ToString();
                recordNode.myIndex = (int)houseNode.wordPlateType * 10;
                List<WordPlatePlayerDB> housePlayerList = DBManager<WordPlatePlayerDB>.Instance.GetRecordsInCache(element => element.houseId == houseNode.houseId);
                if (housePlayerList != null && housePlayerList.Count == WordPlateConstValue.WordPlateMaxPlayer)
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
        public void OnRequestWordPlateBureauRecord(int peerId, bool inbound, object msg)
        {
            RequestWordPlateBureauRecord_L2D reqMsg = msg as RequestWordPlateBureauRecord_L2D;
            ReplyWordPlateBureauRecord_D2L replyMsg = new ReplyWordPlateBureauRecord_D2L();

            replyMsg.summonerId = reqMsg.summonerId;
            replyMsg.onlyHouseId = reqMsg.onlyHouseId;

            List<BureauRecordNode> bureauRecordList = new List<BureauRecordNode>();
            List<WordPlateBureauDB> wordPlateBureauList = DBManager<WordPlateBureauDB>.Instance.GetRecordsInCache(element => element.houseId == reqMsg.onlyHouseId);
            if (wordPlateBureauList != null && wordPlateBureauList.Count > 0)
            {
                wordPlateBureauList.ForEach(wordPlateBureauDB =>
                {
                    List<WordPlatePlayerBureau> playerBureauList = Serializer.tryUncompressObject<List<WordPlatePlayerBureau>>(wordPlateBureauDB.playerinfo);
                    List<PlayerTileNode> playerInitTileList = Serializer.tryUncompressObject<List<PlayerTileNode>>(wordPlateBureauDB.playerWordPlate);
                    List<WordPlateRecordNode> wordPlateRecordList = Serializer.tryUncompressObject<List<WordPlateRecordNode>>(wordPlateBureauDB.showWordPlate);
                    if (playerBureauList != null && playerInitTileList != null && wordPlateRecordList != null && playerBureauList.Count > 0 && playerInitTileList.Count > 0 && wordPlateRecordList.Count > 0)
                    {
                        BureauRecordNode bureauNode = new BureauRecordNode();
                        bureauNode.bureau = wordPlateBureauDB.bureau;
                        bureauNode.bureauTime = wordPlateBureauDB.bureauTime.ToLongTimeString().ToString();
                        foreach (WordPlatePlayerBureau bureauIntegral in playerBureauList)
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
        public void OnRequestWordPlateBureauPlayback(int peerId, bool inbound, object msg)
        {
            RequestWordPlateBureauPlayback_L2D reqMsg = msg as RequestWordPlateBureauPlayback_L2D;
            ReplyWordPlateBureauPlayback_D2L replyMsg = new ReplyWordPlateBureauPlayback_D2L();

            replyMsg.summonerId = reqMsg.summonerId;
            replyMsg.onlyHouseId = reqMsg.onlyHouseId;
            replyMsg.bureau = reqMsg.bureau;

            PlayerPlayBackWordPlate playerWordPlate = new PlayerPlayBackWordPlate();
            WordPlateHouseDB wordPlateHouseDB = DBManager<WordPlateHouseDB>.Instance.GetSingleRecordInCache(element => element.houseId == reqMsg.onlyHouseId);
            if (wordPlateHouseDB != null)
            {
                playerWordPlate.beginGodType = wordPlateHouseDB.beginGodType;
            }
            WordPlateBureauDB wordPlateBureauDB = DBManager<WordPlateBureauDB>.Instance.GetSingleRecordInCache(element => (element.houseId == reqMsg.onlyHouseId && element.bureau == reqMsg.bureau));
            if (wordPlateBureauDB != null)
            {
                List<PlayerTileNode> playerInitTileList = Serializer.tryUncompressObject<List<PlayerTileNode>>(wordPlateBureauDB.playerWordPlate);
                List<WordPlateRecordNode> wordPlateRecordList = Serializer.tryUncompressObject<List<WordPlateRecordNode>>(wordPlateBureauDB.showWordPlate);
                if (playerInitTileList != null && wordPlateRecordList != null)
                {
                    playerWordPlate.playerInitTileList.AddRange(playerInitTileList);
                    playerWordPlate.wordPlateRecordList.AddRange(wordPlateRecordList);
                }
            }

            replyMsg.result = ResultCode.OK;
            replyMsg.playerWordPlate = Serializer.tryCompressObject(playerWordPlate);
            SendMsg(peerId, inbound, replyMsg);
        }
    }
}
#endif