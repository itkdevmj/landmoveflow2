using System;
using System.Globalization;
using System.Windows.Data;

namespace LMFS.Engine
{
    public class MultiFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatType = parameter as string;
            if (formatType == "Date")
            {
                string dateStr = value as string;
                if (!string.IsNullOrEmpty(dateStr) && dateStr.Length == 8)
                {
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                    {
                        return dt.ToString("yyyy-MM-dd");
                    }
                }
            }
            else if (formatType == "Number")
            {
                if (!decimal.TryParse(value?.ToString(), out decimal number))
                {
                    return number.ToString("#,##0");
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 필요시 구현
            return value;
        }
    }
}