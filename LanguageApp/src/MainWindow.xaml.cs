using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using LanguageApp.src;
using System.Text;
using System.Reflection;
using System.IO;
using System.Windows.Controls;

namespace LanguageApp {
    /// <summary>
    /// Main project window, invisible (only contains declaration of the tray icon and context menu)
    /// </summary>
    public partial class MainWindow : Window {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private int maxBalloonStayTime = Const.MAX_MESSAGE_SHOW_TIME;
        private TaskbarIcon taskbarIcon;
        private DatabaseHandler dbHandler;
        private DispatcherTimer mainTimer;
        private DispatcherTimer tooltipTimer;
        private bool isMainTimerEnabled = true;
        private int timeLeftCounter = Const.DEBUG_INTERVAL;
        private int configOldInterval = 0;
        private static readonly Random random = new Random();

        Config config = null;
        DatabaseWindow databaseWindow = null;
        AddWordWindow addWordWindow = null;
        SettingsWindow settingsWindow = null;

        #region TooltipMessage dependency property
        public static readonly DependencyProperty TooltipMessageProperty =
            DependencyProperty.Register("TooltipMessage",
                typeof(String),
                typeof(MainWindow),
                new FrameworkPropertyMetadata("tooltip message"));
        public String TooltipMessage {
            get { return (String)GetValue(TooltipMessageProperty); }
            set { SetValue(TooltipMessageProperty, value); }
        }
        #endregion

        public MainWindow() {
#if !DEBUG //show error message only in RELEASE mod
            try {
#endif
                logger.Info("-------------------------------------------");
                logger.Info("------------ Start application ------------");
                InitializeComponent();
                this.Hide();

                config = Config.getInstance();
                dbHandler = new DatabaseHandler(config.DatabasePath);
                configOldInterval = config.ShowInterval;
                taskbarIcon = MyNotifyIcon; //get the taskbar icon, which was declared in xaml
                timeLeftCounter = config.ShowInterval;

                //hide synchronization menu items if needed
                if (config.Synchronization == Const.SYNC_OFF) {
                    MenuItem itemGet = GetFromServer;
                    itemGet.Visibility = Visibility.Collapsed;
                    MenuItem itemSend = SendToServer;
                    itemSend.Visibility = Visibility.Collapsed;
                }

                mainTimer = new DispatcherTimer(TimeSpan.FromSeconds(config.ShowInterval), DispatcherPriority.Normal, OnMainTimerTick, Application.Current.Dispatcher);
                OnMainTimerTick(null, null); //start timer without waiting

                tooltipTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnTooltipTimerTick, Application.Current.Dispatcher);

            //DictionaryItem item = new DictionaryItem(9, "cios2", "удар", 7, 3, "2019-05-01 23:46:14", "2019-06-01 23:46:14");
            //dbHandler.upsertWord(item);

#if !DEBUG //show error message only in RELEASE mod
            } catch (Exception e) {
                logger.Error(e.Message);
                MessageBoxResult result = MessageBox.Show(e.Message,
                                           "Error",
                                           MessageBoxButton.OK,
                                           MessageBoxImage.Error, 
                                           MessageBoxResult.OK,
                                           MessageBoxOptions.DefaultDesktopOnly);
                this.Close();
            }
#endif
        }

        /*
         * Show message with random word from database
         */
        private void OnMainTimerTick(object sender, EventArgs e) {
            logger.Trace("Main timer tick");
            taskbarIcon.CloseBalloon(); //force close balloon if it's already open

            DictionaryItem randomWord = GetRandomWord();
            if (randomWord != null) {
                FancyBalloon balloon = new FancyBalloon(mainTimer, randomWord, processAnswerFunction);
                //show balloon and close it after maxBalloonStayTime
                if (taskbarIcon != null)
                    taskbarIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, maxBalloonStayTime);
            } else {
                ErrorBaloon balloon = new ErrorBaloon();
                if (taskbarIcon != null)
                    taskbarIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, maxBalloonStayTime);
            }
            timeLeftCounter = config.ShowInterval;
            deleteOldLogFiles(); //delete log files older than 10 days
        }

        /*
         * show a tooltip, which contains countdown till next message shows
         */
        private void OnTooltipTimerTick(object sender, EventArgs e) {
            logger.Trace("tooltipTimer: tick");
            if (mainTimer.IsEnabled != isMainTimerEnabled) {
                logger.Trace("tooltipTimer: reset time left counter");
                timeLeftCounter = config.ShowInterval-1;
                isMainTimerEnabled = mainTimer.IsEnabled;
            } else if (configOldInterval != config.ShowInterval) {
                logger.Trace("tooltipTimer: reset time left couonter because of config change");
                mainTimer.Interval = TimeSpan.FromSeconds(config.ShowInterval);
                mainTimer.Start();
                timeLeftCounter = config.ShowInterval;
                configOldInterval = config.ShowInterval;
            }

            TimeSpan t = TimeSpan.FromSeconds(timeLeftCounter);
            string timeLeft;
            if (t.Hours == 0) {
                timeLeft = string.Format("{0:D2}m:{1:D2}s", t.Minutes, t.Seconds);
            } else {
                timeLeft = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", t.Hours, t.Minutes, t.Seconds);
            }
            TooltipMessage = "Time left before next word is displayed: " + timeLeft;
            timeLeftCounter--;
        }

        //get random word from database (from 30 current studying words)
        // but only if it wasn't return for 15 iterations
        private Queue<String> alreadyShowedWordsQueue = new Queue<String>(Const.WORDS_QUEUE_SIZE);

        private DictionaryItem GetRandomWord() {
            logger.Trace("getting rundom word from database");
            dbHandler.updateDatabasePath(config.DatabasePath); //TODO this should happen somewhere else! Maybe when setting window closed?
            //get the list of 30 words from database
            List<DictionaryItem> currentWordsList = dbHandler.getCurrentWords();
            DictionaryItem randomItem;

            if (currentWordsList.Count > 0) {
                //logger.Debug("Show words queue: " + listToString(alreadyShowedWordsQueue));
                //select random word and return it
                do {
                    int randomIndex = random.Next(0, currentWordsList.Count);
                    randomItem = currentWordsList[randomIndex];
                } while (alreadyShowedWordsQueue.Contains(randomItem.Word) && alreadyShowedWordsQueue.Count < currentWordsList.Count);

                if (alreadyShowedWordsQueue.Count == Const.WORDS_QUEUE_SIZE)
                    alreadyShowedWordsQueue.Dequeue(); //if queue is full, remove first element
                                                       //put the word to queue, so it won't return at least for Const.WORDS_QUEUE_SIZE(by default 15) iterations
                alreadyShowedWordsQueue.Enqueue(randomItem.Word);

                logger.Debug("Show random word: " + randomItem.toString());
            } else {
                logger.Debug("No available words found in database");
                randomItem = null;
            }
            return randomItem;
        }

        private String listToString(Queue<String> list) {
            StringBuilder sb = new StringBuilder();
            int i = 1;
            foreach (String str in list) {
                sb.Append(i++).Append(") ").Append(str).Append(" ");
            }
            return sb.ToString();
        }

        //callback function which will be called when user give an answer in balloon window
        private void processAnswerFunction(DictionaryItem dItem, string answer) {
            logger.Trace("processing answer");
            if(dItem == null) {
                logger.Error("dItem is null!");
                return;
            }

            logger.Debug(String.Format("Answer: {0}. Updating database for the word: {1}, id: {2}", answer, dItem.Word, dItem.Id));
            if (answer.Equals("correct")) {
                dItem.CorrectAnswers = dItem.CorrectAnswers + 1;                    

                if (dItem.Iteration >= 2) {  //started with 2nd iteration needed only one correct answer to proof that user know the word
                    dItem.NextShowDate = DateTime.Now.AddDays(dItem.Iteration * config.DaysInterval);
                    dItem.Iteration = dItem.Iteration + 1;
                } else if (dItem.CorrectAnswers % Const.ITERATION_THRESHOLD == 0) { //num of correct answers on this iteration = new interation threshold
                        if (dItem.Iteration == 1) { //second iteration will start after a time gap of config.DaysInterval
                            dItem.NextShowDate = DateTime.Now.AddDays(config.DaysInterval);
                        }
                        dItem.Iteration = dItem.Iteration + 1;
                    }

            } else {
                dItem.CorrectAnswers = 0;
                dItem.Iteration = 0;
                dItem.NextShowDate = default(DateTime);
            }

            logger.Debug(String.Format("Setting correct answers to: {0}", dItem.CorrectAnswers));
            dbHandler.updateWord(dItem);
            sendWordToServer(dItem);
        }

        private void sendWordToServer(DictionaryItem dItem) {
            String jsonObj = "{\"id\":" + dItem.Id
                + ",\"word\":" + "\"" + dItem.Word + "\""
                + ",\"translation\":" + "\"" + dItem.Translation + "\""
                + ",\"correct_answers\":" + dItem.CorrectAnswers
                + ",\"iteration\":" + dItem.Iteration;

            DateTime nextDate = dItem.NextShowDate;
            if (!Object.Equals(nextDate, default(DateTime))) { //if date != null
                string nextDateStr = nextDate.ToString("yyyy-MM-dd HH:mm:ss");
                jsonObj = jsonObj + ",\"next_show_date\":" + "\"" + nextDateStr + "\"";
            } else {
                jsonObj = jsonObj + ",\"next_show_date\":" + "null";
            }

            DateTime currentDate = DateTime.Now;
            string updateDateStr = currentDate.ToString("yyyy-MM-dd HH:mm:ss");
            jsonObj = jsonObj + ",\"last_update_date\":" + "\"" + updateDateStr + "\"" + "}"; ;

            String jsonStr = "{\"words\": [" + jsonObj + "] }";
            logger.Info("JsonStr: " + jsonStr);

            if(config.Synchronization == Const.SYNC_ON)
                Synchronizator.sendRequestAsync(jsonStr);
        }



        /* -------------------------WINDOW RELATED FUNCTIONS--------------------------*/

        //Context menu item "Close" was clicked
        private void OnCloseMenuItemClick(object sender, RoutedEventArgs e) {
            if(databaseWindow != null) databaseWindow.Close();
            this.Close(); //close the main window
        }

        //on main window closing
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
            //need to hide all visible objects before closing window, or thay will stay visible
            taskbarIcon.CloseBalloon();
            taskbarIcon.Dispose();

            base.OnClosing(e);
        }

        private void OnDatabaseMenuItemClick(object sender, RoutedEventArgs e) {
            if(databaseWindow!=null) databaseWindow.Close();
            databaseWindow = new DatabaseWindow(config.DatabasePath);
            ShowNewWindow(databaseWindow);
        }

        private void OnNextWordMenuItemClick(object sender, RoutedEventArgs e) {
            OnMainTimerTick(null, null); //show next word without waiting
            mainTimer.Start(); //set main timer counter to the begining
        }

        private void OnAddWordMenuItemClick(object sender, RoutedEventArgs e) {
            if (addWordWindow != null) addWordWindow.Close();
            addWordWindow = new AddWordWindow(config.DatabasePath);
            ShowNewWindow(addWordWindow);
        }

        private void OnSettingsMenuItemClick(object sender, RoutedEventArgs e) {
            if (settingsWindow != null) settingsWindow.Close();
            if (settingsWindow != null) settingsWindow.Close();
            settingsWindow = new SettingsWindow();
            ShowNewWindow(settingsWindow);
        }

        private void SendToServerMenuItemClick(object sender, RoutedEventArgs e) {
            logger.Info("getting all data from db as json");
            MessageBoxResult askResult =
                MessageBox.Show("Send data to server? It will rewrite server state completely", "Info",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information, MessageBoxResult.No, MessageBoxOptions.DefaultDesktopOnly);
            if (askResult == MessageBoxResult.Yes) {
                String jsonStr = dbHandler.getAllDataAsJson();
                jsonStr = "{\"words\": " + jsonStr + " }";
                logger.Info("sending data to server, data length is " + jsonStr.Length);
                if (jsonStr.Length > 0)
                    Synchronizator.sendRequestAsync(jsonStr);
            }
        }

        private async void GetFromServerMenuItemClick(object sender, RoutedEventArgs e) {
            logger.Info("getting all data from server as json");

            try {
                String jsonStr = await Synchronizator.getJsonAsync();
                logger.Info("got json from server, length: " + jsonStr.Length);

                if (jsonStr.Length > 0) {
                    String currentDir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                    String syncDir = currentDir + "\\sync";
                    bool exists = System.IO.Directory.Exists(syncDir);
                    if (!exists) {
                        logger.Info("creating directory: " + syncDir);
                        System.IO.Directory.CreateDirectory(syncDir);
                    }

                    String bkpDir = currentDir + "\\bkp";
                    exists = System.IO.Directory.Exists(bkpDir);
                    if (!exists) {
                        logger.Info("creating directory: " + bkpDir);
                        System.IO.Directory.CreateDirectory(bkpDir);
                    }

                    String dateExt = DateTime.Now.ToString("yyyy_MM_dd");
                    String oldPath = currentDir + "\\WordsDatabase.db";
                    String newPath = bkpDir + "\\WordsDatabase_" + dateExt + ".db";
                    logger.Info("copying database from path: " + oldPath + " to path: " + newPath);
                    File.Copy(oldPath, newPath, true);
                    logger.Info("Database file was copied. Replace old file with synchronized one");

                    //String syncPath = syncDir + "\\WordsDatabase_sync.db";
                    String syncPath = currentDir + "\\WordsDatabase.db";
                    dbHandler.createDatabaseFileBasedOnJson(jsonStr, syncPath);
                } else {
                    logger.Info("got empty json from server");
                }
            } catch (Exception ex) {
                logger.Error("exception while getting data from server: " + ex.Message);
            }
        }

        private void ShowNewWindow(Window window) {
            //window.Owner = this; //don't work because main window is not shown
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void deleteOldLogFiles() {
            String currentDir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            String logsDir = currentDir + "\\logs";

            if (System.IO.Directory.Exists(logsDir)) {
                string[] files = Directory.GetFiles(logsDir);

                foreach (string file in files) {
                    FileInfo fi = new FileInfo(file);
                    if (fi.LastAccessTime < DateTime.Now.AddDays(-10))
                        fi.Delete();
                }
            }
        }

    }
}
