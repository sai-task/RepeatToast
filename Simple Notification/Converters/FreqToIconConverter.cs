using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Simple_Notification
{
    /* MainPageのリストビューで、リピートする場合としない場合とでアイコンを変える */
    class FreqToIconConverter : IValueConverter
    {
        private const string _iconPathForOnce = "ms-appx:///Assets/icon/thunder-48.png";
        private const string _iconPathForCyclic = "ms-appx:///Assets/icon/repeat-48.png";
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || !(value is RepeatType))
                return null;
            var freq = (RepeatType)value;
            if (freq == RepeatType.once)
                return _iconPathForOnce;
            else
                return _iconPathForCyclic;
        } 
        // stringを返すとエラー？

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
