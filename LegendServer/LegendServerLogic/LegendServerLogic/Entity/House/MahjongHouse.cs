#if MAHJONG
using LegendProtocol;
using System.Collections.Generic;
using LegendServerLogic.Actor.Summoner;
using LegendServerLogic.Entity.Base;
using System.Linq;
using System;
using LegendServerLogic.Mahjong;
using LegendServerLogic.Entity.Players;

namespace LegendServerLogic.Entity.Houses
{
    public class MahjongHouse : House
    {
        //房间属性类型
        public int housePropertyType;
        //当前麻将类型
        public MahjongType mahjongType;
        //当前抓几个鸟
        public int catchBird;
        //飘(0-不飘,1-飘1分,2-飘2分)
        public int flutter;
        //房间状态
        public MahjongHouseStatus houseStatus;
        //等待操作类型
        public MahjongSpecialType mahjongSpecialType;
        //杠积分是否有变化
        public bool bKongInChange;
        //是否判断假将
        public bool bFakeHu;
        //是否只胡假将胡
        public bool bOnlyFakeHu;
        //当前出牌(长沙麻将会杠2个牌出来)
        public List<MahjongTile> currentMahjongList;
        //抢杠胡列表
        public List<MahjongSelectNode> kongHuPlayerList;
        //当局剩余的牌
        private List<MahjongTile> remainMahjongList;
        //操作类
        private List<MahjongOperatNode> mahjongOperatList;
        //每局的信息
        public List<MahjongHouseBureau> houseBureauList;
        // 麻将逻辑策略
        private MahjongSetTileBase m_setTile;
        // 发牌临时变量
        private Dictionary<int, MahjongHandTile> m_MahjongHandList;
        // 房间临时变量
        private List<MahjongPlayer> m_MahjongPlayerList;
        public MahjongHouse()
        {
            houseCardId = 0;
            houseId = 0;
            maxBureau = 0;
            businessId = 0;
            currentBureau = 0;
            currentWhoPlay = -1;
            currentShowCard = -1;
            housePropertyType = 0;
            competitionKey = 0;
            maxPlayerNum = MahjongConstValue.MahjongFourPlayer;
            houseType = HouseType.MahjongHouse;
            mahjongSpecialType = MahjongSpecialType.EMS_None;
            catchBird = 0;
            flutter = 0;
            bKongInChange = false;
            bFakeHu = false;
            bOnlyFakeHu = false;
            kongHuPlayerList = new List<MahjongSelectNode>();
            currentMahjongList = new List<MahjongTile>();
            remainMahjongList = new List<MahjongTile>();
            mahjongOperatList = new List<MahjongOperatNode>();
            summonerList = new List<Player>();
            houseBureauList = new List<MahjongHouseBureau>();
            m_MahjongHandList = new Dictionary<int, MahjongHandTile>();
            m_MahjongPlayerList = new List<MahjongPlayer>();
            voteBeginTime = DateTime.Parse("1970-01-01 00:00:00");
            operateBeginTime = DateTime.Parse("1970-01-01 00:00:00");
        }
        public bool SetMahjongSetTile(MahjongType mahjongType)
        {
            MahjongSetTileBase setTile = MahjongManager.Instance.GetMahjongSetTile(mahjongType);
            if (setTile == null)
            {
                return true;
            }
            m_setTile = setTile;

            return false;
        }
        public MahjongPlayer CreatHouse(Summoner summoner, MahjongPlayerStatus housePlayerStatus = MahjongPlayerStatus.MahjongFree)
        {
            MahjongStrategyBase strategy = MahjongManager.Instance.GetMahjongStrategy(mahjongType);
            if (strategy == null)
            {
                return null;
            }
            //绑定玩家
            MahjongPlayer newHousePlayer = new MahjongPlayer(strategy);
            newHousePlayer.index = 0;
            newHousePlayer.userId = summoner.userId;
            newHousePlayer.nickName = summoner.nickName;
            newHousePlayer.summonerId = summoner.id;
            newHousePlayer.sex = summoner.sex;
            newHousePlayer.ip = summoner.ip;
            newHousePlayer.proxyServerId = summoner.proxyServerId;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.m_b7Pairs = CheckHousePropertyType(MahjongHousePropertyType.EMHP_Hu7Pairs);
            newHousePlayer.m_bOpenFakeHu = CheckHousePropertyType(MahjongHousePropertyType.EMHP_FakeHu);
            newHousePlayer.m_bPersonalisePendulum = CheckHousePropertyType(MahjongHousePropertyType.EMHP_PersonalisePendulum);
            summonerList.Add(newHousePlayer);
            m_MahjongPlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public MahjongPlayer CreatHouse(PlayerInfo playerInfo, MahjongPlayerStatus housePlayerStatus = MahjongPlayerStatus.MahjongReady)
        {
            MahjongStrategyBase strategy = MahjongManager.Instance.GetMahjongStrategy(mahjongType);
            if (strategy == null)
            {
                return null;
            }
            //绑定玩家
            MahjongPlayer newHousePlayer = new MahjongPlayer(strategy);
            newHousePlayer.index = 0;
            newHousePlayer.userId = playerInfo.userId;
            newHousePlayer.nickName = playerInfo.nickName;
            newHousePlayer.summonerId = playerInfo.summonerId;
            newHousePlayer.sex = playerInfo.sex;
            newHousePlayer.ip = playerInfo.ip;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.m_b7Pairs = CheckHousePropertyType(MahjongHousePropertyType.EMHP_Hu7Pairs);
            newHousePlayer.m_bOpenFakeHu = CheckHousePropertyType(MahjongHousePropertyType.EMHP_FakeHu);
            newHousePlayer.m_bPersonalisePendulum = CheckHousePropertyType(MahjongHousePropertyType.EMHP_PersonalisePendulum);
            summonerList.Add(newHousePlayer);
            m_MahjongPlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public MahjongPlayer AddPlayer(Summoner summoner, MahjongPlayerStatus housePlayerStatus = MahjongPlayerStatus.MahjongFree)
        {
            MahjongStrategyBase strategy = MahjongManager.Instance.GetMahjongStrategy(mahjongType);
            if (strategy == null)
            {
                return null;
            }
            //绑定玩家
            MahjongPlayer newHousePlayer = new MahjongPlayer(strategy);
            newHousePlayer.index = GetHousePlayerIndex();
            newHousePlayer.userId = summoner.userId;
            newHousePlayer.nickName = summoner.nickName;
            newHousePlayer.summonerId = summoner.id;
            newHousePlayer.sex = summoner.sex;
            newHousePlayer.ip = summoner.ip;
            newHousePlayer.proxyServerId = summoner.proxyServerId;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.m_b7Pairs = CheckHousePropertyType(MahjongHousePropertyType.EMHP_Hu7Pairs);
            newHousePlayer.m_bOpenFakeHu = CheckHousePropertyType(MahjongHousePropertyType.EMHP_FakeHu);
            newHousePlayer.m_bPersonalisePendulum = CheckHousePropertyType(MahjongHousePropertyType.EMHP_PersonalisePendulum);
            summonerList.Add(newHousePlayer);
            m_MahjongPlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public MahjongPlayer AddPlayer(PlayerInfo playerInfo, MahjongPlayerStatus housePlayerStatus = MahjongPlayerStatus.MahjongReady)
        {
            MahjongStrategyBase strategy = MahjongManager.Instance.GetMahjongStrategy(mahjongType);
            if (strategy == null)
            {
                return null;
            }
            //绑定玩家
            MahjongPlayer newHousePlayer = new MahjongPlayer(strategy);
            newHousePlayer.index = GetHousePlayerIndex();
            newHousePlayer.userId = playerInfo.userId;
            newHousePlayer.nickName = playerInfo.nickName;
            newHousePlayer.summonerId = playerInfo.summonerId;
            newHousePlayer.sex = playerInfo.sex;
            newHousePlayer.ip = playerInfo.ip;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.m_b7Pairs = CheckHousePropertyType(MahjongHousePropertyType.EMHP_Hu7Pairs);
            newHousePlayer.m_bOpenFakeHu = CheckHousePropertyType(MahjongHousePropertyType.EMHP_FakeHu);
            newHousePlayer.m_bPersonalisePendulum = CheckHousePropertyType(MahjongHousePropertyType.EMHP_PersonalisePendulum);
            summonerList.Add(newHousePlayer);
            m_MahjongPlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public void AddPlayer(MahjongHousePlayerNode playerNode, MahjongStrategyBase strategy, MahjongPlayerStatus housePlayerStatus = MahjongPlayerStatus.MahjongFree)
        {
            //绑定玩家
            MahjongPlayer newHousePlayer = new MahjongPlayer(strategy);
            newHousePlayer.index = playerNode.playerIndex;
            newHousePlayer.userId = playerNode.userId;
            newHousePlayer.nickName = playerNode.nickName;
            newHousePlayer.summonerId = playerNode.summonerId;
            newHousePlayer.sex = playerNode.sex;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.zhuangLeisureType = playerNode.zhuangLeisureType;
            newHousePlayer.m_BigWinMyself = playerNode.bigWinMyself;
            newHousePlayer.m_SmallWinMyself = playerNode.smallWinMyself;
            newHousePlayer.m_BigWinFangBlast = playerNode.bigWinFangBlast;
            newHousePlayer.m_SmallWinFangBlast = playerNode.smallWinFangBlast;
            newHousePlayer.m_BigWinJieBlast = playerNode.bigWinJieBlast;
            newHousePlayer.m_SmallWinJieBlast = playerNode.smallWinJieBlast;
            newHousePlayer.allIntegral = playerNode.allIntegral;
            newHousePlayer.lineType = LineType.OffLine;
            newHousePlayer.m_b7Pairs = CheckHousePropertyType(MahjongHousePropertyType.EMHP_Hu7Pairs);
            newHousePlayer.m_bOpenFakeHu = CheckHousePropertyType(MahjongHousePropertyType.EMHP_FakeHu);
            newHousePlayer.m_bPersonalisePendulum = CheckHousePropertyType(MahjongHousePropertyType.EMHP_PersonalisePendulum);

            summonerList.Add(newHousePlayer);
            m_MahjongPlayerList.Add(newHousePlayer);
        }
        public override MahjongHouseStatus GetMahjongHouseStatus()
        {
            return houseStatus;
        }
        //设置GL剩余的牌
        public void GetRemainMahjongList(List<int> mahjongList)
        {
            foreach (MahjongTile mahjongTile in remainMahjongList)
            {
                mahjongList.Add(mahjongTile.GetMahjongNode());
            }
        }    
        //设置GL当前的牌
        public void GetCurrentMahjongList(List<int> mahjongList)
        {
            foreach (MahjongTile mahjongTile in currentMahjongList)
            {
                mahjongList.Add(mahjongTile.GetMahjongNode());
            }
        }
        //设置剩余的牌
        public void SetRemainMahjongList(List<int> mahjongList)
        {
            remainMahjongList.Clear();
            foreach (int mahjongNode in mahjongList)
            {
                remainMahjongList.Add(new MahjongTile(mahjongNode));
            }
        }
        //判断房间属性
        public bool CheckHousePropertyType(MahjongHousePropertyType type)
        {
            if ((int)type == (housePropertyType & (int)type))
            {
                return true;
            }
            return false;
        }
        //设置当前的牌
        public void SetCurrentMahjongList(List<int> mahjongList)
        {
            currentMahjongList.Clear();
            foreach (int mahjongNode in mahjongList)
            {
                currentMahjongList.Add(new MahjongTile(mahjongNode));
            }
        }
        public bool CheckPlayerOperat(int playerIndex)
        {
            if (currentShowCard == -1)
            {
                //普通操作
                MahjongOperatNode operatNode = mahjongOperatList.Find(element => element.playerIndex == playerIndex);
                if (operatNode != null && operatNode.bWait && operatNode.operatedType != MahjongOperatType.EMO_None)
                {
                    return true;
                }
            }
            else if(currentShowCard == -2)
            {
                //抢杠胡操作
                if (kongHuPlayerList.Exists(element => element.playerIndex == playerIndex && element.bWait))
                {
                    return true;
                }
            }
            return false;
        }
        public void AddMahjongOperat(int playerIndex, MahjongOperatType operatType)
        {
            if (operatType == MahjongOperatType.EMO_None)
            {
                return;
            }
            MahjongOperatNode operatNode = mahjongOperatList.Find(element => element.playerIndex == playerIndex);
            if (operatNode == null)
            {
                operatNode = new MahjongOperatNode();
                operatNode.playerIndex = playerIndex;
                operatNode.operatedType = operatType;
                operatNode.defaultOperatType = operatType;
                mahjongOperatList.Add(operatNode);
            }
            if (operatType > operatNode.operatedType)
            {
                operatNode.operatedType = operatType;
                operatNode.defaultOperatType = operatType;
            }
            operatNode.bWait = true;
        }
        public void ClearMahjongOperat()
        {
            mahjongOperatList.Clear();
        }
        public List<MahjongOperatNode> GetMahjongOperat()
        {
            return mahjongOperatList;
        }
        public void SetMahjongOperat(List<MahjongOperatNode> _mahjongOperatList)
        {
            mahjongOperatList.Clear();
            mahjongOperatList.AddRange(_mahjongOperatList);
        }
        public MahjongOperatType GetPlayerMahjongOperatType(int playerIndex)
        {
            MahjongOperatNode operatNode = mahjongOperatList.Find(element => element.playerIndex == playerIndex);
            if (operatNode != null && operatNode.bWait)
            {
                return operatNode.operatedType;
            }
            return MahjongOperatType.EMO_None;
        }
        public bool SetMahjongOperat(int playerIndex, MahjongOperatType operatType, List<int> operatMahjonList)
        {
            MahjongOperatNode operatNode = mahjongOperatList.Find(element => element.playerIndex == playerIndex);
            if (operatNode != null && operatNode.bWait)
            {
                operatNode.operatedType = operatType;
                operatNode.operatMahjonList.Clear();
                operatNode.operatMahjonList.AddRange(operatMahjonList);
                operatNode.bWait = false;
                return true;
            }
            return false;
        }
        public List<MahjongOperatNode> GetPlayerMahjongOperat()
        {
            MahjongOperatType operatType = mahjongOperatList.Max(element => element.operatedType);
            if (operatType == MahjongOperatType.EMO_Kong)
            {
                //杠跟碰的优先级一样(nimeide)
                return mahjongOperatList.FindAll(element => ((element.operatedType == operatType || element.operatedType == MahjongOperatType.EMO_Pong) && element.operatedType != MahjongOperatType.EMO_None));
            }
            return mahjongOperatList.FindAll(element => (element.operatedType == operatType && element.operatedType != MahjongOperatType.EMO_None));
        }
        public MahjongOperatNode GetPlayerMahjongOperatNode()
        {
            List<MahjongOperatNode> _mahjongOperatList = mahjongOperatList.FindAll(element => element.bWait && element.operatedType != MahjongOperatType.EMO_None);
            if(_mahjongOperatList != null && _mahjongOperatList.Count > 0)
            {
                MahjongOperatType operatType = _mahjongOperatList.Max(element => element.operatedType);
                return _mahjongOperatList.Find(element => (element.operatedType == operatType));
            }
            return null;
        }
        public List<MahjongOperatNode> GetPlayerMahjongOperatHu()
        {
            return mahjongOperatList.FindAll(element => element.defaultOperatType == MahjongOperatType.EMO_Hu);
        }
        public bool CheckCurrentMahjong(List<int> mahjongList)
        {
            if (mahjongList == null || mahjongList.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < mahjongList.Count; ++i)
            {
                if (!CheckCurrentMahjong(mahjongList[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public bool CheckCurrentMahjong(int mahjongNode)
        {
            if (mahjongNode == 0)
            {
                return false;
            }
            return CheckCurrentMahjong(new MahjongTile(mahjongNode));
        }
        public bool CheckCurrentMahjong(MahjongTile mahjongTile)
        {
            if (mahjongTile == null)
            {
                return false;
            }
            return currentMahjongList.Exists(element => element.Equal(mahjongTile));
        }
        public List<MahjongPlayer> GetMahjongPlayer()
        {
            if (m_MahjongPlayerList.Count != summonerList.Count)
            {
                m_MahjongPlayerList.Clear();
                foreach (MahjongPlayer mahjongPlayer in summonerList)
                {
                    m_MahjongPlayerList.Add(mahjongPlayer);
                }
            }
            return m_MahjongPlayerList;
        }
        public List<MahjongPlayer> GetOtherMahjongPlayer(string userId)
        {
            return GetMahjongPlayer().FindAll(element => element.userId != userId);
        }
        public MahjongPlayer GetMahjongPlayer(string userId)
        {
            return GetMahjongPlayer().FirstOrDefault(element => element.userId == userId);
        }
        public MahjongPlayer GetMahjongPlayer(int index)
        {
            return GetMahjongPlayer().FirstOrDefault(element => element.index == index);
        }
        public MahjongPlayer GetMahjongPlayerBySummonerId(ulong summonerId)
        {
            return GetMahjongPlayer().FirstOrDefault(element => element.summonerId == summonerId);
        }
        public MahjongPlayer GetHouseOwner()
        {
            return GetMahjongPlayer(0);
        }
        public MahjongPlayer GetHouseZhuang()
        {
            return GetMahjongPlayer().FirstOrDefault(element => element.zhuangLeisureType == ZhuangLeisureType.Zhuang);
        }
        public int GetHouseZhuangIndex()
        {
            MahjongPlayer zhuangPlayer = GetMahjongPlayer().FirstOrDefault(element => element.zhuangLeisureType == ZhuangLeisureType.Zhuang);
            if (zhuangPlayer != null)
            {
                return zhuangPlayer.index;
            }
            return 0;
        }
        public List<MahjongPlayer> GetMahjongPlayersByCondition(Func<MahjongPlayer, bool> condition)
        {
            return GetMahjongPlayer().Where(condition).ToList();
        }
        public MahjongPlayer GetMahjongPlayerByCondition(Func<MahjongPlayer, bool> condition)
        {
            return GetMahjongPlayer().FirstOrDefault(condition);
        }
        public void SetHouseZhuang(MahjongPlayer player)
        {
            if (player.zhuangLeisureType != ZhuangLeisureType.Zhuang)
            {
                MahjongPlayer zhuangPlayer = GetHouseZhuang();
                if (zhuangPlayer != null)
                {
                    zhuangPlayer.zhuangLeisureType = ZhuangLeisureType.Leisure;
                }
                player.zhuangLeisureType = ZhuangLeisureType.Zhuang;
            }
        }
        public MahjongPlayer GetMahjongVoteLaunchPlayer()
        {
            return GetMahjongPlayer().Find(element => element.voteStatus == VoteStatus.LaunchVote);
        }
        public MahjongPlayer GetNextHousePlayer(int index)
        {
            int nextIndex = GetNextHousePlayerIndex(index);
            return GetMahjongPlayer(nextIndex);
        }
        public MahjongPlayer GetLastHousePlayer(int index)
        {
            int lastIndex = GetLastHousePlayerIndex(index);
            return GetMahjongPlayer(lastIndex);
        }
        public void SetFakeHu(bool fakeHu = true)
        {
            if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_FakeHu) && bFakeHu != fakeHu)
            {
                bFakeHu = fakeHu;
            }
        }
        public List<MahjongTile> GetRemainMahjong()
        {
            return remainMahjongList;
        }
        public void InitRemainMahjong()
        {
            remainMahjongList.Clear();
        }
        public int GetRemainMahjongCount()
        {
            return remainMahjongList.Count;
        }
        public int GetHouseSelectSeabedCount()
        {
            if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_DoubleSeabed))
            {
                return 2;
            }
            return 1;
        }
        public int GetHouseKongCount()
        {
            if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_DoubleKong))
            {
                return 4;
            }
            return 2;
        }
        public MahjongTile GetNewTileByRemainMahjong()
        {
            int remainMahjongCount = remainMahjongList.Count;
            if (remainMahjongCount == 0)
            {
                return null;
            }
            int index = 0;
            if (remainMahjongCount > 1)
            {
                index = MyRandom.NextPrecise(0, remainMahjongCount);
            }
            MahjongTile tile = remainMahjongList[index];
            MahjongTile newTile = new MahjongTile(tile);
            remainMahjongList.Remove(tile);

            return newTile;
        }
        public void InitMahjongHouse()
        {
            mahjongSpecialType = MahjongSpecialType.EMS_None;
            bKongInChange = false;
            bFakeHu = false;
            bOnlyFakeHu = false;
            mahjongOperatList.Clear();
            currentMahjongList.Clear();
            kongHuPlayerList.Clear();
            remainMahjongList.Clear();
        }
        public void InitPlayerBureau(MahjongHouseBureau houseBureau)
        {
            MahjongHouseBureau _houseBureau = houseBureauList.Find(element => element.bureau == houseBureau.bureau);
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
        public MahjongHouseBureau GetHouseBureau()
        {
            return houseBureauList.Find(element => element.bureau == (ulong)currentBureau);
        }
        public MahjongPlayerBureau GetHousePlayerBureau(int playerIndex)
        {
            MahjongHouseBureau houseBureau = GetHouseBureau();
            if (houseBureau != null)
            {
                return houseBureau.playerBureauList.Find(element => element.playerIndex == playerIndex);
            }
            return null;
        }
        public void SetPlayerBureauStartDisplay(int playerIndex, int startDisplayIntegral, CSStartDisplayType startDisplayType = CSStartDisplayType.SDT_None)
        {
            MahjongPlayerBureau housePlayerBureau = GetHousePlayerBureau(playerIndex);
            if (housePlayerBureau != null)
            {
                if (startDisplayType != CSStartDisplayType.SDT_None)
                {
                    housePlayerBureau.startDisplayType |= (int)startDisplayType;
                }
                housePlayerBureau.bureauIntegral += startDisplayIntegral;
            }
        }
        public void SetPlayerBureauMidwayPendulum(int playerIndex)
        {
            MahjongPlayerBureau housePlayerBureau = GetHousePlayerBureau(playerIndex);
            if (housePlayerBureau != null)
            {
                housePlayerBureau.midwayPendulum += 1;
            }
        }
        public void SetPlayerBureauFangKong(int playerIndex, int fangKongCount = 1)
        {
            MahjongPlayerBureau housePlayerBureau = GetHousePlayerBureau(playerIndex);
            if (housePlayerBureau != null)
            {
                housePlayerBureau.startDisplayType += fangKongCount;
            }
        }
        public void SetPlayerBureauKongIntegral(int playerIndex, int kongIntegral)
        {
            MahjongPlayerBureau housePlayerBureau = GetHousePlayerBureau(playerIndex);
            if (housePlayerBureau != null)
            {
                housePlayerBureau.bureauIntegral += kongIntegral;
            }
        }
        public void SetPlayerBureauBirdNumber(int playerIndex, int winBirdNumber)
        {
            MahjongPlayerBureau housePlayerBureau = GetHousePlayerBureau(playerIndex);
            if (housePlayerBureau != null)
            {
                housePlayerBureau.winBirdNumber += winBirdNumber;
            }
        }
        public void GetPlayerSettlementList(List<MahjongSettlementNode> playerMahjongList)
        {
            foreach (MahjongPlayer mahjongPlayer in summonerList)
            {
                MahjongPlayerBureau housePlayerBureau = GetHousePlayerBureau(mahjongPlayer.index);
                if (housePlayerBureau != null)
                {
                    MahjongSettlementNode newNode = new MahjongSettlementNode();
                    newNode.playerIndex = housePlayerBureau.playerIndex;
                    newNode.startDisplayType = housePlayerBureau.startDisplayType;
                    newNode.mahjongWinType = housePlayerBureau.mahjongWinType;
                    newNode.bureauIntegral = housePlayerBureau.bureauIntegral;
                    newNode.winBirdNumber = housePlayerBureau.winBirdNumber;
                    newNode.midwayPendulum = housePlayerBureau.midwayPendulum;
                    newNode.allIntegral = mahjongPlayer.allIntegral;
                    if (mahjongPlayer.housePlayerStatus != MahjongPlayerStatus.MahjongWinCard)
                    {
                        mahjongPlayer.GetPlayerHandTileList(newNode.mahjongList);
                    }
                    newNode.bIntegralCapped = mahjongPlayer.bIntegralCapped;
                    playerMahjongList.Add(newNode);
                }
            }
        }
        public List<MahjongEndSettlementNode> GetMahjongEndSettlementList()
        {
            List<MahjongEndSettlementNode> mahjongEndSettlementList = new List<MahjongEndSettlementNode>();
            foreach (MahjongPlayer housePlayer in summonerList)
            {
                MahjongEndSettlementNode mahjongEndSettlementNode = new MahjongEndSettlementNode();
                mahjongEndSettlementNode.index = housePlayer.index;
                mahjongEndSettlementNode.bigWinMyself = housePlayer.m_BigWinMyself;
                mahjongEndSettlementNode.smallWinMyself = housePlayer.m_SmallWinMyself;
                mahjongEndSettlementNode.bigWinFangBlast = housePlayer.m_BigWinFangBlast;
                mahjongEndSettlementNode.smallWinFangBlast = housePlayer.m_SmallWinFangBlast;
                mahjongEndSettlementNode.bigWinJieBlast = housePlayer.m_BigWinJieBlast;
                mahjongEndSettlementNode.smallWinJieBlast = housePlayer.m_SmallWinJieBlast;
                mahjongEndSettlementNode.allIntegral = housePlayer.allIntegral;
                mahjongEndSettlementList.Add(mahjongEndSettlementNode);
            }
            return mahjongEndSettlementList;
        }
        public PlayerWinMahjongNode GetHousePlayerTile(int playerIndex, List<int> operatMahjonList, bool fakeHu)
        {
            MahjongPlayer mahjongPlayer = GetMahjongPlayer(playerIndex);
            if (mahjongPlayer == null)
            {
                return null;
            }
            PlayerWinMahjongNode playerTileNode = new PlayerWinMahjongNode();
            playerTileNode.playerIndex = mahjongPlayer.index;
            mahjongPlayer.GetPlayerHandTileList(playerTileNode.handMahjongList);
            if (operatMahjonList.Count > 0)
            {
                playerTileNode.winMahjongList.AddRange(operatMahjonList);
            }
            else
            {
                //因为催胡，玩家没有选择自己胡的牌，只能系统自己判断
                currentMahjongList.ForEach(mahjongTile =>
                {
                    if (CSSpecialWinType.WT_None != mahjongPlayer.WinHandTileCheck(mahjongTile, bFakeHu))
                    {
                        playerTileNode.winMahjongList.Add(mahjongTile.GetMahjongNode());
                    }
                });
            }

            return playerTileNode;
        }
        public bool CheckPlayerSeabedWin()
        {
            if (mahjongSpecialType == MahjongSpecialType.EMS_Seabed && 0 == GetRemainMahjongCount() && currentMahjongList.Count == GetHouseSelectSeabedCount())
            {
                return true;
            }
            return false;
        }
        public bool CheckPlayerKongWin(int playerIndex)
        {
            if (kongHuPlayerList.Count == 0 || mahjongSpecialType == MahjongSpecialType.EMS_None || mahjongSpecialType == MahjongSpecialType.EMS_Seabed)
            {
                return false;
            }
            return kongHuPlayerList.Exists(element => element.playerIndex == playerIndex && element.bWait);
        }
        public MahjongKongType SetPlayerKongMahjong(MahjongPlayer player, List<int> operatMahjonList)
        {
            MahjongKongType kongType = MahjongKongType.EMK_None;
            MahjongSpecialType specialType = MahjongSpecialType.EMS_None;
            if (player == null || operatMahjonList == null || operatMahjonList.Count == 0 || operatMahjonList.Count > 2)
            {
                return kongType;
            }
            if (operatMahjonList.Count == 2)
            {
                kongType = MahjongKongType.EMK_Kong;
                specialType = MahjongSpecialType.EMS_Kong;
            }
            else
            {
                kongType = MahjongKongType.EMK_MakeUp;
                specialType = MahjongSpecialType.EMS_MakeUp;
            }
            //判断抢杠胡(长沙麻将或者选择抢杠胡)
            //if(CheckHousePropertyType(MahjongHousePropertyType.EMHP_GrabKongHu))//--因为抢杠胡选择是后期才加的，为照顾到线上的长沙麻将没有加此选择，后期版本这个选择也会加进去
            if (mahjongType == MahjongType.ChangShaMahjong || CheckHousePropertyType(MahjongHousePropertyType.EMHP_GrabKongHu))
            {
                List<MahjongSelectNode> _kongHuPlayerList = new List<MahjongSelectNode>();
                if (specialType == MahjongSpecialType.EMS_Kong)
                {
                    SetFakeHu();
                }
                GetMahjongPlayer().ForEach(housePlayer =>
                {
                    if (housePlayer.userId != player.userId && !housePlayer.m_bGiveUpWin && CSSpecialWinType.WT_None != housePlayer.WinHandTileCheck(operatMahjonList[0], bFakeHu))
                    {
                        _kongHuPlayerList.Add(new MahjongSelectNode { playerIndex = housePlayer.index, bWait = true });
                    }
                });
                if (_kongHuPlayerList.Count > 0)
                {
                    currentShowCard = -2;
                    currentWhoPlay = player.index;
                    currentMahjongList.Clear();
                    currentMahjongList.Add(new MahjongTile(operatMahjonList[0]));

                    mahjongSpecialType = specialType;
                    kongHuPlayerList.Clear();
                    kongHuPlayerList.AddRange(_kongHuPlayerList);
                }
                else if (specialType == MahjongSpecialType.EMS_Kong)
                {
                    mahjongSpecialType = specialType;
                    SetFakeHu(false);
                }
            }
            return kongType;
        }
        public void SetPlayerKongMahjongByFakeHu(int kongPlayerIndex, int kongMahjong)
        {
            if (!CheckHousePropertyType(MahjongHousePropertyType.EMHP_FakeHu) || kongPlayerIndex == currentWhoPlay)
            {
                //没有开启假将胡或者自己杠自己的
                ServerUtil.RecordLog(LogType.Debug, "SetPlayerKongMahjongByFakeHu, 没有开启假将胡或者自己杠自己的");
                return;
            }
            List<MahjongSelectNode> _kongHuPlayerList = new List<MahjongSelectNode>();
            GetMahjongPlayer().ForEach(housePlayer =>
            {
                if (housePlayer.index != kongPlayerIndex && !housePlayer.m_bGiveUpWin && CSSpecialWinType.WT_FakeHu == housePlayer.WinHandTileCheck(kongMahjong, true))
                {
                    _kongHuPlayerList.Add(new MahjongSelectNode { playerIndex = housePlayer.index, bWait = true });
                }
            });
            if (_kongHuPlayerList.Count > 0)
            {
                currentShowCard = -2;
                currentWhoPlay = kongPlayerIndex;
                currentMahjongList.Clear();
                currentMahjongList.Add(new MahjongTile(kongMahjong));
                
                kongHuPlayerList.Clear();
                kongHuPlayerList.AddRange(_kongHuPlayerList);

                SetFakeHu();
                bOnlyFakeHu = true;
            }
        }
        public MahjongKongType GetRecordMahjongKongType(MahjongKongType kongType, bool bNoInKong = false)
        {
            if (kongType == MahjongKongType.EMK_MakeUp && (mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongType == MahjongType.RedLaiZiMahjong))
            {
                if (bNoInKong)
                {
                    return MahjongKongType.EMK_NoInKong;
                }
                return MahjongKongType.EMK_InKong;
            }
            return kongType;
        }
        public bool CheckBeginMahjongs()
        {
            if (!CheckPlayerFull())
            {
                //人不够
                return false;
            }
            if (GetMahjongPlayer().Exists(element => element.housePlayerStatus != MahjongPlayerStatus.MahjongReady))
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
        public void BeginMahjongs(MahjongHouseBureau houseBureau, List<MahjongCountNode> mahjongCountList, List<int> csPendulumPlayerList)
        {
            houseStatus = MahjongHouseStatus.MHS_BeginBureau;
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
                mahjongSpecialType = MahjongSpecialType.EMS_None;
                bKongInChange = false;
                bFakeHu = false;
                bOnlyFakeHu = false;
                currentMahjongList.Clear();
                remainMahjongList.Clear();
                mahjongOperatList.Clear();
                kongHuPlayerList.Clear();
            }
            m_MahjongHandList.Clear();
            foreach (MahjongPlayer mahjongPlayer in summonerList)
            {
                m_MahjongHandList.Add(mahjongPlayer.index, new MahjongHandTile(mahjongPlayer.zhuangLeisureType));
            }
            //人数到齐可以开局
            m_setTile.InitMahjongTile(m_MahjongHandList, remainMahjongList);
            //初始化房间局数信息
            houseBureau.bureau = (ulong)currentBureau;
            houseBureau.bureauTime = DateTime.Now.ToString();
            foreach(MahjongPlayer mahjongPlayer in summonerList)
            {
                mahjongPlayer.SetHandTileList(m_MahjongHandList[mahjongPlayer.index].mahjongTileList);
                mahjongPlayer.housePlayerStatus = MahjongPlayerStatus.MahjongFree;
                if (mahjongType == MahjongType.ChangShaMahjong)
                {
                    //检测摆牌
                    CSStartDisplayType startDisplayType = mahjongPlayer.StartDisplay();
                    if (CSStartDisplayType.SDT_None != startDisplayType)
                    {
                        mahjongPlayer.housePlayerStatus = MahjongPlayerStatus.MahjongPendulum;
                        mahjongPlayer.startDisplayType = startDisplayType;
                        if (houseStatus != MahjongHouseStatus.MHS_PendulumBureau)
                        {
                            houseStatus = MahjongHouseStatus.MHS_PendulumBureau;
                        }
                        csPendulumPlayerList.Add(mahjongPlayer.index);
                    }
                }
                //统计手牌个数
                mahjongCountList.Add(new MahjongCountNode { playerIndex = mahjongPlayer.index, headMahjongCount = mahjongPlayer.GetHeadMahjongCount() });
                //局数
                MahjongPlayerBureau playerBureau = new MahjongPlayerBureau();
                playerBureau.playerIndex = mahjongPlayer.index;
                houseBureau.playerBureauList.Add(playerBureau);
            }
            InitPlayerBureau(houseBureau);       
        }
        public bool PendulumMahjongs()
        {
            if (houseStatus != MahjongHouseStatus.MHS_PendulumBureau)
            {
                return false;
            }
            if (GetMahjongPlayer().Exists(element => (element.startDisplayType != CSStartDisplayType.SDT_None || element.housePlayerStatus == MahjongPlayerStatus.MahjongPendulum || element.housePlayerStatus == MahjongPlayerStatus.MahjongPendulumDice)))
            {
                return false;
            }
            MahjongPlayer zhuangPlayer = GetHouseZhuang();
            if (zhuangPlayer == null || zhuangPlayer.zhuangLeisureType != ZhuangLeisureType.Zhuang)
            {
                return false;
            }

            houseStatus = MahjongHouseStatus.MHS_BeginBureau;
            currentShowCard = zhuangPlayer.index;
            zhuangPlayer.housePlayerStatus = MahjongPlayerStatus.MahjongWaitCard;

            return true;
        }
        public bool SettlementMahjongs()
        {
            if (!CheckPlayerFull())
            {
                //人不够
                ServerUtil.RecordLog(LogType.Debug, "SettlementMahjongs, 人不够!");
                return false;
            }
            if (houseStatus != MahjongHouseStatus.MHS_Settlement)
            {
                return false;
            }
            if (currentBureau > maxBureau)
            {
                //局数满啦
                ServerUtil.RecordLog(LogType.Error, "SettlementMahjongs, 局数满啦!");
                return false;
            }
            List<MahjongPlayer> winPlayerlist = GetMahjongPlayer().FindAll(element => element.housePlayerStatus == MahjongPlayerStatus.MahjongWinCard);
            if (winPlayerlist == null || winPlayerlist.Count <= 0)
            {
                //没人胡
                if (0 == GetRemainMahjongCount() || (GetHouseSelectSeabedCount() == GetRemainMahjongCount() && mahjongSpecialType == MahjongSpecialType.EMS_Seabed))
                {
                    //流局 
                    MahjongPlayer lastPlayer = GetMahjongPlayer(currentWhoPlay);
                    if (lastPlayer != null)
                    {
                        SetHouseZhuang(lastPlayer);
                    }
                    return true;
                }
                ServerUtil.RecordLog(LogType.Error, "SettlementMahjongs, 没人胡，又不是海底!");
                return false;
            }
            MahjongHouseBureau houseBureau = houseBureauList.Find(element => element.bureau == (ulong)currentBureau);
            if (houseBureau == null)
            {
                ServerUtil.RecordLog(LogType.Error, "SettlementMahjongs, houseBureau == null!");
                return false;
            }
            if (currentShowCard == -1)
            {
                //点炮
                if (!MahjongWinByBlast(winPlayerlist, houseBureau.playerBureauList))
                {
                    return false;
                }
            }
            else if (currentShowCard == -2)
            {
                //抢杠胡
                if (!MahjongWinByGrabKong(winPlayerlist, houseBureau.playerBureauList))
                {
                    return false;
                }
            }
            else
            {
                //自摸
                if (!MahjongWinByMyself(winPlayerlist, houseBureau.playerBureauList))
                {
                    return false;
                }
            }            

            return true;
        }
        //放炮
        public bool MahjongWinByBlast(List<MahjongPlayer> winPlayerlist, List<MahjongPlayerBureau> playerBureauList)
        {
            MahjongPlayer losePlayer = GetMahjongPlayer(currentWhoPlay);
            if (losePlayer == null)
            {
                //放炮的人找不到了
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByBlast, 放炮的人找不到了!");
                return false;
            }
            if (winPlayerlist.Exists(element => element.index == losePlayer.index))
            {
                //放炮的人怎么可能也是胡牌的
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByBlast, 放炮的人怎么可能也是胡牌的!");
                return false;
            }
            MahjongPlayerBureau losePlayerBureau = playerBureauList.Find(element => element.playerIndex == losePlayer.index);
            if (losePlayerBureau == null)
            {
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByBlast, losePlayerBureau == null!");
                return false;
            }
            //算分
            int lostIntegral = 0;
            foreach (MahjongPlayer winPlayer in winPlayerlist)
            {
                MahjongPlayerBureau winPlayerBureau = playerBureauList.Find(element => element.playerIndex == winPlayer.index);
                if (winPlayerBureau == null)
                {
                    ServerUtil.RecordLog(LogType.Error, "MahjongWinByBlast, winPlayerBureau == null!");
                    continue;
                }
                int multiple = 0;
                //胡牌类型
                CSSpecialWinType specialWinType = CSSpecialWinType.WT_None;
                foreach (MahjongTile mahjongTile in currentMahjongList)
                {
                    CSSpecialWinType _specialWinType = winPlayer.WinHandTileCheck(mahjongTile, bFakeHu);
                    if (CSSpecialWinType.WT_None == _specialWinType)
                    {
                        ServerUtil.RecordLog(LogType.Debug, "MahjongWinByBlast, CSSpecialWinType.WT_None == _specialWinType!");
                        continue;
                    }
                    if (mahjongType == MahjongType.ChangShaMahjong)
                    {
                        if (_specialWinType == CSSpecialWinType.WT_PingWin)
                        {
                            if (mahjongSpecialType == MahjongSpecialType.EMS_Kong)
                            {
                                _specialWinType = CSSpecialWinType.WT_KongWin;
                            }
                            else if (CheckPlayerSeabedWin())
                            {
                                _specialWinType = CSSpecialWinType.WT_SeabedWin;
                            }
                        }
                        else if (_specialWinType != CSSpecialWinType.WT_FakeHu)
                        {
                            if (mahjongSpecialType == MahjongSpecialType.EMS_Kong)
                            {
                                _specialWinType |= CSSpecialWinType.WT_KongWin;
                            }
                            else if (CheckPlayerSeabedWin())
                            {
                                _specialWinType |= CSSpecialWinType.WT_SeabedWin;
                            }
                        }
                        if (specialWinType == CSSpecialWinType.WT_PingWin || specialWinType == CSSpecialWinType.WT_None)
                        {
                            specialWinType = _specialWinType;
                        }
                        else
                        {
                            specialWinType |= _specialWinType;
                        }
                        multiple += GetMultipleBySpecialWinType(_specialWinType);
                    }
                    else if (mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongType == MahjongType.RedLaiZiMahjong)
                    {
                        specialWinType = _specialWinType;
                    }
                    else
                    {
                        //麻将类型有误
                        ServerUtil.RecordLog(LogType.Error, "MahjongWinByBlast, 麻将类型有误! mahjongType = " + mahjongType);
                        return false;
                    }
                }
                if (specialWinType == CSSpecialWinType.WT_None)
                {
                    //不能胡牌
                    ServerUtil.RecordLog(LogType.Error, "MahjongWinByBlast, 不能胡牌!");
                    return false;
                }
                //基础分和次数统计
                int basisIntegral = 0;
                if (mahjongType == MahjongType.ChangShaMahjong)
                {
                    if (specialWinType == CSSpecialWinType.WT_PingWin)
                    {
                        basisIntegral = ModuleManager.Get<MahjongMain>().csPingWinIntegral;
                        //小胡放炮
                        losePlayer.m_SmallWinFangBlast += 1;
                        //小胡接炮
                        winPlayer.m_SmallWinJieBlast += 1;
                    }
                    else
                    {
                        basisIntegral = ModuleManager.Get<MahjongMain>().csSpecialWinIntegral;
                        //大胡放炮
                        losePlayer.m_BigWinFangBlast += 1;
                        //大胡接炮
                        winPlayer.m_BigWinJieBlast += 1;
                    }
                }
                else if (mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongType == MahjongType.RedLaiZiMahjong)
                {
                    basisIntegral = ModuleManager.Get<MahjongMain>().zzWinFangBlastIntegral;
                    if (specialWinType == CSSpecialWinType.WT_7Pairs)
                    {
                        basisIntegral += ModuleManager.Get<MahjongMain>().zz7PairsAddIntegral;
                    }
                    //算筋
                    if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_Jin))
                    {
                        basisIntegral += losePlayer.GetPlyerKongNum() + winPlayer.GetPlyerKongNum();
                    }
                    //放炮
                    losePlayer.m_BigWinFangBlast += 1;
                    //接炮
                    winPlayer.m_BigWinJieBlast += 1;
                }
                else
                {
                    //麻将类型有误
                    ServerUtil.RecordLog(LogType.Error, "MahjongWinByBlast, 麻将类型有误! mahjongType = " + mahjongType);
                    return false;
                }
                //算庄
                if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_ZhuangLeisure))
                {
                    if (mahjongType == MahjongType.ChangShaMahjong && CheckHousePropertyType(MahjongHousePropertyType.EMHP_HuZhuang))
                    {
                        basisIntegral += 1;
                    }
                    else if (losePlayer.zhuangLeisureType == ZhuangLeisureType.Zhuang || winPlayer.zhuangLeisureType == ZhuangLeisureType.Zhuang)
                    {
                        basisIntegral += 1;
                    }
                }
                //算基础分
                int winIntegral = basisIntegral;
                if (mahjongType == MahjongType.ChangShaMahjong && specialWinType != CSSpecialWinType.WT_PingWin && multiple > 0)
                {
                    winIntegral = GetIntegralBySpecialWin(basisIntegral, multiple);
                }
                //算鸟
                if (losePlayerBureau.winBirdNumber > 0 || winPlayerBureau.winBirdNumber > 0)
                {
                    winIntegral += GetWinBirdIntegral(winIntegral, losePlayerBureau.winBirdNumber, winPlayerBureau.winBirdNumber);
                }  
                //积分封顶
                if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_IntegralCapped) && CheckHousePropertyType(MahjongHousePropertyType.EMHP_BirdDoubleIntegral) && winIntegral > ModuleManager.Get<MahjongMain>().integralCapped)
                {
                    winIntegral = ModuleManager.Get<MahjongMain>().integralCapped;
                    winPlayer.bIntegralCapped = true;
                }
                //算飘
                if (flutter > 0)
                {
                    winIntegral += GetWinFlutterIntegral();
                }
                //保存积分
                winPlayerBureau.mahjongWinType = (int)specialWinType;
                winPlayerBureau.bureauIntegral += winIntegral;
                winPlayer.allIntegral += winIntegral;
                lostIntegral -= winIntegral;
            }
            //自己算分
            losePlayerBureau.bureauIntegral += lostIntegral;
            losePlayer.allIntegral += lostIntegral;
            //算庄
            if (winPlayerlist.Count > 1)
            {
                //一炮多响放炮的人做庄
                SetHouseZhuang(losePlayer);
            }
            else
            {
                //单响炮胡牌的人做庄
                SetHouseZhuang(winPlayerlist[0]);
            }

            return true;
        }
        //抢杠胡
        public bool MahjongWinByGrabKong(List<MahjongPlayer> winPlayerlist, List<MahjongPlayerBureau> playerBureauList)
        {
            MahjongPlayer losePlayer = GetMahjongPlayer(currentWhoPlay);
            if (losePlayer == null)
            {
                //杠牌的人找不到了
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByGrabKong, 杠牌的人找不到了!");
                return false;
            }
            if (winPlayerlist.Exists(element => element.index == losePlayer.index))
            {
                //放炮的人怎么可能也是胡牌的
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByGrabKong, 放炮的人怎么可能也是胡牌的!");
                return false;
            }
            if (kongHuPlayerList.Count == 0 || (mahjongSpecialType != MahjongSpecialType.EMS_Kong && mahjongSpecialType != MahjongSpecialType.EMS_MakeUp))
            {
                //没人杠
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByGrabKong, 没人杠!");
                return false;
            }
            MahjongPlayerBureau losePlayerBureau = playerBureauList.Find(element => element.playerIndex == losePlayer.index);
            if (losePlayerBureau == null)
            {
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByGrabKong, losePlayerBureau == null!");
                return false;
            }
            //算分
            int lostIntegral = 0;
            foreach (MahjongPlayer winPlayer in winPlayerlist)
            {
                MahjongPlayerBureau winPlayerBureau = playerBureauList.Find(element => element.playerIndex == winPlayer.index);
                if (winPlayerBureau == null)
                {
                    ServerUtil.RecordLog(LogType.Error, "MahjongWinByGrabKong, winPlayerBureau == null!");
                    continue;
                }
                CSSpecialWinType specialWinType = winPlayer.WinHandTileCheck(currentMahjongList[0], bFakeHu);
                if (specialWinType == CSSpecialWinType.WT_None)
                {
                    //不能胡啊
                    ServerUtil.RecordLog(LogType.Error, "MahjongWinByGrabKong, 不能胡啊!");
                    return false;
                }
                //抢杠胡
                if (mahjongSpecialType == MahjongSpecialType.EMS_Kong || (mahjongType != MahjongType.ChangShaMahjong && mahjongSpecialType == MahjongSpecialType.EMS_MakeUp))
                {
                    if (specialWinType == CSSpecialWinType.WT_PingWin)
                    {
                        specialWinType = CSSpecialWinType.WT_GrabKongWin;
                    }
                    else if(specialWinType != CSSpecialWinType.WT_FakeHu)
                    {
                        specialWinType |= CSSpecialWinType.WT_GrabKongWin;
                    }
                }
                int winIntegral = 0;
                int basisIntegral = 0;
                //算庄
                if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_ZhuangLeisure))
                {
                    if (mahjongType == MahjongType.ChangShaMahjong && CheckHousePropertyType(MahjongHousePropertyType.EMHP_HuZhuang))
                    {
                        basisIntegral += 1;
                    }
                    else if (losePlayer.zhuangLeisureType == ZhuangLeisureType.Zhuang || winPlayer.zhuangLeisureType == ZhuangLeisureType.Zhuang)
                    {
                        basisIntegral += 1;
                    }
                }
                if (mahjongType == MahjongType.ChangShaMahjong)
                {
                    if (specialWinType == CSSpecialWinType.WT_PingWin)
                    {
                        basisIntegral += ModuleManager.Get<MahjongMain>().csPingWinIntegral;
                        winIntegral = basisIntegral;
                        //小胡放炮
                        losePlayer.m_SmallWinFangBlast += 1;
                        //小胡接炮
                        winPlayer.m_SmallWinJieBlast += 1;
                    }
                    else
                    {
                        basisIntegral += ModuleManager.Get<MahjongMain>().csSpecialWinIntegral;
                        winIntegral = GetIntegralBySpecialWin(basisIntegral, GetMultipleBySpecialWinType(specialWinType));
                        //大胡放炮
                        losePlayer.m_BigWinFangBlast += 1;
                        //大胡接炮
                        winPlayer.m_BigWinJieBlast += 1;
                    }
                }
                else if(mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongType == MahjongType.RedLaiZiMahjong)
                {                    
                    winIntegral = basisIntegral + ModuleManager.Get<MahjongMain>().zzWinFangBlastIntegral;
                    if((specialWinType & CSSpecialWinType.WT_7Pairs) == CSSpecialWinType.WT_7Pairs)
                    {
                        winIntegral += ModuleManager.Get<MahjongMain>().zz7PairsAddIntegral;
                    }
                    //算筋
                    if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_Jin))
                    {
                        winIntegral += losePlayer.GetPlyerKongNum(currentMahjongList[0]) + winPlayer.GetPlyerKongNum(currentMahjongList[0]);
                    }
                    //放炮
                    losePlayer.m_BigWinFangBlast += 1;
                    //接炮
                    winPlayer.m_BigWinJieBlast += 1;
                }
                else
                {
                    //麻将类型有误
                    ServerUtil.RecordLog(LogType.Error, "MahjongWinByBlast, 麻将类型有误! mahjongType = " + mahjongType);
                    return false;
                }
                //算鸟
                if (losePlayerBureau.winBirdNumber > 0 || winPlayerBureau.winBirdNumber > 0)
                {
                    winIntegral += GetWinBirdIntegral(winIntegral, losePlayerBureau.winBirdNumber, winPlayerBureau.winBirdNumber);
                }   
                //积分封顶
                if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_IntegralCapped) && CheckHousePropertyType(MahjongHousePropertyType.EMHP_BirdDoubleIntegral) && winIntegral > ModuleManager.Get<MahjongMain>().integralCapped)
                {
                    winIntegral = ModuleManager.Get<MahjongMain>().integralCapped;
                    winPlayer.bIntegralCapped = true;
                }
                //算飘
                if (flutter > 0)
                {
                    winIntegral += GetWinFlutterIntegral();
                }
                if (mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongType == MahjongType.RedLaiZiMahjong)
                {
                    //转转麻将和红中麻将抢杠胡通赔
                    winIntegral = winIntegral * (maxPlayerNum - 1);
                }
                //保存积分
                winPlayerBureau.mahjongWinType = (int)specialWinType;
                winPlayerBureau.bureauIntegral += winIntegral;
                winPlayer.allIntegral += winIntegral;
                lostIntegral -= winIntegral;
            }
            //自己算分
            losePlayerBureau.bureauIntegral += lostIntegral;
            losePlayer.allIntegral += lostIntegral;
            //算庄
            if (winPlayerlist.Count > 1)
            {
                //放杠的人做庄
                SetHouseZhuang(losePlayer);
            }
            else
            {
                //单响炮胡牌的人做庄
                SetHouseZhuang(winPlayerlist[0]);
            }

            return true;
        }
        //自摸
        public bool MahjongWinByMyself(List<MahjongPlayer> winPlayerlist, List<MahjongPlayerBureau> playerBureauList)
        {
            if (winPlayerlist.Count != 1)
            {
                //自摸肯定是一个人胡啦
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByMyself, 自摸肯定是一个人胡啦!");
                return false;
            }
            MahjongPlayer winPlayer = winPlayerlist[0];
            if (winPlayer.index != currentShowCard)
            {
                //胡牌人不是当前出牌人
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByMyself, 胡牌人不是当前出牌人!");
                return false;
            }
            MahjongPlayerBureau winPlayerBureau = playerBureauList.Find(element => element.playerIndex == winPlayer.index);
            if (winPlayerBureau == null)
            {
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByMyself, winPlayerBureau == null!");
                return false;
            }
            int multiple = 0;
            CSSpecialWinType specialWinType = CSSpecialWinType.WT_None;
            int basisIntegral = 0;
            if (mahjongType == MahjongType.ChangShaMahjong)
            {
                bool bSeabedWin = CheckPlayerSeabedWin();
                if (mahjongSpecialType == MahjongSpecialType.EMS_Kong || bSeabedWin)
                {
                    foreach (MahjongTile mahjongTile in currentMahjongList)
                    {
                        CSSpecialWinType _specialWinType = winPlayer.WinHandTileCheck(mahjongTile, bFakeHu);
                        if (CSSpecialWinType.WT_None == _specialWinType)
                        {
                            ServerUtil.RecordLog(LogType.Debug, "MahjongWinByMyself, CSSpecialWinType.WT_None == _specialWinType!");
                            continue;
                        }

                        if (_specialWinType == CSSpecialWinType.WT_PingWin)
                        {
                            if (mahjongSpecialType == MahjongSpecialType.EMS_Kong)
                            {
                                _specialWinType = CSSpecialWinType.WT_KongWin;
                            }
                            else if (bSeabedWin)
                            {
                                _specialWinType = CSSpecialWinType.WT_SeabedWin;
                            }
                        }
                        else if(_specialWinType != CSSpecialWinType.WT_FakeHu)
                        {
                            if (mahjongSpecialType == MahjongSpecialType.EMS_Kong)
                            {
                                _specialWinType |= CSSpecialWinType.WT_KongWin;
                            }
                            else if (bSeabedWin)
                            {
                                _specialWinType |= CSSpecialWinType.WT_SeabedWin;
                            }
                        }
                        if (specialWinType == CSSpecialWinType.WT_PingWin || specialWinType == CSSpecialWinType.WT_None)
                        {
                            specialWinType = _specialWinType;
                        }
                        else
                        {
                            specialWinType |= _specialWinType;
                        }
                        multiple += GetMultipleBySpecialWinType(_specialWinType);
                    }
                }
                else if (winPlayer.CheckPlayerShowMahjong())
                {
                    specialWinType = winPlayer.WinHandTileCheck();
                    if (specialWinType != CSSpecialWinType.WT_PingWin)
                    {
                        multiple += GetMultipleBySpecialWinType(specialWinType);
                    }
                }
                if (specialWinType == CSSpecialWinType.WT_None)
                {
                    //不能胡牌
                    ServerUtil.RecordLog(LogType.Error, "MahjongWinByMyself, 不能胡牌!");
                    return false;
                }
                else if (specialWinType == CSSpecialWinType.WT_PingWin)
                {
                    basisIntegral = ModuleManager.Get<MahjongMain>().csPingWinIntegral;
                    //小胡自摸
                    winPlayer.m_SmallWinMyself += 1;
                }
                else
                {
                    basisIntegral = ModuleManager.Get<MahjongMain>().csSpecialWinIntegral;
                    //大胡自摸
                    winPlayer.m_BigWinMyself += 1;
                }
            }
            else if(mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongType == MahjongType.RedLaiZiMahjong)
            {
                if (winPlayer.CheckPlayerShowMahjong())
                {
                    specialWinType = winPlayer.WinHandTileCheck();
                }
                if (specialWinType == CSSpecialWinType.WT_None)
                {
                    //不能胡牌
                    ServerUtil.RecordLog(LogType.Error, "MahjongWinByMyself, 不能胡牌!");
                    return false;
                }
                basisIntegral = ModuleManager.Get<MahjongMain>().zzWinMyselfIntegral;
                if (specialWinType == CSSpecialWinType.WT_7Pairs)
                {
                    basisIntegral += ModuleManager.Get<MahjongMain>().zz7PairsAddIntegral;
                }
                if (mahjongType == MahjongType.RedLaiZiMahjong && !winPlayer.CheckPlayerRedMahjong())
                {
                    //门前清加基础分（自摸才算）
                    if (specialWinType == CSSpecialWinType.WT_PingWin)
                    {
                        specialWinType = CSSpecialWinType.WT_FrontClear;
                    }
                    else
                    {
                        specialWinType |= CSSpecialWinType.WT_FrontClear;
                    }
                    basisIntegral += ModuleManager.Get<MahjongMain>().zz7PairsAddIntegral;
                }
                //自摸
                winPlayer.m_BigWinMyself += 1;
            } 
            else
            {
                //麻将类型有误
                ServerUtil.RecordLog(LogType.Error, "MahjongWinByBlast, 麻将类型有误! mahjongType = " + mahjongType);
                return false;
            }
            //算分
            int winIntegral = 0;
            foreach (MahjongPlayer losePlayer in GetOtherMahjongPlayer(winPlayer.userId))
            {
                MahjongPlayerBureau losePlayerBureau = playerBureauList.Find(element => element.playerIndex == losePlayer.index);
                if (losePlayerBureau == null)
                {
                    ServerUtil.RecordLog(LogType.Error, "MahjongWinByMyself, losePlayerBureau == null!");
                    continue;
                }
                //算庄
                int _basisIntegral = basisIntegral;
                if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_ZhuangLeisure))
                {
                    if (mahjongType == MahjongType.ChangShaMahjong && CheckHousePropertyType(MahjongHousePropertyType.EMHP_HuZhuang))
                    {
                        _basisIntegral += 1;
                    }
                    else if (losePlayer.zhuangLeisureType == ZhuangLeisureType.Zhuang || winPlayer.zhuangLeisureType == ZhuangLeisureType.Zhuang)
                    {
                        _basisIntegral += 1;
                    }
                }
                //算基础分
                int huIntegral = _basisIntegral;
                if (mahjongType == MahjongType.ChangShaMahjong && specialWinType != CSSpecialWinType.WT_PingWin && multiple > 0)
                {
                    huIntegral = GetIntegralBySpecialWin(_basisIntegral, multiple);
                }
                //算筋
                if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_Jin) && (mahjongType == MahjongType.ZhuanZhuanMahjong || mahjongType == MahjongType.RedLaiZiMahjong))
                {
                    huIntegral += losePlayer.GetPlyerKongNum() + winPlayer.GetPlyerKongNum();
                }
                //算鸟
                if (losePlayerBureau.winBirdNumber > 0 || winPlayerBureau.winBirdNumber > 0)
                {
                    huIntegral += GetWinBirdIntegral(huIntegral, losePlayerBureau.winBirdNumber, winPlayerBureau.winBirdNumber);
                }   
                //积分封顶
                if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_IntegralCapped) && CheckHousePropertyType(MahjongHousePropertyType.EMHP_BirdDoubleIntegral) && huIntegral > ModuleManager.Get<MahjongMain>().integralCapped)
                {
                    huIntegral = ModuleManager.Get<MahjongMain>().integralCapped;
                    losePlayer.bIntegralCapped = true;
                }
                //算飘
                if (flutter > 0)
                {
                    huIntegral += GetWinFlutterIntegral();
                }
                //保存积分
                losePlayerBureau.bureauIntegral -= huIntegral;
                losePlayer.allIntegral -= huIntegral;
                winIntegral += huIntegral;
            }
            //自己算分
            winPlayerBureau.mahjongWinType = (int)specialWinType;
            winPlayerBureau.bureauIntegral += winIntegral;
            winPlayer.allIntegral += winIntegral;
            //胡牌的人做庄
            SetHouseZhuang(winPlayer);

            return true;
        }
        private int GetMultipleBySpecialWinType(CSSpecialWinType specialWinType)
        {
            int multiple = 0;
            foreach (CSSpecialWinType type in Enum.GetValues(typeof(CSSpecialWinType)))
            {
                if (type != CSSpecialWinType.WT_None && type != CSSpecialWinType.WT_PingWin && (specialWinType & type) > 0)
                {
                    multiple += 1;
                }                  
            }
            return multiple;
        }
        private int GetIntegralBySpecialWin(int basisIntegral, int multiple)
        {
            if (multiple < 1)
            {
                return 0;
            }
            //return basisIntegral * (int)(Math.Pow(2, multiple - 1));
            return basisIntegral * multiple;
        }
        public int GetWinBirdIntegral(int basisIntegral, int losePlayerBirdNumber, int winPlayerBirdNumber)
        {
            if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_BirdDoubleIntegral))
            {
                return basisIntegral * (losePlayerBirdNumber + winPlayerBirdNumber);
            }
            else if (CheckHousePropertyType(MahjongHousePropertyType.EMHP_BirdAddIntegral))
            {
                return losePlayerBirdNumber + winPlayerBirdNumber;
            }
            return 0;
        }
        public int GetWinFlutterIntegral()
        {
            if (flutter == 1)
            {
                return 2;
            }
            else if (flutter == 2)
            {
                return 4;
            }
            return 0;
        }
        public void SetZhuangPlayer(int index)
        {
            MahjongPlayer player = GetMahjongPlayer(index);
            if (player != null)
            {
                player.zhuangLeisureType = ZhuangLeisureType.Zhuang;
            }
        }
        public bool CheckMahjongIsLiuJu()
        {
            if(GetMahjongPlayer().Exists(element => element.housePlayerStatus == MahjongPlayerStatus.MahjongWinCard))
            {
                //有人胡
                return false;
            }
            if (0 == GetRemainMahjongCount() || (GetHouseSelectSeabedCount() == GetRemainMahjongCount() && mahjongSpecialType == MahjongSpecialType.EMS_Seabed))
            {
                //流局 
                return true;
            }
            return false;
        }
        public void DisposeNextPendulumPlayer(int pendulumPlayer)
        {
            List<MahjongPlayer> playerList = GetMahjongPlayer().FindAll(element => (element.startDisplayType != CSStartDisplayType.SDT_None && element.housePlayerStatus == MahjongPlayerStatus.MahjongPendulum));
            if (playerList != null && playerList.Count > 0)
            {
                if (playerList.Count == 1)
                {
                    currentShowCard = playerList[0].index;
                    return;
                }
                for (int i = 1; i < summonerList.Count; ++i)
                {
                    pendulumPlayer = GetNextHousePlayerIndex(pendulumPlayer);
                    if (playerList.Exists(element => element.index == pendulumPlayer))
                    {
                        //从上一次摆牌的人开始逆时针找下一次摆牌的人
                        currentShowCard = pendulumPlayer;
                        return;
                    }
                }
            }
            //不需要有人摆牌了
            currentShowCard = -1;
        }
    }
}
#endif
