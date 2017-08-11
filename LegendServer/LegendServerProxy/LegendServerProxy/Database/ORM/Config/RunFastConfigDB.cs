using FluentNHibernate.Mapping;

namespace LegendServer.Database.Config
{
    //简单的服务器配置表
    class RunFastConfigDB
    {
        public virtual string key { get; set; }
        public virtual string value { get; set; }
        public virtual string remark { get; set; }

    }
    class RunFastConfigDBMapping : ClassMap<RunFastConfigDB>
    {
        public RunFastConfigDBMapping()
        {
            Id(x => x.key).Column("key");
            Map(x => x.value).Column("value");
            Map(x => x.remark).Column("remark");

            Table("runfastconfig");
        }
    }
}
