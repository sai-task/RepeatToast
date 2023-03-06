using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;

namespace Simple_Notification
{
    internal class ToastsReader
    {
        internal async Task<MyToastContents> Load(StorageFolder folder, string fileName)
        {
            Debug.WriteLine("ToastsReader.Loadが開始されました。");

            var localDir = ApplicationData.Current.LocalFolder;

            using (var fileLock = await FileLock.Create(localDir, fileName, null, "ToastsReader.Load"))
            {
                Debug.Assert(fileLock != null);
                var toastsFile = await folder.GetFileAsync(fileName);

                // デシリアライズ
                var properties = await toastsFile.GetBasicPropertiesAsync();
                if (properties.Size == 0)
                {
                    Debug.WriteLine("XMLトーストファイルが空です(ToastsReader.Load)");
                    return null;
                }
                using (var reader = XmlReader.Create(toastsFile.Path))
                {
                    var serializer = new DataContractSerializer(typeof(MyToastContents));
                    MyToastContents temp = null;
                    temp = serializer.ReadObject(reader) as MyToastContents;
                    return temp;
                }
            }
        }
    }
}
