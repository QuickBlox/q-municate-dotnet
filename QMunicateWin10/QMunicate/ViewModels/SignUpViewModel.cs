using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.Models;
using Quickblox.Sdk;
using Quickblox.Sdk.GeneralDataModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using QMunicate.Core.Logger;
using Quickblox.Sdk.Logger;
using Quickblox.Sdk.Modules.ContentModule;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Quickblox.Sdk.Modules.UsersModule.Requests;

namespace QMunicate.ViewModels
{
    public class SignUpViewModel : ViewModel //IFileOpenPickerContinuable
    {
        #region Fields

        private string fullName;
        private string email;
        private string password;
        private byte[] userImageBytes;
        private ImageSource userImage;
        private readonly IMessageService messageService;

        #endregion

        #region Ctor

        public SignUpViewModel()
        {
            messageService = ServiceLocator.Locator.Get<IMessageService>();

            ChoosePhotoCommand = new RelayCommand(ChoosePhotoCommandExecute, () => !IsLoading);
            SignUpCommand = new RelayCommand(SignUpCommandExecute, () => !IsLoading);
            DeleteUserImageCommand = new RelayCommand(DeleteUserImageCommandExecute, () => !IsLoading);
            BackCommand = new RelayCommand(() => NavigationService.GoBack());
        }

        #endregion

        #region Properties

        public string FullName
        {
            get { return fullName; }
            set { Set(ref fullName, value); }
        }

        public string Email
        {
            get { return email; }
            set { Set(ref email, value); }
        }

        public string Password
        {
            get { return password; }
            set { Set(ref password, value); }
        }

        public ImageSource UserImage
        {
            get { return userImage; }
            set { Set(ref userImage, value); }
        }

        public RelayCommand ChoosePhotoCommand { get; set; }

        public RelayCommand SignUpCommand { get; set; }

        public RelayCommand DeleteUserImageCommand { get; set; }

        public RelayCommand BackCommand { get; set; }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            ChoosePhotoCommand.RaiseCanExecuteChanged();
            SignUpCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Public methods

        public async void ContinueFileOpenPicker(IReadOnlyList<StorageFile> files)
        {
            if (files != null && files.Any())
            {
                var stream = (FileRandomAccessStream) await files[0].OpenAsync(FileAccessMode.Read);
                var streamForImage = stream.CloneStream();
                
                userImageBytes = new byte[stream.Size];
                using (var dataReader = new DataReader(stream))
                {
                    await dataReader.LoadAsync((uint)stream.Size);
                    dataReader.ReadBytes(userImageBytes);
                }

                UserImage = ImageHelper.CreateBitmapImage(streamForImage, 100);
            }
        }

        #endregion

        #region Private methods

        private async void ChoosePhotoCommandExecute()
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
#if WINDOWS_PHONE_APP
            picker.PickSingleFileAndContinue();
#endif
        }

        private async void SignUpCommandExecute()
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await messageService.ShowAsync("Message", "Please fill all empty input fields");
                return;
            }

            if (!Helpers.IsInternetConnected())
            {
                await messageService.ShowAsync("Connection failed", "Please check your internet connection.");
                return;
            }

            IsLoading = true;

            await CreateSession();

            var userSignUpRequest = new UserSignUpRequest
            {
                User = new UserRequest()
                {
                    Email = email,
                    FullName = fullName,
                    Password = password
                }
            };

            var response = await QuickbloxClient.UsersClient.SignUpUserAsync(userSignUpRequest);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                int? userId = await Login();

                if (userId != null)
                {
                    if (userImageBytes != null)
                        await UploadUserImage(response.Result.User, userImageBytes);

                    NavigationService.Navigate(ViewLocator.Main,
                                                    new DialogsNavigationParameter
                                                    {
                                                        CurrentUserId = userId.Value,
                                                        Password = Password
                                                    });
                }
            }
            else await Helpers.ShowErrors(response.Errors, messageService);

            IsLoading = false;
        }

        private async Task CreateSession()
        {
            if (!string.IsNullOrEmpty(QuickbloxClient.Token)) return;

            var sessionResponse = await QuickbloxClient.AuthenticationClient.CreateSessionBaseAsync(new DeviceRequest() { Platform = Platform.windows_phone, Udid = Helpers.GetHardwareId() });
            if (sessionResponse.StatusCode != HttpStatusCode.Created)
            {
                await Helpers.ShowErrors(sessionResponse.Errors, messageService);
            }
        }

        private async Task<int?> Login()
        {
            var loginResponse = await QuickbloxClient.AuthenticationClient.ByEmailAsync(Email, Password);
            if (loginResponse.StatusCode == HttpStatusCode.Accepted)
            {
                SettingsManager.Instance.WriteToSettings(SettingsKeys.CurrentUserId, loginResponse.Result.User.Id);
                return loginResponse.Result.User.Id;
            }
            else
            {
                await Helpers.ShowErrors(loginResponse.Errors, messageService);
                return null;
            }
        }

        private async Task UploadUserImage(User user, byte[] imageBytes)
        {
            var contentHelper = new ContentClientHelper(QuickbloxClient.ContentClient);
            var uploadId = await contentHelper.UploadPrivateImage(imageBytes);
            if (uploadId == null)
            {
                await QmunicateLoggerHolder.Log(QmunicateLogLevel.Warn, "SignUpViewModel. Failed to upload user image");
                return;
            }

            UpdateUserRequest updateUserRequest = new UpdateUserRequest { User = new UserRequest { BlobId = uploadId } };
            await QuickbloxClient.UsersClient.UpdateUserAsync(user.Id, updateUserRequest);
        }

        private void DeleteUserImageCommandExecute()
        {
            userImageBytes = null;
            UserImage = null;
        }

        #endregion

    }
}
