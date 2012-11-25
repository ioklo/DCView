using System;
using System.Windows.Data;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DCView
{
    public class BoolToColorConverter : IValueConverter
    {
        object phoneAccentBrush;
        object phoneDisabledBrush;

        public BoolToColorConverter()
        {
            phoneAccentBrush = Application.Current.Resources["PhoneAccentBrush"];
            phoneDisabledBrush = Application.Current.Resources["PhoneDisabledBrush"];
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = (bool)value;

            if (b)
                return phoneAccentBrush;
            else
                return phoneDisabledBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
