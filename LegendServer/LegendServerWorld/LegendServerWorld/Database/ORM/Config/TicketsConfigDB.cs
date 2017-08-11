using FluentNHibernate.Mapping;
using System;

namespace LegendServer.Database.Config
{
    /**券ID,名称,详细描述,生存期,IconURL,结算描述*/
    //专场配置表
    class TicketsConfigDB
    {
        public virtual int ID { get; set; }
        public virtual int MarketID { get; set; }
        public virtual string Name { get; set; }
        public virtual string Context { get; set; }
        public virtual int Indate { get; set; }
        public virtual string IconURL { get; set; }
        public virtual string SettlementInfo { get; set; }

    }
    class TicketsConfigDBMapping : ClassMap<TicketsConfigDB>
    {
        public TicketsConfigDBMapping()
        {
            Id(x => x.ID).Column("ID").GeneratedBy.Assigned();
            Map(x => x.MarketID).Column("MarketID");
            Map(x => x.Name).Column("Name");
            Map(x => x.Context).Column("Context");
            Map(x => x.Indate).Column("Indate");
            Map(x => x.IconURL).Column("IconURL");
            Map(x => x.SettlementInfo).Column("SettlementInfo");

            Table("ticketsconfig");
        }
    }
}
