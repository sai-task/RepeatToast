using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications; //Notifications Library
using Microsoft.QueryStringDotNET; //QueryString.NET
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;
using Windows.Storage;
using Windows.UI.Popups;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Controls;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Simple_Notification
{
    [CollectionDataContract(Namespace = "", Name = "Toasts")]
    public class MyToastContents : ObservableCollection<MyToastContent>
    {
        private const string _xmlFileName = "Toasts.xml";
        private const string _xmlLockFileName = "Toasts.xml.lock";
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private static int _loadToastsCountForDebug = 0;
        private static bool _isBackup;

        public MyToastContents() { }
        ~MyToastContents()
        {
            _rwLock.Dispose();
        }
        public MyToastContents Read()
        {
            try
            {
                _rwLock.EnterReadLock();
                return this;
            }
            catch (Exception)
            {
                _rwLock.ExitReadLock();
                return null;
            }
        }
        public void EndRead()
        {
            if (_rwLock.IsReadLockHeld)
                _rwLock.ExitReadLock();
        }
        public void AddToast(MyToastContent toast)
        {
            try
            {
                _rwLock.EnterWriteLock();
                base.Add(toast);
            }
            finally
            {
                if (_rwLock.IsWriteLockHeld)
                    _rwLock.ExitWriteLock();
            }
        }
        public async Task SaveToasts()
        {
            Debug.WriteLine("SaveToastsが開始されました。");
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                IndentChars = "\t",
            };
            var localDir = ApplicationData.Current.LocalFolder;

            using (var fileLock = await FileLock.Create(localDir, _xmlFileName, _xmlLockFileName, "MyToastContents.SaveToasts"))
            {
                Debug.Assert(fileLock != null);
                var xmlFile = await localDir.CreateFileAsync(_xmlFileName, CreationCollisionOption.ReplaceExisting); // System.IO.FileLoadException
                using (var writer = XmlWriter.Create(xmlFile.Path, settings))
                {
                    var serializer = new DataContractSerializer(typeof(MyToastContents));
                    serializer.WriteObject(writer, this);
                }
            }
            Debug.WriteLine("SaveToastsを正常に終了します。");
        }

        public async Task<bool> LoadToasts()
        {
            Debug.WriteLine("LoadToastsが開始されました。");
            //_loadToastsCountForDebug++;
            //Debug.WriteLine("LoadToast {0} 回目スタート", _loadToastsCountForDebug);

            var localDir = ApplicationData.Current.LocalFolder;
            
            using (var fileLock = await FileLock.Create(localDir, _xmlFileName, _xmlLockFileName, "MyToastContents.LoadToasts"))
            {
                Debug.Assert(fileLock != null);
                var item = await localDir.TryGetItemAsync(_xmlFileName);
                if (item == null)
                {
                    Debug.WriteLine("XMLトーストファイルが存在しません。(LoadToasts)");
                    return false;
                }
                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    // XmlFileNameがフォルダーである場合、リネーム
                    await item.RenameAsync(_xmlFileName + ".Folder", NameCollisionOption.GenerateUniqueName);
                    return false;
                }

                // デシリアライズ
                var xmlFile = item as StorageFile;
                if (xmlFile == null) return false;

                var properties = await xmlFile.GetBasicPropertiesAsync();
                if (properties.Size == 0)
                {
                    Debug.WriteLine("XMLトーストファイルが空です(LoadToasts)");
                    return true;
                }
                using (var reader = XmlReader.Create(xmlFile.Path))
                {
                    var serializer = new DataContractSerializer(this.GetType());
                    MyToastContents temp = null;
                    try
                    {
                        temp = serializer.ReadObject(reader) as MyToastContents;
                    }
                    catch (SerializationException e)
                    {
                        Debug.WriteLine(_xmlFileName + "の読み込みに失敗しました。");
                        Debug.WriteLine(e.Message);
                        Debug.WriteLine("トーストファイルのバックアップを試みます");
                        await xmlFile.CopyAsync(localDir, DateTimeOffset.Now.ToString("u") + "_" + _xmlFileName);
                        
                        return false;
                    }
                    this.CopyFrom(temp);
                }
            }
            Debug.WriteLine("LoadToastsを正常に終了します。");
            return true;
        }
        /* リストをクリアーしてから、１コンテンツ１ファイルとして保存されたMyToastContentをロード ロードすべきものがなければクリアーしない MyToastContent.LoadFromFileに派生*/
        public async Task LoadNewVersion()
        {
            // Debug.WriteLine("LoadNewVersionを開始しました");
            var localFolder = ApplicationData.Current.LocalFolder;
            using (var fileLock = await FileLock.Create(localFolder, "ToastFiles", null, "MyToastContents.LoadNewVersion")) // ファイルロックは必要？
            {
                Debug.Assert(fileLock != null);
                
                var item = await localFolder.TryGetItemAsync("ToastFiles");
                if (item is StorageFolder folder)
                {
                    var toastFiles = await folder.GetFilesAsync();
                    if (toastFiles != null && toastFiles.Count != 0)
                        Clear();
                    foreach (var toastFile in toastFiles)
                    {
                        if (toastFile.Name.Contains("lock")) continue;
                        var toast = await MyToastContent.LoadFromFile(toastFile);
                        Add(toast);
                    }
                }
            }
            // Debug.WriteLine("LoadNewVersionを終了します");
        }
        private void CopyFrom(MyToastContents myToastContents)
        {
            Debug.Assert(myToastContents != null);
            try
            {
                _rwLock.EnterWriteLock();
                this.Clear();
                foreach (var myToast in myToastContents)
                {
                    this.Add(myToast);
                }
            }
            finally
            {
                if (_rwLock.IsWriteLockHeld)
                    _rwLock.ExitWriteLock();
            }
        }
        public async Task MakeBackup()
        {
            while (_isBackup)
            {
                await Task.Delay(500);
            }
            try
            {
                _isBackup = true;
                var localDir = ApplicationData.Current.LocalFolder;
                var backupFileName = "~Toasts.xml";

                var item = await localDir.TryGetItemAsync(backupFileName);
                if (item is StorageFile backupFile)
                {
                    if (backupFile != null)
                    {
                        string s = "Toasts" + DateTimeOffset.Now.ToString("u") + ".xml";
                        s = s.Replace(':', '-');
                        s = s.Replace(' ', '_');
                        Debug.WriteLine(s);
                        await backupFile.RenameAsync(s);
                    }
                }
                var lockFileInfo = new FileInfo(_xmlLockFileName);
                while (lockFileInfo.Exists)
                {
                    await Task.Delay(500);
                }
                item = await localDir.TryGetItemAsync(_xmlFileName);
                if (item is StorageFile file)
                {
                    await file.CopyAsync(localDir, backupFileName, NameCollisionOption.ReplaceExisting);
                }
            }
            finally
            {
                _isBackup = false;
            }
        }
        // インデックスをリセット、_maxIndexもコンストラクタの中でリセットされる
        public void ResetIndexes()
        {
            // ToastGroup = Index であるため、一旦スケジュールされたトーストをキャンセル
            foreach (var toast in this.Where(x => x.IsEnabled))
                SendToast.CancelAll(toast);

            MyToastContents tempToasts = new MyToastContents();
            int i;
            tempToasts.CopyFrom(this);
            try
            {
                _rwLock.EnterWriteLock();
                this.Clear();
                for (i = 0; i < tempToasts.Count; i++)
                {
                    this.Add(new MyToastContent(tempToasts[i], newIndex: i + 1));
                    this[i].ToggleOn = this[i].IsEnabled;
                }
                //
            }
            finally
            {
                if (_rwLock.IsWriteLockHeld)
                    _rwLock.ExitWriteLock();
            }
        }
        public void Sort()
        {
            var toastArray = this.ToArray();
            Array.Sort(toastArray);
            try
            {
                _rwLock.EnterWriteLock();
                this.Clear();
                for (var i = 0; i < toastArray.Length; i++)
                {
                    this.Add(toastArray[i]);
                }
            }
            finally
            {
                if (_rwLock.IsWriteLockHeld)
                    _rwLock.ExitWriteLock();
            }
        }
    }//class
}//namespace
