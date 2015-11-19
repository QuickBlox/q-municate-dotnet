﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Windows.Input;
using Windows.Security.Credentials;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.Models;
using Quickblox.Sdk;
using Quickblox.Sdk.GeneralDataModel.Models;

namespace QMunicate.ViewModels
{
    public class LoginViewModel : ViewModel
    {
        #region Fields

        private string email;
        private string password;
        private bool rememberMe;

        #endregion

        #region Ctor

        public LoginViewModel()
        {
            ForgotPasswordCommand = new RelayCommand(ForgotPasswordCommandExecute, () => !IsLoading);
            LoginCommand = new RelayCommand(LoginCommandExecute, () => !IsLoading);
            BackCommand = new RelayCommand(() => NavigationService.GoBack());
#if DEBUG
            Email = "user1@test.com";
            Password = "12345678";
#endif
        }

        #endregion

        #region Properties

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

        public bool RememberMe
        {
            get { return rememberMe; }
            set { Set(ref rememberMe, value); }
        }

        public RelayCommand ForgotPasswordCommand { get; set; }

        public RelayCommand LoginCommand { get; set; }

        public RelayCommand BackCommand { get; set; }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            ForgotPasswordCommand.RaiseCanExecuteChanged();
            LoginCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Private methods

        private void ForgotPasswordCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.ForgotPassword);
        }

        private async void LoginCommandExecute()
        {
            var messageService = ServiceLocator.Locator.Get<IMessageService>();

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
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

            if (RememberMe)
            {
                var passwordVault = new PasswordVault();
                var credentials = new PasswordCredential(ApplicationKeys.QMunicateCredentials, Email, Password);
                passwordVault.Add(credentials);
            }

            var response = await QuickbloxClient.AuthenticationClient.CreateSessionWithEmailAsync(Email, Password,
                deviceRequestRequest:new DeviceRequest() {Platform = Platform.windows_phone, Udid = Helpers.GetHardwareId()});

            IsLoading = false;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                SettingsManager.Instance.WriteToSettings(SettingsKeys.CurrentUserId, response.Result.Session.UserId);
                NavigationService.Navigate(ViewLocator.Main, new DialogsNavigationParameter {CurrentUserId = response.Result.Session.UserId, Password = Password});
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await messageService.ShowAsync("Unauthorized", "Incorrect email or password");
            }
            else await Helpers.ShowErrors(response.Errors, messageService);

        }

        #endregion

    }
}
