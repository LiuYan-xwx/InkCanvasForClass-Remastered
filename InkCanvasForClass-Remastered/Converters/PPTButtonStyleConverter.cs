using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered.Converters
{
    public class PPTButtonStyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || 
                !(values[0] is bool useHalfOpacity) || 
                !(values[1] is bool useBlackBackground))
            {
                return DependencyProperty.UnsetValue;
            }

            if (parameter?.ToString() == "Opacity")
            {
                return useHalfOpacity ? 0.5 : 1.0;
            }
            else if (parameter?.ToString() == "Background")
            {
                return useBlackBackground 
                    ? new SolidColorBrush(Color.FromRgb(24, 24, 27))
                    : new SolidColorBrush(Color.FromRgb(244, 244, 245));
            }
            else if (parameter?.ToString() == "Foreground")
            {
                return useBlackBackground 
                    ? new SolidColorBrush(Colors.White)
                    : new SolidColorBrush(Color.FromRgb(39, 39, 42));
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}