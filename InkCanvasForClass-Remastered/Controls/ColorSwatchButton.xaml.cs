using System.Windows;
using System.Windows.Controls;

namespace InkCanvasForClass_Remastered.Controls
{
    public partial class ColorSwatchButton : Button
    {
        public ColorSwatchButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(ColorSwatchButton),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsHighlighterProperty =
            DependencyProperty.Register(
                nameof(IsHighlighter),
                typeof(bool),
                typeof(ColorSwatchButton),
                new PropertyMetadata(false));

        public static readonly DependencyProperty UseDarkCheckMarkProperty =
            DependencyProperty.Register(
                nameof(UseDarkCheckMark),
                typeof(bool),
                typeof(ColorSwatchButton),
                new PropertyMetadata(false));

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public bool IsHighlighter
        {
            get => (bool)GetValue(IsHighlighterProperty);
            set => SetValue(IsHighlighterProperty, value);
        }

        public bool UseDarkCheckMark
        {
            get => (bool)GetValue(UseDarkCheckMarkProperty);
            set => SetValue(UseDarkCheckMarkProperty, value);
        }
    }
}
