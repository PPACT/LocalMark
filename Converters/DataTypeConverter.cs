using System.Globalization;
using System.Windows.Data;

namespace LocalMark.Converters;

public class DataTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int dataType)
        {
            return dataType == 0 ? "文本" : "图片";
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
