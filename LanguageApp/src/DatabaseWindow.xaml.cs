using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace LanguageApp.src {
    public partial class DatabaseWindow : Window {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private DatabaseHandler dbHandler;
        DataTable dt;
        DataView dv;
        String[] filters = new string[6];

        public DatabaseWindow(String dbName) {
            InitializeComponent();
            logger.Info("show database window");

            this.dbHandler = new DatabaseHandler(dbName);

            logger.Debug("connected to database");
            dt = dbHandler.getAllDataAsDataTable();
            dv = dt.AsDataView();
            testGrid.AutoGenerateColumns = false;
            this.DataContext = dv;

            this.SizeToContent = SizeToContent.Width;
            this.Left = SystemParameters.PrimaryScreenWidth/2 - this.Width/2;
            this.Top = SystemParameters.PrimaryScreenHeight/2 -this.Height/2;
        }

        /*
         * UPDATE BUTTON, will close the balloon
         */
        private void OnUpdateButtonClick(object sender, RoutedEventArgs e) {
            dbHandler.UpdateTableFromDataTable(dt);
        }

        /*
         * DELETE BUTTON, will remove row from table (not from database)
         */
        private void OnDeleteButtonClick(object sender, RoutedEventArgs e) {
            ((DataRowView)(testGrid.SelectedItem)).Row.Delete();
        }

        private void tbId_TextChanged(object sender, RoutedEventArgs e) {
            var textBox = sender as TextBox;
            if (textBox.Text.Length == 0) {
                filters[0] = "";
            } else {
                filters[0] = String.Format("Convert(id, System.String) LIKE '{0}*'", textBox.Text);
            }
            applyFilters();
        }

        private void tbWord_TextChanged(object sender, RoutedEventArgs e) {
            var textBox = sender as TextBox;
            if (textBox.Text.Length == 0) {
                filters[1] = "";
            } else {
                filters[1] = String.Format("word LIKE '{0}*'", textBox.Text);
            }
            applyFilters();
        }

        private void tbTranslation_TextChanged(object sender, RoutedEventArgs e) {
            var textBox = sender as TextBox;
            if (textBox.Text.Length == 0) {
                filters[2] = "";
            } else {
                filters[2] = String.Format("translation LIKE '{0}*'", textBox.Text);
            }
            applyFilters();
        }

        private void tbAnswers_TextChanged(object sender, RoutedEventArgs e) {
            var textBox = sender as TextBox;
            if (textBox.Text.Length == 0) {
                filters[3] = "";
            } else {
                filters[3] = String.Format("Convert(correct_answers, System.String) LIKE '{0}*'", textBox.Text);
            }
            applyFilters();
        }

        private void tbIteration_TextChanged(object sender, RoutedEventArgs e) {
            var textBox = sender as TextBox;
            if (textBox.Text.Length == 0) {
                filters[4] = "";
            } else {
                filters[4] = String.Format("Convert(iteration, System.String) LIKE '{0}*'", textBox.Text);
            }
            applyFilters();
        }

        private void tbDate_TextChanged(object sender, RoutedEventArgs e) {
            var textBox = sender as TextBox;
            if (textBox.Text.Length == 0) {
                filters[5] = "";
            } else {
                filters[5] = String.Format("next_show_date LIKE '{0}*'", textBox.Text);
            }
            applyFilters();
        }

        private void applyFilters() {
            String rowFilter = "1=1"; //for possibility of using AND for first condition

            for(int i=0; i<filters.Length; i++) {
                if (filters[i] != null && filters[i].Length > 0) {
                    rowFilter += " AND " + filters[i];
                }
            }

            dv.RowFilter = rowFilter;
        }

        #region IdColWidth dependency property
        public static readonly DependencyProperty IdColWidthProperty =
            DependencyProperty.Register("IdColWidth", typeof(Double),
                typeof(DatabaseWindow),
                new FrameworkPropertyMetadata(20.0));
        public Double IdColWidth {
            get { return (Double)GetValue(IdColWidthProperty); }
            set { SetValue(IdColWidthProperty, value); }
        }
        #endregion
        #region WordColWidth dependency property
        public static readonly DependencyProperty WordColWidthProperty =
            DependencyProperty.Register("WordColWidth", typeof(Double),
                typeof(DatabaseWindow),
                new FrameworkPropertyMetadata(100.0));
        public Double WordColWidth {
            get { return (Double)GetValue(WordColWidthProperty); }
            set { SetValue(WordColWidthProperty, value); }
        }
        #endregion
        #region TranColWidth dependency property
        public static readonly DependencyProperty TranColWidthProperty =
            DependencyProperty.Register("TranColWidth", typeof(Double),
                typeof(DatabaseWindow),
                new FrameworkPropertyMetadata(100.0));
        public Double TranColWidth {
            get { return (Double)GetValue(TranColWidthProperty); }
            set { SetValue(TranColWidthProperty, value); }
        }
        #endregion
        #region AnswColWidth dependency property
        public static readonly DependencyProperty AnswColWidthProperty =
            DependencyProperty.Register("AnswColWidth", typeof(Double),
                typeof(DatabaseWindow),
                new FrameworkPropertyMetadata(100.0));
        public Double AnswColWidth {
            get { return (Double)GetValue(AnswColWidthProperty); }
            set { SetValue(AnswColWidthProperty, value); }
        }
        #endregion
        #region IterColWidth dependency property
        public static readonly DependencyProperty IterColWidthProperty =
            DependencyProperty.Register("IterColWidth", typeof(Double),
                typeof(DatabaseWindow),
                new FrameworkPropertyMetadata(100.0));
        public Double IterColWidth {
            get { return (Double)GetValue(IterColWidthProperty); }
            set { SetValue(IterColWidthProperty, value); }
        }
        #endregion
        #region DataColWidth dependency property
        public static readonly DependencyProperty DataColWidthProperty =
            DependencyProperty.Register("DataColWidth", typeof(Double),
                typeof(DatabaseWindow),
                new FrameworkPropertyMetadata(100.0));
        public Double DataColWidth {
            get { return (Double)GetValue(DataColWidthProperty); }
            set { SetValue(DataColWidthProperty, value); }
        }
        #endregion

        private void testGrid_SizeChanged(object sender, EventArgs e) {
            IdColWidth = testGrid.Columns[0].ActualWidth - 4;
            WordColWidth = testGrid.Columns[1].ActualWidth - 4;
            TranColWidth = testGrid.Columns[2].ActualWidth - 4;
            AnswColWidth = testGrid.Columns[3].ActualWidth - 4;
            IterColWidth = testGrid.Columns[4].ActualWidth - 4;
            DataColWidth = testGrid.Columns[5].ActualWidth - 4;
        }
    }
}
