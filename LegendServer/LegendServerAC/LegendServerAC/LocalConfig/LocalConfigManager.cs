using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LegendProtocol;

namespace LegendServer.LocalConfig
{
    //本地配置管理器
    public class LocalConfigManager
    {
        private static LocalConfigManager instance = null;
        private static object objLock = new object();
        private List<LocalConfigInfo> configCache = new List<LocalConfigInfo>();
        public string CurrentPath = "";

        private LocalConfigManager() { }

        public static LocalConfigManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (objLock)
                    {
                        if (instance == null)
                        {
                            instance = new LocalConfigManager();
                            instance.InitAssemblyPath();
                        }
                    }
                }
                return instance;
            }
        }

        private void InitAssemblyPath()
        {
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            codeBase = codeBase.Substring(8, codeBase.Length - 8);
            string[] arrSection = codeBase.Split(new char[] { '/' });
            for (int i = 0; i < arrSection.Length - 1; i++)
            {
                CurrentPath += arrSection[i] + "/";
            }
        }

        //加载配置
        public bool LoadLocalConfig()
        {
            try
            {
                configCache.Clear();

                FileStream fileStream = new FileStream(CurrentPath + "localConfig.csv", FileMode.Open, FileAccess.Read, FileShare.None);
                StreamReader streamReader = new StreamReader(fileStream, System.Text.Encoding.GetEncoding(936));

                int rowCount = 0;
                string record = "";
                while ((record = streamReader.ReadLine()) != null)
                {
                    rowCount++;
                    if (rowCount <= 1) continue;

                    if (record.Count(element => element.ToString() == ",") < 2)  continue;

                    string[] columns = new String[3];
                    columns = record.Split(',');

                    string key = columns[0].Trim();
                    string value = columns[1].Trim();
                    string remark = columns[2].Trim();

                    if (String.IsNullOrEmpty(key) || String.IsNullOrEmpty(value) || String.IsNullOrEmpty(remark)) continue;

                    configCache.Add(new LocalConfigInfo(key, value, remark));
                } 

                streamReader.Close();
                fileStream.Close();

                ModuleManager.LoadLocalConfig();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        

        //取配置
        public LocalConfigInfo GetConfig(string key)
        {
            return configCache.Find(element => element.key == key);
        }
        //取配置
        public List<LocalConfigInfo> GetAllConfig(string key)
        {
            return configCache.FindAll(element => element.key == key);
        }
    }
}
