using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
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
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.ChatModule.Requests;
using Quickblox.Sdk.Modules.ContentModule;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Quickblox.Sdk.Modules.UsersModule.Requests;

namespace QMunicate.ViewModels
{
    public class SettingsEditViewModel : ViewModel, IFileOpenPickerContinuable
    {
        #region Fields

        private string userName;
        private ImageSource userImage;
        private byte[] newImageBytes;
        private User currentUser;

        #endregion

        #region Ctor

        public SettingsEditViewModel()
        {
            ChangeImageCommand = new RelayCommand(ChangeImageCommandExecute, () => !IsLoading);
            SaveCommand = new RelayCommand(SaveCommandExecute, () => !IsLoading && !string.IsNullOrWhiteSpace(UserName));
            CancelCommand = new RelayCommand(CancelCommandExecute, () => !IsLoading);
        }

        #endregion

        #region Properties

        public ImageSource UserImage
        {
            get { return userImage; }
            set { Set(ref userImage, value); }
        }

        public string UserName
        {
            get { return userName; }
            set
            {
                Set(ref userName, value);
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
            await LoadUserData();
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

                    UserImage = ImageHelper.CreateBitmapImage(streamForImage, 100);
                }
            }
        }

        #endregion

        #region Private methods

        private async Task LoadUserData()
        {
            IsLoading = true;
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            currentUser = await cachingQbClient.GetUserById(SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId));
            if (currentUser != null)
            {
                UserName = currentUser.FullName;
                if (currentUser.BlobId.HasValue)
                {
                    var imageService = ServiceLocator.Locator.Get<IImageService>();
                    UserImage = await imageService.GetPrivateImage(currentUser.BlobId.Value, 100);
                }
            }
            IsLoading = false;
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

            var updateUserRequest = new UpdateUserRequest {User = new UserRequest() {FullName = UserName}};

            if (newImageBytes != null)
            {
                var contentHelper = new ContentClientHelper(QuickbloxClient.ContentClient);
                updateUserRequest.User.BlobId = await contentHelper.UploadPrivateImage(newImageBytes);
            }

            var updateUserResponse = await QuickbloxClient.UsersClient.UpdateUserAsync(currentUser.Id, updateUserRequest);

            if (updateUserResponse.StatusCode == HttpStatusCode.OK)
            {
                var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
                cachingQbClient.DeleteUserFromCacheById(currentUser.Id);

                NavigationService.GoBack();
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
