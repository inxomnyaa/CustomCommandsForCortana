using CustomCommandsForCortana.VoiceCommandService.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation.Diagnostics;
using MetroLog;
using MetroLog.Targets;
using System.Diagnostics;
using System.Xml;
using System.IO;

namespace CustomCommandsForCortana.VoiceCommandService
{
    public sealed class VoiceCommandService : IBackgroundTask
    {

        private CommandViewModel ViewModel { get; set; }
        private Collection<CustomCommand> commands = new Collection<CustomCommand> { };
        private string searchTerm = "";
        private string path;
        private XmlDocument doc;

        /// <summary>
        /// the service connection is maintained for the lifetime of a cortana session, once a voice command
        /// has been triggered via Cortana.
        /// </summary>
        VoiceCommandServiceConnection voiceServiceConnection;

        /// <summary>
        /// Lifetime of the background service is controlled via the BackgroundTaskDeferral object, including
        /// registering for cancellation events, signalling end of execution, etc. Cortana may terminate the 
        /// background service task if it loses focus, or the background task takes too long to provide.
        /// 
        /// Background tasks can run for a maximum of 30 seconds.
        /// </summary>
        BackgroundTaskDeferral serviceDeferral;
        private static string command;
        private static int skipCounter = 0;
        // Command type - defines how the command data is interpreted and executed
        private readonly int TYPE_CMD = 0;
        private readonly int TYPE_SERIAL = 1;
        private readonly int TYPE_CURL = 2;

        static string Result { get; set; }

        /// <summary>
        /// Background task entrypoint. Voice Commands using the <VoiceCommandService Target="...">
        /// tag will invoke this when they are recognized by Cortana, passing along details of the 
        /// invocation. 
        /// 
        /// Background tasks must respond to activation by Cortana within 0.5 seconds, and must 
        /// report progress to Cortana every 5 seconds (unless Cortana is waiting for user
        /// input). There is no execution time limit on the background task managed by Cortana,
        /// but developers should use plmdebug (https://msdn.microsoft.com/en-us/library/windows/hardware/jj680085%28v=vs.85%29.aspx)
        /// on the Cortana app package in order to prevent Cortana timing out the task during
        /// debugging.
        /// 
        /// Cortana dismisses its UI if it loses focus. This will cause it to terminate the background
        /// task, even if the background task is being debugged. Use of Remote Debugging is recommended
        /// in order to debug background task behaviors. In order to debug background tasks, open the
        /// project properties for the app package (not the background task project), and enable
        /// Debug -> "Do not launch, but debug my code when it starts". Alternatively, add a long
        /// initial progress screen, and attach to the background task process while it executes.
        /// </summary>
        /// <param name="taskInstance">Connection to the hosting background service process.</param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Start Task");
            skipCounter = 0;
            Result = "";
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileCustomCommands.xml");

            Debug.WriteLine("deferral");
            serviceDeferral = taskInstance.GetDeferral();

            // Register to receive an event if Cortana dismisses the background task. This will
            // occur if the task takes too long to respond, or if Cortana's UI is dismissed.
            // Any pending operations should be cancelled or waited on to clean up where possible.
            taskInstance.Canceled += OnTaskCanceled;

            Debug.WriteLine("details");
            var triggerDetails = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            Debug.WriteLine(triggerDetails.Name);

            // This should match the uap:AppService and VoiceCommandService references from the 
            // package manifest and VCD files, respectively. Make sure we've been launched by
            // a Cortana Voice Command.
            if (triggerDetails != null && triggerDetails.Name == "VoiceCommandService")
            {
                try
                {
                    voiceServiceConnection =
                        VoiceCommandServiceConnection.FromAppServiceTriggerDetails(
                            triggerDetails);

                    voiceServiceConnection.VoiceCommandCompleted += OnVoiceCommandCompleted;

                    // GetVoiceCommandAsync establishes initial connection to Cortana, and must be called prior to any 
                    // messages sent to Cortana. Attempting to use ReportSuccessAsync, ReportProgressAsync, etc
                    // prior to calling this will produce undefined behavior.
                    VoiceCommand voiceCommand = await voiceServiceConnection.GetVoiceCommandAsync();

                    // TODO Clean up debug - this only slows down the progressing and dumps unused variables
                    Debug.WriteLine("CommandName");
                    Debug.WriteLine(voiceCommand.CommandName);
                    Debug.WriteLine("Properties");
                    Debug.WriteLine(voiceCommand.Properties.Values);
                    if (voiceCommand.Properties.Count != 0)
                    {
                        Debug.WriteLine("got Properties");
                        foreach (var property in voiceCommand.Properties)
                        {
                            Debug.WriteLine("Property: Key: " + property.Key + " Value: " + property.Value);
                        }
                        if (voiceCommand.Properties.ContainsKey("searchTerm"))
                        {
                            Debug.WriteLine("got searchTerm");
                            this.searchTerm = voiceCommand.Properties["searchTerm"][0];//TODO multiple terms support
                        }
                    }
                    Debug.WriteLine("searchTerm");
                    Debug.WriteLine(searchTerm);
                    foreach (KeyValuePair<string, IReadOnlyList<string>> property in voiceCommand.Properties)
                    {
                        Debug.WriteLine(property.Key);
                        Debug.WriteLine(property.Value);
                        foreach (string content in property.Value)
                        {
                            Debug.WriteLine(content);
                        }
                    }

                    await ShowProgressScreen("Loading commands");

                    LoadCommands();// TODO - only load the 1 specific command to reduce load time

                    this.ViewModel = new CommandViewModel()
                    {
                        GetCommands = this.commands
                    };

                    Debug.WriteLine("Loaded " + this.commands.Count.ToString() + " commands from file");

                    try
                    {
                        if (!this.ViewModel.GetCommands.Any(i => i.Name == voiceCommand.CommandName))
                            LaunchAppInForeground();
                        else
                        {
                            CustomCommand foundCMD = this.ViewModel.GetCommands.Single(i => i.Name == voiceCommand.CommandName);
                            await SendCompletionMessage(foundCMD);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                        Debug.WriteLine("Command not found, launching in foreground");
                        LaunchAppInForeground();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Handling Voice Command failed. Exception:");
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }

                if (this.serviceDeferral != null)
                {
                    this.serviceDeferral.Complete();
                }
            }
        }

        private void LoadCommands()
        {
            doc = new XmlDocument();
            try { doc.Load(path); }
            catch (XmlException e)
            {
                System.Diagnostics.Debug.WriteLine("Error loading commands: " + e.Message);
                return;
            }
            catch (FileNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine("Error loading commands: " + e.Message);
                LaunchAppInForeground();
                return;
            }

            try
            {
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
                            this.commands.Add(new CustomCommand()
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.ToString());
            }
        }

        private async Task ShowProgressScreen(string message)
        {
            var userProgressMessage = new VoiceCommandUserMessage();
            userProgressMessage.DisplayMessage = userProgressMessage.SpokenMessage = message;

            VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userProgressMessage);
            await voiceServiceConnection.ReportProgressAsync(response);
        }

        private async Task SendCompletionMessage(CustomCommand custom)
        {
            // If this operation is expected to take longer than 0.5 seconds, the task must
            // supply a progress response to Cortana before starting the operation, and
            // updates must be provided at least every 5 seconds.
            await ShowProgressScreen("Executing the command \"" + custom.Name + "\"");

            int type = TYPE_CMD;// TODO add command type - CMD, SERIAL, HTTP/CURL

            try
            {
                switch (type)
                {
                    case 0:// TODO figure out how to use the TYPE_* variable here
                        {
                            Debug.WriteLine("ProcessStartInfo");

                            ProcessStartInfo cmdStartInfo = new ProcessStartInfo
                            {
                                FileName = @"C:\Windows\System32\cmd.exe",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                RedirectStandardInput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            Debug.WriteLine("Process");

                            Process cmdProcess = new Process
                            {
                                StartInfo = cmdStartInfo,
                                EnableRaisingEvents = true
                            };
                            cmdProcess.ErrorDataReceived += Cmd_Error;
                            cmdProcess.OutputDataReceived += Cmd_DataReceived;
                            cmdProcess.Start();
                            cmdProcess.BeginOutputReadLine();
                            cmdProcess.BeginErrorReadLine();

                            Debug.WriteLine("Command");
                            command = custom.BatchCommand.Replace("{searchTerm}", searchTerm);
                            Debug.WriteLine(command);

                            cmdProcess.StandardInput.WriteLine(custom.BatchCommand.Replace("{searchTerm}", searchTerm));//Execute batch command
                            cmdProcess.StandardInput.WriteLine("exit");//Execute exit.

                            Debug.WriteLine("WaitForExit");

                            cmdProcess.WaitForExit();

                            Debug.WriteLine("Done waiting");

                            break;
                        }
                    default:
                        {
                            Debug.WriteLine("TYPE \"" + type + "\" is not implemented yet");
                            throw new NotImplementedException();
                        }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            // Done. Return feedback

            Result = Result.Trim();

            Debug.WriteLine("Result:");
            Debug.WriteLine(Result);

            if (Result.Length > 0)
            {
                var userMessage = new VoiceCommandUserMessage
                {
                    DisplayMessage = "The command \"" + custom.Name + "\" has been executed. Result:\n" + Result,
                    //SpokenMessage = custom.Feedback.Replace("{result}", Result) // TODO re-enable when ParamList in MainPage works
                    SpokenMessage = custom.Feedback + Result
                };

                Debug.WriteLine("Feedback");

                VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userMessage);
                await voiceServiceConnection.ReportSuccessAsync(response);
            }
            else
            {

                var userMessage = new VoiceCommandUserMessage
                {
                    DisplayMessage = "The command \"" + custom.Name + "\" returned no data",
                    //SpokenMessage = custom.Feedback.Replace("{result}", Result) // TODO re-enable when ParamList in MainPage works
                    SpokenMessage = "An error occurred"
                };

                Debug.WriteLine("Feedback");

                VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userMessage);
                await voiceServiceConnection.ReportFailureAsync(response);
            }
        }


        static void Cmd_DataReceived(object sender, DataReceivedEventArgs e)
        {
            skipCounter += 1;
            if (e.Data != null && e.Data.Length > 0 && !e.Data.Contains("exit"))
            {
                Debug.WriteLine("Output from other process");
                Debug.WriteLine(e.Data);
                if (skipCounter >= 4 && !e.Data.Contains(command)) //Skip the first 4 lines of cmd (version, copyright..) and the command execution line
                {
                    Result += e.Data + "\n";
                }
            }
        }

        static void Cmd_Error(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine("Error from other process");
            Debug.WriteLine(e.Data);
            // Do nothing else. We can ignore CMD issues.
        }

        private async void LaunchAppInForeground()
        {
            var userMessage = new VoiceCommandUserMessage
            {
                SpokenMessage = "Launching Custom commands for Cortana"
            };

            var response = VoiceCommandResponse.CreateResponse(userMessage);

            await voiceServiceConnection.RequestAppLaunchAsync(response);
        }

        private void OnVoiceCommandCompleted(VoiceCommandServiceConnection sender, VoiceCommandCompletedEventArgs args)
        {
            if (this.serviceDeferral != null)
            {
                this.serviceDeferral.Complete();
            }
        }

        /// <summary>
        /// When the background task is cancelled, clean up/cancel any ongoing long-running operations.
        /// This cancellation notice may not be due to Cortana directly. The voice command connection will
        /// typically already be destroyed by this point and should not be expected to be active.
        /// </summary>
        /// <param name="sender">This background task instance</param>
        /// <param name="reason">Contains an enumeration with the reason for task cancellation</param>
        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            Debug.WriteLine("Task cancelled, clean up");
            if (this.serviceDeferral != null)
            {
                //Complete the service deferral
                this.serviceDeferral.Complete();
            }
        }
    }
}