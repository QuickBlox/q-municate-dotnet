using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.Models;
using QMunicate.Services;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk.Modules.ChatModule.Models;

namespace QMunicate.ViewModels
{
    public class SendRequestViewModel : ViewModel
    {
        #region Fields

        private int otherUserId;
        private string userName;
        private ImageSource userImage;
        private bool isAdded;

        #endregion

        #region Ctor

        public SendRequestViewModel()
        {
            SendCommand = new RelayCommand(SendCommandExecute, () =>  !IsLoading && !IsAdded);
        }

        #endregion

        #region Properties

        public string UserName
        {
            get { return userName; }
            set { Set(ref userName, value); }
        }

        public ImageSource UserImage
        {
            get { return userImage; }
            set { Set(ref userImage, value); }
        }

        public RelayCommand SendCommand { get; private set; }

        public bool IsAdded
        {
            get { return isAdded; }
            set
            {
                Set(ref isAdded, value);
                SendCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Navigation

        public override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var user = e.Parameter as UserViewModel;
            if (user == null) return;

            IsAdded = QuickbloxClient.ChatXmppClient.Contacts.Any(c => c.UserId == user.UserId);

            UserImage = user.Image;
            UserName = user.FullName;
            otherUserId = user.UserId;
        }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            SendCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Private methods

        private async void SendCommandExecute()
        {
            IsLoading = true;

            var messagesService = ServiceLocator.Locator.Get<IMessageService>();

            bool isSent = await SendContactRequest();
            if (isSent)
            {
                await messagesService.ShowAsync("Sent", "A contact request was sent");
            }
            else
            {
                await messagesService.ShowAsync("Error", "Failed to send a contact request");
            }

            IsLoading = false;
            NavigationService.GoBack();
        }

        private async Task<bool> SendContactRequest()
        {
            var createDialogResponse = await QuickbloxClient.ChatClient.CreateDialogAsync(UserName, DialogType.Private, otherUserId.ToString());
            if (createDialogResponse.StatusCode != HttpStatusCode.Created) return false;

            var privateChatManager = QuickbloxClient.ChatXmppClient.GetPrivateChatManager(otherUserId, createDialogResponse.Result.Id);
            privateChatManager.AddToFriends(true, UserName);

            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            await dialogsManager.ReloadDialogs();

            return true;
        }

        #endregion

    }
}
