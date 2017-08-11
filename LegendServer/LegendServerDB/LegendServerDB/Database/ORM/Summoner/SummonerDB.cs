using System;
using FluentNHibernate.Mapping;
using LegendProtocol;
using System.Collections.Generic;

namespace LegendServer.Database.Summoner
{
    //召唤师表
    public class SummonerDB
    {
        public virtual ulong id { get; set; }
        public virtual string userId { get; set; }
        public virtual UserSex sex { get; set; }
        public virtual string nickName { get; set; }
        public virtual DateTime loginTime { get; set; }
        public virtual DateTime registTime { get; set; }
        public virtual UserAuthority auth { get; set; }
        public virtual DateTime unLockTime { get; set; }
        public virtual ulong houseId { get; set; }
        public virtual int competitionKey { get; set; }
        public virtual int allIntegral { get; set; }
        public virtual string headImgUrl { get; set; }
        public virtual DateTime dailySharingTime { get; set; }
        public virtual bool bOpenHouse { get; set; }
        public virtual byte[] business { get; set; }
        public virtual byte[] tickets { get; set; }
        public virtual ulong belong { get; set; }
        public virtual DateTime belongBindTime { get; set; }
    }
    class SummonerDBMapping : ClassMap<SummonerDB>
    {
        public SummonerDBMapping()
        {
            Id(x => x.id).Column("id").GeneratedBy.Assigned();
            Map(x => x.userId).Column("userId");
            Map(x => x.sex).Column("sex").CustomType<UserSex>();
            Map(x => x.nickName).Column("nickName");
            Map(x => x.loginTime).Column("loginTime");
            Map(x => x.registTime).Column("registTime");
            Map(x => x.auth).Column("auth").CustomType<UserAuthority>();
            Map(x => x.unLockTime).Column("unLockTime");
            Map(x => x.houseId).Column("houseId");
            Map(x => x.competitionKey).Column("competitionKey");
            Map(x => x.allIntegral).Column("allIntegral");
            Map(x => x.headImgUrl).Column("headImgUrl");
            Map(x => x.dailySharingTime).Column("dailySharingTime");
            Map(x => x.bOpenHouse).Column("bOpenHouse");
            Map(x => x.business).Column("business");
            Map(x => x.tickets).Column("tickets");
            Map(x => x.belong).Column("belong");
            Map(x => x.belongBindTime).Column("belongBindTime");
            Table("summoner");
        }
    }
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
    public class EmployeeInfoDB
    {
        public virtual string account { get; set; }
        public virtual string password { get; set; }
        public virtual ulong summonerId { get; set; }
        public virtual string area { get; set; }
        public virtual JobTitle jobTitle { get; set; }
        public virtual string tel { get; set; }
        public virtual string company { get; set; }
        public virtual string leader { get; set; }
        public virtual byte[] myJunniors { get; set; }
        public virtual string sex { get; set; }
        public virtual string card_no { get; set; }
        public virtual string bank_no { get; set; }
        public virtual string bank_user_name { get; set; }
        public virtual string em_status { get; set; }
        public virtual string bank_name { get; set; }
        public virtual ulong referee_id { get; set; }
        public virtual string crete_date { get; set; }
        public virtual string em_email { get; set; }
        public virtual string shopkeeper_type { get; set; }
        public virtual bool ExistJunnior(string target)
        {
            object obj = ServerUtil.UnSerialize(myJunniors, typeof(List<string>));
            if (obj != null)
            {
                List<string> accounts = (List<string>)obj;
                return accounts.Exists(e => e == target);
            }
            return false;
        }
    }
    class EmployeeInfoDBMapping : ClassMap<EmployeeInfoDB>
    {
        public EmployeeInfoDBMapping()
        {
            Id(x => x.account).Column("account").GeneratedBy.Assigned();
            Map(x => x.password).Column("password");
            Map(x => x.summonerId).Column("summonerId");
            Map(x => x.area).Column("area");
            Map(x => x.jobTitle).Column("jobTitle").CustomType<JobTitle>();
            Map(x => x.tel).Column("tel");
            Map(x => x.company).Column("company");
            Map(x => x.leader).Column("leader");
            Map(x => x.myJunniors).Column("myJunniors");
            Map(x => x.sex).Column("sex");
            Map(x => x.card_no).Column("card_no");
            Map(x => x.bank_no).Column("bank_no");
            Map(x => x.bank_user_name).Column("bank_user_name");
            Map(x => x.em_status).Column("em_status");
            Map(x => x.bank_name).Column("bank_name");
            Map(x => x.referee_id).Column("referee_id");//推荐人游戏ID
            Map(x => x.crete_date).Column("crete_date");//创建日期
            Map(x => x.em_email).Column("em_email");
            Map(x => x.shopkeeper_type).Column("shopkeeper_type");
            Table("employeeinfo");
        }
    }
    //玩家id黑名单
    public class SummonerBlackListDB
    {
        public virtual ulong summonerId { get; set; }
    }
    class SummonerBlackListDBMapping : ClassMap<SummonerBlackListDB>
    {
        public SummonerBlackListDBMapping()
        {
            Id(x => x.summonerId).Column("summonerId").GeneratedBy.Assigned();
            Table("summonerblacklist");
        }
    }
}
