#if RUNFAST
using System;
using LegendProtocol;
using FluentNHibernate.Mapping;

namespace LegendServer.Database.RunFast
{
    //房间信息
    public class RunFastHouseDB
    {
        public virtual ulong houseId { get; set; }
        public virtual int houseCardId { get; set; }
        public virtual int logicId { get; set; }
        public virtual int currentBureau { get; set; }
        public virtual int maxBureau { get; set; }
        public virtual int curPlayerNum { get; set; }
        public virtual int maxPlayerNum { get; set; }
        public virtual int businessId { get; set; }
        public virtual int housePropertyType { get; set; }
        public virtual int zhuangPlayerIndex { get; set; }
        public virtual HouseType houseType { get; set; }
        public virtual int runFastType { get; set; }
        public virtual RunFastHouseStatus houseStatus { get; set; }
        public virtual DateTime createTime { get; set; }
        public virtual DateTime endTime { get; set; }
    }
    class RunFastHouseDBMapping : ClassMap<RunFastHouseDB>
    {
        public RunFastHouseDBMapping()
        {
            Id(x => x.houseId).Column("houseId").GeneratedBy.Assigned();
            Map(x => x.houseCardId).Column("houseCardId");
            Map(x => x.logicId).Column("logicId");
            Map(x => x.currentBureau).Column("currentBureau");
            Map(x => x.maxBureau).Column("maxBureau");
            Map(x => x.curPlayerNum).Column("curPlayerNum");
            Map(x => x.maxPlayerNum).Column("maxPlayerNum");
            Map(x => x.businessId).Column("businessId");
            Map(x => x.housePropertyType).Column("housePropertyType");
            Map(x => x.zhuangPlayerIndex).Column("zhuangPlayerIndex");
            Map(x => x.houseType).Column("houseType").CustomType<HouseType>();
            Map(x => x.runFastType).Column("runFastType");
            Map(x => x.houseStatus).Column("houseStatus").CustomType<RunFastHouseStatus>();
            Map(x => x.createTime).Column("createTime");
            Map(x => x.endTime).Column("endTime");
            Table("runfasthouse");
        }
    }
    //房间玩家信息
    public class RunFastPlayerDB
    {
        public virtual ulong houseId { get; set; }
        public virtual ulong summonerId { get; set; }
        public virtual int playerIndex { get; set; }
        public virtual bool bGetRecord { get; set; }
        public virtual int bombIntegral { get; set; }
        public virtual int winBureau { get; set; }
        public virtual int loseBureau { get; set; }
        public virtual int allIntegral { get; set; }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            RunFastPlayerDB playerDB = obj as RunFastPlayerDB;
            if (!this.houseId.Equals(playerDB.houseId) || !this.summonerId.Equals(playerDB.summonerId))
            {
                return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            return this.houseId.GetHashCode() + this.summonerId.GetHashCode();
        }
    }
    class RunFastPlayerDBMapping : ClassMap<RunFastPlayerDB>
    {
        public RunFastPlayerDBMapping()
        {
            CompositeId()
            .KeyProperty(x => x.houseId, "houseId")
            .KeyProperty(x => x.summonerId, "summonerId");
            Map(x => x.playerIndex).Column("playerIndex");
            Map(x => x.bGetRecord).Column("bGetRecord");
            Map(x => x.bombIntegral).Column("bombIntegral");
            Map(x => x.winBureau).Column("winBureau");
            Map(x => x.loseBureau).Column("loseBureau");
            Map(x => x.allIntegral).Column("allIntegral");
            Table("runfastplayer");
        }
    }
    //房间当局信息
    public class RunFastBureauDB
    {
        public virtual ulong houseId { get; set; }
        public virtual ulong bureau { get; set; }
        public virtual byte[] playerinfo { get; set; }
        public virtual byte[] playercard { get; set; }
        public virtual byte[] showcard { get; set; }
        public virtual DateTime bureauTime { get; set; }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            RunFastBureauDB bureauDB = obj as RunFastBureauDB;
            if (!this.houseId.Equals(bureauDB.houseId) || !this.bureau.Equals(bureauDB.bureau))
            {
                return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            return this.houseId.GetHashCode() + this.bureau.GetHashCode();
        }
    }
    class RunFastBureauDBMapping : ClassMap<RunFastBureauDB>
    {
        public RunFastBureauDBMapping()
        {
            CompositeId()
            .KeyProperty(x => x.houseId, "houseId")
            .KeyProperty(x => x.bureau, "bureau");
            Map(x => x.playerinfo).Column("playerinfo");
            Map(x => x.playercard).Column("playercard");
            Map(x => x.showcard).Column("showcard");
            Map(x => x.bureauTime).Column("bureauTime");
            Table("runfastbureau");
        }
    }
}
#endif