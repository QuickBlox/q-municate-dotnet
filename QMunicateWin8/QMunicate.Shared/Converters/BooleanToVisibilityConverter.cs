using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace QMunicate.Converters
{
    /// <summary>
    /// Convert between Boolean and visibility
    /// </summary>
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        #region Properties

        public Boolean Invert { get; set; }

        #endregion

        #region IValueConverter Members

        /// <summary>
        /// Convert Boolean or Nullable to Visibility
        /// </summary>
        /// <param name="value">bool or Nullable</param>
        /// <param name="targetType">Visibility</param>
        /// <param name="parameter">null</param>
        /// <param name="language">null</param>
        /// <returns>Visible or Collapsed</returns>
        public object Convert(Object value, Type targetType, Object parameter, String language)
        {
            var bValue = false;
            if (value is Boolean)
                bValue = (Boolean)value;
            if (Invert)
                bValue = !bValue;
            return (bValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Convert Visibility to Boolean
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public object ConvertBack(Object value, Type targetType, Object parameter, String language)
        {
            return value is Visibility && (Visibility)value == Visibility.Visible;
        }

        #endregion
    }
}

