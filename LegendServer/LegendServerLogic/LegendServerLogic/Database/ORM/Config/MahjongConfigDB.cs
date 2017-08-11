using FluentNHibernate.Mapping;

namespace LegendServer.Database.Config
{
    //简单的服务器配置表
    class MahjongConfigDB
    {
        public virtual string key { get; set; }
        public virtual string value { get; set; }
        public virtual string remark { get; set; }

    }
    class MahjongConfigDBMapping : ClassMap<MahjongConfigDB>
    {
        public MahjongConfigDBMapping()
        {
            Id(x => x.key).Column("key");
            Map(x => x.value).Column("value");
            Map(x => x.remark).Column("remark");

            Table("mahjongconfig");
        }
    }
}
