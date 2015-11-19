using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace QMunicate.Core.Navigation
{
    public interface INavigationService
    {
        event NavigatedEventHandler Navigated;
        event NavigatingCancelEventHandler Navigating;
        event NavigationFailedEventHandler NavigationFailed;
        event NavigationStoppedEventHandler NavigationStopped;
        bool CanGoBack { get; }
        IList<PageStackEntry> BackStack { get; }
        int BackStackDepth { get; }
        void Initialize(Frame frame, PageResolver pageResolver);
        bool Navigate(String pageKey);
        bool Navigate(String pageKey, object parameter);
        void GoBack();
    }
}