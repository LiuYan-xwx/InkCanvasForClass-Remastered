using System.Windows;

namespace InkCanvasForClass_Remastered
{
    /// <summary>
    /// Interaction logic for RestoreHiddenSlidesWindow.xaml
    /// </summary>
    public partial class YesOrNoNotificationWindow : Window
    {
        private readonly Action _yesAction;
        private readonly Action _noAction;
        private readonly Action _windowClose;

        public YesOrNoNotificationWindow(string text, Action yesAction = null, Action noAction = null, Action windowClose = null)
        {
            _yesAction = yesAction;
            _noAction = noAction;
            _windowClose = windowClose;
            InitializeComponent();
            Label.Text = text;
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            if (_yesAction == null)
            {
                Close();
                return;
            }

            _yesAction.Invoke();
            Close();

        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            if (_noAction == null)
            {
                Close();
                return;
            }

            _noAction.Invoke();
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_windowClose == null)
            {
                return;
            }
            _windowClose.Invoke();
        }
    }
}