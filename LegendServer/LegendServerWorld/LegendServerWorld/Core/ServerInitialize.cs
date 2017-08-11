using LegendProtocol;
using LegendServerWorld.Distributed;
using LegendServerWorld.Lobby;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.Profiler;
using LegendServer.Database.ServerDeploy;
using LegendServerWorld.Record;
using LegendServer.Util;
using LegendServerWorldDefine;
using LegendServerWorld.SpecialActivities;
using LegendServer.Database.House;
using LegendServerWorld.UIDAlloc;

namespace LegendServerWorld.Core
{
    class ServerInitialize
    {
        public static void InitObjectPool()
        {
            ObjectPoolManager<Summoner>.Instance.Init(100000);
        }
        public static void RegistAllModule(object root)
        {
            ModuleManager.Regist(new RecordMain(root));
            ModuleManager.Regist(new DistributedMain(root));
            ModuleManager.Regist(new LobbyMain(root));
            ModuleManager.Regist(new SpecialActivitiesMain(root));
            ModuleManager.Regist(new UIDAllocMain(root));
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
                if ((result = DBManager<ServerConfigDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<SpecialActivitiesConfigDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<ActivitiesSystemConfigDB>.Instance.LoadDataToCache()) != null) break;

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
#if MAHJONG
                if ((result = DBManager<MahjongHouseDB>.Instance.LoadDataToCache()) != null) break;
#elif RUNFAST
                if ((result = DBManager<RunFastHouseDB>.Instance.LoadDataToCache()) != null) break;
#elif WORDPLATE
                if ((result = DBManager<WordPlateHouseDB>.Instance.LoadDataToCache()) != null) break;
#endif

                break;
            }
            return result;
        }
    }
}
