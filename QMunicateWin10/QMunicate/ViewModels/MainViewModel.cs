using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Navigation;
using QMunicate.Models;
using QMunicate.Services;
using Quickblox.Sdk.Modules.ChatModule.Models;

namespace QMunicate.ViewModels
{
    public class MainViewModel : ViewModel
    {
        #region Ctor

        public MainViewModel()
        {
            DialogsAndSearchViewModel = new DialogsAndSearchViewModel();
            DialogsAndSearchViewModel.SelectedDialogChanged += DialogsAndSearchViewModelOnSelectedDialogChanged;

            ContentNavigationService = new NavigationService();
        }

        

        #endregion

        #region Properties

        public DialogsAndSearchViewModel DialogsAndSearchViewModel { get; set; }

        /// <summary>
        /// NavigationService for Content part of a SplitView on MainPage.
        /// </summary>
        public INavigationService ContentNavigationService { get; set; }

        #endregion

        #region Navigation

        public async override void OnNavigatedTo(NavigationEventArgs e)
        {
            IsLoading = true;
            await DialogsAndSearchViewModel.Initialize(e.Parameter);

            // this is a testCode
            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            var dialog = dialogsManager.Dialogs.FirstOrDefault(d => d.DialogType == DialogType.Private);


            ContentNavigationService.Navigate(ContentViewLocator.PrivateChat, new ChatNavigationParameter() {Dialog = dialog});
            IsLoading = false;
        }

        #endregion

        #region Private methods

        private void DialogsAndSearchViewModelOnSelectedDialogChanged(object sender, string dialogId)
        {
            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            var dialog = dialogsManager.Dialogs.FirstOrDefault(d => d.Id == dialogId);

            ContentNavigationService.Navigate(ContentViewLocator.PrivateChat, new ChatNavigationParameter() {Dialog = dialog});
        } 

        #endregion
    }
}
