using LegendProtocol;
using System;

namespace LegendServerProxyDefine
{
    //token: userId|ip|时间|最优逻辑服务器" => "198777|192.168.123.125|2015-06-19 17:43:02|1"（本游戏不需要登陆第三方渠道平台，token关键信息是ip即可）
    public class TokenInfo
    {
        public string userId;
        public string ip;
        public DateTime genTime;
        public int logicId;
        public UserAuthority auth;
        public ulong summonerId;
        public TokenInfo(string userId, string ip, DateTime genTime, int logicId, UserAuthority auth, ulong summonerId)
        {
            this.userId = userId;
            this.ip = ip;
            this.genTime = genTime;
            this.logicId = logicId;
            this.auth = auth;
            this.summonerId = summonerId;
        }
    }
    public class PlatformUserInfo
    {
        public string id;
    }
}
