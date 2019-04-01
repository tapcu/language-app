using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;

namespace LanguageApp.src
{
    /// <summary>
    /// Логика взаимодействия для ErrorBaloon.xaml
    /// </summary>
    public partial class ErrorBaloon : UserControl
    {
        public ErrorBaloon()
        {
            InitializeComponent();
        }

        /*
         * OK BUTTON, will close the balloon
         */
        private void OnOkButtonClick(object sender, RoutedEventArgs e) {
            closeThisBalloon();
        }

        private void closeThisBalloon() {
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            if (taskbarIcon != null && taskbarIcon.CustomBalloon != null)
                taskbarIcon.CloseBalloon();
        }
    }
}
