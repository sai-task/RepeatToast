using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
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
using System.Collections.ObjectModel;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Popups;
using System.Xml;
using System.Runtime.Serialization;
using System.Text;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace Simple_Notification
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>

    // Category 何のために作ったか覚えてない
    public class Category
    {
        public String Name { get; set; }
        public String CategoryIcon { get; set; }
        public ObservableCollection<Category> Children { get; set; }
    }

    public sealed partial class MainPage : BasePage
    {
        private const string _backgroundTaskName = "CyclicToasts";

        public MainPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Debug.WriteLine("MainPage.OnNavigatedTo");
            // 選択されていない状態にする
            ToastLV.SelectedIndex = -1;
            foreach (var t in App.GetMyToastContents())
                t.IsSelected = false;
            // イベントの登録

            base.OnNavigatedTo(e);
        }

        private void ToastLV_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var toast = ToastLV.SelectedItem as MyToastContent;
            if (toast == null)
            {
                Debug.WriteLine("toast is null (in MainPage ToastLV_SelectionChanged)");
                return;
            }
            foreach (var t in App.GetMyToastContents())
                t.IsSelected = false;
            toast.IsSelected = true;
        }

        /* AppBarButton クリックイベント */
        private void AddAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ToastAddPage));
        }
        private void EditAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var toast = ToastLV.SelectedItem as MyToastContent;
            if (toast == null) return;
            this.Frame.Navigate(typeof(ToastEditPage), toast);
        }
        private async void DeleteAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var toast = ToastLV.SelectedItem as MyToastContent;
            if (toast == null) return;
            var content = "選択された項目を削除してよろしいですか？\n";
            content += toast.Options?.Title;
            ContentDialog dialog = new ContentDialog()
            {
                Content = content,
                PrimaryButtonText = "削除",
                CloseButtonText = "キャンセル"
            };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
                await DeleteToast(toast);
        }

        /* コンテキストメニュー */
        private void EditMenu_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(sender.GetType());

            var item = sender as MenuFlyoutItem;
            var toast = item?.Tag as MyToastContent;
            if (toast == null)
            {
                Debug.WriteLine("toast is null (in MainPage EditMenu_Click)");
                return;
            }
            this.Frame.Navigate(typeof(ToastEditPage), toast);
        }

        private async void DeleteMenu_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuFlyoutItem;
            var toast = item?.Tag as MyToastContent;
            if (toast == null)
            {
                Debug.WriteLine("toast is null (in MainPage DeleteMenu_Click)");
                return;
            }
            var content = "この項目を削除してよろしいですか？\n";
            content += toast.Options?.Title;
            ContentDialog dialog = new ContentDialog()
            {
                Content = content,
                PrimaryButtonText = "削除",
                CloseButtonText = "キャンセル"
            };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
                await DeleteToast(toast);
        }
        private async Task DeleteToast(MyToastContent toast)
        {
            //トーストのキャンセル
            toast.SwitchOff(saveFlag: false);

            var toasts = App.GetMyToastContents();
            _ = toasts.Remove(toast);

            // ファイルの削除
            var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("ToastFiles");
            var item = await folder.TryGetItemAsync(toast.Index + ".xml");
            if (item is StorageFile file)
            {
                await file.DeleteAsync();
            }
            else
            {
                string s = toast.Index + ".xml は存在しません(MainPage.DeleteToast)";
                Debug.WriteLine(s);
            }
            // 必要ならバックグラウンドタスクの解除
            if (toasts.Any(x => x.Options == null))
            {
                Debug.WriteLine("MyToastContentのOptions.getがnull (MainPage.DeleteToast)");
                return;
            } // 例外回避のため(コードが全て正常なら起こらない？)
            var cyclicToastIsExist = toasts.Any(x => x.Options.RType != RepeatType.once);  // 例外 Options.getがnull
            if (!cyclicToastIsExist)
            {
                var tasks = BackgroundTaskRegistration.AllTasks.Where(x => x.Value.Name == _backgroundTaskName);
                foreach (var task in tasks)
                    task.Value.Unregister(cancelTask: true);
            }
        }
        private void PropertyMenu_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
            //FlyoutText.Text = "abcdefg";
            //FlyoutBase.ShowAttachedFlyout((FrameworkElement)ContentFrame);
            //split_view.IsPaneOpen = true;
        }

        /* テスト用 色のテスト */
        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            //MyToastContent._enabledAndSelectedColor = (sender as ColorPicker).Color.ToString();
        }

        private void ColorPicker2_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            //MyToastContent._enabledAndNotSelectedColor = (sender as ColorPicker).Color.ToString();
        }

        private void ColorPicker3_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            //MyToastContent._disabledAndSelectedColor = (sender as ColorPicker).Color.ToString();
        }

        private void ColorPicker4_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            //MyToastContent._disabledAndNotSelectedColor = (sender as ColorPicker).Color.ToString();
        }
        /*
        public ObservableCollection<Category> Categories = new ObservableCollection<Category>()
        {
        new Category(){
            Name = "Other",
            CategoryIcon = "Icon",
            Children = new ObservableCollection<Category>() {
                new Category(){
                    Name = "Backup",
                    CategoryIcon = "Icon",
                },
                new Category()
                {
                    Name = "Restore",
                    CategoryIcon = "Icon"
                }
            }
        },
        };
        */

        /* バックアップ 現在のリストを一つのファイルに保存する */
        private async void BackupAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileSavePicker()
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
            };
            picker.FileTypeChoices.Add("XML", new List<string>() { ".xml" });
            picker.SuggestedFileName = "Toasts" + DateTime.Now.ToString("yyyyMMdd") + ".xml";
            var destinationFile = await picker.PickSaveFileAsync();

            if (destinationFile == null)
            {
                Debug.WriteLine("選択されたファイルがnullです (MainPage.BackupAppBarButton_Click)");
                return;
            }

            var settings = new XmlWriterSettings
            {
                Encoding = new System.Text.UTF8Encoding(false),
                Indent = true,
                IndentChars = "\t",
            };
            var stream = await destinationFile.OpenStreamForWriteAsync();
            using (var writer = XmlWriter.Create(stream, settings))
            {
                var serializer = new DataContractSerializer(typeof(MyToastContents));
                serializer.WriteObject(writer, App.GetMyToastContents());
            }
        }

        /* バックアップで保存された1つのファイルからリストア（現在のトーストリストに追加） 一旦tempフォルダに移動 */
        private async void RestoreAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            picker.FileTypeFilter.Add(".xml");
            picker.FileTypeFilter.Add("*");

            var sourceFile = await picker.PickSingleFileAsync();
            if (sourceFile != null)
            {
                var properties = await sourceFile.GetBasicPropertiesAsync();
                if (properties.Size == 0)
                {
                    Debug.WriteLine("ファイルが空です(RestoreAppBarButton_Click)");
                    var dialog = new MessageDialog("ファイルが空です。");
                    await dialog.ShowAsync();
                    return;
                }
                var tempFolder = ApplicationData.Current.TemporaryFolder;
                var tempFile = await sourceFile.CopyAsync(tempFolder, sourceFile.Name, NameCollisionOption.GenerateUniqueName);
                MyToastContents restoredToasts = null;
                using (var reader = XmlReader.Create(tempFile.Path))
                {
                    var serializer = new DataContractSerializer(typeof(MyToastContents));
                    try
                    {
                        restoredToasts = serializer.ReadObject(reader) as MyToastContents;
                        // Debug.WriteLine("ファイルの読み込みに成功しました。(MainPage.RestoreAppBarButton)");
                    }
                    catch (SerializationException)
                    {
                        // Debug.WriteLine("ファイルの読み込みに失敗しました。(MainPage.RestoreAppBarButton)");
                        var dialog = new MessageDialog("ファイルの読み込みに失敗しました。");
                        await dialog.ShowAsync();
                        return;
                    }
                }
                // 復元されたトーストをインデックスを変更してOFFにしてリストに追加してファイル保存
                foreach (var toast in restoredToasts)
                {
                    var newToast = new MyToastContent(toast, await ToastAddPage.GetUniqueIndex());
                    App.GetMyToastContents().Add(newToast);
                    newToast.SwitchOff();
                    await newToast.AddSave();
                }
            }
            else
            {
                Debug.WriteLine("fileがnullです(MainPage.RestoreAppBarButton)");
                return;
            }
        }
    }
}
