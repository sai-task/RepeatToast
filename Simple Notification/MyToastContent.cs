// ToastOptionsを保持し、ユーザが変更できないIndex等も保持する
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications; // Notifications library
using Microsoft.QueryStringDotNET; // QueryString.NET
using Windows.UI.Popups;
using Windows.ApplicationModel.Background;
using System.Runtime.Serialization;
using System.Globalization;
using Windows.Storage;
using System.IO;
using System.ComponentModel;
using Windows.Storage.Search;
using System.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using System.Xml;

namespace Simple_Notification
{
    [DataContract (Namespace = "", Name = "Toast")]
    public class MyToastContent : INotifyPropertyChanged, IComparable
    {
        /* 定数 */
        private const string _muteAudioFilePath = "ms-appx:///Assets/sound/mute.mp3";
        private const string _maxIndexFileName = "Index";
        private const string _backgroundTaskName = "CyclicToasts";
        private const string _backgroundTaskEntryPoint = "CyclicToasts.MyBackgroundTask";
        private const string _toastFilesFolderName = "ToastFiles";

        /* バックグラウンドカラー定数 */
        private const string _enabledAndSelectedColor = "#D9FCFF";
        private const string _enabledAndNotSelectedColor = "#FFFFFF";
        private const string _disabledAndSelectedColor = "#A8B9BF";
        private const string _disabledAndNotSelectedColor = "#BFBFBF";

        /* プロパティ */
        [DataMember]
        public ToastOptions Options { get; set; }
        [DataMember]
        public int Index { get; private set; }

        [DataMember (Name = "ScheduledDateTimeOffset")]
        private string _scheduledDateTimeOffsetSerialized;
        [IgnoreDataMember]
        public DateTimeOffset? ScheduledDateTimeOffset { get; private set; }

        [IgnoreDataMember]
        public DateTimeOffset? NextDateTimeOffset => GetNextDateTimeOffset();
        public string ToastGroup() => Index.ToString();

        [DataMember(Name ="IsEnabled")]
        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            private set
            {
                if (_toggleOn != value)
                {
                    IsNotToggled = true;
                    ToggleOn = value;
                    IsNotToggled = false;
                }
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsEnabled"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MyBackgroundColor"));
            }
        }
        public static bool IsNotToggled { get; set; }
        [IgnoreDataMember]
        private bool _toggleOn;
        [IgnoreDataMember]
        public bool ToggleOn
        {
            get => _toggleOn;
            set
            {
                if (_toggleOn != value)
                {
                    _toggleOn = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ToggleOn"));
                }
                if (!IsNotToggled && _isEnabled != value) {
                    if (value)
                        SwitchOn();
                    else
                        SwitchOff();
                }

            }
        }
        [IgnoreDataMember]
        private bool _isSelected;
        [IgnoreDataMember]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MyBackgroundColor"));
            }
        }
        [IgnoreDataMember]
        public string MyBackgroundColor
        {
            get {
                string color;
                if (IsEnabled && IsSelected)
                    color = _enabledAndSelectedColor;
                else if (IsEnabled && !IsSelected)
                    color = _enabledAndNotSelectedColor;
                else if (!IsEnabled && IsSelected)
                    color = _disabledAndSelectedColor;
                else
                    color = _disabledAndNotSelectedColor;
                return color;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /* シリアライズに使われるデフォルトコンストラクタ */
        public MyToastContent() { }
        /* メインのコンストラクタ インデックスは呼び出し元で重複チェック */
        public MyToastContent(int index, ToastOptions options)
        {
            if (options == null) throw new ArgumentException();
            Index = index;
            this.Options = options;
        }

        /* コピーコンストラクタ */
        public MyToastContent(MyToastContent myToast)
        {
            if (myToast == null)
                throw new ArgumentNullException();
            if (myToast.Options == null)
            {
                Debug.WriteLine("myToast.Optionsがnullです(MyToastContentコピーコンストラクタ)");
                return;
            }
            Options = new ToastOptions(myToast.Options);
            Index = myToast.Index;
            IsEnabled = myToast.IsEnabled;
        }
        /* インデックスリセット用のコピーコンストラクタ Indexを引数のインデックスに合わせる scheduledDateTimeOffsetはコピーするべきか */
        public MyToastContent(MyToastContent myToast, int newIndex)
        {
            Options = new ToastOptions(myToast.Options);
            Index = newIndex;
            IsEnabled = myToast.IsEnabled;
        }

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            if (ScheduledDateTimeOffset == null)
                _scheduledDateTimeOffsetSerialized = null;
            else
                _scheduledDateTimeOffsetSerialized = ((DateTimeOffset)ScheduledDateTimeOffset).ToString();
        }
        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            if (_scheduledDateTimeOffsetSerialized == null || _scheduledDateTimeOffsetSerialized == "")
                ScheduledDateTimeOffset = null;
            else
                ScheduledDateTimeOffset = DateTimeOffset.Parse(_scheduledDateTimeOffsetSerialized, null);

            // トグルスイッチにON/OFF状態を反映させる
            IsNotToggled = true;
            ToggleOn = IsEnabled;
            IsNotToggled = false;
        }
        public string GetToastTag(DateTimeOffset dateTime) => dateTime.ToString("u");
 
        public DateTimeOffset? GetNextDateTimeOffset()
        {
            // Options がnullの場合がある 起動時？
            Debug.WriteLineIf(Options == null, "Optionsがnull (MyToastContent.GetNextDateTimeOffset)");
            return Options?.GetNextDateTimeOffset(DateTimeOffset.Now);
        }
        public void SwitchOn()
        {
            Debug.Assert(Options != null);
            IsEnabled = true;

            // スケジュールの重複チェック
            var toastScheduled = false;
            ToastNotifier notifier = ToastNotificationManager.CreateToastNotifier();
            var scheduledToasts = notifier.GetScheduledToastNotifications();
            if (scheduledToasts.Any(t => t.Group == ToastGroup())) // データ消失後の問題 グループが消失
            {
                toastScheduled = true;
            }

            // 次のトーストを一回分スケジュール
            if (!toastScheduled)
            {
                if (Options.RType == RepeatType.once)
                {
                    if (Options.FirstDateTimeOffset > DateTime.Now)
                    {
                        if (SendToast.Schedule(this, Options.FirstDateTimeOffset))
                            ScheduledDateTimeOffset = Options.FirstDateTimeOffset;
                    }
                }
                else
                {
                    // 次回のスケジュールタイムを計算してスケジュール
                    /*
                    DateTimeOffset dt = Options.FirstDateTimeOffset;
                    RepeatType type = Options.RType;
                    switch(type)
					{
                        case RepeatType.hourly:
                            while(dt.CompareTo(DateTimeOffset.Now) <= 0)
							{
                                dt = dt.AddHours(1);
							}
                            break;
                        case RepeatType.daily:
                            while (dt.CompareTo(DateTimeOffset.Now) <= 0)
                            {
                                dt = dt.AddDays(1);
                            }
                            break;
                        case RepeatType.weekly:
                            while (dt.CompareTo(DateTimeOffset.Now) <= 0)
                            {
                                dt = dt.AddDays(7);
                            }
                            break;
                        case RepeatType.monthly:
                            int day = dt.Day; // 日だけは変えない
                            while (dt.CompareTo(DateTimeOffset.Now) <= 0)
                            {

                                dt = dt.AddMonths(1);
                            }
                            dt = new DateTimeOffset(dt.Year, dt.Month, day, dt.Hour, dt.Minute, dt.Second, dt.Offset);
                            break;
                    }
                    if (SendToast.Schedule(this, dt))
                        ScheduledDateTimeOffset = Options.FirstDateTimeOffset;
                    */
                    //throw new NotImplementedException();

                    var next = NextDateTimeOffset;
                    if (next == null)
                    {
                        Debug.WriteLine("NextDateTimeOffsetがnull (MyToastContent.SwitchOn)");
                    }
                    else
                    {
                        SendToast.Schedule(this, (DateTimeOffset)next);
                    }
                }
            }

            if (Options.RType == RepeatType.once)
                return;

            /* 繰り返しが有効ならバックグラウンドタスクを登録する */
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
            if (taskRegistered)
            {
                return;
            }

            // バックグラウンドタスクの登録
            var builder = new BackgroundTaskBuilder();
            builder.Name = _backgroundTaskName;
            builder.TaskEntryPoint = _backgroundTaskEntryPoint;
            builder.SetTrigger(new TimeTrigger(freshnessTime:15, oneShot:false));
            _ = builder.Register();
        }
        public void SwitchOff(bool saveFlag = true)
        {
            IsEnabled = false;
            SendToast.CancelAll(this);
            if (saveFlag)
                _ = EditSave();
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return -1;
            var other = obj as MyToastContent;
            if (other == null)
                throw new ArgumentException("Object is not a MyToastContent");

            if (this.IsEnabled == true && other.IsEnabled == false)
                return -1;
            if (this.IsEnabled == false && other.IsEnabled == true)
                return 1;

            var thisNextDateTime = this.NextDateTimeOffset;
            var otherNextDateTime = other.NextDateTimeOffset;
            if (thisNextDateTime != null && other.NextDateTimeOffset == null)
                return -1;
            if (thisNextDateTime == null && other.NextDateTimeOffset != null)
                return 1;

            return this.Index - other.Index;
        }
        /* 単一ファイルにこのトーストを保存 ファイル名は[Index].xml 同一ファイルが存在すれば失敗 */
        internal async Task AddSave()
        {
            Debug.WriteLine("MyToastContent.AddSaveを開始しました。");
            var localFolder = ApplicationData.Current.LocalFolder;
            var folder = await localFolder.CreateFolderAsync("ToastFiles", CreationCollisionOption.OpenIfExists);
            var newFile = await folder.CreateFileAsync(Index.ToString() + ".xml", CreationCollisionOption.FailIfExists);
            var settings = new XmlWriterSettings
            {
                Encoding = new System.Text.UTF8Encoding(false),
                Indent = true,
                IndentChars = "\t",
            };
            using (var fileLock = await FileLock.Create(folder, newFile.Name, null, "MyToastContent.AddSave"))
            {
                Debug.Assert(fileLock != null);
                using (var writer = XmlWriter.Create(newFile.Path, settings))
                {
                    var serializer = new DataContractSerializer(typeof(MyToastContent));
                    serializer.WriteObject(writer, this);
                }
            }
            Debug.WriteLine("MyToastContent.AddSaveを終了します。");
        }
        internal async Task EditSave()
        {
            Debug.WriteLine("MyToastContent.EditSaveを開始しました。");
            // 一時ファイルに保存
            var tempFolder = ApplicationData.Current.LocalCacheFolder;
            var tempFile = await tempFolder.CreateFileAsync(Index.ToString() + ".xml", CreationCollisionOption.GenerateUniqueName);
            var settings = new XmlWriterSettings
            {
                Encoding = new System.Text.UTF8Encoding(false),
                Indent = true,
                IndentChars = "\t",
            };
            using (var writer = XmlWriter.Create(tempFile.Path, settings))
            {
                var serializer = new DataContractSerializer(typeof(MyToastContent));
                serializer.WriteObject(writer, this);
            }

            // コピー
            var localFolder = ApplicationData.Current.LocalFolder;
            var folder = await localFolder.CreateFolderAsync("ToastFiles", CreationCollisionOption.OpenIfExists);
            using (var fileLock = await FileLock.Create(folder, Index.ToString() + ".xml", null, "MyToastContent.EditSave"))
            {
                Debug.Assert(fileLock != null);
                await tempFile.CopyAsync(folder, Index.ToString() + ".xml", NameCollisionOption.ReplaceExisting);
            }
            Debug.WriteLine("MyToastContent.EditSaveを終了します。");
        }
        // 単一ファイルから一つのトーストを読み込む
        internal static async Task<MyToastContent> LoadFromFile(StorageFile toastFile) // error
        {
            if (toastFile == null) throw new ArgumentNullException();

            var properties = await toastFile.GetBasicPropertiesAsync();
            if (properties.Size == 0)
            {
                Debug.WriteLine("トーストファイルが空です(MyToastContent.LoadFromFile)");
                throw new Exception();
            }

            MyToastContent toast = null;
            using (var reader = XmlReader.Create(toastFile.Path))
            {
                var serializer = new DataContractSerializer(typeof(MyToastContent));
                toast = serializer.ReadObject(reader) as MyToastContent;
            }
            return toast;
        }
    }//class


}//namespace
