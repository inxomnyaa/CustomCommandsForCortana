using CustomCommandsForCortana.Dialogs;
using CustomCommandsForCortana.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace CustomCommandsForCortana.Pages
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {

        //public SettingsViewModel ViewModel { get; set; }

        public SettingsPage()
        {
            this.InitializeComponent();
            //fileSettingsXMLPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "settings.xml");
        }

        private void MenuButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            rootFrame.Navigate(typeof(MainPage));
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Navigation auf eine bestimmte Seite fehlschlägt
        /// </summary>
        /// <param name="sender">Der Rahmen, bei dem die Navigation fehlgeschlagen ist</param>
        /// <param name="e">Details über den Navigationsfehler</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private async void MenuButtonSave_ClickAsync(object sender, RoutedEventArgs e)
        {
            var xmlNode = new XElement("settings");
            foreach (StringSetting setting in App.settingsViewModel.Settings)
            {
                xmlNode.Add(setting.ToXml());
            }

            xmlNode.Save(App.fileSettingsXMLPath);

            ContentDialog savedDialog = new ContentDialog
            {
                IsSecondaryButtonEnabled = false,
                Title = "Saved settings!",
                Content = "",
                PrimaryButtonText = "Ok, cool!"
            };

            await savedDialog.ShowAsync();
        }

        private void MenuButtonEdit_Click(object sender, RoutedEventArgs e)
        {
            this.ShowEditContentDialog();
        }

        private async void ShowEditContentDialog()
        {
            foreach (Setting setting in ListSettings.SelectedItems)// TODO error & empty field handling
            {
                var dialog = new EditSettingDialog(new string[] { setting.Name??"", setting.Description??"", setting.Value??"" });
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    setting.Value = dialog.result[0];
                    App.settingsViewModel.Settings.Remove(setting);
                    App.settingsViewModel.Settings.Add(setting);
                }
                else
                {
                }
            }
        }
    }
}
