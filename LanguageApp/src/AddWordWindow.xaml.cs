using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace LanguageApp.src {
    /// <summary>
    /// Логика взаимодействия для AddWordWindow.xaml
    /// </summary>
    public partial class AddWordWindow : Window {

        private int rightMargin = 20;
        private int bottomMargin = 40;
        private DatabaseHandler dbHandler;

        public AddWordWindow(String dbName) {
            InitializeComponent();

            this.dbHandler = new DatabaseHandler(dbName);
            this.Left = SystemParameters.PrimaryScreenWidth - (this.Width + rightMargin);
            this.Top = SystemParameters.PrimaryScreenHeight - (this.Height + bottomMargin);
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
                dbHandler.AddNewWord(word, translation);
                ShowMessageLbl("Word '" + word +"' was added to database");
            } catch (Exception ex) {
                ShowErrorLbl(ex.Message);
            }

            txtWord.Clear();
            txtTranslation.Clear();
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
    }
}
