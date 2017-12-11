using System;
using System.Threading.Tasks;

using UWP_UI_Project.Helpers;
using UWP_UI_Project.Views;

using Windows.ApplicationModel;

namespace UWP_UI_Project.Services
{
    public static class FirstRunDisplayService
    {
        internal static async Task ShowIfAppropriateAsync()
        {
            bool hasShownFirstRun = false;
            hasShownFirstRun = await Windows.Storage.ApplicationData.Current.LocalSettings.ReadAsync<bool>(nameof(hasShownFirstRun));

            if (!hasShownFirstRun)
            {
                await Windows.Storage.ApplicationData.Current.LocalSettings.SaveAsync(nameof(hasShownFirstRun), true);
                var dialog = new FirstRunDialog();
                await dialog.ShowAsync();
            }
        }
    }
}
