using System;
using LegendProtocol;
using FluentNHibernate.Mapping;

namespace LegendServer.Database.House
{
    //房间信息
#if MAHJONG
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
            Map(x => x.houseType).Column("houseType").CustomType<HouseType>();
            Map(x => x.mahjongType).Column("mahjongType").CustomType<MahjongType>();
            Map(x => x.houseStatus).Column("houseStatus").CustomType<MahjongHouseStatus>();
            Map(x => x.createTime).Column("createTime");
            Map(x => x.endTime).Column("endTime");
            Table("mahjonghouse");
        }
    }
#elif RUNFAST
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
#elif WORDPLATE
    public class WordPlateHouseDB
    {
        public virtual ulong houseId { get; set; }
        public virtual int houseCardId { get; set; }
        public virtual int logicId { get; set; }
        public virtual int currentBureau { get; set; }
        public virtual int maxBureau { get; set; }
        public virtual int maxWinScore { get; set; }
        public virtual int curPlayerNum { get; set; }
        public virtual int maxPlayerNum { get; set; }
        public virtual int businessId { get; set; }
        public virtual int housePropertyType { get; set; }
        public virtual int baseWinScore { get; set; }
        public virtual int beginGodType { get; set; }
        public virtual HouseType houseType { get; set; }
        public virtual WordPlateType wordPlateType { get; set; }
        public virtual WordPlateHouseStatus houseStatus { get; set; }
        public virtual DateTime createTime { get; set; }
        public virtual DateTime endTime { get; set; }
    }
    class WordPlateHouseDBMapping : ClassMap<WordPlateHouseDB>
    {
        public WordPlateHouseDBMapping()
        {
            Id(x => x.houseId).Column("houseId").GeneratedBy.Assigned();
            Map(x => x.houseCardId).Column("houseCardId");
            Map(x => x.logicId).Column("logicId");
            Map(x => x.currentBureau).Column("currentBureau");
            Map(x => x.maxBureau).Column("maxBureau");
            Map(x => x.maxWinScore).Column("maxWinScore");
            Map(x => x.curPlayerNum).Column("curPlayerNum");
            Map(x => x.maxPlayerNum).Column("maxPlayerNum");
            Map(x => x.businessId).Column("businessId");
            Map(x => x.housePropertyType).Column("housePropertyType");
            Map(x => x.baseWinScore).Column("baseWinScore");
            Map(x => x.beginGodType).Column("beginGodType");
            Map(x => x.houseType).Column("houseType").CustomType<HouseType>();
            Map(x => x.wordPlateType).Column("wordPlateType").CustomType<WordPlateType>();
            Map(x => x.houseStatus).Column("houseStatus").CustomType<WordPlateHouseStatus>();
            Map(x => x.createTime).Column("createTime");
            Map(x => x.endTime).Column("endTime");
            Table("wordplatehouse");
        }
    }
#endif
}
