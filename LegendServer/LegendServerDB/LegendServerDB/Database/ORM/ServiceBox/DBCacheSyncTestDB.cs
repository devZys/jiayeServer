using FluentNHibernate.Mapping;

namespace LegendServer.Database.ServiceBox
{
    //数据缓存一致性测试
    public class DBCacheSyncTestDB
    {
        public virtual ulong guid { get; set; }
        public virtual byte[] data { get; set; }
    }
    class DBCacheSyncTestDBMapping : ClassMap<DBCacheSyncTestDB>
    {
        public DBCacheSyncTestDBMapping()
        {
            Id(x => x.guid).Column("guid").GeneratedBy.Assigned();
            Map(x => x.data).Column("data");
            Table("dbcachesynctest");
        }
    }
}
