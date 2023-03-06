/* Toasts.xmlを読む仕様から、ToastFilesフォルダ内のファイルを読む仕様に変更 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.Serialization;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.Foundation;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Linq.Expressions;
using Windows.ApplicationModel.Activation;
using System.Diagnostics;
using Windows.UI.Xaml.Documents;

namespace CyclicToasts
{
    public sealed class MyBackgroundTask : IBackgroundTask
    {
        public StorageFolder LocalFolder => ApplicationData.Current.LocalFolder;
        private const string _toastsFolderName = "ToastFiles";
        BackgroundTaskDeferral _deferral;
        private StorageFile _xmlFile;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            try
            {
                /*
                fileLock = await FileLock.Create(LocalFolder, "ToastFiles", null, "MyBackgroundTask.Run");
                if (fileLock == null)
                {
                    Debug.WriteLine("ファイルロックに失敗したため終了します。(MyBackgroundTask.Run)");
                    return;
                }
                using (fileLock)
                {
                    DirectoryInfo dInfo = new DirectoryInfo(Path.Combine(LocalFolder.Path, "ToastFiles"));
                    StorageFolder folder;
                    if (dInfo.Exists)
                        folder = await LocalFolder.GetFolderAsync("ToastFiles");
                    else
                        return;
                    var files = await folder.GetFilesAsync();
                    if (files == null || files.Count == 0)
                        return;
                    foreach (var file in files)
					{
                        var xdoc = await ReadXml(file);
                        if (xdoc == null)
                            continue;
                        var xtoast = xdoc.Root.Element("Toast");
                        var index = (int)xtoast.Element("Index");
                        var scheduledDateTimeOffset = xtoast.Element("ScheduledDateTimeOffset");
                        var xoptions = xtoast.Element("Options");
                        var title = (string)xoptions.Element("Title");
                        var content = (string)xoptions.Element("Content");
                        var rtype = (string)xoptions.Element("RType");
                        var firstDateTimeOffset = DateTimeOffset.ParseExact(xoptions.Element("FirstDateTimeOffset").Value, "u", null);
                        var isMute = (bool)xoptions.Element("IsMute");
                        var hasEnd = (bool)xoptions.Element("HasEnd");
                        var lastDateTimeOffset = DateTimeOffset.ParseExact(xoptions.Element("LastDateTimeOffset").Value, "u", null);
                        var endOf = xoptions.Element("EndOf").Value;

                    }*/

                    /*
                    var xdoc = await ReadXml();
                    if (xdoc == null)
                    {
                        Debug.WriteLine("xdocがnullです。(MyBackgroundTask.Run)");
                        return;
                    }

                    var xtoasts = xdoc.Root.Elements();
                    if (xtoasts == null)
                    {
                        Debug.WriteLine("xtoastsがnullです。(MyBackgroundTask.Run)");
                        return;
                    }
                    foreach (var xtoast in xtoasts)
                    {
                        if (xtoast.Element("IsEnabled").Value == "false") return;

                        var index = (int)xtoast.Element("Index");
                        var options = xtoast.Element("Options");
                        var title = (string)options.Element("Title");
                        var content = (string)options.Element("Content");
                        var freq = (string)options.Element("Freq");
                        var firstDateTime = DateTime.ParseExact(options.Element("FirstDateTime").Value, "s", null);
                        var isMute = (bool)options.Element("IsMute");

                        string s = xtoast.Element("ScheduledDateTime").Value;
                        DateTime? scheduledDateTime;
                        if (s == "" || s == null)
                            scheduledDateTime = null;
                        else
                        {
                            try
                            {
                                scheduledDateTime = DateTime.ParseExact(s, "s", null); // System.FormatException: 'String '' was not recognized as a valid DateTime.'
                            }
                            catch (FormatException e)
                            {
                                Debug.WriteLine("ScheduledDateTimeのDateTimeへの変換に失敗しました。");
                                Debug.WriteLine(e.Message);
                                scheduledDateTime = null;
                            }
                        }*/

                        /*ShowParamForDebug(new Dictionary<string, object>
                        {
                            ["index"] = index,
                            ["title"] = title,
                            ["content"] = content,
                            ["freq"] = freq,
                            ["firstDateTime"] = firstDateTime,
                            ["isMute"] = isMute,
                            ["scheduledDateTime"] = scheduledDateTime
                        });*//*

                        // 次にスケジュールすべき時刻(nextDateTime)を計算
                        /*var nextDateTime = CalcNextDateTime(firstDateTime, freq);
                        if (nextDateTime <= firstDateTime)
                            continue;
                        if (nextDateTime <= scheduledDateTime)
                            continue;

                        // トーストをスケジュール
                        var sendToast = new SendToast(index, title, content, isMute);
                        sendToast.Schedule(nextDateTime);
                        scheduledDateTime = nextDateTime;

                        // ScheduledDateTime をファイルに反映
                        Debug.Assert(scheduledDateTime != null);
                        string newScheduledDateTime = scheduledDateTime?.ToString("s");
                        xtoast.Element("ScheduledDateTime").Value = newScheduledDateTime;

                    }//foreach
                    xdoc.Save(_xmlFile.Path);
                    Debug.WriteLine("XMLファイルを更新しました。（MyBackgroundTask.Run）");
                }*/

                /* 以下、ToastFilesフォルダから読む新しいコード */
                var item = await LocalFolder.TryGetItemAsync(_toastsFolderName);
                if (item == null || !(item.IsOfType(StorageItemTypes.Folder)))
                {
                    Debug.WriteLine("{0} フォルダが存在しません。終了します。 (MyBackgroundTask.Run)", _toastsFolderName);
                    return;
                }
                StorageFolder toastsFolder = item as StorageFolder;
                var toasts = await toastsFolder.GetFilesAsync();

                var fileLock = await FileLock.Create(toastsFolder, "ToastFiles", "ToastFiles.lock");
                if (fileLock == null)
                {
                    Debug.WriteLine("フォルダ {0} のロックに失敗しました。(MyBackgroundTask.Run)", _toastsFolderName);
                    return;
                }
                using (fileLock)
                {
                    foreach (StorageFile toastFile in toasts)
                    {
                        var xdoc = await ReadXml(toastFile);
                        if (xdoc == null)
                        {
                            Debug.WriteLine("ファイル {0} でxdocがnullです。(MyBackgroundTask.Run)", toastFile.Name);
                            continue;
                        }

                        var xtoast = xdoc.Root;
                        if (xtoast == null)
                        {
                            Debug.WriteLine("ファイル {0} でxtoastがnullです。(MyBackgroundTask.Run)", toastFile.Name);
                            continue;
                        }
                        if (xtoast.Element("IsEnabled").Value == "false")
                            continue;


                        // ファイルからデータを読み取る
                        var index = (int)xtoast.Element("Index");
                        var options = xtoast.Element("Options");
                        var title = (string)options.Element("Title");
                        var content = (string)options.Element("Content");
                        var rtype = (string)options.Element("RType");
                        var firstDateTimeOffset = DateTimeOffset.Parse(options.Element("FirstDateTimeOffset").Value, null);
                        var hasEnd = (bool)options.Element("HasEnd");
                        DateTimeOffset? lastDateTimeOffset;
                        if (options.Element("LastDateTimeOffset").Value == null)
                            lastDateTimeOffset = null;
                        else
                            lastDateTimeOffset = DateTimeOffset.Parse(options.Element("LastDateTimeOffset").Value, null);
                        var isMute = (bool)options.Element("IsMute");
                        var daysOfWeek = new HashSet<DayOfWeek>();
                        if ((bool)options.Element("Sunday")) daysOfWeek.Add(DayOfWeek.Sunday);
                        if ((bool)options.Element("Monday")) daysOfWeek.Add(DayOfWeek.Monday);
                        if ((bool)options.Element("Tuesday")) daysOfWeek.Add(DayOfWeek.Tuesday);
                        if ((bool)options.Element("Wednesday")) daysOfWeek.Add(DayOfWeek.Wednesday);
                        if ((bool)options.Element("Thursday")) daysOfWeek.Add(DayOfWeek.Thursday);
                        if ((bool)options.Element("Friday")) daysOfWeek.Add(DayOfWeek.Friday);
                        if ((bool)options.Element("Saturday")) daysOfWeek.Add(DayOfWeek.Saturday);
                        var endOf = (string)options.Element("EndOf") == "SendAtLastDay" ? true : false;
                        string s = xtoast.Element("ScheduledDateTimeOffset").Value;
                        Debug.WriteLine("ScheduledDateTimeOffset: {0} (MyBackgroundTask)", s);
                        DateTimeOffset? scheduledDateTimeOffset;
                        if (s == "" || s == null)
                            scheduledDateTimeOffset = null;
                        else
                        {
                            try
                            {
                                scheduledDateTimeOffset = DateTimeOffset.Parse(s, null);
                            }
                            catch (FormatException e)
                            {
                                Debug.WriteLine(e.Message);
                                scheduledDateTimeOffset = null;
                                throw new Exception();
                            }
                        }

                        /*ShowParamForDebug(new Dictionary<string, object>
                        {
                            ["index"] = index,
                            ["title"] = title,
                            ["content"] = content,
                            ["freq"] = freq,
                            ["firstDateTime"] = firstDateTime,
                            ["isMute"] = isMute,
                            ["scheduledDateTime"] = scheduledDateTime
                        });*/

                        // 次にスケジュールすべき時刻(nextDateTimeOffset)を計算
                        var nextDateTimeOffset = CalcNextDateTimeOffset(firstDateTimeOffset, rtype, daysOfWeek, hasEnd, lastDateTimeOffset, endOf);
                        if (nextDateTimeOffset == null)
                            continue;
                        if (nextDateTimeOffset <= firstDateTimeOffset)
                            continue;
                        if (scheduledDateTimeOffset != null && nextDateTimeOffset <= scheduledDateTimeOffset)
                            continue;

                        // トーストをスケジュール
                        var sendToast = new SendToast(index, title, content, isMute);
                        sendToast.Schedule((DateTimeOffset)nextDateTimeOffset);
                        scheduledDateTimeOffset = nextDateTimeOffset;

                        // ScheduledDateTimeOffset をファイルに反映
                        Debug.Assert(scheduledDateTimeOffset != null);
                        string newScheduledDateTimeOffset = scheduledDateTimeOffset?.ToString("");
                        xtoast.Element("ScheduledDateTimeOffset").Value = newScheduledDateTimeOffset;
                        try
                        {
                            // テキストエディタで開いていた場合 メインプロセスと同時に書き込まれた場合も発生する？(Editされた場合？)
                            xdoc.Save(toastFile.Path); // System.IO.IOException: 'The process cannot access the file 'C:\Users\shibata\AppData\Local\Packages\91af3c53-84c6-4e86-9fb4-efb9295ef4de_hsg7vhw5w2v0e\LocalState\ToastFiles\5.xml' because it is being used by another process.'
                        }
                        catch (System.IO.IOException)
                        {
                            Debug.WriteLine("{0} への書き込み中にSystem.IO.IOExceptionが発生しました", toastFile.Name);
                        }

                        Debug.WriteLine("XMLファイルを更新しました。（MyBackgroundTask.Run）");
                    }
                }
            }
            finally
            {
                _deferral.Complete();
            }
        }//Run
        private async Task<XDocument> ReadXml(StorageFile xmlFile)
        {
            var properties = await xmlFile.GetBasicPropertiesAsync();
            if (properties.Size == 0)
                return null;
            return XDocument.Load(xmlFile.Path);
        }


        private DateTime CalcNextDateTime(DateTime basis, string frequency)
        {
            var now = DateTime.Now;
            var next = now;
            switch (frequency.ToLower())
            {
                case "once":
                    return basis;
                case "hourly":
                    next = new DateTime(now.Year, now.Month, now.Day, now.Hour, basis.Minute, 0);
                    if (DateTime.Now.Minute >= basis.Minute)
                        next = next.AddHours(1);
                    return next;
                case "daily":
                    next = new DateTime(now.Year, now.Month, now.Day, basis.Hour, basis.Minute, 0);
                    if (now.TimeOfDay >= basis.TimeOfDay)
                        next = next.AddDays(1);
                    break;
                case "weekly":
                    next = new DateTime(now.Year, now.Month, now.Day, basis.Hour, basis.Minute, 0);
                    DayOfWeek day = basis.DayOfWeek;
                    if (now.DayOfWeek == day && next >= now)
                        break;
                    else
                    {
                        var days = (int)day - (int)(next.DayOfWeek);
                        if (days <= 0)
                            days += 7;
                        next = next.AddDays(days);
                    }
                    if (next >= now)
                        next = next.AddDays(1);
                    break;
                case "monthly":
                    next = new DateTime(now.Year, now.Month, basis.Day, basis.Hour, basis.Minute, 0);
                    if (now >= next)
                        next = next.AddMonths(1);
                    break;
            }
            return next;
        }
        /* DateTime型からDateTimeOffset型へ 曜日、日付指定機能も追加 */
        private DateTimeOffset? CalcNextDateTimeOffset(DateTimeOffset basis, string rtype, HashSet<DayOfWeek> daysOfWeek, bool hasEnd, DateTimeOffset? lastDateTimeOffset, bool endOf)
        {
            // オフセットをbasisに合わせる
            var offset = basis.Offset;
            lastDateTimeOffset = lastDateTimeOffset?.ToOffset(offset);
            var now = DateTimeOffset.Now.ToOffset(offset);

            var next = now;
            DateTimeOffset ret;
            switch (rtype.ToLower())
            {
                case "once":
                    ret = basis;
                    break;
                case "hourly":
                    next = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, basis.Minute, 0, offset);
                    if (DateTimeOffset.Now.Minute >= basis.Minute)
                        next = next.AddHours(1);
                    ret = next;
                    break;
                case "daily":
                    next = new DateTimeOffset(now.Year, now.Month, now.Day, basis.Hour, basis.Minute, 0, offset);
                    if (now.TimeOfDay >= basis.TimeOfDay)
                        next = next.AddDays(1);
                    ret = next;
                    break;
                case "weekly":
                    if (daysOfWeek.Count == 0)
                    {
                        Debug.WriteLine("daysOfWeekが空です (MyBackgroundTask.CalcNextDateTimeOffset)");
                        break;
                    }
                    next = new DateTimeOffset(now.Year, now.Month, now.Day, basis.Hour, basis.Minute, 0, offset);
                    if (next <= now)
                        next = next.AddDays(1); // nextを必ず現在以降にする
                    while (true)
                    { // 指定曜日になるまで１日足す
                        if (daysOfWeek.Contains(next.DayOfWeek)) break;
                        next = next.AddDays(1);
                    }
                    ret = next;
                    break;
                case "monthly":
                    next = new DateTimeOffset(now.Year, now.Month, basis.Day, basis.Hour, basis.Minute, 0, offset);
                    if (next <= now)
                    {
                        if (endOf)
                            next = next.AddMonths(1);
                        else
                        {
                            var temp = next;
                            for (var i = 1; i < 12; i++)
                            {
                                temp = next.AddMonths(i);
                                if (temp.Day == basis.Day)
                                {
                                    next = temp;
                                    break;
                                }
                            }
                        }
                    }
                    ret = next;
                    break;
            }
            if (ret < now) return null;
            if (lastDateTimeOffset == null) return ret;
            else if (lastDateTimeOffset < ret) return null;
            else return ret;
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {

        }
        private void ShowParamForDebug(Dictionary<string, object> dict)
        {
            var message = new StringBuilder();
            foreach (var item in dict)
            {
                message.Append(item.Key);
                message.Append(" = ");
                message.Append(item.Value.ToString());
                message.AppendLine();
            }
            Debug.Write(message);
        }
        private void ScheduleToastOneMinuteLater()
        {
            var sendToast = new SendToast(int.MaxValue, "For Debug", "One Minute Later", false);
            sendToast.Schedule(DateTime.Now.AddMinutes(1));
        }
    }//Class
}//namespace
