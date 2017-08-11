using System.Diagnostics;
using LegendProtocol;
using LegendServer.Database;
using LegendServer.Database.ServiceBox;
using LegendServerDB.Core;
using System.Collections.Generic;
using LegendServer.Database.Summoner;
using LegendServerDB.Distributed;
using System;

namespace LegendServerDB.ServiceBox
{
    public class ServiceBoxMsgProxy : ServerMsgProxy
    {
        private ServiceBoxMain main;
        private int currentSyncRecordIndex = 0;

        public ServiceBoxMsgProxy(ServiceBoxMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnRequestGetDBCacheData(int peerId, bool inbound, object msg)
        {
            RequestGetDBCacheData_L2D reqMsg_L2D = msg as RequestGetDBCacheData_L2D;

            ReplyGetDBCacheData_D2L replyMsg_D2L = new ReplyGetDBCacheData_D2L();
            replyMsg_D2L.userId = reqMsg_L2D.userId;
            replyMsg_D2L.dbServerId = reqMsg_L2D.dbServerId;
            replyMsg_D2L.paramTotal = 0;

            DBCacheSyncTestDB record = DBManager<DBCacheSyncTestDB>.Instance.GetSingleRecordInCache(element => element.guid == reqMsg_L2D.guid);
            if (record != null)
            {
                CacheSyncTestInfo testInfo = Serializer.tryUncompressObject<CacheSyncTestInfo>(record.data);
                if (testInfo != null)
                {
                    replyMsg_D2L.paramTotal = testInfo.param1 + testInfo.param2;
                }
            }

            SendMsg(peerId, inbound, replyMsg_D2L);
        }

        public void OnRequestTestDBCacheSync(int peerId, bool inbound, object msg)
        {
            RequestTestDBCacheSync_L2D reqMsg_L2D = msg as RequestTestDBCacheSync_L2D;

            DBManager<DBCacheSyncTestDB>.Instance.DeleteRecordInCache(element => element.guid > 0);

            ReplyTestDBCacheSync_D2L replyMsg_D2L = new ReplyTestDBCacheSync_D2L();
            replyMsg_D2L.userId = reqMsg_L2D.userId;

            ulong newGuid = (ulong)(reqMsg_L2D.data.param1 + reqMsg_L2D.data.param2);
            DBManager<DBCacheSyncTestDB>.Instance.AddRecordToCache(new DBCacheSyncTestDB() { guid = newGuid, data = Serializer.tryCompressObject(reqMsg_L2D.data) }, element => element.guid == newGuid);
            DBCacheSyncTestDB dbRecord = DBManager<DBCacheSyncTestDB>.Instance.GetSingleRecordInCache(element => element.guid == newGuid);
            if (dbRecord != null)
            {
                CacheSyncTestInfo dataFinal = Serializer.tryUncompressObject<CacheSyncTestInfo>(dbRecord.data);
                if (dataFinal != null)
                {
                    replyMsg_D2L.data = dataFinal;
                }
                else
                {
                    replyMsg_D2L.data = new CacheSyncTestInfo() { param1 = 0, param2 = 0 };
                }
            }
            else
            {
                replyMsg_D2L.data = new CacheSyncTestInfo() { param1 = 0, param2 = 0 };
            }

            SendMsg(peerId, inbound, replyMsg_D2L);
        }

        public void SendShowRunningDBCache(string dbCacheInstance)
        {
            ReplyShowRunningDBCache_X2A replyMsg = new ReplyShowRunningDBCache_X2A();

            if (string.IsNullOrEmpty(dbCacheInstance))
            {
                replyMsg.dbCacheInstance = "服务器内部错误!【未识别的数据库操作】";
                replyMsg.senderBoxPeerId = NHibernateHelper.RunningCacheSender.boxPeerId;
                SendACMsg(replyMsg, NHibernateHelper.RunningCacheSender.acServerId);
                return;
            }
            
            replyMsg.senderBoxPeerId = NHibernateHelper.RunningCacheSender.boxPeerId;
            replyMsg.dbCacheInstance = dbCacheInstance;
            replyMsg.show = true;
            replyMsg.fromServerName = root.ServerID + "号 数据库服务器";
            SendACMsg(replyMsg, NHibernateHelper.RunningCacheSender.acServerId);
        }

        public void OnRequestTestDB(int peerId, bool inbound, object msg)
        {
            RequestTestDB_L2D reqMsg_L2D = msg as RequestTestDB_L2D;

            ReplyTestDB_D2L replyMsg_D2L = new ReplyTestDB_D2L();
            replyMsg_D2L.userId = reqMsg_L2D.userId;

            switch (reqMsg_L2D.operate)
            {
                //插入操作
                case DataOperate.Insert:
                    replyMsg_D2L.result = InsertTestData(reqMsg_L2D.strategy, reqMsg_L2D.loop);
                    break;
                //查询操作
                case DataOperate.Select:
                    replyMsg_D2L.result = SelectTestData(reqMsg_L2D.strategy, reqMsg_L2D.loop);
                    break;
                //更新操作
                case DataOperate.Update:
                    replyMsg_D2L.result = UpdateTestData(reqMsg_L2D.strategy, reqMsg_L2D.loop);
                    break;
                //删除操作
                case DataOperate.Delete:
                    replyMsg_D2L.result = DeleteTestData(reqMsg_L2D.strategy, reqMsg_L2D.loop);
                    break;
                default:
                    replyMsg_D2L.result = double.MaxValue;
                    break;
            }
            
            SendMsg(peerId, inbound, replyMsg_D2L);
        }
        private double InsertTestData(DataStrategy strategy, int loop)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            if (strategy == DataStrategy.Cache)
            {
                //缓存处理
                for (int index = 0; index < loop; index++)
                {
                    ServiceBoxTestDB testDB = new ServiceBoxTestDB();
                    testDB.guid = MyGuid.NewGuid(ServiceType.ServiceBox, (uint)root.ServerID);
                    testDB.param1 = 1;
                    testDB.param2 = 2;
                    testDB.param3 = 3;
                    testDB.param4 = 4.0f;
                    testDB.param5 = 5.0f;
                    testDB.param6 = 6.0f;
                    testDB.param7 = "7777777";
                    testDB.param8 = "88888888";
                    testDB.param9 = "999999999";

                    DBManager<ServiceBoxTestDB>.Instance.AddRecordToCache(testDB, element => element.guid == testDB.guid);
                }
            }
            else
            {
                if (strategy == DataStrategy.ORM)
                {
                    //ORM处理
                    for (int index = 0; index < loop; index++)
                    {
                        ServiceBoxTestDB testDB = new ServiceBoxTestDB();
                        testDB.guid = MyGuid.NewGuid(ServiceType.ServiceBox, (uint)root.ServerID);
                        testDB.param1 = 1;
                        testDB.param2 = 2;
                        testDB.param3 = 3;
                        testDB.param4 = 4.0f;
                        testDB.param5 = 5.0f;
                        testDB.param6 = 6.0f;
                        testDB.param7 = "7777777";
                        testDB.param8 = "88888888";
                        testDB.param9 = "999999999";
                        
                        NHibernateHelper.InsertOrUpdateOrDelete<ServiceBoxTestDB>(testDB, DataOperate.Insert);
                    }
                }
            }
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds;
        }
        private double SelectTestData(DataStrategy strategy, int loop)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            if (strategy == DataStrategy.Cache)
            {
                //缓存处理
                if (loop == 1)
                {
                    ServiceBoxTestDB record = DBManager<ServiceBoxTestDB>.Instance.GetSingleRecordInCache(element => element.param1 == 1);
                }
                else
                {
                    List<ServiceBoxTestDB> records = DBManager<ServiceBoxTestDB>.Instance.GetRecordsInCache().GetRange(0, loop);
                }
            }
            else
            {
                if (strategy == DataStrategy.ORM)
                {
                    //ORM处理
                    NHibernateHelper.GetRecordsByCondition<ServiceBoxTestDB>(element => element.param1 == 1, loop);
                }
            }
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds;
        }

        private double UpdateTestData(DataStrategy strategy, int loop)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            if (strategy == DataStrategy.Cache)
            {
                //缓存处理
                if (loop == 1)
                {
                    ServiceBoxTestDB record = DBManager<ServiceBoxTestDB>.Instance.GetSingleRecordInCache(element => element.param1 == 1);
                    record.param2++;

                   DBManager<ServiceBoxTestDB>.Instance.UpdateRecordInCache(record, e => e.guid == record.guid);
                }
                else
                {
                    currentSyncRecordIndex = 0;
                    List<ServiceBoxTestDB> records = DBManager<ServiceBoxTestDB>.Instance.GetRecordsInCache().GetRange(0, loop);                    
                    for (int index = 0; index < loop; index++)
                    {
                        records[index].param2++;

                       DBManager<ServiceBoxTestDB>.Instance.UpdateRecordInCache(records[currentSyncRecordIndex++], e => e.guid == records[currentSyncRecordIndex++].guid);
                    }
                }
            }
            else
            {
                if (strategy == DataStrategy.ORM)
                {
                    //ORM处理
                    List<ServiceBoxTestDB> records = NHibernateHelper.GetRecordsByCondition<ServiceBoxTestDB>(element => element.param1 == 1, loop);
                    for (int index = 0; index < records.Count; index++)
                    {
                        records[index].param2++;
                        NHibernateHelper.InsertOrUpdateOrDelete<ServiceBoxTestDB>(records[index], DataOperate.Update);
                    }
                }
            }
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds;
        }
        private double DeleteTestData(DataStrategy strategy, int loop)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            if (strategy == DataStrategy.Cache)
            {
                //缓存处理
                DBManager<ServiceBoxTestDB>.Instance.DeleteRecordInCache(element => element.param1 == 1, loop);
            }
            else
            {
                if (strategy == DataStrategy.ORM)
                {
                    //ORM处理
                    List<ServiceBoxTestDB> records = NHibernateHelper.GetRecordsByCondition<ServiceBoxTestDB>(element => element.param1 == 1, loop);
                    for (int index = 0; index < records.Count; index++)
                    {
                        NHibernateHelper.InsertOrUpdateOrDelete<ServiceBoxTestDB>(records[index], DataOperate.Delete);
                    }
                }
            }
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds;
        }

        public void OnNotifyShowRunningDBCache(int peerId, bool inbound, object msg)
        {
            NotifyShowRunningDBCache_C2X notifyMsg = msg as NotifyShowRunningDBCache_C2X;

            NHibernateHelper.RunningCacheSender.show = notifyMsg.show;
            NHibernateHelper.RunningCacheSender.acServerId = notifyMsg.senderACServerId;
            NHibernateHelper.RunningCacheSender.boxPeerId = notifyMsg.senderBoxPeerId;
        }
    }
}

