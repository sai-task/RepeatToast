using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Simple_Notification
{
    public abstract class AbstractEditPage : BasePage
    {
        protected RepeatType _repeatType;
        protected DateTimeOffset _firstDateTimeOffset = DateTimeOffset.Now;
        protected DateTimeOffset _lastDateTimeOffset = DateTimeOffset.MaxValue;
        protected bool _isMute = false;
        protected bool _hasEnd = false;
        protected HashSet<DayOfWeek> _dayOfWeeks = new HashSet<DayOfWeek>();
        protected EndOfMonth _endOf = EndOfMonth.NotSend;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
        protected override bool On_BackRequested()
        {
            return true;
        }
        protected void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
        protected virtual void RepeatTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine(sender.GetType());
            var comboBox = sender as ComboBox;
            switch (comboBox.SelectedIndex)
            {
                case 0: _repeatType = RepeatType.hourly; break;
                case 1: _repeatType = RepeatType.daily; break;
                case 2: _repeatType = RepeatType.weekly; break;
                case 3: _repeatType = RepeatType.monthly; break;
            }
        }
        protected void MuteButton_Checked(object sender, RoutedEventArgs e) => _isMute = true;
        protected void MuteButton_Unchecked(object sender, RoutedEventArgs e) => _isMute = false;

        protected void Sunday_Checked(object sender, RoutedEventArgs e) => _dayOfWeeks.Add(DayOfWeek.Sunday);
        protected void Sunday_Unchecked(object sender, RoutedEventArgs e) => _dayOfWeeks.Remove(DayOfWeek.Sunday);
        protected void Monday_Checked(object sender, RoutedEventArgs e) => _dayOfWeeks.Add(DayOfWeek.Monday);
        protected void Monday_Unchecked(object sender, RoutedEventArgs e) => _dayOfWeeks.Remove(DayOfWeek.Monday);
        protected void Tuesday_Checked(object sender, RoutedEventArgs e) => _dayOfWeeks.Add(DayOfWeek.Tuesday);
        protected void Tuesday_Unchecked(object sender, RoutedEventArgs e) => _dayOfWeeks.Remove(DayOfWeek.Tuesday);
        protected void Wednesday_Checked(object sender, RoutedEventArgs e) => _dayOfWeeks.Add(DayOfWeek.Wednesday);
        protected void Wednesday_Unchecked(object sender, RoutedEventArgs e) => _dayOfWeeks.Remove(DayOfWeek.Wednesday);
        protected void Thursday_Checked(object sender, RoutedEventArgs e) => _dayOfWeeks.Add(DayOfWeek.Thursday);
        protected void Thursday_Unchecked(object sender, RoutedEventArgs e) => _dayOfWeeks.Remove(DayOfWeek.Thursday);
        protected void Friday_Checked(object sender, RoutedEventArgs e) => _dayOfWeeks.Add(DayOfWeek.Friday);
        protected void Friday_Unchecked(object sender, RoutedEventArgs e) => _dayOfWeeks.Remove(DayOfWeek.Friday);
        protected void Saturday_Checked(object sender, RoutedEventArgs e) => _dayOfWeeks.Add(DayOfWeek.Saturday);
        protected void Saturday_Unchecked(object sender, RoutedEventArgs e) => _dayOfWeeks.Remove(DayOfWeek.Saturday);
    }
}
