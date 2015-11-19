using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace QMunicate.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public Boolean Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var isVisible = value != null;

            if (Invert)
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
