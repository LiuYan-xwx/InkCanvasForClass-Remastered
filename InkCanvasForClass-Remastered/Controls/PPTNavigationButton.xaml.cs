using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InkCanvasForClass_Remastered.Controls
{
    public partial class PPTNavigationButton : UserControl
    {
        private object? _lastMouseDownSender;

        public PPTNavigationButton()
        {
            InitializeComponent();
        }

        public static readonly RoutedEvent PreviousClickEvent = 
            EventManager.RegisterRoutedEvent(nameof(PreviousClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PPTNavigationButton));

        public event RoutedEventHandler PreviousClick
        {
            add => AddHandler(PreviousClickEvent, value);
            remove => RemoveHandler(PreviousClickEvent, value);
        }

        public static readonly RoutedEvent NextClickEvent = 
            EventManager.RegisterRoutedEvent(nameof(NextClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PPTNavigationButton));

        public event RoutedEventHandler NextClick
        {
            add => AddHandler(NextClickEvent, value);
            remove => RemoveHandler(NextClickEvent, value);
        }

        public static readonly RoutedEvent PageClickEvent = 
            EventManager.RegisterRoutedEvent(nameof(PageClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PPTNavigationButton));

        public event RoutedEventHandler PageClick
        {
            add => AddHandler(PageClickEvent, value);
            remove => RemoveHandler(PageClickEvent, value);
        }

        #region Previous Button Events
        private void PreviousButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _lastMouseDownSender = sender;
            PreviousButtonFeedback.Opacity = 0.15;
        }

        private void PreviousButton_MouseLeave(object sender, MouseEventArgs e)
        {
            _lastMouseDownSender = null;
            PreviousButtonFeedback.Opacity = 0;
        }

        private void PreviousButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_lastMouseDownSender != sender) return;
            
            PreviousButtonFeedback.Opacity = 0;
            RaiseEvent(new RoutedEventArgs(PreviousClickEvent));
        }
        #endregion

        #region Next Button Events
        private void NextButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _lastMouseDownSender = sender;
            NextButtonFeedback.Opacity = 0.15;
        }

        private void NextButton_MouseLeave(object sender, MouseEventArgs e)
        {
            _lastMouseDownSender = null;
            NextButtonFeedback.Opacity = 0;
        }

        private void NextButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_lastMouseDownSender != sender) return;
            
            NextButtonFeedback.Opacity = 0;
            RaiseEvent(new RoutedEventArgs(NextClickEvent));
        }
        #endregion

        #region Page Button Events
        private void PageButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _lastMouseDownSender = sender;
            PageButtonFeedback.Opacity = 0.15;
        }

        private void PageButton_MouseLeave(object sender, MouseEventArgs e)
        {
            _lastMouseDownSender = null;
            PageButtonFeedback.Opacity = 0;
        }

        private void PageButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_lastMouseDownSender != sender) return;
            
            PageButtonFeedback.Opacity = 0;
            RaiseEvent(new RoutedEventArgs(PageClickEvent));
        }
        #endregion
    }
}