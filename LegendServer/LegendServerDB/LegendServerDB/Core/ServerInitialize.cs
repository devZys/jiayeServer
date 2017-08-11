using LegendProtocol;
using LegendServerDB.Distributed;
using LegendServerDB.Login;
using LegendServerDB.MainCity;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.Profiler;
using LegendServer.Database.Summoner;
using LegendServer.Database.ServerDeploy;
using LegendServerDB.Update;
using LegendServerDB.Record;
using LegendServerDB.ServiceBox;
using LegendServer.Database.ServiceBox;
using LegendServerDB.Authority;
using System.Collections.Generic;
#if RUNFAST
using LegendServerDB.RunFast;
using LegendServer.Database.RunFast;
#elif MAHJONG
using LegendServerDB.Mahjong;
using LegendServer.Database.Mahjong;
#elif WORDPLATE
using LegendServerDB.WordPlate;
using LegendServer.Database.WordPlate;
#endif

namespace LegendServerDB.Core
{
    class ServerInitialize
    {
        public static void InitObjectPool()
        {
        }
        public static void RegistSerialize()
        {
            ServerUtil.RegistSerializer(typeof(SummonerDB));
            ServerUtil.RegistSerializer(typeof(List<string>));
        }
        public static void RegistPublicModule(object root)
        {
            ModuleManager.Regist(new ServiceBoxMain(root));
            ModuleManager.Regist(new RecordMain(root));
            ModuleManager.Regist(new DistributedMain(root));
            ModuleManager.Regist(new LoginMain(root));
            ModuleManager.Regist(new MainCityMain(root));
            ModuleManager.Regist(new UpdateMain(root));
            ModuleManager.Regist(new AuthorityMain(root));
        }
        public static void RegistGameModule(object root)
        {
#if RUNFAST
            ModuleManager.Regist(new RunFastMain(root));
#elif MAHJONG
            ModuleManager.Regist(new MahjongMain(root));
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
                if ((result = DBManager<TicketsConfigDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<SpecialActivitiesConfigDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<WebConfigDB>.Instance.LoadDataToCache()) != null) break;

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
                if ((result = DBManager<ServiceBoxTestDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<DBCacheSyncTestDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<ProfilerDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<SummonerDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<SummonerBlackListDB>.Instance.LoadDataToCache()) != null) break;

#if RUNFAST
                if ((result = DBManager<RunFastHouseDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<RunFastPlayerDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<RunFastBureauDB>.Instance.LoadDataToCache()) != null) break;
#elif MAHJONG
                if ((result = DBManager<MahjongHouseDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<MahjongPlayerDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<MahjongBureauDB>.Instance.LoadDataToCache()) != null) break;
#elif WORDPLATE
                if ((result = DBManager<WordPlateHouseDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<WordPlatePlayerDB>.Instance.LoadDataToCache()) != null) break;
                if ((result = DBManager<WordPlateBureauDB>.Instance.LoadDataToCache()) != null) break;
#endif

                break;
            }
            return result;
        }
    }
}