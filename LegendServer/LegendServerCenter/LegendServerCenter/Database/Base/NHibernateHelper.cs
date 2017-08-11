using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using LegendProtocol;
using System.Diagnostics;

namespace LegendServer.Database
{
    //ORM辅助类，在采用数据缓存机制下不要用后面的接口来修改数据库，以免缓存与数据库数据不一致!
    public class NHibernateHelper
    {
        private static ISessionFactory sessionFactory = null;
        private static ISession normalSession;
        private static ISession specialSession;
        private static ISession directSession;
        private static object factoryLock = new object();
        private static object operateLock = new object();
        private static object normalLock = new object();
        private static object specialLock = new object();
        private static object directLock = new object();
        private NHibernateHelper() { }

        //初始化会话工厂
        public static ISessionFactory InitSessionFactory(string address, string port, string database, string user, string password, out string result)
        {
            if (sessionFactory == null)
            {
                lock (factoryLock)
                {
                    if (sessionFactory == null)
                    {
                        try
                        {
                            string strConnect = "Server=" + address + ";Port=" + port + ";Database=" + database + ";Uid=" + user + ";Pwd=" + password;
                            sessionFactory = Fluently.Configure().Database(MySQLConfiguration.Standard.ConnectionString(strConnect)).
                            Mappings(x => x.FluentMappings.AddFromAssemblyOf<NHibernateHelper>()).BuildSessionFactory();
                        }
                        catch (Exception ex)
                        {
                            result = "ExceptionMessage:" + ex.Message + (ex.InnerException != null ? ("InnerExceptionMessage:" + ex.InnerException.Message) : "");
                            return null;
                        }
                    }
                }
            }
            OpenNormalSession();
            OpenSpecialSession();
            OpenDirectSession();
            result = null;
            return sessionFactory;
        }
        //打开普通会话
        public static ISession OpenNormalSession()
        {
            if (normalSession == null)
            {
                lock (normalLock)
                {
                    if (normalSession == null)
                    {
                        normalSession = sessionFactory.OpenSession();
                    }
                }
            }
           
            if (!normalSession.IsConnected || !normalSession.IsOpen)
            {
                lock (normalLock)
                {
                    if (!normalSession.IsConnected || !normalSession.IsOpen)
                    {
                        normalSession = sessionFactory.OpenSession();
                    }
                }
            }
            return normalSession;
        }
        //打开特殊会话(主要用于缓存后台线程入库)
        public static ISession OpenSpecialSession()
        {
            if (specialSession == null)
            {
                lock (specialLock)
                {
                    if (specialSession == null)
                    {
                        specialSession = sessionFactory.OpenSession();
                    }
                }
            }
            if (!specialSession.IsConnected || !specialSession.IsOpen)
            {
                lock (specialLock)
                {
                    if (!specialSession.IsConnected || !specialSession.IsOpen)
                    {
                        specialSession = sessionFactory.OpenSession();
                    }
                }
            }
            return specialSession;
        }
        //打开直接操作的会话
        public static ISession OpenDirectSession()
        {
            if (directSession == null)
            {
                lock (directLock)
                {
                    if (directSession == null)
                    {
                        directSession = sessionFactory.OpenSession();
                    }
                }
            }

            if (!directSession.IsConnected || !directSession.IsOpen)
            {
                lock (directLock)
                {
                    if (!directSession.IsConnected || !directSession.IsOpen)
                    {
                        directSession = sessionFactory.OpenSession();
                    }
                }
            }
            return directSession;
        }
        //获取所有记录
        public static IList<T> GetRecords<T>() where T : class
        {
            try
            {
                using (var session = OpenNormalSession())
                {
                    try
                    {
                        var recordList = session.QueryOver<T>();
                        session.Flush();

                        return recordList.List();
                    }
                    catch (Exception ex)
                    {
                        ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return null;
            }
        }
        //条件查询记录
        public static List<T> GetRecordsByCondition<T>(Expression<Func<T, bool>> condition, int count = int.MaxValue) where T : class
        {
            try
            {
                using (var session = OpenNormalSession())
                {
                    try
                    {
                        var playerList = session.QueryOver<T>().Where(condition);
                        session.Flush();

                        return playerList.List().Take(count).ToList();
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return null;
            }
        }
        //直接条件查询记录
        public static List<T> DirectGetRecordsByCondition<T>(Expression<Func<T, bool>> condition, int count = int.MaxValue) where T : class
        {
            try
            {
                using (var session = OpenDirectSession())
                {
                    try
                    {
                        var playerList = session.QueryOver<T>().Where(condition);
                        session.Flush();

                        return playerList.List().Take(count).ToList();
                    }
                    catch (Exception ex)
                    {
                        ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return null;
            }
        }
        //直接条件查询记录
        public static T DirectGetSingleRecordByCondition<T>(Expression<Func<T, bool>> condition) where T : class
        {
            try
            {
                using (var session = OpenDirectSession())
                {
                    try
                    {
                        var record = session.QueryOver<T>().Where(condition).SingleOrDefault();
                        session.Flush();

                        return record;
                    }
                    catch (Exception ex)
                    {
                        ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return null;
            }
        }
        //增加或者删除或者更新记录
        public static bool InsertOrUpdateOrDelete<T>(T record, DataOperate operate, bool direct = false)
        {
            try
            {
                lock (operateLock)
                {
                    using (var session = direct ? OpenDirectSession() : (typeof(T).ToString().Equals("LegendServer.Database.Profiler.ProfilerDB") ? OpenSpecialSession() : OpenNormalSession()))
                    {
                        try
                        {
                            switch (operate)
                            {
                                case DataOperate.Insert:
                                    session.Save(record);
                                    break;
                                case DataOperate.Update:
                                    session.Update(record);
                                    break;
                                case DataOperate.Delete:
                                    session.Delete(record);
                                    break;
                                default:
                                    return false;
                            }

                            session.Flush();

                            return true;
                        }
                        catch (Exception ex)
                        {
                            ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return false;
            }
        }

        //Sql select语句
        public static IList SQLSelect(string sql)
        {
            try
            {
                using (var session = OpenDirectSession())
                {
                    try
                    {
                        var list = session.CreateSQLQuery(sql);
                        session.Flush();

                        return list.List();
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return null;
            }
        }
        //Sql select语句
        public static object SQL(string sql)
        {
            try
            {
                using (var session = OpenDirectSession())
                {
                    try
                    {
                        object result = session.CreateSQLQuery(sql).UniqueResult();
                        session.Flush();

                        return result;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return null;
            }
        }
        //Sql insert、Update、Delete语句
        public static int SQLInsertOrUpdateOrDelete(string sql)
        {
            try
            {
                using (var session = OpenDirectSession())
                {
                    try
                    {
                        int affectCount = session.CreateSQLQuery(sql).ExecuteUpdate();
                        session.Flush();

                        return affectCount;
                    }
                    catch (Exception)
                    {
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
                return 0;
            }
        }
    }
}
