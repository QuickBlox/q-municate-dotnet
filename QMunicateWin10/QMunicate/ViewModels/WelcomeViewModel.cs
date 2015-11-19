using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Navigation;
//using Facebook.Client;
using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.Models;
using QMunicate.Views;
using Quickblox.Sdk;

namespace QMunicate.ViewModels
{
    public class WelcomeViewModel : ViewModel
    {
        #region Fields

        private string copyrightString;

        #endregion

        #region Ctor

        public WelcomeViewModel()
        {
            FacebookSignUpCommand = new RelayCommand(FacebookSignUpCommandExecute, () => !IsLoading);
            EmailSignUpCommand = new RelayCommand(EmailSingUpCommandExecute, () => !IsLoading);
            LoginCommand = new RelayCommand(LoginCommandExecute, () => !IsLoading);
        }

        #endregion

        #region Properties

        public RelayCommand FacebookSignUpCommand { get; set; }
        public RelayCommand EmailSignUpCommand { get; set; }
        public RelayCommand LoginCommand { get; set; }

        public string CopyrightString
        {
            get { return copyrightString; }
            set { Set(ref copyrightString, value); }
        }

        #endregion

        #region Navigation

        public override void OnNavigatedTo(NavigationEventArgs e)
        {
            CopyrightString = string.Format("© QuickBlox {0} v. {1}", DateTime.Today.Year, Helpers.GetAppVersion());
        }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            FacebookSignUpCommand.RaiseCanExecuteChanged();
            EmailSignUpCommand.RaiseCanExecuteChanged();
            LoginCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Private methods

        private async void FacebookSignUpCommandExecute()
        {
            IsLoading = true;

            var messageService = ServiceLocator.Locator.Get<IMessageService>();
            if (!Helpers.IsInternetConnected())
            {
                await messageService.ShowAsync("Connection failed", "Please check your internet connection.");
                return;
            }

            //Session.OnFacebookAuthenticationFinished += OnFacebookAuthenticationFinished;
            //Session.ActiveSession.LoginWithBehavior("public_profile", FacebookLoginBehavior.LoginBehaviorMobileInternetExplorerOnly);
        }

        //private async void OnFacebookAuthenticationFinished(AccessTokenData fbSession)
        //{
        //    IsLoading = true;
        //    var sessionResponse = await QuickbloxClient.AuthenticationClient.CreateSessionWithSocialNetworkKey(ApplicationKeys.ApplicationId, ApplicationKeys.AuthorizationKey, ApplicationKeys.AuthorizationSecret, "facebook",
        //                                                        "public_profile", fbSession.AccessToken, null, null);
        //    if (sessionResponse.StatusCode == HttpStatusCode.Created)
        //    {
        //        QuickbloxClient.Token = sessionResponse.Result.Session.Token;
        //        SettingsManager.Instance.WriteToSettings(SettingsKeys.CurrentUserId, sessionResponse.Result.Session.UserId);
        //        NavigationService.Navigate(ViewLocator.Dialogs,
        //                                            new DialogsNavigationParameter
        //                                            {
        //                                                CurrentUserId = sessionResponse.Result.Session.UserId,
        //                                                Password = sessionResponse.Result.Session.Token
        //                                            });
        //    }

        //    IsLoading = false;
        //}

        private void EmailSingUpCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.SignUp);
        }

        private void LoginCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.Login);
        }

        #endregion

    }
}
