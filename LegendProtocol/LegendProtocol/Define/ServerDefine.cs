using System;

namespace LegendProtocol
{
    //具体游戏类型
    public enum GameType
    {
        YPHNMJ = 0,
        YPHNPDZ = 1,
        YPYYMJ = 2,
        YPYYPDK = 3,
        YPYYWHZ = 4,
        YPXSMJ = 5,
        YPXSPDK = 6,

        COUNT,
    }

    //支付状态
    public enum PayStatus
    {
        Off = 0,//已半途关闭交易
        PreVerfiy = 1,//待验证
        Paid = 2,//已付款
        SendGoods = 3,//服务器已发货
    }
    //房卡记录
    public enum RecordRoomCardType
    {
        //空
        None,
        //正常消耗
        NormalConsumption,
        //每日分享奖励
        DailySharing,
        //绑定邀请码的奖励
        BindCodeRewardRoomCard,
        //解散归还
        DissolvedRecycle,
    }
    //登陆统计类型
    public enum RecordLoginType
    {
        //用户第一次登陆上线
        NewUser,
        //用户今天第一次登陆上线
        LoginUser,
    }
    //玩家消耗
    public class BureauByHouseCard
    {
        public int bureau;
        public int houseCard;
        public BureauByHouseCard() { }
        public BureauByHouseCard(int bureau, int houseCard)
        {
            this.bureau = bureau;
            this.houseCard = houseCard;
        }
    }
}
