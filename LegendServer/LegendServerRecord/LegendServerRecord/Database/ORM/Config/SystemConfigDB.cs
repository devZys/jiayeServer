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
    //兑换码
    public class RedeemCodeConfigDB
    {
        public virtual string redeemCode { get; set; }
        public virtual int batch { get; set; }
        public virtual int type { get; set; }
        public virtual string channel { get; set; }
        public virtual string currencies { get; set; }
        public virtual string items { get; set; }
    }
    class RedeemCodeConfigDBMapping : ClassMap<RedeemCodeConfigDB>
    {
        public RedeemCodeConfigDBMapping()
        {
            Id(x => x.redeemCode).Column("redeemCode");
            Map(x => x.batch).Column("batch");
            Map(x => x.type).Column("type");
            Map(x => x.channel).Column("channel");
            Map(x => x.currencies).Column("currencies");
            Map(x => x.items).Column("items");

            Table("redeemcodeconfig");
        }
    }
}
