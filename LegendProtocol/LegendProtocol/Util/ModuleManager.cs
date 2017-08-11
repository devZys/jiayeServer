using System.Collections.Generic;

namespace LegendProtocol
{
    public delegate void ModuleUpdateFun(float time);

    //模块基
    public abstract class Module
    {
        public object root;
        public Module(object root)
        {
            this.root = root;
            OnCreate();
            OnRegistMsg();
        }
        public abstract void OnCreate();
        public abstract void OnLoadLocalConfig();
        public abstract void OnLoadDBConfig();
        public abstract void OnStart();
        public abstract void OnDestroy();
        public abstract void OnRegistMsg();
        public abstract void OnRegistTimer();
    }

    //模块管理器
    public class ModuleManager
    {
        public static Dictionary<string, Module> moduleMap = new Dictionary<string, Module>();

        public static void Regist(Module module)
        {
            string name = module.ToString();
            moduleMap[name] = module;
        }
        public static void LoadLocalConfig()
        {
            foreach (Module element in moduleMap.Values)
            {
                element.OnLoadLocalConfig();
            }
        }
        public static void LoadDBConfig()
        {
            foreach (Module element in moduleMap.Values)
            {
                element.OnLoadDBConfig();
            }
        }
        public static void RegistTimer()
        {
            foreach (Module element in moduleMap.Values)
            {
                element.OnRegistTimer();
            }
        }
        public static void Start()
        {
            foreach (Module element in moduleMap.Values)
            {
                element.OnStart();
            }            
        }
        public static void Destroy()
        {
            foreach (Module element in moduleMap.Values)
            {
                element.OnDestroy();
            }
        }
        public static T Get<T>() where T : Module
        {
            return moduleMap[typeof(T).ToString()] as T;
        } 
        public static bool Exist<T>() where T : Module
        {
            return moduleMap.ContainsKey(typeof(T).ToString());
        }
    }

}
