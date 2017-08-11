using FluentNHibernate.Mapping;

namespace LegendServer.Database.ServiceBox
{
    //后台盒子的测试数据库
    public class ServiceBoxTestDB
    {
        public virtual ulong guid { get; set; }
        public virtual int param1 { get; set; }
        public virtual int param2 { get; set; }
        public virtual int param3 { get; set; }
        public virtual float param4 { get; set; }
        public virtual float param5 { get; set; }
        public virtual float param6 { get; set; }
        public virtual string param7 { get; set; }
        public virtual string param8 { get; set; }
        public virtual string param9 { get; set; }
    }
    class ServiceBoxTestDBMapping : ClassMap<ServiceBoxTestDB>
    {
        public ServiceBoxTestDBMapping()
        {
            Id(x => x.guid).Column("guid").GeneratedBy.Assigned();
            Map(x => x.param1).Column("param1");
            Map(x => x.param2).Column("param2");
            Map(x => x.param3).Column("param3");
            Map(x => x.param4).Column("param4");
            Map(x => x.param5).Column("param5");
            Map(x => x.param6).Column("param6");
            Map(x => x.param7).Column("param7");
            Map(x => x.param8).Column("param8");
            Map(x => x.param9).Column("param9"); 
            Table("serviceboxtest");
        }
    }
}
