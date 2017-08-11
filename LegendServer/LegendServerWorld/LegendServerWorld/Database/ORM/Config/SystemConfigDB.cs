using FluentNHibernate.Mapping;

namespace LegendServer.Database.Config
{
    //系统配置表
    class SystemConfigDB
    {
        public virtual string key { get; set; }
        public virtual string value { get; set; }
        public virtual string remark { get; set; }

    }
    class SystemConfigDBMapping : ClassMap<SystemConfigDB>
    {
        public SystemConfigDBMapping()
        {
            Id(x => x.key).Column("key");
            Map(x => x.value).Column("value");
            Map(x => x.remark).Column("remark");

            Table("systemconfig");
        }
    }
}
