using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Simple_Notification
{
    public class ToastToColumnBackgroundColorConverter : IValueConverter
    {
        private const string _enabledAndSelectedColor = "Red";
        private const string _enabledAndNotSelectedColor = "Orange";
        private const string _disabledAndSelectedColor = "Black";
        private const string _disabledAndNotSelectedColor = "Gray";
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || !(value is MyToastContent))
                return null;
            var toast = value as MyToastContent;
            var enabled = toast.IsEnabled;
            var selected = toast.IsSelected;
            string color;
            if (enabled && selected)
                color = _enabledAndSelectedColor;
            else if (enabled && !selected)
                color = _enabledAndNotSelectedColor;
            else if (!enabled && selected)
                color = _disabledAndSelectedColor;
            else
                color = _disabledAndNotSelectedColor;
            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
