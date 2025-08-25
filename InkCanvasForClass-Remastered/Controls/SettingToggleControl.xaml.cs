using System.Windows;
using System.Windows.Controls;

namespace InkCanvasForClass_Remastered.Controls
{
    public partial class SettingsToggleControl : UserControl
    {
        public SettingsToggleControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsToggleControl), new PropertyMetadata(""));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty IsOnProperty =
            DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(SettingsToggleControl), new PropertyMetadata(false));

        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        public static readonly RoutedEvent ToggledEvent =
            EventManager.RegisterRoutedEvent(nameof(Toggled), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SettingsToggleControl));

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