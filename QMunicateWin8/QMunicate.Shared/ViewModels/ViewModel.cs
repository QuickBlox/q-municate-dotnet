using System;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Navigation;
using QMunicate.Core.Observable;
using Quickblox.Sdk;

namespace QMunicate.ViewModels
{
    public class ViewModel : ObservableObject, INavigationAware
    {
        private Boolean isLoading;

        public ViewModel()
        {
            this.NavigationService = ServiceLocator.Locator.Get<INavigationService>();
            this.QuickbloxClient = ServiceLocator.Locator.Get<IQuickbloxClient>();

            this.GoBackCommand = new RelayCommand(this.GoBackCommandExecute, this.CanGobackCommandExecute);
        }
        
        public Boolean IsLoading
        {
            get { return this.isLoading; }
            set
            {
                if(Set(ref this.isLoading, value))
                    OnIsLoadingChanged(); 
            }
        }

        public RelayCommand GoBackCommand { get; set; }

        public INavigationService NavigationService { get; protected set; }

        public IQuickbloxClient QuickbloxClient { get; protected set; }


        private void GoBackCommandExecute()
        {
            this.NavigationService.GoBack();
        }
        
        private bool CanGobackCommandExecute()
        {
            return this.NavigationService.CanGoBack;
        }

        protected virtual void OnIsLoadingChanged() { }

        public virtual void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        public virtual void OnNavigatedFrom(NavigationEventArgs e)
        {
        }
    }
}
