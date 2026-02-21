using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered.Controls
{
    /// <summary>
    /// AutoFoldAppToggle.xaml 的交互逻辑
    /// </summary>
    public partial class AutoFoldAppToggle : UserControl
    {
        public AutoFoldAppToggle()
        {
            InitializeComponent();
        }



        public ImageSource IconSource
        {
            get { return (ImageSource)GetValue(IconSourceProperty); }
            set { SetValue(IconSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IconSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(AutoFoldAppToggle), new PropertyMetadata(null));



        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(AutoFoldAppToggle), new PropertyMetadata(null));



        public bool IsOn
        {
            get { return (bool)GetValue(IsOnProperty); }
            set { SetValue(IsOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsOnProperty =
            DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(AutoFoldAppToggle), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly RoutedEvent ToggledEvent =
            EventManager.RegisterRoutedEvent(nameof(Toggled), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AutoFoldAppToggle));

        public event RoutedEventHandler Toggled
        {
            add => AddHandler(ToggledEvent, value);
            remove => RemoveHandler(ToggledEvent, value);
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ToggledEvent));
        }
    }
}
