using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using LegendProtocol;
using LegendServerDB.Core;
using LegendServerDB.Distributed;
using LegendServer.Database.Summoner;
using LegendServer.Database;
using LegendServer.Util;

namespace LegendServerDB.Authority
{
    public class AuthorityMsgProxy : ServerMsgProxy
    {
        private AuthorityMain main;

        public AuthorityMsgProxy(AuthorityMain main)
            : base(main.root)
        {
            this.main = main;
        }
        public void OnReqSetUserAuthority(int peerId, bool inbound, object msg)
        {
            RequestSetUserAuthority_A2D reqMsg_A2D = msg as RequestSetUserAuthority_A2D;

            ReplySetUserAuthority_D2A replyMsg_D2A = new ReplySetUserAuthority_D2A();

            SummonerDB mySelf = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.userId == reqMsg_A2D.mySelfUserId);
            if (mySelf == null)
            {
                replyMsg_D2A.acPeerId = reqMsg_A2D.acPeerId;
                replyMsg_D2A.result = ResultCode.InvalidPlayer;
                SendMsg(peerId, inbound, replyMsg_D2A);
                return;
            }

            SummonerDB target = DBManager<SummonerDB>.Instance.GetSingleRecordInCache(element => element.id == reqMsg_A2D.guid);
            if (target == null)
            {
                replyMsg_D2A.acPeerId = reqMsg_A2D.acPeerId;
                replyMsg_D2A.result = ResultCode.InvalidPlayer;
                SendMsg(peerId, inbound, replyMsg_D2A);
                return;
            }

            if (reqMsg_A2D.auth == UserAuthority.Root)
            {
                int rootCount = DBManager<SummonerDB>.Instance.GetRecordCount(element => element.auth == UserAuthority.Root);
                if (rootCount <= 0)
                {
                    //首次设置root权限不需要检测权限  
                    replyMsg_D2A.acPeerId = reqMsg_A2D.acPeerId;
                    replyMsg_D2A.userInfo = new UserInfo()
                    {
                        guid = reqMsg_A2D.guid,
                        userId = target.userId,
                        nickName = target.nickName,
                        auth = reqMsg_A2D.auth,
                        unLockTime = (reqMsg_A2D.lockTime > 0) ? DateTime.Now.AddMinutes(reqMsg_A2D.lockTime).ToString() : DateTime.Parse("1970-01-01 00:00:00").ToString()
                    };
                    replyMsg_D2A.result = SetAuthority(mySelf, target, replyMsg_D2A.userInfo.auth, replyMsg_D2A.userInfo.unLockTime);
                    SendMsg(peerId, inbound, replyMsg_D2A);
                    return;
                }
                else
                {
                    //已存在超级管理员帐户
                    replyMsg_D2A.acPeerId = reqMsg_A2D.acPeerId;
                    replyMsg_D2A.result = ResultCode.ExistRootUser;
                    SendMsg(peerId, inbound, replyMsg_D2A);
                    return;
                }
            }
            else
            {               
                if (mySelf.auth == UserAuthority.Guest || mySelf.auth == UserAuthority.Illegal)
                {
                    //没有权限
                    replyMsg_D2A.acPeerId = reqMsg_A2D.acPeerId;
                    replyMsg_D2A.result = ResultCode.NoAuth;
                    SendMsg(peerId, inbound, replyMsg_D2A);
                    return;
                }
                if (reqMsg_A2D.auth == UserAuthority.Admin && mySelf.auth != UserAuthority.Root)
                {
                    //没有权限
                    replyMsg_D2A.acPeerId = reqMsg_A2D.acPeerId;
                    replyMsg_D2A.result = ResultCode.NoAuth;
                    SendMsg(peerId, inbound, replyMsg_D2A);
                    return;
                }

                replyMsg_D2A.acPeerId = reqMsg_A2D.acPeerId;
                replyMsg_D2A.userInfo = new UserInfo()
                {
                    guid = reqMsg_A2D.guid,
                    userId = target.userId,
                    nickName = target.nickName,
                    auth = reqMsg_A2D.auth,
                    unLockTime = (reqMsg_A2D.lockTime > 0) ? DateTime.Now.AddMinutes(reqMsg_A2D.lockTime).ToString() : DateTime.Parse("1970-01-01 00:00:00").ToString()
                };

                replyMsg_D2A.result = SetAuthority(mySelf, target, replyMsg_D2A.userInfo.auth, replyMsg_D2A.userInfo.unLockTime);
                if (replyMsg_D2A.result == ResultCode.OK && reqMsg_A2D.lockTime > 0 && reqMsg_A2D.auth == UserAuthority.Illegal)
                {
                    if (!TimerManager.Instance.Exist(TimerId.UnfreezeCheck))
                    {
                        TimerManager.Instance.Regist(TimerId.UnfreezeCheck, 0, 30000, int.MaxValue, main.OnUnfreezeCheckTimer, null, null, null);
                    }
                }
                SendMsg(peerId, inbound, replyMsg_D2A);
                return;
            }
        }
        public void OnReqGetAllSpecificUser(int peerId, bool inbound, object msg)
        {
            RequestGetAllSpecificUser_A2D reqMsg_A2D = msg as RequestGetAllSpecificUser_A2D;

            ReplyGetAllSpecificUser_D2A replyMsg_D2A = new ReplyGetAllSpecificUser_D2A();
            replyMsg_D2A.acPeerId = reqMsg_A2D.acPeerId;

            List<SummonerDB> userList = DBManager<SummonerDB>.Instance.GetRecordsInCache(element => element.auth == UserAuthority.Root || element.auth == UserAuthority.Admin || element.auth == UserAuthority.Illegal);
            if (userList != null && userList.Count > 0)
            {
                userList.ForEach(user => replyMsg_D2A.allSpecificUser.Add(new UserInfo() { guid = user.id, userId = user.userId, nickName = user.nickName, auth = user.auth, unLockTime = user.unLockTime.ToString() }));
            }
            SendMsg(peerId, inbound, replyMsg_D2A);
        }
        private ResultCode SetAuthority(SummonerDB mySelf, SummonerDB target, UserAuthority auth, string unlockTime)
        {
            if (mySelf == null || target == null) return ResultCode.InvalidPlayer;

            if (target.id == mySelf.id)
            {
                //给自己设置
                mySelf.auth = auth;

                //持久化
                DBManager<SummonerDB>.Instance.UpdateRecordInCache(mySelf, e => e.id == mySelf.id);
            }
            else
            {
                //给别人设置
                target.auth = auth;
                target.unLockTime = DateTime.Parse(unlockTime);

                //持久化
                DBManager<SummonerDB>.Instance.UpdateRecordInCache(target, e => e.id == target.id);
            }
            return ResultCode.OK;
        }
    }
}

