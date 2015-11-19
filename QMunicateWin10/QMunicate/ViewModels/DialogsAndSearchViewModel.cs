using System;
using System.Threading.Tasks;
using QMunicate.Core.Command;
using QMunicate.ViewModels.PartialViewModels;

namespace QMunicate.ViewModels
{
    public class DialogsAndSearchViewModel : ViewModel, IUserControlViewModel
    {
        #region Fields

        private bool isSearchMode;
        private DialogViewModel selectedDialog;

        #endregion

        #region Events

        public event EventHandler<string> SelectedDialogChanged;

        #endregion

        #region Ctor

        public DialogsAndSearchViewModel()
        {
            DialogsViewModel = new DialogsViewModel();
            SearchViewModel = new SearchViewModel();

            SearchViewModel.SearchTextChanged += SearchViewModelOnSearchTextChanged;
            DialogsViewModel.SelectedDialogChanged += DialogsViewModelOnSelectedDialogChanged;

            NewGroupCommand = new RelayCommand(NewGroupCommandExecute, () => !IsLoading);
            SettingsCommand = new RelayCommand(SettingsCommandExecute, () => !IsLoading);
        }

       

        #endregion

        #region Properties

        public DialogViewModel SelectedDialog
        {
            get { return selectedDialog; }
            set
            {
                if(Set(ref selectedDialog, value))
                    SelectedDialogChanged?.Invoke(this, selectedDialog?.Id);
            }
        }

        public DialogsViewModel DialogsViewModel { get; set; }

        public SearchViewModel SearchViewModel { get; set; }

        public RelayCommand NewGroupCommand { get; set; }

        public RelayCommand SettingsCommand { get; set; }

        public bool IsSearchMode
        {
            get { return isSearchMode; }
            set { Set(ref isSearchMode, value); }
        }

        #endregion

        #region Base members

        public async Task Initialize(object parameter)
        {
            await DialogsViewModel.Initialize(parameter);
        }

        #endregion

        #region Private methods

        private void DialogsViewModelOnSelectedDialogChanged(object sender, string dialogId)
        {
            SelectedDialogChanged?.Invoke(this, dialogId);
        }

        private void SearchViewModelOnSearchTextChanged(object sender, string searchText)
        {
            if (!string.IsNullOrEmpty(searchText) && !IsSearchMode)
                IsSearchMode = true;

            if (string.IsNullOrEmpty(searchText) && IsSearchMode)
                IsSearchMode = false;
        }

        private void NewGroupCommandExecute()
        {

        }

        private void SettingsCommandExecute()
        {

        } 
        #endregion
    }
}
