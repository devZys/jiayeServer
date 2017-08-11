#if MAHJONG
using System;
using LegendProtocol;
using FluentNHibernate.Mapping;

namespace LegendServer.Database.Mahjong
{
    //房间信息
    public class MahjongHouseDB
    {
        public virtual ulong houseId { get; set; }
        public virtual int houseCardId { get; set; }
        public virtual int logicId { get; set; }
        public virtual int currentBureau { get; set; }
        public virtual int maxBureau { get; set; }
        public virtual int maxPlayerNum { get; set; }
        public virtual int curPlayerNum { get; set; }
        public virtual int businessId { get; set; }
        public virtual int housePropertyType { get; set; }
        public virtual int catchBird { get; set; }
        public virtual int flutter { get; set; }
        public virtual HouseType houseType { get; set; }
        public virtual MahjongType mahjongType { get; set; }
        public virtual MahjongHouseStatus houseStatus { get; set; }
        public virtual DateTime createTime { get; set; }
        public virtual DateTime endTime { get; set; }
    }
    class MahjongHouseDBMapping : ClassMap<MahjongHouseDB>
    {
        public MahjongHouseDBMapping()
        {
            Id(x => x.houseId).Column("houseId").GeneratedBy.Assigned();
            Map(x => x.houseCardId).Column("houseCardId");
            Map(x => x.logicId).Column("logicId");
            Map(x => x.currentBureau).Column("currentBureau");
            Map(x => x.maxBureau).Column("maxBureau");
            Map(x => x.maxPlayerNum).Column("maxPlayerNum");
            Map(x => x.curPlayerNum).Column("curPlayerNum");
            Map(x => x.businessId).Column("businessId");
            Map(x => x.housePropertyType).Column("housePropertyType");
            Map(x => x.catchBird).Column("catchBird");
            Map(x => x.flutter).Column("flutter");
            Map(x => x.houseType).Column("houseType").CustomType<HouseType>();
            Map(x => x.mahjongType).Column("mahjongType").CustomType<MahjongType>();
            Map(x => x.houseStatus).Column("houseStatus").CustomType<MahjongHouseStatus>();
            Map(x => x.createTime).Column("createTime");
            Map(x => x.endTime).Column("endTime");
            Table("mahjonghouse");
        }
    }
    //房间玩家信息
    public class MahjongPlayerDB
    {
        public virtual ulong houseId { get; set; }
        public virtual ulong summonerId { get; set; }
        public virtual int playerIndex { get; set; }
        public virtual ZhuangLeisureType zhuangLeisureType { get; set; }
        public virtual bool bGetRecord { get; set; }
        public virtual int smallWinFangBlast { get; set; }
        public virtual int smallWinJieBlast { get; set; }
        public virtual int smallWinMyself { get; set; }
        public virtual int bigWinFangBlast { get; set; }
        public virtual int bigWinJieBlast { get; set; }
        public virtual int bigWinMyself { get; set; }
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
            MahjongPlayerDB playerDB = obj as MahjongPlayerDB;
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
    class MahjongPlayerDBMapping : ClassMap<MahjongPlayerDB>
    {
        public MahjongPlayerDBMapping()
        {
            CompositeId()
            .KeyProperty(x => x.houseId, "houseId")
            .KeyProperty(x => x.summonerId, "summonerId");
            Map(x => x.playerIndex).Column("playerIndex");
            Map(x => x.zhuangLeisureType).Column("zhuangLeisureType").CustomType<ZhuangLeisureType>();
            Map(x => x.allIntegral).Column("allIntegral");
            Map(x => x.bGetRecord).Column("bGetRecord");
            Map(x => x.smallWinFangBlast).Column("smallWinFangBlast");
            Map(x => x.smallWinJieBlast).Column("smallWinJieBlast");
            Map(x => x.smallWinMyself).Column("smallWinMyself");
            Map(x => x.bigWinFangBlast).Column("bigWinFangBlast");
            Map(x => x.bigWinJieBlast).Column("bigWinJieBlast");
            Map(x => x.bigWinMyself).Column("bigWinMyself");
            Table("mahjongplayer");
        }
    }
    //房间当局信息
    public class MahjongBureauDB
    {
        public virtual ulong houseId { get; set; }
        public virtual ulong bureau { get; set; }
        public virtual byte[] playerinfo { get; set; }
        public virtual byte[] playermahjong { get; set; }
        public virtual byte[] showmahjong { get; set; }
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
            MahjongBureauDB bureauDB = obj as MahjongBureauDB;
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
    class MahjongBureauDBMapping : ClassMap<MahjongBureauDB>
    {
        public MahjongBureauDBMapping()
        {
            CompositeId()
            .KeyProperty(x => x.houseId, "houseId")
            .KeyProperty(x => x.bureau, "bureau");
            Map(x => x.playerinfo).Column("playerinfo");
            Map(x => x.playermahjong).Column("playermahjong");
            Map(x => x.showmahjong).Column("showmahjong");
            Map(x => x.bureauTime).Column("bureauTime");
            Table("mahjongbureau");
        }
    }
}
#endif
