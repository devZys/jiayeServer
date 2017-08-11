using LegendProtocol;
using LegendServerProxy.Core;
using LegendServerProxyDefine;
using System;

namespace LegendServerProxy.Login
{
    public class LoginMsgProxy : ServerMsgProxy
    {
        private LoginMain main;

        public LoginMsgProxy(LoginMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnTokenNotify(int peerId, bool inbound, object msg)
        {
            TokenNotify_C2P notifyMsg_C2P = msg as TokenNotify_C2P;

            //token: userId|ip|时间|最优逻辑服务器" => "198777|192.168.123.125|2015-06-19 17:43:02|1"（本游戏不需要登陆第三方渠道平台，token关键信息是ip即可）
            string[] tokenParam = notifyMsg_C2P.accessToken.Split('|');
            if (tokenParam.Length >= 4)
            {
                if (main.AddToken(tokenParam[0], new TokenInfo(tokenParam[0], tokenParam[1], DateTime.Parse(tokenParam[2]), int.Parse(tokenParam[3]), notifyMsg_C2P.auth, notifyMsg_C2P.summonerId)))
                {
                    NotifyLoginData_P2A notifyMsg_P2A = new NotifyLoginData_P2A();
                    notifyMsg_P2A.acPeerId = notifyMsg_C2P.acPeerId;
                    notifyMsg_P2A.logicId = int.Parse(tokenParam[3]);
                    notifyMsg_P2A.proxyId = root.ServerID;
                    notifyMsg_P2A.auth = notifyMsg_C2P.auth;
                    notifyMsg_P2A.userId = tokenParam[0];
                    notifyMsg_P2A.closedACServerList.AddRange(notifyMsg_C2P.closedACServerList);
                    notifyMsg_P2A.accessToken = notifyMsg_C2P.accessToken;
                    SendACMsg(notifyMsg_P2A, notifyMsg_C2P.acId);
                }
            }
        }
    }
}

