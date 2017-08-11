using LegendProtocol;
using LegendServerProxy.Distributed;
using LegendServerProxy.SafeCheck;
using LegendServerProxy.Login;
using LegendServerProxy.MainCity;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.Profiler;
using LegendServer.Database.ServerDeploy;
using LegendServerProxy.ServiceBox;
using LegendServerProxy.Record;
using LegendServer.Util;
using LegendServerProxy.SpecialActivities;
#if MAHJONG
using LegendServerProxy.Mahjong;
#elif RUNFAST
using LegendServerProxy.RunFast;
#elif WORDPLATE
using LegendServerProxy.WordPlate;
#endif

namespace LegendServerProxy.Core
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
            ModuleManager.Regist(new ServiceBoxMain(root));
            ModuleManager.Regist(new SafeCheckMain(root));
            ModuleManager.Regist(new LoginMain(root));
            ModuleManager.Regist(new MainCityMain(root));
            ModuleManager.Regist(new SpecialActivitiesMain(root));
        }
        public static void RegistGameModule(object root)
        {
#if MAHJONG
            ModuleManager.Regist(new MahjongMain(root));
#elif RUNFAST
            ModuleManager.Regist(new RunFastMain(root));
#elif WORDPLATE
            ModuleManager.Regist(new WordPlateMain(root));
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
                if ((result = DBManager<ServerConfigDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<ActivitiesSystemConfigDB>.Instance.LoadDataToCache()) != null) break;
#if MAHJONG
                    if ((result = DBManager<MahjongConfigDB>.Instance.LoadDataToCache()) != null) break;
#elif RUNFAST
                    if ((result = DBManager<RunFastConfigDB>.Instance.LoadDataToCache()) != null) break;
#elif WORDPLATE
                    if ((result = DBManager<WordPlateConfigDB>.Instance.LoadDataToCache()) != null) break;
#endif

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
