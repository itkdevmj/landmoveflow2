using DevExpress.CodeParser;
using DevExpress.Diagram.Core.Native;
using DevExpress.Xpf.CodeView;
using DevExpress.Xpf.Diagram;
using DevExpress.Xpo;
using DevExpress.XtraPrinting.XamlExport;
using DevExpress.XtraScheduler.Drawing;
using DevExpress.XtraSpreadsheet.DocumentFormats.Xlsb;
using DevExpress.XtraSpreadsheet.Model;
using LMFS.Models;
using LMFS.Services;
using LMFS.ViewModels.Pages;
using LMFS.Views.Pages;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Media;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace LMFS.Engine;

public class ZoomPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double d = System.Convert.ToDouble(value);
        return $"{d * 100:F0}%"; // 값 뒤에 % 직접 추가
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string str = value.ToString().Replace("%", "");
        double percent = double.Parse(str);
        return percent / 100.0;
    }
}