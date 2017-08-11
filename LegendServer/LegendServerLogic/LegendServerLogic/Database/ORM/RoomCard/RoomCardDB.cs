using FluentNHibernate.Mapping;

namespace LegendServer.Database.RoomCard
{
    //房卡
    public class RoomCardDB
    {
        public virtual ulong id { get; set; }
        public virtual string nickName { get; set; }
        public virtual int roomCard { get; set; }
    }
    class RoomCardDBMapping : ClassMap<RoomCardDB>
    {
        public RoomCardDBMapping()
        {
            Id(x => x.id).Column("id").GeneratedBy.Assigned();
            Map(x => x.nickName).Column("nickName");
            Map(x => x.roomCard).Column("roomCard");
            Table("roomcard");
        }
    }
}
