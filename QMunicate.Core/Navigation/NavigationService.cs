using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.Logger;

namespace QMunicate.Core.Navigation
{
    public class NavigationService : INavigationService
    {
        public event NavigatedEventHandler Navigated;
        public event NavigatingCancelEventHandler Navigating;
        public event NavigationFailedEventHandler NavigationFailed;
        public event NavigationStoppedEventHandler NavigationStopped;

        private Frame frame;
        private PageResolver pageResolver;
        private INavigationAware previousPage;

        public void Initialize(Frame frame, PageResolver pageResolver)
        {
            if (frame == null)
            {
                throw new ArgumentNullException("frame null");
            }

            this.pageResolver = pageResolver;
            this.frame = frame;
            this.frame.Navigated += OnNavigated;
            this.frame.Navigating += OnNavigating;
            this.frame.NavigationFailed += OnNavigationFailed;
            this.frame.NavigationStopped += OnNavigationStopped;
            Debug.WriteLine("Frame initialized");
        }

        private async void OnNavigationStopped(object sender, NavigationEventArgs e)
        {
            await QmunicateLoggerHolder.Log(QmunicateLogLevel.Debug, "OnNavigationStopped. SourcePageType=" + e.SourcePageType + " Mode=" + e.NavigationMode);
            var handler = NavigationStopped;
            if (handler != null)
            {
                handler.Invoke(sender, e);
            }
        }

        private async void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            await QmunicateLoggerHolder.Log(QmunicateLogLevel.Debug, "OnNavigationFailed. SourcePageType=" + e.SourcePageType + "\n Exception=" + e.Exception);
            var handler = NavigationFailed;
            if (handler != null)
            {
                handler.Invoke(sender, e);
            }
        }

        private async void OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            await QmunicateLoggerHolder.Log(QmunicateLogLevel.Debug, "OnNavigating. SourcePageType=" + e.SourcePageType + " Mode=" + e.NavigationMode);
            var handler = Navigating;
            if (handler != null)
            {
                handler.Invoke(sender, e);
            }

            //if (!e.Cancel)
            //{
            //    switch (e.NavigationMode)
            //    {
            //        case NavigationMode.Back:
            //            NavigatedFrom(e);
            //            break;
            //        case NavigationMode.Forward:
            //            break;
            //        case NavigationMode.New:
            //            NavigatedFrom(e);
            //            break;
            //        case NavigationMode.Refresh:
            //            break;
            //    }
            //}
        }

        private async void OnNavigated(object sender, NavigationEventArgs e)
        {
            await QmunicateLoggerHolder.Log(QmunicateLogLevel.Debug, "OnNavigated. SourcePageType=" + e.SourcePageType + " Mode=" + e.NavigationMode);
            var handler = Navigated;
            if (handler != null)
            {
                handler.Invoke(sender, e);
            }

            switch (e.NavigationMode)
            {
                case NavigationMode.Back:
                    NavigatedTo(e);
                    break;
                case NavigationMode.Forward:
                    break;
                case NavigationMode.New:
                    NavigatedTo(e);
                    break;
                case NavigationMode.Refresh:
                    break;
            }
        }

        public bool Navigate(String pageKey)
        {
            var page = this.pageResolver.GetPageByKey(pageKey);
            return this.frame.Navigate(page);
        }

        public bool Navigate(String pageKey, object parameter)
        {
            var page = this.pageResolver.GetPageByKey(pageKey);
            return this.frame.Navigate(page, parameter);
        }

        public void GoBack()
        {
            this.frame.GoBack();
        }

        public bool CanGoBack
        {
            get
            {
                return this.frame.CanGoBack;
            }
        }

        public IList<PageStackEntry> BackStack
        {
            get { return frame.BackStack; }
        }

        public int BackStackDepth
        {
            get { return frame.BackStackDepth; }
        }

        /// <summary>
        /// Called when the implementer is being navigated away from.
        /// </summary>
        /// <param name="args">Provides data for navigation methods and event handlers that cannot cancel the navigation request.</param>
        private void NavigatedFrom(NavigatingCancelEventArgs args)
        {
            //var navigationAware = GetNavigationAware(frame.Content);
            //if (navigationAware == null) return;
            //navigationAware.OnNavigatedFrom(args);
        }

        /// <summary>
        /// Called when the implementer has been navigated to.
        /// </summary>
        /// <param name="args">Provides data for navigation methods and event handlers that cannot cancel the navigation request.</param>
        private void NavigatedTo(NavigationEventArgs args)
        {
            if(previousPage != null)
                previousPage.OnNavigatedFrom(args);

            previousPage = GetNavigationAware(args.Content);
            if (previousPage == null) return;
            previousPage.OnNavigatedTo(args);
        }

        private static INavigationAware GetNavigationAware(Object content)
        {
            if (content == null) return null;
            var frameworkElement = content as FrameworkElement;
            if (frameworkElement == null) return null;
            return frameworkElement.DataContext as INavigationAware;
        }
    }
}
