using System.Globalization;
using System.Windows.Data;

namespace InkCanvasForClass_Remastered.Converters
{
    public class OffsetPreviewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int offset)
            {
                return offset * 0.203;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
