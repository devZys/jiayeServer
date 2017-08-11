using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.Summoner;
using LegendServerDB.Core;

namespace LegendServerDB.Actor.Summoner
{
    public class Summoner
    {
        public Summoner(Session session)
        {
            this.session = session;
            this.status = SummonerStatus.Offline;
            this.account = "";
            this.nickName = "";
            this.currentHero = 0;
        }
        public Session session;
        public string account;
        public string nickName;
        public DateTime loginTime;
        public int currentHero;
        public SummonerStatus status;
        public CombatGains combatGains;
        
        //更新数据到缓存
        public void UpdateDataToCache()
        {
            SummonerDB playerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.account == this.account);
            playerDB.loginTime = this.loginTime;
            playerDB.currentHero = this.currentHero;

            if (combatGains != null)
            {
                playerDB.combatGains = CompressData.CompressObject(combatGains);
            }
            ServerCPU.Instance.PushDBUpdateCommand(() => NHibernateHelper.InsertOrUpdateOrDelete<SummonerDB>(playerDB, ORMDataOperate.Update));
        }

        //增加一个道具
        public void AddItem(Item item)
        {
            if (!combatGains.itemList.Contains(item))
            {
                combatGains.itemList.Add(item);
            }
        }
        //获取一个道具
        public Item GetItem(int id)
        {
            Item item = combatGains.itemList.Find(element => element.id == id);
            return item;
        }
        //获取当前出场的道具
        public Item GetPresentItem()
        {
            var result = (from element in combatGains.itemList where element.useStatus == ItemUseStatus.Present select element);
            if (result.Count() <= 0) return null;

            return result.First<Item>();
        }
        //设置道具使用状态
        public void SetItemUseStatus(int id, ItemUseStatus status)
        {
            var result = (from element in combatGains.itemList where element.id == id select element);
            if (result.Count() <= 0) return;

            Item item = result.First<Item>();
            item.useStatus = status;

        }
        //设置所有英雄的状态
        public void SetAllItemUseStatus(ItemUseStatus status)
        {
            foreach (Item element in combatGains.itemList)
            {
                element.useStatus = status;
            }
        }
        //改变满足条件的道具的使用状态
        public void ChanageItemUseStatus(ItemUseStatus oldStatus, ItemUseStatus newStatus)
        {
            if (combatGains == null) return;

            foreach (Item element in combatGains.itemList)
            {
                if (element.useStatus == oldStatus)
                {
                    element.useStatus = newStatus;
                }
            }
        }        
       
        //响应断线
        public void OnDisconnect()
        {
            //置离线
            this.status = SummonerStatus.Offline;

            //将已出征的道具置为掉线状态
            ChanageItemUseStatus(ItemUseStatus.Present, ItemUseStatus.Disconnect);
        }        
    }
}
