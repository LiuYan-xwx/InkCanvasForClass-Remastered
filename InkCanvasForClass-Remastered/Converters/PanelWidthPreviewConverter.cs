using System.Globalization;
using System.Windows.Data;

namespace InkCanvasForClass_Remastered.Converters
{
    public class PanelWidthPreviewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int width)
            {
                return width * 0.25;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
