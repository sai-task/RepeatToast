using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Simple_Notification
{
    public class Settings : INotifyPropertyChanged
    {
        public bool IsIconVisible { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SaveAll()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["IsIconVisible"] = IsIconVisible;
        }
        public void LoadAll()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("IsIconVisible"))
                IsIconVisible = (bool)localSettings.Values["IsIconVisible"];
            else
            {
                Debug.WriteLine("設定IsIconVisibleが存在しませんでした。作成します。");
                localSettings.Values.Add("IsIconVisible", true);
            }
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsIconVisible"));
        }
    }
}
