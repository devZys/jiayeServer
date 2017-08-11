using LegendProtocol;
using LegendServerRecord.Distributed;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.ServerDeploy;
using LegendServer.Database.Log;
using LegendServer.Database.Profiler;
using LegendServerRecord.Record;
using LegendServer.Util;
using LegendServer.Database.Record;
using LegendServerRecord.ServiceBox;

namespace LegendServerRecord.Core
{
    class ServerInitialize
    {
        public static void InitObjectPool()
        {
        }
        public static void RegistPublicModule(object root)
        {
            ModuleManager.Regist(new DistributedMain(root));
            ModuleManager.Regist(new RecordMain(root));
            ModuleManager.Regist(new ServiceBoxMain(root));
        }
        public static void RegistGameModule(object root)
        {
#if RUNFAST
#elif MAHJONG
#elif WORDPLATE
#endif
        }
        public static string LoadDatabaseToCache(string address, string port, string database, string user, string password)
        {
            string result = null;
            while (true)
            {
                NHibernateHelper.InitSessionFactory(address, port, database, user, password, out result);
                if (result != null) break;

                NHibernateHelper.SQLInsertOrUpdateOrDelete("delete from profiler");

                if ((result = LoadDBConfig()) != null) break;
                if ((result = LoadDBData()) != null) break;

                break;
            }
            return result;
        }
        public static string LoadDBConfig()
        {
            string result = null;
            while (true)
            {
                if ((result = DBManager<SystemConfigDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<ServerDeployConfigDB>.Instance.LoadDataToCache()) != null) break;

                break;
            }
            if (result == null)
            {
                ModuleManager.LoadDBConfig();
            }
            return result;
        }

        public static string LoadDBData()
        {
            string result = null;
            while (true)
            {
                if ((result = DBManager<ProfilerDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<LogDebugDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<LogErrorDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<LogFatalDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<LogInfoDB>.Instance.LoadDataToCache()) != null) break; 

                break;
            }
            return result;
        }
    }
}
