#if WORDPLATE
using System;
using LegendProtocol;
using FluentNHibernate.Mapping;

namespace LegendServer.Database.WordPlate
{
    //房间信息
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
    //房间玩家信息
    public class WordPlatePlayerDB
    {
        public virtual ulong houseId { get; set; }
        public virtual ulong summonerId { get; set; }
        public virtual int playerIndex { get; set; }
        public virtual ZhuangLeisureType zhuangLeisureType { get; set; }
        public virtual bool bGetRecord { get; set; }
        public virtual int winAmount { get; set; }
        public virtual int allWinScore { get; set; }
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
            WordPlatePlayerDB playerDB = obj as WordPlatePlayerDB;
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
    class WordPlatePlayerDBMapping : ClassMap<WordPlatePlayerDB>
    {
        public WordPlatePlayerDBMapping()
        {
            CompositeId()
            .KeyProperty(x => x.houseId, "houseId")
            .KeyProperty(x => x.summonerId, "summonerId");
            Map(x => x.playerIndex).Column("playerIndex");
            Map(x => x.zhuangLeisureType).Column("zhuangLeisureType").CustomType<ZhuangLeisureType>();
            Map(x => x.allIntegral).Column("allIntegral");
            Map(x => x.bGetRecord).Column("bGetRecord");
            Map(x => x.winAmount).Column("winAmount");
            Map(x => x.allWinScore).Column("allWinScore");
            Table("wordplateplayer");
        }
    }
    //房间当局信息
    public class WordPlateBureauDB
    {
        public virtual ulong houseId { get; set; }
        public virtual ulong bureau { get; set; }
        public virtual byte[] playerinfo { get; set; }
        public virtual byte[] playerWordPlate { get; set; }
        public virtual byte[] showWordPlate { get; set; }
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
            WordPlateBureauDB bureauDB = obj as WordPlateBureauDB;
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
    class WordPlateBureauDBMapping : ClassMap<WordPlateBureauDB>
    {
        public WordPlateBureauDBMapping()
        {
            CompositeId()
            .KeyProperty(x => x.houseId, "houseId")
            .KeyProperty(x => x.bureau, "bureau");
            Map(x => x.playerinfo).Column("playerinfo");
            Map(x => x.playerWordPlate).Column("playerWordPlate");
            Map(x => x.showWordPlate).Column("showWordPlate");
            Map(x => x.bureauTime).Column("bureauTime");
            Table("wordplatebureau");
        }
    }
}
#endif
