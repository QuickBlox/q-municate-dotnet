using QMunicate.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Logger;
using QMunicate.Core.MessageService;
using QMunicate.Core.Navigation;
using QMunicate.Logger;
using QMunicate.Services;
using Quickblox.Sdk;

namespace QMunicate
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            var fileLogger = new FileLogger();
            var quickbloxClient = new QuickbloxClient(ApplicationKeys.ApplicationId, ApplicationKeys.AuthorizationKey, ApplicationKeys.AuthorizationSecret, ApplicationKeys.ApiBaseEndPoint, ApplicationKeys.ChatEndpoint, fileLogger);
            QmunicateLoggerHolder.LoggerInstance = fileLogger;
            var fileStorage = new FileStorage();

            ServiceLocator.Locator.Bind<INavigationService, NavigationService>(LifetimeMode.Singleton);
            ServiceLocator.Locator.Bind<IQuickbloxClient, QuickbloxClient>(quickbloxClient);
            ServiceLocator.Locator.Bind<IMessageService, MessageService>(LifetimeMode.Singleton);
            ServiceLocator.Locator.Bind<IDialogsManager, IDialogsManager>(new DialogsManager(quickbloxClient));
            ServiceLocator.Locator.Bind<IPushNotificationsManager, IPushNotificationsManager>(new PushNotificationsManager(quickbloxClient));
            ServiceLocator.Locator.Bind<IFileStorage, IFileStorage>(fileStorage);
            ServiceLocator.Locator.Bind<IImageService, IImageService>(new ImagesService(quickbloxClient, fileStorage));
            ServiceLocator.Locator.Bind<ICachingQuickbloxClient, ICachingQuickbloxClient>(new CachingQuickbloxClient(quickbloxClient));

            UnhandledException += OnUnhandledException;

            //Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
            //    Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
            //    Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            unhandledExceptionEventArgs.Handled = true;

            await QmunicateLoggerHolder.Log(QmunicateLogLevel.Error, unhandledExceptionEventArgs.Exception.ToString());
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

//#if DEBUG
//            if (System.Diagnostics.Debugger.IsAttached)
//            {
//                this.DebugSettings.EnableFrameRateCounter = true;
//            }
//#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                var navigationService = ServiceLocator.Locator.Get<INavigationService>();
                navigationService.Initialize(rootFrame, this.GetPageResolver());
                rootFrame.Navigate(typeof(WelcomePage), e.Arguments);                
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        private PageResolver GetPageResolver()
        {
            var dictionary = new Dictionary<string, Type>();
            dictionary.Add(ViewLocator.SignUp, typeof(SignUpPage));
            dictionary.Add(ViewLocator.Login, typeof(LoginPage));
            dictionary.Add(ViewLocator.ForgotPassword, typeof(ForgotPasswordPage));
            dictionary.Add(ViewLocator.Welcome, typeof(WelcomePage));
            dictionary.Add(ViewLocator.Main, typeof(MainPage));

            return new PageResolver(dictionary);
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
