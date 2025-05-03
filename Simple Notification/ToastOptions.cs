/* ユーザが作成・変更できるオプションを表すクラス
 * RType: 繰り返しの種類 daily, weeklyなど
 * DayOfWeeks: RTypeがWeeklyのときの指定曜日（複数可）
 * EndOf: 指定した日がその月に存在しない場合の対応
 * 
 * FirstDateTimeOffset: 繰り返しの基準、最初にトーストが送られる日時
 * HasEnd: 繰り返しの終了期限が存在するか否か
 * LastDateTimeOffset: 繰り返しの終了期限
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace Simple_Notification
{
    [DataContract (Namespace = "")]
    public class ToastOptions
    {
        /* フィールド・プロパティ */
        [DataMember]
        public string Title { get; private set; }
        [DataMember]
        public string Content { get; private set; }
        [DataMember]
        public RepeatType RType { get; private set; }
        [DataMember]
        public bool Sunday { get; private set; } = false;
        [DataMember]
        public bool Monday { get; private set; } = false;
        [DataMember]
        public bool Tuesday { get; private set; } = false;
        [DataMember]
        public bool Wednesday { get; private set; } = false;
        [DataMember]
        public bool Thursday { get; private set; } = false;
        [DataMember]
        public bool Friday { get; private set; } = false;
        [DataMember]
        public bool Saturday { get; private set; } = false;
        private HashSet<DayOfWeek> _daysOfWeek;
        [IgnoreDataMember]
        public HashSet<DayOfWeek> daysOfWeek
        {
            get
            {
                if (_daysOfWeek == null) _daysOfWeek = new HashSet<DayOfWeek>();
                _daysOfWeek.Clear();
                if (Sunday == true) _daysOfWeek.Add(DayOfWeek.Sunday);
                if (Monday == true) _daysOfWeek.Add(DayOfWeek.Monday);
                if (Tuesday == true) _daysOfWeek.Add(DayOfWeek.Tuesday);
                if (Wednesday == true) _daysOfWeek.Add(DayOfWeek.Wednesday);
                if (Thursday == true) _daysOfWeek.Add(DayOfWeek.Thursday);
                if (Friday == true) _daysOfWeek.Add(DayOfWeek.Friday);
                if (Saturday == true) _daysOfWeek.Add(DayOfWeek.Saturday);
                return _daysOfWeek;
            }
        }
        [IgnoreDataMember]
        public string DaysOfMonth { get; private set; } // 1,3,10 のような形式
        [DataMember (Name = "FirstDateTimeOffset")]
        private string _firstDateTimeOffsetSerialized;
        [IgnoreDataMember]
        public DateTimeOffset FirstDateTimeOffset { get; private set; }
        [DataMember]
        public bool HasEnd { get; private set; }
        [DataMember (Name = "LastDateTimeOffset")]
        private string _lastDateTimeOffsetSerialized;
        [IgnoreDataMember]
        public DateTimeOffset LastDateTimeOffset { get; private set; }
        [DataMember]
        public EndOfMonth EndOf { get; private set; }
        [DataMember]
        public bool IsMute { get; private set; }

        /* コンストラクタ */
        public ToastOptions(){}
        public ToastOptions(string title, string content, RepeatType repeat, DateTimeOffset firstDateTimeOffset, bool isMute, bool hasEnd, DateTimeOffset? lastDateTimeOffset = null, HashSet<DayOfWeek> dayOfWeeks = null, EndOfMonth endOf = EndOfMonth.NotSend)
        {
            Title = title;
            Content = content;
            RType = repeat;
            FirstDateTimeOffset = firstDateTimeOffset;
            IsMute = isMute;
            HasEnd = hasEnd;
            EndOf = endOf;
            if (hasEnd)
            {
                if (lastDateTimeOffset == null)
                    throw new ArgumentException("終了日時が指定されていません。");
                LastDateTimeOffset = (DateTimeOffset)lastDateTimeOffset;
            }
            else
            {
                LastDateTimeOffset = DateTime.MaxValue;
            }
            if (repeat == RepeatType.weekly)
            {
                if (dayOfWeeks == null || dayOfWeeks.Count == 0)
                    throw new ArgumentException("曜日が指定されていません。");
                if (dayOfWeeks.Contains(DayOfWeek.Sunday)) Sunday = true;
                if (dayOfWeeks.Contains(DayOfWeek.Monday)) Monday = true;
                if (dayOfWeeks.Contains(DayOfWeek.Tuesday)) Tuesday = true;
                if (dayOfWeeks.Contains(DayOfWeek.Wednesday)) Wednesday = true;
                if (dayOfWeeks.Contains(DayOfWeek.Thursday)) Thursday = true;
                if (dayOfWeeks.Contains(DayOfWeek.Friday)) Friday = true;
                if (dayOfWeeks.Contains(DayOfWeek.Saturday)) Saturday = true;
            }
        }
        /* コピーコンストラクタ そのままコピーするだけ */
        public ToastOptions(ToastOptions options)
        {
            Title = options.Title; Content = options.Content; RType = options.RType;
            FirstDateTimeOffset = options.FirstDateTimeOffset; IsMute = options.IsMute; HasEnd = options.HasEnd;
            LastDateTimeOffset = options.LastDateTimeOffset; Sunday = options.Sunday; Monday = options.Monday;
            Tuesday = options.Tuesday; Wednesday = options.Wednesday; Thursday = options.Thursday; Friday = options.Friday; Saturday = options.Saturday;
            DaysOfMonth = options.DaysOfMonth;
        }

        /* シリアライズ用 開始日時、終了日時を特定の形式で文字列に変換する */
        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            if (FirstDateTimeOffset == null)
                _firstDateTimeOffsetSerialized = null;
            _firstDateTimeOffsetSerialized = FirstDateTimeOffset.ToString();
            if (LastDateTimeOffset == null)
                _lastDateTimeOffsetSerialized = null;
            _lastDateTimeOffsetSerialized = LastDateTimeOffset.ToString();
        }
        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            System.Diagnostics.Debug.Assert(_firstDateTimeOffsetSerialized != null && _firstDateTimeOffsetSerialized != "");
                
            FirstDateTimeOffset = DateTime.Parse(_firstDateTimeOffsetSerialized, null);
            LastDateTimeOffset = DateTime.Parse(_lastDateTimeOffsetSerialized, null);
        }

        /* 現在から引数の期限までにトーストが予約されるべき時刻を全て返す 未完成 */
        internal IEnumerable<DateTimeOffset> GetNextDateTimeOffsets(DateTimeOffset limit)
        {/*
            var timeList = new List<DateTimeOffset>();
            // onceなら終了期限は無効
            if (RType == RepeatType.once)
            {
                if (FirstDateTime > DateTime.Now)
                    timeList.Add(FirstDateTime);
                return timeList;
            }
            var offset = GetNexDateTimeOffset(DateTimeOffset.Now);
            if (offset == null || offset > limit)
                return timeList;
            timeList.Add((DateTimeOffset)offset);
            while (true)
            {
                offset = GetNexDateTimeOffset((DateTimeOffset)offset);
                if (offset == null || offset > limit)
                    break;
                timeList.Add((DateTimeOffset)offset);
            }
            return timeList;*/
            throw new NotImplementedException();
        }
        /* 引数の時刻または現在以降に最初にスケジュールされるべき時刻を返す スケジュールされるべき時刻がなければnullを返す FirstDateTimeOffsetのオフセットを基準にする */
        internal DateTimeOffset? GetNextDateTimeOffset(DateTimeOffset dateTimeOffset)
        {
            // 戻り値
            DateTimeOffset ret;

            // オフセット
            TimeSpan offset = FirstDateTimeOffset.Offset;
            dateTimeOffset = dateTimeOffset.ToOffset(offset);

            // dt 以降の時刻を返す
            DateTimeOffset dt;
            var now = DateTimeOffset.Now.ToOffset(offset);
            if (dateTimeOffset <= now)
                dt = now;
            else
                dt = dateTimeOffset;

            DateTimeOffset first = FirstDateTimeOffset;

            if (RType == RepeatType.once)
            {
                if (first > dt)
                    ret = first;
                else
                    return null;
            }

            switch (RType)
			{
                case RepeatType.hourly:
                    ret = new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, first.Minute, 0, offset);
                    if (ret <= dt)
                        ret = ret.AddHours(1);
                    break;
                case RepeatType.daily:
                    ret = new DateTimeOffset(dt.Year, dt.Month, dt.Day, first.Hour, first.Minute, 0, offset);
                    if (ret <= dt)
                        ret = ret.AddDays(1);
                    break;
                case RepeatType.weekly:
                    if (daysOfWeek.Count == 0)
                    {
                        Debug.WriteLine("daysOfWeekが空です (ToastOptions.GetNextDateTimeOffset)");
                        break;
                    }
                    ret = new DateTimeOffset(dt.Year, dt.Month, dt.Day, first.Hour, first.Minute, 0, offset);
                    if (ret <= dt)
                        ret = ret.AddDays(1); // nextを必ず現在以降にする
                    while (true) { // 指定曜日になるまで１日足す
                        if (daysOfWeek.Contains(ret.DayOfWeek)) break;
                        ret = ret.AddDays(1);
                    }
                    break;
                case RepeatType.monthly:
                    ret = new DateTimeOffset(dt.Year, dt.Month, first.Day, first.Hour, first.Minute, 0, offset);
                    if (ret <= dt)
                    {
                        if (EndOf == EndOfMonth.SendAtLastDay)
                            ret = ret.AddMonths(1);
                        else
                        {
                            var temp = ret;
                            for (var i= 1; i < 12; i++)
                            {
                                temp = ret.AddMonths(i);
                                if (temp.Day == first.Day)
                                {
                                    ret = temp;
                                    break;
                                }
                            }
                        }
                    }
                    break;
            }
            if (ret > LastDateTimeOffset)
                return null;
            return ret;
        }
        private bool IsLastDayOfMonth(DateTimeOffset dt)
		{
            if (dt.Month != dt.AddDays(1).Month)
                return true;
            else
                return false;
		}
    }
}
