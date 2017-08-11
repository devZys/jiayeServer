using LegendProtocol;
using System;
using System.Collections.Concurrent;
using LegendServer.Util;
using LegendServerWorldDefine;
using System.Linq;
using System.Collections.Generic;
using LegendServerWorld.SpecialActivities;

namespace LegendServerMarketManager
{
    //商场口令
    public class MarketKey
    {
        public MarketKeyType keyType;
        public int key;
        public DateTime buildTime;
        public DateTime destroyTime;
        public int keyUsedLimit = 1;//玩家通过该口令加入游戏的次数限制
        public List<ulong> players = new List<ulong>();
        public MarketKey() { }
        public MarketKey(MarketKeyType keyType, int key, int indate = 0)
        {
            this.keyType = keyType;
            this.key = key;
            this.buildTime = DateTime.Now;
            this.destroyTime = DateTime.Now.AddMinutes(indate);
            this.keyUsedLimit = ModuleManager.Get<SpecialActivitiesMain>().KeyUsedLimit;
        }
        //响应玩家对该口令的使用
        public void OnPlayerJoin(ulong summonerId)
        {
            players.Add(summonerId);
        }
        //获取该玩家使用该口令的次数
        public int GetPlayerJoinCount(ulong summonerId)
        {
            return players.Count(e => (e == summonerId));
        }
        //该玩家能否使用该口令
        public bool CanPlayerJoin(ulong summonerId)
        {
            return GetPlayerJoinCount(summonerId) < keyUsedLimit;
        }
        //是不是比赛场口令
        public bool IsCompetition()
        {
            return keyType == MarketKeyType.EMK_Competition;
        }
    }
    //商场
    public class Market
    {
        public int id;
        public int latestKey;
        public int logicId;
        public ConcurrentDictionary<int, MarketKey> keys = new ConcurrentDictionary<int, MarketKey>();
        public Market() { }
        public Market(int logicId)
        {
            this.logicId = logicId;
        }
        public Market(MarketKeyType keyType, int id, int logicId, int keyIndate)
        {
            this.id = id;
            this.logicId = logicId;
            MarketKey keyInfo = BuildKey(keyType, keyIndate);
            keys[keyInfo.key] = keyInfo;
        }
        public bool ExistKey(int key)
        {
            MarketKey keyInfo = null;
            return keys.TryGetValue(key, out keyInfo);
        }
        public MarketKey GetKeyInfo(int key)
        {
            MarketKey keyInfo = null;
            keys.TryGetValue(key, out keyInfo);

            return keyInfo;
        }
        public MarketKey BuildKey(MarketKeyType keyType, int id, int keyIndate)
        {
            this.id = id;
            MarketKey keyInfo = BuildKey(keyType, keyIndate);
            keys[keyInfo.key] = keyInfo;

            return keyInfo;
        }
        public MarketKey BuildKey(MarketKeyType keyType, int keyIndate)
        {
            if (MarketManager.Instance.KeyRandomBox.Size <= 0)
            {
                //口令池为空说明所以口令的生存期还未结束，哎哟不错哦~~游戏很火爆~~
                MarketManager.Instance.GenKeyCollections();
            }
            int key = MarketManager.Instance.KeyRandomBox.Next(true);
            MarketKey keyInfo = new MarketKey(keyType, key, keyIndate);
            this.keys[key] = keyInfo;
            if (keyType == MarketKeyType.EMK_Ordinary)
            {
                this.latestKey = keyInfo.key;
            }
            return keyInfo;
        }
        public void DestroyKey(int key)
        {
            MarketKey keyInfo = null;
            keys.TryRemove(key, out keyInfo);
        }
        public bool IsExistCompetition()
        {
            return null != keys.Values.FirstOrDefault(e => e.IsCompetition());
        }
    }
    //商场管理器
    public class MarketManager
    {
        private static object singletonLocker = new object();//单例双检锁
        private static MarketManager instance = null;
        public PowerRandomBox<int> KeyRandomBox = new PowerRandomBox<int>();
        public ConcurrentDictionary<int, Market> Markets = new ConcurrentDictionary<int, Market>();
        private int currentMarketKeyStartID = MarketDefine.KeyStartID;
        public static MarketManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new MarketManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }
        public MarketManager() { }
        public void Init()
        {
            GenKeyCollections();
        }
        //生成口令集合
        public void GenKeyCollections()
        {
            for(int key = currentMarketKeyStartID; key < currentMarketKeyStartID + MarketDefine.KeyCollectionsSizeStep; key++)
            {
                KeyRandomBox.AddElement(key, 1);
            }
            currentMarketKeyStartID = currentMarketKeyStartID + MarketDefine.KeyCollectionsSizeStep;
        }
        //商场营业
        public bool TryAddMarket(int id, Market market)
        {
            if (ExistMarket(id)) return false;

            Markets[id] = market;
            return true;
        }
        //商场打烊
        public bool TryRemoveMarket(int id)
        {
            Market market = null;
            return Markets.TryRemove(id, out market);
        }
        //获取商场（通过商场ID）
        public Market TryGetMarketByID(int id)
        {
            Market market = null;
            Markets.TryGetValue(id, out market);
            return market;
        }
        //获取商场（通过口令）
        public Market TryGetMarketByKey(int key)
        {
            Market market = Markets.Values.FirstOrDefault(e => e.ExistKey(key));
            return market;
        }

        //获取商场（通过口令，并返回口令信息）
        public Market TryGetMarketByKey(int key, out MarketKey keyInfo)
        {
            Market market = Markets.Values.FirstOrDefault(e => e.ExistKey(key));
            if (market != null)
            {
                keyInfo = market.GetKeyInfo(key);
            }
            else
            {
                keyInfo = null;
            }
            return market;
        }
        //获取口令信息
        public MarketKey GetKeyInfo(int key)
        {
            MarketKey keyInfo = null;
            Market market = Markets.Values.FirstOrDefault(e => e.ExistKey(key));
            if (market != null)
            {
                keyInfo = market.GetKeyInfo(key);
            }
            return keyInfo;
        }
        //是否存在该商场
        public bool ExistMarket(int id)
        {
            Market market = null;
            return Markets.TryGetValue(id, out market);
        }
        public void TryRemoveMarketKey(int key)
        {
            MarketKey keyInfo = new MarketKey();
            Market market = TryGetMarketByKey(key, out keyInfo);
            if (market != null && keyInfo != null && keyInfo.keyType == MarketKeyType.EMK_Competition)
            {
                market.DestroyKey(key);
                //此口令销毁则归还口令池
                KeyRandomBox.AddElement(key, 1);
                if (market.keys.Count <= 0)
                {
                    //此商场所有口令生存期结束则商场打烊
                    TryRemoveMarket(market.id);
                }
            }
        }
        public int GetMarketCompetitionLogicId(int marketId)
        {
            Market market = TryGetMarketByID(marketId);
            if (market == null || market.keys.Count == 0)
            {
                return 0;
            }
            if(market.IsExistCompetition())
            {
                return market.logicId;
            }
            return 0;
        }
    }
}
