using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Helper;
using QMunicate.Models;
using QMunicate.Services;
using QMunicate.ViewModels.PartialViewModels;
using Quickblox.Sdk.Modules.ChatModule.Requests;
using Quickblox.Sdk.Modules.ContentModule;

namespace QMunicate.ViewModels
{
    public class GroupEditViewModel : ViewModel, IFileOpenPickerContinuable
    {
        #region Fields

        private string chatName;
        private ImageSource chatImage;
        private DialogViewModel currentDialog;
        private byte[] newImageBytes;

        #endregion

        #region Ctor

        public GroupEditViewModel()
        {
            ChangeImageCommand = new RelayCommand(ChangeImageCommandExecute, () => !IsLoading);
            SaveCommand = new RelayCommand(SaveCommandExecute, () => !IsLoading && !string.IsNullOrWhiteSpace(ChatName));
            CancelCommand = new RelayCommand(CancelCommandExecute, () => !IsLoading);
        }

        #endregion

        #region Properties

        public ImageSource ChatImage
        {
            get { return chatImage; }
            set { Set(ref chatImage, value); }
        }

        public string ChatName
        {
            get { return chatName; }
            set
            {
                Set(ref chatName, value);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand ChangeImageCommand { get; private set; }

        public RelayCommand SaveCommand { get; private set; }

        public RelayCommand CancelCommand { get; set; }

        #endregion

        #region Navigation

        public async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var dialog = e.Parameter as DialogViewModel;
            if (dialog == null) return;

            currentDialog = dialog;
            await Initialize(dialog);
        }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            ChangeImageCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region IFileOpenPickerContinuable Members

        public async void ContinueFileOpenPicker(IReadOnlyList<StorageFile> files)
        {
            if (files != null && files.Any())
            {
                var stream = (FileRandomAccessStream)await files[0].OpenAsync(FileAccessMode.Read);
                using (var streamForImage = stream.CloneStream())
                {
                    newImageBytes = new byte[stream.Size];
                    using (var dataReader = new DataReader(stream))
                    {
                        await dataReader.LoadAsync((uint)stream.Size);
                        dataReader.ReadBytes(newImageBytes);
                    }

                    ChatImage = ImageHelper.CreateBitmapImage(streamForImage, 100);
                }
            }
        }

        #endregion

        #region Private methods

        private async Task Initialize(DialogViewModel dialog)
        {
            ChatName = dialog.Name;
            ChatImage = dialog.Image;
        }

        private async void ChangeImageCommandExecute()
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
#if WINDOWS_PHONE_APP
            picker.PickSingleFileAndContinue();
#endif
        }

        private async void SaveCommandExecute()
        {
            IsLoading = true;
            var updateDialogRequest = new UpdateDialogRequest { DialogId = currentDialog.Id };

            if (newImageBytes != null)
            {
                var contentHelper = new ContentClientHelper(QuickbloxClient.ContentClient);
                updateDialogRequest.PhotoLink = await contentHelper.UploadPublicImage(newImageBytes);
            }

            if (currentDialog.Name != ChatName)
            {
                updateDialogRequest.Name = ChatName;
            }

            if (!string.IsNullOrEmpty(updateDialogRequest.PhotoLink) || !string.IsNullOrEmpty(updateDialogRequest.Name))
            {
                var updateDialogResponse = await QuickbloxClient.ChatClient.UpdateDialogAsync(updateDialogRequest);

                if (updateDialogResponse.StatusCode == HttpStatusCode.OK)
                {
                    var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
                    var dialog = dialogsManager.Dialogs.FirstOrDefault(d => d.Id == currentDialog.Id);

                    var groupChatManager = QuickbloxClient.ChatXmppClient.GetGroupChatManager(currentDialog.XmppRoomJid, currentDialog.Id);

                    if (!string.IsNullOrEmpty(updateDialogRequest.Name))
                    {
                        dialog.Name = updateDialogRequest.Name;
                        groupChatManager.NotifyGroupNameChanged(updateDialogRequest.Name);
                    }

                    if (!string.IsNullOrEmpty(updateDialogRequest.PhotoLink))
                    {
                        dialog.Photo = updateDialogRequest.PhotoLink;
                        dialog.Image = ChatImage;
                        groupChatManager.NotifyGroupImageChanged(updateDialogRequest.PhotoLink);
                    }
                    
                    IsLoading = false;
                    NavigationService.GoBack();
                }
            }

            IsLoading = false;
        }

        private async void CancelCommandExecute()
        {
            NavigationService.GoBack();
        }

        #endregion
    }
}
