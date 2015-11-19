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
using QMunicate.Core.Navigation;
using QMunicate.ViewModels;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace QMunicate.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var mainViewModel = this.DataContext as MainViewModel;
            mainViewModel.ContentNavigationService.Initialize(ContentFrame, GetPageResolver());
        }

        private PageResolver GetPageResolver()
        {
            var dictionary = new Dictionary<string, Type>();
            dictionary.Add(ContentViewLocator.PrivateChat, typeof(PrivateChatPage));


            return new PageResolver(dictionary);
        }
    }
}
