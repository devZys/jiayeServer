using LegendProtocol;
using LegendServerCenter.Distributed;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.Profiler;
using LegendServer.Database.ServerDeploy;
using LegendServerCenter.ServiceBox;
using LegendServerCenter.Record;
using LegendServer.Util;
using LegendServerCenter.UIDAlloc;

namespace LegendServerCenter.Core
{
    class ServerInitialize
    {
        public static void InitObjectPool()
        {
        }
        public static void RegistAllModule(object root)
        {
            ModuleManager.Regist(new RecordMain(root));
            ModuleManager.Regist(new DistributedMain(root));
            ModuleManager.Regist(new ServiceBoxMain(root));
            ModuleManager.Regist(new UIDAllocMain(root));
        }
        public static string LoadDatabaseToCache(string address, string port, string database, string user, string password)
        {
            string result = null;
            while(true)
            {
                NHibernateHelper.InitSessionFactory(address, port, database, user, password, out result);
                if (result != null) break;

                NHibernateHelper.SQLInsertOrUpdateOrDelete("delete from profiler");

                if ((result = LoadDBConfig()) != null) break;
                if ((result = LoadDBData()) != null) break;

                ModuleManager.Get<UIDAllocMain>().DisSummonerId();

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
