using FluentNHibernate.Mapping;
using LegendProtocol;
using System;

namespace LegendServer.Database.Record
{
    //玩家房卡消耗统计
    public class RecordRoomCardDB
    {
        public virtual ulong recordID { get; set; }
        public virtual ulong summonerId { get; set; }
        public virtual int roomCard { get; set; }
        public virtual RecordRoomCardType recordRoomCardType { get; set; }
        public virtual DateTime time { get; set; }
    }
    class RecordRoomCardDBMapping : ClassMap<RecordRoomCardDB>
    {
        public RecordRoomCardDBMapping()
        {
            Id(x => x.recordID).Column("recordID").GeneratedBy.Assigned();
            Map(x => x.summonerId).Column("summonerId");
            Map(x => x.roomCard).Column("roomCard");
            Map(x => x.recordRoomCardType).Column("recordRoomCardType").CustomType<RecordRoomCardType>();
            Map(x => x.time).Column("time");
            Table("recordroomcard");
        }
    }
    public class RecordCurrenciesDB
    {
        public virtual ulong recordID { get; set; }
        public virtual string userID { get; set; }
        public virtual string nickName { get; set; }
        public virtual int recordType { get; set; }
        public virtual string recordContext { get; set; }
        public virtual string currenciesRecordContext { get; set; }
        public virtual DateTime time { get; set; }
    }
    class RecordCurrenciesDBMapping : ClassMap<RecordCurrenciesDB>
    {
        public RecordCurrenciesDBMapping()
        {
            Id(x => x.recordID).Column("recordID").GeneratedBy.Assigned();
            Map(x => x.userID).Column("userID");
            Map(x => x.nickName).Column("nickName");
            Map(x => x.recordType).Column("recordType");
            Map(x => x.recordContext).Column("recordContext");
            Map(x => x.currenciesRecordContext).Column("currenciesRecordContext");
            Map(x => x.time).Column("time");

            Table("recordcurrencies");
        }
    }
    public class RecordLoginUserDB
    {
        public virtual DateTime recordTime { get; set; }
        public virtual int newUsers { get; set; }
        public virtual int loginUsers { get; set; }
    }
    class RecordLoginUserDBMapping : ClassMap<RecordLoginUserDB>
    {
        public RecordLoginUserDBMapping()
        {
            Id(x => x.recordTime).Column("recordTime");
            Map(x => x.newUsers).Column("newUsers");
            Map(x => x.loginUsers).Column("loginUsers");

            Table("recordloginuser");
        }
    }
    public class RecordBusinessUserDB
    {
        public virtual int businessId { get; set; }
        public virtual DateTime recordTime { get; set; }
        public virtual int allUsers { get; set; }
        public virtual int newUsers { get; set; }
        public virtual int effectiveUsers { get; set; }
        public virtual int useOneTickets { get; set; }
        public virtual int useTwoTickets { get; set; }
        public virtual int useThreeTickets { get; set; }
        public virtual int useFourTickets { get; set; }
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
            RecordBusinessUserDB businessUser = obj as RecordBusinessUserDB;
            if (!this.businessId.Equals(businessUser.businessId) || !this.recordTime.Equals(businessUser.recordTime))
            {
                return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            return this.businessId.GetHashCode() + this.recordTime.GetHashCode();
        }
    }
    class RecordBusinessUserDBMapping : ClassMap<RecordBusinessUserDB>
    {
        public RecordBusinessUserDBMapping()
        {
            CompositeId()
            .KeyProperty(x => x.businessId, "businessId")
            .KeyProperty(x => x.recordTime, "recordTime");
            Map(x => x.allUsers).Column("allUsers");
            Map(x => x.newUsers).Column("newUsers");
            Map(x => x.effectiveUsers).Column("effectiveUsers");
            Map(x => x.useOneTickets).Column("useOneTickets");
            Map(x => x.useTwoTickets).Column("useTwoTickets");
            Map(x => x.useThreeTickets).Column("useThreeTickets");
            Map(x => x.useFourTickets).Column("useFourTickets");

            Table("recordbusinessuser");
        }
    }
}
