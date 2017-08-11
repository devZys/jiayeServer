using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using LegendProtocol;
using System.Diagnostics;

namespace LegendServer.Util
{
    public delegate void TimerHandle(object obj);

    //计时器ID
    public enum TimerId
    {
        DisconnectCheck = 0,//断线检测
        FrequentAttackCheck,//频繁发包攻击检测
        ServiceStopKeyCheck,//服务中止key的检测计时器
    }
    //计时器状态
    public enum TimerStatus
    {
        Running = 0,//运行中
        Sleeping,//休眠中
    }
    //计时器
    public class MyTimer
    {
        public TimerId id;
        public int dueTime;//到这个时间才执行
        public int period;//执行间隔时间
        public int lifeTimeTotal;//生命周期
        public TimerHandle handleInLife;//在生命周期内的回调处理
        public object param1;//回调参数
        public TimerHandle handleInEnd;//生命周期结束时的回调处理
        public object param2;//回调参数
        public int lifeTimeCount;//生命时间统计
        public int periodTimeCount;//间隔周期的时间统计
        public int executedCount;//执行统计
        public TimerStatus status;//状态
        private MyTimer() { }
        public MyTimer(TimerId id, int dueTime, int period, int lifeTime, TimerHandle handleInLife, object param1, TimerHandle handleInEnd, object param2)
        {
            this.id = id;
            this.dueTime = dueTime;
            this.period = period;
            this.lifeTimeTotal = lifeTime;
            this.handleInLife = handleInLife;
            this.param1 = param1;
            this.handleInEnd = handleInEnd;
            this.param2 = param2;
            this.lifeTimeCount = 0;
            this.periodTimeCount = 0;
            this.executedCount = 0;
            this.status = TimerStatus.Running;
        }
    }
    public class TimerManager
    {
        private static TimerManager instance = null;
        private static object singletonLocker = new object();//单例双检锁
        private ConcurrentDictionary<TimerId, MyTimer> timerCollection = new ConcurrentDictionary<TimerId, MyTimer>();

        public static TimerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (singletonLocker)
                    {
                        if (instance == null)
                        {
                            instance = new TimerManager();
                            instance.Init();
                        }
                    }
                }
                return instance;
            }
        }
        public TimerManager() { }
        private void Init()
        {
        }
        public bool Exist(TimerId id)
        {
            return timerCollection.ContainsKey(id);
        }
        public void Regist(TimerId id, int dueTime, int period, int lifeTime, TimerHandle handleInLife, object param1, TimerHandle handleInEnd, object param2)
        {
            if (timerCollection.ContainsKey(id))
            {
                MyTimer timer = null;
                timerCollection.TryRemove(id, out timer);
            }
            timerCollection.TryAdd(id, new MyTimer(id, dueTime, period, lifeTime, handleInLife, param1, handleInEnd, param2));
        }
        public MyTimer Get(TimerId id)
        {
            MyTimer timer = null;
            timerCollection.TryGetValue(id, out timer);
            return timer;
        }
        public void Remove(TimerId id)
        {
            MyTimer timer = null;
            timerCollection.TryRemove(id, out timer);
        }
        public void SetStatus(TimerId id, TimerStatus status)
        {
            MyTimer timer = null;
            timerCollection.TryGetValue(id, out timer);

            if (timer != null)
            {
                timer.status = status;
            }
        }
        public void Reset(TimerId id)
        {
            MyTimer timer = null;
            timerCollection.TryGetValue(id, out timer);

            if (timer != null)
            {
                timer.lifeTimeCount = 0;
                timer.periodTimeCount = 0;
                timer.executedCount = 0;
                timer.status = TimerStatus.Running;
            }
        }
        public void Update(int timeFrame)
        {
            try
            {
                foreach (KeyValuePair<TimerId, MyTimer> element in timerCollection)
                {
                    MyTimer myTimer = element.Value;
                    if (myTimer.status == TimerStatus.Sleeping) continue;

                    //计时器的生命周期到了则删除（如果生命周期为int.MaxValue则说明是永久性计时器）
                    if (myTimer.lifeTimeCount >= myTimer.lifeTimeTotal && myTimer.lifeTimeTotal < int.MaxValue)
                    {
                        if (myTimer.handleInEnd != null)
                        {
                            //生命周期结束时的执行
                            myTimer.handleInEnd(myTimer.param2);
                        }

                        MyTimer timer = null;
                        timerCollection.TryRemove(element.Key, out timer);
                        continue;
                    }

                    //累计生命计时
                    if (myTimer.lifeTimeCount < int.MaxValue)
                    {
                        myTimer.lifeTimeCount += timeFrame;
                    }

                    //只有当计时器累计到dueTime后才开始执行
                    if (myTimer.lifeTimeCount >= myTimer.dueTime)
                    {
                        if (myTimer.executedCount <= 0)
                        {
                            if (myTimer.handleInLife != null)
                            {
                                //首次执行
                                myTimer.handleInLife(myTimer.param1);

                                if (myTimer.executedCount < int.MaxValue)
                                {
                                    myTimer.executedCount++;
                                }
                            }
                        }
                        else
                        {
                            //累计间隔周期的时间
                            myTimer.periodTimeCount += timeFrame;

                            if (myTimer.handleInLife != null)
                            {
                                //间隔执行
                                if (myTimer.periodTimeCount >= myTimer.period && myTimer.period > 0)
                                {
                                    myTimer.handleInLife(myTimer.param1);
                                    myTimer.periodTimeCount = 0;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ServerUtil.RecordLog(LogType.Error, ex, new StackTrace(new StackFrame(true)).GetFrame(0));
            }
        }
        public bool IsInTheDay(DateTime lastTime)
        {
            TimeSpan timeSpan = DateTime.Now - lastTime;
            return timeSpan.TotalSeconds <= 86399;
        }
    }
}
