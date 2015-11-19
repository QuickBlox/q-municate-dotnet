using Windows.UI.Xaml.Navigation;

namespace QMunicate.Core.Navigation
{
    public interface INavigationAware
    {
        void OnNavigatedTo(NavigationEventArgs e);

        void OnNavigatedFrom(NavigationEventArgs e);
    }
}
