using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System;
using LegendProtocol;
using LegendServerLogic.Distributed;

namespace LegendServerLogic.Actor.Summoner
{
    public class SummonerManager
    {
        private static object singletonLocker = new object();//单例双检锁
        private ConcurrentDictionary<ulong, Summoner> summonerCollection = new ConcurrentDictionary<ulong, Summoner>();

        private static SummonerManager instance = null;
        public static SummonerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new SummonerManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }
        public SummonerManager() {}
        public void Init()
        {
        }
        public ConcurrentDictionary<ulong, Summoner> GetSummonerCollection()
        {
            return summonerCollection;
        }
        public int GetSummonerCount()
        {
            return summonerCollection.Count;
        }
        //新增召唤师
        public void AddSummoner(ulong id, Summoner summoner)
        {
            if (summonerCollection.ContainsKey(id))
            {
                summonerCollection[id] = summoner;
            }
            else
            {
                summonerCollection.TryAdd(id, summoner);
            }

            ServerUtil.RecordLog(LogType.Debug, "玩家：" + summoner.userId + " 进入逻辑!");
        }
        //通过帐户名获取召唤师
        public Summoner GetSummonerByUserId(string userId)
        {
            return summonerCollection.Values.FirstOrDefault(element => element.userId == userId);
        }
        //通过昵称获取召唤师
        public Summoner GetSummonerByNickName(string nickName)
        {
            return summonerCollection.Values.FirstOrDefault(element => element.nickName == nickName);
        }
        //通过ID获取召唤师
        public Summoner GetSummonerById(ulong id)
        {
            Summoner summoner = summonerCollection.Values.FirstOrDefault(element => element.id == id);
            return summoner;
        }
        //通过帐号获取所在网关
        public int GetProxyByAccount(string userId)
        {
            Summoner summoner = summonerCollection.Values.FirstOrDefault(element => element.userId == userId);
            if (summoner == null) return 0;

            return summoner.proxyServerId;
        }
        //移除召唤师
        public void RemoveSummoner(ulong id)
        {
            ModuleManager.Get<DistributedMain>().UpdateLoadBlanceStatus(false);

            Summoner summoner = null;
            summonerCollection.TryRemove(id, out summoner);

            if (summoner != null)
            {
                summoner.status = SummonerStatus.Offline;
                ObjectPoolManager<Summoner>.Instance.FreeObject(summoner);

                ServerUtil.RecordLog(LogType.Debug, "玩家：" + summoner.userId + " 退出逻辑!");
            }
        }
        //移除召唤师
        public void RemoveSummoner(string userId)
        {
            ModuleManager.Get<DistributedMain>().UpdateLoadBlanceStatus(false);

            Summoner summoner = GetSummonerByUserId(userId);
            if (summoner == null) return;

            summonerCollection.TryRemove(summoner.id, out summoner);

            if (summoner != null)
            {
                ServerUtil.RecordLog(LogType.Debug, "玩家：" + summoner.userId + " 退出逻辑!");

                ObjectPoolManager<Summoner>.Instance.FreeObject(summoner);
            }
        }
        public bool IsSummonerExist(ulong id)
        {
            return summonerCollection.ContainsKey(id);
        }
    }
}
