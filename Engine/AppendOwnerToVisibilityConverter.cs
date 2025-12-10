using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LMFS.Engine
{
    public class AppendOwnerToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
                              object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Visibility.Collapsed;

            var appendId = values[0]?.ToString();
            var currentUserId = values[1]?.ToString();

            if (!string.IsNullOrEmpty(appendId) &&
                !string.IsNullOrEmpty(currentUserId) &&
                appendId == currentUserId)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes,
                                    object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}