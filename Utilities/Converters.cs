using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NowReadable.Utilities.Converters
{
    /// <summary>
    /// The value can be true, false or null. Its null to allow for a case where neither of the two options should be visible.
    /// An example is when you know you're about to authenticate, and the page will update, but you don't want to display the old text.
    /// </summary>
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var convertedValue = (bool?)value;
            if (convertedValue != null && (bool)convertedValue)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// The value can be true, false or null. Its null to allow for a case where neither of the two options should be visible.
    /// An example is when you know you're about to authenticate, and the page will update, but you don't want to display the old text.
    /// </summary>
    public class NegativeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var convertedValue = (bool?)value;
            if (convertedValue == null || !(bool)convertedValue)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
