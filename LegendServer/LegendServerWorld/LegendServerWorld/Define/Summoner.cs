using System;
using System.Linq;
using System.Collections.Concurrent;
using LegendProtocol;
using System.Collections.Generic;
using LegendServerWorld.Core;

namespace LegendServerWorldDefine
{
    public class Summoner : ObjectBase
    {
        public Summoner()
        {
            this.id = 0;
            this.userId = "";
            this.nickName = "";
            this.proxyId = 0;
            this.logicId = 0;
        }

        public override void Init(params object[] paramList)
        {
            if (paramList.Length < 7) return;         

            this.id = (ulong)paramList[0];
            this.userId = (string)paramList[1];
            this.nickName = (string)paramList[2];
            DateTime.TryParse((string)paramList[3], out this.loginTime);
            this.acId = (int)paramList[4];
            this.proxyId = (int)paramList[5];
            this.logicId = (int)paramList[6];
        }
        public ulong id;
        public string userId;
        public string nickName;
        public DateTime loginTime;
        public int acId;
        public int proxyId;
        public int logicId;
    }

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
        public SummonerManager() { }
        public void Init()
        {
        }
        public ConcurrentDictionary<ulong, Summoner> GetSummonerCollection()
        {
            return summonerCollection;
        }
        //新增召唤师
        public void AddSummoner(ulong id, Summoner summoner)
        {
            summonerCollection.TryAdd(id, summoner);
        }
        //通过用户ID获取召唤师
        public Summoner GetSummoner(string userId)
        {
            return summonerCollection.Values.FirstOrDefault(element => element.userId == userId);
        }
        //通过ID获取召唤师
        public Summoner GetSummoner(ulong id)
        {
            Summoner summoner;
            summonerCollection.TryGetValue(id, out summoner);
            return summoner;
        }
        //获取指定条件的召唤师
        public Summoner GetSummoner(Func<Summoner, bool> judge)
        {
            return summonerCollection.Values.FirstOrDefault(judge);
        }
        //获取指定条件的召唤师所在的逻辑服编号
        public int GetLogicIdBySummoner(Func<Summoner, bool> judge)
        {
            Summoner summoner = summonerCollection.Values.FirstOrDefault(judge);
            if (summoner != null)
            {
                return summoner.logicId;
            }
            return 0;
        }
        //移除召唤师
        public Summoner RemoveSummoner(ulong id)
        {
            Summoner summoner = null;
            summonerCollection.TryRemove(id, out summoner);

            return summoner;
        }
    }
}
