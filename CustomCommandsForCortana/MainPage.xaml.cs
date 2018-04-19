using System;
using CustomCommandsForCortana.Model;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.System;
using CortanaExtensions;
using CortanaExtensions.Commands;
using CortanaExtensions.Models;
using CustomCommandsForCortana.Dialogs;
using CortanaExtensions.Enums;
using System.Collections.Generic;
using CortanaExtensions.PhraseGroups;
using MetroLog;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Collections;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace CustomCommandsForCortana
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ILogger log = LogManagerFactory.DefaultLogManager.GetLogger<MainPage>();
        public string fileCommandsXMLPath;
        public string fileSettingsXMLPath;
        private XmlDocument doc;

        public CommandViewModel ViewModel { get; set; }
        public MainPage()
        {
            InitializeComponent();
            ViewModel = new CommandViewModel();
            fileCommandsXMLPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileCustomCommands.xml");
            fileSettingsXMLPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "settings.xml");
            System.Diagnostics.Debug.WriteLine("Load FileCustomCommand from xml");
            LoadCommands(ViewModel);
            System.Diagnostics.Debug.WriteLine("Loaded " + ViewModel.Commands.Count.ToString() + " commands from file");
        }

        private void MenuButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            // Using the Tag property value lets you localize the Label value
            // without affecting the app code.

            this.DisplayDeleteCommandDialog();
        }

        private void ToggleActiveState()
        {
            if (ListCommands.SelectedItems.Count != 1) return;
            return; //TODO prob re-implement
            //this.ViewModel.Commands[ListCommands.SelectedIndex].Active = !this.ViewModel.Commands[ListCommands.SelectedIndex].Active;
            //this.ViewModel.Commands[ListCommands.SelectedIndex] = this.ViewModel.Commands[ListCommands.SelectedIndex];
        }

        private async void DisplayDeleteCommandDialog()
        {

            foreach (CustomCommand command in ListCommands.SelectedItems)// TODO error & empty field handling
            {
                ContentDialog deleteFileDialog = new ContentDialog
                {
                    Title = "Delete command?",
                    Content = command.Summary + "\nIf you delete this command, you won't be able to recover it. Do you want to delete it?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel"
                };

                ContentDialogResult result = await deleteFileDialog.ShowAsync();

                // Delete the file if the user clicked the primary button.
                /// Otherwise, do nothing.
                if (result == ContentDialogResult.Primary)
                {
                    // Delete the file.
                    this.ViewModel.Commands.Remove(command);
                }
                else { }
            }
        }

        private void ListCommands_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Delete: this.DisplayDeleteCommandDialog(); break;
                case VirtualKey.Enter: this.ExecuteSelected(); break;
                case VirtualKey.S: this.SaveXML(); break;
                case VirtualKey.F5: this.ReloadCommands(); break;
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            this.ReloadCommands();
        }

        private async void ReloadCommands()
        {
            ContentDialog reloadDialog = new ContentDialog
            {
                Title = "Reload commands?",
                Content = "Are you sure you want to reload the list? ALL UNSAVED CHANGES WILL NOT BE SAVED!",
                PrimaryButtonText = "Reload",
                CloseButtonText = "Cancel"
            };

            ContentDialogResult result = await reloadDialog.ShowAsync();

            // Delete the file if the user clicked the primary button.
            /// Otherwise, do nothing.
            if (result == ContentDialogResult.Primary)
            {
                this.ViewModel.Commands.Clear();
                this.LoadCommands(this.ViewModel);
            }
            else { }
        }

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            this.ToggleActiveState();
        }

        private void MenuButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            this.ShowAddContentDialog();
        }

        private void MenuButtonEdit_Click(object sender, RoutedEventArgs e)
        {
            this.ShowEditContentDialog();
        }

        private async void ShowAddContentDialog()
        {
            var dialog = new AddDialog("Add command");
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                this.ViewModel.Commands.Add(new CustomCommand()
                {
                    Name = dialog.result[0],
                    Example = dialog.result[1],
                    ListenFor = dialog.result[2],
                    Feedback = dialog.result[3],
                    BatchCommand = dialog.result[4]
                });
            }
            else
            {
            }
        }

        private async void ShowEditContentDialog()
        {
            foreach (CustomCommand command in ListCommands.SelectedItems)// TODO error & empty field handling
            {
                var dialog = new EditDialog("Edit command", new string[] { command.Name, command.Example, command.ListenFor, command.Feedback, command.BatchCommand });
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    ViewModel.Commands.Remove(command);
                    ViewModel.Commands.Add(new CustomCommand()
                    {
                        Name = dialog.result[0],
                        Example = dialog.result[1],
                        ListenFor = dialog.result[2],
                        Feedback = dialog.result[3],
                        BatchCommand = dialog.result[4]
                    });
                }
                else
                {
                }
            }
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteSelected();
        }

        private void ExecuteSelected()
        {
            if (ListCommands.SelectedItems.Count != 1) return;
            this.ViewModel.Commands[ListCommands.SelectedIndex].Execute();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            this.SaveXML();
        }

        private void LoadCommands(CommandViewModel model)
        {
            doc = new XmlDocument();
            try { doc.Load(fileCommandsXMLPath); }
            catch (XmlException e)
            {
                System.Diagnostics.Debug.WriteLine("Error loading commands: " + e.Message);
                return;
            }
            catch (FileNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine("Error loading commands: " + e.Message);
                return;
            }

            IEnumerator docChildNodes = doc.ChildNodes.GetEnumerator();
            while (docChildNodes.MoveNext())
            {
                XmlNode currentNode = (docChildNodes.Current as XmlNode);
                if (currentNode.Name == "commands")
                {
                    IEnumerator docNodes = currentNode.ChildNodes.GetEnumerator();

                    while (docNodes.MoveNext())
                    {
                        XmlNode currentSubNode = (docNodes.Current as XmlNode);
                        XmlAttributeCollection currentAttrib = currentSubNode.Attributes;
                        model.Commands.Add(new CustomCommand()
                        {
                            Name = currentAttrib.GetNamedItem("name").Value,
                            Example = currentAttrib.GetNamedItem("example").Value,
                            ListenFor = currentAttrib.GetNamedItem("listenfor").Value,
                            Feedback = currentAttrib.GetNamedItem("feedback").Value,
                            BatchCommand = currentAttrib.GetNamedItem("batchcommand").Value
                        });
                    }

                }
            }
        }

        private async void SaveXML1()
        {
            List<string> Places = new List<string> { "Auckland", "Wellington", "Christchurch" };
            var VCD = new VoiceCommandDefinition()
            {
                CommandSets =
                {
                    new CommandSet("en")
                    {
                        Example = "Book trips to Destinations",
                        AppName = "TravelPlan",
                        Name = "TravelPlanCommands(English)",
                        Commands =
                        {
                            new ForegroundActivatedVoiceCommand("Command")
                            {
                                Example = "Book a Trip to Wellington",
                                Feedback = "Booking Trip",
                                ListenStatements =
                                {
                                    new ListenStatement("Book [a] Trip [to] {Places}", RequireAppName.BeforeOrAfterPhrase),
                                    new ListenStatement("[I] [would] [like] [to] Book [a] [my] [Holiday] [Trip] to {searchTerm}",
                                    RequireAppName.BeforeOrAfterPhrase)
                                },
                                AppTarget = "BOOKTRIP"
                            }
                        },
                        PhraseLists =
                        {
                            new PhraseList("Places", Places)
                        },
                        PhraseTopics =
                        {
                            new PhraseTopic("searchTerm", PhraseTopicScenario.Search)
                            {
                                Subjects =
                                {
                                    PhraseTopicSubject.CityORState
                                }
                            }
                        }
                    }
                }
            };
            await VCD.CreateAndInstall();
        }

        private async void SaveXML()
        {
            Setting appNameSetting = App.GetStringSettingByName("appname");
            string appName = "Command";
            if(appNameSetting != null && appNameSetting.Value.Trim().Length > 0)
            {
                appName = appNameSetting.Value.Trim();
            }
            var xmlNode = new XElement("commands");
            var VCD = new VoiceCommandDefinition() { };
            var CS = new CommandSet("de-de")
            {
                Example = "Command do something",
                AppName = appName,//prefix TODO StringSetting variable
                Name = "CommandCommandSet_de-de",
                PhraseTopics =
                {
                     new PhraseTopic("searchTerm", PhraseTopicScenario.ShortMessage){ }
                }/*,
                PhraseLists =
                {
                    new PhraseList("result", new List<string>{ })
                }*/
            };
            foreach (CustomCommand customcommand in this.ViewModel.Commands)
            {
                // Add VoiceCommand
                var command = new BackgroundActivatedVoiceCommand(customcommand.Name)
                {
                    Example = customcommand.Example,
                    Feedback = customcommand.Feedback,
                    ListenStatements =
                    {
                        new ListenStatement(customcommand.ListenFor, RequireAppName.BeforePhrase)
                    },
                    BackgroundTarget = "VoiceCommandService"
                };
                CS.Commands.Add(command);
                XElement xmlCommand =
                    new XElement("command",
                        new XAttribute("name", customcommand.Name),
                        new XAttribute("example", customcommand.Example),
                        new XAttribute("listenfor", customcommand.ListenFor),
                        new XAttribute("feedback", customcommand.Feedback),
                        new XAttribute("batchcommand", customcommand.BatchCommand)
                        );
                xmlNode.Add(xmlCommand);
            }
            VCD.CommandSets.Add(CS);
            try
            {
                // Save and install VCD file
                await VCD.CreateAndInstall();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(CS.Commands);
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            // Save FileCustomCommands.xml
            xmlNode.Save(fileCommandsXMLPath);

            ContentDialog savedDialog = new ContentDialog
            {
                IsSecondaryButtonEnabled = false,
                Title = "Saved commands!",
                Content = "",
                PrimaryButtonText = "Ok, cool!"
            };

            await savedDialog.ShowAsync();
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {

                Frame rootFrame = Window.Current.Content as Frame;
                /*
                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (rootFrame == null)
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();

                    rootFrame.NavigationFailed += OnNavigationFailed;

                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                }*/

                rootFrame.Navigate(typeof(Pages.SettingsPage));
                // Ensure the current window is active
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
    }
}
