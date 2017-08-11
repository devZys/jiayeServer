using System;
using System.Collections.Generic;
using LegendProtocol;
using LegendServer.Util;
using LegendServer.Database;
using LegendServer.Database.Config;
using System.Collections.Concurrent;
using LegendServerProxy.MainCity;
using LegendServerProxy.Core;
using LegendServerProxyDefine;
using System.Net.Http;
using Newtonsoft.Json;

namespace LegendServerProxy.Login
{
    public class LoginMain : Module
    {
        public LoginMsgProxy msg_proxy;
        public static readonly HttpClient httpClient = new HttpClient();
        private ConcurrentDictionary<string, TokenInfo> allToken = new ConcurrentDictionary<string, TokenInfo>();
        private int tokenExistTime = 60;//token留存时间：分钟

        public LoginMain(object root)
            : base(root)
        {
            httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
        }
        public override void OnCreate()
        {
            msg_proxy = new LoginMsgProxy(this);
        }
        public override void OnLoadLocalConfig()
        {
        }
        public override void OnLoadDBConfig()
        {
            SystemConfigDB cfg = DBManager<SystemConfigDB>.Instance.GetSingleRecordInCache(config => config.key == "tokenExistTime");
            if (cfg != null)
            {
                int.TryParse(cfg.value, out tokenExistTime);
            }
            TimerManager.Instance.Regist(TimerId.TokenCheck, 0, 3000, int.MaxValue, OnTokenCheck, null, null, null);
        }
        public override void OnRegistMsg()
        {
            MsgFactory.Regist(MsgID.C2P_TokenNotify, new MsgComponent(msg_proxy.OnTokenNotify, typeof(TokenNotify_C2P)));
        }
        public override void OnRegistTimer()
        {
        }
        public override void OnStart()
        {
        }
        public override void OnDestroy()
        {
        }
        public bool AddToken(string userId, TokenInfo tokenInfo)
        {
            if (string.IsNullOrEmpty(userId) || tokenInfo == null) return false;

            allToken[userId] = tokenInfo;
            return true;
        }
        public bool TokenIsOK(string accessToken, string requesterUserId, string requesterIp)
        {
            TokenInfo token = null;
            if (!allToken.TryGetValue(requesterUserId, out token))
            {
                //找不到这个人
                ServerUtil.RecordLog(LogType.Error, "TokenIsOK 找不到这个人：【requesterUserId:" + requesterUserId + "】");
                return false;
            }
            if (string.IsNullOrEmpty(accessToken))
            {
                //非法格式的token
                ServerUtil.RecordLog(LogType.Error, "TokenIsOK 非法格式的token：【accessToken:" + accessToken + "】");
                return false;
            }
            string[] tokenParam = accessToken.Split('|');
            if (tokenParam.Length != 4)
            {
                //非法格式的token
                ServerUtil.RecordLog(LogType.Error, "TokenIsOK 非法格式的token：【accessToken:" + accessToken + "】");
                return false;
            }
            if (string.IsNullOrEmpty(tokenParam[0]) || !tokenParam[0].Equals(token.userId))
            {
                //拿着别人的token来验证
                ServerUtil.RecordLog(LogType.Error, "TokenIsOK 拿着别人的token来验证：【sendUserId:" + tokenParam[0] + ", oldUserId:" + token.userId + "】");
                return false;
            }
            if (string.IsNullOrEmpty(tokenParam[1]) || !tokenParam[1].Equals(token.ip) || !requesterIp.Equals(token.ip))
            {
                //IP伪装或者变化了
                ServerUtil.RecordLog(LogType.Error, "TokenIsOK IP伪装或者变化了：【sendIp:" + tokenParam[1] + ", oldIp:" + token.ip + ", requesterIp:" + requesterIp + "】");
                return false;
            }
            if (string.IsNullOrEmpty(tokenParam[2]) || !tokenParam[2].Equals(token.genTime.ToString()))
            {
                //token的生成时间不一致
                ServerUtil.RecordLog(LogType.Error, "TokenIsOK token的生成时间不一致：【sendGenTime:" + tokenParam[2] + ", oldGenTime:" + token.genTime.ToString() + "】");
                return false;
            }
            if (string.IsNullOrEmpty(tokenParam[3]) || !tokenParam[3].Equals(token.logicId.ToString()))
            {
                //token中的逻辑处理服务器id不一致
                ServerUtil.RecordLog(LogType.Error, "TokenIsOK token中的逻辑处理服务器id不一致：【sendLogicId:" + tokenParam[3] + ", oldLogicId:" + token.logicId + "】");
                return false;
            }
            if ((DateTime.Now - token.genTime).Ticks > tokenExistTime * 60 * 10000000L)
            {
                //认证时间过期了（在计时器里会移除）
                ServerUtil.RecordLog(LogType.Error, "TokenIsOK 认证时间过期了：【genTime:" + token.genTime + "】");
                return false;
            }

            return true;
        }
        public void RemoveToken(string userId)
        {
            TokenInfo removeElement = null;
            allToken.TryRemove(userId, out removeElement);
        }
        public TokenInfo GetToken(string userId)
        {
            TokenInfo token = null;
            allToken.TryGetValue(userId, out token);
            return token;
        }
        public int GetRouteServerID(string userId)
        {
            TokenInfo token = null;
            if (allToken.TryGetValue(userId, out token))
            {
                return token.logicId;
            }
            return 0;
        }
        public UserAuthority GetUserAuthority(string userId)
        {
            TokenInfo token = null;
            if (allToken.TryGetValue(userId, out token))
            {
                return token.auth;
            }
            return UserAuthority.Illegal;
        }
        private void OnTokenCheck(object obj)
        {
            //检测过期的token
            try
            {
                if (allToken.Count <= 0) return;

                foreach (KeyValuePair<string, TokenInfo> element in allToken)
                {
                    if ((DateTime.Now - element.Value.genTime).Ticks > tokenExistTime * 60 * 10000000L)
                    {
                        TokenInfo token;
                        allToken.TryRemove(element.Key, out token);

                        ServerUtil.RecordLog(LogType.Error, "发现基因变异恶意僵尸玩家：" + token.userId + " 迟迟未连接网关做认证，已移除！");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex);
            }
        }
    }

}