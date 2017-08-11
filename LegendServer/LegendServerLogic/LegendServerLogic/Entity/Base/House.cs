using LegendProtocol;
using System;
using System.Collections.Generic;

namespace LegendServerLogic.Entity.Base
{
    public abstract class House
    {
        //房卡ID
        public int houseCardId;
        //房间ID
        public ulong houseId;
        //所属逻辑服Id
        public int logicId;
        //当前局数
        public int currentBureau;
        //最大局数
        public int maxBureau;
        //当前出牌属于谁
        public int currentWhoPlay;
        //当前该谁出牌
        public int currentShowCard;
        //房间最大人数
        public int maxPlayerNum;
        //商家ID
        public int businessId;
        //商家比赛场口令
        public int competitionKey;
        //房间类型
        public HouseType houseType;
        //房间创建时间
        public DateTime createTime;
        //房间投票开始时间
        public DateTime voteBeginTime;
        //操作开始时间
        public DateTime operateBeginTime;
        //玩家列表
        protected List<Player> summonerList;

#if MAHJONG
        public virtual MahjongHouseStatus GetMahjongHouseStatus()
        {
            return MahjongHouseStatus.MHS_FreeBureau;
        }
#elif RUNFAST
        public virtual RunFastHouseStatus GetRunFastHouseStatus()
        {
            return RunFastHouseStatus.RFHS_FreeBureau;
        }
#elif WORDPLATE
        public virtual WordPlateHouseStatus GetWordPlateHouseStatus()
        {
            return WordPlateHouseStatus.EWPS_FreeBureau;
        }
#endif

        public int RemovePlayer(string userId)
        {
            return summonerList.RemoveAll(element => element.userId == userId);
        }
        public bool CheckPlayer(string userId)
        {
            return summonerList.Exists(element => element.userId == userId);
        }
        public bool CheckPlayerFull()
        {
            if (summonerList.Count == this.maxPlayerNum)
            {
                return true;
            }
            return false;
        }
        public VoteStatus GetDissolveHouseVote()
        {
            int oppose = 1;
            int agree = maxPlayerNum - 1;
            int opposeCount = summonerList.FindAll(element => element.voteStatus == VoteStatus.OpposeVote).Count;
            if (opposeCount >= oppose)
            {
                return VoteStatus.OpposeVote;
            }
            int agreeCount = summonerList.FindAll(element => element.voteStatus == VoteStatus.AgreeVote).Count;
            if (agreeCount >= agree)
            {
                return VoteStatus.AgreeVote;
            }
            return VoteStatus.FreeVote;
        }
        public bool CheckeHouseBureau()
        {
            if (currentBureau == maxBureau)
            {
                return true;
            }
            return false;
        }
        public bool CheckeFirstBureauEnd()
        {
            if (currentBureau == 1)
            {
                return true;
            }
            return false;
        }
        public int GetNextHousePlayerIndex(int index)
        {
            int nextIndex = index + 1;
            if (nextIndex >= summonerList.Count)
            {
                nextIndex = 0;
            }
            return nextIndex;
        }
        public int GetLastHousePlayerIndex(int index)
        {
            int lastIndex = index - 1;
            if (lastIndex < 0)
            {
                lastIndex = summonerList.Count - 1;
            }
            return lastIndex;
        }
        protected int GetHousePlayerIndex()
        {
            for (int index = 0; index <= summonerList.Count; ++index)
            {
                if (null == GetHousePlayer(index))
                {
                    return index;
                }
            }
            return summonerList.Count;
        }
        public List<Player> GetHousePlayer()
        {
            return summonerList;
        }
        public Player GetHousePlayer(string userId)
        {
            return summonerList.Find(element => element.userId == userId);
        }
        public Player GetHousePlayer(int index)
        {
            return summonerList.Find(element => element.index == index);
        }
        public Player GetHousePlayerBySummonerId(ulong summonerId)
        {
            return summonerList.Find(element => element.summonerId == summonerId);
        }
        public List<Player> GetOtherHousePlayer(string userId)
        {
            return summonerList.FindAll(element => element.userId != userId);
        }
        public int GetHousePlayerCount()
        {
            return summonerList.Count;
        }
        public int GetCurrentRanking(int index, int score)
        {
            int ranking = 0;
            for (int i = 0; i < summonerList.Count; ++i)
            {
                if (index == i)
                    continue;

                if (summonerList[i].allIntegral > score)
                {
                    ranking++;
                }

            }
            return ranking;
        }
        public bool SetHouseOperateBeginTime()
        {
            if (businessId > 0)
            {
                operateBeginTime = DateTime.Now;
                return true;
            }
            return false;
        }
        public bool CheckNeedAddBureau()
        {
            if (businessId > 0 && currentBureau == maxBureau)
            {
                int _allIntegral = summonerList[0].allIntegral;
                bool bResult = false;
                for (int i = 1; i < summonerList.Count; ++i)
                {
                    if (_allIntegral == summonerList[i].allIntegral)
                    {
                        bResult = true;
                    }
                    else if (_allIntegral < summonerList[i].allIntegral)
                    {
                        _allIntegral = summonerList[i].allIntegral;
                        bResult = false;
                    }
                }
                if (bResult)
                {
                    maxBureau += 1;
                    return true;
                }
            }
            return false;
        }
    }
}
