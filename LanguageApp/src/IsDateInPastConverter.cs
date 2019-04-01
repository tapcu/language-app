using System;
using System.Globalization;
using System.Windows.Data;

namespace LanguageApp.src {
    public class IsDateInPastConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null)
                return "False";
            String dateStr = value.ToString();
            //if (dateStr.Equals("#FF000000")) return "True";
            DateTime dateVal;
            if (!DateTime.TryParse(dateStr, out dateVal)) { //try to convert date to str
                return "False";
            }
            if (dateVal <= DateTime.Now) {
                return "True";
            }
            return "False";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new InvalidOperationException("IsDateInPastConverter can only be used OneWay.");
        }
    }
}
