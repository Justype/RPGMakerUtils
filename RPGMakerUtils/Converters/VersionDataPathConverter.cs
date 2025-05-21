using RPGMakerUtils.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RPGMakerUtils.Converters
{
    internal class VersionDataPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RPGMakerVersion version)
            {
                switch (version)
                {
                    case RPGMakerVersion.MV:
                        return "www/data";
                    case RPGMakerVersion.MZ:
                        return "data";
                    default:
                        break;
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
