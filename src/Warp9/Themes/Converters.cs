using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Warp9.Themes
{
    public class RadioBoolToIntConverter<T> : IValueConverter
        where T: Enum
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is T e &&
                parameter is string s)
            {
                return e.CompareTo(Enum.Parse(typeof(T), s, true)) == 0;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string s)
                return Enum.Parse(typeof(T), s, true);

            return default(T);
        }
    }
}
