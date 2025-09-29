using System.Windows;
using System.Windows.Controls;

namespace InkCanvasForClass_Remastered.Controls
{
    public partial class PPTNavigationButton : UserControl
    {
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

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(PreviousClickEvent));
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(NextClickEvent));
        }

        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(PageClickEvent));
        }
    }
}