using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Simple_Notification
{
    public abstract class BasePage : Page
    {
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("BasePage.OnNavigatedTo");
            base.OnNavigatedTo(e);
        }

        protected void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected == true)
            {
                NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
            }
            else if (args.SelectedItemContainer != null)
            {
                if (args.SelectedItemContainer.Tag == null)
				{
                    Debug.WriteLine(args.SelectedItemContainer.Content);
				}
                var navItemTag = args.SelectedItemContainer.Tag.ToString(); // TagがNULLになって例外 なぜ？
                NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }
        protected void NavView_Navigate(string navItemTag, Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo)
        {
            Type _page = null;
            if (navItemTag == "settings")
            {
                _page = typeof(SettingsPage);
            }
            else if (navItemTag == "home")
            {
                _page = typeof(MainPage);
            }
            else if (navItemTag == "debug")
            {
                _page = typeof(DebugPage);
            }
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            var preNavPageType = Frame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (!(_page is null) && !Type.Equals(preNavPageType, _page))
            {
                Frame.Navigate(_page, null, transitionInfo);
            }
        }

        protected void On_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            bool isXButton1Pressed =
                e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.XButton1Pressed;

            if (isXButton1Pressed)
            {
                e.Handled = On_BackRequested();
            }
        }
        protected void BackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            On_BackRequested();
            args.Handled = true;
        }
        protected void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            On_BackRequested();
        }
        protected virtual bool On_BackRequested()
        {
            Debug.WriteLine("ToastAddPage.On_BackRequested");
            // 参考URL: https://docs.microsoft.com/ja-jp/windows/uwp/design/controls-and-patterns/navigationview#backwards-navigation
            if (!this.Frame.CanGoBack)
                return false;

            this.Frame.GoBack();
            return true;
        }
    }
}
