using System;
using System.Collections.Generic;
using System.Linq;
using LegendServerCenter.Core;
using System.Collections.Concurrent;
using LegendProtocol;

namespace LegendServer.Database
{
    //数据库管理者（由主线程与守护线程异步调用，守护线程只负责取缓存，不负责写，守护线程在将缓存数据更新到DB时有可能过程中数据已经被更改，这需求是合理的，因此不需要为缓存加锁）
    public class DBManager<T> where T : class
    {
        private static DBManager<T> instance = null;
        private static object objLock = new object();
        private ConcurrentDictionary<string, List<T>> databaseCache = new ConcurrentDictionary<string, List<T>>();

        private DBManager() { }

        public static DBManager<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (objLock)
                    {
                        if (instance == null)
                        {
                            instance = new DBManager<T>();
                        }
                    }
                }
                return instance;
            }
        }

        //加载数据库里数据到缓存
        public string LoadDataToCache()
        {
            try
            {
                databaseCache[typeof(T).ToString()] = NHibernateHelper.GetRecords<T>().ToList();
                return null;
            }
            catch (Exception ex)
            {
                return typeof(T).ToString() + " -> ExceptionMessage:" + ex.Message + (ex.InnerException != null ? ("InnerExceptionMessage:" + ex.InnerException.Message) : "");
            }
        }
        //取得缓存中的记录
        public List<T> GetRecordsInCache()
        {
            try
            {
                return databaseCache[typeof(T).ToString()];
            }
            catch (Exception)
            {
                return null;
            }
        }
        //模块key是否存在字典中
        public bool ExistModule()
        {
            return databaseCache.ContainsKey(typeof(T).ToString());
        }

        //取得缓存中的满足条件首条记录
        public T GetSingleRecordInCache(Predicate<T> condition)
        {
            try
            {
                return databaseCache[typeof(T).ToString()].Find(condition);
            }
            catch (Exception)
            {
                return null;
            }
        }

        //往缓存中增加记录
        public bool AddRecordToCache(T record, Predicate<T> existJudge)
        {
            try
            {
                List<T> records = databaseCache[typeof(T).ToString()];
                bool exist = records.Exists(existJudge);
                if (exist) return false;

                records.Add(record);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }     
    }
}
