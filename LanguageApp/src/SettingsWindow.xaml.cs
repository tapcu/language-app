using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LanguageApp.src
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        Config config = null;
        private int rightMargin = 20;
        private int bottomMargin = 40;

        private int initialShowInterval = 0;
        private int initialDaysInterval = 0;
        private String initialDatabasePath = null;
        
        #region ShowInterval dependency property
        public static readonly DependencyProperty ShowIntervalProperty =
            DependencyProperty.Register("ShowInterval" ,typeof(String), typeof(SettingsWindow));
        public Double ShowInterval {
            get { return (Double)GetValue(ShowIntervalProperty); }
            set { SetValue(ShowIntervalProperty, value); }
        }
        #endregion
        #region DaysInterval dependency property
        public static readonly DependencyProperty DaysIntervalProperty =
            DependencyProperty.Register("DaysInterval", typeof(Int32), typeof(SettingsWindow));
        public Int32 DaysInterval {
            get { return (Int32)GetValue(DaysIntervalProperty); }
            set { SetValue(DaysIntervalProperty, value); }
        }
        #endregion

        public SettingsWindow()
        {
            logger.Info("Showing settings window");
            InitializeComponent();
            logger.Info("Getting current config values");
            config = Config.getInstance();
            initialShowInterval = config.ShowInterval;
            initialDaysInterval = config.DaysInterval;
            initialDatabasePath = config.DatabasePath;

            ShowIntervalValue = initialShowInterval;
            DaysInterval = initialDaysInterval;
            dbNameFld.Text = initialDatabasePath;

            this.Left = SystemParameters.PrimaryScreenWidth - (this.Width + rightMargin);
            this.Top = SystemParameters.PrimaryScreenHeight - (this.Height + bottomMargin);
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e) {
            logger.Trace("Save button clicked");

            Boolean settingsChanged = false;
            //check if SHOW INTERVAL value should be updated in config file
            if (txtInterval.Text != null && !(txtInterval.Text.Length == 0)) {
                TimeSpan parsedVal;
                TimeSpan.TryParse(txtInterval.Text, out parsedVal);
                if (parsedVal.TotalSeconds != initialShowInterval) {
                    settingsChanged = true;
                    logger.Debug("update config show interval to " + parsedVal.TotalSeconds);
                    config.ShowInterval = (int)parsedVal.TotalSeconds;
                }
            }
            //check if DAYS INTERVAL value should be updated in config file
            if (daysInterval.Text != null && !(daysInterval.Text.Length == 0)) {
                Int32 parsedDayInterval = 0;
                bool tryParseResult = Int32.TryParse(daysInterval.Text, out parsedDayInterval);
                if (tryParseResult == false) { //text parsing goes wrong
                    logger.Error("Days intervals field parsing goes worng! The text was: " + daysInterval.Text);
                    MessageBoxResult result = MessageBox.Show("Cannot parse value " + daysInterval.Text + " as number!",
                                               "Error",
                                               MessageBoxButton.OK,
                                               MessageBoxImage.Error);
                    return;
                }
                if (parsedDayInterval != 0 && parsedDayInterval != initialDaysInterval) {
                    settingsChanged = true;
                    logger.Debug("update config days interval to " + parsedDayInterval);
                    config.DaysInterval = parsedDayInterval;
                }
            }
            //check if DATABASE NAME value should be updated in config file
            if (dbNameFld.Text != null && !(dbNameFld.Text.Length==0)) {
                if (!(dbNameFld.Text.Equals(initialDatabasePath))) {
                    settingsChanged = true;
                    logger.Debug("update config database name to " + dbNameFld.Text);
                    config.DatabasePath = dbNameFld.Text;
                }
            }
            if(settingsChanged)
                config.saveToFile();
            this.Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private double _showIntervalValue = 0;
        public double ShowIntervalValue {
            get { return _showIntervalValue; }
            set {
                _showIntervalValue = value;
                TimeSpan t = TimeSpan.FromSeconds(value);
                txtInterval.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
            }
        }

        private void CmdUp_Click(object sender, RoutedEventArgs e) {
            ShowIntervalValue++;
        }

        private void CmdDown_Click(object sender, RoutedEventArgs e) {
            ShowIntervalValue--;
        }

        private void Interval_TextChanged(object sender, TextChangedEventArgs e) {
            if (txtInterval == null) {
                return;
            }

            TimeSpan parsedVal;
            if (TimeSpan.TryParse(txtInterval.Text, out parsedVal) && parsedVal.TotalSeconds > 0 && parsedVal.TotalSeconds<=(3*60*60)) {
                ShowIntervalValue = parsedVal.TotalSeconds;
            } else {
                TimeSpan t = TimeSpan.FromSeconds(ShowIntervalValue);
                txtInterval.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
                if(parsedVal.TotalSeconds > (3 * 60 * 60)) {
                    MessageBox.Show("Show interval can not be more than 3 hours");
                }
            }
        }

        private void browseBtn_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".db";
            dlg.Filter = "Database files (.db) | *.db";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true) {
                // Open document
                string filePath = dlg.FileName;
                string fileName = dlg.SafeFileName;
                dbNameFld.Text = filePath;
            }
        }
    }
}
