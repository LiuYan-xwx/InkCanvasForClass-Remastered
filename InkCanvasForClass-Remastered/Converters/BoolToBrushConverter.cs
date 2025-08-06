using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        public SolidColorBrush EnabledBrush { get; set; } = new SolidColorBrush(Color.FromArgb(255, 24, 24, 27));
        public SolidColorBrush DisabledBrush { get; set; } = new SolidColorBrush(Color.FromArgb(127, 24, 24, 27));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? EnabledBrush : DisabledBrush;
            }
            return EnabledBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}