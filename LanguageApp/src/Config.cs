using System;
using System.IO;
using Newtonsoft.Json;

namespace LanguageApp.src {
    class Config {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

#if DEBUG
        private static string configPath = Const.DEBUG_CONFIG_FILE;
#else
        private static string configPath = Const.CONFIG_FILE;
#endif

        private static Config instance;

        public int ShowInterval { get; set; }
        public int IterationThreshold { get; set; }
        public int DaysInterval { get; set; }
        public string DatabasePath { get; set; }
        public string ServerUrl { get; set; }
        public int Synchronization { get; set; }

        private Config() { }

        public static Config getInstance() {
            if (instance == null) {
                instance = new Config();
                loadJson();
                validateConfigItems();
                logger.Info(getConfigValuesAsStr);
                //---just because i'm lazy and don't whant to do the authorization
                instance.Synchronization = Const.SYNC_ON;
            }
            return instance;
        }

        public void saveToFile(String filename=null) {
            if(filename == null) {
                filename = configPath;
            }

            logger.Info("Saving config to file " + filename);
            saveJson(filename);
        }
        
        private static void loadJson() {
            logger.Info("loading config from " + configPath);
            try {
                instance = JsonConvert.DeserializeObject<Config>(File.ReadAllText(@configPath));
            } catch (Exception e) {
                logger.Warn("Exception while loading config" + e.Message);
                logger.Warn("Use default config instead");
                setDefaultValues();
            }
        }

        private static void saveJson(String fileName) {
            File.WriteAllText(@fileName, JsonConvert.SerializeObject(instance));
        }

        private static void setDefaultValues() {
#if DEBUG
            instance.ShowInterval = Const.DEBUG_INTERVAL;
            instance.DatabasePath = Const.DEBUG_DATABASE;
            instance.DaysInterval = Const.DAYS_INTERVAL;
            instance.ServerUrl = Const.SERVER_URL;
#else
            instance.ShowInterval = Const.RELEASE_INTERVAL;
            instance.DatabasePath = Const.RELEASE_DATABASE;
            instance.DaysInterval = Const.DAYS_INTERVAL;
            instance.ServerUrl = Const.SERVER_URL;
#endif
        }

        private static void validateConfigItems() {
            if(instance.DatabasePath.Length==0 || instance.DatabasePath.Equals("") || !instance.DatabasePath.EndsWith(".db")) {
                //this.log("incorrect database path: " + instance.DatabasePath + ". Will use default value");
#if DEBUG
                instance.DatabasePath = Const.DEBUG_DATABASE;
#else
            instance.DatabasePath = Const.RELEASE_DATABASE;
#endif
            }
        }

        private static String getConfigValuesAsStr() {
            if (instance == null)
                return null;

            String configStr =
                "CONFIG VALUES:   " +
                "\"Show next word interval\": " + instance.ShowInterval + " sec, " +
                "\"Iteration threshold\": " + instance.IterationThreshold + " day(s), " +
                "\"Show word next time interval\": " + instance.DaysInterval + " day(s), " +
                "\"Database path\": " + instance.DatabasePath +
                "\"Server url\": " + instance.ServerUrl;
            return configStr;
        }
    }
}
