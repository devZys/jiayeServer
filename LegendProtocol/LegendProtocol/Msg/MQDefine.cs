using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LegendProtocol
{
    //MQ 消息id
    public enum MQID
    {
        NotifyPayCheckResult = 0,//Web服务器支付验证结果信息
        NotifyPlayerBindInfo = 1,//通知玩家绑定信息给客户Web系统
        NotifyNewPlayerInfo = 2,//通知新创建的玩家给客户Web系统
        NotifyRechargeDetail = 3,//通知新充值日志给客户Web系统
        NotifyChangeBindCode = 4,//客户Web系统修改了玩家的邀请码绑定关系通知给游戏服务器
        NotifyBroadCastAnnouncement = 5,//通知游戏广播公告（跑马灯）
        NotifyGameServerSendGoods = 6,//通知游戏服务器发货
        NotifyPaySystemFinishPayOrder = 7,//通知支付系统结束订单
        NotifyCustomerSystemRecordPayOrder = 8,//通知客服系统记录订单
        NotifyCustomerSystemModifyPayOrder = 9,//通知客服系统修改订单
        NotifyPaySystemAddRoomCard = 10,//客服在客户系统中手动点补卡按钮时发通知给支付系统来做操作（微信官方回调不及时客服给玩家做手动补卡操作）

    }
    //MQ主机信息
    public class MQHostInfo
    {
        public GameType Game;
        public string Name;
        public string User;
        public string Password;
        public MQHostInfo(GameType game, string host, string user, string password)
        {
            this.Game = game;
            this.Name = host;
            this.User = user;
            this.Password = password;
        }
    }
    //Web服务器支付验证结果信息
    public class WebPayCheckResultInfo
    {
        public string userId;
        public string orderId;
    }
}
