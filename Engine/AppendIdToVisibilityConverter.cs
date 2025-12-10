using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LMFS.Engine
{
    // uploadid 가 비어있지 않을 때만 Visible
    public class AppendIdToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
                              object parameter, CultureInfo culture)
        {
            // values[0] : IsNewItemRow, values[1] : uploadid
            bool isNewRow = values.Length > 0 && values[0] is bool b && b;
            string appendId = values.Length > 1 ? values[1]?.ToString() : null;

            // 새 행이면 무조건 삭제 버튼 숨김
            if (isNewRow)
                return Visibility.Collapsed;

            // 기존 행이고 uploadid 에 문자열이 있을 때만 삭제 버튼 표시
            if (!string.IsNullOrWhiteSpace(appendId))
                return Visibility.Visible;

            // 그 외(기존 행 + uploadid 없음) → 버튼 없음
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes,
                                    object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}