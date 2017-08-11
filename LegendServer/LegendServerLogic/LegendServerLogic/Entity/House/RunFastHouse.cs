#if RUNFAST
using LegendProtocol;
using System.Collections.Generic;
using LegendServerLogic.Actor.Summoner;
using LegendServerLogic.Entity.Base;
using System.Linq;
using System;
using LegendServerLogic.Entity.Players;

namespace LegendServerLogic.Entity.Houses
{
    public class RunFastHouse : House
    {
        //房间属性
        public int housePropertyType;
        //本局庄家
        public int zhuangPlayerIndex;
        //当前跑得快类型
        public RunFastType runFastType;
        //当前出牌类型
        public PokerGroupType pokerGroupType;
        //当前出牌
        public List<Card> currentCardList;
        //每局的信息
        public List<HouseBureau> houseBureauList;
        //房间状态
        public RunFastHouseStatus houseStatus;
        //房间发牌临时变量
        private List<List<Card>> m_RunFastHandCardList;
        //房间玩家临时变量
        private List<RunFastPlayer> m_RunFastPlayerList;
        public RunFastHouse()
        {
            housePropertyType = 0;
            houseCardId = 0;
            houseId = 0;
            logicId = 0;
            maxBureau = 0;
            businessId = 0;
            currentBureau = 0;
            currentWhoPlay = -1;
            currentShowCard = -1;
            zhuangPlayerIndex = -1;
            competitionKey = 0;
            maxPlayerNum = RunFastConstValue.RunFastThreePlayer;
            houseType = HouseType.RunFastHouse;
            pokerGroupType = PokerGroupType.None;
            currentCardList = new List<Card>();
            summonerList = new List<Player>();
            houseBureauList = new List<HouseBureau>();
            m_RunFastHandCardList = new List<List<Card>>();
            m_RunFastPlayerList = new List<RunFastPlayer>();
            voteBeginTime = DateTime.Parse("1970-01-01 00:00:00");
            operateBeginTime = DateTime.Parse("1970-01-01 00:00:00");
        }
        public RunFastPlayer CreatHouse(Summoner summoner, HousePlayerStatus housePlayerStatus = HousePlayerStatus.Free)
        {
            //绑定玩家
            RunFastPlayer newHousePlayer = new RunFastPlayer();
            newHousePlayer.index = 0;
            newHousePlayer.userId = summoner.userId;
            newHousePlayer.nickName = summoner.nickName;
            newHousePlayer.summonerId = summoner.id;
            newHousePlayer.sex = summoner.sex;
            newHousePlayer.ip = summoner.ip;
            newHousePlayer.proxyServerId = summoner.proxyServerId;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            summonerList.Add(newHousePlayer);
            m_RunFastPlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public RunFastPlayer CreatHouse(PlayerInfo playerInfo, HousePlayerStatus housePlayerStatus = HousePlayerStatus.Ready)
        {
            //绑定玩家
            RunFastPlayer newHousePlayer = new RunFastPlayer();
            newHousePlayer.index = 0;
            newHousePlayer.userId = playerInfo.userId;
            newHousePlayer.nickName = playerInfo.nickName;
            newHousePlayer.summonerId = playerInfo.summonerId;
            newHousePlayer.sex = playerInfo.sex;
            newHousePlayer.ip = playerInfo.ip;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            summonerList.Add(newHousePlayer);
            m_RunFastPlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public RunFastPlayer AddPlayer(PlayerInfo playerInfo, HousePlayerStatus housePlayerStatus = HousePlayerStatus.Ready)
        {
            //绑定玩家
            RunFastPlayer newHousePlayer = new RunFastPlayer();
            newHousePlayer.index = GetHousePlayerIndex();
            newHousePlayer.userId = playerInfo.userId;
            newHousePlayer.nickName = playerInfo.nickName;
            newHousePlayer.summonerId = playerInfo.summonerId;
            newHousePlayer.sex = playerInfo.sex;
            newHousePlayer.ip = playerInfo.ip;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            summonerList.Add(newHousePlayer);
            m_RunFastPlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public RunFastPlayer AddPlayer(Summoner summoner, HousePlayerStatus housePlayerStatus = HousePlayerStatus.Free)
        {
            //绑定玩家
            RunFastPlayer newHousePlayer = new RunFastPlayer();
            newHousePlayer.index = GetHousePlayerIndex();
            newHousePlayer.userId = summoner.userId;
            newHousePlayer.nickName = summoner.nickName;
            newHousePlayer.summonerId = summoner.id;
            newHousePlayer.sex = summoner.sex;
            newHousePlayer.ip = summoner.ip;
            newHousePlayer.proxyServerId = summoner.proxyServerId;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            summonerList.Add(newHousePlayer);
            m_RunFastPlayerList.Add(newHousePlayer);

            return newHousePlayer;
        }
        public void AddPlayer(HousePlayerNode playerNode, HousePlayerStatus housePlayerStatus = HousePlayerStatus.Free)
        {
            //绑定玩家
            RunFastPlayer newHousePlayer = new RunFastPlayer();
            newHousePlayer.index = playerNode.index;
            newHousePlayer.userId = playerNode.userId;
            newHousePlayer.nickName = playerNode.nickName;
            newHousePlayer.summonerId = playerNode.summonerId;
            newHousePlayer.sex = playerNode.sex;
            newHousePlayer.housePlayerStatus = housePlayerStatus;
            newHousePlayer.bombIntegral = playerNode.bombIntegral;
            newHousePlayer.winBureau = playerNode.winBureau;
            newHousePlayer.loseBureau = playerNode.loseBureau;
            newHousePlayer.allIntegral = playerNode.allIntegral;
            newHousePlayer.lineType = LineType.OffLine;

            summonerList.Add(newHousePlayer);
            m_RunFastPlayerList.Add(newHousePlayer);
        }
        public override RunFastHouseStatus GetRunFastHouseStatus()
        {
            return houseStatus;
        }
        public SettlementType GetPlayerSettlementType(int cardCount)
        {
            if (cardCount == 0)
            {
                return SettlementType.Winner;
            }
            return SettlementType.Loser;
        }
        public List<RunFastPlayer> GetRunFastPlayer()
        {
            if (m_RunFastPlayerList.Count != summonerList.Count)
            {
                m_RunFastPlayerList.Clear();
                foreach (RunFastPlayer runFastPlayer in summonerList)
                {
                    m_RunFastPlayerList.Add(runFastPlayer);
                }
            }
            return m_RunFastPlayerList;
        }
        public List<RunFastPlayer> GetOtherRunFastPlayer(string userId)
        {
            return GetRunFastPlayer().FindAll(element => element.userId != userId);
        }
        public RunFastPlayer GetRunFastPlayer(string userId)
        {
            return GetRunFastPlayer().FirstOrDefault(element => element.userId == userId);
        }
        public RunFastPlayer GetRunFastPlayer(int index)
        {
            return GetRunFastPlayer().FirstOrDefault(element => element.index == index);
        }
        public RunFastPlayer GetRunFastPlayerBySummonerId(ulong summonerId)
        {
            return GetRunFastPlayer().FirstOrDefault(element => element.summonerId == summonerId);
        }
        public RunFastPlayer GetHouseOwner()
        {
            return GetRunFastPlayer(0);
        }
        public RunFastPlayer GetRunFastVoteLaunchPlayer()
        {
            return GetRunFastPlayer().Find(element => element.voteStatus == VoteStatus.LaunchVote);
        }
        public RunFastPlayer GetNextHousePlayer(int index)
        {
            int nextIndex = GetNextHousePlayerIndex(index);
            return GetRunFastPlayer(nextIndex);
        }
        public RunFastPlayer GetLastHousePlayer(int index)
        {
            int lastIndex = GetLastHousePlayerIndex(index);
            return GetRunFastPlayer(lastIndex);
        }
        public List<RunFastPlayer> GetRunFastPlayersByCondition(Func<RunFastPlayer, bool> condition)
        {
            return GetRunFastPlayer().Where(condition).ToList();
        }
        public RunFastPlayer GetRunFastPlayerByCondition(Func<RunFastPlayer, bool> condition)
        {
            return GetRunFastPlayer().FirstOrDefault(condition);
        }
        //判断房间属性
        public bool CheckHousePropertyType(RunFastHousePropertyType type)
        {
            if ((int)type == (housePropertyType & (int)type))
            {
                return true;
            }
            return false;
        }
        public void InitPlayerBureau(HouseBureau houseBureau)
        {
            HouseBureau _houseBureau = houseBureauList.Find(element => element.bureau == houseBureau.bureau);
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
        public HouseBureau GetHouseBureau()
        {
            return houseBureauList.Find(element => element.bureau == (ulong)currentBureau);
        }
        public PlayerBureauIntegral GetHousePlayerBureau(int playerIndex)
        {
            HouseBureau houseBureau = GetHouseBureau();
            if (houseBureau != null)
            {
                return houseBureau.playerBureauList.Find(element => element.playerIndex == playerIndex);
            }
            return null;
        }
        public int GetPlayerBureauMaxIntegral(int playerIndex)
        {
            int maxInteral = int.MinValue;
            foreach(HouseBureau houseBureau in houseBureauList)
            {
                PlayerBureauIntegral playerBureau = houseBureau.playerBureauList.Find(element => element.playerIndex == playerIndex);
                if (playerBureau != null && playerBureau.bureauIntegral > maxInteral)
                {
                    maxInteral = playerBureau.bureauIntegral; 
                }
            }
            return maxInteral;
        }
        public void SetPlayerBureauIntegral(int playerIndex, int cardIntegral)
        {
            PlayerBureauIntegral housePlayerBureau = GetHousePlayerBureau(playerIndex);
            if (housePlayerBureau != null)
            {
                housePlayerBureau.bureauIntegral += cardIntegral;
                housePlayerBureau.cardIntegral += cardIntegral;
            }
        }
        public void SetPlayerZhaDanIntegral(int playerIndex, int bombIntegral)
        {
            PlayerBureauIntegral housePlayerBureau = GetHousePlayerBureau(playerIndex);
            if (housePlayerBureau != null)
            {
                housePlayerBureau.bureauIntegral += bombIntegral;
                housePlayerBureau.bombIntegral += bombIntegral;
            }
        }
        public List<PlayerSettlementNode> GetPlayerSettlementList()
        {
            List<PlayerSettlementNode> playerSettlementList = new List<PlayerSettlementNode>();
            foreach (RunFastPlayer housePlayer in summonerList)
            {
                PlayerSettlementNode playerSettlementNode = new PlayerSettlementNode();
                playerSettlementNode.index = housePlayer.index;
                playerSettlementNode.nickName = housePlayer.nickName;
                playerSettlementNode.cardList.AddRange(housePlayer.cardList);
                playerSettlementNode.allIntegral = housePlayer.allIntegral;
                playerSettlementNode.bStrongOff = housePlayer.bStrongOff;
                PlayerBureauIntegral housePlayerBureau = GetHousePlayerBureau(housePlayer.index);
                if (housePlayerBureau != null)
                {
                    playerSettlementNode.bombIntegral = housePlayerBureau.bombIntegral;
                    playerSettlementNode.cardIntegral = housePlayerBureau.cardIntegral;
                    playerSettlementNode.bureauIntegral = housePlayerBureau.bureauIntegral;
                }
                playerSettlementList.Add(playerSettlementNode);
            }
            return playerSettlementList;
        }
        public List<PlayerEndSettlementNode> GetPlayerEndSettlementList()
        {
            List<PlayerEndSettlementNode> playerEndSettlementList = new List<PlayerEndSettlementNode>();
            foreach (RunFastPlayer housePlayer in summonerList)
            {
                PlayerEndSettlementNode playerEndSettlementNode = new PlayerEndSettlementNode();
                playerEndSettlementNode.index = housePlayer.index;
                playerEndSettlementNode.winBureau = housePlayer.winBureau;
                playerEndSettlementNode.loseBureau = housePlayer.loseBureau;
                playerEndSettlementNode.bombIntegral = housePlayer.bombIntegral;
                playerEndSettlementNode.maxIntegral = GetPlayerBureauMaxIntegral(housePlayer.index);
                playerEndSettlementList.Add(playerEndSettlementNode);
            }
            return playerEndSettlementList;
        }
        public int GetPlayerCardIntegral(int cardCount, bool bStrongOff = false)
        {
            int allCardCount = GetAllCardCount();
            if (cardCount > 1)
            {
                int multiple = 1;
                if (cardCount == allCardCount && !bStrongOff)
                {
                    multiple = 2;
                }

                return (cardCount * multiple);
            }
            return 0;
        }
        public int GetAllCardCount()
        {
            int allCardCount = 0;
            if (runFastType == RunFastType.Fifteen)
            {
                allCardCount = RunFastConstValue.RunFastFifteen;
            }
            else if (runFastType == RunFastType.Sixteen)
            {
                allCardCount = RunFastConstValue.RunFastSixteen;
            }
            return allCardCount;
        }
        public bool CheckBeginRunFastDecks()
        {
            if (!CheckPlayerFull())
            {
                //人不够
                return false;
            }
            if (GetRunFastPlayer().Exists(element => element.housePlayerStatus != HousePlayerStatus.Ready))
            {
                //有人没准备
                return false;
            }
            if (currentBureau > maxBureau)
            {
                //局数满啦
                return false;
            }

            return true;
        }
        public bool BeginRunFastDecks(HouseBureau houseBureau)
        {
            //人数到齐可以开局
            m_RunFastHandCardList.Clear();
            GetRunFastPlayer().ForEach(runFastPlayer =>
            {
                m_RunFastHandCardList.Add(runFastPlayer.cardList);
            });
            MyRunFastDeck.GetRunFastDeck().InitRunFastDecks(runFastType, m_RunFastHandCardList);
            if (m_RunFastPlayerList.Exists(runFastPlayer => runFastPlayer.cardList.Count == 0))
            {
                return false;
            }
            if (currentBureau == 0 && this.maxPlayerNum == RunFastConstValue.RunFastTwoPlayer)
            {
                //处理黑桃3
                DisSpadeThree(m_RunFastPlayerList[0].cardList, m_RunFastPlayerList[1].cardList);
            }
            houseStatus = RunFastHouseStatus.RFHS_BeginBureau;
            if (currentBureau == 0)
            {
                //标志第一局
                currentBureau = 1;
                //当前谁先出牌
                GetRunFastPlayer().ForEach(runFastPlayer =>
                {
                    runFastPlayer.housePlayerStatus = HousePlayerStatus.Free;
                    if (runFastPlayer.cardList.Exists(element => (element.suit == Suit.Spade && element.rank == Rank.Three)))
                    {
                        runFastPlayer.housePlayerStatus = HousePlayerStatus.WaitShowCard;
                        currentShowCard = runFastPlayer.index;
                    }
                    //初始积分
                    PlayerBureauIntegral playerBureau = new PlayerBureauIntegral();
                    playerBureau.playerIndex = runFastPlayer.index;
                    houseBureau.playerBureauList.Add(playerBureau);
                });
                zhuangPlayerIndex = currentShowCard;
            }
            else
            {
                currentShowCard = zhuangPlayerIndex;
                currentBureau += 1;
                currentWhoPlay = -1;
                pokerGroupType = PokerGroupType.None;
                currentCardList.Clear();
                GetRunFastPlayer().ForEach(runFastPlayer =>
                {
                    runFastPlayer.housePlayerStatus = HousePlayerStatus.Free;
                    runFastPlayer.bStrongOff = false;
                    if (runFastPlayer.index == currentShowCard)
                    {
                        runFastPlayer.housePlayerStatus = HousePlayerStatus.WaitShowCard;
                    }
                    //初始积分
                    PlayerBureauIntegral playerBureau = new PlayerBureauIntegral();
                    playerBureau.playerIndex = runFastPlayer.index;
                    houseBureau.playerBureauList.Add(playerBureau);
                });
            }
            //局信息
            houseBureau.bureau = (ulong)currentBureau;
            houseBureau.bureauTime = DateTime.Now.ToString();
            InitPlayerBureau(houseBureau);

            return true;
        }
        public bool CheckSettlementRunFastDecks()
        {
            if (!CheckPlayerFull())
            {
                //人不够
                return false;
            }
            if (houseStatus != RunFastHouseStatus.RFHS_Settlement)
            {
                return false;
            }
            if (currentBureau > maxBureau)
            {
                //局数满啦
                return false;
            }

            return true;
        }
        public bool SettlementRunFastDecks()
        {
            RunFastPlayer player = GetRunFastPlayer().Find(element => element.cardList.Count == 0);
            if (player == null)
            {
                return false;
            }
            RunFastPlayer lastPlayer = GetLastHousePlayer(player.index);
            if (lastPlayer == null)
            {
                return false;
            }
            RunFastPlayer nextPlayer = GetNextHousePlayer(player.index);
            if (nextPlayer == null)
            {
                return false;
            }
            int lastLoseIntegral = GetPlayerCardIntegral(lastPlayer.cardList.Count, lastPlayer.bStrongOff);
            if (lastLoseIntegral > 0)
            {
                lastPlayer.allIntegral -= lastLoseIntegral;
                SetPlayerBureauIntegral(lastPlayer.index, -lastLoseIntegral);
            }
            lastPlayer.loseBureau += 1;
            int nextLoseIntegral = 0;
            if (maxPlayerNum == RunFastConstValue.RunFastThreePlayer)
            {
                if (nextPlayer.bStrongOff)
                {
                    //只能赢家的上家才能被强关
                    nextPlayer.bStrongOff = false;
                }
                nextLoseIntegral = GetPlayerCardIntegral(nextPlayer.cardList.Count, nextPlayer.bStrongOff);
                if (nextLoseIntegral > 0)
                {
                    nextPlayer.allIntegral -= nextLoseIntegral;
                    SetPlayerBureauIntegral(nextPlayer.index, -nextLoseIntegral);
                }
                nextPlayer.loseBureau += 1;
            }
            //胜利
            int winIntegral = lastLoseIntegral + nextLoseIntegral;
            if (winIntegral > 0)
            {
                player.allIntegral += winIntegral;
                SetPlayerBureauIntegral(player.index, winIntegral);
            }
            player.winBureau += 1;
            //下一个庄家
            zhuangPlayerIndex = player.index;

            return true;
        }
        public int GetBeginShowCard()
        {
            int index = 0;
            foreach (RunFastPlayer runFastPlayer in summonerList)
            {
                runFastPlayer.housePlayerStatus = HousePlayerStatus.Free;
                if (runFastPlayer.cardList.Exists(element => (element.suit == Suit.Spade && element.rank == Rank.Three)))
                {
                    runFastPlayer.housePlayerStatus = HousePlayerStatus.WaitShowCard;
                    index = runFastPlayer.index;
                }
            }
            return index;
        }
        private void DisSpadeThree(List<Card> oneCardList, List<Card> twoCardList)
        {
            if (!oneCardList.Exists(element => (element.suit == Suit.Spade && element.rank == Rank.Three)) && !twoCardList.Exists(element => (element.suit == Suit.Spade && element.rank == Rank.Three)))
            {
                //随机给谁上黑桃3
                int rand = MyRandom.NextPrecise(0, 10000);
                if (0 == rand % 2)
                {
                    DisSpadeThree(oneCardList);
                }
                else
                {
                    DisSpadeThree(twoCardList);
                }
            }
        }
        private void DisSpadeThree(List<Card> cardList)
        {
            if (!cardList.Exists(element => (element.suit == Suit.Spade && element.rank == Rank.Three)))
            {
                //上在哪个位置
                int index = MyRandom.NextPrecise(0, cardList.Count);
                cardList[index].suit = Suit.Spade;
                cardList[index].rank = Rank.Three;
            }
        }
    }
}
#endif
