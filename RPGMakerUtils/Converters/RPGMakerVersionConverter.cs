using RPGMakerUtils.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Linq;

namespace RPGMakerUtils.Converters
{
    public class RPGMakerVersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is RPGMakerVersion version)
            {
                string name = version.ToString();
                if (name == "Unknown")
                    return "未知";
                if (Regex.IsMatch(name, @"^V\d+"))
                    return name.Substring(1);
                return version.ToString();
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string versionString)
            {
                if (Enum.TryParse<RPGMakerVersion>(versionString, out var version))
                {
                    return version;
                }
            }
            return null;
        }
    }
}
