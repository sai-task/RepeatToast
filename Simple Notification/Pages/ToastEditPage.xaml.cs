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
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.ApplicationModel.Background;
using System.Diagnostics;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Simple_Notification
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class ToastEditPage : AbstractEditPage
    {
        private const string _backgroundTaskName = "CyclicToasts";
        private const string _backgroundTaskEntryPoint = "CyclicToasts.MyBackgroundTask";
        private MyToastContent _toast;

        public ToastEditPage() => this.InitializeComponent();
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is MyToastContent)
            {
                _toast = (MyToastContent)e.Parameter;
                var options = _toast.Options;
                if (options is null) // optionsがnullであることによって例外が発生するため。暫定的な措置。恐らくファイル保存の段階で問題がある。
                {
                    Debug.WriteLine("トーストのオプションがNULLです。");
                    goto Label;
                }

                // 既存の設定をフィールドに格納
                _repeatType = options.RType;
                _firstDateTimeOffset = options.FirstDateTimeOffset;
                _hasEnd = options.HasEnd;
                _lastDateTimeOffset = options.LastDateTimeOffset;
                _dayOfWeeks = new HashSet<DayOfWeek>();
                if (options.Sunday) _dayOfWeeks.Add(DayOfWeek.Sunday);
                if (options.Monday) _dayOfWeeks.Add(DayOfWeek.Monday);
                if (options.Tuesday) _dayOfWeeks.Add(DayOfWeek.Tuesday);
                if (options.Wednesday) _dayOfWeeks.Add(DayOfWeek.Wednesday);
                if (options.Thursday) _dayOfWeeks.Add(DayOfWeek.Thursday);
                if (options.Friday) _dayOfWeeks.Add(DayOfWeek.Friday);
                if (options.Saturday) _dayOfWeeks.Add(DayOfWeek.Saturday);
                _isMute = options.IsMute;
                _endOf = options.EndOf;

                // 設定に応じて要素を非表示、バインドが使えない要素に値をセット
                switch (_repeatType)
                {
                    case RepeatType.once:
                        RepeatSettings.Visibility = Visibility.Collapsed;
                        Repeatable.IsChecked = false;
                        break;
                    case RepeatType.hourly:
                        DayChecker.Visibility = Visibility.Collapsed;
                        DayNotExist.Visibility = Visibility.Collapsed;
                        RepeatTypeComboBox.SelectedIndex = 0;
                        Repeatable.IsChecked = true;
                        break;
                    case RepeatType.daily:
                        DayChecker.Visibility = Visibility.Collapsed;
                        DayNotExist.Visibility = Visibility.Collapsed;
                        RepeatTypeComboBox.SelectedIndex = 1;
                        Repeatable.IsChecked = true;
                        break;
                    case RepeatType.weekly:
                        DayNotExist.Visibility = Visibility.Collapsed;
                        RepeatTypeComboBox.SelectedIndex = 2;
                        Repeatable.IsChecked = true;
                        break;
                    case RepeatType.monthly:
                        DayChecker.Visibility = Visibility.Collapsed;
                        RepeatTypeComboBox.SelectedIndex = 3;
                        Repeatable.IsChecked = true;
                        break;
                }
                if (_hasEnd == false)
                    EndDateAndTime.Visibility = Visibility.Collapsed;
            }
            Label:
            base.OnNavigatedTo(e); // このコードの位置は？
        }

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

            /* トーストオプションの差し替え */
            _toast.Options = new ToastOptions(Title.Text, MyContent.Text, _repeatType, _firstDateTimeOffset, _isMute, _hasEnd, _lastDateTimeOffset, _dayOfWeeks);

            /* トーストの再スケジュール */
            bool isEnalbed = _toast.IsEnabled; 
            _toast.SwitchOff(saveFlag: false); // SwitchOffでスケジュールがキャンセルされる
            if (isEnalbed)
                _toast.SwitchOn();

            /* 必要ならバックグラウンドタスクの登録 or 登録解除 */
            var cyclicToastIsExist = App.GetMyToastContents().Any(x => x.Options.RType != RepeatType.once && x.IsEnabled);
            if (cyclicToastIsExist)
            {
                // バックグラウンドタスクの重複チェック
                var taskRegistered = false;
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == _backgroundTaskName)
                    {
                        taskRegistered = true;
                        break;
                    }
                }
                if (!taskRegistered)
                {
                    // バックグラウンドタスクの登録
                    var builder = new BackgroundTaskBuilder()
                    {
                        Name = _backgroundTaskName,
                        TaskEntryPoint = _backgroundTaskEntryPoint
                    };
                    builder.SetTrigger(new TimeTrigger(freshnessTime: 15, oneShot: false));
                    _ = builder.Register();
                }
            }
            else
            {
                // バックグラウンドタスクの登録解除
                var tasks = BackgroundTaskRegistration.AllTasks.Where(x => x.Value.Name == _backgroundTaskName);
                foreach (var task in tasks)
                    task.Value.Unregister(cancelTask:true);
            }
            /* ファイルセーブ */
            await _toast.EditSave();

            this.Frame.Navigate(typeof(MainPage));
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var options = new ToastOptions(Title.Text, MyContent.Text, RepeatType.once, DateTimeOffset.MinValue, MuteButton.IsChecked ?? false, false, DateTimeOffset.Now, new HashSet<DayOfWeek>());
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
        protected override void RepeatTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            // Weeklyの場合 曜日選択パネルを表示 / 非表示
            if (DayChecker != null)
            {
                if (comboBox.SelectedIndex == 2)
                    DayChecker.Visibility = Visibility.Visible;
                else
                    DayChecker.Visibility = Visibility.Collapsed; // エラー DayCheckerがnull
            }

            // Monthlyの場合
            if (DayNotExist != null)
            {
                if (comboBox.SelectedIndex == 3)
                {
                    DayNotExist.Visibility = Visibility.Visible;
                }
                else
                {
                    DayNotExist.Visibility = Visibility.Collapsed;
                }
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

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
