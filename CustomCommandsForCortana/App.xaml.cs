using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using MetroLog;
using MetroLog.Targets;
using Windows.Foundation;
using System.Xml;
using System.Collections;
using CustomCommandsForCortana.Model;
using System.Xml.Linq;
using System.IO;
using System.Diagnostics;

namespace CustomCommandsForCortana
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    sealed partial class App : Application
    {

        private ILogManager logManager;
        private string path;
        public static XmlDocument settingsDoc = new XmlDocument();
        public static SettingsViewModel settingsViewModel { get; set; }
        public static string fileSettingsXMLPath;

        /// <summary>
        /// Initialisiert das Singletonanwendungsobjekt. Dies ist die erste Zeile von erstelltem Code
        /// und daher das logische Äquivalent von main() bzw. WinMain().
        /// </summary>
        public App()
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileCustomCommands.xml");
            // Initialize MetroLog using the defaults
            LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new StreamingFileTarget());
            this.logManager = LogManagerFactory.DefaultLogManager;

            this.logManager.GetLogger<App>().Info("App Start");

            GlobalCrashHandler.Configure();

            this.InitializeComponent();
            this.Suspending += OnSuspending;
            //this.RegisterVoiceCommands();

            // Settings
            App.settingsViewModel = new SettingsViewModel();
            GetDefaultSettingsAsync();
        }

        private async void GetDefaultSettingsAsync()
        {
            //StorageFile settingsFile = await Package.Current.InstalledLocation.GetFileAsync(@"settings.xml");

            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var existingFile = await localFolder.TryGetItemAsync("settings.xml");

            if (existingFile == null)
            {
                // Copy the file from the install folder to the local folder
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation/*.GetFolderAsync("MyFolder")*/;
                var file = await folder.GetFileAsync("settings.xml");
                if (file != null)
                {
                    await file.CopyAsync(localFolder, "settings.xml", Windows.Storage.NameCollisionOption.FailIfExists);
                }
            }
            fileSettingsXMLPath = Path.Combine(localFolder.Path, "settings.xml");
            Debug.WriteLine(fileSettingsXMLPath);
            LoadSettings(App.settingsViewModel);
        }

        private void LoadSettings(SettingsViewModel model)
        {
            // Load defaults

            // Load settings
            XmlDocument settingsDoc = App.settingsDoc;
            try { settingsDoc.Load(fileSettingsXMLPath); }
            catch (XmlException e)
            {
                System.Diagnostics.Debug.WriteLine("Error loading settings: " + e.Message);
                return;
            }
            catch (FileNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine("Error loading settings: " + e.Message);
                return;
            }

            IEnumerator docChildNodes = settingsDoc.ChildNodes.GetEnumerator();
            while (docChildNodes.MoveNext())
            {
                XmlNode currentNode = (docChildNodes.Current as XmlNode);
                if (currentNode.Name == "settings")
                {
                    IEnumerator docNodes = currentNode.ChildNodes.GetEnumerator();

                    while (docNodes.MoveNext())
                    {
                        XmlNode currentSubNode = (docNodes.Current as XmlNode);
                        Setting setting = new Setting();
                        XmlAttributeCollection currentAttrib = currentSubNode.Attributes;

                        string name = currentAttrib.GetNamedItem("name").Value ?? "";
                        Debug.WriteLine(name);
                        string value = currentAttrib.GetNamedItem("value").Value ?? "";
                        Debug.WriteLine(value);
                        string description = currentAttrib.GetNamedItem("description").Value ?? "";
                        Debug.WriteLine(description);

                        IEnumerator attributes = currentAttrib.GetEnumerator();
                        while (attributes.MoveNext())
                        {
                            XmlAttribute attr = (attributes.Current as XmlAttribute);
                            Debug.WriteLine("Attribute");
                            Debug.WriteLine(attr.Name);
                            Debug.WriteLine(attr.Value);
                        }

                        switch (currentSubNode.Name)
                        {
                            case "string":
                                {
                                    setting = new StringSetting(name, value, description);
                                    break;
                                }
                            /*case "int":// TODO cast string to int
                                {
                                    setting = new IntSetting(name, value, description);
                                    break;
                                }*/
                            case "toggle":
                                {
                                    setting = new ToggleSetting(name, (value == "true"), description);
                                    break;
                                }
                            default:
                                {
                                    Debug.WriteLine("DEFAULT");
                                    Debug.WriteLine(name + value + description);
                                    Debug.WriteLine(currentSubNode.Name);
                                    Debug.WriteLine(setting.Name + setting.Value + setting.Description);
                                    break;
                                }
                        }
                        Debug.WriteLine(setting.ToString());
                        model.Settings.Add(setting);
                    }

                }
            }
        }

        public static Setting GetStringSettingByName(string name)
        {
            foreach (Setting setting in settingsViewModel.Settings)
            {
                if (setting.Name.ToLower() == name.ToLower())
                {
                    return setting;
                }
            }
            return null;
        }

        protected override void OnActivated(IActivatedEventArgs e)
        {
            base.OnActivated(e);
            if (e.Kind != ActivationKind.VoiceCommand)
            {
                return;
            }

            var commandArgs = e as VoiceCommandActivatedEventArgs;
            var speechRecognitionResult = commandArgs.Result;
            var command = speechRecognitionResult.Text;

            // Get the name of the voice command and the text spoken.
            string voiceCommandName = speechRecognitionResult.RulePath[0];
            //CustomCommandsForCortana.Models.CommandViewModel cvm = new CustomCommandsForCortana.Models.CommandViewModel();
            System.Diagnostics.Debug.WriteLine(voiceCommandName);
            //TODO do something..

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

            if (rootFrame.Content == null)
            {
                //use the "command" which was spoken by the user.
                // i am just checking weather the Activation is by Voice command or not and navigate.
                if (e.Kind != ActivationKind.VoiceCommand)
                {
                    rootFrame.Navigate(typeof(MainPage), e.Kind);
                }
                else
                {
                    rootFrame.Navigate(typeof(MainPage), e.Kind);
                }

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
            }
            // Ensure the current window is active
            Window.Current.Activate();


        }

        /// <summary>
        /// Wird aufgerufen, wenn die Anwendung durch den Endbenutzer normal gestartet wird. Weitere Einstiegspunkte
        /// werden z. B. verwendet, wenn die Anwendung gestartet wird, um eine bestimmte Datei zu öffnen.
        /// </summary>
        /// <param name="e">Details über Startanforderung und -prozess.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // App-Initialisierung nicht wiederholen, wenn das Fenster bereits Inhalte enthält.
            // Nur sicherstellen, dass das Fenster aktiv ist.
            if (rootFrame == null)
            {
                // Frame erstellen, der als Navigationskontext fungiert und zum Parameter der ersten Seite navigieren
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Zustand von zuvor angehaltener Anwendung laden
                }

                // Den Frame im aktuellen Fenster platzieren
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // Wenn der Navigationsstapel nicht wiederhergestellt wird, zur ersten Seite navigieren
                    // und die neue Seite konfigurieren, indem die erforderlichen Informationen als Navigationsparameter
                    // übergeben werden
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Sicherstellen, dass das aktuelle Fenster aktiv ist
                Window.Current.Activate();
            }
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

        /// <summary>
        /// Wird aufgerufen, wenn die Ausführung der Anwendung angehalten wird.  Der Anwendungszustand wird gespeichert,
        /// ohne zu wissen, ob die Anwendung beendet oder fortgesetzt wird und die Speicherinhalte dabei
        /// unbeschädigt bleiben.
        /// </summary>
        /// <param name="sender">Die Quelle der Anhalteanforderung.</param>
        /// <param name="e">Details zur Anhalteanforderung.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Anwendungszustand speichern und alle Hintergrundaktivitäten beenden
            deferral.Complete();
        }

        private void OnLoggingEnabled(ILoggingChannel sender, object args)
        {
            // Here, you could note a change in the level or keywords.
        }
    }
}
