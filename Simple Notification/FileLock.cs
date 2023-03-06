/* Createメソッドでロックファイルを作成
 * Disposeまたはデストラクタでロックファイルを削除
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;
using System.IO;

namespace Simple_Notification
{
    internal class FileLock : IDisposable
    {
        private const long _timeLimitForWaitLock = 60000; // ミリ秒
        private StorageFile _file;
        private FileLock(StorageFile file)
        {
            _file = file;
        }
		~FileLock()
		{
            // Debug.WriteLine("FileLockデストラクタ");
            var task = Dispose(false);
            task.Wait(500);
		}
        public void Dispose()
        {
            // Debug.WriteLine("FileLock.Dispose()");
            var task = Dispose(true);
            GC.SuppressFinalize(this);
            task.Wait(500);
        }
        protected virtual async Task Dispose(bool disposing)
		{
            FileInfo fileInfo = new FileInfo(_file.Path);
            if (fileInfo.Exists)
            {
                await _file.DeleteAsync();
            }
            else
                Debug.WriteLine("FileLock.Disposeが呼ばれましたが、ロックファイルは存在しませんでした");
        }
        internal static async Task<FileLock> Create(StorageFolder folder, string fileName, string lockFileName = null, string fileContent = null)
        {
            if (folder == null || fileName == null)
            {
                throw new ArgumentNullException();
            }
            if (fileName == "")
                throw new ArgumentException();
            if (lockFileName == null)
                lockFileName = fileName + ".lock";

            /* ロックファイルの存在確認、削除 */
            FindLockFile:
            var fInfo = new FileInfo(Path.Combine(folder.Path, lockFileName));
            //var fileLock = new FileLock(fInfo);

            // if (fInfo.Exists) Debug.WriteLine("ロックファイルが存在します。待機します。(FileLock.Create)");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (fInfo.Exists)
            {
                await Task.Delay(1000);
                //IStorageItem item = await folder.TryGetItemAsync(_xmlLockFileName);

                var item = await folder.TryGetItemAsync(lockFileName);
                if (item == null) break;
                var file = (StorageFile)item;
                var dataCreated = file.DateCreated;
                if (DateTimeOffset.Now - dataCreated > new TimeSpan(0, 3, 0))
                {
                    await file.DeleteAsync();
                    Debug.WriteLine("古いロックファイルを削除しました。(FileLock.Create)");
                    break;
                }
                /*if (stopWatch.ElapsedMilliseconds > _timeLimitForWaitLock)
                {
                    Debug.WriteLine("{0} ミリ秒を過ぎてもファイルのロックが解除されませんでした。ロックファイルの作成に失敗しました。(FileLock.Create)", _timeLimitForWaitLock);
                    return null;
                }*/
            }

            /* ロックファイルの作成 */
            StorageFile lockFile;
            try
            {
                // Debug.WriteLine("この直後にロックファイルを作成します。（FileLock.Create）");
                lockFile = await folder.CreateFileAsync(lockFileName, CreationCollisionOption.FailIfExists);
                // Debug.WriteLine("ロックファイルを作成しました。(FileLock.Create)");
            }
            catch (Exception)
            {
                Debug.WriteLine("ロックファイルの作成に失敗しました。待機します。(FileLock.Create)");
                goto FindLockFile;
            }
            /* ロックファイルへの書き込み */
            if (fileContent != null)
            {
                await FileIO.WriteTextAsync(lockFile, fileContent);
            }
            return new FileLock(lockFile);
        }
    }
}
