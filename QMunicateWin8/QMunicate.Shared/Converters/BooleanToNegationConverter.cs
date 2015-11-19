using System;
using Windows.UI.Xaml.Data;

namespace QMunicate.Converters
{
    /// <summary>
    /// BooleanToNegationConverter class.
    /// </summary>
    public sealed class BooleanToNegationConverter : IValueConverter
    {
        public object Convert(Object value, Type targetType, Object parameter, String language)
        {
            var bValue = (Boolean)value;
            return !bValue;
        }

        public object ConvertBack(Object value, Type targetType, Object parameter, String language)
        {
            throw new NotImplementedException();
        }
    }
}
