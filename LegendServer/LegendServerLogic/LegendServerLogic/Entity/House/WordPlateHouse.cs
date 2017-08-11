#if WORDPLATE
using LegendProtocol;
using System.Collections.Generic;
using LegendServerLogic.Actor.Summoner;
using LegendServerLogic.Entity.Base;
using System.Linq;
using System;
using LegendServerLogic.Entity.Players;
using LegendServerLogic.WordPlate;
using LegendServerLogicDefine;

namespace LegendServerLogic.Entity.Houses
{
    public class WordPlateHouse : House
    {
        //房间属性类型
        public int housePropertyType;
        //当前字牌类型
        public WordPlateType wordPlateType;
        //当前胡牌基础分
        public int baseWinScore;
        //房间最大胡息数
        public int maxWinScore;
        //单神 双神
        public int beginGodType;
        //结束时的神牌
        public WordPlateTile endGodTile;
        //是否流局
        public bool bLiuJu;
        //房间状态
        public WordPlateHouseStatus houseStatus;
        //当前出牌是否为玩家打出
        public bool bPlayerShowPlate;
        //当前出牌
        public WordPlateTile currentWordPlate;
        //发牌延迟
        public int waitTime;
        //当前摸牌的玩家位置
        public int giveOffPlayerIndex;
        //当局剩余的牌
        private List<WordPlateTile> remainWordPlateList;
        //操作类
        private List<WordPlateOperatNode> wordPlateOperatList;
        //每局的信息
        public List<WordPlateHouseBureau> houseBureauList;
        // 字牌逻辑策略
        private WordPlateStrategyBase m_strategy;
        // 发牌临时变量
        private Dictionary<int, WordPlateHandTile> m_WordPlateHandList;
        // 房间临时变量
        private List<WordPlatePlayer> m_WordPlatePlayerList;
        public WordPlateHouse()
        {
            houseCardId = 0;
            logicId = 0;
            houseId = 0;
            maxBureau = 0;
            businessId = 0;
            currentBureau = 0;
            currentWhoPlay = -1;
            currentShowCard = -1;
            giveOffPlayerIndex = 0;
            housePropertyType = 0;
            competitionKey = 0;
            maxPlayerNum = WordPlateConstValue.WordPlateMaxPlayer;
            houseType = HouseType.WordPlateHouse;
            baseWinScore = 0;
            beginGodType = -1;
            waitTime = 0;
            bLiuJu = false;
            bPlayerShowPlate = false;
            currentWordPlate = null;
            endGodTile = null;
            remainWordPlateList = new List<WordPlateTile>();
            wordPlateOperatList = new List<WordPlateOperatNode>();
            summonerList = new List<Player>();
            houseBureauList = new List<WordPlateHouseBureau>();
            m_WordPlateHandList = new Dictionary<int, WordPlateHandTile>();
            m_WordPlatePlayerList = new List<WordPlatePlayer>();
            voteBeginTime = DateTime.Parse("1970-01-01 00:00:00");
            operateBeginTime = DateTime.Parse("1970-01-01 00:00:00");
        }
        public bool SetWordPlateStrategy(WordPlateType wordPlateType)
        {
            WordPlateStrategyBase strategy = WordPlateManager.Instance.GetWordPlateStrategy(wordPlateType);
            if (strategy == null)
            {
                return true;
            }
            m_strategy = strategy;

            return false;
        }
        public WordPlatePlayer CreatHouse(Summoner summoner, WordPlatePlayerStatus housePlayerStatus = WordPlatePlayerStatus.WordPlateFree)
        {
            //绑定玩家
            WordPlatePlayer newHousePlayer = new WordPlatePlayer(m_strategy);
            newHousePlayer.index = 0;
            newHousePlayer.userId = summoner.userId;
            newHousePlayer.nickName = summoner.nickName;
            newHousePlayer.summonerId = summoner.id;
            newHousePlayer.sex = summoner.sex;
            newHousePlayer.ip = summoner.ip;
            newHousePlayer.proxyServerId = summoner.proxyServerId;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.baseScore = baseWinScore;
            newHousePlayer.bFamous = CheckHousePropertyType(WordPlateHousePropertyType.EMHP_Famous);
            summonerList.Add(newHousePlayer);
            m_WordPlatePlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public WordPlatePlayer CreatHouse(PlayerInfo playerInfo, WordPlatePlayerStatus housePlayerStatus = WordPlatePlayerStatus.WordPlateReady)
        {
            //绑定玩家
            WordPlatePlayer newHousePlayer = new WordPlatePlayer(m_strategy);
            newHousePlayer.index = 0;
            newHousePlayer.userId = playerInfo.userId;
            newHousePlayer.nickName = playerInfo.nickName;
            newHousePlayer.summonerId = playerInfo.summonerId;
            newHousePlayer.sex = playerInfo.sex;
            newHousePlayer.ip = playerInfo.ip;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.baseScore = baseWinScore;
            newHousePlayer.bFamous = CheckHousePropertyType(WordPlateHousePropertyType.EMHP_Famous);
            summonerList.Add(newHousePlayer);
            m_WordPlatePlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public WordPlatePlayer AddPlayer(Summoner summoner, WordPlatePlayerStatus housePlayerStatus = WordPlatePlayerStatus.WordPlateFree)
        {
            //绑定玩家
            WordPlatePlayer newHousePlayer = new WordPlatePlayer(m_strategy);
            newHousePlayer.index = GetHousePlayerIndex();
            newHousePlayer.userId = summoner.userId;
            newHousePlayer.nickName = summoner.nickName;
            newHousePlayer.summonerId = summoner.id;
            newHousePlayer.sex = summoner.sex;
            newHousePlayer.ip = summoner.ip;
            newHousePlayer.proxyServerId = summoner.proxyServerId;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.baseScore = baseWinScore;
            newHousePlayer.bFamous = CheckHousePropertyType(WordPlateHousePropertyType.EMHP_Famous);
            summonerList.Add(newHousePlayer);
            m_WordPlatePlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public WordPlatePlayer AddPlayer(PlayerInfo playerInfo, WordPlatePlayerStatus housePlayerStatus = WordPlatePlayerStatus.WordPlateReady)
        {
            //绑定玩家
            WordPlatePlayer newHousePlayer = new WordPlatePlayer(m_strategy);
            newHousePlayer.index = GetHousePlayerIndex();
            newHousePlayer.userId = playerInfo.userId;
            newHousePlayer.nickName = playerInfo.nickName;
            newHousePlayer.summonerId = playerInfo.summonerId;
            newHousePlayer.sex = playerInfo.sex;
            newHousePlayer.ip = playerInfo.ip;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.baseScore = baseWinScore;
            newHousePlayer.bFamous = CheckHousePropertyType(WordPlateHousePropertyType.EMHP_Famous);
            summonerList.Add(newHousePlayer);
            m_WordPlatePlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public void AddPlayer(WordPlateHousePlayerNode playerNode, WordPlatePlayerStatus housePlayerStatus = WordPlatePlayerStatus.WordPlateFree)
        {
            //绑定玩家
            WordPlatePlayer newHousePlayer = new WordPlatePlayer(m_strategy);
            newHousePlayer.index = playerNode.playerIndex;
            newHousePlayer.userId = playerNode.userId;
            newHousePlayer.nickName = playerNode.nickName;
            newHousePlayer.summonerId = playerNode.summonerId;
            newHousePlayer.sex = playerNode.sex;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.zhuangLeisureType = playerNode.zhuangLeisureType;
            newHousePlayer.m_nWinAmount = playerNode.winAmount;
            newHousePlayer.m_nAllWinScore = playerNode.allWinScore;
            newHousePlayer.allIntegral = playerNode.allIntegral;
            newHousePlayer.lineType = LineType.OffLine;
            newHousePlayer.baseScore = baseWinScore;
            newHousePlayer.bFamous = CheckHousePropertyType(WordPlateHousePropertyType.EMHP_Famous);
            summonerList.Add(newHousePlayer);
            m_WordPlatePlayerList.Add(newHousePlayer);
        }
        public override WordPlateHouseStatus GetWordPlateHouseStatus()
        {
            return houseStatus;
        }
        //设置GL剩余的牌
        public void GetRemainWordPlateList(List<int> wordPlateList)
        {
            remainWordPlateList.ForEach(wordPlateTile =>
            {
                wordPlateList.Add(wordPlateTile.GetWordPlateNode());
            });
        }    
        //设置GL当前的牌
        public int GetCurrentWordPlateNode()
        {
            if (currentWordPlate != null)
            {
                return currentWordPlate.GetWordPlateNode();
            }
            return 0;
        }
        //判断房间属性
        public bool CheckHousePropertyType(WordPlateHousePropertyType type)
        {
            if ((int)type == (housePropertyType & (int)type))
            {
                return true;
            }
            return false;
        }
        public void GetPlayerOperat(int playerIndex, List<WordPlateOperatType> operatTypeList)
        {
            if (currentShowCard == -1)
            {
                //普通操作
                WordPlateOperatNode operatNode = wordPlateOperatList.Find(element => element.playerIndex == playerIndex);
                if (operatNode != null && operatNode.bWait && operatNode.operatType != WordPlateOperatType.EWPO_None)
                {
                    operatTypeList.AddRange(operatNode.operatTypeList);
                }
            }
        } 
        public void AddWordPlateOperat(int playerIndex, List<WordPlateOperatType> operatTypeList)
        {
            if (operatTypeList.Count == 0)
            {
                return;
            }
            WordPlateOperatType maxOperatType = operatTypeList.Max();
            if (maxOperatType == WordPlateOperatType.EWPO_Flutter)
            {
                if (playerIndex == currentWhoPlay)
                {
                    maxOperatType = WordPlateOperatType.EWPO_NFlutter;
                }
                else
                {
                    maxOperatType = WordPlateOperatType.EWPO_WFlutter;
                }
            }
            else if (maxOperatType == WordPlateOperatType.EWPO_None)
            {
                return;
            }
            WordPlateOperatNode operatNode = wordPlateOperatList.Find(element => element.playerIndex == playerIndex);
            if (operatNode == null)
            {
                operatNode = new WordPlateOperatNode();
                operatNode.playerIndex = playerIndex;
                operatNode.bWait = true;
                operatNode.operatType = maxOperatType;
                operatNode.operatTypeList.AddRange(operatTypeList);
                wordPlateOperatList.Add(operatNode);
            }
            else
            {
                if (!operatNode.bWait)
                {
                    operatNode.bWait = true;
                }
                if (maxOperatType > operatNode.operatType)
                {
                    operatNode.operatType = maxOperatType;
                }
                if (operatNode.operatTypeList.Count > 0)
                {
                    operatNode.operatTypeList.Clear();
                }
                operatNode.operatTypeList.AddRange(operatTypeList);
            }
        }
        public void ClearWordPlateOperat()
        {
            wordPlateOperatList.Clear();
        }
        public List<WordPlateOperatNode> GetWordPlateOperat()
        {
            return wordPlateOperatList;
        }
        public bool SetWordPlateOperat(int playerIndex, WordPlateOperatType operatType, List<int> operatWordPlateList)
        {
            WordPlateOperatNode operatNode = wordPlateOperatList.Find(element => element.playerIndex == playerIndex);
            if (operatNode == null || !operatNode.bWait || (operatType != WordPlateOperatType.EWPO_None && !operatNode.operatTypeList.Exists(element => element == operatType)))
            {
                return false;
            }
            if (operatType == WordPlateOperatType.EWPO_Flutter)
            {
                if (playerIndex == currentWhoPlay)
                {
                    operatNode.operatType = WordPlateOperatType.EWPO_NFlutter;
                }
                else
                {
                    operatNode.operatType = WordPlateOperatType.EWPO_WFlutter;
                }
            }
            else
            {
                operatNode.operatType = operatType;
            }
            operatNode.operatWordPlateList.Clear();
            operatNode.operatWordPlateList.AddRange(operatWordPlateList);
            operatNode.bWait = false;
            return true;
        }
        public List<WordPlateOperatNode> GetPlayerWordPlateOperat()
        {
            WordPlateOperatType operatType = wordPlateOperatList.Max(element => element.operatType);
            return wordPlateOperatList.FindAll(element => (element.operatType == operatType && element.operatType != WordPlateOperatType.EWPO_None));
        }
        public WordPlateOperatNode GetPlayerWordPlateOperatNode()
        {
            List<WordPlateOperatNode> _wordPlateOperatList = wordPlateOperatList.FindAll(element => element.bWait && element.operatType != WordPlateOperatType.EWPO_None);
            if (_wordPlateOperatList != null && _wordPlateOperatList.Count > 0)
            {
                WordPlateOperatType operatType = _wordPlateOperatList.Max(element => element.operatType);
                return _wordPlateOperatList.Find(element => (element.operatType == operatType));
            }
            return null;
        }
        public void DisWordPlatePassOperat(WordPlateOperatType operatType = WordPlateOperatType.EWPO_None)
        {
            if (operatType != WordPlateOperatType.EWPO_None && operatType != WordPlateOperatType.EWPO_Chow)
            {
                return;
            }
            List<WordPlateOperatNode> _wordPlateOperatList = wordPlateOperatList.FindAll(element => !element.bWait && element.operatType <= operatType);
            if (_wordPlateOperatList != null && _wordPlateOperatList.Count > 0)
            {
                _wordPlateOperatList.ForEach(wordPlateOperat =>
                {
                    WordPlatePlayer operatPlayer = GetWordPlatePlayer(wordPlateOperat.playerIndex);
                    if (operatPlayer != null && !operatPlayer.m_bDeadHand)
                    {
                        wordPlateOperat.operatTypeList.ForEach(element =>
                        {
                            if (element == WordPlateOperatType.EWPO_Pong || element == WordPlateOperatType.EWPO_Wai)
                            {
                                //碰臭
                                operatPlayer.SetPassPongTile(currentWordPlate);
                            }
                            else if (wordPlateOperat.operatType == WordPlateOperatType.EWPO_None && element == WordPlateOperatType.EWPO_Chow && (operatType == WordPlateOperatType.EWPO_None || (operatPlayer.index == currentWhoPlay)))
                            {
                                //吃臭
                                if (operatPlayer.SetPassChowTile(currentWordPlate))
                                {
                                    //告诉玩家你有臭牌
                                    ModuleManager.Get<WordPlateMain>().SendPassChowWordPlate(operatPlayer.summonerId, operatPlayer.proxyServerId, currentWordPlate);
                                }
                            }
                        });
                    }
                });
            }
        }
        public bool CheckCurrentWordPlate(int wordPlateNode)
        {
            if (wordPlateNode == 0)
            {
                return false;
            }
            return CheckCurrentWordPlate(new WordPlateTile(wordPlateNode));
        }
        public bool CheckCurrentWordPlate(WordPlateTile wordPlateTile)
        {
            if (currentWordPlate == null || wordPlateTile == null)
            {
                return false;
            }
            return currentWordPlate.Equal(wordPlateTile);
        }
        public List<WordPlatePlayer> GetWordPlatePlayer()
        {
            if (m_WordPlatePlayerList.Count != summonerList.Count)
            {
                m_WordPlatePlayerList.Clear();
                foreach (WordPlatePlayer wordPlatePlayer in summonerList)
                {
                    m_WordPlatePlayerList.Add(wordPlatePlayer);
                }
            }
            return m_WordPlatePlayerList;
        }
        public List<WordPlatePlayer> GetOtherWordPlatePlayer(string userId)
        {
            return GetWordPlatePlayer().FindAll(element => element.userId != userId);
        }
        public WordPlatePlayer GetWordPlatePlayer(string userId)
        {
            return GetWordPlatePlayer().FirstOrDefault(element => element.userId == userId);
        }
        public WordPlatePlayer GetWordPlatePlayer(int index)
        {
            return GetWordPlatePlayer().FirstOrDefault(element => element.index == index);
        }
        public WordPlatePlayer GetWordPlatePlayerBySummonerId(ulong summonerId)
        {
            return GetWordPlatePlayer().FirstOrDefault(element => element.summonerId == summonerId);
        }
        public WordPlatePlayer GetHouseOwner()
        {
            return GetWordPlatePlayer(0);
        }
        public WordPlatePlayer GetHouseZhuang()
        {
            return GetWordPlatePlayer().FirstOrDefault(element => element.zhuangLeisureType == ZhuangLeisureType.Zhuang);
        }
        public int GetHouseZhuangIndex()
        {
            WordPlatePlayer zhuangPlayer = GetWordPlatePlayer().FirstOrDefault(element => element.zhuangLeisureType == ZhuangLeisureType.Zhuang);
            if (zhuangPlayer != null)
            {
                return zhuangPlayer.index;
            }
            return 0;
        }
        public List<WordPlatePlayer> GetWordPlatePlayersByCondition(Func<WordPlatePlayer, bool> condition)
        {
            return GetWordPlatePlayer().Where(condition).ToList();
        }
        public WordPlatePlayer GetWordPlatePlayerByCondition(Func<WordPlatePlayer, bool> condition)
        {
            return GetWordPlatePlayer().FirstOrDefault(condition);
        }
        public void SetHouseZhuang(WordPlatePlayer player)
        {
            if (player.zhuangLeisureType != ZhuangLeisureType.Zhuang)
            {
                WordPlatePlayer zhuangPlayer = GetHouseZhuang();
                if (zhuangPlayer != null)
                {
                    zhuangPlayer.zhuangLeisureType = ZhuangLeisureType.Leisure;
                }
                player.zhuangLeisureType = ZhuangLeisureType.Zhuang;
            }
        }
        public WordPlatePlayer GetWordPlateVoteLaunchPlayer()
        {
            return GetWordPlatePlayer().Find(element => element.voteStatus == VoteStatus.LaunchVote);
        }
        public WordPlatePlayer GetNextHousePlayer(int index)
        {
            int nextIndex = GetNextHousePlayerIndex(index);
            return GetWordPlatePlayer(nextIndex);
        }
        public WordPlatePlayer GetLastHousePlayer(int index)
        {
            int lastIndex = GetLastHousePlayerIndex(index);
            return GetWordPlatePlayer(lastIndex);
        }
        public List<WordPlateTile> GetRemainWordPlate()
        {
            return remainWordPlateList;
        }
        public void InitRemainWordPlate()
        {
            remainWordPlateList.Clear();
        }
        public int GetRemainWordPlateCount()
        {
            return remainWordPlateList.Count;
        }
        public WordPlateTile GetNewTileByRemainWordPlate()
        {
            int remainWordPlateCount = remainWordPlateList.Count;
            if (remainWordPlateCount == 0)
            {
                return null;
            }
            int index = 0;
            if (remainWordPlateCount > 1)
            {
                index = MyRandom.NextPrecise(0, remainWordPlateCount);
            }
            WordPlateTile tile = remainWordPlateList[index];
            WordPlateTile newTile = new WordPlateTile(tile);
            remainWordPlateList.Remove(tile);

            return newTile;
        }
        public int GetEndGodWordPlateTile()
        {
            int remainWordPlateCount = remainWordPlateList.Count;
            if (remainWordPlateCount == 0)
            {
                endGodTile = currentWordPlate;
            }
            else if (remainWordPlateCount > 0)
            {
                endGodTile = remainWordPlateList.FirstOrDefault();
            }
            if (endGodTile != null)
            {
                return endGodTile.GetWordPlateNode();
            }
            return 0;
        }
        public WordPlateTile GetGodWordPlateTile()
        {
            if (currentBureau == 1 || beginGodType == -1)
            {
                //只有第一局才抓神
                PlateDescType descType = (PlateDescType)MyRandom.NextPrecise((int)PlateDescType.EPD_Small, (int)PlateDescType.EPD_Big + 1);
                PlateNumType numType = (PlateNumType)MyRandom.NextPrecise((int)PlateNumType.EPN_One, (int)PlateNumType.EPN_Ten + 1);

                beginGodType = (int)numType % 2;

                return new WordPlateTile(descType, numType);
            }

            return null;
        }
        public void InitWordPlateHouse()
        {
            bLiuJu = false;
            bPlayerShowPlate = false;
            wordPlateOperatList.Clear();
            currentWordPlate = null;
            endGodTile = null;
            remainWordPlateList.Clear();
        }
        public void InitPlayerBureau(WordPlateHouseBureau houseBureau)
        {
            WordPlateHouseBureau _houseBureau = houseBureauList.Find(element => element.bureau == houseBureau.bureau);
            if (_houseBureau == null)
            {
                houseBureauList.Add(houseBureau);
            }
            else
            {
                _houseBureau.bureauTime = houseBureau.bureauTime;
                _houseBureau.playerBureauList.Clear();
                _houseBureau.playerBureauList.AddRange(houseBureau.playerBureauList);
            }
        }
        public WordPlateHouseBureau GetHouseBureau()
        {
            return houseBureauList.Find(element => element.bureau == (ulong)currentBureau);
        }
        public List<WordPlateEndSettlementNode> GetWordPlateEndSettlementList()
        {
            List<WordPlateEndSettlementNode> wordPlateEndSettlementList = new List<WordPlateEndSettlementNode>();
            GetWordPlatePlayer().ForEach(housePlayer =>
            {
                WordPlateEndSettlementNode wordPlateEndSettlementNode = new WordPlateEndSettlementNode();
                wordPlateEndSettlementNode.index = housePlayer.index;
                wordPlateEndSettlementNode.winAmount = housePlayer.m_nWinAmount;
                wordPlateEndSettlementNode.allWinScore = housePlayer.m_nAllWinScore;
                wordPlateEndSettlementNode.allIntegral = housePlayer.allIntegral;
                wordPlateEndSettlementList.Add(wordPlateEndSettlementNode);
            });

            return wordPlateEndSettlementList;
        }
        public bool CheckBeginWordPlates()
        {
            if (!CheckPlayerFull())
            {
                //人不够
                return false;
            }
            if (GetWordPlatePlayer().Exists(element => element.housePlayerStatus != WordPlatePlayerStatus.WordPlateReady))
            {
                //有人没准备
                return false;
            }
            if (currentBureau >= maxBureau)
            {
                //局数满啦
                return false;
            }

            return true;
        }
        public void BeginWordPlates(WordPlateHouseBureau houseBureau)
        {
            houseStatus = WordPlateHouseStatus.EWPS_BeginBureau;
            if (currentBureau == 0)
            {                
                //标志第一局
                currentBureau = 1;
                //设置庄家
                SetZhuangPlayer(0);
            }
            else
            {
                currentBureau += 1;
                currentWhoPlay = -1;
                bLiuJu = false;
                bPlayerShowPlate = false;
                endGodTile = null;
                currentWordPlate = null;
                remainWordPlateList.Clear();
                wordPlateOperatList.Clear();
            }
            m_WordPlateHandList.Clear();
            foreach (WordPlatePlayer wordPlatePlayer in summonerList)
            {
                m_WordPlateHandList.Add(wordPlatePlayer.index, new WordPlateHandTile(wordPlatePlayer.zhuangLeisureType));
            }
            //人数到齐可以开局
            m_strategy.InitWordPlateTile(m_WordPlateHandList, remainWordPlateList);
            //初始化房间局数信息
            houseBureau.bureau = (ulong)currentBureau;
            houseBureau.bureauTime = DateTime.Now.ToString();
            foreach(WordPlatePlayer wordPlatePlayer in summonerList)
            {
                wordPlatePlayer.SetHandTileList(m_WordPlateHandList[wordPlatePlayer.index].wordPlateTileList);
                if (wordPlatePlayer.zhuangLeisureType == ZhuangLeisureType.Zhuang)
                {
                    currentShowCard = wordPlatePlayer.index;
                    wordPlatePlayer.housePlayerStatus = WordPlatePlayerStatus.WordPlateWaitCard;
                }
                else
                {
                    wordPlatePlayer.housePlayerStatus = WordPlatePlayerStatus.WordPlateFree;
                }
                //局数
                WordPlatePlayerBureau playerBureau = new WordPlatePlayerBureau();
                playerBureau.playerIndex = wordPlatePlayer.index;
                houseBureau.playerBureauList.Add(playerBureau);
            }
            InitPlayerBureau(houseBureau);       
        }
        public bool CheckSettlementWordPlates()
        {
            if (!CheckPlayerFull())
            {
                //人不够
                ServerUtil.RecordLog(LogType.Debug, "CheckSettlementWordPlates, 人不够!");
                return false;
            }
            if (houseStatus != WordPlateHouseStatus.EWPS_Settlement)
            {
                return false;
            }
            if (currentBureau > maxBureau)
            {
                //局数满啦
                ServerUtil.RecordLog(LogType.Error, "CheckSettlementWordPlates, 局数满啦!");
                return false;
            }
            bool bHavePlayerWin = GetWordPlatePlayer().Exists(element => element.housePlayerStatus == WordPlatePlayerStatus.WordPlateWinCard);
            if (!bLiuJu && (endGodTile == null || !bHavePlayerWin))
            {
                //既不是流局 也没人胡牌或者结束神牌为空
                ServerUtil.RecordLog(LogType.Error, "CheckSettlementWordPlates, 既不是流局 也没人胡牌或者结束神牌为空!");
                return false;
            }
            if (bLiuJu && bHavePlayerWin)
            {
                //流局还有人胡
                ServerUtil.RecordLog(LogType.Error, "CheckSettlementWordPlates, 流局还有人胡!");
                return false;
            }
            return true;
        }
        public bool SettlementWordPlates(WordPlateSettlementNode wordPlateSettlement)
        {
            if (bLiuJu)
            {
                //流局
                WordPlatePlayer lastPlayer = GetWordPlatePlayer(giveOffPlayerIndex);
                if (lastPlayer != null)
                {
                    SetHouseZhuang(lastPlayer);
                }
                return true;
            }
            WordPlateHouseBureau houseBureau = houseBureauList.Find(element => element.bureau == (ulong)currentBureau);                                                                                                                                                
            if (houseBureau == null)
            {
                ServerUtil.RecordLog(LogType.Error, "SettlementWordPlates, houseBureau == null!");
                return false;
            }
            WordPlatePlayer winPlayer = GetWordPlatePlayer().Find(element => element.housePlayerStatus == WordPlatePlayerStatus.WordPlateWinCard);
            if (winPlayer == null)
            {
                //胡牌的人找不到了
                ServerUtil.RecordLog(LogType.Error, "SettlementWordPlates, 胡牌的人找不到了!");
                return false;
            }  
            if (winPlayer.CheckHuPlateMeld())
            {
                //胡牌牌组统计有误
                ServerUtil.RecordLog(LogType.Error, "SettlementWordPlates, 胡牌牌组统计有误!");
                return false;
            }
            WordPlatePlayerBureau winPlayerBureau = houseBureau.playerBureauList.Find(element => element.playerIndex == winPlayer.index);
            if (winPlayerBureau == null)
            {
                ServerUtil.RecordLog(LogType.Error, "SettlementWordPlates, winPlayerBureau == null!");
                return false;
            }
            //神腰
            if (endGodTile != null && beginGodType == (int)endGodTile.GetNumType() % 2)
            {
                wordPlateSettlement.wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_GodPlate, fanCount = 2 });
            }   
            //海底胡
            if (0 == remainWordPlateList.Count)
            {
                wordPlateSettlement.wordPlateFanList.Add(new WordPlateFanNode { fanType = WordPlateFanType.EWPF_SeabedHu, fanCount = 4 });
            }
            WordPlateTile huTile = null;
            if(!winPlayer.IsWaiHu(currentWordPlate, currentWhoPlay == winPlayer.index))
            {
                huTile = currentWordPlate;
            }
            WordPlateWinSorce wpWinSorce = winPlayer.GetWordPlateMeldScoreAndFan(wordPlateSettlement.wordPlateFanList, huTile, CheckHousePropertyType(WordPlateHousePropertyType.EMHP_Famous),
                CheckHousePropertyType(WordPlateHousePropertyType.EMHP_BigSmallHu), CheckHousePropertyType(WordPlateHousePropertyType.EMHP_BaoTing));
            if (wpWinSorce == null || wpWinSorce.winBaseSorce < baseWinScore || wpWinSorce.winAllSorce < baseWinScore)
            {
                //胡牌息计算有误
                ServerUtil.RecordLog(LogType.Error, "SettlementWordPlates, 胡牌息计算有误!");
                return false;
            }
            //有没有超过最大胡息
            if (maxWinScore > 0 && wpWinSorce.winAllSorce > maxWinScore)
            {
                wpWinSorce.winAllSorce = maxWinScore;
            }
            //算分
            int winIntegral = 0;
            GetWordPlatePlayer().ForEach(losePlayer =>
            {
                if (losePlayer.index != winPlayer.index)
                {
                    WordPlatePlayerBureau losePlayerBureau = houseBureau.playerBureauList.Find(element => element.playerIndex == losePlayer.index);
                    if (losePlayerBureau != null)
                    {
                        //保存积分
                        losePlayerBureau.bureauIntegral -= wpWinSorce.winAllSorce;
                        losePlayer.allIntegral -= wpWinSorce.winAllSorce;
                        winIntegral += wpWinSorce.winAllSorce;
                        //统计积分
                        wordPlateSettlement.wordPlatePlayerList.Add(new WordPlatePlayerSettlementNode { playerIndex = losePlayer.index, bureauIntegral = losePlayerBureau.bureauIntegral, allIntegral = losePlayer.allIntegral });
                    }
                }
            });
            //自己算分
            winPlayerBureau.bureauIntegral += winIntegral;
            winPlayer.m_nAllWinScore += wpWinSorce.winAllSorce;
            winPlayer.m_nWinAmount += 1;
            winPlayer.allIntegral += winIntegral;
            //统计积分
            wordPlateSettlement.wordPlatePlayerList.Add(new WordPlatePlayerSettlementNode { playerIndex = winPlayer.index, bureauIntegral = winPlayerBureau.bureauIntegral, allIntegral = winPlayer.allIntegral });
            //胡牌的人做庄
            SetHouseZhuang(winPlayer);

            return true;
        }
        public void SetZhuangPlayer(int index)
        {
            WordPlatePlayer player = GetWordPlatePlayer(index);
            if (player != null)
            {
                player.zhuangLeisureType = ZhuangLeisureType.Zhuang;
            }
        }
    }
}
#endif
