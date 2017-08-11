using LegendProtocol;
using LegendServerLogic.Entity.Houses;
using LegendServerLogic.Entity.Players;
using LegendServerLogic.SpecialActivities;
using LegendServerLogicDefine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LegendServerCompetitionManager
{
    //比赛场
    public class Competition
    {
        public int competitionKey;          //口令
        public int marketId;                //商家Id
        public ulong createSummonerId;        //创建者玩家Id
        public int maxApplyNum;             //最大申请人数
        public int firstAdmitNum;           //第一局录取人数
        public int curAdmitNum;             //当前录取人数
        public int maxGameBureau;           //游戏最大局
        public int currentBureau;           //当前匹配场次
        public int maxBureau;               //最大匹配场次
        public int waitCount;               //等待次数
        public int houseCount;              //多少桌房
        public string[] strTicketList;      //奖励列表
        public CompetitionStatus status;    //比赛场状态
        public DateTime createTime;         //创建时间
        public DateTime endTime;            //结束时间
        public List<CompetitionPlayer> comPlayerList = new List<CompetitionPlayer>();       //玩家列表
        public Competition() { }
        public Competition(ulong summonerId, int marketId, int competitionKey, int maxApplyNum, int firstAdmitNum, int maxBureau, int maxGameBureau)
        {
            this.createSummonerId = summonerId;
            this.marketId = marketId;
            this.competitionKey = competitionKey;
            this.maxApplyNum = maxApplyNum;
            this.firstAdmitNum = firstAdmitNum;
            this.maxGameBureau = maxGameBureau;
            this.maxBureau = maxBureau;
            this.currentBureau = 0;
            this.waitCount = 0;
            this.curAdmitNum = 0;
            this.houseCount = 0;
            this.strTicketList = ModuleManager.Get<SpecialActivitiesMain>().GetStrTickets(marketId);
            this.status = CompetitionStatus.ECS_Apply;
            this.createTime = DateTime.Now;
            this.endTime = DateTime.Now;
        }
        public int GetComPlayerCount()
        {
            return comPlayerList.Count;
        }
        public void AddCompetitionPlayer(ulong summonerId, string nickName, string userId, string ip, UserSex sex)
        {
            if (!CheckPlayerExist(summonerId))
            {
                int rank = comPlayerList.Count + 1;
                comPlayerList.Add(new CompetitionPlayer(summonerId, nickName, userId, ip, sex, rank));
            }
            if (CheckFullPlayerNum())
            {
                waitCount = 0;
                ChangeCompetitionStatus(CompetitionStatus.ECS_Begin);
            }
        }
        public void RemoveCompetitionPlayer(ulong summonerId)
        {
            comPlayerList.RemoveAll(element => element.summonerId == summonerId);
        }
        public void ChangeCompetitionStatus(CompetitionStatus competitionStatus, bool bTimer = true)
        {
            status = competitionStatus;
            if (competitionStatus == CompetitionStatus.ECS_Begin || competitionStatus == CompetitionStatus.ECS_Wait)
            {
                if (competitionStatus == CompetitionStatus.ECS_Begin)
                {
                    currentBureau += 1;
                }
                if (bTimer)
                {
                    ModuleManager.Get<SpecialActivitiesMain>().AddMarketsCompetitionTimer(competitionKey);
                }
            }
            else if (competitionStatus == CompetitionStatus.ECS_End)
            {
                endTime = DateTime.Now;
                comPlayerList.Sort(ModuleManager.Get<SpecialActivitiesMain>().CompareTileByRank);
                ModuleManager.Get<SpecialActivitiesMain>().AddMarketsCompetitionEndTimer(competitionKey);
            }
            else if(competitionStatus == CompetitionStatus.ECS_Game)
            {
                int result = (int)Math.Pow(ModuleManager.Get<SpecialActivitiesMain>().maxPlayerNum, currentBureau - 1);
                curAdmitNum = firstAdmitNum / result;
                houseCount = HouseManager.Instance.GetHouseListByCondition(element => element.businessId == marketId && element.competitionKey == competitionKey).Count;
                comPlayerList.Sort(ModuleManager.Get<SpecialActivitiesMain>().CompareTileByRank);
            }
        }
        public void AddPlayerIntegral(List<ComPlayerIntegral> playerIntegralList)
        {
            playerIntegralList.ForEach(player =>
            {
                AddPlayerIntegral(player.summonerId, player.integral);
            });
            if (!CheckFullBureau())
            {
                //排名次
                List<CompetitionPlayer> playerList = comPlayerList.FindAll(element => element.status == CompPlayerStatus.ECPS_Wait);
                playerList.Sort(ModuleManager.Get<SpecialActivitiesMain>().CompareTileByIntegra);
                //少了个房间
                houseCount -= 1;
                int rank = 0;
                playerList.ForEach(player =>
                {
                    rank += 1;
                    player.rank = rank;
                    ModuleManager.Get<SpecialActivitiesMain>().SendPlayerRank(player.summonerId, player.rank, curAdmitNum, houseCount);
                });
            }
            if (!comPlayerList.Exists(element => (element.status == CompPlayerStatus.ECPS_Game)))
            {
                waitCount = 0;
                ChangeCompetitionStatus(CompetitionStatus.ECS_Wait);
            }
        }
        private void AddPlayerIntegral(ulong summonerId, int integral)
        {
            CompetitionPlayer player = comPlayerList.Find(element => element.summonerId == summonerId);
            if (player != null)
            {
                player.allIntegral = integral;
                player.status = CompPlayerStatus.ECPS_Wait;
            }
        }
        public bool CheckPlayerExist(ulong summonerId)
        {
            return comPlayerList.Exists(element => element.summonerId == summonerId);
        }
        public bool CheckFullBureau()
        {
            return currentBureau == maxBureau;
        }
        public bool CheckFullPlayerNum()
        {
            return maxApplyNum == comPlayerList.Count;
        }
        public void GetComPlayerInfo(int min, int max, List<ComPlayerNode> _comPlayerList)
        {
            for(int i = min; i < max && i < GetComPlayerCount(); ++i)
            {
                ComPlayerNode playerNode = new ComPlayerNode();
                playerNode.summonerId = comPlayerList[i].summonerId;
                playerNode.nickName = comPlayerList[i].nickName;
                playerNode.playerStatus = comPlayerList[i].status;
                playerNode.rank = comPlayerList[i].rank;
                _comPlayerList.Add(playerNode);
            }
        }
        public void SendPlayerQuitCompetition()
        {
            if (status != CompetitionStatus.ECS_Apply)
            {
                return;
            }
            comPlayerList.ForEach(player =>
            {
                ModuleManager.Get<SpecialActivitiesMain>().SendPlayerQuitCompetition(player.summonerId);
            });
        }
    }
    //商家
    public class Market
    {
        public int marketId;                //商家Id
        private ConcurrentDictionary<int, Competition> m_Competitions = new ConcurrentDictionary<int, Competition>();
        public Market() { }
        public Market(int marketId)
        {
            this.marketId = marketId;
        }
        public int GetCompetitionCount()
        {
            return m_Competitions.Count;
        }
        public void AddCompetition(int competitionKey, Competition competition)
        {
            if (m_Competitions.ContainsKey(competitionKey))
            {
                m_Competitions[competitionKey] = competition;
            }
            else
            {
                m_Competitions.TryAdd(competitionKey, competition);
            }
        }
        public bool IsCompetitionExist(int competitionKey)
        {
            return m_Competitions.ContainsKey(competitionKey);
        }
        public bool IsCompetitionExist(int competitionKey, ulong summonerId)
        {
            Competition competition = GetCompetition(competitionKey);
            if (competition != null && competition.CheckPlayerExist(summonerId))
            {
                return true;
            }
            return false;
        }
        //通过商家Id获取比赛场
        public Competition GetCompetition(int competitionKey)
        {
            if (m_Competitions.ContainsKey(competitionKey))
            {
                return m_Competitions[competitionKey];
            }
            return null;
        }
        //移除比赛场
        public void RemoveCompetition(int competitionKey)
        {
            Competition competition = null;
            m_Competitions.TryRemove(competitionKey, out competition);
            
            if (competition != null)
            {
                competition.SendPlayerQuitCompetition();
            }
        }
        //获取口令信息
        public void GetCompetitionInfo(List<MarketComNode> marketComList)
        {
            var competitionList = m_Competitions.ToList();
            competitionList.ForEach(competition =>
            {
                MarketComNode comNode = new MarketComNode();
                comNode.competitionKey = competition.Value.competitionKey;
                comNode.joinPalyerNum = competition.Value.GetComPlayerCount();
                comNode.maxApplyNum = competition.Value.maxApplyNum;
                comNode.firstAdmitNum = competition.Value.firstAdmitNum;
                comNode.createTime = competition.Value.createTime.ToString();
                marketComList.Add(comNode);
            });
        }
        //获取报名状态比赛场的个数
        public int GetApplyCompetitionNum()
        {
            List<Competition> applyCompetitionList = m_Competitions.Values.Where(element => element.status == CompetitionStatus.ECS_Apply).ToList();
            if (applyCompetitionList != null)
            {
                return applyCompetitionList.Count;
            }
            return 0;
        }
    }
    public class CompetitionManager
    {
        private static object singletonLocker = new object();   //单例锁
        private static CompetitionManager instance = null;
        private ConcurrentDictionary<int, Market> m_Markets = new ConcurrentDictionary<int, Market>();
        private CompetitionManager() { }
        public static CompetitionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock(singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new CompetitionManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }
        private void Init()
        {
        }
        public int GetMarketCount()
        {
            return m_Markets.Count;
        }
        //新增商场
        public void CreateMarket(int marketId, out Market market)
        {
            market = new Market(marketId);
            AddMarket(marketId, market);
        }
        private void AddMarket(int marketId, Market market)
        {
            if (m_Markets.ContainsKey(marketId))
            {
                m_Markets[marketId] = market;
            }
            else
            {
                m_Markets.TryAdd(marketId, market);
            }
        }
        public bool IsMarketExist(int marketId)
        {
            return m_Markets.ContainsKey(marketId);
        }
        //通过商家Id获取商场
        public Market GetMarket(int marketId)
        {
            if (m_Markets.ContainsKey(marketId))
            {
                return m_Markets[marketId];
            }
            return null;
        }
        //获取商场（通过口令）
        public Market TryGetMarketByKey(int competitionKey)
        {
            Market market = m_Markets.Values.FirstOrDefault(e => e.IsCompetitionExist(competitionKey));
            return market;
        }
        //检查比赛场（通过口令）
        public bool IsCompetitionExistByKey(int competitionKey)
        {
            Market market = m_Markets.Values.FirstOrDefault(e => e.IsCompetitionExist(competitionKey));
            if (market != null)
            {
                return true;
            }
            return false;
        }
        //检查比赛场（通过口令和玩家Id）
        public bool IsCompetitionExistByKey(int competitionKey, ulong summonerId)
        {
            Market market = m_Markets.Values.FirstOrDefault(e => e.IsCompetitionExist(competitionKey, summonerId));
            if (market != null)
            {
                return true;
            }
            return false;
        }
        //获取比赛场（通过口令）
        public Competition GetCompetitionByKey(int competitionKey)
        {
            Competition competition = null;
            Market market = m_Markets.Values.FirstOrDefault(e => e.IsCompetitionExist(competitionKey));
            if (market != null)
            {
                competition = market.GetCompetition(competitionKey);
            }
            return competition;
        }
        //获取比赛场（通过口令）
        public void RemoveCompetitionByKey(int competitionKey)
        {
            Market market = m_Markets.Values.FirstOrDefault(e => e.IsCompetitionExist(competitionKey));
            if (market != null)
            {
                market.RemoveCompetition(competitionKey);
                if (market.GetCompetitionCount() == 0)
                {
                    //没比赛场了
                    RemoveMarket(market.marketId);
                }
            }
        }
        //移除商家
        public void RemoveMarket(int marketId)
        {
            Market market = null;
            m_Markets.TryRemove(marketId, out market);            
        }
    }
}
