using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using LanguageApp.src;

namespace LanguageApp {
    /// <summary>
    /// Interaction logic for FancyBalloon.xaml
    /// </summary>
    public partial class FancyBalloon : UserControl {
        #region ClickCount dependency property
        /// The number of clicks on the button.
        public static readonly DependencyProperty ClickCountProperty =
            DependencyProperty.Register("ClickCount",
                typeof(int),
                typeof(FancyBalloon),
                new FrameworkPropertyMetadata(0));
        public int ClickCount {
            get { return (int)GetValue(ClickCountProperty); }
            set { SetValue(ClickCountProperty, value); }
        }
        #endregion
        #region MainWord dependency property
        /// The number of clicks on the button.
        public static readonly DependencyProperty MainWordProperty =
            DependencyProperty.Register("MainWord",
                typeof(String),
                typeof(FancyBalloon),
                new FrameworkPropertyMetadata("random word"));
        public String MainWord {
            get { return (String)GetValue(MainWordProperty); }
            set { SetValue(MainWordProperty, value); }
        }
        #endregion
        #region Translation dependency property
        public static readonly DependencyProperty TranslationProperty =
            DependencyProperty.Register("Translation",
                typeof(string),
                typeof(FancyBalloon),
                new FrameworkPropertyMetadata(""));
        public string Translation {
            get { return (string)GetValue(TranslationProperty); }
            set { SetValue(TranslationProperty, value); }
        }

        #endregion

        private DispatcherTimer mainTimer;
        private DictionaryItem wordsPair;
        private bool closeWhenLeave = false;
        private bool isClosing = false;
        private bool waitingForAnswer = true;
        private Action<DictionaryItem, string> processAnswer;

        public FancyBalloon(DispatcherTimer timer, DictionaryItem newWordsPair = null) {
            InitializeComponent();

            this.mainTimer = timer;
            this.wordsPair = newWordsPair;

            showMainWord();
        }

        public FancyBalloon(DispatcherTimer timer, DictionaryItem wordsPair = null, Action<DictionaryItem, string> callback = null) : this(timer, wordsPair) {
            this.processAnswer = callback;
        }

        private void showMainWord() {
            MainWord = getNormalizedWordPair(wordsPair).Item1;
        }

        private void showTranslation() {
            Translation = getNormalizedWordPair(wordsPair).Item2;
        }

        private Tuple<string, string> getNormalizedWordPair(DictionaryItem item) {
            String word = "word not specified", translation = "translation not specified";

            if (item != null)
                if (item.Iteration == 0) { //(easy) translate foreign word to mother language
                    if (item.Word != null) word = item.Word;
                    if (item.Translation != null) translation = item.Translation;
                } else { //(harder) translate from own language to foreign one
                    if (item.Word != null) word = item.Translation;
                    if (item.Translation != null) translation = item.Word;
                }

            return new Tuple<string, string>(word, translation);
        }

        /*
         * CORRECT ANSWER BUTTON
         * The same button is used to give an answer, and to confirm it, that's why
         * it's using "waitingForAnswer" flag
         */
        private void OnFirstButtonClick(object sender, RoutedEventArgs e) {
            if (waitingForAnswer) {
                //The answer is given, need to confirm it. So button purpose is changes
                waitingForAnswer = false;
                showTranslation(); //show the correct answer, so user could check if he was right
                FirstButton.Content = "Верно"; //use the same buttons for different purpose
                SecondButton.Content = "Неверно";
            } else {
                //it answer was already given, and this button was pressed again
                //it means that answer was correct
                processAnswer(wordsPair, "correct");
                closeThisBalloon();
            }
        }

        /*
         * WRONG ANSWER BUTTON
         * The same button is used to indicate that user doesn't know the answer
         * or to indicate that user made a mistake first time, and his answer was incorrect
         */
        private void OnSecondButtonClick(object sender, RoutedEventArgs e) {
            showTranslation(); //anyway show the correct answer

            processAnswer(wordsPair, "wrong");

            FirstButton.Visibility = Visibility.Hidden; //close both buttons
            SecondButton.Visibility = Visibility.Hidden;
            OkButton.Visibility = Visibility.Visible; //show button that will close the window
            closeWhenLeave = true; //if mouse leaves the window, it will be closed
        }

        /*
         * OK BUTTON, will close the balloon
         */
        private void OnOkButtonClick(object sender, RoutedEventArgs e) {
            closeThisBalloon();
        }

        /// <summary>
        /// Resolves the <see cref="TaskbarIcon"/> that displayed
        /// the balloon and requests a close action.
        /// </summary>
        private void imgClose_MouseDown(object sender, MouseButtonEventArgs e) {
            //the tray icon assigned this attached property to simplify access
            closeWhenLeave = false;
            closeThisBalloon();
        }

        /// <summary>
        /// If the users hovers over the balloon, we don't close it (main timer is stopped).
        /// </summary>
        private void grid_MouseEnter(object sender, MouseEventArgs e) {
            this.mainTimer.Stop();
            //Console.WriteLine("main timer stoped!");

            //if we're already running the fade-out animation, do not interrupt
            if (isClosing) return;

            //the tray icon assigned this attached property to simplify access
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.ResetBalloonCloseTimer();
        }

        /// <summary>
        /// When users leaves the ballon, we close the balloon, if any button was pressed
        /// </summary>
        private void grid_MouseLeave(object sender, MouseEventArgs e) {
            this.mainTimer.Start();
            //Console.WriteLine("main timer started!");
            if (closeWhenLeave)
                new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal,
                    (s, o) =>  //timer click function
                    {
                        closeThisBalloon();
                        (s as DispatcherTimer).Stop(); //timer work only once
                    },
                    Application.Current.Dispatcher);

        }

        private void closeThisBalloon() {
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            if (taskbarIcon != null && taskbarIcon.CustomBalloon != null)
                taskbarIcon.CloseBalloon();
        }
    }
}
