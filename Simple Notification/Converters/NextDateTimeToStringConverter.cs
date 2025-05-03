using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Simple_Notification
{
    /* MainPageのリストビュー上に、次に送られるトーストの時刻を表示する */
    class NextDateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || !(value is DateTimeOffset?))
                return null;
            var next = ((DateTimeOffset)value).LocalDateTime;
            var now = DateTime.Now;
            var sb = new StringBuilder("");
            if (next.Year != now.Year)
                sb.Append("yyyy/");
            if (next.Date != now.Date)
                sb.Append("MM/dd ");
            sb.Append("HH:mm");
            var s = sb.ToString();
            return "Next " + next.ToString(s);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
