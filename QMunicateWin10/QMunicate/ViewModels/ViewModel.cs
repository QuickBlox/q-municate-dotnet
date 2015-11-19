using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Navigation;
using QMunicate.Core.Observable;
using Quickblox.Sdk;

namespace QMunicate.ViewModels
{
    /// <summary>
    /// Interface for ViewModels for UserControls that are a part of a page
    /// </summary>
    public interface IUserControlViewModel
    {
        /// <summary>
        /// Initializes a ViewModel.
        /// Is analogous to OnNavigated.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        Task Initialize(object parameter);
    }


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
