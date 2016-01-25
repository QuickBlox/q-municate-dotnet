using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Helper;
using QMunicate.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using QMunicate.Services;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;

namespace QMunicate.ViewModels
{
    public class GroupInfoViewModel : ViewModel
    {
        #region Fields

        private string chatName;
        private ImageSource chatImage;
        private DialogViewModel currentDialog;

        #endregion

        #region Ctor

        public GroupInfoViewModel()
        {
            Participants = new ObservableCollection<UserViewModel>();
            AddMembersCommand = new RelayCommand(AddMembersCommandExecute, () => !IsLoading);
            EditCommand = new RelayCommand(EditCommandExecute, () => !IsLoading);
        }

        #endregion

        #region Properties

        public string ChatName
        {
            get { return chatName; }
            set { Set(ref chatName, value); }
        }

        public ImageSource ChatImage
        {
            get { return chatImage; }
            set { Set(ref chatImage, value); }
        }

        public ObservableCollection<UserViewModel> Participants { get; set; }

        public RelayCommand AddMembersCommand { get; set; }

        public RelayCommand EditCommand { get; set; }

        #endregion

        #region Navigation

        public async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var dialogId = e.Parameter as string;
            if (dialogId == null) return;

            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            currentDialog = dialogsManager.Dialogs.FirstOrDefault(d => d.Id == dialogId);
            if(currentDialog != null)
                await Initialize(currentDialog);
        }

        public async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            foreach (UserViewModel userVm in Participants)
            {
                userVm.Image = null;
            }
        }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            AddMembersCommand.RaiseCanExecuteChanged();
            EditCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Private methods

        private async Task Initialize(DialogViewModel dialog)
        {
            IsLoading = true;
            ChatName = dialog.Name;
            ChatImage = dialog.Image;

            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var imagesService = ServiceLocator.Locator.Get<IImageService>();

            foreach (int occupantId in dialog.OccupantIds)
            {
                var user = await cachingQbClient.GetUserById(occupantId);
                if (user != null)
                    Participants.Add(UserViewModel.FromUser(user));
            }

            foreach (var userViewModel in Participants)
            {
                userViewModel.IsOnline = QuickbloxClient.ChatXmppClient.Presences.Any(p => p.UserId == userViewModel.UserId && (p.PresenceType == PresenceType.None || p.PresenceType == PresenceType.Subscribed));
                var user = await cachingQbClient.GetUserById(userViewModel.UserId);
                if (user?.LastRequestAt != null)
                {
                    userViewModel.LastActive = user.LastRequestAt.Value;
                }
            }

            foreach (UserViewModel userVm in Participants)
            {
                if (userVm.ImageUploadId.HasValue)
                {
                    userVm.Image = await imagesService.GetPrivateImage(userVm.ImageUploadId.Value, 100);
                }
            }

            IsLoading = false;
        }

        private void AddMembersCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.GroupAddMember, currentDialog);
        }

        private void EditCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.GroupEdit, currentDialog);
        }

        #endregion

    }
}
