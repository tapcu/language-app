using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LanguageApp.src {
    /// <summary>
    /// Логика взаимодействия для AddWordWindow.xaml
    /// </summary>
    public partial class AddWordWindow : Window {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private int rightMargin = 20;
        private int bottomMargin = 40;
        private DatabaseHandler dbHandler;

        public AddWordWindow(String dbName) {
            InitializeComponent();

            this.dbHandler = new DatabaseHandler(dbName);
            this.Left = SystemParameters.PrimaryScreenWidth - (this.Width + rightMargin);
            this.Top = SystemParameters.PrimaryScreenHeight - (this.Height + bottomMargin);

            txtWord.Focus();
        }

        private void OnAddButtonClick(object sender, RoutedEventArgs e) {
            ClearMessage();

            String word = txtWord.Text;
            String translation = txtTranslation.Text;

            if (word.Length == 0) {
                ShowErrorLbl("please type the new word");
                return;
            }
            if (word.Length > 30) {
                ShowErrorLbl("New word lenght should be smaller than 30");
                return;
            }
            if (translation.Length == 0) {
                ShowErrorLbl("please type the translation");
                return;
            }
            if (translation.Length > 30) {
                ShowErrorLbl("Translation lenght should be smaller than 30");
                return;
            }

            try {
                //if translation already exist in database for other word, suggest user to add index to it
                //TODO split translations list to words and check each separately
                int translationNumber = dbHandler.CheckTranslationExistence(translation);
                if (translationNumber > 0) {
                    MessageBoxResult result =
                        MessageBox.Show("Translation '" + translation + "' already occurs in database " + translationNumber + " times.\n" +
                        "Change translation to '" + translation+(translationNumber+1) + "'?" , "Notification",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes) {
                        translation = translation + (translationNumber + 1);
                    }
                    if (result == MessageBoxResult.Cancel) {
                        return;
                    }
                }

                dbHandler.AddNewWord(word, translation);
                ShowMessageLbl("Word '" + word + "' was added to database");

                Config config = Config.getInstance();
                if (config.Synchronization == Const.SYNC_ON) {
                    sendAllDataToServer(); //TODO get ID of added word, and send only this one word
                }

                txtWord.Clear();
                txtTranslation.Clear();

            } catch (Exception ex) {
                ShowErrorLbl(ex.Message);
            }
        }

        private void ClearMessage() {
            ClearMessageLbl(null, null);
        }

        private void ClearMessageLbl(object sender, RoutedEventArgs e) {
            label.Text = "";
        }

        private void ShowMessageLbl(String message) {
            label.Foreground = new SolidColorBrush(Colors.Green);
            label.Text = message;
        }

        private void ShowErrorLbl(String error) {
            label.Foreground = new SolidColorBrush(Colors.Red);
            label.Text = error;
        }

        private void sendAllDataToServer() {
            String jsonStr = dbHandler.getAllDataAsJson();
            jsonStr = "{\"words\": " + jsonStr + " }";
            logger.Info("sending data to server, data length is " + jsonStr.Length);
            if (jsonStr.Length > 0)
                Synchronizator.sendRequestAsync(jsonStr);
        }
    }
}
