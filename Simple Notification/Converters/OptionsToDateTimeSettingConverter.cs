using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Simple_Notification
{
    /* MainPageのリストビューに、毎日何時、毎月何日、などの情報を表示する。 */
    class OptionsToDateTimeSettingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || !(value is ToastOptions))
                return null;
            var options = value as ToastOptions;
            var repeat = options.RType;
            var firstDt = options.FirstDateTimeOffset;
            var now = DateTimeOffset.Now.ToOffset(firstDt.Offset);
            var sunday = options.Sunday;
            var monday = options.Monday;
            var tuesday = options.Tuesday;
            var wednesday = options.Wednesday;
            var thursday = options.Thursday;
            var friday = options.Friday;
            var saturday = options.Saturday;
            if (repeat == RepeatType.once)
            {
                if (firstDt.Year != now.Year)
                    return firstDt.ToString("yyyy年M月d日 HH:mm");
                else
                    return firstDt.ToString("M月d日 HH:mm");
            }
            switch (repeat)
            {
                case RepeatType.hourly:
                    return "毎時 " + firstDt.Minute + "分";
                case RepeatType.daily:
                    return firstDt.ToString("毎日 HH:mm");
                case RepeatType.weekly:
                    {
                        var str = "毎週";
                        if (monday) str = str + "月";
                        if (tuesday) str = str + "火";
                        if (wednesday) str = str + "水";
                        if (thursday) str = str + "木";
                        if (friday) str = str + "金";
                        if (saturday) str = str + "土";
                        if (sunday) str = str + "日";
                        return str + firstDt.ToString("HH:mm");
                    }
                case RepeatType.monthly:
                    return firstDt.ToString("毎月 d日 HH:mm");
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
