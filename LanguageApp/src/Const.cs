using System;

namespace LanguageApp.src {
    class Const {
        public static int DEBUG_INTERVAL = 60;       //3 seconds
        public static int RELEASE_INTERVAL = 300;   //5 minutes
        public static int MAX_MESSAGE_SHOW_TIME = 6000000; //60 minutes, 1 sec = 1000
        public static int WORDS_NUMBER_LIMIT = 30;
        public static int WORDS_QUEUE_SIZE = 15;

        public static string DEBUG_DATABASE = "W:\\c#\\MyApp\\LanguageApp\\LanguageApp\\WordsDatabase.db";
        public static string RELEASE_DATABASE = "WordsDatabase.db";
        public static string DEBUG_CONFIG_FILE = "W:\\c#\\MyApp\\LanguageApp\\LanguageApp\\config.json";
        public static string CONFIG_FILE = "config.json";

        public static int ITERATION_THRESHOLD = 3;
        public static int DAYS_INTERVAL = 7;
    }
}
