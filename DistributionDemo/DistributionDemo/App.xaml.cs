using Prism;
using Prism.Ioc;
using DistributionDemo.ViewModels;
using DistributionDemo.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Prism.DryIoc;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using DistributionDemo.Helper;
using Microsoft.AppCenter.Distribute;
using System;
using System.Threading.Tasks;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace DistributionDemo
{
    public partial class App : PrismApplication
    {
        #region Constructor

        /* 
         * The Xamarin Forms XAML Previewer in Visual Studio uses System.Activator.CreateInstance.
         * This imposes a limitation in which the App class must have a default constructor. 
         * App(IPlatformInitializer initializer = null) cannot be handled by the Activator.
         */
        public App() : this(null) { }

        public App(IPlatformInitializer initializer) : base(initializer) { }

        #endregion

        #region Life Cycle

        protected override async void OnInitialized()
        {
            InitializeComponent();

            await NavigationService.NavigateAsync("NavigationPage/MainPage");
        }

        protected override void OnStart()
        {
            base.OnStart();
            Distribute.SetEnabledAsync(true);

            Distribute.ReleaseAvailable = OnReleaseAvailable;
            AppCenter.LogLevel = LogLevel.Verbose;

            AppCenter.Start($"ios={Constants.iOSAppCenterKey};android={Constants.AndroidAppCenterKey}",
                typeof(Analytics),
                typeof(Crashes),
                typeof(Distribute));
        }

        #endregion

        #region On New Release

        bool OnReleaseAvailable(ReleaseDetails releaseDetails)
        {
            string versionName = releaseDetails.ShortVersion;
            string versionCodeOrBuildNumber = releaseDetails.Version;
            string releaseNotes = releaseDetails.ReleaseNotes;
            Uri releaseNotesUrl = releaseDetails.ReleaseNotesUrl;

            // custom dialog
            var title = "Version " + versionName + " available!";
            Task response;

            // On mandatory update, user cannot postpone
            if (releaseDetails.MandatoryUpdate)
            {
                response = Current.MainPage.DisplayAlert(title, releaseNotes, "Download and Install");
            }
            else
            {
                response = Current.MainPage.DisplayAlert(title, releaseNotes, "Download and Install", "Maybe tomorrow...");
            }
            response.ContinueWith((task) =>
            {
                // If mandatory or if answer was positive
                if (releaseDetails.MandatoryUpdate || (task as Task<bool>).Result)
                {
                    // Notify SDK that user selected update
                    Distribute.NotifyUpdateAction(UpdateAction.Update);
                }
                else
                {
                    // Notify SDK that user selected postpone (for 1 day)
                    // Note that this method call is ignored by the SDK if the update is mandatory
                    Distribute.NotifyUpdateAction(UpdateAction.Postpone);
                }
            });

            // Return true if you are using your own dialog, false otherwise
            return true;
        }

        #endregion

        #region Register Types

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<MainPage>();
        }

        #endregion
    }
}
