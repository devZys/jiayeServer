using System;
using System.Collections.Generic;
using System.Linq;
using LegendServerDB.Core;
using System.Collections.Concurrent;
using LegendProtocol;
using LegendServerDB.Distributed;
using System.Diagnostics;

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
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[key];
                }
                else
                {
                    return databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        //取得缓存中的记录数
        public int GetRecordCountInCache()
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].Count;
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).Count;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }
        //返回缓存记录中某元素最大的那条记录
        public T GetMaxRecord<R>(Func<T, R> element)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].OrderByDescending(element).FirstOrDefault();
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).OrderByDescending(element).FirstOrDefault();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        //返回满足条件的缓存记录中某元素最大的那条记录
        public T GetMaxRecord<R>(Predicate<T> condition, Func<T, R> element)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].FindAll(condition).OrderByDescending(element).FirstOrDefault();
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).FindAll(condition).OrderByDescending(element).FirstOrDefault();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        //返回缓存记录中某元素最小的那条记录
        public T GetMinRecord<R>(Func<T, R> element)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].OrderBy(element).FirstOrDefault();
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).OrderBy(element).FirstOrDefault();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        //返回满足条件的缓存记录中某元素最小的那条记录
        public T GetMinRecord<R>(Predicate<T> condition, Func<T, R> element)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].FindAll(condition).OrderBy(element).FirstOrDefault();
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).FindAll(condition).OrderBy(element).FirstOrDefault();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        //获取满足条件的记录的个数
        public int GetRecordCount(Func<T, bool> condition)
        {
            string key = typeof(T).ToString();
            if (databaseCache.ContainsKey(key))
            {
                return databaseCache[typeof(T).ToString()].Count(condition);
            }
            else
            {
                return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).Count(condition);
            }
        }
        //取得缓存中的记录(带条件)
        public List<T> GetRecordsInCache(Predicate<T> condition)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].FindAll(condition);
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).FindAll(condition);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        //取降序N条记录
        public List<T> GetDescendingRecordsInCache<R>(Func<T, R> element, int count)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].OrderByDescending(element).Take(count).ToList();
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).OrderByDescending(element).Take(count).ToList();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        //取升序N条记录
        public List<T> GetAscendingRecordsInCache<R>(Func<T, R> element, int count)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].OrderBy(element).Take(count).ToList();
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).OrderBy(element).Take(count).ToList();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        //取降序N条记录(带条件)
        public List<T> GetDescendingRecordsInCache<R>(Predicate<T> condition, Func<T, R> element, int count)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].FindAll(condition).OrderByDescending(element).Take(count).ToList();
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).FindAll(condition).OrderByDescending(element).Take(count).ToList();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        //取升序N条记录(带条件)
        public List<T> GetAscendingRecordsInCache<R>(Predicate<T> condition, Func<T, R> element, int count)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].FindAll(condition).OrderBy(element).Take(count).ToList();
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).FindAll(condition).OrderBy(element).Take(count).ToList();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        //是否在缓存中存在满足条件的记录
        public bool ExistRecordsInCache(Predicate<T> existJudge)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].Exists(existJudge);
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).Exists(existJudge);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        //取得缓存中的满足条件首条记录
        public T GetSingleRecordInCache(Predicate<T> condition)
        {
            try
            {
                string key = typeof(T).ToString();
                if (databaseCache.ContainsKey(key))
                {
                    return databaseCache[typeof(T).ToString()].Find(condition);
                }
                else
                {
                    return (databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()).Find(condition);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
       
        //在缓存中更新记录
        public bool UpdateRecordInCache(T record, Predicate<T> existJudge, bool saveDB = true, bool sync = true)
        {
            string key = typeof(T).ToString();
            List<T> records = null;
            if (!databaseCache.TryGetValue(key, out records))
            {
                records = ((databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()));
            }

            T result = records.Find(existJudge);
            if (result == default(T)) return false;

            records.Remove(result);
            records.Add(record);

            if (sync)
            {
                ServerCPU.Instance.PushDBSyncCommand(() => Sync(record, DataOperate.Update));
            }
            if (saveDB)
            {
                ServerCPU.Instance.PushDBUpdateCommand(() => NHibernateHelper.InsertOrUpdateOrDelete(record, DataOperate.Update));
            }

            return true;
        }

        //往缓存中增加记录
        public bool AddRecordToCache(T record, Predicate<T> existJudge, bool isProfiler = false, bool saveDB = true, bool sync = true)
        {
            try
            {
                string key = typeof(T).ToString();
                List<T> records = null;
                if (!databaseCache.TryGetValue(key, out records))
                {
                    records = ((databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()));
                }

                bool exist = records.Exists(existJudge);
                if (exist) return false;

                records.Add(record);

                if (!isProfiler)
                {
                    if (sync)
                    {
                        ServerCPU.Instance.PushDBSyncCommand(() => Sync(record, DataOperate.Insert));
                    }
                    if (saveDB)
                    {
                        ServerCPU.Instance.PushDBUpdateCommand(() => NHibernateHelper.InsertOrUpdateOrDelete<T>(record, DataOperate.Insert));
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return false;
            }
        }

        //在缓存中删除记录
        public int DeleteRecordInCache(Predicate<T> condition, int count = int.MaxValue, bool sync = true)
        {
            try
            {
                string key = typeof(T).ToString();
                List<T> records = null;
                if (!databaseCache.TryGetValue(key, out records))
                {
                    records = ((databaseCache[key] = NHibernateHelper.GetRecords<T>().ToList()));
                }

                List<T> result = records.FindAll(condition).Take(count).ToList();

                if (sync)
                {
                    result.ForEach(element => ServerCPU.Instance.PushDBSyncCommand(() => Sync(element, DataOperate.Delete)));
                    result.ForEach(element => ServerCPU.Instance.PushDBUpdateCommand(() => NHibernateHelper.InsertOrUpdateOrDelete<T>(element, DataOperate.Delete)));
                }

                result.ForEach(element => records.Remove(element));
                return result.Count;

            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return 0;
            }
        }      
        //同步缓存
        public void Sync(T record, DataOperate operate)
        {
            try
            {
                DBCacheSync<T>.SyncHandle handle = DBCacheSync<T>.Instance.Handle;
                if (handle != null)
                {
                    List<ulong> values = new List<ulong>();
                    for (int index = 0; index < handle.keys.Count; index++)
                    {
                        ulong val = (ulong)(record.GetType().GetProperty(handle.keys[index]).GetValue(record));
                        values.Add(val);
                    }
                    ModuleManager.Get<DistributedMain>().SyncCache(record, values, operate);
                }
            }
            catch(Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
            }
        }
    }
}
