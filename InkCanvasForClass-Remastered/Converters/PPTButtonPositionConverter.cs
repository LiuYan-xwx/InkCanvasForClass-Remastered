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
                // Based on original logic: position affects vertical offset
                // Left side: 3,0,0,0 baseline + vertical offset
                // Right side: 0,0,3,0 baseline + vertical offset
                if (parameter?.ToString() == "Right")
                {
                    return new Thickness(0, -position, 3, 0);
                }
                else
                {
                    return new Thickness(3, -position, 0, 0);
                }
            }
            
            // Default margins
            if (parameter?.ToString() == "Right")
            {
                return new Thickness(0, 0, 3, 0);
            }
            return new Thickness(3, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}