using System;
using System.Globalization;
using System.Windows.Data;

namespace InkCanvasForClass_Remastered.Converters
{
    public class BoolToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled && isEnabled)
            {
                return 0.8;
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}