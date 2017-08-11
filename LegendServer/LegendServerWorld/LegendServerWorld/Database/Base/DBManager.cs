using System;
using System.Collections.Generic;
using System.Linq;
using LegendServerWorld.Core;
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
        //取得缓存中的记录(带条件)
        public List<T> GetRecordsInCache(Predicate<T> condition)
        {
            try
            {
                return databaseCache[typeof(T).ToString()].FindAll(condition);
            }
            catch (Exception)
            {
                return null;
            }
        }

        //往缓存中增加记录
        public bool AddRecordToCache(T record, Predicate<T> existJudge, bool isProfiler = false)
        {
            try
            {
                List<T> records = databaseCache[typeof(T).ToString()];
                bool exist = records.Exists(existJudge);
                if (exist) return false;

                records.Add(record);

                if (!isProfiler)
                {
                    ServerCPU.Instance.PushDBUpdateCommand(() => NHibernateHelper.InsertOrUpdateOrDelete<T>(record, DataOperate.Insert));
                }
                return true;
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex);
                return false;
            }
        }
        //在缓存中更新记录
        public bool UpdateRecordInCache(T record, Predicate<T> existJudge, bool saveDB = true)
        {
            List<T> records = databaseCache[typeof(T).ToString()];
            T result = records.Find(existJudge);
            if (result == default(T)) return false;

            records.Remove(result);
            records.Add(record);

            if (saveDB)
            {
                ServerCPU.Instance.PushDBUpdateCommand(() => NHibernateHelper.InsertOrUpdateOrDelete(record, DataOperate.Update));
            }

            return true;
        }

        //在缓存中删除记录
        public int DeleteRecordInCache(Predicate<T> condition, int count = int.MaxValue)
        {
            try
            {
                List<T> records = databaseCache[typeof(T).ToString()];
                List<T> result = records.FindAll(condition).Take(count).ToList();

                result.ForEach(element => ServerCPU.Instance.PushDBUpdateCommand(() => NHibernateHelper.InsertOrUpdateOrDelete<T>(element, DataOperate.Delete)));
                result.ForEach(element => records.Remove(element));
                return result.Count;

            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex);
                return 0;
            }
        }
        //返回满足条件的缓存记录中某元素最小的那条记录
        public T GetMinRecord<R>(Predicate<T> condition, Func<T, R> element)
        {
            try
            {
                return databaseCache[typeof(T).ToString()].FindAll(condition).OrderBy(element).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }
        public void ClearRecordInCache()
        {
            try
            {
                List<T> records = null;
                databaseCache.TryRemove(typeof(T).ToString(), out records);

                records = null;
            }
            catch (System.Exception)
            {
                return;
            }
        }
    }
}
