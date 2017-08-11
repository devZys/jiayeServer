using FluentNHibernate.Mapping;

namespace LegendServer.Database.Config
{
    //活动专场系统配置表
    class ActivitiesSystemConfigDB
    {
        public virtual string key { get; set; }
        public virtual string value { get; set; }
        public virtual string remark { get; set; }

    }
    class ActivitiesSystemConfigDBMapping : ClassMap<ActivitiesSystemConfigDB>
    {
        public ActivitiesSystemConfigDBMapping()
        {
            Id(x => x.key).Column("key");
            Map(x => x.value).Column("value");
            Map(x => x.remark).Column("remark");

            Table("activitiessystemconfig");
        }
    }
}
