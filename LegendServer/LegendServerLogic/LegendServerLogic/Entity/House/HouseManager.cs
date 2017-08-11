using System.Linq;
using System.Collections.Concurrent;
using LegendServerLogic.Entity.Base;
using System.Collections.Generic;
using System;
using LegendProtocol;
using LegendServerLogic.UIDAlloc;

namespace LegendServerLogic.Entity.Houses
{
    public class HouseManager
    {
        private static object singletonLocker = new object();//单例双检锁
        private ConcurrentDictionary<ulong, House> houseCollection = new ConcurrentDictionary<ulong, House>();

        private static HouseManager instance = null;
        public static HouseManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new HouseManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }
        public HouseManager() {}
        public void Init()
        {
        }
        public ConcurrentDictionary<ulong, House> GetHouseCollection()
        {
            return houseCollection;
        }
        public int GetHouseCount()
        {
            return houseCollection.Count;
        }
        //新增房间
        public void AddHouse(ulong id, House house)
        {
            if (houseCollection.ContainsKey(id))
            {
                houseCollection[id] = house;
            }
            else
            {
                houseCollection.TryAdd(id, house);
            }
        }
        //通过房间ID获取房间
        public House GetHouseById(ulong houseId)
        {
            if (houseCollection.ContainsKey(houseId))
            {
                return houseCollection[houseId];
            }
            return null;
        }  
        //通过条件获取
        public House GetHouseByCondition(Func<House, bool> condition)
        {
            House house = houseCollection.Values.FirstOrDefault(condition);
            return house;
        }
        //通过条件获取
        public List<House> GetHouseListByCondition(Func<House, bool> condition)
        {
            return houseCollection.Values.Where(condition).ToList();
        }
        //通过房卡ID获取房间
        public House GetHouseById(int houseCardId)
        {
            return houseCollection.Values.FirstOrDefault(element => element.houseCardId == houseCardId);
        }
        //移除房间
        public void RemoveHouse(ulong id)
        {
            House house = null;
            houseCollection.TryRemove(id, out house);

            //归还房卡ID
            if (house != null && house.houseCardId > 0)
            {
                ModuleManager.Get<UIDAllocMain>().msg_proxy.NotifyRecycleUID(UIDType.RoomID, (ulong)house.houseCardId);
            }
        }
    }
}
