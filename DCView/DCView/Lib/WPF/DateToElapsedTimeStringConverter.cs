using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace DCView
{
    public class DateToElapsedTimeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {   
            DateTime date = (DateTime)value;

            string dateString = string.Empty;
            TimeSpan elapsed = DateTime.Now.Subtract(date);

            if (elapsed < new TimeSpan(1, 0, 0))
                dateString = string.Format("{0}분 전", elapsed.Minutes);
            else if (elapsed < new TimeSpan(1, 0, 0, 0))
                dateString = string.Format("{0}시간 전", elapsed.Hours);
            else
                dateString = date.ToString("MM-dd");

            return dateString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
