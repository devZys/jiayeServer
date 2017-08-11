using FluentNHibernate.Mapping;
using System;

namespace LegendServer.Database.Config
{
    /**专场ID,专场名称,游戏规则,简介,序号,标题LogoURL,主LogoURL,主图URL,宣传图URL,券列表,专场折扣描述,营业执照,联系地址,联系电话,联系人,动态口令生存期，版本号，比赛场券列表*/
    //专场配置表
    class SpecialActivitiesConfigDB
    {
        public virtual int ID { get; set; }
        public virtual string Name { get; set; }
        public virtual string RuleInfo { get; set; }
        public virtual string Intro { get; set; }
        public virtual int OrderId { get; set; }
        public virtual string TitleLogoURL { get; set; }
        public virtual string MainLogoURL { get; set; }
        public virtual string MainPicURL { get; set; }
        public virtual string BroadcastPicURL { get; set; }
        public virtual string AdminId { get; set; }
        public virtual string Tickets { get; set; }
        public virtual string DiscountInfo { get; set; }
        public virtual string BusinessLicense { get; set; }
        public virtual string ContactAddress { get; set; }
        public virtual string ContactCall { get; set; }
        public virtual string ContactPerson { get; set; }
        public virtual int DynamicPasswordIndate { get; set; }
        public virtual int UpdateFlag { get; set; }
        public virtual string ComTickets { get; set; }

    }
    class SpecialActivitiesConfigDBMapping : ClassMap<SpecialActivitiesConfigDB>
    {
        public SpecialActivitiesConfigDBMapping()
        {
            Id(x => x.ID).Column("ID").GeneratedBy.Assigned();
            Map(x => x.Name).Column("Name");
            Map(x => x.RuleInfo).Column("RuleInfo");
            Map(x => x.Intro).Column("Intro");
            Map(x => x.OrderId).Column("OrderId");
            Map(x => x.TitleLogoURL).Column("TitleLogoURL");
            Map(x => x.MainLogoURL).Column("MainLogoURL");            
            Map(x => x.MainPicURL).Column("MainPicURL");
            Map(x => x.BroadcastPicURL).Column("BroadcastPicURL");
            Map(x => x.AdminId).Column("AdminId");
            Map(x => x.Tickets).Column("Tickets");
            Map(x => x.DiscountInfo).Column("DiscountInfo");
            Map(x => x.BusinessLicense).Column("BusinessLicense");
            Map(x => x.ContactAddress).Column("ContactAddress");
            Map(x => x.ContactCall).Column("ContactCall");
            Map(x => x.ContactPerson).Column("ContactPerson");
            Map(x => x.DynamicPasswordIndate).Column("DynamicPasswordIndate");
            Map(x => x.UpdateFlag).Column("UpdateFlag");
            Map(x => x.ComTickets).Column("ComTickets");

            Table("specialactivitiesconfig");
        }
    }
}
