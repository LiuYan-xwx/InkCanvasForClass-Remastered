using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered.Controls
{
    /// <summary>
    /// FloatingBarButton.xaml 的交互逻辑
    /// </summary>
    public partial class FloatingBarButton : UserControl
    {
        public FloatingBarButton()
        {
            InitializeComponent();
            PART_Button.Click += (s, e) => RaiseEvent(new RoutedEventArgs(ClickEvent));
        }

        public static readonly DependencyProperty PathDataProperty =
            DependencyProperty.Register(
                nameof(PathData),
                typeof(Geometry),
                typeof(FloatingBarButton),
                new PropertyMetadata(null));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(FloatingBarButton),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(FloatingBarButton),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(FloatingBarButton),
                new PropertyMetadata(null));

        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Click),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(FloatingBarButton));

        public Geometry PathData
        {
            get => (Geometry)GetValue(PathDataProperty);
            set => SetValue(PathDataProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }
    }
}
