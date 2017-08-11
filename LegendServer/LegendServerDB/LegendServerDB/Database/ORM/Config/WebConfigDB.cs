using FluentNHibernate.Mapping;

namespace LegendServer.Database.Config
{
    //系统配置表
    public class WebConfigDB
    {
        public virtual string configKey { get; set; }
        public virtual string configValue { get; set; }
        public virtual string configRemark { get; set; }

    }
    public class WebConfigDBMapping : ClassMap<WebConfigDB>
    {
        public WebConfigDBMapping()
        {
            Id(x => x.configKey).Column("configKey");
            Map(x => x.configValue).Column("configValue");
            Map(x => x.configRemark).Column("configRemark");

            Table("webconfig");
        }
    }
}
