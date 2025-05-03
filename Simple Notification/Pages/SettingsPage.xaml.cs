using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.Storage;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Simple_Notification
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SettingsPage : BasePage
    {
        private readonly Dictionary<string, object> _defaultSettings = new Dictionary<string, object>()
        {
            { "IsIconVisible", true },
        };
        private Dictionary<string, object> _currentSettings = new Dictionary<string, object>()
        {
            { "IsIconVisible", true },
        };
        private bool isSettingsEdited = false;
        private bool _isIconVisible;
        public SettingsPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // 設定の読み込み
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            foreach (var item in _defaultSettings)
            {
                if (localSettings.Values.ContainsKey(item.Key))
                {
                    _currentSettings[item.Key] = localSettings.Values[item.Key];
                    _isIconVisible = (bool)_currentSettings[item.Key];
                }
                else
                {
                    Debug.WriteLine("設定に{0}が存在しませんでした。(Settingspage)", item.Key);
                    // デフォルト値を設定
                    localSettings.Values.Add(item.Key, item.Value);
                    _currentSettings[item.Key] = item.Value;
                    _isIconVisible = true;
                }
            }

            base.OnNavigatedTo(e);
        }

        protected override bool On_BackRequested()
        {
            // 参考URL: https://docs.microsoft.com/ja-jp/windows/uwp/design/controls-and-patterns/navigationview#backwards-navigation
            if (!this.Frame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (NavView.IsPaneOpen &&
                (NavView.DisplayMode == NavigationViewDisplayMode.Compact ||
                 NavView.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            this.Frame.GoBack();
            return true;
        }
        private void Icon_Toggled(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Icon Switch Toggled.");
            _isIconVisible = Icon.IsOn;
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            _currentSettings["IsIconVisible"] = _isIconVisible;
            localSettings.Values["IsIconVisible"] = _isIconVisible;
        }
    }
}
