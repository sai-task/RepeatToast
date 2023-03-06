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
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications; //Notifications Library
using Microsoft.QueryStringDotNET; //QueryString.NET
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Threading.Tasks.Dataflow;
using System.Collections.ObjectModel;
using Windows.UI.Popups;
using System.Diagnostics;
using System.Threading.Tasks;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Simple_Notification
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class ToastAddPage : AbstractEditPage
    {
        private const string _toastFilesFolderName = "ToastFiles";

        public ToastAddPage() => this.InitializeComponent();

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 開始日時が入力されていないとき警告を表示して終了
            NullDateAlertText.Visibility = Date.SelectedDate == null ? Visibility.Visible : Visibility.Collapsed;
            NullTimeAlertText.Visibility = Time.SelectedTime == null ? Visibility.Visible : Visibility.Collapsed;
            if (Date.SelectedDate == null || Time.SelectedTime == null) return;

            // 開始日時が過去のとき警告を表示して終了
            if (_firstDateTimeOffset.CompareTo(DateTimeOffset.Now) <= 0)
            {
                PastDateTimeAlertText.Visibility = Visibility.Visible;
                return;
            }
            else { PastDateTimeAlertText.Visibility = Visibility.Collapsed; }

            // 終了時刻
            if (EndCheckBox.IsChecked == false)
            {
                _lastDateTimeOffset = DateTimeOffset.MaxValue;
            }
            else if (EndDate.SelectedDate == null || EndTime.SelectedTime == null)
            {
                // 終了日時が入力されていないとき警告を表示して終了
                if (EndDate.SelectedDate == null)
                    NullEndDateAlertText.Visibility = Visibility.Visible;
                else
                    NullEndDateAlertText.Visibility = Visibility.Collapsed;
                if (EndTime.SelectedTime == null)
                    NullEndTimeAlertText.Visibility = Visibility.Visible;
                else
                    NullEndTimeAlertText.Visibility = Visibility.Collapsed;

                return;
            }

            // MyToastContent を作成してリストに追加
            var options = new ToastOptions(Title.Text, MyContent.Text, _repeatType, _firstDateTimeOffset, _isMute, _hasEnd, _lastDateTimeOffset, _dayOfWeeks);
            var index = await GetUniqueIndex();
            var newToast = new MyToastContent(index, options);
            App.GetMyToastContents().Add(newToast);

            // スイッチオンと同時に最初のトーストが予約される
            newToast.SwitchOn();

            // セーブ
            await newToast.AddSave();

            this.Frame.Navigate(typeof(MainPage));
        }

        /* インデックスの重複回避のため、コレクションとトーストファイル 双方に存在しないインデックスを探す */
        internal static async Task<int> GetUniqueIndex()
        {
            var existIndexes = new HashSet<int>();
            foreach (var toast in App.GetMyToastContents())
                existIndexes.Add(toast.Index);
            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(_toastFilesFolderName);
            if (item is StorageFolder folder)
            {
                var files = await folder.GetFilesAsync();
                foreach (var file in files)
                {
                    var s = file.Name.Split('.')[0];
                    if (int.TryParse(s, out int index))
                        existIndexes.Add(index);
                }
            }
            for (var i = 1; i < int.MaxValue; i++)
            {
                if (i >= 1000000)
                {
                    throw new Exception("重複しないインデックスが見つかりません。");
                }
                if (!existIndexes.Contains(i))
                {
                    return i;
                }
            }
            throw new Exception("重複しないインデックスが見つかりません。");
        }

        /* トーストのテスト送信 */
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var isMute = MuteButton.IsChecked ?? false;
            var options = new ToastOptions(Title.Text, MyContent.Text, RepeatType.once, DateTimeOffset.MinValue, isMute, false);
            var toast = new MyToastContent(-1, options);
            SendToast.Send(toast);
        }

        private void Time_SelectedTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs args) => SelectedDateOrTimeChanged();
        private void Date_SelectedDateChanged(DatePicker sender, DatePickerSelectedValueChangedEventArgs args) => SelectedDateOrTimeChanged();
        // セーブボタンの有効 or 無効、 _firstDateTimeOffsetの設定
        private void SelectedDateOrTimeChanged()
        {
            if (Date.SelectedDate == null || Time.SelectedTime == null)
            {
                SaveAppBarButton.IsEnabled = false;
                CreateButon.IsEnabled = false;
            }
            else
            {
                SaveAppBarButton.IsEnabled = true;
                CreateButon.IsEnabled = true;
                _firstDateTimeOffset = (DateTimeOffset)(Date.SelectedDate?.Date + (TimeSpan)Time.SelectedTime);
            }
        }
        // Hourly, Dailyなど
        protected override void RepeatTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            // Weeklyの場合 曜日選択パネルを表示 or 非表示
            if (DayChecker != null)
            {
                if (comboBox.SelectedIndex == 2)
                    DayChecker.Visibility = Visibility.Visible;
                else
                    DayChecker.Visibility = Visibility.Collapsed;
            }

            // Monthlyの場合
            if (comboBox.SelectedIndex == 3)
            {
                if (DayNotExist != null) DayNotExist.Visibility = Visibility.Visible;
            }
            else
            {
                if (DayNotExist != null) DayNotExist.Visibility = Visibility.Collapsed; // 例外 DayNotExistがnull
            }
            // _repeatTypeの変更
            if (Repeatable.IsChecked == true)
            {
                switch (comboBox.SelectedIndex)
                {
                    case 0: _repeatType = RepeatType.hourly; break;
                    case 1: _repeatType = RepeatType.daily; break;
                    case 2: _repeatType = RepeatType.weekly; break;
                    case 3: _repeatType = RepeatType.monthly; break;
                }
            }
        }

        // 繰り返しの設定
        private void Repeatable_Checked(object sender, RoutedEventArgs e)
        {
            switch (RepeatTypeComboBox.SelectedIndex)
            {
                case 0: _repeatType = RepeatType.hourly; break;
                case 1: _repeatType = RepeatType.daily; break;
                case 2: _repeatType = RepeatType.weekly; break;
                case 3: _repeatType = RepeatType.monthly; break;
            }
            RepeatSettings.Visibility = Visibility.Visible;
        }
        private void Repeatable_Unchecked(object sender, RoutedEventArgs e)
        {
            _repeatType = RepeatType.once;
            RepeatSettings.Visibility = Visibility.Collapsed;
        }

        // 終了期限の設定
        private void HasEnd_Checked(object sender, RoutedEventArgs e)
        {
            _hasEnd = true;
            if (EndDate.SelectedDate != null && EndTime.SelectedTime != null)
            {
                _lastDateTimeOffset = (DateTimeOffset)EndDate.SelectedDate + (TimeSpan)EndTime.SelectedTime;
            }
            EndDateAndTime.Visibility = Visibility.Visible;
        }

        private void HasEnd_Unchecked(object sender, RoutedEventArgs e)
        {
            _hasEnd = false;
            _lastDateTimeOffset = DateTimeOffset.MaxValue;
            EndDateAndTime.Visibility = Visibility.Collapsed;
        }


        protected override bool On_BackRequested()
        {
            Debug.WriteLine("ToastAddPage.On_BackRequested");
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

        // 月末の設定
        private void DayNotExist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DayNotExist.SelectedIndex == 0)
                _endOf = EndOfMonth.NotSend;
            else if (DayNotExist.SelectedIndex == 1)
                _endOf = EndOfMonth.SendAtLastDay;
        }
    }
}
