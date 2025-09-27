using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InkCanvasForClass_Remastered.Converters
{
    public class PPTButtonPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int position)
            {
                // Convert position to Thickness for margin adjustment
                return new Thickness(0, -position, 0, 0);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}