using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using InkCanvasForClass_Remastered.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace InkCanvasForClass_Remastered.Controls
{
    public partial class PPTNavigationPanel : UserControl
    {
        public PPTNavigationPanel()
        {
            InitializeComponent();
        }

        public static readonly RoutedEvent PreviousClickEvent = 
            EventManager.RegisterRoutedEvent(nameof(PreviousClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PPTNavigationPanel));

        public event RoutedEventHandler PreviousClick
        {
            add => AddHandler(PreviousClickEvent, value);
            remove => RemoveHandler(PreviousClickEvent, value);
        }

        public static readonly RoutedEvent NextClickEvent = 
            EventManager.RegisterRoutedEvent(nameof(NextClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PPTNavigationPanel));



        public double PanelWidth
        {
            get { return (double)GetValue(PanelWidthProperty); }
            set { SetValue(PanelWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PanelWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PanelWidthProperty =
            DependencyProperty.Register("PanelWidth", typeof(double), typeof(PPTNavigationPanel), new PropertyMetadata(60.0));



        public event RoutedEventHandler NextClick
        {
            add => AddHandler(NextClickEvent, value);
            remove => RemoveHandler(NextClickEvent, value);
        }

        public static readonly RoutedEvent PageClickEvent = 
            EventManager.RegisterRoutedEvent(nameof(PageClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PPTNavigationPanel));

        public event RoutedEventHandler PageClick
        {
            add => AddHandler(PageClickEvent, value);
            remove => RemoveHandler(PageClickEvent, value);
        }

        private void PPTButton_PreviousClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(PreviousClickEvent));
        }

        private void PPTButton_NextClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(NextClickEvent));
        }

        private void PPTButton_PageClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(PageClickEvent));
        }
    }
}