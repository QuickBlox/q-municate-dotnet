using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Windows.Input;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using Quickblox.Sdk;
using Quickblox.Sdk.GeneralDataModel.Models;

namespace QMunicate.ViewModels
{
    public class ForgotPasswordViewModel : ViewModel
    {
        #region Fields

        private string email;

        #endregion

        #region Ctor

        public ForgotPasswordViewModel()
        {
            ResetCommand = new RelayCommand(ResetCommandExecute, () => !IsLoading);
            BackCommand = new RelayCommand(() => NavigationService.GoBack());
        }

        #endregion

        #region Properties

        public string Email
        {
            get { return email; }
            set { Set(ref email, value); }
        }

        public RelayCommand ResetCommand { get; private set; }

        public RelayCommand BackCommand { get; set; }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            ResetCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Private methods

        private async void ResetCommandExecute()
        {
            var messageService = ServiceLocator.Locator.Get<IMessageService>();

            if (string.IsNullOrWhiteSpace(Email))
            {
                await messageService.ShowAsync("Message", "Please fill all empty input fields");
                return;
            }

            if (!Helpers.IsInternetConnected())
            {
                await messageService.ShowAsync("Connection failed", "Please check your internet connection.");
                return;
            }

            var sessionResponse = await QuickbloxClient.AuthenticationClient.CreateSessionBaseAsync(new DeviceRequest() { Platform = Platform.windows_phone, Udid = Helpers.GetHardwareId() });

            if (sessionResponse.StatusCode != HttpStatusCode.Created)
            {
                await Helpers.ShowErrors(sessionResponse.Errors, messageService);
                return;
            }

            var response = await QuickbloxClient.UsersClient.ResetUserPasswordByEmailAsync(Email);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                await messageService.ShowAsync("Reset", "A link to reset your password was sent to your email.");
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                await messageService.ShowAsync("Not found", "The user with this email wasn't found.");
            }
            else await Helpers.ShowErrors(response.Errors, messageService);
        }

        #endregion

    }
}
