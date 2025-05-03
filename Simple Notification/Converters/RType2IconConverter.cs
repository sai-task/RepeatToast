using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Simple_Notification.Converters
{
    class RType2IconConverter : IValueConverter
    {
        private const int _maxTitleLength = 15;
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // 未完成
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
