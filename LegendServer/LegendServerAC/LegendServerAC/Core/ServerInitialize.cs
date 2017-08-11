using LegendProtocol;
using LegendServerAC.Distributed;
using LegendServerAC.Login;
using LegendServerAC.SafeCheck;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.Profiler;
using LegendServer.Database.ServerDeploy;
using LegendServerAC.Record;
using LegendServer.Util;
using LegendServerAC.Authority;
using LegendServerAC.ServiceBox;

namespace LegendServerAC.Core
{
    class ServerInitialize
    {
        public static void InitObjectPool()
        {
            ObjectPoolManager<InboundClientSession>.Instance.Init(100000);
        }
        public static void RegistPublicModule(object root)
        {
            ModuleManager.Regist(new DistributedMain(root));
            ModuleManager.Regist(new RecordMain(root));
            ModuleManager.Regist(new LoginMain(root));
            ModuleManager.Regist(new SafeCheckMain(root));
            ModuleManager.Regist(new AuthorityMain(root));
            ModuleManager.Regist(new ServiceBoxMain(root));
        }
        public static void RegistGameModule(object root)
        {
#if MAHJONG
#elif RUNFAST
#elif WORDPLATE
#endif
        }
        public static void RegistAllModule(object root)
        {
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

                break;
            }
            return result;
        }       
    }
}
