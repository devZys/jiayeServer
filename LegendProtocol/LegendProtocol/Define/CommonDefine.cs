namespace LegendProtocol
{
    //DB配置
    public class DBConfigInfo
    {
        public string address;
        public string port;
        public string database;
        public string user;
        public string password;
    }
    //会话状态
    public enum SessionStatus
    {
        Disconnect = 1,
        Connected,     
    }
    //通用常量定义
    public struct Util
    {
        public const int needCompressSize = 1024;//byte 超过该字节数则需要压缩
    };    
    //服务类型
    public enum ServiceType
    {
        DB = 1,
        Logic = 2,
        World = 3,
        AC = 4,
        Proxy = 5,
        Center = 6,
        Record = 7,
        ServiceBox = 8,
        WebConsole = 9,
    }

    //行为类型
    public enum ActionType
    {
        GameLog = 0,//普通游戏日志
        ServerInternal,//服务器内部的一些行为，比如掉线出处理或者中心服务器过来的消息处理等等
        DBFlush,//DB入库操作
        GameLoginUser,//游戏登陆用户统计
        GameRoomCard,//游戏房卡统计
        GameBusinessUser,//游戏商家统计

        Count,   
    }

    //本地配置
    public class LocalConfigInfo
    {
        public string key { get; set; }
        public string value { get; set; }
        public string remark { get; set; }

        public LocalConfigInfo(string key, string value, string remark)
        {
            this.key = key;
            this.value = value;
            this.remark = remark;
        }
    };
    //连接结点信息
    public class PeerLink
    {
        public string fromName = "";
        public string fromIp = "";
        public int fromPort = 0;
        public int fromId = 0;

        public string toName = "";
        public string toIp = "";
        public int toPort = 0;
        public int toId = 0;
        public PeerLink(string fromName, string fromIp, int fromPort, int fromId, string toName, string toIp, int toPort, int toId)
        {
            this.fromName = fromName;
            this.fromIp = fromIp;
            this.fromPort = fromPort;
            this.fromId = fromId;

            this.toName = toName;
            this.toIp = toIp;
            this.toPort = toPort;
            this.toId = toId;
        }
    }
    //召唤师状态
    public enum SummonerStatus
    {
        Online = 1,
        Offline = 2,
    }
    //召唤师的英雄使用状态
    public enum HeroUseStatus
    {
        Stay = 1,//空闲在格里
        Present = 2,//出征使用中
    }
    //常量定义
    public struct ConstValue
    {
        public const int AccountLengthLimit = 16;//帐号名称长度限制
        public const int ChatLengthLimit = 50;//聊天内容长度限制
        public const int PasswordLengthLimit = 16;//超过密码长度上限
        public const int NickNameLengthLimit = 12;//超过昵称长度上限
        public const int AccountMinLength = 4;//帐户名最小长度
        public const int PasswordMinLength = 4;//密码最小长度
        public const int NickNameMinLength = 4;//
        public const int RedeemCodeLengthLimit = 32;//兑换码长度上限
    }
    //显示正在刷新到数据库的DB缓存实例的请求者信息
    public class RunningCacheSenderInfo
    {
        public bool show = false;
        public int acServerId = 0;
        public int boxPeerId = 0;
        public RunningCacheSenderInfo(bool show, int acServerId, int boxPeerId)
        {
            this.show = show;
            this.acServerId = acServerId;
            this.boxPeerId = boxPeerId;
        }
    }

    //常量定义
    public struct PokerConstValue
    {
        public const int PokerDanZhang = 1;         //扑克单张
        public const int PokerDuiZi = 2;            //扑克对子
        public const int PokerSanZhang = 3;         //扑克三张
        public const int PokerZhaDan = 4;           //扑克炸弹
        public const int PokerShunZi = 5;           //扑克顺子
    }
    //工作头衔
    public enum JobTitle
    {
        None = 0,//无头衔
        Shopkeeper = 1,//店长
        Customersman = 2,//业务员
        Supervisor = 3,//业务主管
        Manager = 4,//区域经理
        Boss = 5,//公司老板
        God = 6,//上帝
    }
    //销售系统GM权限
    public enum GMAuthority
    {
        Partner = 0,//合伙人
        Servicer = 1,//客服
        Admin = 2,//管理员（有多个）
        Root = 3, //创始人（只有1个人）
    }
}
