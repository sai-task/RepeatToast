using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Simple_Notification
{
    /* MainPageのリストビュー上で、長すぎるタイトルを省略して表示する。 */
    class TitleToStringConverter : IValueConverter
    {
        private const int _maxTitleLength = 15;
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || !(value is string))
                return null;
            var s = value as string;
            if (s.Length > _maxTitleLength)
                s = s.Substring(0, 15) + "...";
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
