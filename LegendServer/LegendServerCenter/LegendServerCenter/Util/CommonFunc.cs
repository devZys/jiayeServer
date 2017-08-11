using System;
using System.Collections.Generic;
using LegendServerCenter.Record;
using NLog;
using LegendServerCenter.Distributed;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace LegendProtocol
{
    //通用接口
    public class ServerUtil
    {
        private static Logger myLogger = LogManager.GetCurrentClassLogger();
        static public void RecordLog(LogType logType, string context, StackFrame stackFrame = null, bool isSaveToDB = false)
        {
            try
            {
                if (ModuleManager.Get<DistributedMain>().ServerStatus == ServerInternalStatus.Running)
                {
                    ModuleManager.Get<RecordMain>().RecordLog(logType, context, stackFrame, isSaveToDB);
                }
                else
                {
                    PrintLog(logType, "【当前服务器名：" + ModuleManager.Get<RecordMain>().msg_proxy.root.ServerName + " 服务器ID：" + ModuleManager.Get<RecordMain>().msg_proxy.root.ServerID + "】===》" + " " + context, stackFrame);
                }
            }
            catch (System.Exception)
            {
                PrintLog(logType, "【当前服务器名：" + ModuleManager.Get<RecordMain>().msg_proxy.root.ServerName + " 服务器ID：" + ModuleManager.Get<RecordMain>().msg_proxy.root.ServerID + "】===》" + " " + context, stackFrame);
            }
        }
        static public void RecordLog(LogType logType, Exception ex, StackFrame stackFrame = null, bool isSaveToDB = false)
        {
            try
            {
                if (ModuleManager.Get<DistributedMain>().ServerStatus == ServerInternalStatus.Running)
                {
                    ModuleManager.Get<RecordMain>().RecordLog(logType, ex, stackFrame, isSaveToDB);
                }
                else
                {
                    PrintLog(logType, ex, stackFrame);
                }
            }
            catch (System.Exception)
            {

                PrintLog(logType, ex, stackFrame);
            }
        }
        static private void PrintLog(LogType logType, string context, StackFrame stackFrame = null)
        {
            switch (logType)
            {
                case LogType.Info:
                    myLogger.Info(context);
                    break;
                case LogType.Debug:
                    myLogger.Debug(context);
                    break;
                case LogType.Error:
                    if (stackFrame != null)
                    {
                        context = context + " [文件名：" + stackFrame.GetFileName() + " 文件行：" + stackFrame.GetFileLineNumber() + " 文件列：" + stackFrame.GetFileColumnNumber() + "]";
                    }
                    myLogger.Error(context);
                    break;
                case LogType.Fatal:
                    if (stackFrame != null)
                    {
                        context = context + " [文件名：" + stackFrame.GetFileName() + " 文件行：" + stackFrame.GetFileLineNumber() + " 文件列：" + stackFrame.GetFileColumnNumber() + "]";
                    }
                    myLogger.Fatal(context);
                    break;
                default:
                    break;
            }
        }
        static private void PrintLog(LogType logType, Exception ex, StackFrame stackFrame = null)
        {
            switch (logType)
            {
                case LogType.Info:
                    myLogger.Info(ex);
                    break;
                case LogType.Debug:
                    myLogger.Debug(ex);
                    break;
                case LogType.Error:
                    if (stackFrame != null)
                    {
                        myLogger.Error("ExceptionMessage:" + ex.Message + (ex.InnerException != null ? ("InnerExceptionMessage:" + ex.InnerException.Message) : "") + " [文件名：" + stackFrame.GetFileName() + " 文件行：" + stackFrame.GetFileLineNumber() + " 文件列：" + stackFrame.GetFileColumnNumber() + "]");
                    }
                    else
                    {
                        myLogger.Error("ExceptionMessage:" + ex.Message + (ex.InnerException != null ? ("InnerExceptionMessage:" + ex.InnerException.Message) : ""));
                    }
                    break;
                case LogType.Fatal:
                    if (stackFrame != null)
                    {
                        myLogger.Fatal("ExceptionMessage:" + ex.Message + (ex.InnerException != null ? ("InnerExceptionMessage:" + ex.InnerException.Message) : "") + " [文件名：" + stackFrame.GetFileName() + " 文件行：" + stackFrame.GetFileLineNumber() + " 文件列：" + stackFrame.GetFileColumnNumber() + "]");
                    }
                    else
                    {
                        myLogger.Fatal("ExceptionMessage:" + ex.Message + (ex.InnerException != null ? ("InnerExceptionMessage:" + ex.InnerException.Message) : ""));
                    }
                    break;
                default:
                    break;
            }
        }
    }
    //对象基
    public abstract class ObjectBase
    {
        public abstract void Init(params object[] paramList);
    }

    //对象池管理器(采用堆栈存储，支持动态扩容，支持多线程，新扩容的则自动加入到池中能被重复利用)
    public class ObjectPoolManager<T> where T : ObjectBase, new()
    {
        private static ObjectPoolManager<T> instance = null;
        private int blockCapacity = 1000;
        private static object doubleCheckLock = new object();
        private static object objLock = new object();
        private bool inited = false;
        private ConcurrentDictionary<string, Stack<T>> objectPool = new ConcurrentDictionary<string, Stack<T>>();

        private ObjectPoolManager() { }

        public static ObjectPoolManager<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (doubleCheckLock)
                    {
                        if (instance == null)
                        {
                            instance = new ObjectPoolManager<T>();
                        }
                    }
                }
                return instance;
            }
        }

        //初始化对象池
        public string Init(int blockCapacity)
        {
            lock (objLock)
            {
                try
                {
                    if (blockCapacity < 1 || blockCapacity > int.MaxValue)
                    {
                        this.blockCapacity = 1000;
                    }
                    else
                    {
                        this.blockCapacity = blockCapacity;
                    }
                    Stack<T> freeObjList = new Stack<T>();
                    for (int index = 0; index < blockCapacity; index++)
                    {
                        T obj = new T();
                        freeObjList.Push(obj);
                    }
                    objectPool[typeof(T).ToString()] = freeObjList;
                    inited = true;
                    return null;
                }
                catch (Exception ex)
                {
                    return typeof(T).ToString() + " -> ExceptionMessage:" + ex.Message + (ex.InnerException != null ? ("InnerExceptionMessage:" + ex.InnerException.Message) : "");
                }
            }
        }
        //取新对象
        public T NewObject(params object[] paramList)
        {
            lock (objLock)
            {
                try
                {
                    if (!inited)
                    {
                        Init(blockCapacity);
                    }

                    string key = typeof(T).ToString();
                    Stack<T> objList = objectPool[key];

                    if (objList.Count > 0)
                    {
                        T obj = objList.Pop();
                        obj.Init(paramList);
                        return obj;
                    }
                    else
                    {
                        for (int index = 0; index < this.blockCapacity; index++)
                        {
                            T newObj = new T();
                            objList.Push(newObj);
                        }
                        T obj = objList.Pop();
                        obj.Init(paramList);
                        return obj;
                    }
                }
                catch (Exception ex)
                {
                    ServerUtil.RecordLog(LogType.Error, ex);
                    T newObj = new T();
                    newObj.Init(paramList);
                    return newObj;
                }
            }
        }
        //释放对象
        public void FreeObject(T obj)
        {
            lock (objLock)
            {
                try
                {
                    if (obj == default(T)) return;

                    Stack<T> objList = objectPool[typeof(T).ToString()];
                    if (!objList.Contains(obj))
                    {
                        objList.Push(obj);
                    }
                }
                catch (Exception ex)
                {
                    ServerUtil.RecordLog(LogType.Error, ex);
                }
            }
        }
    }
}
