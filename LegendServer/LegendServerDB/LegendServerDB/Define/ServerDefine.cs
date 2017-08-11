using LegendProtocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LegendServerDBDefine
{
    public class PreRegistSummoner
    {
        public string nickName;
        public string headImgUrl;
        public UserSex sex;
        public int acPeerId;
        public int acServerId;
        public string requesterIp;
        public PreRegistSummoner(string nickName, string headImgUrl, UserSex sex, int acPeerId, int acServerId, string requesterIp)
        {
            this.nickName = nickName;
            this.headImgUrl = headImgUrl;
            this.sex = sex;
            this.acPeerId = acPeerId;
            this.acServerId = acServerId;
            this.requesterIp = requesterIp;
        }
    }
    public class CompetitionKeyManager
    {
        private static object singletonLocker = new object();   //单例锁
        private static CompetitionKeyManager instance = null;
        private ConcurrentDictionary<int, int> m_CompetitionKeys = new ConcurrentDictionary<int, int>();
        private CompetitionKeyManager() { }
        public static CompetitionKeyManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new CompetitionKeyManager();
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
        public void AddCompetitionKey(int competitionKey, int logicId)
        {
            if (m_CompetitionKeys.ContainsKey(competitionKey))
            {
                m_CompetitionKeys[competitionKey] = logicId;
            }
            else
            {
                m_CompetitionKeys.TryAdd(competitionKey, logicId);
            }
        }
        public void RemoveCompetitionKey(int competitionKey)
        {
            int logicId = 0;
            m_CompetitionKeys.TryRemove(competitionKey, out logicId);
        }
        public int GetLogicIdByCompetitionKey(int competitionKey)
        {
            if (m_CompetitionKeys.ContainsKey(competitionKey))
            {
                return m_CompetitionKeys[competitionKey];
            }
            return 0;
        }
    }
}
