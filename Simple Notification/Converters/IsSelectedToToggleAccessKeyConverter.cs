using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Simple_Notification
{
    /* リストビュー内のトグルスイッチのアクセスキーを選択されたカラムに設定する */
    public class IsSelectedToToggleAccessKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || !(value is bool))
                return null;
            var isSelected = (bool)value;
            if (isSelected)
                return "o";
            else return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
