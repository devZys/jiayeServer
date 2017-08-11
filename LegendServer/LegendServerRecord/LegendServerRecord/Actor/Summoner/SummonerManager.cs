using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using LegendProtocol;
using LegendServer.Util;
using LegendServer.Database;
using LegendServer.Database.Config;
using LegendServer.Database.Summoner;
using LegendServerDB.Core;

namespace LegendServerDB.Actor.Summoner
{    
    public class SummonerManager
    {
        private static object singletonLocker = new object();//单例双检锁
        private ConcurrentDictionary<string, Summoner> summonerCollection = new ConcurrentDictionary<string, Summoner>();

        private static SummonerManager instance = null;
        public static SummonerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new SummonerManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }
        public SummonerManager() {}
        public void Init()
        {
        }
        public ConcurrentDictionary<string, Summoner> GetSummonerCollection()
        {
            return summonerCollection;
        }
        //新增召唤师
        public void AddSummoner(string account, Summoner summoner)
        {
            summonerCollection.TryAdd(account, summoner);
        }
        //通过PeerId获取召唤师
        public Summoner GetSummonerByPeerId(int peerId)
        {
            KeyValuePair<string, Summoner> findResult = summonerCollection.FirstOrDefault(element => element.Value.session.peer.ConnectionId == peerId);
            return findResult.Value;
        }
        //通过帐户名获取召唤师
        public Summoner GetSummonerByAccount(string account)
        {
            Summoner summoner;
            summonerCollection.TryGetValue(account, out summoner);
            return summoner;
        }        
        //移除召唤师
        public void RemoveSummoner(string account)
        {
            Summoner summoner = null;
            summonerCollection.TryRemove(account, out summoner);
        }        
        //召唤师的普通登陆
        public ResultCode OnSummonerNormalLogin(int peerId, string account, string nickName, int currentHero, CombatGains combatGains)
        {
            Session session = SessionManager.Instance.GetSessionByPeerId(peerId);
            if (session.status != SessionStatus.Connected)
            {
                //未连接状态下是非法操作
                return ResultCode.NotConnected;
            }
            Summoner summoner = SummonerManager.Instance.GetSummonerByAccount(account);
            if (summoner != null)
            {
                //重复登陆也是非法操作
                return ResultCode.RepeatOnline;
            }

            Summoner newSummoner = new Summoner(session);
            newSummoner.account = account;
            newSummoner.nickName = nickName;
            newSummoner.currentHero = currentHero;
            newSummoner.loginTime = System.DateTime.Now;
            newSummoner.combatGains = combatGains;
            newSummoner.status = SummonerStatus.Online;

            AddSummoner(account, newSummoner);

            return ResultCode.OK;          
        }
        //召唤师的断线重新登陆
        public ResultCode OnSummonerReconnectLogin(string account, int newPeerId)
        {
            //找出断线重连时创建的会话空对象
            Session newMySelfSession = SessionManager.Instance.GetSessionByPeerId(newPeerId);
            if (newMySelfSession == null)
            {
                //非法会话
                return ResultCode.InvalidPlayer;
            }
            //找出断线前的Summoner对象
            Summoner summoner = GetSummonerByAccount(account);
            if (summoner == null)
            {
                //召唤师断线超时已经被删除
                return ResultCode.DisconnectTimeOut;
            }
            //找出断线前时的会话
            Session oldMySelfSession = null;
            if (summoner.session != null && summoner.session.peer != null)
            {
                oldMySelfSession = SessionManager.Instance.GetSessionByPeerId(summoner.session.peer.ConnectionId);
                if (oldMySelfSession == null)
                {
                    //召唤师断线超时已经被删除
                    return ResultCode.DisconnectTimeOut;
                }
            }
            else
            {
                //召唤师断线超时已经被删除
                return ResultCode.DisconnectTimeOut;
            }
            if (summoner.status != SummonerStatus.Offline || oldMySelfSession.status != SessionStatus.Disconnect)
            {
                //不是断线状态的重连是非法操作
                return ResultCode.PlayerNotDisconnect;
            }

            //克隆掉线前的会话数据
            oldMySelfSession.ReconnectClone(newMySelfSession);

            //更新召唤师的信息
            summoner.session = newMySelfSession;
            summoner.loginTime = DateTime.Now;
            summoner.status = SummonerStatus.Online;

            //删除掉线前的Session对象
            SessionManager.Instance.RemoveSession(oldMySelfSession.peer.ConnectionId);

            return ResultCode.OK;          
        }
        //召唤师上线
        public ResultCode OnSummonerOnline(int peerId, int heroIndex, bool isReconnect)
        {
            Session session = SessionManager.Instance.GetSessionByPeerId(peerId);
            if (session == null || session.peer == null)
            {
                //非法召唤师
                return ResultCode.InvalidPlayer;
            }
            if (session.status != SessionStatus.Connected)
            {
                //未连接的召唤师
                return ResultCode.NotConnected;
            }
            KeyValuePair<string, Summoner> findResult = summonerCollection.FirstOrDefault(element => element.Value.session.peer.ConnectionId == peerId);
            Summoner summoner = findResult.Value;
            if (summoner == null)
            {
                //非法召唤师
                return ResultCode.InvalidPlayer;
            }
            SummonerDB summonerDB = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.account == summoner.account);
            if (summonerDB == null)
            {
                //非法召唤师
                return ResultCode.InvalidPlayer;
            }
            Item item = summoner.GetItem(heroIndex);
            if (item == null)
            {
                //该召唤师不拥有该英雄道具
                return ResultCode.NoHaveThisHero;
            }
            if (isReconnect == false)
            {
                if (item.useStatus == ItemUseStatus.Present)
                {
                    //该召唤师要上线的英雄不能重复上线
                    return ResultCode.RepeatOnline;
                }
            }
            else
            {
                if (item.useStatus != ItemUseStatus.Disconnect)
                {
                    //英雄已不是断线状态(有可能本场游戏结束)
                    return ResultCode.HeroNotDisconnect;
                }
            }

            //修改召唤师的状态
            summoner.status = SummonerStatus.Online;

            //将当前已上线的英雄下架
            Item currentHero = summoner.GetItem(summoner.currentHero);
            currentHero.useStatus = ItemUseStatus.Stay;

            //上架新英雄
            item.useStatus = ItemUseStatus.Present;
            summoner.currentHero = item.id;

            //修改入库
            ServerCPU.Instance.PushDBUpdateCommand(() => NHibernateHelper.InsertOrUpdateOrDelete<SummonerDB>(summonerDB, ORMDataOperate.Update));            
            return ResultCode.OK;
        }       
        //召唤师断线
        public void OnSummonerDisconnect(int peerId)
        {
            Session session = SessionManager.Instance.GetSessionByPeerId(peerId);

            KeyValuePair<string, Summoner> findResult = summonerCollection.FirstOrDefault(element => element.Value.session.peer.ConnectionId == peerId);
            Summoner summoner = findResult.Value;
            if (summoner == null) return;

            summoner.OnDisconnect();
        }
        //召唤师登出
        public void OnSummonerLogout(int peerId)
        {
            Session session = SessionManager.Instance.GetSessionByPeerId(peerId);

            KeyValuePair<string, Summoner> findResult = summonerCollection.FirstOrDefault(element => element.Value.session.peer.ConnectionId == peerId);
            Summoner summoner = findResult.Value;
            if (summoner == null) return;

            RemoveSummoner(summoner.account);
        }
    }
}
