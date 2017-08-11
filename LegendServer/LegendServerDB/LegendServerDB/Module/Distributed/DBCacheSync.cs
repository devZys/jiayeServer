using System;
using System.Collections.Generic;
using LegendServer.Database;
using System.Collections.Concurrent;
using LegendProtocol;
using System.Diagnostics;
using MsgPack.Serialization;

namespace LegendServerDB.Distributed
{
    //DB缓存同步者
    public class DBCacheSync<T> where T : class
    {
        public class SyncHandle
        {
            public List<string> keys;
            public Func<byte[], DataOperate, Predicate<T>, bool> func;
            public SyncHandle(List<string> keys, Func<byte[], DataOperate, Predicate<T>, bool> func)
            {
                this.keys = keys;
                this.func = func;
            }
        }
        private static DBCacheSync<T> instance = null;
        private static object objLock = new object();
        private ConcurrentDictionary<string, SyncHandle> dataTableSyncHandles = new ConcurrentDictionary<string, SyncHandle>();

        public DBCacheSync() { }

        public static DBCacheSync<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (objLock)
                    {
                        if (instance == null)
                        {
                            instance = new DBCacheSync<T>();
                        }
                    }
                }
                return instance;
            }
        }
        public bool RegistSyncTable(List<string> keys)
        {
            try
            {
                dataTableSyncHandles[typeof(T).ToString()] = new SyncHandle(keys, (byte[] bytes, DataOperate operate, Predicate<T> existJudge) =>
                {
                    T record = (T)DBCacheSyncFacade.Instance.UnSerialize(bytes, typeof(T));
                    switch (operate)
                    {
                        case DataOperate.Insert:
                            DBManager<T>.Instance.AddRecordToCache(record, existJudge, false, false, false);
                            break;
                        case DataOperate.Update:
                            DBManager<T>.Instance.UpdateRecordInCache(record, existJudge, false, false);
                            break;
                        case DataOperate.Delete:
                            DBManager<T>.Instance.DeleteRecordInCache(existJudge, int.MaxValue, false);
                            break;
                    }
                    return true;
                });

                DBCacheSyncFacade.Instance.RegistSyncGainer<T>();
                return true;
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Fatal, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return false;
            }
        }
        public SyncHandle Handle
        {
            get
            {
                SyncHandle handle = null;
                dataTableSyncHandles.TryGetValue(typeof(T).ToString(), out handle);

                return handle;
            }
        }
    }
    //数据缓存同步对外包装接口
    public class DBCacheSyncFacade
    {
        private static DBCacheSyncFacade instance = null;
        private static object objLock = new object();
        private ConcurrentDictionary<string, Action<byte[], DataOperate, List<ulong>>> syncHandleGainers = new ConcurrentDictionary<string, Action<byte[], DataOperate, List<ulong>>>();
        public Dictionary<Type, IMessagePackSingleObjectSerializer> AllObjectSerializer = new Dictionary<Type, IMessagePackSingleObjectSerializer>();
        private DBCacheSyncFacade() { }

        public static DBCacheSyncFacade Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (objLock)
                    {
                        if (instance == null)
                        {
                            instance = new DBCacheSyncFacade();
                        }
                    }
                }
                return instance;
            }
        }
        public bool RegistSyncGainer<T>() where T : class
        {
            try
            {
                RegistSerializer(typeof(T));
                syncHandleGainers[typeof(T).ToString()] = (byte[] data, DataOperate operateType, List<ulong> values) =>
                {
                    DBCacheSync<T>.SyncHandle handle = DBCacheSync<T>.Instance.Handle;
                    if ((handle.keys.Count <= 0) || (handle.keys.Count != values.Count))
                    {
                        ServerUtil.RecordLog(LogType.Fatal, "需要同步的数据表的关键字段名个数为0或者与其对应的字段值的个数不一值", new StackTrace(new StackFrame(true)).GetFrame(0));
                        return;
                    }

                    if (handle.keys.Count >= 2)
                    {
                        handle.func(data, operateType, element => (ulong)(element.GetType().GetProperty(handle.keys[0]).GetValue(element)) == values[0] && (ulong)(element.GetType().GetProperty(handle.keys[1]).GetValue(element)) == values[1]);
                    }
                    else
                    {
                        if (handle.keys.Count >= 1)
                        {
                            handle.func(data, operateType, element => (ulong)(element.GetType().GetProperty(handle.keys[0]).GetValue(element)) == values[0]);
                        }
                    }

                };
                return true;
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Fatal, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return false;
            }
        }
        public Action<byte[], DataOperate, List<ulong>> Handle(string key)
        {
            Action<byte[], DataOperate, List<ulong>> handle = null;
            syncHandleGainers.TryGetValue(key, out handle);

            return handle;
        }
        public bool IsMustSync<T>() where T : class
        {
            return syncHandleGainers.ContainsKey(typeof(T).ToString());
        }
        public void RegistSerializer(Type type)
        {
            if (!AllObjectSerializer.ContainsKey(type))
            {
                AllObjectSerializer.Add(type, SerializationContext.Default.GetSerializer(type));
            }
        }
        public byte[] Serialize(object obj)
        {
            try
            {
                byte[] msgByte = AllObjectSerializer[obj.GetType()].PackSingleObject(obj);

                return msgByte;
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return null;
            }
        }
        public object UnSerialize(byte[] objByte, Type type)
        {
            try
            {
                return AllObjectSerializer[type].UnpackSingleObject(objByte);
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return null;
            }
        }
    }
}
