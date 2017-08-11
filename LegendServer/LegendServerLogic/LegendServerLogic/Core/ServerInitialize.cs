using LegendProtocol;
using LegendServerLogic.MainCity;
using LegendServerLogic.Distributed;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.Profiler;
using LegendServer.Database.ServerDeploy;
using LegendServerLogic.Record;
using LegendServer.Util;
using LegendServerLogic.Actor.Summoner;
using LegendServerLogic.ServiceBox;
using LegendServerLogic.UIDAlloc;
using System.Collections.Generic;
using LegendServerLogic.SpecialActivities;
#if MAHJONG
using LegendServerLogic.Mahjong;
#elif RUNFAST
using LegendServerLogic.RunFast;
#elif WORDPLATE
using LegendServerLogic.WordPlate;
#endif

namespace LegendServerLogic.Core
{
    class ServerInitialize
    {
        public static void InitObjectPool()
        {
            ObjectPoolManager<Summoner>.Instance.Init(100000);
        }
        public static void RegistSerialize()
        {
            ServerUtil.RegistSerializer(typeof(Summoner));
        }
        public static void RegistPublicModule(object root)
        {
            ModuleManager.Regist(new ServiceBoxMain(root));
            ModuleManager.Regist(new RecordMain(root));
            ModuleManager.Regist(new DistributedMain(root));
            ModuleManager.Regist(new MainCityMain(root));
            ModuleManager.Regist(new SpecialActivitiesMain(root));
            ModuleManager.Regist(new UIDAllocMain(root));
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
                if ((result = DBManager<SpecialActivitiesConfigDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<TicketsConfigDB>.Instance.LoadDataToCache()) != null) break;
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
