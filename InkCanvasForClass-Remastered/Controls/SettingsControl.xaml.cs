using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered.Controls
{
    public partial class SettingsControl : UserControl
    {
        public SettingsControl()
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
            DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(SettingsControl), new PropertyMetadata(null));


        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsControl), new PropertyMetadata(""));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsControl), new PropertyMetadata(""));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty HasSwitchProperty =
            DependencyProperty.Register(nameof(HasSwitcher), typeof(bool), typeof(SettingsControl), new PropertyMetadata(true));

        public bool HasSwitcher
        {
            get => (bool)GetValue(HasSwitchProperty);
            set => SetValue(HasSwitchProperty, value);
        }

        public static readonly DependencyProperty IsOnProperty =
            DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(SettingsControl),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOnChanged));

        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsControl control)
            {
                control.RaiseEvent(new RoutedEventArgs(ToggledEvent));
            }
        }

        public static readonly DependencyProperty SwitcherProperty =
            DependencyProperty.Register(nameof(Switcher), typeof(object), typeof(SettingsControl), new PropertyMetadata(null));

        public object Switcher
        {
            get => GetValue(SwitcherProperty);
            set => SetValue(SwitcherProperty, value);
        }

        public static readonly RoutedEvent ToggledEvent =
            EventManager.RegisterRoutedEvent(nameof(Toggled), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SettingsControl));

        public event RoutedEventHandler Toggled
        {
            add => AddHandler(ToggledEvent, value);
            remove => RemoveHandler(ToggledEvent, value);
        }


        public bool ShowExperimentLabel
        {
            get { return (bool)GetValue(ShowExperimentLabelProperty); }
            set { SetValue(ShowExperimentLabelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowExperimentLabel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowExperimentLabelProperty =
            DependencyProperty.Register(nameof(ShowExperimentLabel), typeof(bool), typeof(SettingsControl), new PropertyMetadata(false));


    }
}